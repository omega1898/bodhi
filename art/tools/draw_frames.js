// Purpose: draws every animation frame for both characters, frame by frame, the way 2D games
// animated by hand do: each frame is a whole new drawing of the body, not pieces rotating on
// pins. Limbs and the dog's back are single continuous shapes that bend; the dog's fur filter
// merges legs into body. The faces, coat, satchel and other details come from the painted
// sheets in art/characters/, so the style stays the same.
//
// The poses are keyed like an animator keys them (contact, down, passing, up for a walk; a
// real rotary gallop for the dog), as numbers in the tables below. The output is one SVG file
// per animation in art/animations/ (each frame editable by hand in any illustration app), and
// prototype/web/js/frames_atlas.js, which tells the game where each frame is.
//
// Run from the repository root:  node art/tools/draw_frames.js
'use strict';

const fs = require('fs');
const path = require('path');

const repository_root = path.resolve(__dirname, '..', '..');
const output_folder = path.join(repository_root, 'art', 'animations');
const atlas_path = path.join(repository_root, 'prototype', 'web', 'js', 'frames_atlas.js');
const units_to_pixels = 10;  // Drawing units per game unit, the same as the painted sheets.

// ---------- Small geometry helpers ----------

// Formats a number for SVG, keeping files small.
function number_text(value) {
    return (Math.round(value * 10) / 10).toString();
}

// Converts a point in game units to drawing units.
function to_pixels(point) {
    return [point[0] * units_to_pixels, point[1] * units_to_pixels];
}

function add(first, second) { return [first[0] + second[0], first[1] + second[1]]; }
function subtract(first, second) { return [first[0] - second[0], first[1] - second[1]]; }
function scale(point, amount) { return [point[0] * amount, point[1] * amount]; }
function length_of(point) { return Math.hypot(point[0], point[1]) || 0.0001; }
function normalized(point) { const length = length_of(point); return [point[0] / length, point[1] / length]; }
function middle(first, second) { return [(first[0] + second[0]) / 2, (first[1] + second[1]) / 2]; }
function degrees(radians) { return radians * 180 / Math.PI; }

// Where a knee or elbow goes, from the two ends and the two bone lengths (two-bone joints).
// `bend` is +1 to bend backward, -1 to bend forward.
function solve_joint(start, end, upper_length, lower_length, bend) {
    const delta = subtract(end, start);
    const full_distance = length_of(delta);
    const distance = Math.min(full_distance, upper_length + lower_length - 0.01);
    const along = (upper_length * upper_length - lower_length * lower_length + distance * distance) / (2 * distance);
    const height = Math.sqrt(Math.max(0, upper_length * upper_length - along * along));
    const unit = scale(delta, 1 / full_distance);
    return [start[0] + unit[0] * along - unit[1] * height * bend, start[1] + unit[1] * along + unit[0] * height * bend];
}

// A smooth closed outline through a list of points (Catmull-Rom curves turned into SVG curves).
// This is what makes a body one continuous shape instead of a set of joined pieces.
function smooth_closed_path(points) {
    const count = points.length;
    let text = 'M ' + points[0].map(number_text).join(',');
    for (let index = 0; index < count; index++) {
        const before = points[(index - 1 + count) % count];
        const start = points[index];
        const end = points[(index + 1) % count];
        const after = points[(index + 2) % count];
        const control_one = add(start, scale(subtract(end, before), 1 / 6));
        const control_two = subtract(end, scale(subtract(after, start), 1 / 6));
        text += ' C ' + control_one.map(number_text).join(',') + ' ' + control_two.map(number_text).join(',') + ' ' + end.map(number_text).join(',');
    }
    return text + ' Z';
}

// A limb as one tapered tube through a joint: thick at the top, thinner toward the end, with a
// rounded bend at the joint. No pin, no seam.
function tube_path(points, widths) {
    const left = [];
    const right = [];
    for (let index = 0; index < points.length; index++) {
        const before = points[Math.max(0, index - 1)];
        const after = points[Math.min(points.length - 1, index + 1)];
        const direction = normalized(subtract(after, before));
        const side = [-direction[1], direction[0]];
        left.push(add(points[index], scale(side, widths[index] / 2)));
        right.push(subtract(points[index], scale(side, widths[index] / 2)));
    }
    const outline = left.concat(right.slice().reverse());
    return smooth_closed_path(outline);
}

// ---------- Reusing the painted details ----------

// Pulls one part's drawing out of a painted sheet, with its original attributes (filters),
// ready to be placed with a new transform. Groups can be nested, so count opening and closing tags.
function extract_part(sheet_text, part_name) {
    const start = sheet_text.indexOf('<g id="part_' + part_name + '"');
    if (start === -1) {
        throw new Error('part not found: ' + part_name);
    }
    const tag_end = sheet_text.indexOf('>', start);
    const opening_tag = sheet_text.slice(start, tag_end + 1);
    let depth = 1;
    let cursor = tag_end + 1;
    while (depth > 0) {
        const next_open = sheet_text.indexOf('<g', cursor);
        const next_close = sheet_text.indexOf('</g>', cursor);
        if (next_open !== -1 && next_open < next_close) {
            const close_of_tag = sheet_text.indexOf('>', next_open);
            if (sheet_text[close_of_tag - 1] !== '/') {
                depth += 1;
            }
            cursor = close_of_tag + 1;
        } else {
            depth -= 1;
            cursor = next_close + 4;
        }
    }
    const inner = sheet_text.slice(tag_end + 1, cursor - 4);
    const filter_match = /filter="([^"]+)"/.exec(opening_tag);
    return { inner: inner, filter: filter_match ? filter_match[1] : null };
}

