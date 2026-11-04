// Purpose: makes movement look physical instead of stiff. Springs that lag and settle (ears,
// tail, hair, scarf), turning that rotates through instead of flipping, a body that leans into
// speed, joints that bend correctly, and poses that blend into each other instead of snapping.
// game.js moves characters; this file decides how their bodies respond to that movement.
'use strict';

// Moves a value toward a target by at most `largest_step`. The basis of acceleration:
// speed changes at a steady rate instead of jumping.
function move_toward(value, target, largest_step) {
    if (Math.abs(target - value) <= largest_step) {
        return target;
    }
    return value + Math.sign(target - value) * largest_step;
}

// Clamps a value between a lowest and highest allowed value.
function clamp_between(value, lowest, highest) {
    return Math.max(lowest, Math.min(highest, value));
}

// A damped spring, the core of natural motion: the value is pulled toward a target,
// overshoots a little, and settles. `stiffness` is how hard it pulls; `damping` how fast
// the wobble dies. Soft ears: low stiffness, low damping. A head: high both.
function make_spring(starting_value) {
    return { value: starting_value, velocity: 0 };
}
function update_spring(spring, target, stiffness, damping, delta_seconds) {
    // Small sub-steps keep stiff springs stable even if a frame runs long.
    const steps = Math.max(1, Math.ceil(delta_seconds / (1 / 120)));
    const step_seconds = delta_seconds / steps;
    for (let step = 0; step < steps; step++) {
        const pull = (target - spring.value) * stiffness - spring.velocity * damping;
        spring.velocity += pull * step_seconds;
        spring.value += spring.velocity * step_seconds;
    }
    return spring.value;
}

// Follows a moving target smoothly without overshooting (a "critically damped" spring).
// Used for the camera. `state` keeps the speed between calls; `smooth_seconds` is roughly
// how long it takes to catch up.
function smooth_follow(current, target, state, key, smooth_seconds, delta_seconds) {
    const omega = 2 / Math.max(0.0001, smooth_seconds);
    const x = omega * delta_seconds;
    const decay = 1 / (1 + x + 0.48 * x * x + 0.235 * x * x * x);
    const change = current - target;
    const velocity = state[key] || 0;
    const temporary = (velocity + omega * change) * delta_seconds;
    state[key] = (velocity - omega * temporary) * decay;
    return target + (change + temporary) * decay;
}

// Finds where a knee or elbow goes, given the two ends of the limb and the two bone lengths
// ("two-bone inverse kinematics"). `bend` is +1 to bend backward, -1 to bend forward.
// If the ends are too far apart, the limb is drawn straight instead of breaking.
function solve_joint(start, end, upper_length, lower_length, bend) {
    const delta_x = end[0] - start[0];
    const delta_y = end[1] - start[1];
    const full_distance = Math.hypot(delta_x, delta_y) || 0.0001;
    const distance = Math.min(full_distance, upper_length + lower_length - 0.01);
    const along = (upper_length * upper_length - lower_length * lower_length + distance * distance) / (2 * distance);
    const height = Math.sqrt(Math.max(0, upper_length * upper_length - along * along));
    const unit_x = delta_x / full_distance;
    const unit_y = delta_y / full_distance;
    // The perpendicular of (unit_x, unit_y) is (-unit_y, unit_x).
    return [start[0] + unit_x * along - unit_y * height * bend, start[1] + unit_y * along + unit_x * height * bend];
}

// Eases a value from 0 to 1 so motion starts and ends gently ("ease in and out").
function ease_in_out(progress) {
    const t = clamp_between(progress, 0, 1);
    return t * t * (3 - 2 * t);
}

// Eases out with a small bounce at the end, like something heavy landing.
function ease_out_bounce(progress) {
    const t = clamp_between(progress, 0, 1);
    if (t < 0.7) {
        return (t / 0.7) * (t / 0.7);
    }
    const settle = (t - 0.7) / 0.3;
    return 1 - Math.sin(settle * Math.PI) * 0.08 * (1 - settle);
}

