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
    [['shot', 'prologue_start'], ['walk', 760], ['act'], ['act'], ['wait', 4], ['walk', 330], ['act'], ['wait_for', 'photo_taken'],
        ['walk', 150], ['act'], ['shot', 'prologue_flat'], ['walk', 2500], ['wait_for', 'set_out'], ['shot', 'prologue_street']],
    [['shot', 'station_start'], ['walk', 1200], ['wait_for', 'walkway_collapsed'], ['wait', 0.6], ['shot', 'station_collapse'],
        ['wait_for', 'separation_scene_finished'], ['walk', 220], ['act'], ['wait_for', 'power_restored'], ['shot', 'station_dog'],
        ['walk', 1960], ['low'], ['wait', 1.2], ['shot', 'station_sniff'], ['act'],
        ['walk', 2700], ['act'], ['wait_for', 'glove_found'], ['walk', 3010], ['act'], ['wait_for', 'dog_barked'],
        ['walk', 3240], ['wait_for', 'pair_met_at_fence'], ['walk', 3222], ['act'], ['wait_for', 'fence_touched'], ['shot', 'station_fence'],
        ['walk', 3460], ['act'], ['switch'], ['walk', 3130], ['act'], ['wait_for', 'debris_cleared'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait', 2.5], ['shot', 'station_reunion'], ['wait_for', 'reunion_complete'], ['walk', 3800]],
    [['shot', 'hospital_start'], ['walk', 900], ['act'], ['walk', 1400], ['wait_for', 'doors_closed'], ['wait_for', 'separation_scene_finished'],
        ['walk', 1900], ['act'], ['wait_for', 'basement_unlocked'], ['walk', 1650], ['shot', 'hospital_vent'], ['walk', 2500], ['act'], ['wait_for', 'lift_called'],
        ['walk', 2840], ['wait_for', 'pair_met_at_lift'], ['walk', 2776], ['act'], ['wait_for', 'glass_touched'], ['shot', 'hospital_glass'],
        ['walk', 3010], ['act'], ['switch'], ['walk', 2650], ['act'], ['wait_for', 'cord_pulled'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait_for', 'reunion_complete'], ['walk', 3650]],
    [['shot', 'school_start'], ['walk', 320], ['act'], ['act'], ['wait_for', 'ball_fetched'], ['wait_for', 'dog_bolted'], ['wait', 1], ['shot', 'school_bolt'],
        ['walk', 1450], ['wait_for', 'saw_dog_from_roof'], ['walk', 1520], ['act'], ['wait_for', 'crate_pushed'], ['shot', 'school_roof'],
        ['walk', 2300], ['act'], ['wait_for', 'rope_pulled'], ['walk', 2700], ['wait_for', 'pair_met_at_window'], ['walk', 2652], ['act'],
        ['wait_for', 'window_touched'], ['walk', 2860], ['act'], ['switch'], ['walk', 2560], ['act'], ['wait_for', 'desk_pushed'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait_for', 'reunion_complete']],
    [['shot', 'river_start'], ['walk', 420], ['act'], ['walk', 1400], ['wait_for', 'footbridge_broke'], ['wait', 0.8], ['shot', 'river_flood'],
        ['wait_for', 'dog_surfaced'], ['walk', 300], ['act'], ['wait_for', 'floodgate_turned'], ['wait', 2], ['walk', 2620], ['act'], ['wait_for', 'pin_pulled'],
        ['walk', 3050], ['wait_for', 'pair_met_at_fence'], ['walk', 3016], ['act'], ['wait_for', 'fence_touched'], ['walk', 3150], ['act'],
        ['shot', 'river_fence'], ['walk', 3320], ['act'], ['switch'], ['walk', 2945], ['act'], ['wait_for', 'post_dug'], ['wait', 1.5],
        ['wait_prompt', 'Kneel'], ['low'], ['wait_prompt', 'Hold'], ['act'], ['wait', 2.5], ['shot', 'river_reunion'], ['wait_for', 'reunion_complete']],
    [['shot', 'home_start'], ['walk', 1460], ['wait_for', 'saw_house'], ['walk', 2030], ['act'], ['walk', 2148], ['act'], ['wait', 2], ['shot', 'home_step'],
        ['wait_for', 'dog_waits'], ['walk', 3300], ['wait_for', 'followed'], ['shot', 'home_end']],
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

// Plays one chapter's steps. Returns the frame timings measured during it.
async function play_chapter(page, chapter_index, steps) {
    await page.evaluate((index) => window.until_we_find_home_debug.start_level(index), chapter_index);
    await page.evaluate(() => window.until_we_find_home_debug.skip_card());
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
    await wait_until(page, (state) => state.complete, 40, 'chapter ' + chapter_index + ' to finish');
    return page.evaluate(() => window.until_we_find_home_debug.frame_costs());
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
    for (let chapter_index = 0; chapter_index < playthroughs.length; chapter_index++) {
        try {
            const costs = await play_chapter(page, chapter_index, playthroughs[chapter_index]);
            costs.sort((a, b) => a - b);
            const average = costs.reduce((total, cost) => total + cost, 0) / Math.max(1, costs.length);
            const slowest_tenth = costs[Math.floor(costs.length * 0.9)] || 0;
            console.log('chapter ' + chapter_index + ' finished | frame work: average ' + average.toFixed(2) + ' ms, 90th percentile ' + slowest_tenth.toFixed(2) + ' ms (at ' + test_speed + 'x speed)');
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
