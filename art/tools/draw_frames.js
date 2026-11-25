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
const gaits = require('./gaits.js');  // How a real person and a real dog walk, drawing by drawing.

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

// ---------- In-betweens ----------
// Animators draw the key poses first, then the drawings in between them. These functions make
// the in-betweens from the keyed poses along smooth curves (Catmull-Rom splines), not straight
// lines, so the motion flows through each key instead of turning sharply at it.

// Points that stand on the floor. Between two keys where one is planted, it moves in a straight
// line at an even speed (so it never slides), and no in-between pushes it below the floor.
const floor_points = new Set(['near_foot', 'far_foot', 'near_front', 'far_front', 'near_back', 'far_back']);

// One number partway (t from 0 to 1) from b to c, on a smooth curve through a, b, c and d.
function spline_number(a, b, c, d, t) {
    return 0.5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (3 * b - a - 3 * c + d) * t * t * t);
}

// One pose partway from pose b to pose c, field by field. Numbers and [x, y] points follow the
// curve, nested groups (the dog's legs) are blended the same way, and words such as 'closed'
// switch halfway. A field a neighbouring key leaves out counts as unchanged.
function spline_pose(a, b, c, d, t) {
    const result = {};
    for (const name of Object.keys(b)) {
        const value = b[name];
        const before = a[name] === undefined ? value : a[name];
        const after = c[name] === undefined ? value : c[name];
        const later = d[name] === undefined ? after : d[name];
        if (typeof value === 'number') {
            result[name] = spline_number(before, value, after, later, t);
        } else if (Array.isArray(value)) {
            if (floor_points.has(name) && value[1] === 0 && after[1] === 0) {
                result[name] = [value[0] + (after[0] - value[0]) * t, 0];
            } else {
                result[name] = value.map(function blend(coordinate, index) {
                    return spline_number(before[index], coordinate, after[index], later[index], t);
                });
                if (floor_points.has(name)) {
                    result[name][1] = Math.min(0, result[name][1]);
                }
            }
        } else if (value && typeof value === 'object') {
            result[name] = spline_pose(before, value, after, later, t);
        } else {
            result[name] = t < 0.5 ? value : after;
        }
    }
    return result;
}

// How many drawings go from one key to the next. Walking cycles get the most, because they are
// on screen the most and a planted foot must move back in small, even steps; one-off moves get
// four too; breathing loops (idle, sitting) are drawn with twice as many drawings as they first had.
// More drawings, played proportionally faster, keep every move's timing but halve how far
// anything jumps between two drawings, which is what reads as shaky on a phone.
const cycle_steps = 4;
// Walk and trot cycles are drawn 40 times a cycle: they are on screen most, and a foot swinging
// forward moves fast, so more drawings keep each jump between them small.
const walk_drawings = 40;
const move_steps = 4;
// The jump's drawings are picked by how fast the body rises or falls, not by time, so extra ones
// cost no speed: the knees tucking up is quick, and needs them most.
const jump_steps = 6;
// The biggest moves (getting down onto the knees or rising into the hug, the hands or paws
// travelling half a body's height, and the impact of landing) get six, so they still glide.
const big_move_steps = 6;
const breath_detail = 2;

// Makes `steps` drawings from each key to the next. A loop (a walk, a trot) wraps round, so the
// last key flows back into the first; a one-off move (a landing) ends exactly on its last key,
// arriving there at rest (it slows into it), so whatever plays next starts from stillness
// instead of from a body that stopped dead mid-motion. Its start keeps its speed, since a
// landing or a skid begins with the body already moving.
function in_between(keys, steps, loops) {
    const count = keys.length;
    const key_at = function key_at(index) {
        if (loops) {
            return keys[(index + count) % count];
        }
        if (index >= count) {
            return keys[count - 2];  // Mirrors the second-to-last key: no speed at the last one.
        }
        return keys[Math.max(0, index)];
    };
    const frames = [];
    const segments = loops ? count : count - 1;
    for (let index = 0; index < segments; index++) {
        for (let step = 0; step < steps; step++) {
            frames.push(spline_pose(key_at(index - 1), key_at(index), key_at(index + 1), key_at(index + 2), step / steps));
        }
    }
    if (!loops) {
        frames.push(keys[count - 1]);
    }
    return frames;
}

// Follow-through: the loose parts (her ponytail, scarf and coat hem; the dog's ears and tail)
// don't move with the body like the rest of it. Each one is a spring pulled toward the angle the
// keys give it: it gets there a moment late, swings a little past, and settles. That lag and
// overshoot is what makes hair and ears read as soft instead of glued on.
//   parts:   { field: period }, the spring's swing time in beats (one beat = one key's worth of
//            drawings); longer is floppier (a long ear, a scarf end).
//   beat:    drawings per beat.
//   loops:   a loop is simulated round several times and the last time kept, so it joins up.
//            A one-off move starts on its keyed angles and eases back onto them over its last
//            beat, so it hands over to the next move exactly as before.
const follow_damping = 0.45;   // Under 1: a little overshoot. 1 would settle with no swing past.
const follow_substeps = 8;     // Small steps through each drawing keep the spring accurate.
function follow_through(frames, parts, beat, loops) {
    const result = frames.map(function copy(pose) { return Object.assign({}, pose); });
    for (const [field, period] of Object.entries(parts)) {
        const targets = frames.map(function target(pose) { return pose[field]; });
        const speed = 2 * Math.PI / (period * beat);  // Natural swing speed, in turns per drawing.
        let value = targets[0];
        let velocity = 0;
        const rounds = loops ? 4 : 1;
        for (let round = 0; round < rounds; round++) {
            for (let index = 0; index < frames.length; index++) {
                if (round === rounds - 1) {
                    result[index][field] = value;
                }
                const next_target = targets[(index + 1) % frames.length];
                for (let step = 0; step < follow_substeps; step++) {
                    const fraction = (step + 0.5) / follow_substeps;
                    const target = loops || index + 1 < frames.length ? targets[index] + (next_target - targets[index]) * fraction : targets[index];
                    const pull = speed * speed * (target - value) - 2 * follow_damping * speed * velocity;
                    velocity += pull / follow_substeps;
                    value += velocity / follow_substeps;
                }
            }
        }
        if (!loops) {
            // Ease back onto the keyed angle over the last beat (a smooth S-curve blend).
            for (let index = Math.max(0, frames.length - 1 - beat); index < frames.length; index++) {
                const along = beat > 0 ? (index - (frames.length - 1 - beat)) / beat : 1;
                const weight = along * along * (3 - 2 * along);
                result[index][field] = result[index][field] + (targets[index] - result[index][field]) * weight;
            }
        }
    }
    return result;
}

// How floppy each loose part is (see follow_through).
const her_loose_parts = { hair: 2.0, scarf: 2.6, hem: 1.4 };
const dog_loose_parts = { ear: 1.8, tail: 2.4 };

// A gentle loop of `count` drawings. `make(breath, turn)` gives the pose for breath 0 (out) to
// 1 (in), with turn going from 0 to 1 once round the loop, for swaying things like a tail.
function breathing(count, make) {
    const frames = [];
    for (let index = 0; index < count; index++) {
        const turn = index / count;
        frames.push(make((1 - Math.cos(2 * Math.PI * turn)) / 2, turn));
    }
    return frames;
}

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
// A short curved line across a limb, for a crease or a seam. `from` and `to` are the ends of
// one bone in pixels; the line sits `along` (0 to 1) of the way down it, reaches `width`
// pixels either side of the bone's middle, bows `bulge` pixels toward `to`, and is moved
// `shift` pixels to one side (negative is toward the front of a limb hanging straight down).
function across_limb(from, to, along, width, bulge, shift) {
    const direction = normalized(subtract(to, from));
    const side = [-direction[1], direction[0]];
    const centre = add(add(from, scale(subtract(to, from), along)), scale(side, shift));
    const start = add(centre, scale(side, -width));
    const end = add(centre, scale(side, width));
    const bend = add(centre, scale(direction, bulge));
    return 'M ' + start.map(number_text).join(',') + ' Q ' + bend.map(number_text).join(',') + ' ' + end.map(number_text).join(',');
}

// A line down a limb through its joints (pixel points), kept `shift` pixels to one side of
// the middle, as a seam is.
function along_limb(points, shift) {
    return 'M ' + points.map(function moved(point, index) {
        const before = points[Math.max(0, index - 1)];
        const after = points[Math.min(points.length - 1, index + 1)];
        const direction = normalized(subtract(after, before));
        return add(point, scale([-direction[1], direction[0]], shift)).map(number_text).join(',');
    }).join(' L ');
}

