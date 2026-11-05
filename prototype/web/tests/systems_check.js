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
const station_chapter = 3;   // "The Other Side": no opening film, a clear spot to stand, a dog section.
const puppy_chapter = 2;     // "The Last Morning": Bodhi at five months, no opening film.

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
    await start_chapter(page, station_chapter);
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
    const appetite = await page.evaluate(({ chapter_indexes }) => {
        const debug = window.until_we_find_home_debug;
        const rise_in = function rise_in(chapter_index) {
            debug.start_level(chapter_index);
            debug.skip_card();
            debug.set_care({ hunger: 0, dirt: 0, food: 0 });
            debug.step(60 * 30);  // Thirty seconds of play, stepped directly.
            return debug.state().care.hunger;
        };
        return { puppy: rise_in(chapter_indexes.puppy), grown: rise_in(levels.length - 1) };
    }, { chapter_indexes: { puppy: puppy_chapter } });
    check('she gets hungry as time passes, a puppy faster than a grown dog', appetite.puppy > appetite.grown && appetite.grown > 0, JSON.stringify(appetite));
}

// Saving and continuing: a save made mid-chapter survives closing the page and puts the story,
// the characters and Bodhi's needs back as they were. Tampered and broken saves are refused
// and counted, and never crash the game or skip the story.
async function check_saving(page) {
    const debug_state = () => page.evaluate(() => window.until_we_find_home_debug.state());
    const wait_for_event = async (name) => {
        for (let tries = 0; tries < 300; tries++) {
            if ((await debug_state()).happened.includes(name)) {
                return true;
            }
            await page.waitForTimeout(50);
        }
        return false;
    };
    // Play chapter one up to the dog's section: the walkway collapses, she restores the power.
    await page.evaluate(() => { localStorage.clear(); window.until_we_find_home_debug.set_speed(3); });
    await start_chapter(page, station_chapter);
    await page.evaluate(() => window.until_we_find_home_debug.walk_to(1200));
    await wait_for_event('separation_scene_finished');
    await page.evaluate(() => window.until_we_find_home_debug.walk_to(220));
    await page.waitForTimeout(2500);
    await page.evaluate(() => window.until_we_find_home_debug.press('act'));
    await wait_for_event('power_restored');
    await page.waitForTimeout(400);
    await page.evaluate(() => window.until_we_find_home_debug.set_care({ hunger: 0.6, dirt: 0.4, food: 4 }));
    // Save at the next safe moment (nobody in mid-air), and pause in the same instant, so no
    // autosave can replace this save before the page is closed.
    let saved = false;
    for (let tries = 0; tries < 60 && !saved; tries++) {
        saved = await page.evaluate(() => {
            const debug = window.until_we_find_home_debug;
            const made = debug.save_now();
            if (made) {
                debug.set_paused(true);
            }
            return made;
        });
        if (!saved) {
            await page.waitForTimeout(100);
        }
    }
    const before = await debug_state();
    const saved_file = await page.evaluate(() => JSON.parse(localStorage.getItem('until_we_find_home_save')));
    check('a save is made mid-chapter', saved && before.controlled === 'dog', before.controlled);

    // Close the page and open it again, as when the app is closed: Continue picks up from the save.
    await page.reload();
    await page.waitForFunction(() => window.until_we_find_home_debug && window.until_we_find_home_debug.flipbook_ready(), null, { timeout: 20000 }).catch(() => {});
    const title_text = await page.evaluate(() => document.getElementById('begin_button').textContent + ' | ' + document.getElementById('continue_note').textContent);
    check('after reopening, the title offers Continue and names the chapter', title_text.startsWith('Continue') && title_text.includes('The Other Side'), title_text);
    // Continue, and pause in the same instant, so the world hasn't moved on when it's read.
    await page.evaluate(() => { const debug = window.until_we_find_home_debug; debug.continue_save(); debug.skip_card(); debug.set_paused(true); });
    const after = await debug_state();
    await page.evaluate(() => window.until_we_find_home_debug.set_paused(false));
    const gate_open = await page.evaluate(() => window.until_we_find_home_debug.solid_active('luggage_gate'));
    const walkway = await page.evaluate(() => window.until_we_find_home_debug.solid_active('walkway'));
    const same_events = before.happened.every(function kept(name) { return after.happened.includes(name); });
    check('continuing restores the story so far (events, the open gate, the fallen walkway)', same_events && gate_open === false && walkway === false, 'gate active: ' + gate_open + ', walkway active: ' + walkway);
    check('continuing restores who you control and where they stand',
        after.controlled === 'dog' && Math.abs(after.dog_x - saved_file.dog[0]) < 1.5 && Math.abs(after.human_x - saved_file.human[0]) < 1.5,
        'dog saved at ' + saved_file.dog[0].toFixed(1) + ', back at ' + after.dog_x + '; her saved at ' + saved_file.human[0].toFixed(1) + ', back at ' + after.human_x);
    check("continuing restores Bodhi's needs", Math.abs(after.care.hunger - 0.6) < 0.02 && after.care.food === 4, JSON.stringify(after.care));
    check('replaying the save is silent (no story lines queued)', after.subtitle === '' || after.subtitle === before.subtitle, after.subtitle);

    // Pausing saves.
    await page.evaluate(() => { const debug = window.until_we_find_home_debug; debug.walk_to(1700); });
    await page.waitForTimeout(1500);
    const paused_save = await page.evaluate(() => { const debug = window.until_we_find_home_debug; debug.set_paused(true); const save = debug.read_save(); debug.set_paused(false); return save; });
    check('pausing saves the game', paused_save && paused_save.level_index === station_chapter && !paused_save.at_start);

    // Finishing a chapter moves the save to the start of the next one.
    await page.evaluate(() => window.until_we_find_home_debug.finish_chapter());
    const next_save = await page.evaluate(() => window.until_we_find_home_debug.read_save());
    check('finishing a chapter saves the start of the next', next_save && next_save.level_index === station_chapter + 1 && next_save.at_start, JSON.stringify(next_save && { level_index: next_save.level_index, at_start: next_save.at_start }));

    // Break it: tampered and broken saves.
    const verdicts = await page.evaluate(() => {
        const debug = window.until_we_find_home_debug;
        const good = { version: 1, chapter: 'other_side', events: ['level_started'], human: [560, 300], dog: [660, 300], controlled: 'human', care: { hunger: 0.2, dirt: 0, food: 2 }, bond: { fear: 0.1, closeness: 0.3 }, seconds: 3 };
        const variant = function variant(changes) { return JSON.stringify(Object.assign({}, good, changes)); };
        const attempts = {
            'not JSON': '{"version": 1, "chapter": ',
            'far too big': 'x'.repeat(20000),
            'a newer version': variant({ version: 2 }),
            'a chapter that does not exist': variant({ chapter: 'nowhere' }),
            'an event that cannot happen (a spoofed shortcut)': variant({ events: ['level_started', 'shared_route_opened'] }),
            'the same event twice': variant({ events: ['level_started', 'walkway_collapsed', 'walkway_collapsed'] }),
            'the event that ends the chapter': variant({ events: ['level_started', 'chapter_done'] }),
            'a position of infinity': '{"version":1,"chapter":"other_side","events":["level_started"],"human":[1e999,300],"dog":[660,300],"controlled":"human"}',
            'a character that does not exist': variant({ controlled: 'cat' }),
            'events that are not a list': variant({ events: { 0: 'level_started' } }),
            'not starting at the start': variant({ events: ['walkway_collapsed'] }),
        };
        const verdict = {};
        for (const [name, text] of Object.entries(attempts)) {
            verdict[name] = debug.read_save(text) === null;
        }
        // Odd but harmless numbers are cleaned, not refused.
        const odd = debug.read_save(variant({ care: { hunger: 'starving', food: 1e9 }, bond: { fear: 7 } }));
        verdict.odd_numbers_cleaned = Boolean(odd) && odd.care.hunger === 0.3 && odd.care.food === 9 && odd.bond.fear === 1;
        // A save trying to add a property to every object in the page ("prototype pollution").
        debug.read_save('{"version":1,"chapter":"other_side","events":["level_started"],"human":[560,300],"dog":[660,300],"controlled":"human","__proto__":{"polluted":true}}');
        verdict.no_pollution = ({}).polluted === undefined;
        verdict.refusals_counted = debug.refused_saves().count;
        return verdict;
    });
    const refused_all = Object.entries(verdicts).filter(function attempt(entry) { return !['odd_numbers_cleaned', 'no_pollution', 'refusals_counted'].includes(entry[0]); });
    for (const [name, refused] of refused_all) {
        check('break it: a save with ' + name + ' is refused', refused);
    }
    check('break it: odd numbers in a save are cleaned, not trusted', verdicts.odd_numbers_cleaned);
    check('break it: a save cannot add properties to every object', verdicts.no_pollution);
    check('break it: every refusal is counted', verdicts.refusals_counted >= refused_all.length, String(verdicts.refusals_counted));

    // Break it: a save that passes the checks but puts her inside a wall starts the chapter fresh.
    await page.evaluate(() => {
        localStorage.setItem('until_we_find_home_save', JSON.stringify({ version: 1, chapter: 'other_side', events: ['level_started'], human: [300, 450], dog: [660, 300], controlled: 'human' }));
        const debug = window.until_we_find_home_debug;
        debug.continue_save();
        debug.skip_card();
    });
    await page.waitForTimeout(300);
    const walled = await debug_state();
    check('break it: a save putting her inside a wall starts the chapter fresh instead', walled.chapter === 'other_side' && walled.human_x < 700 && walled.happened.length >= 1,
        'her x ' + walled.human_x);
    await page.evaluate(() => { localStorage.clear(); window.until_we_find_home_debug.set_speed(1); });
}

