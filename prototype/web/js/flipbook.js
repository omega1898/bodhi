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
    cut_queue: [],        // Sheets waiting to be painted; one per drawn frame, so the game never stalls.
    pixels_per_unit: 0,   // The resolution sheets are painted at.
    // Only the drawings in use are painted: hers, and the dog's set for this chapter (the puppy's
    // or the grown dog's). The other dog set is let go, which halves the dog's memory on a phone.
    dog_set: 'bodhi',
    guest_set: null,      // A visiting dog's drawings (Biscuit), in chapters that have one.
    set_ready: {},        // Per set: true once every one of its sheets is painted.
    ready: false,         // True once every set in use is painted.
};

// How each animation plays when it isn't driven by distance or by jumping speed. Rates are in
// drawings a second; the drawing tool makes many in-between drawings, so the rates are high
// while each move still takes the same time.
//   loop: round and round at `rate` drawings a second.
//   once: through once at `rate`, then holds the last drawing.
//   intro: the first `intro_frames` drawings once at `rate` (getting down), then loops the rest
//   at `loop_rate` (breathing while down).
const flipbook_playback = {
    her: {
        idle: { mode: 'loop', rate: 10 },  // 24 drawings: one breath every 2.4 seconds.
        land: { mode: 'once', rate: 65 },
        turn: { mode: 'once', rate: 40 },
        stop: { mode: 'once', rate: 32 },
        kneel: { mode: 'intro', rate: 36, intro_frames: 13, loop_rate: 6 },
        embrace: { mode: 'loop', rate: 8 },
        sit: { mode: 'loop', rate: 8 },
    },
    bodhi: {
        idle: { mode: 'loop', rate: 12 },  // Faster when happy: see choose_flipbook_frame.
        land: { mode: 'once', rate: 65 },
        turn: { mode: 'once', rate: 40 },
        stop: { mode: 'once', rate: 32 },
        sit: { mode: 'intro', rate: 36, intro_frames: 13, loop_rate: 6 },
        sniff: { mode: 'loop', rate: 20 },
        bark: { mode: 'once', rate: 25 },
        sleep: { mode: 'loop', rate: 6 },
        lean: { mode: 'loop', rate: 8 },
        embrace: { mode: 'intro', rate: 31, intro_frames: 13, loop_rate: 8 },
    },
};

// The vertical speeds at the start of a jump (character_specs in game.js) and of a fast fall.
// The jump drawings run from the first speed, through the top of the arc, to the second.
const flipbook_jump_speeds = { her: { rise: 620, fall: 700 }, bodhi: { rise: 690, fall: 700 } };

// The puppy drawings (bodhi_puppy) are the same moves with a puppy's build, so they play
// exactly as the grown dog's do.
flipbook_playback.bodhi_puppy = flipbook_playback.bodhi;
flipbook_jump_speeds.bodhi_puppy = flipbook_jump_speeds.bodhi;
// Biscuit, the old dog next door, moves the same way too.
flipbook_playback.biscuit = flipbook_playback.bodhi;
flipbook_jump_speeds.biscuit = flipbook_jump_speeds.bodhi;

// The dog drawing sets, and the colour of each one's eyelid when it blinks.
const dog_drawing_sets = { bodhi: '#ecc791', bodhi_puppy: '#e2b578', biscuit: '#dcd8d0' };

// True for any of the dogs' drawing sets.
function is_dog_drawing(character_name) {
    return Object.prototype.hasOwnProperty.call(dog_drawing_sets, character_name);
}

// How sharply each set is painted, compared with the grown characters. The puppy is never
// drawn at more than about two thirds of full size, so painting her at 0.7 looks the same and
// uses half the memory, which matters on a phone.
const flipbook_paint_scale = { bodhi_puppy: 0.7, biscuit: 0.9 };

// Drawings a second when getting up: an intro played backwards, faster than getting down.
const getting_up_rate = 78;