// Places a reused painted part with its joint at `joint` (game units), turned by `angle` radians.
function place_part(part, joint, angle, extra_transform) {
    const pixel = to_pixels(joint);
    const filter = part.filter ? ' filter="' + part.filter + '"' : '';
    return '<g transform="translate(' + number_text(pixel[0]) + ',' + number_text(pixel[1]) + ') rotate(' + number_text(degrees(angle)) + ')' + (extra_transform || '') + '"' + filter + '>' + part.inner + '</g>';
}

// The <defs> block (gradients and filters) of a painted sheet.
function extract_definitions(sheet_text) {
    return sheet_text.slice(sheet_text.indexOf('<defs>') + 6, sheet_text.indexOf('</defs>'));
}

const her_sheet = fs.readFileSync(path.join(repository_root, 'art', 'characters', 'her.svg'), 'utf8');
const dog_sheet = fs.readFileSync(path.join(repository_root, 'art', 'characters', 'bodhi.svg'), 'utf8');
const her_parts = {};
for (const name of ['head', 'ponytail', 'torso', 'scarf_tail', 'bag', 'boot']) {
    her_parts[name] = extract_part(her_sheet, name);
}
const dog_parts = {};
for (const name of ['head', 'ear']) {
    dog_parts[name] = extract_part(dog_sheet, name);
}
// Far-side limbs sit in the character's own shadow: this filter darkens them a little.
const shade_filter = '<filter id="far_side"><feColorMatrix type="matrix" values="0.64 0 0 0 0 0 0.64 0 0 0 0 0 0.7 0 0 0 0 0 1 0"/></filter>';

// ---------- Her ----------

// The bone (from shoulder to hip) angle, so the painted coat hangs along her body.
function torso_angle_of(pose) {
    const delta = subtract(pose.hip, pose.shoulder);
    return Math.atan2(-delta[0], delta[1]);
}

// Her leg bones, in game units. Their sum (40.2) is the hip-to-ankle distance when she stands,
// so a standing leg is nearly straight; longer bones forced every pose into a crouch.
const her_thigh_length = 20.2;
const her_shin_length = 20.0;
// The coat's shoulder line sits this far above the hip on the painted torso.
const her_torso_length = 27.5;

// Draws one frame of her as SVG, from a pose given in game units (feet at 0, 0; facing right).
function draw_her(pose) {
    const torso_angle = torso_angle_of(pose);
    const head_angle = torso_angle * 0.4 + (pose.head_tilt || 0);
    const pieces = [];

    // A leg: one jeans tube from hip through knee to ankle, a knee crease, then the boot.
    const leg = function draw_leg(foot, foot_angle, near) {
        const ankle = add(foot, [0, -3.6]);
        const knee = solve_joint(pose.hip, ankle, her_thigh_length, her_shin_length, -1);
        const points = [pose.hip, knee, ankle].map(to_pixels);
        let text = '<path d="' + tube_path(points, [80, 58, 46]) + '" fill="' + (near ? 'url(#jeans)' : '#2b3550') + '" stroke="#161c2b" stroke-width="3" stroke-linejoin="round"/>';
        const crease = to_pixels(knee);
        text += '<path d="M ' + number_text(crease[0] - 14) + ',' + number_text(crease[1] - 4) + ' Q ' + number_text(crease[0]) + ',' + number_text(crease[1] + 8) + ' ' + number_text(crease[0] + 14) + ',' + number_text(crease[1] - 2) + '" fill="none" stroke="#1a2030" stroke-width="3" opacity="0.5" stroke-linecap="round"/>';
        text += place_part(her_parts.boot, ankle, foot_angle || 0);
        return near ? text : '<g filter="url(#far_side)">' + text + '</g>';
    };
    // An arm: one sleeve tube from shoulder through elbow to wrist, then the hand.
    const arm = function draw_arm(hand, near) {
        const wrist = hand;
        const elbow = solve_joint(pose.shoulder, wrist, 14, 12, 1);
        const points = [pose.shoulder, elbow, wrist].map(to_pixels);
        const sleeve = tube_path(points, [56, 46, 40]);
        let text = '';
        if (near) {
            text += '<path d="' + sleeve + '" fill="#0c2322" opacity="0.4" transform="translate(-9,7)"/>';
        }
        text += '<path d="' + sleeve + '" fill="' + (near ? 'url(#sleeve_lit)' : '#23514f') + '" stroke="#0f2a29" stroke-width="4" stroke-linejoin="round"/>';
        const cuff_direction = normalized(subtract(wrist, elbow));
        const cuff = to_pixels(subtract(wrist, scale(cuff_direction, 1.2)));
        text += '<circle cx="' + number_text(cuff[0]) + '" cy="' + number_text(cuff[1]) + '" r="21" fill="#1f4c4b"/>';
        const hand_point = to_pixels(add(wrist, scale(cuff_direction, 1.6)));
        text += '<ellipse cx="' + number_text(hand_point[0]) + '" cy="' + number_text(hand_point[1]) + '" rx="17" ry="20" fill="url(#skin)"/>';
        return near ? text : '<g filter="url(#far_side)">' + text + '</g>';
    };

    pieces.push(arm(pose.far_hand, false));
    pieces.push(leg(pose.far_foot, pose.far_foot_angle, false));
    pieces.push(place_part(her_parts.bag, add(pose.hip, rotate_point([-6.6, -9.6], torso_angle)), torso_angle * 0.5 + (pose.hem || 0) * 0.4));
    pieces.push(leg(pose.near_foot, pose.near_foot_angle, true));
    // The coat hangs from the shoulders; its hem swings (a skew around the shoulder line).
    const hem_skew = number_text(-degrees(pose.hem || 0));
    pieces.push(place_part(her_parts.torso, pose.hip, torso_angle, ' translate(0,-275) skewX(' + hem_skew + ') translate(0,275)'));
    pieces.push(place_part(her_parts.scarf_tail, add(pose.hip, rotate_point([-3.6, -26.4], torso_angle)), 0.25 + (pose.scarf || 0)));
    pieces.push(place_part(her_parts.ponytail, add(pose.shoulder, rotate_point([-7.8, -14.8], head_angle)), 0.3 + (pose.hair || 0)));
    let head = place_part(her_parts.head, pose.shoulder, head_angle);
    if (pose.eyes === 'closed') {
        // A closed eye: skin over the painted eye, and a soft lash line.
        const lid = '<path d="M 50,-104 C 58,-110 70,-110 78,-104 C 70,-99 58,-99 50,-104 Z" fill="#f2d2bb"/><path d="M 50,-103 C 58,-97 70,-97 79,-103" fill="none" stroke="#4a3326" stroke-width="3" stroke-linecap="round"/>';
        head = head.replace(/<\/g>$/, lid + '</g>');
    }
    pieces.push(head);
    pieces.push(arm(pose.near_hand, true));
    return '<g filter="url(#grain_soft)">' + pieces.join('') + '</g>';
}

