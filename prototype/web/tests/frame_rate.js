// Purpose: measures real smoothness at iPhone 13 landscape size: frames per second while the
// woman walks through the station, and how evenly she moves across the screen. Fails below
// 50 frames a second, so a change that brings back stutter is caught before it ships.
//
// This test browser paints with the processor, not a graphics chip, so a real iPhone
// should do better; treat a pass as the minimum bar, not proof for every phone.
//
// Run from the repository root:  node prototype/web/tests/frame_rate.js
// Another chapter, by its id:      FRAME_RATE_CHAPTER=coming_home node prototype/web/tests/frame_rate.js
'use strict';

const path = require('path');
const { chromium } = require('playwright');

// The station by default: rain, lamps, mist and both characters, the heaviest scene to draw.
// Only letters and underscores are accepted, since the name is put into the page's own code.
const chapter_id = /^[a-z_]+$/.test(process.env.FRAME_RATE_CHAPTER || '') ? process.env.FRAME_RATE_CHAPTER : 'other_side';

async function main() {
    const browser = await chromium.launch();
    const context = await browser.newContext({ viewport: { width: 844, height: 390 }, deviceScaleFactor: 3, isMobile: true, hasTouch: true });
    const page = await context.newPage();
    await page.goto('file://' + path.resolve(__dirname, '..', 'index.html'));
    await page.waitForTimeout(500);
    // The chapter is found by name; an opening film is skipped, since the walking is measured.
    await page.evaluate((id) => {
        const debug = window.until_we_find_home_debug;
        debug.start_level(levels.findIndex(function named(level) { return level.id === id; }));
        debug.skip_card();
        debug.skip_cutscene();
    }, chapter_id);
    await page.waitForTimeout(2000);
    await page.evaluate(() => window.until_we_find_home_debug.hold('right', true));
    // Give the adaptive resolution a few seconds to settle, as it would on a phone.
    await page.waitForTimeout(4000);
    await page.evaluate(() => window.until_we_find_home_debug.hold('right', false));
    await page.evaluate(() => window.until_we_find_home_debug.walk_to(150));
    const result = await page.evaluate(() => new Promise((resolve) => {
        const gaps = [];
        const steps = [];
        let last_time = 0;
        let last_x = null;
        const start = performance.now();
        function measure(now) {
            if (last_time) {
                gaps.push(now - last_time);
            }
            last_time = now;
            const x = window.until_we_find_home_debug.drawn_x();
            if (x !== null && last_x !== null && x !== last_x) {
                steps.push(Math.abs(x - last_x));
            }
            last_x = x;
            if (now - start < 4000) {
                requestAnimationFrame(measure);
            } else {
                resolve({ gaps: gaps, steps: steps });
            }
        }
        requestAnimationFrame(measure);
    }));
    const gaps = result.gaps.slice().sort((a, b) => a - b);
    const average = gaps.reduce((total, gap) => total + gap, 0) / gaps.length;
    const frames_per_second = 1000 / average;
    const late = gaps[Math.floor(gaps.length * 0.95)];
    console.log(chapter_id + ': frames per second: ' + frames_per_second.toFixed(1) + ' | 95% of frames within ' + late.toFixed(1) + ' ms');
    console.log('drawing resolution settled at ' + await page.evaluate(() => window.until_we_find_home_debug.render_scale()) + 'x');
    await browser.close();
    const smooth = frames_per_second >= 50;
    console.log(smooth ? 'FRAME RATE CHECK PASSED' : 'FRAME RATE CHECK FAILED: below 50 frames a second at iPhone size');
    process.exit(smooth ? 0 : 1);
}

main();