// Where in its cycle a walk or trot starts when there are no feet to match yet (a character's
// very first drawing): just before the back foot swings through, which reads as a first step.
const walk_start_phase = 0.2;

// Her gaits and a dog's, slowest first, and the speeds between them. A real person breaks into
// a run somewhere above a brisk walk; a dog walks, trots at the pace of a person hurrying, and
// gallops only when it really runs. Each switch has two lines `gait_margin` either side, so a
// speed close to a line doesn't flicker between two gaits. Matches FlipbookChooser.gait_for.
const gait_steps = {
    her: { gaits: ['walk', 'run'], speeds: [160] },
    dog: { gaits: ['walk', 'trot', 'gallop'], speeds: [85, 200] },
};
const gait_margin = 10;
function gait_for(is_dog, current, speed) {
    const steps = is_dog ? gait_steps.dog : gait_steps.her;
    let index = Math.max(0, steps.gaits.indexOf(current));
    while (index < steps.speeds.length && speed > steps.speeds[index] + gait_margin) {
        index += 1;
    }
    while (index > 0 && speed < steps.speeds[index - 1] - gait_margin) {
        index -= 1;
    }
    return steps.gaits[index];
}

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
                if (flipbook_set_in_use(character_name)) {
                    flipbook.cut_queue.push([character_name, animation_name]);
                }
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

// True for the drawing sets in use: hers, this chapter's dog set, and a visiting dog's.
function flipbook_set_in_use(character_name) {
    return character_name === 'her' || character_name === flipbook.dog_set || character_name === flipbook.guest_set;
}

// Notes which sets have every sheet painted. A set is drawn only once it is complete; until then
// its character shows as the cut-out puppet.
function update_flipbook_readiness() {
    for (const [character_name, character] of Object.entries(frames_atlas)) {
        flipbook.set_ready[character_name] = Object.keys(character.animations).every(function painted(animation_name) {
            return Boolean(flipbook.frames[character_name] && flipbook.frames[character_name][animation_name]);
        });
    }
    flipbook.ready = Boolean(flipbook.set_ready.her && flipbook.set_ready[flipbook.dog_set] && (!flipbook.guest_set || flipbook.set_ready[flipbook.guest_set]));
}

// Switches which dog drawings are in use (at the start of a chapter). The new set's sheets are
// queued for painting, and the other set's paintings are let go to free memory.
function use_dog_drawings(set_name) {
    if (typeof frames_atlas === 'undefined' || !frames_atlas[set_name] || set_name === flipbook.dog_set) {
        return;
    }
    flipbook.dog_set = set_name;
    for (const character_name of Object.keys(frames_atlas)) {
        if (!flipbook_set_in_use(character_name)) {
            flipbook.frames[character_name] = {};
        }
    }
    for (const animation_name of Object.keys(flipbook.sheets[set_name] || {})) {
        flipbook.cut_queue.push([set_name, animation_name]);
    }
    update_flipbook_readiness();
}

// Switches the visiting dog's drawings on (a set's name) or off (null), at the start of a
// chapter. Like use_dog_drawings, it paints what's needed and lets go of what isn't.
function use_guest_drawings(set_name) {
    const wanted = typeof frames_atlas !== 'undefined' && set_name && frames_atlas[set_name] ? set_name : null;
    if (wanted === flipbook.guest_set) {
        return;
    }
    flipbook.guest_set = wanted;
    for (const character_name of Object.keys(frames_atlas)) {
        if (!flipbook_set_in_use(character_name)) {
            flipbook.frames[character_name] = {};
        }
    }
    for (const animation_name of Object.keys((wanted && flipbook.sheets[wanted]) || {})) {
        flipbook.cut_queue.push([wanted, animation_name]);
    }
    update_flipbook_readiness();
}

// True when a set's drawings can be shown.
function flipbook_set_ready(character_name) {
    return Boolean(flipbook.set_ready[character_name]);
}