// Turns a point in a part's own coordinates by an angle.
function rotate_point(point, angle) {
    const cosine = Math.cos(angle);
    const sine = Math.sin(angle);
    return [point[0] * cosine - point[1] * sine, point[0] * sine + point[1] * cosine];
}

// Builds her pose from a few numbers: hip height, how far forward the shoulders lean, where the
// feet and hands are. Hands are given relative to the shoulder. The shoulder is always worked out
// from the lean, so the head can never drift off the coat's collar.
function her_pose(values) {
    const hip = [values.hip_x || 0, values.hip_y];
    const lean = values.lean || 0;
    const shoulder = add(hip, [Math.sin(lean) * her_torso_length + 1.5, -Math.cos(lean) * her_torso_length]);
    return {
        hip: hip, shoulder: shoulder,
        near_foot: values.near_foot, far_foot: values.far_foot,
        near_foot_angle: values.near_foot_angle || 0, far_foot_angle: values.far_foot_angle || 0,
        near_hand: add(shoulder, values.near_hand), far_hand: add(shoulder, values.far_hand),
        head_tilt: values.head_tilt || 0, hair: values.hair || 0, scarf: values.scarf || 0.3, hem: values.hem || 0,
        eyes: values.eyes || 'open',
    };
}

// Swaps her near and far side, for the second half of a walk or run cycle.
function mirror_her(values) {
    return Object.assign({}, values, {
        near_foot: values.far_foot, far_foot: values.near_foot,
        near_foot_angle: values.far_foot_angle, far_foot_angle: values.near_foot_angle,
        near_hand: values.far_hand, far_hand: values.near_hand,
    });
}

// Her walk: the four classic keys (contact, down, passing, up), then the same with the other leg.
// Each row: hip height, near foot, far foot, foot angles, arm swing, hair and hem lag.
// The planted foot moves back the same 7.5 units every frame, so in the game, where frames
// advance with distance walked, it stays put on the ground instead of sliding.
const her_walk_keys = [
    { hip_y: -40.8, near_foot: [15, 0], far_foot: [-15, -0.6], near_foot_angle: 0.28, far_foot_angle: -0.45, swing: -7, hair: 0.0, hem: 0.03 },
    { hip_y: -40.2, near_foot: [7.5, 0], far_foot: [-12, -3.2], near_foot_angle: 0, far_foot_angle: -0.7, swing: -4, hair: 0.06, hem: 0.05 },
    { hip_y: -43.3, near_foot: [0, 0], far_foot: [2, -5], near_foot_angle: 0, far_foot_angle: -0.2, swing: 0, hair: 0.09, hem: 0.01 },
    { hip_y: -43.8, near_foot: [-7.5, 0], far_foot: [10, -2.2], near_foot_angle: -0.3, far_foot_angle: 0.12, swing: 5, hair: 0.05, hem: -0.03 },
];
// Her run: contact, recoil, push off, flight. Shoulders lean in, elbows bent, heels kick high.
// The planted foot moves back 10 units a frame.
const her_run_keys = [
    { hip_y: -41.0, near_foot: [13, 0], far_foot: [-16, -7], near_foot_angle: 0.2, far_foot_angle: -0.9, swing: -9, hair: 0.05, hem: 0.06 },
    { hip_y: -38.5, near_foot: [3, 0], far_foot: [-10, -12], near_foot_angle: 0, far_foot_angle: -1.1, swing: -6, hair: 0.14, hem: 0.08 },
    { hip_y: -42.0, near_foot: [-7, -0.5], far_foot: [5, -10], near_foot_angle: -0.5, far_foot_angle: -0.4, swing: 2, hair: 0.18, hem: 0.03 },
    { hip_y: -44.0, near_foot: [-14, -5], far_foot: [14, -5], near_foot_angle: -0.9, far_foot_angle: 0.1, swing: 8, hair: 0.1, hem: -0.03 },
];

