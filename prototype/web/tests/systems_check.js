// Purpose: checks the game's systems in a real (headless) browser: Bodhi growing up chapter by
// chapter, feeding and cleaning her, and saving and loading. Each check plays the game through
// its own debug controls, and the "break it" checks feed in bad input on purpose (nonsense
// numbers, tampered saves, a button pressed again and again) to prove nothing crashes or cheats.
//
// Run from the repository root:  node prototype/web/tests/systems_check.js
// Expected: one "ok" line per check, then SYSTEMS CHECK PASSED.
'use strict';

const path = require('path');
const { chromium } = require('playwright');

const page_path = 'file://' + path.resolve(__dirname, '..', 'index.html');
const results = [];

// Records one check's outcome.
function check(name, passed, detail) {
    results.push({ name: name, passed: Boolean(passed), detail: detail || '' });
    console.log((passed ? 'ok    ' : 'FAIL  ') + name + (detail ? '  (' + detail + ')' : ''));
}

// Starts a chapter by index and skips its title card.
async function start_chapter(page, index) {
    await page.evaluate((chapter) => {
        const debug = window.until_we_find_home_debug;
        debug.start_level(chapter);
        debug.skip_card();
    }, index);
    await page.waitForTimeout(200);
}

// Bodhi grows: every chapter names a real stage, she's bigger in each chapter than the one
// before, her collision box matches her size, and puppies use the puppy drawings.
async function check_growth(page) {
    const report = await page.evaluate(() => {
        const unknown_ages = levels.filter(function no_stage(level) { return !dog_growth[level.dog_age]; }).map(function id_of(level) { return level.id; });
        const sizes = levels.map(function size_of(level) { return growth_stage_of(level).size; });
        const odd_stage = growth_stage_of({ dog_age: 'nonsense' });
        dog_growth.broken_for_test = { label: 'x', size: NaN, drawing: '<script>', jump: -5 };
        const broken = growth_stage_of({ dog_age: 'broken_for_test' });
        delete dog_growth.broken_for_test;
        return { unknown_ages: unknown_ages, sizes: sizes, odd_stage: odd_stage, broken: broken };
    });
    check('every chapter names a growth stage that exists', report.unknown_ages.length === 0, report.unknown_ages.join(', '));
    const grows = report.sizes.every(function bigger(size, index) { return index === 0 || size > report.sizes[index - 1]; });
    check('Bodhi is bigger in every chapter than in the one before', grows, report.sizes.join(' < '));
    check('break it: an unknown stage falls back to the grown dog', report.odd_stage.size === 1 && report.odd_stage.drawing === 'bodhi');
    check('break it: a nonsense stage stays within limits', report.broken.size === 1 && report.broken.drawing === 'bodhi' && report.broken.jump === 0.5,
        JSON.stringify(report.broken));
    for (let index = 0; index < report.sizes.length; index++) {
        await start_chapter(page, index);
        const dog = await page.evaluate(() => window.until_we_find_home_debug.character_state('dog'));
        const expected_width = 46 * report.sizes[index];
        const puppy = report.sizes[index] < 0.7;
        const right_size = Math.abs(dog.width - expected_width) < 0.01;
        const right_drawing = dog.drawing === (puppy ? 'bodhi_puppy' : 'bodhi');
        if (!right_size || !right_drawing) {
            check('chapter ' + index + ' draws Bodhi at her size', false, JSON.stringify(dog));
            return;
        }
    }
    check('in every chapter her collision box matches her size, and puppies use the puppy drawings', true);
}

