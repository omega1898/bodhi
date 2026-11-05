// Purpose: checks the hand-drawn animation frames (art/animations/, frames_atlas.js) in a real
// browser. It fails if a sheet is missing or the wrong size; if any drawing is cut off at the
// edge of its frame, sinks below the floor or is empty; if the game shows the wrong animation
// for what a character is doing; if running changes drawing too rarely to look fluid; if a
// turn at speed doesn't skid or getting up from kneeling pops; or if odd
// input (an unknown pose, missing numbers) makes the drawing code crash.
//
// Run from the repository root:  node prototype/web/tests/frames_check.js
'use strict';

const path = require('path');
const { chromium } = require('playwright');

const edge_band = 3;          // Pixels along each frame edge that must stay empty.
const visible_alpha = 12;     // A pixel more opaque than this (out of 255) counts as drawn.
const floor_allowance = 3.5;  // Game units a paw, sole or tuft of fur may reach below the feet.
const fluid_drawings_per_second = 24;  // Running flat out must change drawing at least this often.

// Checks every sheet: its size, and that each frame is drawn and not cut off at its edges.
async function check_sheets(page) {
    return page.evaluate(({ band, alpha_limit, allowance }) => {
        const problems = [];
        for (const [character_name, character] of Object.entries(frames_atlas)) {
            const cell = character.cell;
            for (const [animation_name, animation] of Object.entries(character.animations)) {
                const image = flipbook.sheets[character_name][animation_name];
                const label = character_name + ' ' + animation_name;
                if (!image) {
                    problems.push(label + ': sheet did not load');
                    continue;
                }
                const width = cell.width * 10;
                const height = cell.height * 10;
                const columns = animation.columns;
                const rows = Math.ceil(animation.frames / columns);
                if (image.naturalWidth !== width * columns || image.naturalHeight !== height * rows) {
                    problems.push(label + ': sheet is ' + image.naturalWidth + 'x' + image.naturalHeight + ', expected ' + width * columns + 'x' + height * rows);
                }
                if (animation.points.length !== animation.frames) {
                    problems.push(label + ': ' + animation.points.length + ' sets of points for ' + animation.frames + ' frames');
                }
                const canvas = document.createElement('canvas');
                canvas.width = width * columns;
                canvas.height = height * rows;
                const context = canvas.getContext('2d');
                context.drawImage(image, 0, 0);
                const pixels = context.getImageData(0, 0, canvas.width, canvas.height).data;
                for (let frame = 0; frame < animation.frames; frame++) {
                    const left = (frame % columns) * width;
                    const top = Math.floor(frame / columns) * height;
                    let drawn = 0;
                    let on_edge = 0;
                    let below_floor = 0;
                    const floor_y = (cell.feet_y + allowance) * 10;
                    for (let y = 0; y < height; y++) {
                        for (let x = 0; x < width; x++) {
                            if (pixels[((top + y) * canvas.width + left + x) * 4 + 3] > alpha_limit) {
                                drawn += 1;
                                below_floor += y > floor_y ? 1 : 0;
                                if (x < band || y < band || x >= width - band || y >= height - band) {
                                    on_edge += 1;
                                }
                            }
                        }
                    }
                    if (drawn < width * height * 0.02) {
                        problems.push(label + ' frame ' + frame + ': almost empty');
                    }
                    if (below_floor > 0) {
                        problems.push(label + ' frame ' + frame + ': sinks through the floor (' + below_floor + ' pixels)');
                    }
                    if (on_edge > 0) {
                        problems.push(label + ' frame ' + frame + ': cut off at the frame edge (' + on_edge + ' pixels)');
                    }
                }
            }
        }
        return problems;
    }, { band: edge_band, alpha_limit: visible_alpha, allowance: floor_allowance });
}

// Waits until a check on what she shows passes, or returns what she showed last.
async function wait_for_shown(page, kind, check, seconds) {
    const started = Date.now();
    let shown = null;
    while (Date.now() - started < seconds * 1000) {
        shown = await page.evaluate((who) => window.until_we_find_home_debug.character_state(who).shown, kind);
        if (shown && check(shown)) {
            return { passed: true, shown: shown };
        }
        await page.waitForTimeout(30);
    }
    return { passed: false, shown: shown };
}