// How far she travels in one full cycle (two steps): how far the planted foot moves back per
// frame, times the number of frames. The game uses this to keep feet from sliding.
function cycle_length_of(keys) {
    return (keys[0].near_foot[0] - keys[1].near_foot[0]) * keys.length * 2;
}

// Turns a cycle's keys into full poses: the second half mirrors the first with arms swapped.
function her_cycle(keys, lean, hand_drop) {
    const frames = [];
    for (const half of [0, 1]) {
        for (const key of keys) {
            let values = {
                hip_y: key.hip_y, lean: lean, near_foot: key.near_foot, far_foot: key.far_foot,
                near_foot_angle: key.near_foot_angle, far_foot_angle: key.far_foot_angle,
                near_hand: [key.swing + 1, hand_drop], far_hand: [-key.swing + 1, hand_drop],
                hair: key.hair, hem: key.hem, scarf: 0.35 + key.hair,
            };
            if (half === 1) {
                values = mirror_her(values);
            }
            frames.push(her_pose(values));
        }
    }
    return frames;
}

const her_animations = {
    idle: [0, 1, 2, 1].map(function breathe(step) {
        return her_pose({ hip_y: -43.6 - step * 0.2, near_foot: [3, 0], far_foot: [-3, 0], near_hand: [1.5, 25.5 - step * 0.2], far_hand: [-0.5, 25.5 - step * 0.2], hair: step * 0.02, scarf: 0.3 + step * 0.03 });
    }),
    walk: her_cycle(her_walk_keys, 0.04, 25),
    run: her_cycle(her_run_keys, 0.14, 17),
    jump_rise: [
        her_pose({ hip_y: -46, lean: 0.08, near_foot: [4, -2], far_foot: [-6, -1], near_foot_angle: -0.6, far_foot_angle: -0.8, near_hand: [11, 6], far_hand: [-5, 9], hair: -0.1 }),
        her_pose({ hip_y: -45, lean: 0.05, near_foot: [6, -12], far_foot: [-2, -14], near_foot_angle: -0.2, far_foot_angle: -0.3, near_hand: [12, 1], far_hand: [-8, 4], hair: -0.15 }),
    ],
    jump_fall: [
        her_pose({ hip_y: -45, lean: 0.02, near_foot: [5, -4], far_foot: [-5, -6], near_foot_angle: 0, far_foot_angle: -0.3, near_hand: [12, 10], far_hand: [-10, 8], hair: 0.2, hem: -0.05 }),
        her_pose({ hip_y: -44, lean: 0.02, near_foot: [4, 0], far_foot: [-4, -1], near_foot_angle: 0.1, far_foot_angle: -0.1, near_hand: [9, 16], far_hand: [-7, 14], hair: 0.25, hem: -0.06 }),
    ],
    land: [
        her_pose({ hip_y: -33, lean: 0.16, near_foot: [7, 0], far_foot: [-6, 0], near_hand: [9, 18], far_hand: [-3, 20], hair: -0.1, hem: 0.06 }),
        her_pose({ hip_y: -40, lean: 0.06, near_foot: [4, 0], far_foot: [-4, 0], near_hand: [3, 24], far_hand: [-1, 24], hair: 0.06, hem: 0.02 }),
    ],
    turn: [
        her_pose({ hip_y: -42, lean: -0.1, near_foot: [6, 0], far_foot: [-3, 0], near_hand: [-5, 24], far_hand: [6, 23], hair: 0.25, scarf: 0.6, hem: -0.06 }),
        her_pose({ hip_y: -43, lean: 0.05, near_foot: [4, 0], far_foot: [-4, 0], near_hand: [2, 25], far_hand: [0, 25], hair: 0.12, scarf: 0.45, hem: 0.03 }),
    ],
    kneel: [
        her_pose({ hip_y: -35, hip_x: 0, lean: 0.12, near_foot: [11, 0], far_foot: [-10, 0], far_foot_angle: -0.5, near_hand: [8, 22], far_hand: [3, 23], hair: 0.05, hem: 0.04 }),
        her_pose({ hip_y: -23, hip_x: -1, lean: 0.24, near_foot: [13, 0], far_foot: [-19, 0], far_foot_angle: -1.3, near_hand: [9, 23], far_hand: [5, 24], hair: 0.1, hem: 0.06 }),
        her_pose({ hip_y: -23.3, hip_x: -1, lean: 0.23, near_foot: [13, 0], far_foot: [-19, 0], far_foot_angle: -1.3, near_hand: [9, 23], far_hand: [5, 24], hair: 0.07, hem: 0.05 }),
    ],
    embrace: [0, 1].map(function hold(step) {
        return her_pose({ hip_y: -23 - step * 0.4, hip_x: -1, lean: 0.2, near_foot: [13, 0], far_foot: [-19, 0], far_foot_angle: -1.3, near_hand: [11, 19], far_hand: [16, 14], head_tilt: 0.14, eyes: 'closed', hair: 0.05 });
    }),
    sit: [0, 1].map(function breathe(step) {
        return her_pose({ hip_y: -22 - step * 0.3, hip_x: 0, lean: 0.05, near_foot: [21, 0], far_foot: [18, 0], near_hand: [15, 22], far_hand: [12, 22], hair: 0.02 * step });
    }),
};

// ---------- The dog ----------

// A point on the dog's body: `along` the spine (positive toward the chest) and `up` from it,
// measured from `base`. Using the body's own directions keeps shapes right when it rears up.
function body_point(base, axis, along, up) {
    const normal = [axis[1], -axis[0]];
    return add(base, add(scale(axis, along), scale(normal, up)));
}