// Called once per drawn frame. Paints at most one waiting sheet, and queues every sheet in use
// again if the screen's resolution has changed a lot (after a resize or the camera zooming in).
function update_flipbook() {
    if (flipbook.sheets_expected === 0) {
        return;
    }
    // Screen pixels per game unit at the closest camera, plus a fifth for crisp edges.
    const wanted = Math.max(1.5, game.pixel_scale * (game.camera.close_zoom || 1) * 1.2);
    if (flipbook.pixels_per_unit === 0) {
        flipbook.pixels_per_unit = wanted;
    }
    if (flipbook.ready && flipbook.cut_queue.length === 0 && Math.abs(wanted - flipbook.pixels_per_unit) / wanted > 0.15) {
        flipbook.pixels_per_unit = wanted;
        for (const [character_name, sheets] of Object.entries(flipbook.sheets)) {
            if (!flipbook_set_in_use(character_name)) {
                continue;
            }
            for (const animation_name of Object.keys(sheets)) {
                flipbook.cut_queue.push([character_name, animation_name]);
            }
        }
    }
    // Skip anything queued for a set that has since gone out of use.
    let next = flipbook.cut_queue.shift();
    while (next && !flipbook_set_in_use(next[0])) {
        next = flipbook.cut_queue.shift();
    }
    if (next) {
        paint_flipbook_sheet(next[0], next[1]);
        update_flipbook_readiness();
    }
}

// Paints one sheet into a canvas at screen resolution. The sheet's filters (fur, grain) are
// worked out once here, not every frame. Frames sit in rows of fixed-size cells.
function paint_flipbook_sheet(character_name, animation_name) {
    const cell = frames_atlas[character_name].cell;
    const entry = frames_atlas[character_name].animations[animation_name];
    const columns = entry.columns || entry.frames;
    const rows = Math.ceil(entry.frames / columns);
    const pixels_per_unit = flipbook.pixels_per_unit * (flipbook_paint_scale[character_name] || 1);
    const frame_width = Math.round(cell.width * pixels_per_unit);
    const frame_height = Math.round(cell.height * pixels_per_unit);
    const canvas = make_sprite_canvas(frame_width * columns, frame_height * rows);
    canvas.getContext('2d').drawImage(flipbook.sheets[character_name][animation_name], 0, 0, canvas.width, canvas.height);
    flipbook.frames[character_name][animation_name] = { canvas: canvas, frame_width: frame_width, frame_height: frame_height, columns: columns };
}

// How low each pose is, and which getting-down drawings (that pose's intro) bring it back up
// when played backwards. Moving to another level gets up first, then plays the new pose's own
// getting-down drawings; moving within a level (her kneeling into the hug) needs neither.
const flipbook_levels = {
    her: {
        kneel: { level: 'kneeling', up_from: 'kneel' },
        embrace: { level: 'kneeling', up_from: 'kneel' },
        sit: { level: 'sitting', up_from: null },
    },
    bodhi: {
        sit: { level: 'sitting', up_from: 'sit' },
        sleep: { level: 'lying', up_from: null },
        embrace: { level: 'hugging', up_from: 'embrace' },
    },
};
flipbook_levels.bodhi_puppy = flipbook_levels.bodhi;
flipbook_levels.biscuit = flipbook_levels.bodhi;

// The level of a pose: 'upright' unless the table above says otherwise.
function level_of(character_name, pose) {
    const entry = flipbook_levels[character_name][pose];
    return entry ? entry.level : 'upright';
}

// Starts a short move that plays over whatever the pose would show. `when` says where it may
// show: 'stand' (only standing), 'ground' (standing or setting off slowly) or 'slow' (any pose on
// the ground, while slow). `backwards` plays a getting-down intro in reverse: getting up.
function start_one_shot(state, name, time_seconds, when, backwards) {
    state.one_shot = { name: name, started: time_seconds, when: when, backwards: backwards === true };
}