// Updates every "secondary motion" of a character once per physics step: things that react
// to the body's movement rather than to the player directly.
function update_rig(character, delta_seconds, time_seconds) {
    const is_dog = character.kind === 'dog';
    if (character.facing_blend === undefined) {
        character.facing_blend = character.facing;
        character.lean = make_spring(0);
        character.ear = make_spring(0.2);
        character.tail = make_spring(-0.6);
        character.hair = make_spring(0);
        character.scarf = make_spring(0);
        character.hem = make_spring(0);
        character.head_tilt = make_spring(0);
        character.smoothed_acceleration = 0;
        character.last_velocity_x = character.velocity_x;
        character.last_velocity_y = character.velocity_y;
    }

    // Turning rotates through in about a sixth of a second, instead of flipping in one frame.
    character.facing_blend = move_toward(character.facing_blend, character.facing, delta_seconds * (is_dog ? 15 : 11));

    // How quickly speed is changing, smoothed so one bumpy frame doesn't jerk the body.
    const raw_acceleration = (character.velocity_x - character.last_velocity_x) / Math.max(delta_seconds, 0.0001);
    character.smoothed_acceleration += (raw_acceleration - character.smoothed_acceleration) * Math.min(1, delta_seconds * 10);
    const vertical_change = character.velocity_y - character.last_velocity_y;
    character.last_velocity_x = character.velocity_x;
    character.last_velocity_y = character.velocity_y;
    const speed = Math.abs(character.velocity_x);
    const moving_forward = Math.sign(character.velocity_x) || character.facing;

    if (is_dog) {
        // A dog pitches forward when braking and lifts its chest when pushing off.
        const pitch = clamp_between(-character.smoothed_acceleration * 0.00007, -0.12, 0.12);
        update_spring(character.lean, character.on_ground ? pitch : clamp_between(character.velocity_y * 0.0003, -0.25, 0.25) * moving_forward, 140, 14, delta_seconds);
        // Floppy ears: blown back by speed, flung up by landings and jumps, lifted by a call.
        const ear_target = 0.15 + (speed / 300) * 0.55 + (character.fear || 0) * 0.3 - (character.ear_lift || 0) * 0.6;
        character.ear.velocity -= vertical_change * 0.012;
        update_spring(character.ear, ear_target, 90, 5, delta_seconds);
        // The tail drags a little behind sudden moves.
        const tail_target = -0.75 + (character.fear || 0) * 1.35 + (speed / 300) * 0.35;
        character.tail.velocity += character.smoothed_acceleration * moving_forward * -0.00004;
        update_spring(character.tail, tail_target, 60, 6, delta_seconds);
    } else {
        // She leans into speed and into speeding up, and back when she stops.
        const lean_target = character.on_ground
            ? clamp_between(character.velocity_x * 0.00028 + character.smoothed_acceleration * 0.00007, -0.13, 0.13)
            : clamp_between(character.velocity_x * 0.0002, -0.08, 0.08);
        update_spring(character.lean, character.kneeling ? 0 : lean_target, 120, 13, delta_seconds);
        // Hair, scarf and coat hem have weight: they swing behind her and settle.
        const flow = -speed / 240;
        character.hair.velocity -= vertical_change * 0.004;
        update_spring(character.hair, flow * 0.5 + character.velocity_y * 0.0006, 70, 7, delta_seconds);
        update_spring(character.scarf, speed / 240 + Math.sin(time_seconds * 2.1) * 0.25, 40, 5, delta_seconds);
        character.hem.velocity += vertical_change * 0.003;
        update_spring(character.hem, speed / 240, 80, 8, delta_seconds);
    }

    // Stretch while flying fast up or down; squash on landing is set by the landing itself.
    if (!character.on_ground) {
        const stretch = 1 + Math.min(0.1, Math.abs(character.velocity_y) / 7000);
        character.squash_y += (stretch - character.squash_y) * Math.min(1, delta_seconds * 14);
        character.squash_x += (1 / Math.sqrt(stretch) - character.squash_x) * Math.min(1, delta_seconds * 14);
    }
}

// Gives the width multiplier for drawing a character mid-turn: full width facing either way,
// thin halfway through, as if the body is rotating to face the other direction.
function turn_scale(character) {
    const blend = character.facing_blend === undefined ? character.facing : character.facing_blend;
    const side = blend === 0 ? character.facing : Math.sign(blend);
    // Never thinner than about half: a body turning round, not a sheet of card.
    return side * (0.48 + 0.52 * Math.pow(Math.abs(blend), 0.6));
}

// Remembers the body's shape when a pose changes, then blends from that shape to the new
// pose over a short time, so a walk flows into a jump instead of snapping into it.
// `shape` is any object of numbers or [x, y] pairs; the result has the same layout.
function blend_pose_shape(character, pose, shape, time_seconds, blend_seconds) {
    if (character.blend_from_pose !== pose) {
        if (character.last_shape) {
            character.blend_start_shape = character.last_shape;
            character.blend_start_time = time_seconds;
        }
        character.blend_from_pose = pose;
    }
    let result = shape;
    if (character.blend_start_shape && time_seconds - character.blend_start_time < blend_seconds) {
        const progress = ease_in_out((time_seconds - character.blend_start_time) / blend_seconds);
        result = mix_shapes(character.blend_start_shape, shape, progress);
    }
    character.last_shape = result;
    return result;
}

// Mixes two shapes with the same layout: 0 gives the first, 1 the second.
function mix_shapes(first, second, amount) {
    if (typeof second === 'number') {
        return typeof first === 'number' ? first + (second - first) * amount : second;
    }
    if (Array.isArray(second)) {
        return second.map(function mix_item(item, index) {
            return mix_shapes(Array.isArray(first) && first.length === second.length ? first[index] : item, item, amount);
        });
    }
    const mixed = {};
    for (const key of Object.keys(second)) {
        mixed[key] = first && key in first ? mix_shapes(first[key], second[key], amount) : second[key];
    }
    return mixed;
}