// Draws one frame of the dog as SVG, from a pose in game units (feet at 0, 0; facing right).
function draw_dog(pose) {
    const pelvis = pose.pelvis;
    const withers = pose.withers;
    const axis = normalized(subtract(withers, pelvis));
    const spine_middle = middle(pelvis, withers);
    const arch = pose.arch || 0;
    const head = pose.head;
    // The silhouette, from the rump, along the back and neck, round the chest and belly.
    const outline = [
        body_point(pelvis, axis, -11, 0),
        body_point(pelvis, axis, -8, 8.5),
        body_point(pelvis, axis, -1, 10.5),
        body_point(spine_middle, axis, 0, 10.5 + arch),
        body_point(withers, axis, 1, 11.5),
        add(head, [-7.5, -5]),
        add(head, [-1, 9]),
        body_point(withers, axis, 10, -2),
        body_point(withers, axis, 7, -9.5),
        body_point(withers, axis, 1, -12),
        body_point(spine_middle, axis, 0, -10.5 + (pose.tuck || 2) + arch * 0.4),
        body_point(pelvis, axis, 4, -9.5),
        body_point(pelvis, axis, -6, -9.5),
        body_point(pelvis, axis, -10.5, -5),
    ].map(to_pixels);

    // A leg: one furry tube from inside the body, through the joint, to the paw.
    const leg = function draw_leg(top, paw, widths, color) {
        const paw_point = add(paw, [0, -1.2]);
        // Standing, the joints bend back as a dog's do; lying down (`folded`), they fold up into
        // the body instead of poking down through the floor.
        const joint = solve_joint(top, paw_point, 10.5, 10.5, pose.folded ? -1 : 1);
        const points = [top, joint, paw_point].map(to_pixels);
        const paw_pixels = to_pixels(add(paw_point, [0.8, 0]));
        return '<path d="' + tube_path(points, widths) + '" fill="' + color + '"/>' +
            '<ellipse cx="' + number_text(paw_pixels[0]) + '" cy="' + number_text(paw_pixels[1]) + '" rx="54" ry="31" fill="' + color + '"/>';
    };
    const front_top_near = body_point(withers, axis, 2, -3);
    const front_top_far = body_point(withers, axis, 0, -3);
    const back_top_near = body_point(pelvis, axis, 1, -3);
    const back_top_far = body_point(pelvis, axis, -1, -3);

    // The tail: a fluffy plume from the rump, pointing back and up by `tail` radians.
    const tail_root = body_point(pelvis, axis, -8.5, 6);
    const tail_direction = rotate_point([-1, 0], -(pose.tail || 0));
    const tail_tip = add(tail_root, scale(tail_direction, 14));
    const tail_middle = add(middle(tail_root, tail_tip), [0, -(pose.tail_curl || 2)]);
    const tail_points = [tail_root, tail_middle, tail_tip].map(to_pixels);

    const fur = [
        '<g filter="url(#far_side)">',
        leg(front_top_far, pose.legs.far_front, [96, 76, 66], 'url(#coat)'),
        leg(back_top_far, pose.legs.far_back, [124, 82, 66], 'url(#coat_dark)'),
        '</g>',
        '<path d="' + tube_path(tail_points, [70, 76, 40]) + '" fill="url(#coat_light)"/>',
        '<path d="' + tube_path(tail_points, [70, 76, 40]) + '" fill="url(#curls)"/>',
        // Near legs go behind the body: the coat hides where they join, as on a real dog. A front
        // leg reaching up (the hug) is drawn over the body instead, so it reads as an arm.
        pose.reaching ? '' : leg(front_top_near, pose.legs.near_front, [100, 80, 68], 'url(#coat)'),
        leg(back_top_near, pose.legs.near_back, [128, 86, 68], 'url(#coat)'),
        '<path d="' + smooth_closed_path(outline) + '" fill="url(#coat)"/>',
        '<path d="' + smooth_closed_path(outline) + '" fill="url(#curls)"/>',
        pose.reaching ? '<g transform="translate(0,8)" opacity="0.35">' + leg(front_top_near, pose.legs.near_front, [92, 74, 64], '#6b4320') + '</g>' + leg(front_top_near, pose.legs.near_front, [92, 74, 64], 'url(#coat_light)') : '',
    ];
    // Fur detail: a lighter back where the light catches it.
    const back_line = [body_point(pelvis, axis, -5, 8), body_point(spine_middle, axis, 0, 9 + arch), body_point(withers, axis, 1, 9)].map(to_pixels);
    let head_markup = place_part(dog_parts.head, head, pose.head_angle || 0);
    const overlays = [];
    if (pose.eyes === 'closed') {
        overlays.push('<ellipse cx="44" cy="-22" rx="22" ry="24" fill="#f0d0a0"/><path d="M 26,-20 C 36,-8 54,-8 64,-20" fill="none" stroke="#3a2418" stroke-width="5" stroke-linecap="round"/>');
    }
    if (pose.mouth === 'open') {
        overlays.push('<path d="M 82,60 C 100,92 136,90 148,62 C 130,74 102,74 82,60 Z" fill="#5a2a22"/><path d="M 96,72 C 108,84 126,84 136,72" fill="#c96a62"/>');
    }
    if (overlays.length) {
        head_markup = head_markup.replace(/<\/g>$/, overlays.join('') + '</g>');
    }
    const ear_root = add(head, rotate_point([-4.2, -3.6], pose.head_angle || 0));
    const ear_markup = place_part(dog_parts.ear, ear_root, (pose.head_angle || 0) + (pose.ear || 0.2), ' scale(0.92)');
    return '<g filter="url(#fluffy)">' + fur.join('') + '</g>' +
        '<path d="M ' + back_line.map(function point_text(point) { return point.map(number_text).join(','); }).join(' L ') + '" fill="none" stroke="#fff3d8" stroke-width="7" stroke-linecap="round" opacity="0.45"/>' +
        head_markup + ear_markup;
}