// Remembers, per character, what it was doing last frame: to notice landings, turns, stops and
// pose changes, and how far it moved. Figures made fresh each frame (the title scene) start
// "settled": breathing on the shared clock, never replaying a getting-down drawing.
function flipbook_state_of(character, character_name, time_seconds) {
    const x = character.x || 0;
    const on_ground = character.on_ground !== false;
    const facing = character.facing || 1;
    const pose = character.pose || 'stand';
    let state = character.flipbook_state;
    if (!state) {
        state = {
            last_x: x, moved: 0, phase: 0, fast_until: -100, was_on_ground: on_ground, facing: facing, pose: pose,
            pose_started: -100, intro_allowed: false, one_shot: null, gait: null, starting_stride: false,
            idle_started: 0, last_animation: 'idle', last_feet: null,
        };
        character.flipbook_state = state;
    }
    const moved = Math.abs(x - state.last_x);
    state.moved = moved < 40 ? moved : 0;  // A bigger jump is a scene change, not a step.
    state.last_x = x;
    if (Math.abs(character.velocity_x || 0) > 100) {
        state.fast_until = time_seconds + 0.2;  // Moving briskly: stopping soon gets a stop.
    }
    if (on_ground && !state.was_on_ground) {
        start_one_shot(state, 'land', time_seconds, 'ground');
    }
    if (facing !== state.facing && on_ground) {
        start_one_shot(state, 'turn', time_seconds, 'ground');
    }
    if (pose !== state.pose) {
        state.pose_started = time_seconds;
        state.starting_stride = pose === 'walk';
        const turning = state.one_shot && state.one_shot.name === 'turn';
        if (state.pose === 'walk' && pose === 'stand' && time_seconds < state.fast_until && !turning) {
            start_one_shot(state, 'stop', time_seconds, 'stand');
        }
        const old_level = level_of(character_name, state.pose);
        const getting_up = (flipbook_levels[character_name][state.pose] || {}).up_from;
        if (old_level !== level_of(character_name, pose) && getting_up) {
            start_one_shot(state, getting_up, time_seconds, 'slow', true);
        }
        // Getting-down drawings play only when coming from upright (or once back up).
        state.intro_allowed = old_level === 'upright' || (old_level !== level_of(character_name, pose) && Boolean(getting_up));
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

// Which frame of a timed animation to show, `seconds` after it started. `skip_intro` goes
// straight to the loop of an intro animation (already down, so no getting down again).
function timed_frame(playback, frame_count, seconds, skip_intro) {
    const rate = Math.max(0, playback.rate);
    const played = Math.max(0, seconds) * rate;
    if (playback.mode === 'once') {
        return safe_frame(played, frame_count);
    }
    if (playback.mode === 'intro') {
        const intro_frames = Math.min(frame_count, playback.intro_frames || 1);
        const loop_count = frame_count - intro_frames;
        if (loop_count <= 0 || (!skip_intro && played < intro_frames)) {
            return safe_frame(played, intro_frames);
        }
        const looped = (skip_intro ? Math.max(0, seconds) : seconds - intro_frames / rate) * playback.loop_rate;
        return intro_frames + safe_frame(Math.floor(looped) % loop_count, loop_count);
    }
    return safe_frame(Math.floor(played) % frame_count, frame_count);
}

// The point in a walking cycle whose feet are closest to the feet just shown, so setting off,
// landing into a run or coming out of a skid carries on from where the feet already are.
// Feet are compared as drawn on screen, so `facing` turns the cycle's feet the same way.
function closest_phase(entry, last_feet, facing) {
    let best_index = 0;
    let best_gap = Infinity;
    for (let index = 0; index < entry.frames; index++) {
        const feet = entry.points[index].feet;
        let gap = 0;
        for (let foot = 0; foot < feet.length && foot < last_feet.length; foot++) {
            gap += Math.hypot(feet[foot][0] * facing - last_feet[foot][0], feet[foot][1] - last_feet[foot][1]);
        }
        if (gap < best_gap) {
            best_gap = gap;
            best_index = index;
        }
    }
    return (best_index + 0.5) / entry.frames;
}

// Picks what to draw for a character right now: [animation name, frame index, facing]. The
// facing is only given when it differs from the character's own (a skid faces the slide).
function choose_flipbook_frame(character, character_name, time_seconds) {
    const state = flipbook_state_of(character, character_name, time_seconds);
    const animations = frames_atlas[character_name].animations;
    const playbacks = flipbook_playback[character_name];
    const pose = state.pose;
    const speed = Math.abs(character.velocity_x || 0);

    // In the air: one arc of drawings, picked by vertical speed. Take-off speed shows the first,
    // the top of the jump (no vertical speed) the middle one, a fast fall the last.
    if (pose === 'jump') {
        const frame_count = animations.jump.frames;
        const speeds = flipbook_jump_speeds[character_name];
        const velocity_y = character.velocity_y || 0;
        // A puppy takes off slower (he jumps lower), so his arc starts from his own speed.
        const rise = speeds.rise * (character.jump_scale || 1);
        const through_arc = velocity_y < 0 ? 1 + velocity_y / rise : 1 + velocity_y / speeds.fall;
        state.starting_stride = true;  // Landing into a run starts the stride from the feet.
        return ['jump', safe_frame(Math.round(clamp_between(through_arc, 0, 2) * (frame_count - 1) / 2), frame_count)];
    }
    // A skid: reversed at speed, still sliding the old way. Drawn facing the slide, braced; the
    // drawing follows the speed, from sliding fast to about to pivot.
    if (pose === 'walk' && Math.sign(character.velocity_x || 0) === -state.facing && speed > 50) {
        const frame_count = animations.skid.frames;
        const slowing = 1 - clamp_between((speed - 50) / 150, 0, 1);
        state.starting_stride = true;
        return ['skid', safe_frame(Math.round(slowing * (frame_count - 1)), frame_count), -state.facing];
    }
    // A landing, turn, stop or getting up, while slow enough for it to show. When it ends, the
    // pose's own animation starts from its beginning.
    if (state.one_shot) {
        const shot = state.one_shot;
        const where_allowed = shot.when === 'stand' ? pose === 'stand' : shot.when === 'ground' ? pose === 'stand' || pose === 'walk' : true;
        const source = playbacks[shot.name];
        // Getting up plays quickly (it's usually cut short by setting off) and stays on a little
        // longer as speed builds; the other moves give way to the stride sooner.
        const frame_count = shot.backwards ? source.intro_frames : animations[shot.name].frames;
        const rate = shot.backwards ? getting_up_rate : source.rate;
        const speed_limit = shot.backwards ? 150 : 110;
        const seconds = time_seconds - shot.started;
        if (speed < speed_limit && where_allowed && seconds < frame_count / rate) {
            const index = safe_frame(seconds * rate, frame_count);
            return [shot.name, shot.backwards ? frame_count - 1 - index : index];
        }
        state.one_shot = null;
        state.pose_started = Math.max(state.pose_started, time_seconds);
    }
    if (pose === 'walk') {
        // Walk or run for her; walk, trot or gallop for the dog, by speed (see gait_for).
        // A smaller dog's stride is shorter in the world, so his cycle covers less ground.
        const size = character.size_scale || 1;
        let gait = gait_for(character_name !== 'her', state.gait, speed);
        if (gait === 'walk' && character_name !== 'her' && !animations.walk) {
            gait = 'trot';  // Drawings made before dogs had a walk: trot instead.
        }
        // A new gait carries on from the drawing whose feet are closest to the ones just shown:
        // a walk and a trot put their feet down in a different order.
        const changed_gait = state.gait !== null && state.gait !== undefined && gait !== state.gait;
        state.gait = gait;
        // The cycle moves on by the distance covered, so a planted foot stays planted.
        const entry = animations[gait];
        if (state.starting_stride || (changed_gait && state.last_feet)) {
            state.phase = state.last_feet ? closest_phase(entry, state.last_feet, state.facing) : walk_start_phase;
            state.starting_stride = false;
        } else {
            state.phase = (state.phase + state.moved / (entry.cycle_length * size)) % 1;
        }
        return [gait, safe_frame(Math.floor(state.phase * entry.frames), entry.frames)];
    }
    // Everything else: the pose's own animation, or standing still. Idle breathes from the home
    // stance each time it begins, which is exactly where landings, turns and stops end.
    const animation_name = pose === 'stand' || !animations[pose] ? 'idle' : pose;
    const playback = Object.assign({}, playbacks[animation_name]);
    if (is_dog_drawing(character_name) && animation_name === 'idle') {
        playback.rate = 12 + clamp_between(character.wag || 0, 0, 1) * 28;  // A happier dog wags faster.
    }
    let started = state.pose_started;
    if (animation_name === 'idle') {
        if (state.last_animation !== 'idle') {
            state.idle_started = time_seconds;
        }
        started = state.idle_started;
    }
    return [animation_name, timed_frame(playback, animations[animation_name].frames, time_seconds - started, !state.intro_allowed)];
}

// Draws a character from its frames, feet at the bottom middle of its collision box. Facing
// flips instantly, as hand-drawn games do; skid and turn drawings carry the motion of turning.
function draw_flipbook_character(context, character, character_name, time_seconds) {
    const [animation_name, frame_index, facing_override] = choose_flipbook_frame(character, character_name, time_seconds);
    const drawn_facing = facing_override || character.facing || 1;
    const cell = frames_atlas[character_name].cell;
    const painted = flipbook.frames[character_name][animation_name];
    const points = frames_atlas[character_name].animations[animation_name].points[frame_index];
    const state = character.flipbook_state;
    state.shown = animation_name + ' ' + frame_index;  // For tests: what's on screen,
    state.shown_facing = drawn_facing;                  // and which way it faces.
    state.last_animation = animation_name;
    // The feet as drawn on screen (turned the way the drawing faces), for starting the next stride.
    state.last_feet = points.feet.map(function on_screen(foot) { return [foot[0] * drawn_facing, foot[1]]; });
    context.save();
    context.translate(character.x + character.width / 2, character.y + character.height);
    // Frames are drawn for a full-grown body; a growing dog is drawn smaller around his feet.
    const size = character.size_scale || 1;
    context.scale(drawn_facing * (character.squash_x || 1) * size, (character.squash_y || 1) * size);
    const column = frame_index % painted.columns;
    const row = Math.floor(frame_index / painted.columns);
    context.drawImage(painted.canvas, column * painted.frame_width, row * painted.frame_height, painted.frame_width, painted.frame_height,
        -cell.feet_x, -cell.feet_y, cell.width, cell.height);
    if (is_dog_drawing(character_name)) {
        draw_mud(context, character.dirt, points.feet);
        draw_dog_blink(context, character, animation_name, points.eye, dog_drawing_sets[character_name]);
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
function draw_dog_blink(context, dog, animation_name, eye, lid_color) {
    if (animation_name === 'sleep' || animation_name === 'lean' || animation_name === 'embrace') {
        return;
    }
    if (((dog.x || 0) * 0.29 + performance.now() / 1000) % 3.7 >= 0.11) {
        return;
    }
    // The puppy's bigger head has a bigger eye: the atlas gives its size after its angle.
    const eye_size = Number.isFinite(eye[3]) ? clamp_between(eye[3], 0.5, 2) : 1;
    context.fillStyle = lid_color || '#ecc791';
    context.beginPath();
    context.ellipse(eye[0], eye[1], 2.3 * eye_size, 2.4 * eye_size, eye[2], 0, Math.PI * 2);
    context.fill();
    context.strokeStyle = '#3a2418';
    context.lineWidth = 0.5;
    context.beginPath();
    context.arc(eye[0], eye[1] + 0.2 * eye_size, 1.7 * eye_size, 0.25, Math.PI - 0.25);
    context.stroke();
}