// Plays a little and checks the game picks the right animation: still, moving, stopping, in the air.
async function check_choices(page) {
    const problems = [];
    // Chapters are found by name, so adding a chapter never changes which one a check plays.
    await page.evaluate(() => { const debug = window.until_we_find_home_debug; debug.start_level(levels.findIndex(function named(level) { return level.id === 'last_morning'; })); debug.skip_card(); });
    await page.waitForTimeout(1500);
    const expectations = [
        ['standing still shows idle', null, (shown) => shown.startsWith('idle')],
        ['moving shows walk or run', async () => page.evaluate(() => window.until_we_find_home_debug.hold('right', true)), (shown) => shown.startsWith('walk') || shown.startsWith('run')],
        ['stopping from a run shows the stop', async () => { await page.waitForTimeout(600); await page.evaluate(() => window.until_we_find_home_debug.hold('right', false)); }, (shown) => shown.startsWith('stop')],
        ['jumping shows a jump drawing', async () => { await page.waitForTimeout(600); await page.evaluate(() => window.until_we_find_home_debug.press('jump')); }, (shown) => shown.startsWith('jump')],
    ];
    for (const [description, action, check] of expectations) {
        if (action) {
            await action();
        }
        const result = await wait_for_shown(page, 'human', check, 3);
        console.log((result.passed ? '  ok   ' : '  FAIL ') + description + ' (showed "' + result.shown + '")');
        if (!result.passed) {
            problems.push(description);
        }
    }
    const dog = await wait_for_shown(page, 'dog', (shown) => shown.length > 0, 3);
    console.log((dog.passed ? '  ok   ' : '  FAIL ') + 'the dog is drawn from frames (showed "' + dog.shown + '")');
    if (!dog.passed) {
        problems.push('the dog is not drawn from frames');
    }
    return problems;
}

// Counts how many times a second the drawing on screen changes while a character runs flat out.
// Fewer than about 24 reads as steppy rather than fluid.
async function drawings_per_second_running(page, kind) {
    await page.evaluate(() => { const debug = window.until_we_find_home_debug; debug.start_level(levels.findIndex(function named(level) { return level.id === 'home'; })); debug.skip_card(); });
    await page.waitForTimeout(1200);
    await page.evaluate((who) => window.until_we_find_home_debug.take_control(who), kind);
    await page.evaluate(() => window.until_we_find_home_debug.hold('right', true));
    await page.waitForTimeout(900);
    const result = await page.evaluate((who) => new Promise((resolve) => {
        let changes = 0;
        let last = null;
        const start = performance.now();
        function look(now) {
            const shown = window.until_we_find_home_debug.character_state(who).shown;
            if (last !== null && shown !== last) {
                changes += 1;
            }
            last = shown;
            if (now - start < 1000) {
                requestAnimationFrame(look);
            } else {
                resolve({ rate: changes / ((now - start) / 1000), shown: shown });
            }
        }
        requestAnimationFrame(look);
    }), kind);
    await page.evaluate(() => window.until_we_find_home_debug.hold('right', false));
    return result;
}

// Fluid motion: she and the dog must both change drawing often enough at a run.
async function check_fluidity(page) {
    const problems = [];
    for (const kind of ['human', 'dog']) {
        const result = await drawings_per_second_running(page, kind);
        const passed = result.rate >= fluid_drawings_per_second;
        console.log((passed ? '  ok   ' : '  FAIL ') + kind + ' running: ' + result.rate.toFixed(1) + ' drawings a second (at least ' + fluid_drawings_per_second + '; last showed "' + result.shown + '")');
        if (!passed) {
            problems.push(kind + ' running changes drawing only ' + result.rate.toFixed(1) + ' times a second');
        }
    }
    return problems;
}

// Records every drawing shown (and the way it faces), frame by frame, for `seconds` after
// running `action` inside the page. Short moves like a skid last only a few frames.
async function record_shown(page, kind, seconds, action) {
    return page.evaluate(({ who, duration, action_text }) => new Promise((resolve) => {
        const seen = [];
        const debug = window.until_we_find_home_debug;
        (new Function('debug', action_text))(debug);
        const start = performance.now();
        function look(now) {
            const state = debug.character_state(who);
            seen.push(state.shown + ' facing ' + state.shown_facing);
            if (now - start < duration * 1000) {
                requestAnimationFrame(look);
            } else {
                resolve(seen);
            }
        }
        requestAnimationFrame(look);
    }), { who: kind, duration: seconds, action_text: action });
}