// Builds a dog pose from a base standing pose plus changes.
function dog_pose(changes) {
    const standing = {
        pelvis: [-11, -27], withers: [11, -28], arch: 0, tuck: 2, head: [19, -39], head_angle: 0, ear: 0.22, tail: 0.45, tail_curl: 2.5,
        legs: { near_front: [13, 0], far_front: [11, 0], near_back: [-8, 0], far_back: [-11, 0] }, eyes: 'open', mouth: 'closed',
    };
    const result = Object.assign({}, standing, changes);
    result.legs = Object.assign({}, standing.legs, changes.legs || {});
    return result;
}

// The trot: diagonal legs move together. One leg's path over 8 frames: planted and pushed back
// for five frames (weight on it), then a quick lifted swing forward for three.
const trot_paw_path = [[8, 0], [4, 0], [0, 0], [-4, 0], [-8, -0.6], [-5, -4.5], [2, -5.5], [6, -2]];
const trot_body_lift = [-0.4, 0.4, -0.3, -1.0, -0.4, 0.4, -0.3, -1.0];
const dog_trot = trot_paw_path.map(function trot_frame(unused, frame) {
    const pair_one = trot_paw_path[frame];
    const pair_two = trot_paw_path[(frame + 4) % 8];
    const lift = trot_body_lift[frame];
    return dog_pose({
        pelvis: [-11, -27 + lift], withers: [11, -28 + lift], head: [19, -39 + lift * 0.4], head_angle: [0, 0.04, 0, -0.03][frame % 4],
        ear: [0.3, 0.42, 0.36, 0.26][frame % 4], tail: [0.5, 0.42, 0.48, 0.58][frame % 4],
        legs: {
            near_front: [13 + pair_one[0], pair_one[1]], far_back: [-11 + pair_one[0], pair_one[1]],
            far_front: [11 + pair_two[0], pair_two[1]], near_back: [-8 + pair_two[0], pair_two[1]],
        },
    });
});

// The gallop (a "rotary" gallop): stretch out, front feet land, gather the back feet under a
// rounded spine, back feet land, push, fly. The spine extending and gathering is what makes a
// running dog look alive.
const dog_gallop = [
    dog_pose({ pelvis: [-12.5, -28], withers: [12.5, -29], arch: -1.5, head: [22, -37], head_angle: 0.1, ear: 0.7, tail: 0.15,
        legs: { near_front: [22, -3], far_front: [20, -4], near_back: [-21, -2], far_back: [-19, -3] } }),
    dog_pose({ pelvis: [-11.5, -29.5], withers: [11.5, -27], arch: 0, head: [20, -37], head_angle: 0.06, ear: 0.6, tail: 0.2,
        legs: { near_front: [18, 0], far_front: [15, -3], near_back: [-12, -5], far_back: [-15, -4] } }),
    dog_pose({ pelvis: [-10, -29], withers: [10, -28], arch: 2.5, head: [18, -40], head_angle: -0.04, ear: 0.45, tail: 0.3,
        legs: { near_front: [9, 0], far_front: [12, 0], near_back: [-7, -6], far_back: [-9, -5] } }),
    dog_pose({ pelvis: [-8.5, -29.5], withers: [8.5, -29], arch: 4, head: [17, -40], head_angle: -0.06, ear: 0.5, tail: 0.25,
        legs: { near_front: [5, -5], far_front: [7, -6], near_back: [0, -3], far_back: [-2, -4] } }),
    dog_pose({ pelvis: [-10, -26.5], withers: [10, -29], arch: 2, head: [18, -40], head_angle: 0, ear: 0.65, tail: 0.15,
        legs: { near_front: [14, -6], far_front: [12, -5], near_back: [-4, 0], far_back: [-7, 0] } }),
    dog_pose({ pelvis: [-11.5, -27.5], withers: [11.5, -29.5], arch: 0, head: [21, -39], head_angle: 0.06, ear: 0.7, tail: 0.1,
        legs: { near_front: [20, -4], far_front: [18, -5], near_back: [-15, 0], far_back: [-18, -1] } }),
];

