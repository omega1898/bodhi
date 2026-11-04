// Purpose: plays the characters' hand-drawn animation frames, the way frame-by-frame 2D games
// do: every frame is a whole drawing of the body (art/animations/*.svg, mapped by
// frames_atlas.js), chosen by what the character is doing. Walking and running frames advance
// with the distance covered, so a planted foot stays planted; everything else plays at a
// steady frame rate. Until every sheet has loaded, the cut-out puppets (puppets.js) are drawn.
'use strict';

const flipbook = {
    sheets: {},           // Loaded sheet images: flipbook.sheets.her.walk
    frames: {},           // Sheets painted at screen resolution: flipbook.frames.her.walk
    sheets_expected: 0,   // How many sheets frames_atlas.js lists.
    sheets_cut: 0,        // How many have been painted at least once.
    cut_queue: [],        // Sheets waiting to be painted; one per drawn frame, so the game never stalls.
    pixels_per_unit: 0,   // The resolution sheets are painted at.
    ready: false,
};

// How each animation plays when it isn't driven by distance or by jumping speed.
//   loop: round and round at `rate` frames a second.
//   once: through once, then holds the last frame.
//   intro: the first frame once (getting down), then loops the rest at `loop_rate`.
const flipbook_playback = {
    her: {
        idle: { mode: 'loop', rate: 2.5 },
        land: { mode: 'once', rate: 14 },
        turn: { mode: 'once', rate: 14 },
        kneel: { mode: 'intro', rate: 9, loop_rate: 1.2 },
        embrace: { mode: 'loop', rate: 1.2 },
        sit: { mode: 'loop', rate: 1.2 },
    },
    bodhi: {
        idle: { mode: 'loop', rate: 4 },
        land: { mode: 'once', rate: 14 },
        turn: { mode: 'once', rate: 14 },
        sit: { mode: 'intro', rate: 8, loop_rate: 2 },
        sniff: { mode: 'loop', rate: 7 },
        bark: { mode: 'once', rate: 10 },
        sleep: { mode: 'loop', rate: 0.8 },
        lean: { mode: 'loop', rate: 1.5 },
        embrace: { mode: 'loop', rate: 1.2 },
    },
};

// Loads every animation sheet. Tries the path next to the page first (the app), then the
// repository's art folder (playing prototype/web/index.html from a checkout); the likely one first.
function load_flipbook_sheets() {
    if (typeof frames_atlas === 'undefined') {
        return;
    }
    const from_repository = window.location.pathname.indexOf('/prototype/web/') !== -1;
    for (const [character_name, character] of Object.entries(frames_atlas)) {
        flipbook.sheets[character_name] = {};
        flipbook.frames[character_name] = {};
        for (const [animation_name, animation] of Object.entries(character.animations)) {
            flipbook.sheets_expected += 1;
            const image = new Image();
            const candidates = from_repository ? ['../../' + animation.sheet, animation.sheet] : [animation.sheet, '../../' + animation.sheet];
            let attempt = 0;
            image.onload = function sheet_loaded() {
                flipbook.sheets[character_name][animation_name] = image;
                flipbook.cut_queue.push([character_name, animation_name]);
            };
            image.onerror = function sheet_failed() {
                // If no path works, the flipbook never becomes ready and the puppets stay on.
                attempt += 1;
                if (attempt < candidates.length) {
                    image.src = candidates[attempt];
                }
            };
            image.src = candidates[0];
        }
    }
}

// Called once per drawn frame. Paints at most one waiting sheet, and queues every sheet again
// if the screen's resolution has changed a lot (after a resize or the camera zooming in).
function update_flipbook() {
    if (flipbook.sheets_expected === 0) {
        return;
    }
    const wanted = Math.max(1.5, game.pixel_scale * (game.camera.close_zoom || 1) * 1.35);
    if (flipbook.pixels_per_unit === 0) {
        flipbook.pixels_per_unit = wanted;
    }
    if (flipbook.ready && flipbook.cut_queue.length === 0 && Math.abs(wanted - flipbook.pixels_per_unit) / wanted > 0.15) {
        flipbook.pixels_per_unit = wanted;
        for (const [character_name, sheets] of Object.entries(flipbook.sheets)) {
            for (const animation_name of Object.keys(sheets)) {
                flipbook.cut_queue.push([character_name, animation_name]);
            }
        }
    }
    const next = flipbook.cut_queue.shift();
    if (next) {
        const first_time = !flipbook.frames[next[0]][next[1]];
        paint_flipbook_sheet(next[0], next[1]);
        flipbook.sheets_cut += first_time ? 1 : 0;
        flipbook.ready = flipbook.sheets_cut === flipbook.sheets_expected;
    }
}

