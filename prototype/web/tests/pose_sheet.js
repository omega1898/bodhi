// Purpose: draws both painted characters large, in every pose, on one sheet, the way an
// animator checks a character's poses before using them. Saves pose_sheet.png.
//
// Run from the repository root:  node prototype/web/tests/pose_sheet.js [output_folder]
'use strict';

const path = require('path');
const { chromium } = require('playwright');

const output_folder = path.resolve(process.argv[2] || path.join(__dirname, 'screenshots'));

async function main() {
    const browser = await chromium.launch();
    const page = await browser.newPage({ viewport: { width: 1600, height: 900 } });
    await page.goto('file://' + path.resolve(__dirname, '..', 'index.html'));
    await page.waitForFunction(() => typeof puppets !== 'undefined' && puppets.ready, null, { timeout: 15000 }).catch(() => {});
    await page.evaluate(() => {
        const sheet = document.createElement('canvas');
        sheet.width = 1600;
        sheet.height = 900;
        sheet.style.cssText = 'position:fixed;inset:0;z-index:99;background:#cfc8bd';
        document.body.appendChild(sheet);
        const context = sheet.getContext('2d');
        context.fillStyle = '#d8d2c8';
        context.fillRect(0, 0, 1600, 900);
        context.translate(0, -40);
        const human_poses = [['stand', 0, 0], ['walk', 0.6, 140], ['walk', 2.2, 140], ['walk', 1.2, 230], ['jump', 0, 0, -400], ['jump', 0, 0, 500], ['kneel', 0, 0], ['sit', 0, 0], ['embrace', 0, 0]];
        const dog_poses = [['stand', 0, 0], ['walk', 0.8, 160], ['walk', 2.4, 160], ['walk', 1.2, 270], ['sit', 0, 0], ['sniff', 0, 0], ['bark', 0, 0], ['jump', 0, 0, -500], ['sleep', 0, 0], ['embrace', 0, 0]];
        const scale = 3.2;
        human_poses.forEach(function draw_human_pose(pose, index) {
            const figure = { kind: 'human', x: -13, y: -86, width: 26, height: 86, facing: 1, pose: pose[0], walk_phase: pose[1], velocity_x: pose[2], velocity_y: pose[3] || 0, on_ground: true };
            context.save();
            context.translate(90 + index * 170, 380);
            context.scale(scale, scale);
            draw_human(context, figure, 1.0);
            context.restore();
            context.fillStyle = '#333';
            context.font = '16px sans-serif';
            context.fillText(pose[0] + (pose[2] ? ' ' + pose[2] : ''), 50 + index * 170, 410);
        });
        dog_poses.forEach(function draw_dog_pose(pose, index) {
            const figure = { kind: 'dog', x: -23, y: -38, width: 46, height: 38, facing: 1, pose: pose[0], walk_phase: pose[1], velocity_x: pose[2], velocity_y: pose[3] || 0, on_ground: true, wag: 0.5, fear: 0 };
            context.save();
            context.translate(110 + (index % 5) * 300, 620 + Math.floor(index / 5) * 200);
            context.scale(scale, scale);
            draw_dog(context, figure, 1.0);
            context.restore();
            context.fillStyle = '#333';
            context.font = '16px sans-serif';
            context.fillText(pose[0] + (pose[2] ? ' ' + pose[2] : ''), 60 + (index % 5) * 300, 650 + Math.floor(index / 5) * 200);
        });
    });
    await page.screenshot({ path: path.join(output_folder, 'pose_sheet.png') });
    console.log('saved ' + path.join(output_folder, 'pose_sheet.png'));
    await browser.close();
}

main();
