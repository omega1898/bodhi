// Purpose: films short moves (start walking, turn around, jump and land, the dog following)
// and stitches the frames into one contact sheet, so a person can judge how natural the
// motion looks, frame by frame. It also checks two smoothness numbers and fails if the
// motion has gone stiff again: she must go through many different drawings, a turn must show
// the turning frames, and a jump must show both rising and falling drawings.
//
// Run from the repository root:  node prototype/web/tests/filmstrip.js output_folder
'use strict';

const path = require('path');
const fs = require('fs');
const { execFileSync } = require('child_process');
const { chromium } = require('playwright');

const output_folder = path.resolve(process.argv[2] || path.join(__dirname, 'screenshots'));
const frame_gap_ms = 66;  // About 15 frames a second of game time.

// Reads where the camera and a character are, so each frame can be cropped around them.
async function crop_around(page, kind) {
    return page.evaluate((character_kind) => {
        const debug = window.until_we_find_home_debug;
        const human = debug.character_state(character_kind);
        const camera = debug.camera();
        const canvas = document.getElementById('world_canvas');
        const css_scale = canvas.getBoundingClientRect().height / (canvas.height / (window.devicePixelRatio || 1)) ;
        const view_height = canvas.height / Math.min(canvas.height / 540, canvas.width / 640);
        const pixels_per_unit = (canvas.getBoundingClientRect().height / view_height) * camera.zoom;
        const screen_x = (human.x + 13 - camera.center_x) * pixels_per_unit + canvas.getBoundingClientRect().width / 2;
        const screen_y = (human.y + 43 - camera.center_y) * pixels_per_unit + canvas.getBoundingClientRect().height / 2;
        return { x: screen_x, y: screen_y, scale: pixels_per_unit, css_scale: css_scale };
    }, kind);
}

// Plays a list of [control, pressed, wait_ms] steps while saving cropped frames.
async function film(page, name, steps, total_ms, kind) {
    const frames = [];
    let step_index = 0;
    let elapsed = 0;
    let next_step_at = 0;
    while (elapsed <= total_ms) {
        while (step_index < steps.length && elapsed >= next_step_at) {
            const [control, pressed, wait_ms] = steps[step_index];
            await page.evaluate(([c, p]) => window.until_we_find_home_debug.hold(c, p), [control, pressed]);
            next_step_at = elapsed + wait_ms;
            step_index += 1;
        }
        const center = await crop_around(page, kind || 'human');
        const width = Math.round(center.scale * 210);
        const height = Math.round(center.scale * 130);
        const file = path.join(output_folder, name + '_' + String(frames.length).padStart(2, '0') + '.png');
        await page.screenshot({ path: file, clip: { x: Math.max(0, center.x - width / 2), y: Math.max(0, center.y - height * 0.6), width: width, height: height } });
        frames.push(file);
        await page.waitForTimeout(frame_gap_ms);
        elapsed += frame_gap_ms;
    }
    // Stitch the frames into a grid with Python's image library (installed in cloud sessions).
    const sheet = path.join(output_folder, name + '_sheet.png');
    execFileSync('python3', ['-I', '-c', [
        'import sys',
        'from PIL import Image',
        'paths = sys.argv[2:]',
        'images = [Image.open(p) for p in paths]',
        'w, h = images[0].size',
        'columns = 6',
        'rows = (len(images) + columns - 1) // columns',
        'sheet = Image.new("RGB", (w * columns, h * rows), (0, 0, 0))',
        'for index, image in enumerate(images): sheet.paste(image, ((index % columns) * w, (index // columns) * h))',
        'sheet.save(sys.argv[1])',
    ].join('\n'), sheet, ...frames]);
    for (const file of frames) {
        fs.unlinkSync(file);
    }
    return sheet;
}

async function main() {
    fs.mkdirSync(output_folder, { recursive: true });
    const browser = await chromium.launch();
    const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
    await page.goto('file://' + path.resolve(__dirname, '..', 'index.html'));
    await page.waitForTimeout(500);
    await page.evaluate(() => { window.until_we_find_home_debug.start_level(5); window.until_we_find_home_debug.skip_card(); });
    await page.waitForTimeout(1500);

    // Measure smoothness while she walks right, turns, walks left, jumps.
    const samples = [];
    const sampler = setInterval(async () => {
        try {
            samples.push(await page.evaluate(() => window.until_we_find_home_debug.character_state('human')));
        } catch (error) { /* page closing */ }
    }, 16);
    const sheet = await film(page, 'walk_turn_jump', [
        ['right', true, 900], ['right', false, 120], ['left', true, 700], ['jump', true, 200], ['jump', false, 600], ['left', false, 400],
    ], 2900);
    clearInterval(sampler);
    const frames_atlas_jump_count = await page.evaluate(() => frames_atlas.her.animations.jump.frames);

    // What was actually drawn: each sample says which animation and frame was on screen.
    const shown = samples.map((sample) => sample.shown || '');
    const different_drawings = new Set(shown.filter((name) => name)).size;
    const turn_drawn = shown.some((name) => name.startsWith('turn'));
    // The jump is one arc of drawings with the top in the middle: lower numbers rise, higher fall.
    const jump_drawings = frames_atlas_jump_count;
    const rise_drawn = shown.some((name) => name.startsWith('jump ') && Number(name.split(' ')[1]) < (jump_drawings - 1) / 2);
    const fall_drawn = shown.some((name) => name.startsWith('jump ') && Number(name.split(' ')[1]) > (jump_drawings - 1) / 2);
    console.log('different drawings shown (at least 10 = alive): ' + different_drawings);
    console.log('turn drawn: ' + turn_drawn + ' | jump rising drawn: ' + rise_drawn + ' | falling drawn: ' + fall_drawn);
    console.log('contact sheet: ' + sheet);

    // The dog, close up: a sprint, a turn, a jump, a stop.
    await page.evaluate(() => window.until_we_find_home_debug.take_control('dog'));
    const dog_sheet = await film(page, 'dog_run_turn_jump', [
        ['right', true, 1100], ['right', false, 100], ['left', true, 600], ['jump', true, 250], ['jump', false, 500], ['left', false, 600],
    ], 2600, 'dog');
    console.log('dog contact sheet: ' + dog_sheet);
    await browser.close();
    const smooth_enough = different_drawings >= 10 && turn_drawn && rise_drawn && fall_drawn;
    console.log(smooth_enough ? 'MOTION CHECK PASSED' : 'MOTION CHECK FAILED: too few drawings, or the turn or jump frames never showed');
    process.exit(smooth_enough ? 0 : 1);
}

main();