// Paints one sheet into a canvas at screen resolution. The sheet's filters (fur, grain) are
// worked out once here, not every frame. Each frame is a fixed-size cell of that canvas.
function paint_flipbook_sheet(character_name, animation_name) {
    const cell = frames_atlas[character_name].cell;
    const entry = frames_atlas[character_name].animations[animation_name];
    const frame_width = Math.round(cell.width * flipbook.pixels_per_unit);
    const frame_height = Math.round(cell.height * flipbook.pixels_per_unit);
    const canvas = make_sprite_canvas(frame_width * entry.frames, frame_height);
    canvas.getContext('2d').drawImage(flipbook.sheets[character_name][animation_name], 0, 0, canvas.width, canvas.height);
    flipbook.frames[character_name][animation_name] = { canvas: canvas, frame_width: frame_width, frame_height: frame_height };
}

// Remembers, per character, what it was doing last frame: to notice landings, turns and pose
// changes, and to add up the distance walked. Figures made fresh each frame (the title scene)
// start "settled", so they never replay a getting-down frame.
function flipbook_state_of(character, time_seconds) {
    const x = character.x || 0;
    const on_ground = character.on_ground !== false;
    const facing = character.facing || 1;
    const pose = character.pose || 'stand';
    let state = character.flipbook_state;
    if (!state) {
        state = { last_x: x, distance: 0, was_on_ground: on_ground, facing: facing, pose: pose, pose_started: -100, one_shot: null, one_shot_started: -100, gait: null };
        character.flipbook_state = state;
    }
    const moved = Math.abs(x - state.last_x);
    if (moved < 40) {
        state.distance += moved;  // A bigger jump is a scene change, not a step.
    }
    state.last_x = x;
    if (on_ground && !state.was_on_ground) {
        state.one_shot = 'land';
        state.one_shot_started = time_seconds;
    }
    if (facing !== state.facing && on_ground) {
        state.one_shot = 'turn';
        state.one_shot_started = time_seconds;
    }
    if (pose !== state.pose) {
        state.pose_started = time_seconds;
    }
    state.was_on_ground = on_ground;
    state.facing = facing;
    state.pose = pose;
    return state;
}

// Turns any number into a frame that exists: whole, from 0 to frame_count - 1. Odd input
// (NaN, negative, huge) becomes a real frame instead of nothing.
function safe_frame(index, frame_count) {
    if (!Number.isFinite(index) || index < 0) {
        return 0;
    }
    return Math.min(frame_count - 1, Math.floor(index));
}

// Which frame of a timed animation to show, `seconds` after it started.
function timed_frame(playback, frame_count, seconds) {
    const rate = Math.max(0, playback.rate);
    const played = Math.max(0, seconds) * rate;
    if (playback.mode === 'once') {
        return safe_frame(played, frame_count);
    }
    if (playback.mode === 'intro') {
        if (played < 1) {
            return 0;
        }
        const looped = (seconds - 1 / rate) * playback.loop_rate;
        return 1 + safe_frame(Math.floor(looped) % (frame_count - 1), frame_count - 1);
    }
    return safe_frame(Math.floor(played) % frame_count, frame_count);
}