const dog_animations = {
    idle: [0.55, 0.78, 0.55, 0.32].map(function wag(tail, frame) {
        const breath = [0, -0.2, -0.3, -0.2][frame];
        return dog_pose({ pelvis: [-11, -27 + breath], withers: [11, -28 + breath], head: [19, -39 + breath * 0.5], tail: tail, ear: 0.22 + frame * 0.01 });
    }),
    trot: dog_trot,
    gallop: dog_gallop,
    jump_rise: [
        dog_pose({ pelvis: [-11, -26], withers: [11, -33], arch: 0, head: [20, -44], head_angle: -0.15, ear: 0.1, tail: 0.6,
            legs: { near_front: [17, -12], far_front: [15, -11], near_back: [-19, -2], far_back: [-21, -1] } }),
        dog_pose({ pelvis: [-11, -28], withers: [11, -31], arch: 1.5, head: [20, -42], head_angle: -0.05, ear: 0.0, tail: 0.5,
            legs: { near_front: [20, -9], far_front: [18, -9], near_back: [-8, -9], far_back: [-10, -8] } }),
    ],
    jump_fall: [
        dog_pose({ pelvis: [-11, -30], withers: [11, -27], arch: 0, head: [21, -37], head_angle: 0.1, ear: 0.55, tail: 0.35,
            legs: { near_front: [19, -5], far_front: [17, -6], near_back: [-15, -6], far_back: [-13, -7] } }),
        dog_pose({ pelvis: [-11, -29], withers: [11, -26], arch: 0, head: [21, -37], head_angle: 0.08, ear: 0.65, tail: 0.3,
            legs: { near_front: [16, -1], far_front: [14, -2], near_back: [-12, -4], far_back: [-10, -5] } }),
    ],
    land: [
        dog_pose({ pelvis: [-10.5, -22], withers: [10.5, -21], arch: -0.5, head: [19, -32], head_angle: 0.12, ear: 0.75, tail: 0.3,
            legs: { near_front: [14, 0], far_front: [12, 0], near_back: [-9, 0], far_back: [-12, 0] } }),
        dog_pose({ pelvis: [-11, -25.5], withers: [11, -26.5], head: [19, -37], head_angle: 0.04, ear: 0.45, tail: 0.4 }),
    ],
    turn: [
        dog_pose({ pelvis: [-8, -27], withers: [8, -28], arch: 2, head: [16, -38], head_angle: -0.12, ear: 0.5, tail: 0.7,
            legs: { near_front: [9, 0], far_front: [7, 0], near_back: [-5, 0], far_back: [-7, 0] } }),
        dog_pose({ pelvis: [-10, -27], withers: [10, -28], arch: 0.5, head: [18, -39], head_angle: -0.04, ear: 0.32, tail: 0.6 }),
    ],
    sit: [
        dog_pose({ pelvis: [-9, -18], withers: [10, -28], head: [18, -41], ear: 0.25, tail: 0.1, legs: { near_back: [-3, 0], far_back: [-6, 0] } }),
        dog_pose({ pelvis: [-7, -12], withers: [8, -28], arch: -1, head: [16, -43], ear: 0.22, tail: -0.25, legs: { near_front: [12, 0], far_front: [10, 0], near_back: [1, 0], far_back: [-2, 0] } }),
        dog_pose({ pelvis: [-7, -12], withers: [8, -28.3], arch: -1, head: [16, -43.3], ear: 0.24, tail: -0.05, legs: { near_front: [12, 0], far_front: [10, 0], near_back: [1, 0], far_back: [-2, 0] } }),
        dog_pose({ pelvis: [-7, -12], withers: [8, -28], arch: -1, head: [16, -43], ear: 0.22, tail: -0.4, legs: { near_front: [12, 0], far_front: [10, 0], near_back: [1, 0], far_back: [-2, 0] } }),
    ],
    sniff: [-16, -15, -16.5, -17].map(function nose(head_y, frame) {
        return dog_pose({ pelvis: [-11, -27.5], withers: [11, -26], head: [25, head_y], head_angle: 0.4 + frame * 0.02, ear: 0.0, tail: 0.5 + (frame % 2) * 0.15,
            legs: { near_front: [14, 0], far_front: [12, 0] } });
    }),
    bark: [
        dog_pose({ pelvis: [-11, -26], withers: [11, -26.5], head: [17, -36], head_angle: 0.1, ear: 0.3, tail: 0.5 }),
        dog_pose({ pelvis: [-11, -27.5], withers: [11, -29.5], head: [21, -43], head_angle: -0.3, ear: 0.0, tail: 0.65, mouth: 'open' }),
        dog_pose({ pelvis: [-11, -27], withers: [11, -28.5], head: [20, -41], head_angle: -0.15, ear: 0.15, tail: 0.55 }),
    ],
    sleep: [0, 1].map(function breathe(step) {
        return dog_pose({ pelvis: [-10, -10 - step * 0.4], withers: [10, -11 - step * 0.4], arch: -2, tuck: 0, head: [21, -10], head_angle: 0.15, ear: 0.6, tail: -0.15, tail_curl: -1,
            eyes: 'closed', folded: true, legs: { near_front: [22, -2], far_front: [20, -2], near_back: [-1, -1], far_back: [-4, -1] } });
    }),
    lean: [0, 1].map(function nuzzle(step) {
        return dog_pose({ pelvis: [-11, -27], withers: [11, -27.5], head: [21, -35 + step * 0.4], head_angle: -0.2, ear: 0.3, tail: 0.7 - step * 0.35, eyes: 'closed' });
    }),
    embrace: [0, 1].map(function hug(step) {
        return dog_pose({ pelvis: [-5, -14], withers: [2, -36 - step * 0.4], arch: 0, tuck: 0, head: [8, -50 - step * 0.4], head_angle: 0.35, ear: 0.25, tail: 0.2 + step * 0.25,
            eyes: 'closed', reaching: true, legs: { near_front: [19, -47], far_front: [16, -45], near_back: [-4, 0], far_back: [-7, 0] } });
    }),
};

// ---------- Writing the files ----------

// The cell each frame is drawn in, in game units, and where the feet sit inside it.
const cells = {
    her: { width: 64, height: 106, feet_x: 30, feet_y: 99 },
    bodhi: { width: 90, height: 84, feet_x: 42, feet_y: 76 },
};