// Film scenes: the controls do nothing while one plays, Skip lands in the same state as the
// end of the film, and no save is made mid-film.
async function check_films(page) {
    await start_chapter(page, 0);
    const during = await page.evaluate(() => {
        const debug = window.until_we_find_home_debug;
        const start_x = debug.state().human_x;
        debug.hold('left', true);
        for (const control of ['call', 'act', 'jump', 'switch', 'low']) {
            debug.press(control);
        }
        debug.step(30);
        debug.hold('left', false);
        const state = debug.state();
        return { cutscene: state.cutscene, called: state.happened.includes('human_called'), moved_left: state.human_x < start_x - 2, saved: debug.save_now() };
    });
    check('a film plays with the controls set aside', during.cutscene);
    check('break it: buttons pressed during a film do nothing', !during.called && !during.moved_left, JSON.stringify(during));
    check('break it: no save is made in the middle of a film', during.saved === false);
    const skipped = await page.evaluate(() => {
        const debug = window.until_we_find_home_debug;
        debug.skip_cutscene();
        debug.skip_cutscene();  // Twice: the second must do nothing.
        debug.step(60 * 20);    // Twenty seconds: no leftover film timer may fire.
        return debug.state();
    });
    check('Skip ends the film where the film would have ended', !skipped.cutscene && skipped.happened.includes('arrival_done') && skipped.caption === '',
        JSON.stringify({ cutscene: skipped.cutscene, caption: skipped.caption }));
    check('break it: after Skip, no leftover step of the film plays', !skipped.happened.includes('intro_3') && !skipped.happened.includes('intro_5'),
        skipped.happened.join(', '));
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
    await check_saving(page);
    await check_films(page);
    check('the page logged no errors', page_errors.length === 0, page_errors.join(' | '));
    await browser.close();
    const failed = results.filter(function did_fail(result) { return !result.passed; });
    console.log(failed.length === 0 ? 'SYSTEMS CHECK PASSED' : 'SYSTEMS CHECK FAILED: ' + failed.length + ' of ' + results.length + ' checks');
    process.exit(failed.length === 0 ? 0 : 1);
}

main();
