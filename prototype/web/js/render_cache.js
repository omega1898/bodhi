// Purpose: keeps the game smooth on phones by painting expensive things once and reusing
// them. Soft glows, light holes, mist and blurred foreground shapes become small pictures
// ("sprites") that are stamped onto the screen; gradients are built once and kept.
// Rebuilding a gradient or a big blur every frame is what made the first version stutter.
'use strict';

const render_cache = {
    glow_sprites: new Map(),     // One soft glow picture per colour.
    hole_sprite: null,           // A soft black dot, used to cut light into darkness.
    mist_sprites: new Map(),
    blur_sprites: new Map(),
    gradients: new WeakMap(),    // Gradients kept per drawing context.
};

// Makes a small hidden canvas to paint a sprite on.
function make_sprite_canvas(width, height) {
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.ceil(width));
    canvas.height = Math.max(1, Math.ceil(height));
    return canvas;
}

// A soft round glow of one colour, bright in the middle and fading to nothing at the edge.
// `color` is an "r,g,b" string, for example "255,190,110".
function glow_sprite(color) {
    if (!render_cache.glow_sprites.has(color)) {
        const canvas = make_sprite_canvas(128, 128);
        const context = canvas.getContext('2d');
        const gradient = context.createRadialGradient(64, 64, 0, 64, 64, 64);
        gradient.addColorStop(0, 'rgba(' + color + ',1)');
        gradient.addColorStop(0.35, 'rgba(' + color + ',0.45)');
        gradient.addColorStop(1, 'rgba(' + color + ',0)');
        context.fillStyle = gradient;
        context.fillRect(0, 0, 128, 128);
        render_cache.glow_sprites.set(color, canvas);
    }
    return render_cache.glow_sprites.get(color);
}

// Stamps a glow centred on (x, y) with the given radius and strength (0 to 1).
function stamp_glow(context, color, x, y, radius, strength) {
    if (radius <= 0 || strength <= 0) {
        return;
    }
    context.globalAlpha = Math.min(1, strength);
    context.drawImage(glow_sprite(color), x - radius, y - radius, radius * 2, radius * 2);
    context.globalAlpha = 1;
}

// A soft black dot. Stamped with "destination-out" blending, it cuts a soft hole of light
// into the darkness layer.
function hole_sprite() {
    if (!render_cache.hole_sprite) {
        const canvas = make_sprite_canvas(96, 96);
        const context = canvas.getContext('2d');
        const gradient = context.createRadialGradient(48, 48, 0, 48, 48, 48);
        gradient.addColorStop(0, 'rgba(0,0,0,1)');
        gradient.addColorStop(0.4, 'rgba(0,0,0,0.7)');
        gradient.addColorStop(1, 'rgba(0,0,0,0)');
        context.fillStyle = gradient;
        context.fillRect(0, 0, 96, 96);
        render_cache.hole_sprite = canvas;
    }
    return render_cache.hole_sprite;
}

// A wide, flat, soft cloud of mist in one colour.
function mist_sprite(color) {
    if (!render_cache.mist_sprites.has(color)) {
        const canvas = make_sprite_canvas(256, 64);
        const context = canvas.getContext('2d');
        context.scale(1, 0.25);
        const gradient = context.createRadialGradient(128, 128, 0, 128, 128, 128);
        gradient.addColorStop(0, color);
        gradient.addColorStop(1, 'rgba(0,0,0,0)');
        context.fillStyle = gradient;
        context.fillRect(0, 0, 256, 256);
        render_cache.mist_sprites.set(color, canvas);
    }
    return render_cache.mist_sprites.get(color);
}

// Builds a gradient once and keeps it. Gradients are positioned in the coordinates they're
// drawn in, so a gradient for a wall that never moves can be reused every frame.
// `owner` is the object it belongs to (a wall, a lamp); `key` names which gradient.
function cached_gradient(context, owner, key, make) {
    let per_context = render_cache.gradients.get(context);
    if (!per_context) {
        per_context = new WeakMap();
        render_cache.gradients.set(context, per_context);
    }
    let per_owner = per_context.get(owner);
    if (!per_owner) {
        per_owner = new Map();
        per_context.set(owner, per_owner);
    }
    if (!per_owner.has(key)) {
        per_owner.set(key, make());
    }
    return per_owner.get(key);
}

// A shared owner for gradients that belong to no particular object (fur shading, shadows).
const shared_gradient_owner = {};

// A dark, out-of-focus shape very close to the camera (a pillar, reeds, grass). Painted small
// and blurred once, then stretched when drawn, which gives the soft look of a camera lens
// focused on the characters behind it.
function blur_sprite(kind, color) {
    const key = kind + '|' + color;
    if (!render_cache.blur_sprites.has(key)) {
        const width = kind === 'pillars' ? 40 : kind === 'beams' ? 30 : 110;
        const height = 200;
        const sharp = make_sprite_canvas(width, height);
        const sharp_context = sharp.getContext('2d');
        sharp_context.fillStyle = color;
        if (kind === 'pillars' || kind === 'beams') {
            sharp_context.fillRect(width * 0.2, 0, width * 0.6, height);
            sharp_context.fillRect(0, height - 14, width, 14);
            sharp_context.fillRect(0, 0, width, 8);
        } else {
            const random = make_seeded_random(kind.length * 31);
            sharp_context.beginPath();
            for (let blade = 0; blade < 18; blade++) {
                const blade_x = 6 + blade * 5.6;
                const blade_height = 70 + random() * 90;
                const bend = (random() - 0.5) * 26;
                sharp_context.moveTo(blade_x - 2.5, height);
                sharp_context.quadraticCurveTo(blade_x + bend * 0.4, height - blade_height * 0.6, blade_x + bend, height - blade_height);
                sharp_context.lineTo(blade_x + 2.5, height);
            }
            sharp_context.fill();
        }
        // Blur by shrinking and stretching back in several smoothed steps.
        const small = make_sprite_canvas(width / 4, height / 4);
        const small_context = small.getContext('2d');
        small_context.imageSmoothingEnabled = true;
        small_context.drawImage(sharp, 0, 0, small.width, small.height);
        const soft = make_sprite_canvas(width, height);
        const soft_context = soft.getContext('2d');
        soft_context.imageSmoothingEnabled = true;
        for (const [offset_x, offset_y] of [[0, 0], [1.5, 0], [-1.5, 0], [0, 1.5], [0, -1.5]]) {
            soft_context.globalAlpha = 0.22;
            soft_context.drawImage(small, offset_x, offset_y, width, height);
        }
        soft_context.globalAlpha = 1;
        soft_context.drawImage(small, 0, 0, width, height);
        render_cache.blur_sprites.set(key, soft);
    }
    return render_cache.blur_sprites.get(key);
}