// Turning round at a run must skid (drawn facing the old way, the way it's still sliding), and
// moving off from kneeling must show her getting up rather than popping upright.
async function check_transitions(page) {
    const problems = [];
    await page.evaluate(() => { const debug = window.until_we_find_home_debug; debug.start_level(levels.findIndex(function named(level) { return level.id === 'home'; })); debug.skip_card(); debug.take_control('human'); });
    await page.waitForTimeout(1200);
    await page.evaluate(() => window.until_we_find_home_debug.hold('right', true));
    await page.waitForTimeout(1200);
    const reversing = await record_shown(page, 'human', 0.5, "debug.hold('right', false); debug.hold('left', true);");
    await page.evaluate(() => window.until_we_find_home_debug.hold('left', false));
    const skid_facing_right = reversing.some((entry) => entry.startsWith('skid') && entry.endsWith('facing 1'));
    console.log((skid_facing_right ? '  ok   ' : '  FAIL ') + 'reversing at a run skids, facing the slide (' + reversing.filter((entry) => entry.startsWith('skid')).length + ' skid frames)');
    if (!skid_facing_right) {
        problems.push('no skid facing the slide when reversing at a run: ' + Array.from(new Set(reversing)).slice(0, 8).join(', '));
    }
    await page.waitForTimeout(800);
    await page.evaluate(() => window.until_we_find_home_debug.press('low'));
    await page.waitForTimeout(1500);
    const rising = await record_shown(page, 'human', 0.5, "debug.hold('right', true);");
    await page.evaluate(() => window.until_we_find_home_debug.hold('right', false));
    const got_up = rising.some((entry) => /^kneel [0-3] /.test(entry));
    console.log((got_up ? '  ok   ' : '  FAIL ') + 'moving off from kneeling shows her getting up (' + Array.from(new Set(rising.map((entry) => entry.split(' facing')[0]))).slice(0, 6).join(', ') + ')');
    if (!got_up) {
        problems.push('moving off from kneeling pops upright with no getting-up drawings');
    }
    return problems;
}

// Break it: hands the drawing code characters with an unknown pose, missing or nonsense
// numbers. None of it may throw; an unknown pose must fall back to standing.
async function check_odd_input(page) {
    return page.evaluate(() => {
        const problems = [];
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d');
        const odd_characters = [
            { kind: 'human', pose: 'dance', x: 0, y: 0, width: 26, height: 86 },
            { kind: 'human', pose: 'walk', x: NaN, y: 0, width: 26, height: 86, velocity_x: Infinity },
            { kind: 'dog', pose: 'jump', x: 5, y: 0, width: 46, height: 38, velocity_y: NaN, carrying: 'ball' },
            { kind: 'dog', pose: undefined, x: 1e12, y: -1e12, width: 46, height: 38, facing: 0, wag: -5 },
        ];
        for (const character of odd_characters) {
            try {
                for (let repeat = 0; repeat < 3; repeat++) {
                    draw_flipbook_character(context, character, character.kind === 'dog' ? 'bodhi' : 'her', repeat * 0.5);
                }
            } catch (error) {
                problems.push('pose ' + character.pose + ' threw: ' + error.message);
            }
        }
        if (!odd_characters[0].flipbook_state.shown.startsWith('idle')) {
            problems.push('an unknown pose showed "' + odd_characters[0].flipbook_state.shown + '" instead of idle');
        }
        return problems;
    });
}

async function main() {
    // Local files count as separate websites, which blocks reading pixels back; this test
    // browser is allowed to treat them as one site. The game itself never reads pixels.
    const browser = await chromium.launch({ args: ['--allow-file-access-from-files'] });
    const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
    const page_errors = [];
    page.on('pageerror', (error) => page_errors.push(error.message));
    await page.goto('file://' + path.resolve(__dirname, '..', 'index.html'));
    await page.waitForFunction(() => flipbook.ready, null, { timeout: 15000 }).catch(() => page_errors.push('the frames never finished loading'));
    await page.click('#begin_button');

    const sheet_problems = await check_sheets(page);
    console.log('sheets: ' + (sheet_problems.length === 0 ? 'every frame present, sized right and inside its frame' : sheet_problems.length + ' problems'));
    const choice_problems = await check_choices(page);
    const fluidity_problems = await check_fluidity(page);
    const transition_problems = await check_transitions(page);
    const odd_problems = await check_odd_input(page);
    console.log('odd input: ' + (odd_problems.length === 0 ? 'nothing crashed; an unknown pose falls back to idle' : odd_problems.length + ' problems'));
    await browser.close();

    const problems = sheet_problems.concat(choice_problems, fluidity_problems, transition_problems, odd_problems, page_errors);
    if (problems.length > 0) {
        console.log('PROBLEMS:\n  ' + problems.join('\n  '));
    }
    console.log(problems.length === 0 ? 'FRAMES CHECK PASSED' : 'FRAMES CHECK FAILED');
    process.exit(problems.length === 0 ? 0 : 1);
}

main();