// How far each walking cycle travels, in game units (see cycle_length_of). The gallop's paws
// move back about 10 units a frame over its 6 frames.
const cycle_lengths = {
    her: { walk: cycle_length_of(her_walk_keys), run: cycle_length_of(her_run_keys) },
    bodhi: { trot: (trot_paw_path[0][0] - trot_paw_path[1][0]) * trot_paw_path.length, gallop: 60 },
};

// Points the game draws things at, per frame: what she holds goes in her near hand; what the dog
// carries goes in its mouth; its blink goes over its eye (with the head's angle).
function frame_points(character, pose) {
    if (character === 'her') {
        return { hand: [pose.near_hand[0], pose.near_hand[1]].map(round_tenth) };
    }
    const angle = pose.head_angle || 0;
    return {
        mouth: add(pose.head, rotate_point([11.5, 6.5], angle)).map(round_tenth),
        eye: add(pose.head, rotate_point([4.4, -2.2], angle)).map(round_tenth).concat([round_tenth(angle)]),
    };
}

// Rounds to one decimal place, keeping the atlas file short.
function round_tenth(value) {
    return Math.round(value * 10) / 10;
}

// Writes one SVG file per animation: frames side by side, each an editable drawing.
function write_animation(character, name, frames, definitions, draw) {
    const cell = cells[character];
    const width = cell.width * units_to_pixels;
    const height = cell.height * units_to_pixels;
    const pieces = frames.map(function frame_group(pose, index) {
        const feet = [index * width + cell.feet_x * units_to_pixels, cell.feet_y * units_to_pixels];
        return '  <g id="frame_' + index + '" transform="translate(' + feet[0] + ',' + feet[1] + ')">' + draw(pose) + '</g>';
    });
    const svg = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        '<!-- Purpose: ' + character + ', "' + name + '" animation, ' + frames.length + ' frames side by side, each ' + width + ' wide.',
        '     Generated by art/tools/draw_frames.js; any frame can be redrawn by hand afterwards. Feet sit at',
        '     (' + cell.feet_x * units_to_pixels + ', ' + cell.feet_y * units_to_pixels + ') inside each frame. -->',
        '<svg xmlns="http://www.w3.org/2000/svg" width="' + width * frames.length + '" height="' + height + '" viewBox="0 0 ' + width * frames.length + ' ' + height + '">',
        '<defs>' + definitions + shade_filter + '</defs>',
    ].concat(pieces, ['</svg>']).join('\n');
    const file_name = character + '_' + name + '.svg';
    fs.writeFileSync(path.join(output_folder, file_name), svg);
    const entry = { sheet: 'art/animations/' + file_name, frames: frames.length };
    if (cycle_lengths[character][name]) {
        entry.cycle_length = cycle_lengths[character][name];
    }
    entry.points = frames.map(function points_of(pose) { return frame_points(character, pose); });
    return entry;
}

function main() {
    fs.mkdirSync(output_folder, { recursive: true });
    // A lighter grain for whole frames (the painted parts already carry their own).
    const her_definitions = extract_definitions(her_sheet) + '<linearGradient id="sleeve_lit" x1="1" y1="0" x2="0" y2="0"><stop offset="0" stop-color="#5aa59f"/><stop offset="1" stop-color="#2c625f"/></linearGradient>' + '<filter id="grain_soft" x="-5%" y="-5%" width="110%" height="110%"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="1" seed="9" result="noise"/><feColorMatrix in="noise" type="saturate" values="0" result="mono"/><feComponentTransfer in="mono" result="faint"><feFuncA type="table" tableValues="0 0.06"/></feComponentTransfer><feComposite in="faint" in2="SourceGraphic" operator="in" result="grain"/><feBlend in="grain" in2="SourceGraphic" mode="multiply"/></filter>';
    const dog_definitions = extract_definitions(dog_sheet);
    const atlas = { her: { cell: cells.her, animations: {} }, bodhi: { cell: cells.bodhi, animations: {} } };
    for (const [name, frames] of Object.entries(her_animations)) {
        atlas.her.animations[name] = write_animation('her', name, frames, her_definitions, draw_her);
    }
    for (const [name, frames] of Object.entries(dog_animations)) {
        atlas.bodhi.animations[name] = write_animation('bodhi', name, frames, dog_definitions, draw_dog);
    }
    const header = [
        '// Purpose: where each hand-keyed animation frame is: one sheet per animation, frames side by side.',
        '// Generated by art/tools/draw_frames.js. Do not edit by hand: change the poses or the art,',
        '// then run  node art/tools/draw_frames.js',
        "'use strict';",
        '',
    ].join('\n');
    const lines = ['const frames_atlas = {'];
    for (const [character, entry] of Object.entries(atlas)) {
        lines.push('    ' + character + ': {', '        cell: ' + JSON.stringify(entry.cell) + ',', '        animations: {');
        for (const [name, animation] of Object.entries(entry.animations)) {
            lines.push('            ' + name + ': ' + JSON.stringify(animation) + ',');
        }
        lines.push('        },', '    },');
    }
    lines.push('};', '');
    fs.writeFileSync(atlas_path, header + lines.join('\n'));
    let total = 0;
    for (const character of Object.values(atlas)) {
        for (const animation of Object.values(character.animations)) {
            total += animation.frames;
        }
    }
    console.log('drew ' + total + ' frames into ' + path.relative(repository_root, output_folder) + ' and wrote ' + path.relative(repository_root, atlas_path));
}

main();
