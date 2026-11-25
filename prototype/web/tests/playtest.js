// Purpose: plays every chapter of the browser game from start to finish in a real (headless)
// browser, the way a player would: walking, jumping, using things, switching characters.
// It fails if any chapter can't be finished, if the page logs an error, or if frames take too
// long to compute. It also saves screenshots so a person can judge how the game looks.
//
// Run from the repository root:
//   node prototype/web/tests/playtest.js [folder_for_screenshots]
'use strict';

const path = require('path');
const fs = require('fs');
const { chromium } = require('playwright');

// PLAYTEST_PAGE lets the same robot play the copy bundled into the iOS app:
//   PLAYTEST_PAGE=ios/UntilWeFindHome/Game/index.html PLAYTEST_OFFLINE=1 node prototype/web/tests/playtest.js
const page_path = 'file://' + (process.env.PLAYTEST_PAGE ? path.resolve(process.env.PLAYTEST_PAGE) : path.resolve(__dirname, '..', 'index.html'));
const screenshot_folder = path.resolve(process.argv[2] || path.join(__dirname, 'screenshots'));
const test_speed = 3;              // The game runs three times faster during tests.
const slowest_allowed_frame_ms = 16;  // Average work per frame must leave room for 60 frames a second.

// Each chapter's playthrough, as steps:
//   ['walk', x]   walk the controlled character to x (jumping when blocked)
//   ['act'] ['low'] ['switch'] ['call']   press that button
//   ['wait_for', 'event_name']   wait until that story event has happened
//   ['wait', seconds]   wait (in game seconds)
//   ['wait_prompt', 'text']   wait until the on-screen prompt contains this text
//   ['shot', 'name']   save a screenshot
const playthroughs = [
    [['wait', 1.5], ['shot', 'coming_home_film'], ['wait_for', 'arrival_done'], ['shot', 'coming_home_garden'],
        ['wait_prompt', 'Towel'], ['act'], ['wait_for', 'ball_fetched'],
        ['call'], ['walk', 848], ['wait_prompt', 'front door'], ['act'], ['wait_for', 'front_door_opened'],
        ['walk', 1560], ['wait_prompt', 'Mark'], ['act'], ['walk', 1900], ['wait_prompt', 'bag'], ['act'],
        ['walk', 2120], ['wait_prompt', 'bowl'], ['act'], ['wait_for', 'first_meal'], ['shot', 'coming_home_meal'],
        ['walk', 2620], ['wait_prompt', 'bath'], ['act'], ['walk', 3160], ['wait_prompt', 'photograph'], ['act'],
        ['walk', 3450], ['wait_prompt', 'basket'], ['act'], ['walk', 3870], ['wait_prompt', 'Go to bed'], ['act'],
        ['wait_for', 'puppy_cried'], ['walk', 3990], ['wait_for', 'on_the_bed'], ['shot', 'coming_home_bed']],
    [['wait', 1.5], ['shot', 'storm_film'], ['wait_for', 'storm_done'], ['shot', 'storm_separated'],
        ['walk', 250], ['wait_prompt', 'sheets'], ['act'], ['wait_for', 'sheets_taken'], ['call'], ['wait_for', 'dog_climbing'],
        ['walk', 1640], ['wait_for', 'pair_met_at_landing'], ['shot', 'storm_shelf'], ['walk', 1385], ['wait_prompt', 'Lower'], ['act'], ['wait_for', 'sheets_lowered'],
        ['switch'], ['wait_prompt', 'Grab'], ['act'], ['wait_for', 'sheet_grabbed'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait_for', 'reunion_complete'], ['walk', 90], ['wait_for', 'reached_window']],
    [['shot', 'prologue_start'], ['walk', 760], ['act'], ['act'], ['wait', 4], ['walk', 330], ['act'], ['wait_for', 'photo_taken'],
        ['walk', 150], ['act'], ['shot', 'prologue_flat'], ['walk', 2500], ['wait_for', 'set_out'], ['shot', 'prologue_street']],
    [['shot', 'station_start'], ['walk', 1200], ['wait_for', 'walkway_collapsed'], ['wait', 0.6], ['shot', 'station_collapse'],
        ['wait_for', 'separation_scene_finished'], ['walk', 220], ['act'], ['wait_for', 'power_restored'], ['shot', 'station_dog'],
        ['walk', 1960], ['low'], ['wait', 1.2], ['shot', 'station_sniff'], ['act'],
        ['walk', 2700], ['act'], ['wait_for', 'glove_found'], ['walk', 3010], ['act'], ['wait_for', 'dog_barked'],
        ['walk', 3240], ['wait_for', 'pair_met_at_fence'], ['walk', 3222], ['act'], ['wait_for', 'fence_touched'], ['shot', 'station_fence'],
        ['walk', 3460], ['act'], ['switch'], ['walk', 3130], ['act'], ['wait_for', 'debris_cleared'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait', 2.5], ['shot', 'station_reunion'], ['wait_for', 'reunion_complete'], ['walk', 3800]],
    [['shot', 'motorway_start'], ['walk', 1430], ['wait_for', 'deck_cracked'], ['wait_for', 'separation_scene_finished'],
        ['walk', 1040], ['wait_prompt', 'ladder off'], ['act'], ['walk', 1270], ['wait_prompt', 'Lay'], ['act'], ['wait_for', 'ladder_laid'],
        ['walk', 2300], ['wait_prompt', 'bag'], ['act'], ['walk', 2660], ['wait_for', 'biscuit_scent'], ['shot', 'motorway_scent'],
        ['walk', 2960], ['wait_prompt', 'Bark'], ['act'], ['wait_for', 'dog_barked'], ['walk', 3300], ['wait_for', 'pair_met_at_gate'],
        ['walk', 3040], ['wait_prompt', 'Reach'], ['act'], ['wait_for', 'barrier_touched'], ['walk', 3150], ['wait_prompt', 'crank'], ['act'],
        ['switch'], ['wait_prompt', 'Dig'], ['act'], ['wait_for', 'gravel_dug'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait_for', 'reunion_complete'], ['walk', 3950]],
    [['shot', 'hospital_start'], ['walk', 900], ['act'], ['walk', 1400], ['wait_for', 'doors_closed'], ['wait_for', 'separation_scene_finished'],
        ['walk', 1900], ['act'], ['wait_for', 'basement_unlocked'], ['walk', 1650], ['shot', 'hospital_vent'], ['walk', 2500], ['act'], ['wait_for', 'lift_called'],
        ['walk', 2840], ['wait_for', 'pair_met_at_lift'], ['walk', 2776], ['act'], ['wait_for', 'glass_touched'], ['shot', 'hospital_glass'],
        ['walk', 3010], ['act'], ['switch'], ['walk', 2650], ['act'], ['wait_for', 'cord_pulled'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait_for', 'reunion_complete'], ['walk', 3650]],
    [['shot', 'orchard_start'], ['walk', 1300], ['wait_for', 'orchard_gate_found'], ['wait_for', 'separation_scene_finished'],
        ['walk', 560], ['wait_prompt', 'key'], ['act'], ['wait_for', 'dog_turn'], ['walk', 1620], ['wait_prompt', 'apple'], ['act'],
        ['walk', 3000], ['wait_for', 'biscuit_bed'], ['shot', 'orchard_hayloft'], ['walk', 3050], ['wait_prompt', 'Bark'], ['act'], ['wait_for', 'dog_barked'],
        ['walk', 1380], ['wait_for', 'pair_met_at_gate'], ['wait_prompt', 'Turn the key'], ['act'], ['wait_for', 'key_turned'],
        ['switch'], ['wait_prompt', 'bolt'], ['act'], ['wait_for', 'bolt_pulled'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait_for', 'reunion_complete'], ['walk', 3000], ['wait_for', 'barn_rest']],
    [['shot', 'school_start'], ['walk', 320], ['act'], ['act'], ['wait_for', 'ball_fetched'], ['wait_for', 'dog_bolted'], ['wait', 1], ['shot', 'school_bolt'],
        ['walk', 1450], ['wait_for', 'saw_dog_from_roof'], ['walk', 1520], ['act'], ['wait_for', 'crate_pushed'], ['shot', 'school_roof'],
        ['walk', 2300], ['act'], ['wait_for', 'rope_pulled'], ['walk', 2700], ['wait_for', 'pair_met_at_window'], ['walk', 2652], ['act'],
        ['wait_for', 'window_touched'], ['walk', 2860], ['act'], ['switch'], ['walk', 2560], ['act'], ['wait_for', 'desk_pushed'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait_for', 'reunion_complete']],
    [['shot', 'snow_start'], ['walk', 1020], ['wait_for', 'cornice_broke'], ['wait_for', 'separation_scene_finished'],
        ['walk', 300], ['wait_prompt', 'stove'], ['act'], ['walk', 520], ['wait_prompt', 'hurdle'], ['act'], ['walk', 880], ['wait_prompt', 'Lay'], ['act'],
        ['wait_for', 'hurdle_laid'], ['walk', 2200], ['wait_prompt', 'collar'], ['act'], ['wait_for', 'collar_found'], ['shot', 'snow_collar'],
        ['walk', 2760], ['wait_prompt', 'Bark'], ['act'], ['wait_for', 'dog_barked'], ['walk', 3100], ['wait_for', 'pair_met_at_fold'],
        ['walk', 2840], ['wait_prompt', 'Reach'], ['act'], ['wait_for', 'gate_touched'], ['walk', 2950], ['wait_prompt', 'ice'], ['act'],
        ['switch'], ['wait_prompt', 'Pull the gate'], ['act'], ['wait_for', 'gate_pulled'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait_for', 'reunion_complete'], ['walk', 3950]],
    [['shot', 'river_start'], ['walk', 420], ['act'], ['walk', 1400], ['wait_for', 'footbridge_broke'], ['wait', 0.8], ['shot', 'river_flood'],
        ['wait_for', 'dog_surfaced'], ['walk', 300], ['act'], ['wait_for', 'floodgate_turned'], ['wait', 2], ['walk', 2620], ['act'], ['wait_for', 'pin_pulled'],
        ['walk', 3050], ['wait_for', 'pair_met_at_fence'], ['walk', 3016], ['act'], ['wait_for', 'fence_touched'], ['walk', 3150], ['act'],
        ['shot', 'river_fence'], ['walk', 3320], ['act'], ['switch'], ['walk', 2945], ['act'], ['wait_for', 'post_dug'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait', 2.5], ['shot', 'river_reunion'], ['wait_for', 'reunion_complete']],
    [['wait', 1.5], ['shot', 'farm_film'], ['wait_for', 'arrival_done'], ['walk', 980], ['wait_prompt', 'stew'], ['act'], ['wait_for', 'scent_on_wind'],
        ['walk', 3000], ['wait_for', 'found_biscuit'], ['shot', 'farm_biscuit_found'], ['wait_prompt', 'Bark'], ['act'], ['wait_for', 'barked_for_help'],
        ['walk', 1460], ['wait_prompt', 'gate'], ['act'], ['wait_for', 'gate_opened'], ['walk', 3010], ['wait_for', 'pair_at_byre'],
        ['wait_prompt', 'Lever'], ['act'], ['switch'], ['walk', 3030], ['wait_prompt', 'Dig'], ['act'], ['wait_for', 'biscuit_free'], ['wait', 1.5], ['shot', 'farm_three'],
        ['walk', 900], ['wait_for', 'back_at_fire']],
    [['shot', 'home_start'], ['walk', 1460], ['wait_for', 'saw_house'], ['walk', 2048], ['wait_prompt', 'green door'], ['act'], ['wait_for', 'home_door_opened'],
        ['walk', 2900], ['wait_prompt', 'Mark'], ['act'], ['walk', 3300], ['wait_prompt', 'bowl'], ['act'], ['walk', 3600], ['wait_prompt', 'dry'], ['act'],
        ['walk', 3870], ['wait_prompt', 'Tuck'], ['act'], ['wait_for', 'tucked_in'], ['shot', 'home_tucked_in'], ['wait_for', 'lay_down']],
];

// Waits until the game's state passes a check, or gives up after a time limit.
async function wait_until(page, check, limit_seconds, description) {
    const started = Date.now();
    while (Date.now() - started < limit_seconds * 1000) {
        const state = await page.evaluate(() => window.until_we_find_home_debug.state());
        if (check(state)) {
            return state;
        }
        await page.waitForTimeout(50);
    }
    const state = await page.evaluate(() => window.until_we_find_home_debug.state());
    throw new Error('Timed out waiting for ' + description + '. State: ' + JSON.stringify(state));
}

// Chapters that set how Bodhi starts (care_start) are played with those needs, since their
// story is about looking after him. Every other chapter starts with him fed and clean, so a
// "Feed" or "Towel" prompt never takes the place of a step the robot is about to press.
const chapters_with_their_own_care = new Set([0, 11]);

// Plays one chapter's steps. Returns the frame timings measured during it.
async function play_chapter(page, chapter_index, steps) {
    await page.evaluate((index) => window.until_we_find_home_debug.start_level(index), chapter_index);
    await page.evaluate(() => window.until_we_find_home_debug.skip_card());
    if (!chapters_with_their_own_care.has(chapter_index)) {
        await page.evaluate(() => window.until_we_find_home_debug.set_care({ hunger: 0, dirt: 0 }));
    }
    await page.waitForTimeout(400);
    await page.evaluate(() => window.until_we_find_home_debug.frame_costs());
    for (const step of steps) {
        const [kind, value] = step;
        if (kind === 'walk') {
            await page.evaluate((x) => window.until_we_find_home_debug.walk_to(x), value);
            await wait_until(page, (state) => !state.walking, 40, 'walking to ' + value);
            await page.waitForTimeout(120);
        } else if (kind === 'wait_for') {
            await wait_until(page, (state) => state.happened.includes(value), 40, 'event ' + value);
        } else if (kind === 'wait_prompt') {
            // Wait until the on-screen prompt offers this action, the way a player would.
            await wait_until(page, (state) => (state.prompt || '').includes(value), 40, 'the prompt "' + value + '"');
        } else if (kind === 'wait') {
            await page.waitForTimeout((value * 1000) / test_speed);
        } else if (kind === 'shot') {
            await page.screenshot({ path: path.join(screenshot_folder, String(chapter_index) + '_' + value + '.png') });
        } else {
            await page.evaluate((control) => window.until_we_find_home_debug.press(control), kind);
            await page.waitForTimeout(150);
        }
    }
    const finished = await wait_until(page, (state) => state.complete, 40, 'chapter ' + chapter_index + ' to finish');
    const costs = await page.evaluate(() => window.until_we_find_home_debug.frame_costs());
    costs.seconds_played = finished.seconds_played;
    return costs;
}

// Runs every chapter, then reports timings and any page errors.
async function main() {
    fs.mkdirSync(screenshot_folder, { recursive: true });
    const browser = await chromium.launch();
    const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
    const page_errors = [];
    if (process.env.PLAYTEST_OFFLINE) {
        // Like the app on a phone in airplane mode: every request to the internet fails.
        await page.route(/^https?:/, (route) => { page_errors.push('tried the internet: ' + route.request().url()); route.abort(); });
    }
    page.on('pageerror', (error) => page_errors.push(error.message));
    page.on('console', (message) => {
        if (message.type() === 'error' && !message.text().includes('fonts.g')) {
            page_errors.push(message.text());
        }
    });
    await page.goto(page_path);
    await page.waitForTimeout(800);
    await page.screenshot({ path: path.join(screenshot_folder, 'title.png') });
    await page.click('#begin_button');
    await page.evaluate((speed) => window.until_we_find_home_debug.set_speed(speed), test_speed);

    let failed = false;
    let robot_seconds = 0;
    for (let chapter_index = 0; chapter_index < playthroughs.length; chapter_index++) {
        try {
            const costs = await play_chapter(page, chapter_index, playthroughs[chapter_index]);
            robot_seconds += costs.seconds_played;
            costs.sort((a, b) => a - b);
            const average = costs.reduce((total, cost) => total + cost, 0) / Math.max(1, costs.length);
            const slowest_tenth = costs[Math.floor(costs.length * 0.9)] || 0;
            console.log('chapter ' + chapter_index + ' finished in ' + costs.seconds_played + ' game seconds | frame work: average ' + average.toFixed(2) + ' ms, 90th percentile ' + slowest_tenth.toFixed(2) + ' ms (at ' + test_speed + 'x speed)');
            if (average > slowest_allowed_frame_ms) {
                console.log('  TOO SLOW: average frame work is over ' + slowest_allowed_frame_ms + ' ms');
                failed = true;
            }
            await page.waitForTimeout(2600 / test_speed + 300);
            await page.screenshot({ path: path.join(screenshot_folder, String(chapter_index) + '_aftermath.png') });
        } catch (error) {
            console.log('chapter ' + chapter_index + ' FAILED: ' + error.message);
            await page.screenshot({ path: path.join(screenshot_folder, String(chapter_index) + '_failure.png') });
            failed = true;
        }
    }
    // The robot knows every answer and never explores, so this is the shortest possible play
    // time, not how long a person takes.
    console.log('shortest play through the whole story: ' + (robot_seconds / 60).toFixed(1) + ' minutes of game time');
    const keepsakes = await page.evaluate(() => Array.from(window.until_we_find_home_debug.state().keepsakes_found || []));
    console.log('keepsakes found during the run: ' + keepsakes.join(', '));
    if (page_errors.length > 0) {
        console.log('PAGE ERRORS:\n  ' + page_errors.join('\n  '));
        failed = true;
    }
    await browser.close();
    console.log(failed ? 'PLAYTEST FAILED' : 'PLAYTEST PASSED: every chapter finished');
    process.exit(failed ? 1 : 0);
}

main();