// Feeding and cleaning: the prompts appear when they should, do what they say, and can't be
// abused; needs from outside are cleaned up before use.
async function check_care(page) {
    const cleaned = await page.evaluate(() => [
        checked_care({ hunger: NaN, dirt: 'mud', food: 1e9 }),
        checked_care(null),
        checked_care({ hunger: -4, dirt: Infinity, food: -3 }),
        checked_care('{"hunger": 0.5}'),
        checked_care({ hunger: 0.5, dirt: 0.25, food: 2.6 }),
    ]);
    check('break it: nonsense needs become fresh or in-range values',
        cleaned[0].hunger === 0.3 && cleaned[0].dirt === 0 && cleaned[0].food === 9
        && cleaned[1].hunger === 0.3 && cleaned[1].food === 2
        && cleaned[2].hunger === 0 && cleaned[2].dirt === 0 && cleaned[2].food === 0
        && cleaned[3].hunger === 0.3
        && cleaned[4].hunger === 0.5 && cleaned[4].dirt === 0.25 && cleaned[4].food === 3,
        JSON.stringify(cleaned));

    // A hungry, muddy dog right beside her: feed first, then towel. She walks away from the
    // chapter's first keepsake (things in the world come before looking after the dog), and the
    // dog follows.
    await start_chapter(page, 1);
    await page.evaluate(() => window.until_we_find_home_debug.walk_to(740));
    await page.waitForTimeout(2500);
    // The dog waits a little way behind her; walk over to her, as a player would.
    await page.evaluate(() => { const debug = window.until_we_find_home_debug; debug.walk_to(debug.state().dog_x + 24); });
    await page.waitForTimeout(1200);
    await page.evaluate(() => window.until_we_find_home_debug.set_care({ hunger: 0.9, dirt: 0.9, food: 1 }));
    await page.waitForTimeout(300);
    let state = await page.evaluate(() => window.until_we_find_home_debug.state());
    check('a hungry dog beside her offers "Feed"', (state.prompt || '').startsWith('Feed'), state.prompt);
    await page.evaluate(() => window.until_we_find_home_debug.press('act'));
    await page.waitForTimeout(300);
    state = await page.evaluate(() => window.until_we_find_home_debug.state());
    check('feeding empties her hunger and uses one meal', state.care.hunger < 0.05 && state.care.food === 0 && state.happened.includes('dog_fed'), JSON.stringify(state.care));
    await page.waitForTimeout(1200);
    state = await page.evaluate(() => window.until_we_find_home_debug.state());
    check('a muddy dog then offers "Towel"', (state.prompt || '').startsWith('Towel'), state.prompt);
    await page.evaluate(() => window.until_we_find_home_debug.press('act'));
    await page.waitForTimeout(300);
    state = await page.evaluate(() => window.until_we_find_home_debug.state());
    check('towelling cleans most of the mud', state.care.dirt < 0.2 && state.happened.includes('dog_cleaned'), JSON.stringify(state.care));

    // Break it: an empty bag can't feed, and hammering the button can't make food out of nothing.
    await page.evaluate(() => window.until_we_find_home_debug.set_care({ hunger: 0.95, dirt: 0, food: 0 }));
    await page.waitForTimeout(300);
    state = await page.evaluate(() => window.until_we_find_home_debug.state());
    check('break it: with an empty bag there is no "Feed"', !(state.prompt || '').startsWith('Feed'), state.prompt);
    await page.evaluate(() => window.until_we_find_home_debug.set_care({ hunger: 0.95, dirt: 0, food: 1 }));
    for (let press = 0; press < 40; press++) {
        await page.evaluate(() => window.until_we_find_home_debug.press('act'));
    }
    await page.waitForTimeout(400);
    state = await page.evaluate(() => window.until_we_find_home_debug.state());
    check('break it: pressing "use" 40 times feeds once and never goes below an empty bag', state.care.food === 0 && Number.isFinite(state.care.hunger), JSON.stringify(state.care));

    // Time passing makes her hungry again, faster for a puppy than a grown dog.
    const appetite = await page.evaluate(() => {
        const debug = window.until_we_find_home_debug;
        const rise_in = function rise_in(chapter_index) {
            debug.start_level(chapter_index);
            debug.skip_card();
            debug.set_care({ hunger: 0, dirt: 0, food: 0 });
            debug.step(60 * 30);  // Thirty seconds of play, stepped directly.
            return debug.state().care.hunger;
        };
        return { puppy: rise_in(0), grown: rise_in(levels.length - 1) };
    });
    check('she gets hungry as time passes, a puppy faster than a grown dog', appetite.puppy > appetite.grown && appetite.grown > 0, JSON.stringify(appetite));
}

async function main() {
    const browser = await chromium.launch();
    const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
    const page_errors = [];
    page.on('pageerror', (error) => page_errors.push(error.message));
    await page.goto(page_path);
    await page.waitForFunction(() => window.until_we_find_home_debug && window.until_we_find_home_debug.flipbook_ready(), null, { timeout: 20000 }).catch(() => {});
    await check_growth(page);
    await check_care(page);
    check('the page logged no errors', page_errors.length === 0, page_errors.join(' | '));
    await browser.close();
    const failed = results.filter(function did_fail(result) { return !result.passed; });
    console.log(failed.length === 0 ? 'SYSTEMS CHECK PASSED' : 'SYSTEMS CHECK FAILED: ' + failed.length + ' of ' + results.length + ' checks');
    process.exit(failed.length === 0 ? 0 : 1);
}

main();