function draw_her(pose) {
    const torso_angle = torso_angle_of(pose);
    const head_angle = torso_angle * 0.4 + (pose.head_tilt || 0);
    const pieces = [];

    // A leg: one jeans tube from hip through knee to ankle, a knee crease, then the boot.
    const leg = function draw_leg(foot, foot_angle, near) {
        const ankle = add(foot, [0, -3.6]);
        const knee = solve_joint(pose.hip, ankle, her_thigh_length, her_shin_length, -1);
        const points = [pose.hip, knee, ankle].map(to_pixels);
        const jeans = tube_path(points, [80, 58, 46]);
        let text = '<path d="' + jeans + '" fill="' + (near ? 'url(#jeans)' : '#2b3550') + '" stroke="#161c2b" stroke-width="3" stroke-linejoin="round"/>';
        // Denim close up: the diagonal weave, gold topstitching down the side seam, creases
        // where the leg bends at the hip, and the jeans bunching where they meet the boot.
        text += '<path d="' + jeans + '" fill="url(#twill)"/>';
        text += '<path d="' + along_limb([points[0], points[1], points[2]], -3) + '" fill="none" stroke="#c49a52" stroke-width="1.6" stroke-dasharray="5 4" stroke-linejoin="round" opacity="0.5"/>';
        text += '<path d="' + across_limb(points[0], points[1], 0.2, 14, 4, -14) + ' ' + across_limb(points[0], points[1], 0.3, 12, 4, -16) +
            ' ' + across_limb(points[1], points[2], 0.74, 21, -5, 0) + ' ' + across_limb(points[1], points[2], 0.84, 22, -5, 0) + '" fill="none" stroke="#141a28" stroke-width="2.2" stroke-linecap="round" opacity="0.4"/>';
        text += '<path d="' + across_limb(points[1], points[2], 0.71, 20, -5, 0) + ' ' + across_limb(points[1], points[2], 0.81, 21, -5, 0) + '" fill="none" stroke="#6c7eaa" stroke-width="1.8" stroke-linecap="round" opacity="0.35"/>';
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
        // Sleeve close up: the seam where the sleeve is sewn on at the shoulder, a long soft
        // fold down the upper arm, and the fabric bunching inside the elbow, with light on the
        // raised edge of each fold.
        text += '<path d="' + across_limb(points[0], points[1], 0.12, 24, 5, 0) + '" fill="none" stroke="#0f2a29" stroke-width="2.2" stroke-linecap="round" opacity="0.45"/>' +
            '<path d="' + across_limb(points[0], points[1], 0.17, 23, 5, 0) + '" fill="none" stroke="#7cc0b8" stroke-width="1.4" stroke-dasharray="4 3" opacity="0.45"/>' +
            '<path d="' + along_limb([add(points[0], scale(subtract(points[1], points[0]), 0.3)), add(points[0], scale(subtract(points[1], points[0]), 0.85))], 6) + '" fill="none" stroke="#0f2a29" stroke-width="2.5" stroke-linecap="round" opacity="0.25"/>' +
            '<path d="' + across_limb(points[1], points[2], 0.02, 14, -6, -9) + ' ' + across_limb(points[0], points[1], 0.9, 13, 6, -9) + '" fill="none" stroke="#0f2a29" stroke-width="2.6" stroke-linecap="round" opacity="0.4"/>' +
            '<path d="' + across_limb(points[1], points[2], 0.09, 13, -6, -9) + '" fill="none" stroke="#7cc0b8" stroke-width="1.6" stroke-linecap="round" opacity="0.4"/>';
        const cuff_direction = normalized(subtract(wrist, elbow));
        // Close up, the sleeve ended in a flat dark disc with an oval for a hand. Now the cuff
        // is a turned-back band with the dark opening of the sleeve inside it, and the hand
        // comes out of that opening: palm, fingers curled loosely as a resting hand's are, the
        // thumb in front, and a crease where the fingers fold.
        const cuff = to_pixels(subtract(wrist, scale(cuff_direction, 0.4)));
        const turn = number_text(degrees(Math.atan2(cuff_direction[1], cuff_direction[0])) - 90);
        const at_cuff = 'translate(' + number_text(cuff[0]) + ',' + number_text(cuff[1]) + ') rotate(' + turn + ')';
        text += '<g transform="' + at_cuff + '">' +
            '<ellipse cx="0" cy="2" rx="21" ry="8" fill="#0c2322"/>' +
            '<path d="M -14,0 C -17,14 -16,28 -12,38 C -10,43 -6,46 -1,46 C 2,48 6,47 8,44 C 12,42 14,36 12,30 C 13,27 13,25 12,24 C 16,22 19,17 18,11 C 17,6 15,2 14,0 Z" fill="url(#skin)" stroke="#b9836a" stroke-width="1.6" stroke-linejoin="round"/>' +
            '<path d="M 10,7 C 18,10 21,20 16,27 C 14,28 12,27 11,25 C 12,20 11,15 9,12 Z" fill="#edc4aa" stroke="#b9836a" stroke-width="1.4" stroke-linejoin="round"/>' +
            '<path d="M -9,30 C -6,37 0,41 7,40 M -4,34 C -2,40 2,44 5,45 M -12,22 C -11,26 -10,29 -8,31" fill="none" stroke="#c48d72" stroke-width="1.8" stroke-linecap="round" opacity="0.8"/>' +
            '<path d="M -13,6 C -15,16 -14,26 -11,34" fill="none" stroke="#b07d64" stroke-width="2.5" stroke-linecap="round" opacity="0.35"/>' +
            '<path d="M -22,-6 C -22,4 22,4 22,-6 L 21,-12 C 21,-4 -21,-4 -21,-12 Z" fill="#1f4c4b"/>' +
            '<path d="M -20,-4 C -10,1 10,1 20,-4" fill="none" stroke="#5aa59f" stroke-width="2" stroke-linecap="round" opacity="0.5"/>' +
            '<circle cx="13" cy="-7" r="3.4" fill="#163837"/><circle cx="12.2" cy="-7.8" r="1.1" fill="#6fb3ad" opacity="0.7"/>' +
            '</g>';
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
    // Her long hair, worn down: it hangs from the back of the crown over the coat's shoulders.
    pieces.push(place_part(her_parts.ponytail, add(pose.shoulder, rotate_point([-7.2, -12.6], head_angle)), 0.14 + (pose.hair || 0)));
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
        head_tilt: values.head_tilt || 0, hair: values.hair || 0, scarf: values.scarf === undefined ? 0.3 : values.scarf,  // A scarf at 0 (the skid) is a real value, not missing.
        hem: values.hem || 0,
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

// Fills in the values a key pose leaves out, so every key has the same fields to blend.
function her_values(values) {
    return Object.assign({ hip_x: 0, lean: 0, near_foot_angle: 0, far_foot_angle: 0, hair: 0, hem: 0, scarf: 0.3, head_tilt: 0, eyes: 'open' }, values);
}

// Turns a cycle's four keys into eight: the second half mirrors the first with arms swapped.
function her_cycle_keys(keys, lean, hand_drop) {
    const result = [];
    for (const half of [0, 1]) {
        for (const key of keys) {
            let values = her_values({
                hip_y: key.hip_y, lean: lean, near_foot: key.near_foot, far_foot: key.far_foot,
                near_foot_angle: key.near_foot_angle, far_foot_angle: key.far_foot_angle,
                near_hand: [key.swing + 1, hand_drop], far_hand: [-key.swing + 1, hand_drop],
                hair: key.hair, hem: key.hem, scarf: 0.35 + key.hair,
            });
            if (half === 1) {
                values = mirror_her(values);
            }
            result.push(values);
        }
    }
    return result;
}

// Her home stance. Every landing, turn and stop settles into it, and idle breathes from it, so
// one move hands over to the next without a jump.
const her_standing = her_values({ hip_y: -43.6, near_foot: [4.5, 0], far_foot: [-4.5, 0], near_hand: [1.5, 25.5], far_hand: [-0.5, 25.5] });

// Her jump as one arc: take-off, knees tucked, the top (the middle drawing), falling, reaching
// for the ground. The game picks the drawing from how fast she is rising or falling.
const her_jump_keys = [
    { hip_y: -46, lean: 0.08, near_foot: [4, -2], far_foot: [-6, -1], near_foot_angle: -0.6, far_foot_angle: -0.8, near_hand: [11, 6], far_hand: [-5, 9], hair: -0.1 },
    { hip_y: -45, lean: 0.05, near_foot: [6, -12], far_foot: [-2, -14], near_foot_angle: -0.2, far_foot_angle: -0.3, near_hand: [12, 1], far_hand: [-8, 4], hair: -0.15 },
    { hip_y: -45.5, lean: 0.04, near_foot: [6, -9], far_foot: [-4, -11], near_foot_angle: -0.1, far_foot_angle: -0.3, near_hand: [12, 5], far_hand: [-9, 6], hair: 0.05, hem: -0.02 },
    { hip_y: -45, lean: 0.02, near_foot: [5, -4], far_foot: [-5, -6], far_foot_angle: -0.3, near_hand: [12, 10], far_hand: [-10, 8], hair: 0.2, hem: -0.05 },
    { hip_y: -44, lean: 0.02, near_foot: [4.5, -0.5], far_foot: [-4.5, -1], near_foot_angle: 0.1, far_foot_angle: -0.1, near_hand: [9, 16], far_hand: [-7, 14], hair: 0.25, hem: -0.06 },
].map(her_values);

// Her kneeling pose, breathing (0 = out, 1 = in).
function her_kneeling(breath) {
    return her_values({ hip_y: -23 - breath * 0.3, hip_x: -1, lean: 0.24 - breath * 0.01, near_foot: [13, 0], far_foot: [-19, 0], far_foot_angle: -1.3, near_hand: [11, 19.5 - breath * 0.3], far_hand: [15, 15.5 - breath * 0.3], hair: 0.1 - breath * 0.03, hem: 0.06 });
}

// Every animation as a list of poses. Loops get two drawings per key; one-off moves get
// in-betweens too, so nothing snaps from one pose to the next.
const her_animations = {
    idle: breathing(12 * breath_detail, function idle_pose(breath) {
        return her_pose(Object.assign({}, her_standing, { hip_y: -43.6 - breath * 0.4, near_hand: [1.5, 25.5 - breath * 0.4], far_hand: [-0.5, 25.5 - breath * 0.4], hair: breath * 0.04, scarf: 0.3 + breath * 0.06 }));
    }),
    // The walk is worked out drawing by drawing from how a real person walks (see gaits.js).
    walk: follow_through(gaits.her_walk_values(walk_drawings).map(function walk_pose(values) { return her_pose(her_values(values)); }), her_loose_parts, walk_drawings / 8, true),
    run: follow_through(in_between(her_cycle_keys(her_run_keys, 0.14, 17), cycle_steps, true).map(her_pose), her_loose_parts, cycle_steps, true),
    jump: follow_through(in_between(her_jump_keys, jump_steps, false).map(her_pose), her_loose_parts, jump_steps, false),
    // Landing starts from touch-down (the end of the jump, feet now on the floor), sinks into
    // the impact, then rises back to standing, so the jump hands over without a snap.
    land: follow_through(in_between([
        Object.assign({}, her_jump_keys[4], { hip_y: -43, near_foot: [4.5, 0], far_foot: [-4.5, 0], near_foot_angle: 0, far_foot_angle: 0 }),
        her_values({ hip_y: -33, lean: 0.16, near_foot: [4.5, 0], far_foot: [-4.5, 0], near_hand: [9, 18], far_hand: [-3, 20], hair: -0.1, hem: 0.06 }),
        her_values({ hip_y: -40, lean: 0.06, near_foot: [4.5, 0], far_foot: [-4.5, 0], near_hand: [3, 24], far_hand: [-1, 24], hair: 0.06, hem: 0.02 }),
        her_standing,
    ], big_move_steps, false).map(her_pose), her_loose_parts, big_move_steps, false),
    turn: follow_through(in_between([
        her_values({ hip_y: -41.5, lean: 0.1, near_foot: [4.5, 0], far_foot: [-6, -1.2], far_foot_angle: -0.4, near_hand: [5, 22], far_hand: [-3, 22], hair: 0.3, scarf: 0.6, hem: 0.05 }),
        her_values({ hip_y: -43, lean: 0.05, near_foot: [4.5, 0], far_foot: [-4.5, 0], near_hand: [2, 25], far_hand: [0, 25], hair: 0.12, scarf: 0.45, hem: 0.03 }),
        her_standing,
    ], move_steps, false).map(her_pose), her_loose_parts, move_steps, false),
    // Stopping from a run: the front foot brakes, the back foot steps up, the coat and hair
    // carry on forward a moment, then everything settles.
    stop: follow_through(in_between([
        her_values({ hip_y: -41, lean: -0.08, near_foot: [5, 0], far_foot: [-8, -1.5], far_foot_angle: -0.5, near_hand: [6, 22], far_hand: [-4, 23], hair: -0.15, scarf: 0.1, hem: -0.08 }),
        her_values({ hip_y: -42.8, lean: 0.03, near_foot: [5, 0], far_foot: [-3, -1.2], far_foot_angle: -0.2, near_hand: [3, 24.5], far_hand: [-1, 25], hair: 0.05, scarf: 0.35, hem: 0.03 }),
        her_standing,
    ], move_steps, false).map(her_pose), her_loose_parts, move_steps, false),
    // A skid: she reversed at speed and is still sliding the old way. Drawn facing the slide,
    // the front heel dug in, body braced back, hair and scarf thrown forward. The game picks the
    // drawing by speed: the first while sliding fast, the last just before she pivots.
    skid: follow_through(in_between([
        her_values({ hip_y: -39.5, lean: -0.16, near_foot: [10, 0], near_foot_angle: 0.35, far_foot: [-5, -1.5], far_foot_angle: -0.3, near_hand: [9, 16], far_hand: [12, 12], hair: -0.25, scarf: 0.0, hem: -0.1 }),
        her_values({ hip_y: -40.5, lean: -0.1, near_foot: [9, 0], near_foot_angle: 0.25, far_foot: [-3, -1], far_foot_angle: -0.2, near_hand: [7, 18], far_hand: [10, 15], hair: -0.18, scarf: 0.1, hem: -0.08 }),
        her_values({ hip_y: -41.8, lean: -0.04, near_foot: [7, 0], near_foot_angle: 0.1, far_foot: [-2, -0.5], near_hand: [4, 22], far_hand: [5, 21], hair: -0.08, scarf: 0.2, hem: -0.04 }),
    ], move_steps, false).map(her_pose), her_loose_parts, move_steps, false),
    // Getting down (played once), then breathing while kneeling (looped).
    // Played backwards, the getting-down drawings are also her getting up.
    kneel: in_between([
        her_standing,
        her_values({ hip_y: -35, lean: 0.12, near_foot: [10, -2], far_foot: [-10, -2], far_foot_angle: -0.5, near_hand: [8, 22], far_hand: [3, 23], hair: 0.05, hem: 0.04 }),
        her_kneeling(0),
    ], big_move_steps, false).map(her_pose).concat(breathing(4 * breath_detail, function kneeling_pose(breath) { return her_pose(her_kneeling(breath)); })),
    embrace: breathing(8 * breath_detail, function hold(breath) {
        return her_pose(her_values({ hip_y: -23 - breath * 0.4, hip_x: -1, lean: 0.2, near_foot: [13, 0], far_foot: [-19, 0], far_foot_angle: -1.3, near_hand: [11, 19], far_hand: [16, 14], head_tilt: 0.14, eyes: 'closed', hair: 0.05 }));
    }),
    sit: breathing(8 * breath_detail, function seated(breath) {
        return her_pose(her_values({ hip_y: -22 - breath * 0.3, lean: 0.05, near_foot: [21, 0], far_foot: [18, 0], near_hand: [15, 22], far_hand: [12, 22], hair: 0.02 * breath }));
    }),
};

// ---------- The dog ----------

// His markings, matched to the real Bodhi: a cream-white blaze on his chest and paws paler
// than his legs. Biscuit's look turns both grey-white.
const blaze_color = '#f6ead8';
const paw_color = '#f1dcc0';

// A point on the dog's body: `along` the spine (positive toward the chest) and `up` from it,
// measured from `base`. Using the body's own directions keeps shapes right when it rears up.
function body_point(base, axis, along, up) {
    const normal = [axis[1], -axis[0]];
    return add(base, add(scale(axis, along), scale(normal, up)));
}

// How the dog is built at each drawn age. Goldendoodle puppies have a big head, big paws,
// short legs, large ears for their size and a short plume of a tail; the adult drawings take
// over from about six months, drawn smaller while he is still growing (see dog_growth in
// prototype/web/js/levels.js).
//   head, ear, tail: how big each is compared with the adult.
//   leg_width: how thick the legs and paws are.  leg_bone: how long each leg bone is.
//   lower_body: how far the body sinks toward the paws when standing (0 = adult height).
const adult_build = { head: 1, ear: 1, tail: 1, leg_width: 1, leg_bone: 1, lower_body: 0 };
const puppy_build = { head: 1.34, ear: 1.15, tail: 0.6, leg_width: 1.1, leg_bone: 0.8, lower_body: 0.27 };
// Biscuit, the old dog next door: the grown build, carried a little lower, with a slightly
// smaller head and a shorter, heavier tail. His colours and smoother coat are in biscuit_look.
const old_dog_build = { head: 0.96, ear: 1.05, tail: 0.85, leg_width: 0.96, leg_bone: 1, lower_body: 0.05 };

// A puppy's body sits lower over its paws. Standing, the whole body (hips, shoulders and head
// together, so its angle doesn't change) comes down by `lower_body` of the hips' height. Sitting
// or lying, the hips are already on the floor, so nothing moves. The drop grows smoothly with
// the hips' height, so every hand-over between two drawings stays as smooth as the adult's.
function body_drop(pose, build) {
    const hip_height = -pose.pelvis[1];
    const upright = Math.min(1, Math.max(0, (hip_height - 12) / 15));
    return build.lower_body * upright * hip_height;
}

// How far the adult head (with its beard) reaches below its centre, in game units.
const head_reach_below = 13.4;

// Turns an adult pose into the same pose with this build's proportions. The head rides down
// with the shoulders, and a puppy's bigger head sits a little further back on its neck.
// Two limits keep the bigger head inside its frame, each a smooth function of the pose so
// hand-overs stay smooth:
//   - near the floor (sniffing, lying down) it rises until its chin stays above the floor;
//   - reaching far forward (sniffing) it comes back toward the body.
function built_dog_pose(pose, build) {
    if (build.lower_body === 0) {
        return pose;
    }
    const drop = body_drop(pose, build);
    // A head tipped down to sniff swings its beard lower, so it needs more room the more it tips.
    const reach_below = head_reach_below + 7 * Math.max(0, pose.head_angle || 0);
    const lowest_head_y = 1 - reach_below * build.head;  // 1 unit spare for the fluffy edge.
    const head_x = pose.head[0] - 1.5 - Math.max(0, pose.head[0] - 20) * 0.8;
    const head_y = Math.min(pose.head[1] + drop + 1, lowest_head_y);
    return Object.assign({}, pose, {
        pelvis: [pose.pelvis[0], pose.pelvis[1] + drop],
        withers: [pose.withers[0], pose.withers[1] + drop],
        head: [head_x, head_y],
    });
}

// Draws one frame of the dog as SVG, from a pose in game units (feet at 0, 0; facing right).
// `build` gives the proportions (adult_build or puppy_build).
function draw_dog(adult_pose, build) {
    build = build || adult_build;
    const pose = built_dog_pose(adult_pose, build);
    const pelvis = pose.pelvis;
    const withers = pose.withers;
    const axis = normalized(subtract(withers, pelvis));
    const spine_middle = middle(pelvis, withers);
    const arch = pose.arch || 0;
    const head = pose.head;
    // The fur is drawn in the body's own coordinates (origin mid-spine, turned with the spine),
    // so its curl texture, clumps and fluffy edge move with the body. Drawn in the frame's coordinates
    // the texture stayed put while the body moved, and the fur seemed to swim.
    const body_angle = Math.atan2(axis[1], axis[0]);
    const to_body = function to_body(point) {
        return to_pixels(rotate_point(subtract(point, spine_middle), -body_angle));
    };
    // The silhouette, from the rump, along the back and neck, round the chest and belly.
    const outline = [
        body_point(pelvis, axis, -11, 0),
        body_point(pelvis, axis, -8, 8.5),
        body_point(pelvis, axis, -1, 10.5),
        body_point(spine_middle, axis, 0, 10.5 + arch),
        body_point(withers, axis, 1, 11.5),
        add(head, [-7.5, -5]),
        add(head, [-1, 9]),
        body_point(withers, axis, 10.5, -2),
        body_point(withers, axis, 8, -10),      // A full, fluffy chest.
        body_point(withers, axis, 1, -12),
        body_point(spine_middle, axis, 0, -10.5 + (pose.tuck || 2) + arch * 0.4),
        body_point(pelvis, axis, 4, -9.5),
        body_point(pelvis, axis, -6, -9.5),
        body_point(pelvis, axis, -10.5, -5),
    ].map(to_body);

    // The blaze on his chest, inside the outline: from under the beard, down the front of the
    // chest, to between his front legs.
    const blaze = [
        add(head, [-1.5, 8.5]),
        body_point(withers, axis, 10.2, -2.5),
        body_point(withers, axis, 8, -10),
        body_point(withers, axis, 2.5, -11.6),
        body_point(withers, axis, 2.8, -4),
        body_point(withers, axis, 5.5, 2),
    ].map(to_body);

    // How far the hips are down toward sitting (0 standing, 1 sitting). Sitting, the back feet
    // lie flat on the floor from paw to hock.
    const crouch = Math.min(1, Math.max(0, (22 + pelvis[1]) / 8));
    // A leg: one furry tube from inside the body, through its joints, to the paw.
    //   front:  a front leg (shoulder, elbow, wrist, paw) rather than a back one (hip, stifle,
    //           hock, paw). A dog's elbow points back and its stifle (knee) forward.
    //   fold:   in the air, how far the wrist or hock folds the paw back (0 to 1).
    const leg = function draw_leg(top, paw, widths, color, front, fold) {
        // Bigger paws are lifted by as much as they grew, so they still stand on the floor.
        const paw_point = add(paw, [0, -1.2 - 8 * (build.leg_width - 1)]);
        const bone = 10.5 * build.leg_bone;
        let points;
        if (pose.folded || (front && pose.reaching)) {
            // Lying down (`folded`) the joints fold up into the body instead of poking down
            // through the floor; a front leg reaching up (the hug) bends like an arm.
            const joint = solve_joint(top, paw_point, bone, bone, pose.folded ? -1 : 1);
            points = [top, joint, paw_point];
        } else {
            // The short bone above the paw: nearly upright when the paw is down, folding back
            // in the air. The back one slopes more (the hock sits behind the paw), and lies flat
            // along the floor when sitting.
            const lowest_bone = front ? [-0.4, -3.6] : [-1.8, -5.6];
            const turned = rotate_point(scale(lowest_bone, build.leg_bone), (fold || 0) * (front ? 1.9 : 1.2) - (front ? 0 : crouch * 1.25));
            const wrist = add(paw_point, turned);
            const upper = (front ? 10.4 : 9.4) * build.leg_bone;
            const joint = solve_joint(top, wrist, upper, upper, front ? 1 : -1);
            points = [top, joint, wrist, paw_point];
            widths = [widths[0], widths[1], widths[2] * 0.95, widths[2]];
        }
        points = points.map(to_body);
        const paw_pixels = to_body(add(paw_point, [0.8, 0]));
        // Goldendoodle legs carry thick, shaggy fur ("furnishings") down to big round paws. Lying
        // down, the fur on the folded legs is pressed flat, and the knees tuck under the body.
        const flattened = pose.folded ? 0.88 : 1;
        const fluffy_widths = widths.map(function thicker(width) { return width * build.leg_width * flattened; });
        const paw_width = 58 * build.leg_width;
        const paw_height = 32 * build.leg_width;
        // The paw stays level with the floor, whatever angle the body is at.
        const level = ' transform="rotate(' + number_text(-degrees(body_angle)) + ' ' + number_text(paw_pixels[0]) + ' ' + number_text(paw_pixels[1]) + ')"';
        const tube = tube_path(points, fluffy_widths);
        const paw_shape = '<ellipse cx="' + number_text(paw_pixels[0]) + '" cy="' + number_text(paw_pixels[1]) + '" rx="' + number_text(paw_width) + '" ry="' + number_text(paw_height) + '"';
        // Close up, the legs were flat colour while the body was curly. The same ringlets now
        // cover the legs and paws, a shadow falls on the paw's underside, and three tufts of
        // fur part over the toes, so a paw reads as a paw rather than a round blob.
        const toes = [-0.45, 0, 0.45].map(function toe(across) {
            const x = paw_pixels[0] + across * paw_width * 0.9;
            const y = paw_pixels[1] - paw_height * 0.15;
            return 'M ' + number_text(x) + ',' + number_text(y - paw_height * 0.55) + ' C ' + number_text(x - 4) + ',' + number_text(y - paw_height * 0.1) + ' ' + number_text(x - 3) + ',' + number_text(y + paw_height * 0.35) + ' ' + number_text(x + 1) + ',' + number_text(y + paw_height * 0.6);
        }).join(' ');
        return '<path d="' + tube + '" fill="' + color + '"/>' +
            '<path d="' + tube + '" fill="url(#curls)"/>' +
            paw_shape + ' fill="' + paw_color + '"' + level + '/>' +
            paw_shape + ' fill="url(#curls)"' + level + '/>' +
            '<g' + level + '><ellipse cx="' + number_text(paw_pixels[0]) + '" cy="' + number_text(paw_pixels[1] + paw_height * 0.55) + '" rx="' + number_text(paw_width * 0.9) + '" ry="' + number_text(paw_height * 0.4) + '" fill="#5e3218" opacity="0.28"/>' +
            '<path d="' + toes + '" fill="none" stroke="#5e3218" stroke-width="3" stroke-linecap="round" opacity="0.4"/></g>';
    };
    const front_top_near = body_point(withers, axis, 2, -3);
    const front_top_far = body_point(withers, axis, 0, -3);
    const back_top_near = body_point(pelvis, axis, 1, -3);
    const back_top_far = body_point(pelvis, axis, -1, -3);

    // The tail: a fluffy plume from the rump, pointing back and up by `tail` radians.
    const tail_root = body_point(pelvis, axis, -8.5, 6);
    const tail_direction = rotate_point([-1, 0], -(pose.tail || 0));
    const tail_tip = add(tail_root, scale(tail_direction, 15 * build.tail));
    const tail_middle = add(middle(tail_root, tail_tip), [0, -(pose.tail_curl || 2) * build.tail]);
    const tail_points = [tail_root, tail_middle, tail_tip].map(to_body);
    // A goldendoodle's tail is a feathered plume: wide in the middle, still full at the tip.
    const tail_widths = [78, 104, 72].map(function plume(width) { return width * (0.7 + 0.3 * build.tail); });

    const fur = [
        '<g filter="url(#far_side)">',
        leg(front_top_far, pose.legs.far_front, [104, 88, 80], 'url(#coat)', true, (pose.leg_fold || {}).far_front),
        leg(back_top_far, pose.legs.far_back, [130, 84, 68], 'url(#coat_dark)', false, (pose.leg_fold || {}).far_back),
        '</g>',
        '<path d="' + tube_path(tail_points, tail_widths) + '" fill="url(#coat_light)"/>',
        '<path d="' + tube_path(tail_points, tail_widths) + '" fill="url(#curls)"/>',
        // Near legs go behind the body: the coat hides where they join, as on a real dog. A front
        // leg reaching up (the hug) is drawn over the body instead, so it reads as an arm.
        pose.reaching ? '' : leg(front_top_near, pose.legs.near_front, [108, 92, 82], 'url(#coat)', true, (pose.leg_fold || {}).near_front),
        leg(back_top_near, pose.legs.near_back, [134, 86, 70], 'url(#coat)', false, (pose.leg_fold || {}).near_back),  // Hock and foot stay slim: sitting, they lie on the floor.
        '<path d="' + smooth_closed_path(outline) + '" fill="url(#coat)"/>',
        '<path d="' + smooth_closed_path(outline) + '" fill="url(#curls)"/>',
        // His white blaze: the real Bodhi has a cream-white bib from his throat down his chest.
        '<path d="' + smooth_closed_path(blaze) + '" fill="' + blaze_color + '"/>',
        '<path d="' + smooth_closed_path(blaze) + '" fill="url(#curls)"/>',
        pose.reaching ? '<g transform="translate(0,8)" opacity="0.35">' + leg(front_top_near, pose.legs.near_front, [92, 74, 64], '#6b4320', true) + '</g>' + leg(front_top_near, pose.legs.near_front, [92, 74, 64], 'url(#coat_light)', true) : '',
    ];
    // Fur detail: a soft lighter band along the back where the light catches it. A wavy
    // goldendoodle coat isn't glossy, so this is wide and faint, not a shine.
    const back_line = [body_point(pelvis, axis, -5, 8), body_point(spine_middle, axis, 0, 9 + arch), body_point(withers, axis, 1, 9)].map(to_pixels);
    let head_markup = place_part(dog_parts.head, head, pose.head_angle || 0, build.head === 1 ? '' : ' scale(' + build.head + ')');
    const overlays = [];
    if (pose.eyes === 'closed') {
        overlays.push('<ellipse cx="44" cy="-22" rx="22" ry="24" fill="#f3d2a8"/><path d="M 26,-20 C 36,-8 54,-8 64,-20" fill="none" stroke="#3a2418" stroke-width="5" stroke-linecap="round"/>');
    }
    if (pose.mouth === 'open') {
        overlays.push('<path d="M 82,60 C 100,92 136,90 148,62 C 130,74 102,74 82,60 Z" fill="#5a2a22"/><path d="M 96,72 C 108,84 126,84 136,72" fill="#c96a62"/>');
    }
    if (overlays.length) {
        head_markup = head_markup.replace(/<\/g>$/, overlays.join('') + '</g>');
    }
    const ear_root = add(head, rotate_point(scale([-4.2, -3.6], build.head), pose.head_angle || 0));
    const ear_markup = place_part(dog_parts.ear, ear_root, (pose.head_angle || 0) + (pose.ear || 0.2), ' scale(' + Math.round(0.92 * build.ear * 1000) / 1000 + ')');
    const body_place = to_pixels(spine_middle);
    return '<g transform="translate(' + number_text(body_place[0]) + ',' + number_text(body_place[1]) + ') rotate(' + number_text(degrees(body_angle)) + ')"><g filter="url(#fluffy)"><g filter="url(#clumps)">' + fur.join('') + '</g></g></g>' +
        '<path d="M ' + back_line.map(function point_text(point) { return point.map(number_text).join(','); }).join(' L ') + '" fill="none" stroke="#fff3d8" stroke-width="12" stroke-linecap="round" opacity="0.18"/>' +
        head_markup + ear_markup;
}

// Builds a dog pose from a base standing pose plus changes.
function dog_pose(changes) {
    const standing = {
        pelvis: [-11, -27], withers: [11, -28], arch: 0, tuck: 2, head: [19, -39], head_angle: 0, ear: 0.22, tail: 0.45, tail_curl: 2.5,
        legs: { near_front: [13, 0], far_front: [11, 0], near_back: [-8, 0], far_back: [-11, 0] }, eyes: 'open', mouth: 'closed',
        leg_fold: { near_front: 0, far_front: 0, near_back: 0, far_back: 0 },
    };
    const result = Object.assign({}, standing, changes);
    result.legs = Object.assign({}, standing.legs, changes.legs || {});
    result.leg_fold = Object.assign({}, standing.leg_fold, changes.leg_fold || {});
    return result;
}

// The gallop (a "rotary" gallop): stretch out, front feet land, gather the back feet under a
// rounded spine, back feet land, push, fly. Paws in the air fold back at the wrist and hock. The spine extending and gathering is what makes a
// running dog look alive.
const dog_gallop_keys = [
    dog_pose({ pelvis: [-12.5, -28], withers: [12.5, -29], arch: -1.5, head: [22, -37], head_angle: 0.1, ear: 0.7, tail: 0.15,
        legs: { near_front: [22, -3], far_front: [20, -4], near_back: [-18.5, -3.5], far_back: [-19, -3] },
        leg_fold: { near_front: 0, far_front: 0, near_back: 0.2, far_back: 0.15 } }),
    dog_pose({ pelvis: [-11.5, -29.5], withers: [11.5, -27], arch: 0, head: [20, -37], head_angle: 0.06, ear: 0.6, tail: 0.2,
        legs: { near_front: [18, 0], far_front: [15, -3], near_back: [-12, -5], far_back: [-15, -4] },
        leg_fold: { near_front: 0, far_front: 0.2, near_back: 0.6, far_back: 0.5 } }),
    dog_pose({ pelvis: [-10, -29], withers: [10, -28], arch: 2.5, head: [18, -40], head_angle: -0.04, ear: 0.45, tail: 0.3,
        legs: { near_front: [8, 0], far_front: [12, 0], near_back: [-7, -6], far_back: [-9, -5] },
        leg_fold: { near_front: 0, far_front: 0, near_back: 0.7, far_back: 0.6 } }),
    dog_pose({ pelvis: [-8.5, -29.5], withers: [8.5, -29], arch: 4, head: [17, -40], head_angle: -0.06, ear: 0.5, tail: 0.25,
        legs: { near_front: [5, -5], far_front: [7, -6], near_back: [0, -3], far_back: [-2, -4] },
        leg_fold: { near_front: 0.6, far_front: 0.7, near_back: 0.3, far_back: 0.4 } }),
    dog_pose({ pelvis: [-10, -26.5], withers: [10, -29], arch: 2, head: [18, -40], head_angle: 0, ear: 0.65, tail: 0.15,
        legs: { near_front: [14, -6], far_front: [12, -5], near_back: [-4, 0], far_back: [-7, 0] },
        leg_fold: { near_front: 0.8, far_front: 0.7, near_back: 0, far_back: 0 } }),
    dog_pose({ pelvis: [-11.5, -27.5], withers: [11.5, -29.5], arch: 0, head: [21, -39], head_angle: 0.06, ear: 0.7, tail: 0.1,
        legs: { near_front: [20, -4], far_front: [18, -5], near_back: [-14, 0], far_back: [-18, -1] },
        leg_fold: { near_front: 0.4, far_front: 0.5, near_back: 0, far_back: 0.1 } }),
];

// The dog's home stance, which every landing, turn and stop settles into.
const dog_standing = dog_pose({});

// A tail wag over one loop (turn from 0 to 1): it starts at `rest`, still, swings to `high`
// halfway and comes back. Starting still matters: the move before a loop (sitting down, a
// landing) slows to a stop at that same tail angle, and a wag that set off at full speed from
// there would flick.
function wag(turn, rest, high) {
    return rest + (high - rest) * (1 - Math.cos(2 * Math.PI * turn)) / 2;
}

// The dog sitting: hips down, front legs straight, the tail sweeping the floor.
function dog_sitting(breath, turn) {
    return dog_pose({ pelvis: [-7, -12], withers: [8, -28 - breath * 0.3], arch: -1, head: [16, -43 - breath * 0.3], ear: 0.22 + breath * 0.02, tail: wag(turn, -0.2, 0.1),
        legs: { near_front: [12, 0], far_front: [10, 0], near_back: [1, 0], far_back: [-2, 0] } });
}

// The dog up on its back legs, front paws over her shoulders, eyes closed, tail going.
function dog_hugging(breath, turn) {
    return dog_pose({ pelvis: [-5, -14], withers: [2, -36 - breath * 0.4], arch: 0, tuck: 0, head: [8, -50 - breath * 0.4], head_angle: 0.35, ear: 0.25, tail: wag(turn, 0.3, 0.6),
        eyes: 'closed', reaching: true, legs: { near_front: [19, -47], far_front: [16, -45], near_back: [-4, 0], far_back: [-7, 0] } });
}

const dog_animations = {
    // The wag swings either side of the standing tail angle, so idle starts exactly where every
    // landing, turn and stop ends.
    idle: breathing(8 * breath_detail, function idle_pose(breath, turn) {
        return dog_pose({ pelvis: [-11, -27 - breath * 0.3], withers: [11, -28 - breath * 0.3], head: [19, -39 - breath * 0.15], tail: wag(turn, 0.45, 0.8), ear: 0.22 + breath * 0.03 });
    }),
    // The walk and trot are worked out drawing by drawing from how a real dog moves (see gaits.js).
    walk: follow_through(gaits.dog_gait_values(gaits.dog_walk, walk_drawings).map(dog_pose), dog_loose_parts, walk_drawings / 8, true),
    trot: follow_through(gaits.dog_gait_values(gaits.dog_trot, walk_drawings).map(dog_pose), dog_loose_parts, walk_drawings / 8, true),
    gallop: follow_through(in_between(dog_gallop_keys, cycle_steps, true), dog_loose_parts, cycle_steps, true),
    // One arc: take-off, rising, the top (the middle drawing), falling, reaching for the ground.
    jump: follow_through(in_between([
        dog_pose({ pelvis: [-11, -26], withers: [11, -33], arch: 0, head: [20, -44], head_angle: -0.15, ear: 0.1, tail: 0.6,
            legs: { near_front: [17, -12], far_front: [15, -11], near_back: [-19, -2], far_back: [-21, -1] } }),
        dog_pose({ pelvis: [-11, -28], withers: [11, -31], arch: 1.5, head: [20, -42], head_angle: -0.05, ear: 0.0, tail: 0.5,
            legs: { near_front: [20, -9], far_front: [18, -9], near_back: [-8, -9], far_back: [-10, -8] } }),
        dog_pose({ pelvis: [-11, -29], withers: [11, -29.5], arch: 1, head: [21, -40], head_angle: 0.02, ear: 0.3, tail: 0.45,
            legs: { near_front: [20, -7], far_front: [18, -7], near_back: [-11, -8], far_back: [-12, -7] } }),
        dog_pose({ pelvis: [-11, -30], withers: [11, -27], arch: 0, head: [21, -37], head_angle: 0.1, ear: 0.55, tail: 0.35,
            legs: { near_front: [19, -5], far_front: [17, -6], near_back: [-15, -6], far_back: [-13, -7] } }),
        dog_pose({ pelvis: [-11, -29], withers: [11, -26], arch: 0, head: [21, -37], head_angle: 0.08, ear: 0.65, tail: 0.3,
            legs: { near_front: [16, -1], far_front: [14, -2], near_back: [-12, -4], far_back: [-10, -5] } }),
    ], jump_steps, false), dog_loose_parts, jump_steps, false),
    // Landing starts from touch-down (front paws first), sinks into the impact, then stands.
    land: follow_through(in_between([
        dog_pose({ pelvis: [-11, -28], withers: [11, -25.5], head: [21, -37], head_angle: 0.08, ear: 0.6, tail: 0.3,
            legs: { near_front: [15.5, 0], far_front: [13.5, -0.8], near_back: [-11.5, -3], far_back: [-10, -3.5] } }),
        dog_pose({ pelvis: [-10.5, -22], withers: [10.5, -21], arch: -0.5, head: [19, -32], head_angle: 0.12, ear: 0.75, tail: 0.3 }),
        dog_pose({ pelvis: [-11, -25.5], withers: [11, -26.5], head: [19, -37], head_angle: 0.04, ear: 0.45, tail: 0.4 }),
        dog_standing,
    ], big_move_steps, false), dog_loose_parts, big_move_steps, false),
    turn: follow_through(in_between([
        dog_pose({ pelvis: [-8, -27], withers: [8, -28], arch: 2, head: [16, -38], head_angle: -0.12, ear: 0.5, tail: 0.7,
            legs: { near_front: [9, -1], far_front: [7, -1], near_back: [-5, -1], far_back: [-7, -1] } }),
        dog_pose({ pelvis: [-10, -27], withers: [10, -28], arch: 0.5, head: [18, -39], head_angle: -0.04, ear: 0.32, tail: 0.6 }),
        dog_standing,
    ], move_steps, false), dog_loose_parts, move_steps, false),
    // A skid, as hers: drawn facing the slide, front paws braced, hips sunk, ears flung forward.
    skid: follow_through(in_between([
        dog_pose({ pelvis: [-12.5, -23.5], withers: [9, -25.5], arch: -1.5, head: [17, -35], head_angle: 0.12, ear: -0.25, tail: 0.25,
            legs: { near_front: [21, 0], far_front: [19, 0], near_back: [-5, 0], far_back: [-7, 0] } }),
        dog_pose({ pelvis: [-12, -25], withers: [9.5, -26.5], arch: -1, head: [17.5, -36.5], head_angle: 0.08, ear: -0.1, tail: 0.3,
            legs: { near_front: [19, 0], far_front: [17, 0], near_back: [-6, 0], far_back: [-8, 0] } }),
        dog_pose({ pelvis: [-11.5, -26.3], withers: [10.5, -27.5], head: [18.5, -38], head_angle: 0.03, ear: 0.05, tail: 0.38,
            legs: { near_front: [16, 0], far_front: [14, 0], near_back: [-7, 0], far_back: [-9.5, 0] } }),
    ], move_steps, false), dog_loose_parts, move_steps, false),
    // Stopping from a run: front paws brace, the hips sink back, the ears swing forward.
    stop: follow_through(in_between([
        dog_pose({ pelvis: [-12, -25], withers: [10, -26.5], arch: -1, head: [20, -36], head_angle: 0.08, ear: 0.7, tail: 0.3,
            legs: { near_back: [-6, -1.5], far_back: [-8, -1] } }),
        dog_pose({ pelvis: [-11, -26.5], withers: [11, -27.8], head: [19, -38.5], ear: 0.4, tail: 0.4, legs: { far_back: [-10, -0.8] } }),
        dog_standing,
    ], move_steps, false), dog_loose_parts, move_steps, false),
    // Sitting down (played once), then sitting and breathing (looped).
    // Played backwards, the getting-down drawings are also the dog getting up.
    sit: in_between([
        dog_standing,
        dog_pose({ pelvis: [-9, -18], withers: [10, -28], head: [18, -41], ear: 0.25, tail: 0.1, legs: { near_back: [-3, -1.5], far_back: [-6, -1.5] } }),
        dog_sitting(0, 0),
    ], big_move_steps, false).concat(breathing(4 * breath_detail, dog_sitting)),
    sniff: breathing(8 * breath_detail, function nose(breath, turn) {
        return dog_pose({ pelvis: [-11, -27.5], withers: [11, -26], head: [25 + 0.5 * Math.sin(4 * Math.PI * turn), -16 + breath * 1.2], head_angle: 0.42, ear: 0.0, tail: 0.55 + 0.15 * Math.sin(4 * Math.PI * turn),
            legs: { near_front: [14, 0], far_front: [12, 0] } });
    }),
    bark: follow_through(in_between([
        dog_pose({ pelvis: [-11, -26], withers: [11, -26.5], head: [17, -36], head_angle: 0.1, ear: 0.3, tail: 0.5 }),
        dog_pose({ pelvis: [-11, -27.5], withers: [11, -29.5], head: [21, -43], head_angle: -0.3, ear: 0.0, tail: 0.65, mouth: 'open' }),
        dog_pose({ pelvis: [-11, -27], withers: [11, -28.5], head: [20, -41], head_angle: -0.15, ear: 0.15, tail: 0.55 }),
    ], move_steps, false), dog_loose_parts, move_steps, false),
    sleep: breathing(6 * breath_detail, function breathe(breath) {
        // The long ear lies back along the floor, as a sleeping dog's does, and the bearded chin
        // rests just above it.
        return dog_pose({ pelvis: [-10, -10 - breath * 0.4], withers: [10, -11 - breath * 0.4], arch: -2, tuck: 0, head: [21, -12.5], head_angle: 0.15, ear: 0.95, tail: -0.15, tail_curl: -1,
            eyes: 'closed', folded: true, legs: { near_front: [22, -2], far_front: [20, -2], near_back: [-1, -1], far_back: [-4, -1] } });
    }),
    lean: breathing(6 * breath_detail, function nuzzle(breath, turn) {
        return dog_pose({ pelvis: [-11, -27], withers: [11, -27.5], head: [21, -35 + breath * 0.4], head_angle: -0.2, ear: 0.3, tail: wag(turn, 0.55, 0.85), eyes: 'closed' });
    }),
    // Rising onto the back legs into the hug (played once), then holding on and breathing
    // (looped). Played backwards, the getting-up drawings bring the dog back down.
    embrace: in_between([
        dog_standing,
        dog_pose({ pelvis: [-8, -20], withers: [5, -33], arch: 0.5, tuck: 1, head: [12, -46], head_angle: 0.22, ear: 0.3, tail: 0.5, reaching: true,
            legs: { near_front: [17, -30], far_front: [15, -28], near_back: [-5, 0], far_back: [-8, 0] } }),
        dog_hugging(0, 0),
    ], big_move_steps, false).concat(breathing(6 * breath_detail, dog_hugging)),
};

// ---------- Writing the files ----------

// The cell each frame is drawn in, in game units, and where the feet sit inside it.
const cells = {
    her: { width: 64, height: 106, feet_x: 30, feet_y: 99 },
    bodhi: { width: 90, height: 84, feet_x: 42, feet_y: 76 },
    bodhi_puppy: { width: 90, height: 84, feet_x: 42, feet_y: 76 },
    biscuit: { width: 90, height: 84, feet_x: 42, feet_y: 76 },
};

// Which build each dog drawing set uses. Both sets share the same poses and timing.
const dog_builds = { bodhi: adult_build, bodhi_puppy: puppy_build, biscuit: old_dog_build };

// How far each walking cycle travels, in game units (see cycle_length_of). The gallop's planted
// paws move back about 10 units per key over its 6 keys. In-betweens don't change the distance.
const cycle_lengths = {
    her: { walk: gaits.her_walk.cycle_length, run: cycle_length_of(her_run_keys) },
    bodhi: { walk: gaits.dog_walk.cycle_length, trot: gaits.dog_trot.cycle_length, gallop: 60 },
};
cycle_lengths.bodhi_puppy = cycle_lengths.bodhi;  // The puppy's paws travel the same paths.
cycle_lengths.biscuit = cycle_lengths.bodhi;

// Points the game draws things at, per frame: what she holds goes in her near hand; what the dog
// carries goes in its mouth; its blink goes over its eye (with the head's angle).
// Also where the feet are, so the game can start a stride from the drawing whose feet are
// closest to the ones just shown.
function frame_points(character, pose) {
    if (character === 'her') {
        return {
            hand: [pose.near_hand[0], pose.near_hand[1]].map(round_tenth),
            feet: [pose.near_foot.map(round_tenth), pose.far_foot.map(round_tenth)],
        };
    }
    // The puppy's bigger head moves its mouth and eye outward; the eye's size goes last, so its
    // blink covers it.
    const build = dog_builds[character];
    const built = built_dog_pose(pose, build);
    const angle = built.head_angle || 0;
    return {
        mouth: add(built.head, rotate_point(scale([11.5, 6.5], build.head), angle)).map(round_tenth),
        eye: add(built.head, rotate_point(scale([4.4, -2.2], build.head), angle)).map(round_tenth).concat([round_tenth(angle), build.head]),
        feet: [built.legs.near_front, built.legs.far_front, built.legs.near_back, built.legs.far_back].map(function rounded(point) { return point.map(round_tenth); }),
    };
}

// The points compared when one move hands over to the next.
function key_points(character, pose) {
    if (character === 'her') {
        return [pose.hip, pose.shoulder, pose.near_foot, pose.far_foot, pose.near_hand, pose.far_hand];
    }
    const built = built_dog_pose(pose, dog_builds[character]);
    return [built.pelvis, built.withers, built.head, built.legs.near_front, built.legs.far_front, built.legs.near_back, built.legs.far_back];
}

// How far the body jumps when the game goes from one drawing straight to another: the largest
// distance any key point moves, in game units. Under about 2.5 reads as continuous motion.
function hand_over_gap(character, from_pose, to_pose) {
    const from_points = key_points(character, from_pose);
    const to_points = key_points(character, to_pose);
    let largest = 0;
    for (let index = 0; index < from_points.length; index++) {
        largest = Math.max(largest, length_of(subtract(from_points[index], to_points[index])));
    }
    return largest;
}

// The hand-overs the game makes from one animation straight into another: [from animation,
// from drawing, to animation, to drawing]. A drawing of -1 means the last one.
// Getting down (kneeling, sitting, rising into the hug) is three keys, so its drawings are the
// ones in between plus the last key; the breathing loop starts right after them.
const intro_drawings = 2 * big_move_steps + 1;
const hand_overs = {
    her: [
        ['land', -1, 'idle', 0], ['turn', -1, 'idle', 0], ['stop', -1, 'idle', 0], ['jump', -1, 'land', 0],
        ['idle', 0, 'kneel', 0], ['kneel', intro_drawings - 1, 'kneel', intro_drawings], ['kneel', intro_drawings, 'embrace', 0],
        ['embrace', 0, 'kneel', intro_drawings], ['kneel', 0, 'idle', 0],
    ],
    bodhi: [
        ['land', -1, 'idle', 0], ['turn', -1, 'idle', 0], ['stop', -1, 'idle', 0], ['jump', -1, 'land', 0],
        ['idle', 0, 'sit', 0], ['sit', intro_drawings - 1, 'sit', intro_drawings], ['sit', 0, 'idle', 0],
        ['idle', 0, 'embrace', 0], ['embrace', intro_drawings - 1, 'embrace', intro_drawings], ['embrace', 0, 'idle', 0],
    ],
};
hand_overs.bodhi_puppy = hand_overs.bodhi;  // Same moves, checked again with puppy proportions.
hand_overs.biscuit = hand_overs.bodhi;

// Prints every hand-over's gap, marking any big enough to see as a jump.
function report_hand_overs() {
    const animations = { her: her_animations, bodhi: dog_animations, bodhi_puppy: dog_animations, biscuit: dog_animations };
    const lines = [];
    let worst = 0;
    for (const [character, pairs] of Object.entries(hand_overs)) {
        for (const [from_name, from_index, to_name, to_index] of pairs) {
            const from_frames = animations[character][from_name];
            const from_pose = from_frames[from_index < 0 ? from_frames.length + from_index : from_index];
            const gap = hand_over_gap(character, from_pose, animations[character][to_name][to_index]);
            worst = Math.max(worst, gap);
            lines.push('  ' + (gap > largest_smooth_gap ? 'JUMP ' : 'ok   ') + character + ' ' + from_name + ' ' + from_index + ' -> ' + to_name + ' ' + to_index + ': ' + gap.toFixed(1) + ' units');
        }
    }
    console.log('hand-overs (largest move of any key point between two drawings):\n' + lines.join('\n'));
    return worst;
}

// Rounds to one decimal place, keeping the atlas file short.
function round_tenth(value) {
    return Math.round(value * 10) / 10;
}

// Frames per row in a sheet. Rows keep the canvas the game paints each sheet into narrow
// enough for phones, which refuse very wide canvases.
const sheet_columns = 8;

// Writes one SVG file per animation: frames in rows, left to right, each an editable drawing.
function write_animation(character, name, frames, definitions, draw) {
    const cell = cells[character];
    const width = cell.width * units_to_pixels;
    const height = cell.height * units_to_pixels;
    const columns = Math.min(sheet_columns, frames.length);
    const rows = Math.ceil(frames.length / columns);
    const pieces = frames.map(function frame_group(pose, index) {
        const feet = [(index % columns) * width + cell.feet_x * units_to_pixels, Math.floor(index / columns) * height + cell.feet_y * units_to_pixels];
        return '  <g id="frame_' + index + '" transform="translate(' + feet[0] + ',' + feet[1] + ')">' + draw(pose) + '</g>';
    });
    const svg = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        '<!-- Purpose: ' + character + ', "' + name + '" animation, ' + frames.length + ' frames in rows of ' + columns + ', each ' + width + ' x ' + height + '.',
        '     Generated by art/tools/draw_frames.js; any frame can be redrawn by hand afterwards. Feet sit at',
        '     (' + cell.feet_x * units_to_pixels + ', ' + cell.feet_y * units_to_pixels + ') inside each frame. -->',
        '<svg xmlns="http://www.w3.org/2000/svg" width="' + width * columns + '" height="' + height * rows + '" viewBox="0 0 ' + width * columns + ' ' + height * rows + '">',
        '<defs>' + definitions + shade_filter + '</defs>',
    ].concat(pieces, ['</svg>']).join('\n');
    const file_name = character + '_' + name + '.svg';
    fs.writeFileSync(path.join(output_folder, file_name), svg);
    const entry = { sheet: 'art/animations/' + file_name, frames: frames.length, columns: columns };
    if (cycle_lengths[character][name]) {
        entry.cycle_length = cycle_lengths[character][name];
    }
    entry.points = frames.map(function points_of(pose) { return frame_points(character, pose); });
    return entry;
}

// The puppy's coat colours. The real Bodhi was a deep red-apricot as a puppy and is lightening
// as he grows, as goldendoodles do, so the puppy is a deeper red than the adult. Each adult colour in the painted sheet is swapped for its
// puppy shade; a colour missing from the sheet stops the tool, so a repaint can't silently
// leave the puppy the adult's colour.
const puppy_coat_colors = {
    '#eebf8a': '#e6a870', '#d4925a': '#c67b40', '#a1612f': '#8e4d22',
    '#c07a44': '#b0662f', '#7c4422': '#6a3519',
    '#f6d3a6': '#efbf8a', '#dc9e64': '#cf8547',
    '#b8692f': '#a65824', '#6e3518': '#5c2a12',
};
function puppy_definitions(definitions) {
    let result = definitions;
    for (const [adult_color, puppy_color] of Object.entries(puppy_coat_colors)) {
        if (result.indexOf(adult_color) === -1) {
            throw new Error('puppy colours: ' + adult_color + ' is no longer in bodhi.svg; update puppy_coat_colors');
        }
        result = result.split(adult_color).join(puppy_color);
    }
    return result;
}

// Biscuit's look: grey and white, the muzzle gone white with age, a blue collar, and a
// smoother coat (less fur noise at the edge, fainter curls), so he reads as a different and
// older dog at a glance. Used on the sheet's definitions and on every drawn frame.
const biscuit_changes = [
    // Coat, light coat, shadowed coat and ears.
    ['#eebf8a', '#e4e0d8'], ['#d4925a', '#c2bdb3'], ['#a1612f', '#8e887d'],
    ['#c07a44', '#aaa499'], ['#7c4422', '#6d675d'],
    ['#f6d3a6', '#f1efea'], ['#dc9e64', '#cfcac1'],
    ['#b8692f', '#857d70'], ['#6e3518', '#4f4940'],
    // Muzzle, beard, moustache, brow, curl shading, the closed-eye lid.
    ['#f7e7d0', '#ece9e2'], ['#efd9bb', '#d3cfc7'], ['#e0c3a0', '#c4bfb6'], ['#fbf0e0', '#f2f0ea'], ['#a35f2c', '#8a8478'], ['#7e4421', '#6f6a62'], ['#5e3218', '#5a554e'], ['#f3d2a8', '#e8e5de'],
    // A blue collar instead of Bodhi's red.
    ['#9a2f2a', '#2f4f8a'], ['#c45a50', '#5a78b8'],
    // The lower eyelid's warm edge.
    ['#c4895a', '#a8a399'],
    // His chest blaze and paws, grey-white instead of Bodhi's cream.
    ['#f6ead8', '#eeece6'], ['#f1dcc0', '#dcd8d0'],
    // A smoother coat.
    ['scale="22"', 'scale="9"'], ['opacity="0.31"', 'opacity="0.12"'],
    // Shallower clumps, lit in plain grey rather than the warm red-brown of Bodhi's coat.
    ['surfaceScale="13"', 'surfaceScale="6"'], ['intercept="0.71"', 'intercept="0.68"'], ['intercept="0.65"', 'intercept="0.68"'],
];
function biscuit_look(markup) {
    let result = markup;
    for (const [from, to] of biscuit_changes) {
        result = result.split(from).join(to);
    }
    return result;
}
// Every change must still find something to change, or a repaint of the art could leave him
// half golden without anyone noticing. Checked once, against the sheet and one drawn frame.
function check_biscuit_look(definitions) {
    const sample = definitions + draw_dog(dog_animations.idle[0], old_dog_build) + draw_dog(dog_animations.sleep[0], old_dog_build);
    for (const [from] of biscuit_changes) {
        if (sample.indexOf(from) === -1) {
            throw new Error('biscuit_look: "' + from + '" is no longer in the art; update biscuit_changes');
        }
    }
}

// The largest hand-over gap allowed, in game units. Bigger shows on screen as a jump.
const largest_smooth_gap = 2.5;

function main() {
    // Like build_atlas.js refusing parts that overlap: frames whose hand-overs would visibly
    // jump are not written, so a pose change can't quietly bring the jumping back.
    if (report_hand_overs() > largest_smooth_gap) {
        console.log('REFUSED: a hand-over above is bigger than ' + largest_smooth_gap + ' units. Change the poses so the two drawings meet, then run again.');
        process.exit(1);
    }
    fs.mkdirSync(output_folder, { recursive: true });
    // A lighter grain for whole frames (the painted parts already carry their own).
    const her_definitions = extract_definitions(her_sheet) + '<linearGradient id="sleeve_lit" x1="1" y1="0" x2="0" y2="0"><stop offset="0" stop-color="#5aa59f"/><stop offset="1" stop-color="#2c625f"/></linearGradient>' + '<filter id="grain_soft" x="-5%" y="-5%" width="110%" height="110%"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="1" seed="9" result="noise"/><feColorMatrix in="noise" type="saturate" values="0" result="mono"/><feComponentTransfer in="mono" result="faint"><feFuncA type="table" tableValues="0 0.06"/></feComponentTransfer><feComposite in="faint" in2="SourceGraphic" operator="in" result="grain"/><feBlend in="grain" in2="SourceGraphic" mode="multiply"/></filter>';
    const dog_definitions = extract_definitions(dog_sheet);
    check_biscuit_look(dog_definitions);
    const atlas = { her: { cell: cells.her, animations: {} }, bodhi: { cell: cells.bodhi, animations: {} }, bodhi_puppy: { cell: cells.bodhi_puppy, animations: {} }, biscuit: { cell: cells.biscuit, animations: {} } };
    for (const [name, frames] of Object.entries(her_animations)) {
        atlas.her.animations[name] = write_animation('her', name, frames, her_definitions, draw_her);
    }
    for (const [name, frames] of Object.entries(dog_animations)) {
        atlas.bodhi.animations[name] = write_animation('bodhi', name, frames, dog_definitions, function adult(pose) { return draw_dog(pose, adult_build); });
        atlas.bodhi_puppy.animations[name] = write_animation('bodhi_puppy', name, frames, puppy_definitions(dog_definitions), function puppy(pose) { return draw_dog(pose, puppy_build); });
        atlas.biscuit.animations[name] = write_animation('biscuit', name, frames, biscuit_look(dog_definitions), function old_dog(pose) { return biscuit_look(draw_dog(pose, old_dog_build)); });
    }
    const header = [
        '// Purpose: where each animation frame is: one sheet per animation, frames in rows.',
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

// Run as a command, it draws everything. Loaded by a test (require), it only hands over its
// pieces, so the test can check them without rewriting the art.
if (require.main === module) {
    main();
} else {
    module.exports = {
        her_animations, dog_animations, cells, intro_drawings, follow_through, wag, cycle_steps, move_steps, jump_steps, big_move_steps, breath_detail, cycle_lengths, dog_builds, built_dog_pose, key_points,
        puppy_definitions, biscuit_look, check_biscuit_look, extract_definitions, dog_sheet, hand_over_gap, largest_smooth_gap,
    };
}
