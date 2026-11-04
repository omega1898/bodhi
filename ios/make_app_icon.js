// Purpose: paints the app icon (1024 x 1024, as Apple requires) from the game's own painted
// characters: the reunion, her kneeling, the dog facing her, in lamplight on a rainy night.
// Writes ios/UntilWeFindHome/Assets.xcassets/AppIcon.appiconset/app_icon_1024.png
// Run from the repository root:  node ios/make_app_icon.js
'use strict';

const path = require('path');
const { execFileSync } = require('child_process');
const { chromium } = require('playwright');

const icon_path = path.resolve(__dirname, 'UntilWeFindHome', 'Assets.xcassets', 'AppIcon.appiconset', 'app_icon_1024.png');

async function main() {
    const browser = await chromium.launch();
    const page = await browser.newPage({ viewport: { width: 1024, height: 1024 } });
    await page.goto('file://' + path.resolve(__dirname, '..', 'prototype', 'web', 'index.html'));
    await page.waitForFunction(() => typeof puppets !== 'undefined' && puppets.ready, null, { timeout: 15000 });
    await page.evaluate(() => {
        const icon = document.createElement('canvas');
        icon.width = 1024;
        icon.height = 1024;
        icon.style.cssText = 'position:fixed;left:0;top:0;width:1024px;height:1024px;z-index:99';
        document.body.appendChild(icon);
        const context = icon.getContext('2d');
        // Night sky, and warm lamplight pooling above and between them.
        const sky = context.createLinearGradient(0, 0, 0, 1024);
        sky.addColorStop(0, '#0f1724');
        sky.addColorStop(1, '#1e2a3a');
        context.fillStyle = sky;
        context.fillRect(0, 0, 1024, 1024);
        const glow = context.createRadialGradient(512, 360, 20, 512, 420, 620);
        glow.addColorStop(0, 'rgba(255,205,140,0.85)');
        glow.addColorStop(0.35, 'rgba(240,170,100,0.35)');
        glow.addColorStop(1, 'rgba(240,170,100,0)');
        context.fillStyle = glow;
        context.fillRect(0, 0, 1024, 1024);
        // Rain, a few soft streaks.
        context.strokeStyle = 'rgba(210,225,240,0.22)';
        context.lineWidth = 3;
        for (let index = 0; index < 46; index++) {
            const x = (index * 197) % 1024;
            const y = (index * 331) % 900;
            context.beginPath();
            context.moveTo(x, y);
            context.lineTo(x - 10, y + 44);
            context.stroke();
        }
        // The wet ground they're on, catching the light.
        const ground = context.createLinearGradient(0, 820, 0, 1024);
        ground.addColorStop(0, '#2c3646');
        ground.addColorStop(1, '#141b25');
        context.fillStyle = ground;
        context.fillRect(0, 820, 1024, 204);
        context.fillStyle = 'rgba(255,200,130,0.25)';
        context.fillRect(260, 820, 504, 6);
        // Her, kneeling, facing right; the dog sitting, facing her, nose to her hand.
        const human = { kind: 'human', x: -13, y: -86, width: 26, height: 86, facing: 1, pose: 'kneel', walk_phase: 0, on_ground: true, velocity_x: 0, velocity_y: 0 };
        const dog = { kind: 'dog', x: -23, y: -38, width: 46, height: 38, facing: -1, pose: 'sit', walk_phase: 0, on_ground: true, wag: 0.6, fear: 0, velocity_x: 0, velocity_y: 0 };
        for (const [figure, x] of [[human, 370], [dog, 660]]) {
            context.save();
            context.translate(x, 826);
            context.scale(7.2, 7.2);
            draw_contact_shadow(context, figure);
            if (figure.kind === 'dog') {
                draw_dog(context, figure, 2.0);
            } else {
                draw_human(context, figure, 2.0);
            }
            context.restore();
        }
    });
    await page.locator('canvas').last().screenshot({ path: icon_path });
    await browser.close();
    // Apple refuses icons with an alpha (transparency) channel, so save it as plain RGB.
    execFileSync('python3', ['-I', '-c', 'import sys\nfrom PIL import Image\nimage = Image.open(sys.argv[1]).convert("RGB").resize((1024, 1024))\nimage.save(sys.argv[1])', icon_path]);
    console.log('wrote ' + icon_path);
}

main();