// Picks the animation and frame for a character right now: [animation name, frame index].
function choose_flipbook_frame(character, character_name, time_seconds) {
    const state = flipbook_state_of(character, time_seconds);
    const animations = frames_atlas[character_name].animations;
    const playbacks = flipbook_playback[character_name];
    const pose = state.pose;
    const speed = Math.abs(character.velocity_x || 0);

    // In the air: rising, then falling, picked by vertical speed.
    if (pose === 'jump') {
        const velocity_y = character.velocity_y || 0;
        if (velocity_y < 0) {
            return ['jump_rise', velocity_y < -300 ? 0 : 1];
        }
        return ['jump_fall', velocity_y < 250 ? 0 : 1];
    }
    // A landing or a turn plays briefly when standing or setting off slowly; at speed the
    // stride simply carries on.
    if (state.one_shot && speed < 110 && (pose === 'stand' || pose === 'walk')) {
        const playback = playbacks[state.one_shot];
        const frame_count = animations[state.one_shot].frames;
        const seconds = time_seconds - state.one_shot_started;
        if (seconds < frame_count / playback.rate) {
            return [state.one_shot, timed_frame(playback, frame_count, seconds)];
        }
        state.one_shot = null;
    }
    if (pose === 'walk') {
        // Walk or run (trot or gallop for the dog), by speed. Two thresholds, so a speed near
        // the line doesn't flicker between the two.
        const slow_gait = character_name === 'her' ? 'walk' : 'trot';
        const fast_gait = character_name === 'her' ? 'run' : 'gallop';
        const switch_speed = character_name === 'her' ? 125 : 150;
        if (state.gait !== fast_gait && speed > switch_speed + 10) {
            state.gait = fast_gait;
        } else if (state.gait !== slow_gait && speed < switch_speed - 10) {
            state.gait = slow_gait;
        }
        const gait = state.gait || slow_gait;
        const entry = animations[gait];
        const position = (state.distance / entry.cycle_length) % 1;
        return [gait, safe_frame(Math.floor(position * entry.frames) % entry.frames, entry.frames)];
    }
    // Everything else: the pose's own animation, or standing still.
    const animation_name = pose === 'stand' || !animations[pose] ? 'idle' : pose;
    const playback = Object.assign({}, playbacks[animation_name]);
    if (character_name === 'bodhi' && animation_name === 'idle') {
        playback.rate = 3 + clamp_between(character.wag || 0, 0, 1) * 8;  // A happier dog wags faster.
    }
    const started = animation_name === 'idle' ? 0 : state.pose_started;
    return [animation_name, timed_frame(playback, animations[animation_name].frames, time_seconds - started)];
}

// Draws a character from its frames, feet at the bottom middle of its collision box. Facing
// flips instantly, as hand-drawn games do; the turn frames carry the motion of turning round.
function draw_flipbook_character(context, character, character_name, time_seconds) {
    const [animation_name, frame_index] = choose_flipbook_frame(character, character_name, time_seconds);
    const cell = frames_atlas[character_name].cell;
    const painted = flipbook.frames[character_name][animation_name];
    const points = frames_atlas[character_name].animations[animation_name].points[frame_index];
    character.flipbook_state.shown = animation_name + ' ' + frame_index;  // For tests: what's on screen.
    context.save();
    context.translate(character.x + character.width / 2, character.y + character.height);
    context.scale((character.facing || 1) * (character.squash_x || 1), character.squash_y || 1);
    context.drawImage(painted.canvas, frame_index * painted.frame_width, 0, painted.frame_width, painted.frame_height,
        -cell.feet_x, -cell.feet_y, cell.width, cell.height);
    if (character_name === 'bodhi') {
        draw_dog_blink(context, character, animation_name, points.eye);
        if (character.carrying) {
            draw_carried_item(context, character.carrying, points.mouth[0], points.mouth[1]);
        }
    } else if (character.holding) {
        draw_carried_item(context, character.holding, points.hand[0] + 3, points.hand[1] - 2);
    }
    context.restore();
}

// Every few seconds the dog blinks: a soft lid over the painted eye for a tenth of a second.
// Frames that already have closed eyes (sleeping, leaning in, the embrace) are left alone.
function draw_dog_blink(context, dog, animation_name, eye) {
    if (animation_name === 'sleep' || animation_name === 'lean' || animation_name === 'embrace') {
        return;
    }
    if (((dog.x || 0) * 0.29 + performance.now() / 1000) % 3.7 >= 0.11) {
        return;
    }
    context.fillStyle = '#ecc791';
    context.beginPath();
    context.ellipse(eye[0], eye[1], 2.3, 2.4, eye[2], 0, Math.PI * 2);
    context.fill();
    context.strokeStyle = '#3a2418';
    context.lineWidth = 0.5;
    context.beginPath();
    context.arc(eye[0], eye[1] + 0.2, 1.7, 0.25, Math.PI - 0.25);
    context.stroke();
}
