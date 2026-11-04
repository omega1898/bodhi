// Purpose: runs the game. Movement and collisions, the companion who follows you, calls and
// answers, the story rules that turn events into scenes, the reunion, the controls, and the
// screens between chapters. The chapters themselves are data in levels.js.
'use strict';

// ---------- Fixed numbers ----------

let view_height = 540;   // How many world units fit vertically on screen. Set on resize.
let view_width = 960;    // How many fit horizontally. Set on resize.
const minimum_view_width = 640;
const fixed_step_seconds = 1 / 60;
const gravity = 1800;          // Pull while rising: sets how high a jump goes.
const falling_gravity = 2500;  // Stronger pull on the way down: jumps feel weighty, not floaty.
const maximum_fall_speed = 1100;
const step_up_height = 14;
const character_specs = {
    // Speeds are world units per second; accelerations are units per second, per second.
    // Starting, stopping and turning take a moment, the way a body with weight does.
    // Weight shows in the animation; the controls themselves answer within about a tenth of a second.
    human: { width: 26, height: 86, walk_speed: 175, jog_speed: 235, jump_speed: 620, acceleration: 1800, braking: 2600, turning: 3600, air_control: 1500 },
    dog: { width: 46, height: 38, walk_speed: 235, jog_speed: 235, jump_speed: 690, acceleration: 2400, braking: 3000, turning: 4400, air_control: 2000 },
};
const progress_storage_key = 'until_we_find_home_progress';
const keepsake_storage_key = 'until_we_find_home_keepsakes';

// ---------- Everything the running game remembers ----------

const game = {
    screen: 'title',          // title, card, play, aftermath
    paused: false,
    level_index: 0,
    level: null,
    world: null,
    backdrop_layers: [],
    // The camera looks at a point in the world (center_x, center_y) from a zoom level.
    camera: { center_x: 480, center_y: 300, zoom: 1, base_zoom: 1, shake_seconds: 0, previous_center_x: 480, previous_center_y: 300, look_ahead: 0 },
    interpolation: 0,          // How far between two physics steps this frame is drawn (0 to 1).
    pixel_scale: 1,
    accumulator_seconds: 0,
    speed_multiplier: 1,      // Tests run the game faster; players never change this.
    last_frame_time: 0,
    clock_seconds: 0,
    rain_drops: [],
    fade: 0,                  // 0 = clear, 1 = black.
    fade_target: 0,
    last_input: 'keyboard',
    unlocked_chapters: 1,
    play_log: [],             // Every story event with its time, for playtests.
    aftermath_seconds: 0,
    found_keepsakes: new Set(),  // Names like 'other_side:ticket', kept between visits.
    toast_timer: 0,
    frame_costs: [],             // Milliseconds of work per frame, newest last.
};

const input = {
    left: false, right: false, low_held: false, jump_held: false,
    jump_pressed: false, act_pressed: false, call_pressed: false, low_pressed: false, switch_pressed: false,
};

const canvas = document.getElementById('world_canvas');
const context = canvas.getContext('2d');
const light_canvas = document.createElement('canvas');
const aftermath_canvas = document.getElementById('aftermath_canvas');
const aftermath_context = aftermath_canvas.getContext('2d');
const page = {
    title_screen: document.getElementById('title_screen'),
    chapter_card: document.getElementById('chapter_card'),
    card_eyebrow: document.getElementById('card_eyebrow'),
    card_title: document.getElementById('card_title'),
    card_place: document.getElementById('card_place'),
    aftermath_screen: document.getElementById('aftermath_screen'),
    aftermath_text: document.getElementById('aftermath_text'),
    aftermath_continue: document.getElementById('aftermath_continue'),
    pause_screen: document.getElementById('pause_screen'),
    subtitle: document.getElementById('subtitle'),
    objective: document.getElementById('objective'),
    chapter_list: document.getElementById('chapter_list'),
    touch_switch: document.getElementById('touch_switch'),
    touch_call: document.getElementById('touch_call'),
    touch_low: document.getElementById('touch_low'),
    touch_act: document.getElementById('touch_act'),
    sound_button: document.getElementById('sound_button'),
    toast: document.getElementById('toast'),
    memory_box: document.getElementById('memory_box'),
    memory_list: document.getElementById('memory_list'),
    aftermath_tally: document.getElementById('aftermath_tally'),
};

// ---------- Small helpers ----------

// Replaces {dog}, {them} and {their} in story text with the dog's name and pronouns.
function personalize(text) {
    return text
        .split('{dog}').join(story_config.dog_name)
        .split('{them}').join(story_config.dog_object_pronoun)
        .split('{their}').join(story_config.dog_possessive_pronoun);
}

// The horizontal middle of a character, used for distances.
function center_x(character) {
    return character.x + character.width / 2;
}

// True when two boxes overlap. Boxes have x, y (top-left), and width/height or w/h.
function boxes_overlap(a_x, a_y, a_width, a_height, b_x, b_y, b_width, b_height) {
    return a_x < b_x + b_width - 0.01 && a_x + a_width > b_x + 0.01 && a_y < b_y + b_height - 0.01 && a_y + a_height > b_y + 0.01;
}

// Reads saved progress. Storage can be missing or blocked (private browsing), so failures
// fall back to "only chapter one unlocked" instead of breaking the game.
function load_progress() {
    try {
        const saved = Number(window.localStorage.getItem(progress_storage_key));
        if (Number.isInteger(saved) && saved >= 1 && saved <= levels.length) {
            game.unlocked_chapters = saved;
        }
    } catch (storage_error) {
        game.unlocked_chapters = 1;
    }
}

// Saves how many chapters are unlocked, ignoring storage that refuses.
function save_progress() {
    try {
        window.localStorage.setItem(progress_storage_key, String(game.unlocked_chapters));
    } catch (storage_error) {
        // Progress just won't survive closing the page.
    }
}

// Reads which keepsakes have been found on earlier visits. Saved data is treated as
// untrusted: anything other than a short list of short names is ignored, never trusted.
function load_keepsakes() {
    game.found_keepsakes = new Set();
    try {
        const parsed = JSON.parse(window.localStorage.getItem(keepsake_storage_key) || '[]');
        if (Array.isArray(parsed)) {
            for (const name of parsed.slice(0, 200)) {
                if (typeof name === 'string' && name.length <= 80) {
                    game.found_keepsakes.add(name);
                }
            }
        }
    } catch (storage_error) {
        game.found_keepsakes = new Set();
    }
}

// Saves the found keepsakes, ignoring storage that refuses.
function save_keepsakes() {
    try {
        window.localStorage.setItem(keepsake_storage_key, JSON.stringify(Array.from(game.found_keepsakes)));
    } catch (storage_error) {
        // They just won't survive closing the page.
    }
}

// Lists a chapter's keepsakes and how many have been found.
function keepsake_tally(level) {
    const items = level.interactables.filter(function is_keepsake(item) { return item.keepsake; });
    const found = items.filter(function was_found(item) { return game.found_keepsakes.has(level.id + ':' + item.keepsake); });
    return { items: items, found: found.length, total: items.length };
}

// A keepsake was found: tell its memory, save it, and show a small note at the top.
function record_keepsake(item) {
    const name = game.level.id + ':' + item.keepsake;
    const is_new = !game.found_keepsakes.has(name);
    game.found_keepsakes.add(name);
    save_keepsakes();
    queue_lines([item.memory], 'her');
    const tally = keepsake_tally(game.level);
    show_toast(is_new ? 'Keepsake found  \u00b7  ' + tally.found + ' of ' + tally.total + ' in this chapter' : 'Already in your memory box');
    play_sound('chime', 0.3, 0);
}

// Shows a short message at the top of the screen for a few seconds.
function show_toast(text) {
    page.toast.textContent = text;
    page.toast.classList.add('shown');
    window.clearTimeout(game.toast_timer);
    game.toast_timer = window.setTimeout(function hide_toast() { page.toast.classList.remove('shown'); }, 3200);
}

// Fills the memory box: every keepsake found so far, and a hint for each one still missing.
function fill_memory_box() {
    page.memory_list.textContent = '';
    for (const level of levels) {
        const tally = keepsake_tally(level);
        const heading = document.createElement('h3');
        heading.className = 'memory_chapter';
        heading.textContent = personalize(level.title) + '  \u00b7  ' + tally.found + ' of ' + tally.total;
        page.memory_list.appendChild(heading);
        for (const item of tally.items) {
            const entry = document.createElement('p');
            const found = game.found_keepsakes.has(level.id + ':' + item.keepsake);
            entry.className = found ? 'memory_entry' : 'memory_entry missing';
            entry.textContent = found ? personalize(item.memory)
                : item.hidden ? personalize('Something only {dog} can find. Try sniffing.') : 'Something she could find, somewhere in this chapter.';
            page.memory_list.appendChild(entry);
        }
    }
}

// ---------- Building a level ----------

// Makes a character in its starting position.
function make_character(kind, x, y) {
    const spec = character_specs[kind];
    return {
        kind: kind, x: x, y: y - spec.height, width: spec.width, height: spec.height,
        velocity_x: 0, velocity_y: 0, on_ground: false, facing: 1, walk_phase: 0,
        pose: 'stand', pose_override: null, in_water: false, blocked: false, blocked_seconds: 0,
        rest_seconds: 0, jump_cooldown: 0, call_cooldown: 0, ear_lift: 0, wag: 0.4, fear: 0,
        carrying: null, kneeling: false, idle_seconds: 0, ahead_target: null, ahead_timer: 6,
        safe_x: x, safe_y: y - spec.height, safe_timer: 0, speed_ratio: 0, step_parity: 0, whine_timer: 4,
    };
}

// Creates the live state of a level from its data. The data itself is never changed,
// so restarting a chapter always starts from the same place.
function build_world(level) {
    const world = {
        solids: level.solids.map(function copy_solid(solid) {
            return Object.assign({}, solid, { active: !solid.hidden, fade: solid.hidden ? 0 : 1 });
        }),
        water: (level.water || []).map(function copy_water(water) {
            return Object.assign({}, water, { current_top: water.y, target_top: water.y });
        }),
        characters: {
            human: make_character('human', level.start.human[0], level.start.human[1]),
            dog: make_character('dog', level.start.dog[0], level.start.dog[1]),
        },
        controlled: 'human',
        happened: new Set(),
        fired_rules: new Set(),
        switching_allowed: false,
        follow: true,
        fetch_enabled: Boolean(level.ball),
        beat: 'together',
        scent_target: null,
        sniff_seconds: 0,
        mood: level.mood || 'calm',
        objective: '',
        scheduled: [],
        bond: Object.assign({ fear: 0.05, closeness: 0.3 }, level.bond || {}),
        revealed: new Set(),
        reunion: null,
        ball: level.ball ? { x: level.ball[0], y: level.ball[1], velocity_x: 0, velocity_y: 0, held_by: null } : null,
        dog_run_target: null,
        particles: [],
        call_markers: [],
        pending_answer: null,
        elapsed_seconds: 0,
        lightning_seconds: 0,
        lightning_timer: 7,
        complete: false,
        prompt: null,
        subtitle_queue: [],
        subtitle_seconds: 0,
        subtitle_showing: false,
        autopilot: null,
    };
    return world;
}

// Starts a chapter: builds its world, paints its backdrop, and shows its title card.
function start_level(level_index) {
    game.level_index = level_index;
    game.level = levels[level_index];
    game.world = build_world(game.level);
    load_scenery_art(game.level);
    game.backdrop_layers = render_backdrop(game.level, game.pixel_scale, view_width, view_height);
    game.rain_drops = [];
    game.near_rain_drops = [];
    prepare_level_lighting(game.level);
    const start_human = game.world.characters.human;
    const camera = game.camera;
    camera.zoom = camera.base_zoom;
    camera.center_x = center_x(start_human);
    camera.center_y = game.level.height;
    camera.look_ahead = 0;
    update_camera(10);
    camera.previous_center_x = camera.center_x;
    camera.previous_center_y = camera.center_y;
    page.subtitle.classList.remove('shown');
    page.objective.textContent = '';
    const scenery = game.level.scenery;
    set_weather_sound(scenery.rain_sound !== undefined ? scenery.rain_sound : (scenery.rain || 0), scenery.wind || 0);
    set_music_mood(game.world.mood);
    show_chapter_card();
}

// Works out, once per chapter, where each lamp's light lands (the floor under it) and
// which areas are indoors, so rain doesn't fall inside. Derived only from the level data.
function prepare_level_lighting(level) {
    for (const prop of level.props) {
        if (prop.kind !== 'lamp') {
            continue;
        }
        const bulb_y = prop.hanging ? prop.y + 42 : prop.y;
        let floor_y = bulb_y + 240;
        for (const solid of level.solids) {
            if ((solid.kind === 'floor' || solid.kind === 'plank') && solid.y > bulb_y && solid.x <= prop.x && solid.x + solid.w >= prop.x) {
                floor_y = Math.min(floor_y, solid.y);
            }
        }
        prop.floor_y = floor_y;
    }
    game.shelters = level.props.filter(function is_indoors(prop) { return prop.kind === 'room_wall'; });
}

// Shows the chapter's title card, then fades into play.
function show_chapter_card() {
    game.screen = 'card';
    hide_screen(page.title_screen);
    hide_screen(page.aftermath_screen);
    page.card_eyebrow.textContent = game.level.eyebrow;
    page.card_title.textContent = personalize(game.level.title);
    page.card_place.textContent = personalize(game.level.place);
    show_screen(page.chapter_card);
    window.clearTimeout(game.card_timer);
    game.card_timer = window.setTimeout(begin_play, 3400 / game.speed_multiplier);
}

// Hides the title card and lets the player move.
function begin_play() {
    if (game.screen !== 'card') {
        return;
    }
    hide_screen(page.chapter_card);
    game.screen = 'play';
    document.body.classList.add('playing');
    update_touch_labels();
    fire_event('level_started');
}

// ---------- Story rules ----------

// Records that something happened and runs every rule that was waiting for it.
// Each event counts once, so a lever pulled twice can't run its scene twice.
function fire_event(event_name) {
    const world = game.world;
    if (!world || world.happened.has(event_name)) {
        return false;
    }
    world.happened.add(event_name);
    game.play_log.push({ chapter: game.level.id, seconds: Number(world.elapsed_seconds.toFixed(2)), event: event_name });
    const rules = game.level.rules;
    for (let rule_index = 0; rule_index < rules.length; rule_index++) {
        const rule = rules[rule_index];
        if (world.fired_rules.has(rule_index)) {
            continue;
        }
        const triggered_by_this = rule.on === event_name;
        const all_needed_done = rule.when_all && rule.when_all.every(function has_happened(name) { return world.happened.has(name); });
        if (triggered_by_this || all_needed_done) {
            world.fired_rules.add(rule_index);
            const blocked = (rule.unless || []).some(function done(name) { return world.happened.has(name); });
            if (!blocked) {
                apply_effects(rule);
            }
        }
    }
    return true;
}

// Carries out what a rule says should happen. Every key is optional.
function apply_effects(rule) {
    const world = game.world;
    if (rule.say) {
        queue_lines(rule.say, rule.voice || 'her');
    }
    for (const solid_id of rule.remove || []) {
        set_solid_active(solid_id, false);
    }
    for (const solid_id of rule.add || []) {
        set_solid_active(solid_id, true);
    }
    if (rule.beat) {
        world.beat = rule.beat;
    }
    if (rule.place) {
        for (const kind of Object.keys(rule.place)) {
            const character = world.characters[kind];
            character.x = rule.place[kind][0] - character.width / 2;
            character.y = rule.place[kind][1] - character.height;
            character.velocity_x = 0;
            character.velocity_y = 0;
            character.kneeling = false;
            character.ahead_target = null;
        }
        world.autopilot = null;  // A scene moved someone: stop any walk in progress.
    }
    if (rule.control) {
        take_control(rule.control);
    }
    if (rule.switching !== undefined) {
        world.switching_allowed = rule.switching;
        update_touch_labels();
    }
    if (rule.follow !== undefined) {
        world.follow = rule.follow;
    }
    if (rule.fetch !== undefined) {
        world.fetch_enabled = rule.fetch;
    }
    if (rule.scent !== undefined) {
        world.scent_target = rule.scent;
    }
    if (rule.mood) {
        world.mood = rule.mood;
        set_music_mood(rule.mood);
    }
    if (rule.shake) {
        game.camera.shake_seconds = rule.shake;
    }
    if (rule.sound) {
        play_sound(rule.sound, 0, 0);
    }
    if (rule.bond) {
        world.bond.fear = Math.max(0, Math.min(1, world.bond.fear + (rule.bond.fear || 0)));
        world.bond.closeness = Math.max(0, Math.min(1, world.bond.closeness + (rule.bond.closeness || 0)));
    }
    if (rule.objective !== undefined) {
        set_objective(rule.objective);
    }
    for (const water_change of rule.water || []) {
        const water = world.water.find(function match_water(candidate) { return candidate.id === water_change.id; });
        if (water) {
            water.target_top = water_change.top;
        }
    }
    if (rule.schedule) {
        world.scheduled.push({ seconds_left: rule.schedule.seconds, event: rule.schedule.event });
    }
    if (rule.dog_runs_to !== undefined) {
        world.dog_run_target = rule.dog_runs_to;
    }
    if (rule.carry) {
        for (const kind of Object.keys(rule.carry)) {
            world.characters[kind].carrying = rule.carry[kind];
        }
    }
    if (rule.effect) {
        start_visual_effect(rule.effect);
    }
    for (const kind of Object.keys(rule.pose || {})) {
        const character = world.characters[kind];
        character.kneeling = false;
        character.pose_override = { pose: rule.pose[kind][0], seconds: rule.pose[kind][1] };
    }
    if (rule.reunion) {
        start_reunion();
    }
    if (rule.complete) {
        finish_level();
    }
}

// Opens or closes a gate, collapses a walkway, or adds a plank.
function set_solid_active(solid_id, active) {
    for (const solid of game.world.solids) {
        if (solid.id !== solid_id) {
            continue;
        }
        solid.active = active;
        // How the change looks. Collision changes at once; the picture catches up physically.
        if (solid.kind === 'plank' && !active) {
            break_into_pieces(solid);
            solid.fade = 0;
        } else if (solid.kind === 'plank' && active) {
            solid.motion = { type: 'swing_down', seconds: 0 };
            solid.fade = 1;
        } else if (solid.kind === 'crate' && active) {
            solid.motion = { type: 'fall', seconds: 0 };
            solid.fade = 1;
        } else if (['gate', 'bars', 'glass'].includes(solid.kind)) {
            solid.motion = { type: active ? 'drop_shut' : 'slide_open', seconds: 0 };
            solid.fade = 1;
        }
    }
}

// A collapsing walkway breaks into boards that tumble and fall, each spinning its own way.
function break_into_pieces(solid) {
    const piece_width = 34;
    for (let piece_x = solid.x; piece_x < solid.x + solid.w; piece_x += piece_width) {
        const from_middle = (piece_x + piece_width / 2 - (solid.x + solid.w / 2)) / (solid.w / 2);
        game.world.particles.push({
            kind: 'board', x: piece_x + piece_width / 2, y: solid.y + solid.h / 2, width: piece_width - 2, height: solid.h,
            velocity_x: from_middle * 60 + (Math.random() - 0.5) * 50, velocity_y: -40 - Math.random() * 80 + Math.abs(from_middle) * 40,
            gravity: 1500, life: 2.2, max_life: 2.2, size: 1, angle: 0, spin: (Math.random() - 0.5) * 7, delay: Math.abs(from_middle) * 0.18,
        });
    }
}

// Starts a one-off visual effect named by a rule.
function start_visual_effect(effect) {
    const world = game.world;
    if (effect.kind === 'debris') {
        for (let index = 0; index < 46; index++) {
            world.particles.push({
                kind: 'debris', x: effect.x + (Math.random() - 0.5) * effect.width, y: effect.y,
                velocity_x: (Math.random() - 0.5) * 160, velocity_y: Math.random() * -160, gravity: 1400,
                life: 1.6, max_life: 1.6, size: 3 + Math.random() * 5, color: Math.random() < 0.5 ? '#5a4634' : '#3b332b', angle: Math.random() * 3, spin: (Math.random() - 0.5) * 12,
            });
        }
    } else if (effect.kind === 'photo') {
        world.particles.push({ kind: 'photo', x: effect.x, y: effect.y, velocity_x: 70, velocity_y: -60, gravity: 18, life: 7, max_life: 7, size: 1, spin: 0.9 });
    } else if (effect.kind === 'lightning') {
        world.lightning_seconds = 0.35;
        play_sound('thunder', 0, 0);
    }
}

// Changes the small objective line in the corner.
function set_objective(text) {
    game.world.objective = text;
    page.objective.textContent = text ? personalize(text) : '';
}

// Adds story lines to the subtitle queue. `voice` changes the style: 'her' (her thoughts),
// 'dog' (the dog's senses, in small capitals) or 'note' (something written).
function queue_lines(lines, voice) {
    for (const line of lines) {
        game.world.subtitle_queue.push({ text: personalize(line), voice: voice, queued_at: game.world.elapsed_seconds });
    }
}

// Shows each queued line long enough to read, then the next.
function update_subtitles(delta_seconds) {
    const world = game.world;
    world.subtitle_seconds -= delta_seconds;
    if (world.subtitle_seconds > 0) {
        return;
    }
    if (world.subtitle_showing) {
        page.subtitle.classList.remove('shown');
        world.subtitle_showing = false;
        world.subtitle_seconds = 0.55;
        return;
    }
    // Lines that waited too long belong to a moment that has passed: skip them, so the words
    // on screen always match what's happening now.
    while (world.subtitle_queue.length > 1 && world.elapsed_seconds - world.subtitle_queue[0].queued_at > 9) {
        world.subtitle_queue.shift();
    }
    const next_line = world.subtitle_queue.shift();
    if (!next_line) {
        return;
    }
    page.subtitle.textContent = next_line.text;
    page.subtitle.className = 'shown voice_' + next_line.voice;
    world.subtitle_showing = true;
    world.subtitle_seconds = 2.2 + next_line.text.length * 0.055;
}

// ---------- Control ----------

// Hands the player control of the human or the dog.
function take_control(kind) {
    const world = game.world;
    if (world.controlled === kind) {
        return;
    }
    world.controlled = kind;
    world.characters.human.kneeling = false;
    update_touch_labels();
}

// Updates the phone buttons so they say what they'll do for the current character.
function update_touch_labels() {
    if (!game.world) {
        return;
    }
    const is_dog = game.world.controlled === 'dog';
    page.touch_call.textContent = is_dog ? 'Bark' : 'Whistle';
    page.touch_low.textContent = is_dog ? 'Sniff' : 'Kneel';
    page.touch_switch.hidden = !game.world.switching_allowed;
}

// The character the player controls, and the other one.
function controlled_character() {
    return game.world.characters[game.world.controlled];
}
function companion_character() {
    return game.world.characters[game.world.controlled === 'human' ? 'dog' : 'human'];
}

// ---------- Movement and collisions ----------

// True when a solid stops this kind of character. Some invisible solids stop only one kind,
// for example an edge the woman won't step off, which the dog can still pass.
function solid_blocks(solid, kind) {
    return solid.active && (!solid.blocks || solid.blocks === kind);
}

// True when the character, placed at (x, y), would overlap any solid that blocks it.
function position_is_blocked(character, x, y) {
    for (const solid of game.world.solids) {
        if (solid_blocks(solid, character.kind) && boxes_overlap(x, y, character.width, character.height, solid.x, solid.y, solid.w, solid.h)) {
            return true;
        }
    }
    return false;
}

// Moves a character one step: walking, jumping, gravity, then pushing it out of anything solid.
function move_character(character, direction, wants_jump, delta_seconds, speed_scale) {
    const spec = character_specs[character.kind];
    // Keep pushing one way and she breaks into a jog after a moment.
    character.push_seconds = direction !== 0 && direction === character.last_direction ? (character.push_seconds || 0) + delta_seconds : 0;
    character.last_direction = direction;
    const jog = character.kind === 'human' ? ease_in_out((character.push_seconds - 0.7) / 0.6) : 0;
    const top_speed = (spec.walk_speed + (spec.jog_speed - spec.walk_speed) * jog) * (character.in_water ? 0.62 : 1) * (speed_scale || 1);
    const target_velocity = direction * top_speed;
    // Speed changes at a steady rate: quick to start, quicker to stop, quickest to turn round.
    const turning = direction !== 0 && Math.sign(character.velocity_x) === -direction;
    let rate = character.on_ground ? (turning ? spec.turning : direction !== 0 ? spec.acceleration : spec.braking) : spec.air_control;
    if (character.on_ground && Math.abs(character.velocity_x) > top_speed + 1 && direction !== 0) {
        rate = spec.braking * 0.5;  // Slowing from a jog to a walk is gradual.
    }
    // A hard turn at speed kicks dust up from the feet, once per turn, flying the way of the slide.
    if (turning && character.on_ground && Math.abs(character.velocity_x) > 150 && !character.skidding && game.world) {
        for (let index = 0; index < 4; index++) {
            game.world.particles.push({ kind: 'puff', x: center_x(character) - direction * (4 + index * 4), y: character.y + character.height - 3, velocity_x: -direction * (40 + index * 25), velocity_y: -14 - index * 6, gravity: 0, life: 0.4, max_life: 0.4, size: 4 });
        }
    }
    character.skidding = turning && character.on_ground;
    character.velocity_x = move_toward(character.velocity_x, target_velocity, rate * delta_seconds);
    if (direction !== 0) {
        character.facing = direction;
    }
    // "Coyote time": a jump still works for a tenth of a second after walking off an edge.
    character.ground_grace = character.on_ground ? 0.1 : Math.max(0, (character.ground_grace || 0) - delta_seconds);
    if (wants_jump && character.ground_grace > 0) {
        character.velocity_y = -spec.jump_speed;
        character.on_ground = false;
        character.ground_grace = 0;
        character.jump_cut_allowed = true;
        character.squash_y = 1.12;  // A stretch as the legs push off.
        character.squash_x = 0.92;
        if (game.world && character.kind === game.world.controlled) {
            play_sound('step', 0.3, 0);
        }
    }
    const pull = character.velocity_y < 0 ? gravity : falling_gravity;
    character.velocity_y = Math.min(maximum_fall_speed, character.velocity_y + pull * delta_seconds);

    // Sideways first, so a wall stops you without also stopping a fall.
    const start_x = character.x;
    character.x += character.velocity_x * delta_seconds;
    character.blocked = false;
    for (const solid of game.world.solids) {
        if (!solid_blocks(solid, character.kind) || !boxes_overlap(character.x, character.y, character.width, character.height, solid.x, solid.y, solid.w, solid.h)) {
            continue;
        }
        const feet_y = character.y + character.height;
        const step_needed = feet_y - solid.y;
        if (character.on_ground && step_needed > 0 && step_needed <= step_up_height && !position_is_blocked(character, character.x, solid.y - character.height)) {
            character.y = solid.y - character.height;  // A small step: walk up it.
            continue;
        }
        if (character.x + character.width / 2 < solid.x + solid.w / 2) {
            character.x = solid.x - character.width;
        } else {
            character.x = solid.x + solid.w;
        }
        character.velocity_x = 0;
        character.blocked = direction !== 0;
        character.blocked_by_top = solid.y;
    }
    const level_width = game.level.width;
    if (character.x < 0 || character.x + character.width > level_width) {
        character.x = Math.max(0, Math.min(level_width - character.width, character.x));
        character.blocked = direction !== 0;
    }

    // Then up and down: land on floors, bump heads on ceilings.
    const was_on_ground = character.on_ground;
    const falling_speed = character.velocity_y;
    character.y += character.velocity_y * delta_seconds;
    character.on_ground = false;
    for (const solid of game.world.solids) {
        if (!solid_blocks(solid, character.kind) || !boxes_overlap(character.x, character.y, character.width, character.height, solid.x, solid.y, solid.w, solid.h)) {
            continue;
        }
        if (character.velocity_y >= 0) {
            character.y = solid.y - character.height;
            character.on_ground = true;
        } else {
            character.y = solid.y + solid.h;
        }
        character.velocity_y = 0;
    }
    // Landing has weight: a squash that springs back, a puff of dust, a soft sound.
    if (character.on_ground && !was_on_ground && falling_speed > 260) {
        const impact = Math.min(1, (falling_speed - 260) / 700);
        character.squash_y = 1 - 0.18 * impact - 0.04;
        character.squash_x = 1 + 0.14 * impact + 0.03;
        for (let index = 0; index < 6; index++) {
            game.world.particles.push({ kind: 'puff', x: center_x(character) + (index - 2.5) * 6, y: character.y + character.height - 3, velocity_x: (index - 2.5) * 22, velocity_y: -12, gravity: 0, life: 0.45, max_life: 0.45, size: 5 });
        }
        if (character.kind === game.world.controlled) {
            play_sound('step', 0.1, 0);
        }
    }
    if (character.on_ground) {
        character.squash_y = (character.squash_y || 1) + (1 - (character.squash_y || 1)) * Math.min(1, delta_seconds * 11);
        character.squash_x = (character.squash_x || 1) + (1 - (character.squash_x || 1)) * Math.min(1, delta_seconds * 11);
    }

    // Water slows you down; wading into it splashes.
    const feet_y = character.y + character.height;
    const was_in_water = character.in_water;
    character.in_water = game.world.water.some(function is_in(water) {
        return center_x(character) > water.x && center_x(character) < water.x + water.w && feet_y > water.current_top + 3;
    });
    if (character.in_water && !was_in_water) {
        splash(center_x(character), Math.min(feet_y, water_surface_at(center_x(character))), 10 + Math.abs(falling_speed) / 60);
    }
    character.shadow_y = character.y + character.height;

    // Remember the last safe ground, and return there if something goes wrong and the
    // character falls out of the world, instead of leaving the player stuck.
    character.safe_timer -= delta_seconds;
    if (character.on_ground && character.safe_timer <= 0) {
        character.safe_x = character.x;
        character.safe_y = character.y;
        character.safe_timer = 0.5;
    }
    if (character.y > game.level.height + 200) {
        character.x = character.safe_x;
        character.y = character.safe_y;
        character.velocity_y = 0;
    }

    const moved = Math.abs(character.x - start_x) / Math.max(delta_seconds, 0.0001);
    character.speed_ratio = Math.min(1, moved / spec.walk_speed);
    // The walk cycle advances with distance covered, so feet never slide on the ground.
    character.walk_phase += moved * delta_seconds * (character.kind === 'dog' ? 0.15 : 0.105);
    update_rig(character, delta_seconds, game.clock_seconds);
}

// The height of the water's surface at a point, for placing splashes.
function water_surface_at(x) {
    for (const water of game.world.water) {
        if (x > water.x && x < water.x + water.w) {
            return water.current_top;
        }
    }
    return game.level.height;
}

// Throws up droplets and a ring where something enters the water.
function splash(x, y, strength) {
    const world = game.world;
    for (let index = 0; index < Math.min(16, strength); index++) {
        world.particles.push({ kind: 'drop', x: x + (Math.random() - 0.5) * 16, y: y - 2, velocity_x: (Math.random() - 0.5) * 140, velocity_y: -90 - Math.random() * 150, gravity: 1100, life: 0.6, max_life: 0.6, size: 2 });
    }
    world.particles.push({ kind: 'ripple', x: x, y: y, velocity_x: 0, velocity_y: 0, gravity: 0, life: 0.9, max_life: 0.9, size: 1 });
    play_sound('splash', 0.3, 0);
}

// Picks the drawing pose from what the character is doing right now.
function update_pose(character, delta_seconds) {
    if (character.pose_override) {
        character.pose_override.seconds -= delta_seconds;
        if (character.pose_override.seconds <= 0) {
            character.pose_override = null;
        }
    }
    const moving = Math.abs(character.velocity_x) > 18;
    character.idle_seconds = moving ? 0 : character.idle_seconds + delta_seconds;
    if (character.pose_override) {
        character.pose = character.pose_override.pose;
    } else if (!character.on_ground) {
        character.pose = 'jump';
    } else if (character.kind === 'human' && character.kneeling) {
        character.pose = 'kneel';
    } else if (moving) {
        character.pose = 'walk';
    } else if (character.kind === 'dog' && character.idle_seconds > 2.5) {
        character.pose = 'sit';
    } else {
        character.pose = 'stand';
    }
    character.ear_lift = Math.max(0, character.ear_lift - delta_seconds * 0.6);
    character.call_cooldown = Math.max(0, character.call_cooldown - delta_seconds);
    character.jump_cooldown = Math.max(0, character.jump_cooldown - delta_seconds);
}

// Plays footsteps and splashes in time with the controlled character's walk.
function update_footsteps(character) {
    const steps_taken = Math.floor(character.walk_phase / Math.PI);
    if (steps_taken !== character.step_parity) {
        character.step_parity = steps_taken;
        if (character.on_ground && character.speed_ratio > 0.2) {
            play_sound(character.in_water ? 'splash' : 'step', character.in_water ? 0.6 : 0.2, 0);
            if (character.in_water) {
                for (let index = 0; index < 4; index++) {
                    game.world.particles.push({ kind: 'drop', x: center_x(character), y: character.y + character.height - 4, velocity_x: (Math.random() - 0.5) * 120, velocity_y: -80 - Math.random() * 100, gravity: 900, life: 0.5, max_life: 0.5, size: 2 });
                }
            }
        }
    }
}

// ---------- The companion ----------

// How far behind the companion walks. Right after a scare, the dog stays much closer.
function follow_gap(companion) {
    if (companion.kind === 'human') {
        return 70;
    }
    return 118 - game.world.bond.closeness * 80;
}

// Decides where the character you're not controlling walks: beside you, after the ball,
// ahead to explore, or to a spot the story asks for. When blocked by a wall, it tries a
// jump, then waits and looks toward you.
function update_companion(companion, leader, delta_seconds) {
    const world = game.world;
    const ball = world.ball;
    let target_x = null;
    let stop_distance = follow_gap(companion);
    let speed_scale = 1;

    if (world.reunion && world.reunion.stage !== 'done' && companion.kind === 'dog') {
        target_x = center_x(leader);
        stop_distance = 50;
        speed_scale = 1.15;
    } else if (companion.kind === 'dog' && world.dog_run_target !== null) {
        target_x = world.dog_run_target;
        stop_distance = 10;
        speed_scale = 1.1;
        if (Math.abs(center_x(companion) - target_x) < 14) {
            world.dog_run_target = null;
            companion.pose_override = { pose: 'sit', seconds: 2 };
            companion.facing = Math.sign(center_x(leader) - center_x(companion)) || companion.facing;
        }
    } else if (companion.kind === 'dog' && ball && world.fetch_enabled && ball.held_by !== 'human') {
        if (ball.held_by === 'dog') {
            target_x = center_x(leader);
            stop_distance = 34;
            if (Math.abs(center_x(leader) - center_x(companion)) < 40) {
                drop_ball_for(leader, companion);
            }
        } else if (Math.abs(ball.velocity_x) < 60) {
            target_x = ball.x;
            stop_distance = 6;
            if (Math.abs(ball.x - center_x(companion)) < 20 && Math.abs(ball.y - (companion.y + companion.height)) < 30) {
                ball.held_by = 'dog';
                companion.carrying = 'ball';
                play_sound('pickup', 0.3, 0);
            }
        }
    } else if (companion.kind === 'dog' && companion.ahead_target !== null) {
        target_x = companion.ahead_target;
        stop_distance = 12;
        if (Math.abs(center_x(companion) - target_x) < 16 || companion.blocked_seconds > 0.8) {
            companion.ahead_target = null;
            companion.pose_override = { pose: 'sit', seconds: 1.5 };  // Stop, sit, and check back.
            companion.facing = Math.sign(center_x(leader) - center_x(companion)) || companion.facing;
        }
    } else if (world.follow) {
        target_x = center_x(leader);
    }

    // The dog's habit, from the design: runs a little ahead, then stops and checks back.
    if (companion.kind === 'dog' && world.beat === 'together' && world.bond.fear < 0.35 && !world.reunion && companion.ahead_target === null) {
        companion.ahead_timer -= delta_seconds;
        if (companion.ahead_timer <= 0 && Math.abs(leader.velocity_x) > 60) {
            companion.ahead_target = center_x(leader) + leader.facing * 230;
            companion.ahead_timer = 9 + Math.random() * 6;
        }
    }

    let direction = 0;
    if (target_x !== null) {
        const distance = target_x - center_x(companion);
        // Two thresholds ("hysteresis"): set off only when clearly too far, stop when close.
        // With one threshold the companion would twitch back and forth at the boundary.
        if (Math.abs(distance) > stop_distance + 26) {
            companion.following = true;
        } else if (Math.abs(distance) < stop_distance) {
            companion.following = false;
        }
        if (companion.following) {
            direction = Math.sign(distance);
            if (companion.kind === 'dog' && Math.abs(distance) < 140 && world.dog_run_target === null) {
                speed_scale *= 0.7;  // Trot when close, run when far.
            }
        }
    }

    let wants_jump = false;
    if (companion.rest_seconds > 0) {
        companion.rest_seconds -= delta_seconds;
        direction = 0;
    } else if (direction !== 0 && companion.blocked && companion.on_ground) {
        companion.blocked_seconds += delta_seconds;
        // Only try a jump if the obstacle could be cleared. Facing a tall fence, a sensible dog
        // doesn't leap at it over and over: it sits and waits.
        const jump_reach = Math.pow(character_specs[companion.kind].jump_speed, 2) / (2 * gravity);
        const hopeless = (companion.y + companion.height) - (companion.blocked_by_top || 0) > jump_reach;
        if (hopeless) {
            companion.blocked_seconds = 2;
        }
        if (companion.jump_cooldown <= 0 && companion.blocked_seconds < 1.4) {
            wants_jump = true;
            companion.jump_cooldown = 0.6;
        }
        if (companion.blocked_seconds > 1.4) {
            // Can't get through. Wait, face the other one, and (if the dog) whine softly.
            companion.blocked_seconds = 0;
            companion.rest_seconds = 2.5;
            companion.ahead_target = null;
            companion.facing = Math.sign(center_x(leader) - center_x(companion)) || companion.facing;
            if (companion.kind === 'dog') {
                companion.pose_override = { pose: 'sit', seconds: 2.5 };
                companion.whine_timer -= 1;
                if (companion.whine_timer <= 0) {
                    play_sound('whine', distance_factor(companion), screen_pan(companion));
                    companion.whine_timer = 3;
                }
            }
            direction = 0;
        }
    } else if (!companion.blocked) {
        companion.blocked_seconds = Math.max(0, companion.blocked_seconds - delta_seconds);
    }
    if (direction === 0 && target_x !== null && !companion.pose_override) {
        const toward_leader = Math.sign(center_x(leader) - center_x(companion));
        if (toward_leader !== 0 && Math.abs(companion.velocity_x) < 10 && Math.abs(center_x(leader) - center_x(companion)) > 24) {
            companion.facing = toward_leader;
        }
    }
    move_character(companion, direction, wants_jump, delta_seconds, speed_scale);
}

// The dog drops the ball at the woman's feet: the end of one round of fetch.
function drop_ball_for(human, dog) {
    const ball = game.world.ball;
    ball.held_by = null;
    ball.x = center_x(human) + human.facing * 18;
    ball.y = human.y + human.height - 6;
    ball.velocity_x = 0;
    ball.velocity_y = 0;
    dog.carrying = null;
    dog.pose_override = { pose: 'sit', seconds: 1.6 };
    dog.wag = 1;
    fire_event('ball_fetched');
}

// Moves the ball: carried, thrown, bouncing and rolling to a stop.
function update_ball(delta_seconds) {
    const world = game.world;
    const ball = world.ball;
    if (!ball) {
        return;
    }
    if (ball.held_by) {
        const holder = world.characters[ball.held_by];
        ball.x = center_x(holder) + holder.facing * (holder.kind === 'dog' ? 30 : 12);
        ball.y = holder.y + (holder.kind === 'dog' ? 12 : 42);
        return;
    }
    ball.velocity_y = Math.min(900, ball.velocity_y + gravity * 0.8 * delta_seconds);
    ball.x += ball.velocity_x * delta_seconds;
    for (const solid of world.solids) {
        if (solid_blocks(solid, 'dog') && boxes_overlap(ball.x - 5, ball.y - 5, 10, 10, solid.x, solid.y, solid.w, solid.h)) {
            ball.x = ball.velocity_x > 0 ? solid.x - 5 : solid.x + solid.w + 5;
            ball.velocity_x *= -0.5;
        }
    }
    ball.y += ball.velocity_y * delta_seconds;
    for (const solid of world.solids) {
        if (solid_blocks(solid, 'dog') && boxes_overlap(ball.x - 5, ball.y - 5, 10, 10, solid.x, solid.y, solid.w, solid.h)) {
            if (ball.velocity_y > 0) {
                ball.y = solid.y - 5;
                ball.velocity_y = Math.abs(ball.velocity_y) > 120 ? -ball.velocity_y * 0.45 : 0;
                ball.velocity_x *= 0.86;
            } else {
                ball.y = solid.y + solid.h + 5;
                ball.velocity_y = 0;
            }
        }
    }
    ball.x = Math.max(6, Math.min(game.level.width - 6, ball.x));
}

// ---------- Calls, sniffing, kneeling, using things ----------

// How far away something is, from 0 (on top of you) to 1 (far), for quieter sounds.
function distance_factor(character) {
    const listener = controlled_character();
    const distance = Math.hypot(center_x(character) - center_x(listener), character.y - listener.y);
    return Math.min(1, distance / 1400);
}

// Where a sound should come from: -1 far left of the screen, 1 far right.
function screen_pan(character) {
    return Math.max(-1, Math.min(1, (center_x(character) - game.camera.center_x) * game.camera.zoom / (view_width / 2)));
}

// The woman whistles or the dog barks. The other one answers a moment later, and a ring
// shows where the answer came from: the "can they hear me?" moment from the design.
function call_out(caller) {
    const world = game.world;
    if (caller.call_cooldown > 0) {
        return;
    }
    caller.call_cooldown = 1.4;
    play_sound(caller.kind === 'human' ? 'whistle' : 'bark', 0, 0);
    if (caller.kind === 'dog') {
        caller.pose_override = { pose: 'bark', seconds: 0.6 };
    }
    world.bond.fear = Math.max(0, world.bond.fear - 0.03);
    world.call_markers.push({ x: center_x(caller), y: caller.y, seconds: 1.2, kind: caller.kind, own: true });
    world.pending_answer = { seconds: 0.75, answerer: caller.kind === 'human' ? 'dog' : 'human' };
    fire_event(caller.kind + '_called');
    fire_event(caller.kind + '_called_during_' + world.beat);
}

// Plays the answering call, once its short delay has passed.
function update_answer(delta_seconds) {
    const world = game.world;
    if (!world.pending_answer) {
        return;
    }
    world.pending_answer.seconds -= delta_seconds;
    if (world.pending_answer.seconds > 0) {
        return;
    }
    const answerer = world.characters[world.pending_answer.answerer];
    world.pending_answer = null;
    if (world.reunion && world.reunion.stage === 'embrace') {
        return;
    }
    play_sound(answerer.kind === 'dog' ? 'bark' : 'whistle', distance_factor(answerer) * 0.8, screen_pan(answerer));
    if (answerer.kind === 'dog') {
        answerer.pose_override = { pose: 'bark', seconds: 0.6 };
        answerer.ear_lift = 1;
        answerer.wag = Math.min(1, answerer.wag + 0.2);
    }
    world.call_markers.push({ x: center_x(answerer), y: answerer.y, seconds: 2.4, kind: answerer.kind, own: false });
    fire_event(answerer.kind + '_answered');
}

// The dog sniffs: a faint trail of scent drifts toward wherever the story says her scent leads.
function sniff(dog) {
    const world = game.world;
    dog.pose_override = { pose: 'sniff', seconds: 1.5 };
    world.sniff_seconds = 3;
    for (const item of game.level.interactables) {
        if (item.hidden && !world.revealed.has(item.keepsake) && Math.abs(item.x - center_x(dog)) < 300 && Math.abs(item.y - (dog.y + dog.height)) < 120) {
            world.revealed.add(item.keepsake);
            dog.ear_lift = 1;
            for (let index = 0; index < 18; index++) {
                world.particles.push({ kind: 'scent', x: item.x + (Math.random() - 0.5) * 30, y: item.y - 20 - Math.random() * 30, velocity_x: 0, velocity_y: -12, gravity: 0, life: 2.4, max_life: 2.4, size: 4 });
            }
        }
    }
    fire_event('dog_sniffed');
    fire_event('dog_sniffed_during_' + world.beat);
}

// Adds scent particles while the dog is sniffing.
function update_scent(delta_seconds) {
    const world = game.world;
    if (world.sniff_seconds <= 0) {
        return;
    }
    world.sniff_seconds -= delta_seconds;
    const dog = world.characters.dog;
    const nose_x = center_x(dog) + dog.facing * 26;
    const nose_y = dog.y + dog.height - 10;
    for (let index = 0; index < 2; index++) {
        if (world.scent_target) {
            const target_x = world.scent_target[0];
            const target_y = world.scent_target[1];
            const total = Math.hypot(target_x - nose_x, target_y - nose_y);
            const shown_length = Math.min(total, 520);
            const along = Math.random() * shown_length / Math.max(total, 1);
            const wobble = Math.sin(along * 20 + world.elapsed_seconds * 2) * 14;
            world.particles.push({
                kind: 'scent', x: nose_x + (target_x - nose_x) * along, y: nose_y + (target_y - nose_y) * along + wobble - 6,
                velocity_x: (Math.random() - 0.5) * 10, velocity_y: -6 - Math.random() * 8, gravity: 0,
                life: 1.8, max_life: 1.8, size: 3 + Math.random() * 2,
            });
        } else {
            const angle = Math.random() * Math.PI * 2;
            world.particles.push({
                kind: 'scent', x: nose_x + Math.cos(angle) * 30, y: nose_y - 10 + Math.sin(angle) * 14,
                velocity_x: -Math.sin(angle) * 20, velocity_y: Math.cos(angle) * 10, gravity: 0,
                life: 1.1, max_life: 1.1, size: 2.5,
            });
        }
    }
}

// True when an interactable thing can be used right now by this character.
function interactable_available(item, character) {
    const world = game.world;
    if (item.who !== 'either' && item.who !== character.kind) {
        return false;
    }
    if (world.happened.has(item.event) && !item.repeatable) {
        return false;
    }
    if (item.hidden && !world.revealed.has(item.keepsake)) {
        return false;
    }
    if ((item.requires || []).some(function missing(name) { return !world.happened.has(name); })) {
        return false;
    }
    if ((item.until || []).some(function happened(name) { return world.happened.has(name); })) {
        return false;
    }
    return true;
}

// True when the character stands close enough to use the thing.
function in_reach(item, character) {
    return Math.abs(item.x - center_x(character)) < (item.reach || 56) && Math.abs(item.y - (character.y + character.height)) < 70;
}

// Finds what pressing "use" would do right now, and the words to show above it.
function find_current_prompt() {
    const world = game.world;
    const character = controlled_character();
    const other = companion_character();
    const ball = world.ball;
    if (world.reunion) {
        if (world.reunion.stage === 'approach' && character.kind === 'human') {
            const close = Math.abs(center_x(other) - center_x(character)) < 150;
            if (close && !character.kneeling) {
                return { x: center_x(character), y: character.y - 20, text: 'Kneel', control: 'low' };
            }
            if (close && character.kneeling) {
                return { x: center_x(character), y: character.y - 4, text: personalize('Hold {dog}'), control: 'act' };
            }
        }
        return null;
    }
    if (character.kind === 'human' && ball && ball.held_by === 'human') {
        return { x: center_x(character), y: character.y - 20, text: 'Throw the ball', control: 'act', action: 'throw' };
    }
    let nearest = null;
    for (const item of game.level.interactables) {
        if (interactable_available(item, character) && in_reach(item, character)) {
            if (!nearest || Math.abs(item.x - center_x(character)) < Math.abs(nearest.x - center_x(character))) {
                nearest = item;
            }
        }
    }
    if (nearest) {
        return { x: nearest.x, y: nearest.y - 84, text: personalize(nearest.label), control: 'act', item: nearest };
    }
    if (character.kind === 'human' && ball && !ball.held_by && Math.abs(ball.x - center_x(character)) < 40 && Math.abs(ball.y - (character.y + character.height)) < 40) {
        return { x: ball.x, y: ball.y - 40, text: 'Pick up the ball', control: 'act', action: 'pick_up' };
    }
    const together = Math.abs(center_x(other) - center_x(character)) < 70 && Math.abs((other.y + other.height) - (character.y + character.height)) < 30;
    if (together && character.kind === 'human') {
        return { x: center_x(other), y: other.y - 26, text: personalize('Pet {dog}'), control: 'act', action: 'pet' };
    }
    if (together && character.kind === 'dog') {
        return { x: center_x(other), y: other.y - 18, text: 'Lean in', control: 'act', action: 'lean' };
    }
    return null;
}

// Does whatever "use" means right now.
function use_action() {
    const world = game.world;
    const character = controlled_character();
    const other = companion_character();
    const prompt = world.prompt;
    if (world.reunion) {
        if (world.reunion.stage === 'approach' && prompt && prompt.control === 'act') {
            begin_embrace();
        }
        return;
    }
    if (!prompt || prompt.control !== 'act') {
        return;
    }
    if (prompt.action === 'throw') {
        const ball = world.ball;
        ball.held_by = null;
        ball.velocity_x = character.facing * 520;
        ball.velocity_y = -380;
        world.characters.dog.ear_lift = 1;
        fire_event('ball_thrown');
    } else if (prompt.action === 'pick_up') {
        world.ball.held_by = 'human';
        play_sound('pickup', 0, 0);
    } else if (prompt.action === 'pet' || prompt.action === 'lean') {
        const dog = world.characters.dog;
        dog.pose_override = { pose: 'lean', seconds: 1.6 };
        dog.wag = 1;
        world.bond.fear = Math.max(0, world.bond.fear - 0.08);
        if (prompt.action === 'pet') {
            character.kneeling = true;
            character.facing = Math.sign(center_x(other) - center_x(character)) || character.facing;
        }
        fire_event(prompt.action === 'pet' ? 'dog_petted' : 'dog_leaned_in');
    } else if (prompt.item) {
        const item = prompt.item;
        item.done = true;
        item.used_at = game.clock_seconds;
        if (item.sound) {
            play_sound(item.sound, 0, 0);
        }
        if (item.pose) {
            character.pose_override = { pose: item.pose, seconds: 0.8 };
        }
        if (item.keepsake) {
            record_keepsake(item);
        }
        fire_event(item.event);
    }
}

// The "low" button: the woman kneels, the dog sniffs.
function low_action() {
    const character = controlled_character();
    if (character.kind === 'dog') {
        sniff(character);
    } else if (character.on_ground) {
        character.kneeling = !character.kneeling;
        if (character.kneeling) {
            character.velocity_x = 0;
            const dog = game.world.characters.dog;
            if (Math.abs(center_x(dog) - center_x(character)) < 110 && !game.world.reunion) {
                dog.pose_override = { pose: 'lean', seconds: 1.4 };
                fire_event('knelt_by_dog');
            }
        }
    }
}

// ---------- The reunion ----------

// Starts the reunion: the dog runs to the woman. The player chooses when to kneel and hold.
function start_reunion() {
    const world = game.world;
    world.reunion = { stage: 'approach', seconds: 0 };
    take_control('human');
    world.switching_allowed = false;
    world.follow = false;
    world.characters.dog.ahead_target = null;
    world.dog_run_target = null;
    world.characters.dog.wag = 1;
    update_touch_labels();
    set_music_mood('tender');
    set_objective('');
}

// The embrace: both characters hold still, the light warms, the music opens up.
function begin_embrace() {
    const world = game.world;
    const human = world.characters.human;
    const dog = world.characters.dog;
    world.reunion.stage = 'embrace';
    world.reunion.seconds = 0;
    human.facing = Math.sign(center_x(dog) - center_x(human)) || 1;
    dog.facing = -human.facing;
    dog.x = center_x(human) + human.facing * 20 - dog.width / 2;
    dog.y = human.y + human.height - dog.height;
    human.kneeling = true;
    human.pose_override = { pose: 'embrace', seconds: 6.5 };
    dog.pose_override = { pose: 'embrace', seconds: 6.5 };
    dog.wag = 1;
    play_sound('heartbeat', 0, 0);
    play_sound('chime', 0, 0);
    world.bond.fear = 0;
    world.bond.closeness = 0.95;
    fire_event('reunion_embrace');
}

// Counts down the embrace, then hands back control. The design asks that the dog's
// moment finishes before any new goal appears, so nothing else shows until it ends.
function update_reunion(delta_seconds) {
    const world = game.world;
    if (!world.reunion || world.reunion.stage !== 'embrace') {
        return;
    }
    world.reunion.seconds += delta_seconds;
    if (world.reunion.seconds > 6.5) {
        world.reunion.stage = 'done';
        world.follow = true;
        world.characters.human.kneeling = false;
        world.characters.dog.pose_override = { pose: 'lean', seconds: 1.5 };
        fire_event('reunion_complete');
    }
}

// ---------- One step of the world ----------

// Advances the world by one fixed step (one sixtieth of a second).
function update_world(delta_seconds) {
    const world = game.world;
    if (world.complete) {
        return;
    }
    world.elapsed_seconds += delta_seconds;
    // Remember where everything was, so drawing can blend smoothly between physics steps.
    for (const character of [world.characters.human, world.characters.dog]) {
        character.previous_x = character.x;
        character.previous_y = character.y;
    }
    game.camera.previous_center_x = game.camera.center_x;
    game.camera.previous_center_y = game.camera.center_y;

    for (const timer of world.scheduled.slice()) {
        timer.seconds_left -= delta_seconds;
        if (timer.seconds_left <= 0) {
            world.scheduled.splice(world.scheduled.indexOf(timer), 1);
            fire_event(timer.event);
        }
    }

    const player = controlled_character();
    const companion = companion_character();
    const embracing = world.reunion && world.reunion.stage === 'embrace';

    // Player input.
    if (!embracing) {
        let direction = (input.right ? 1 : 0) - (input.left ? 1 : 0);
        // Jump buffering: a jump pressed just before landing still happens on landing.
        if (input.jump_pressed) {
            world.jump_buffer_seconds = 0.13;
        }
        world.jump_buffer_seconds = Math.max(0, (world.jump_buffer_seconds || 0) - delta_seconds);
        let wants_jump = world.jump_buffer_seconds > 0;
        if (world.autopilot) {
            const distance = world.autopilot.target_x - center_x(player);
            direction = Math.abs(distance) > 10 ? Math.sign(distance) : 0;
            wants_jump = player.blocked && player.on_ground;
            if (direction === 0) {
                world.autopilot = null;
            }
        }
        if (player.kneeling && (direction !== 0 || wants_jump)) {
            player.kneeling = false;
        }
        if (player.pose_override && player.pose_override.pose === 'sit') {
            if (direction !== 0 || wants_jump) {
                player.pose_override = null;
            } else {
                direction = 0;
            }
        }
        if (player.kneeling) {
            direction = 0;
            wants_jump = false;
        }
        const was_grounded = player.ground_grace > 0;
        move_character(player, direction, wants_jump, delta_seconds, 1);
        if (wants_jump && was_grounded && player.velocity_y < 0) {
            world.jump_buffer_seconds = 0;
        }
        // Short tap, short hop: letting go of jump early cuts the rise.
        if (!input.jump_held && !world.autopilot && player.jump_cut_allowed && player.velocity_y < -200) {
            player.velocity_y *= 0.5;
            player.jump_cut_allowed = false;
        }
        if (player.velocity_y >= 0) {
            player.jump_cut_allowed = false;
        }
        if (input.call_pressed) {
            call_out(player);
        }
        if (input.low_pressed) {
            low_action();
        }
        if (input.switch_pressed && world.switching_allowed) {
            take_control(companion.kind);
            play_sound('pickup', 0.4, 0);
        }
        world.prompt = find_current_prompt();
        if (input.act_pressed) {
            use_action();
        }
        update_companion(world.characters[world.controlled === 'human' ? 'dog' : 'human'], controlled_character(), delta_seconds);
    } else {
        world.prompt = null;
    }
    input.jump_pressed = input.act_pressed = input.call_pressed = input.low_pressed = input.switch_pressed = false;

    for (const character of [world.characters.human, world.characters.dog]) {
        update_pose(character, delta_seconds);
    }
    update_footsteps(controlled_character());
    const dog = world.characters.dog;
    const human = world.characters.human;
    if (human.head_tilt) {
        const dog_close = Math.abs(center_x(dog) - center_x(human)) < 110 && Math.abs(dog.y + dog.height - human.y - human.height) < 30;
        update_spring(human.head_tilt, dog_close && Math.abs(human.velocity_x) < 20 ? 0.16 : 0, 40, 10, delta_seconds);
    }
    dog.fear = world.bond.fear;
    dog.wag += ((world.bond.fear > 0.5 ? 0.1 : 0.45) - dog.wag) * delta_seconds * 0.4;

    // Fear fades slowly; the need to stay close fades even more slowly. After a frightening
    // separation the dog keeps close for a while, which says more than any line of text.
    world.bond.fear = Math.max(0.02, world.bond.fear - delta_seconds * 0.012);
    world.bond.closeness = Math.max(0.3, world.bond.closeness - delta_seconds * 0.004);

    update_ball(delta_seconds);
    check_zones();
    update_answer(delta_seconds);
    update_scent(delta_seconds);
    update_reunion(delta_seconds);
    update_effects(delta_seconds);
    update_camera(delta_seconds);
    update_subtitles(delta_seconds);
}

// Fires the event of any area the right character has walked into.
function check_zones() {
    const world = game.world;
    for (const zone of game.level.zones || []) {
        if (world.happened.has(zone.event)) {
            continue;
        }
        if ((zone.requires || []).some(function missing(name) { return !world.happened.has(name); })) {
            continue;
        }
        if ((zone.unless || []).some(function done(name) { return world.happened.has(name); })) {
            continue;
        }
        const inside = function is_inside(character) {
            const middle_x = center_x(character);
            const middle_y = character.y + character.height / 2;
            return middle_x >= zone.x && middle_x <= zone.x + zone.w && middle_y >= zone.y && middle_y <= zone.y + zone.h;
        };
        let triggered = false;
        if (zone.who === 'both') {
            triggered = inside(world.characters.human) && inside(world.characters.dog);
        } else {
            const character = world.characters[zone.who];
            triggered = inside(character) && (zone.anyone || world.controlled === zone.who);
        }
        if (triggered) {
            fire_event(zone.event);
        }
    }
}

// Moves particles, call rings, rising or falling water, fading gates, lightning and fireflies.
function update_effects(delta_seconds) {
    const world = game.world;
    for (const solid of world.solids) {
        if (solid.motion) {
            solid.motion.seconds += delta_seconds;
            if (solid.motion.type === 'fall' && !solid.motion.landed && solid.motion.seconds > 0.45) {
                solid.motion.landed = true;
                play_sound('thud', 0.2, 0);
                for (let index = 0; index < 8; index++) {
                    world.particles.push({ kind: 'puff', x: solid.x + Math.random() * solid.w, y: solid.y + solid.h - 2, velocity_x: (Math.random() - 0.5) * 90, velocity_y: -15, gravity: 0, life: 0.6, max_life: 0.6, size: 6 });
                }
            }
        }
    }
    for (const prop of game.level.props) {
        if (prop.kind === 'lamp' && prop.hanging) {
            // Hanging lamps swing gently in the draught, and hard when the ground shakes.
            prop.sway = prop.sway || make_spring(0);
            if (game.camera.shake_seconds > 0) {
                prop.sway.velocity += (Math.random() - 0.5) * 3;
            }
            update_spring(prop.sway, Math.sin(world.elapsed_seconds * 0.9 + prop.x) * 0.025 * (game.level.scenery.wind || 0.2), 9, 0.8, delta_seconds);
        }
    }
    for (const particle of world.particles) {
        if (particle.delay > 0) {
            particle.delay -= delta_seconds;
            continue;
        }
        if (particle.spin) {
            particle.angle += particle.spin * delta_seconds;
        }
        particle.velocity_y += (particle.gravity || 0) * delta_seconds;
        particle.x += particle.velocity_x * delta_seconds;
        particle.y += particle.velocity_y * delta_seconds;
        particle.life -= delta_seconds;
    }
    world.particles = world.particles.filter(function still_alive(particle) { return particle.life > 0; });
    for (const marker of world.call_markers) {
        marker.seconds -= delta_seconds;
    }
    world.call_markers = world.call_markers.filter(function still_showing(marker) { return marker.seconds > 0; });
    for (const water of world.water) {
        water.current_top += (water.target_top - water.current_top) * Math.min(1, delta_seconds * 0.8);
    }
    for (const solid of world.solids) {
        if (!solid.motion && solid.kind !== 'plank') {
            solid.fade += ((solid.active ? 1 : 0) - solid.fade) * Math.min(1, delta_seconds * 4);
        }
    }
    // Rain hitting the ground: tiny splashes on open floors near the camera.
    const rain = game.level.scenery.rain || 0;
    if (rain > 0.2) {
        for (let attempt = 0; attempt < rain * 3; attempt++) {
            const splash_x = game.camera.center_x + (Math.random() - 0.5) * view_width / game.camera.zoom;
            let floor_top = null;
            for (const solid of world.solids) {
                if (solid.active && (solid.kind === 'floor' || solid.kind === 'plank') && splash_x >= solid.x && splash_x <= solid.x + solid.w && (floor_top === null || solid.y < floor_top)) {
                    floor_top = solid.y;
                }
            }
            const sheltered = game.shelters.some(function covers(shelter) { return splash_x >= shelter.x && splash_x <= shelter.x + shelter.w && floor_top !== null && floor_top <= shelter.y + shelter.h + 2 && floor_top >= shelter.y; });
            if (floor_top !== null && !sheltered) {
                world.particles.push({ kind: 'drop', x: splash_x, y: floor_top - 1, velocity_x: (Math.random() - 0.5) * 40, velocity_y: -30 - Math.random() * 50, gravity: 900, life: 0.22, max_life: 0.22, size: 1.5 });
            }
        }
    }
    const scenery = game.level.scenery;
    world.lightning_seconds = Math.max(0, world.lightning_seconds - delta_seconds);
    if (scenery.storm) {
        world.lightning_timer -= delta_seconds;
        if (world.lightning_timer <= 0) {
            world.lightning_seconds = 0.3;
            world.lightning_timer = 7 + Math.random() * 9;
            window.setTimeout(function delayed_thunder() { play_sound('thunder', 0, 0); }, 900);
        }
    }
    if (scenery.fireflies && Math.random() < delta_seconds * 3) {
        world.particles.push({ kind: 'firefly', x: game.camera.center_x + (Math.random() - 0.5) * view_width, y: 260 + Math.random() * 240, velocity_x: (Math.random() - 0.5) * 20, velocity_y: -5 - Math.random() * 10, gravity: 0, life: 4, max_life: 4, size: 2 });
    }
    if (scenery.dust && Math.random() < delta_seconds * 6) {
        world.particles.push({ kind: 'dust', x: game.camera.center_x + (Math.random() - 0.5) * view_width, y: game.camera.center_y + (Math.random() - 0.5) * view_height, velocity_x: 4 + Math.random() * 6, velocity_y: -2 + Math.random() * 4, gravity: 0, life: 5, max_life: 5, size: 1.5, color: 'rgba(239,230,212,0.35)' });
    }
    if (game.camera.shake_seconds > 0) {
        game.camera.shake_seconds -= delta_seconds;
    }
}

// Moves the camera smoothly after the controlled character, looking a little ahead.
function update_camera(delta_seconds) {
    const world = game.world;
    const camera = game.camera;
    const player = controlled_character();
    const companion = companion_character();
    // Look ahead in the direction of travel, eased so turning around doesn't jolt the view.
    const wanted_look_ahead = player.facing * 50 * Math.min(1, 0.35 + player.speed_ratio);
    camera.look_ahead += (wanted_look_ahead - camera.look_ahead) * Math.min(1, delta_seconds * 1.6);
    let focus_x = center_x(player) + camera.look_ahead;
    let focus_y = null;
    let target_zoom = camera.close_zoom;

    // Close on the character you control most of the time, so faces and bodies read clearly.
    // Pull back to frame both of them only when both matter: different floors close together,
    // meeting at a barrier, an answered call, the approach before a reunion.
    const gap_x = Math.abs(center_x(companion) - center_x(player));
    const gap_y = Math.abs((companion.y + companion.height) - (player.y + player.height));
    const answer_heard = world.call_markers.some(function is_answer(marker) { return !marker.own; });
    const both_matter = ['near_contact', 'final_obstacle'].includes(world.beat)
        || (world.reunion && world.reunion.stage === 'approach')
        || (gap_y > 60 && gap_x < 420)
        || (answer_heard && gap_x < 700);
    if (both_matter) {
        const top = Math.min(player.y, companion.y);
        const bottom = Math.max(player.y + player.height, companion.y + companion.height);
        const zoom_to_fit = Math.min(view_height / (bottom - top + 170), view_width / (gap_x + 300));
        target_zoom = Math.max(camera.wide_zoom, Math.min(camera.close_zoom, zoom_to_fit));
        focus_x = (center_x(player) + center_x(companion)) / 2 + camera.look_ahead * 0.4;
        focus_y = (top + bottom) / 2 + 20;
    }
    if (world.reunion && world.reunion.stage === 'embrace') {
        focus_x = center_x(player);
        focus_y = player.y + player.height * 0.55;
        target_zoom = camera.close_zoom * 1.3;
    }
    // Zoom glides rather than snaps.
    camera.zoom = smooth_follow(camera.zoom, target_zoom, camera, 'zoom_velocity', 0.7, delta_seconds);
    const visible_width = view_width / camera.zoom;
    const visible_height = view_height / camera.zoom;
    const lowest_center_y = game.level.height - visible_height / 2;
    // Put the ground she stands on about two thirds of the way down the screen (a common
    // framing in film and games), with her head well clear of the top edge.
    let target_y = Math.min(lowest_center_y, player.y + player.height - visible_height * 0.18, player.y - 40 + visible_height / 2);
    if (focus_y !== null) {
        target_y = Math.min(lowest_center_y + 30, focus_y);
    }
    const target_x = game.level.width <= visible_width ? game.level.width / 2
        : Math.max(visible_width / 2, Math.min(game.level.width - visible_width / 2, focus_x));
    // Ignore small jumps vertically: only re-aim up or down when she lands, or leaves the frame.
    if (camera.anchor_y === undefined || player.on_ground || Math.abs(target_y - camera.anchor_y) > visible_height * 0.25 || focus_y !== null) {
        camera.anchor_y = target_y;
    }
    camera.center_x = smooth_follow(camera.center_x, target_x, camera, 'velocity_x', 0.3, delta_seconds);
    camera.center_y = smooth_follow(camera.center_y, camera.anchor_y, camera, 'velocity_y', 0.45, delta_seconds);
}

// Ends the chapter: fades out, then shows the quiet scene that follows it.
function finish_level() {
    const world = game.world;
    if (world.complete) {
        return;
    }
    world.complete = true;
    game.unlocked_chapters = Math.max(game.unlocked_chapters, Math.min(levels.length, game.level_index + 2));
    save_progress();
    game.fade_target = 1;
    window.setTimeout(show_aftermath, 2200 / game.speed_multiplier);
}

// Shows the still scene after a chapter: its picture, then its lines, one at a time.
function show_aftermath() {
    document.body.classList.remove('playing');
    context.setTransform(1, 0, 0, 1, 0, 0);
    context.fillStyle = '#0d1118';
    context.fillRect(0, 0, canvas.width, canvas.height);
    game.screen = 'aftermath';
    game.aftermath_seconds = 0;
    page.subtitle.classList.remove('shown');
    page.objective.textContent = '';
    page.aftermath_text.textContent = '';
    page.aftermath_continue.hidden = true;
    const tally = keepsake_tally(game.level);
    page.aftermath_tally.textContent = 'Keepsakes found in this chapter: ' + tally.found + ' of ' + tally.total;
    page.aftermath_tally.classList.remove('shown');
    const aftermath = game.level.aftermath;
    set_music_mood(aftermath.mood || 'quiet');
    set_weather_sound(aftermath.rain || 0, 0);
    aftermath.lines.forEach(function add_line(line, index) {
        const paragraph = document.createElement('p');
        paragraph.className = 'aftermath_line';
        paragraph.textContent = personalize(line);
        page.aftermath_text.appendChild(paragraph);
        window.setTimeout(function reveal_line() { paragraph.classList.add('shown'); }, (1200 + index * 3000) / game.speed_multiplier);
    });
    window.setTimeout(function reveal_continue() {
        page.aftermath_continue.hidden = false;
        page.aftermath_tally.classList.add('shown');
        page.aftermath_continue.textContent = game.level_index + 1 < levels.length ? 'Continue' : 'Back to the beginning';
        page.aftermath_continue.focus({ preventScroll: true });
    }, (1600 + aftermath.lines.length * 3000) / game.speed_multiplier);
    show_screen(page.aftermath_screen);
    game.fade_target = 0;
    game.fade = 0;
}

// Leaves the quiet scene: on to the next chapter, or back to the title after the last.
function continue_from_aftermath() {
    if (game.level_index + 1 < levels.length) {
        start_level(game.level_index + 1);
    } else {
        game.world = null;
        hide_screen(page.aftermath_screen);
        show_title();
    }
}

// ---------- Drawing a frame ----------

// Draws the whole picture for this frame.
function render(delta_seconds) {
    context.setTransform(game.pixel_scale, 0, 0, game.pixel_scale, 0, 0);
    const time_seconds = game.clock_seconds;
    update_flipbook();
    if (!game.world || !game.level) {
        draw_title_backdrop(delta_seconds);
        return;
    }
    const world = game.world;
    const level = game.level;
    const camera = game.camera;
    const scenery = level.scenery;
    const blend = game.screen === 'play' && !game.paused ? game.interpolation : 1;

    // Draw everything at a point between the last two physics steps. On 120 Hz phone screens
    // this is the difference between smooth motion and a slight judder.
    const draw_center_x = camera.previous_center_x + (camera.center_x - camera.previous_center_x) * blend;
    const draw_center_y = camera.previous_center_y + (camera.center_y - camera.previous_center_y) * blend;
    const visible_width = view_width / camera.zoom;
    const visible_height = view_height / camera.zoom;
    const camera_left = draw_center_x - visible_width / 2;
    const camera_top = draw_center_y - visible_height / 2;
    game.draw_camera = { center_x: draw_center_x, center_y: draw_center_y, zoom: camera.zoom };

    draw_backdrop(context, level, game.backdrop_layers, camera_left, camera_top, view_width, time_seconds, world.lightning_seconds);
    if (scenery.rain) {
        update_and_draw_rain(context, game.rain_drops, view_width, scenery.rain, scenery.wind_slant || -0.25, delta_seconds, is_sheltered_on_screen, false);
    }

    const shake = camera.shake_seconds > 0 ? 6 * Math.min(1, camera.shake_seconds) : 0;
    context.save();
    context.translate(view_width / 2 + (Math.random() - 0.5) * shake, view_height / 2 + (Math.random() - 0.5) * shake);
    context.scale(camera.zoom, camera.zoom);
    context.translate(-draw_center_x, -draw_center_y);
    const left_edge = camera_left - 260;
    const right_edge = camera_left + visible_width + 260;

    for (const prop of level.props) {
        if (!prop.front && prop.x < right_edge && prop.x + (prop.w || 450) > left_edge) {
            draw_prop(context, prop, scenery, time_seconds);
        }
    }
    for (const solid of world.solids) {
        if (solid.x < right_edge && solid.x + solid.w > left_edge) {
            draw_solid(context, solid, scenery, time_seconds);
        }
    }
    const player = controlled_character();
    const companion = companion_character();
    for (const item of level.interactables) {
        if (item.x > left_edge && item.x < right_edge && !(item.hidden && !world.revealed.has(item.keepsake))) {
            const available = interactable_available(item, player) || interactable_available(item, companion);
            draw_interactable(context, item, time_seconds, available && in_reach(item, player) && interactable_available(item, player), available);
        }
    }
    if (world.ball && !world.ball.held_by) {
        draw_ball(context, world.ball);
    }
    for (const character of [companion, player]) {
        // Blend between physics steps for drawing only, then put the true position back.
        const true_x = character.x;
        const true_y = character.y;
        if (character.previous_x !== undefined) {
            character.x = character.previous_x + (true_x - character.previous_x) * blend;
            character.y = character.previous_y + (true_y - character.previous_y) * blend;
        }
        character.shadow_y = character.on_ground ? character.y + character.height : find_floor_below(character);
        if (character === player) {
            game.last_drawn_player_x = character.x;  // Where she is drawn, for judder tests.
        }
        draw_contact_shadow(context, character);
        if (character.kind === 'dog') {
            draw_dog(context, character, time_seconds);
        } else {
            draw_human(context, character, time_seconds);
        }
        character.x = true_x;
        character.y = true_y;
    }
    if (world.ball && world.ball.held_by === 'human') {
        draw_ball(context, world.ball);
    }
    for (const water of world.water) {
        draw_water(context, water, time_seconds);
    }
    draw_light_shafts(context, level.props, time_seconds, left_edge, right_edge);
    draw_light_effects(context, level.props, time_seconds, (scenery.rain || 0) > 0.3 || scenery.wet, left_edge, right_edge);
    for (const prop of level.props) {
        if (prop.front && prop.x < right_edge && prop.x + (prop.w || 450) > left_edge) {
            draw_prop(context, prop, scenery, time_seconds);
        }
    }
    draw_particles(context, world.particles);
    draw_call_markers(context, world.call_markers);
    context.restore();

    const vignette_strength = world.reunion && world.reunion.stage === 'embrace' ? 0.8 : 0.55;
    draw_lighting(context, light_canvas, collect_lights(), world.lightning_seconds > 0 ? 0 : scenery.darkness, scenery.darkness_color || 'rgb(6,9,16)', view_width, vignette_strength);
    if (scenery.mist) {
        draw_mist(context, view_width, camera_left, time_seconds, scenery.mist);
    }
    if (scenery.rain) {
        update_and_draw_rain(context, game.near_rain_drops, view_width, scenery.rain, scenery.wind_slant || -0.25, delta_seconds, is_sheltered_on_screen, true);
    }
    draw_foreground(context, scenery.foreground, view_width, camera_left * camera.zoom, time_seconds, scenery.foreground_color, world_to_screen(center_x(player), 0)[0]);
    // The prompt is drawn last, above the darkness, so it's always readable.
    context.save();
    context.translate(view_width / 2, view_height / 2);
    context.scale(camera.zoom, camera.zoom);
    context.translate(-draw_center_x, -draw_center_y);
    draw_prompt(context, world.prompt);
    context.restore();
    if (world.lightning_seconds > 0) {
        context.fillStyle = 'rgba(220,230,255,' + (world.lightning_seconds * 0.6).toFixed(2) + ')';
        context.fillRect(0, 0, view_width, view_height);
    }
    draw_offscreen_markers(context, world.call_markers);
    if (game.fade > 0.01) {
        context.fillStyle = 'rgba(13,17,24,' + game.fade.toFixed(3) + ')';
        context.fillRect(0, 0, view_width, view_height);
    }
}

// The top of the nearest floor under a character in mid-air, for its shadow.
function find_floor_below(character) {
    let floor_y = character.y + character.height + 400;
    const middle = center_x(character);
    for (const solid of game.world.solids) {
        if (solid.active && solid.kind !== 'invisible' && solid.x <= middle && solid.x + solid.w >= middle && solid.y >= character.y + character.height - 2) {
            floor_y = Math.min(floor_y, solid.y);
        }
    }
    return floor_y;
}

// True when a point on screen is indoors (under a roof), so rain isn't drawn there.
function is_sheltered_on_screen(screen_x, screen_y) {
    const camera = game.draw_camera;
    const world_x = (screen_x - view_width / 2) / camera.zoom + camera.center_x;
    const world_y = (screen_y - view_height / 2) / camera.zoom + camera.center_y;
    for (const shelter of game.shelters) {
        if (world_x >= shelter.x && world_x <= shelter.x + shelter.w && world_y >= shelter.y && world_y <= shelter.y + shelter.h + 80) {
            return true;
        }
    }
    return false;
}

// Turns a world position into a screen position, including the camera's zoom.
function world_to_screen(x, y) {
    const camera = game.draw_camera || { center_x: game.camera.center_x, center_y: game.camera.center_y, zoom: game.camera.zoom };
    return [
        (x - camera.center_x) * camera.zoom + view_width / 2,
        (y - camera.center_y) * camera.zoom + view_height / 2,
    ];
}

// Lists every light on screen: both characters carry a little warmth, plus lamps, windows
// and fires. During the embrace, the light around them slowly grows.
function collect_lights() {
    const world = game.world;
    const zoom = game.camera.zoom;
    const lights = [];
    const embrace = world.reunion && world.reunion.stage === 'embrace' ? Math.min(1, world.reunion.seconds / 3) : 0;
    for (const character of [world.characters.human, world.characters.dog]) {
        const is_player = character.kind === world.controlled;
        const position = world_to_screen(center_x(character), character.y + character.height * 0.5);
        lights.push({
            x: position[0], y: position[1], radius: ((is_player ? 170 : 120) + embrace * 240) * zoom, strength: is_player ? 0.8 : 0.65,
            glow_color: embrace > 0 ? '255,170,90' : null, glow_strength: 0.3 * embrace,
        });
    }
    for (const prop of game.level.props) {
        if (!['lamp', 'window', 'fire'].includes(prop.kind) || prop.off || (prop.kind === 'window' && !prop.lit && !prop.glow)) {
            continue;
        }
        let radius = prop.radius || (prop.kind === 'fire' ? 260 : prop.kind === 'window' ? 170 : 210);
        if (prop.flicker) {
            const flicker = Math.sin(game.clock_seconds * 23 + prop.x) * Math.sin(game.clock_seconds * 3.1 + prop.x);
            radius *= flicker > 0.85 ? 0.15 : 0.85 + flicker * 0.15;
        }
        let light_x = prop.x;
        let light_y = prop.y - 20;
        if (prop.kind === 'lamp') {
            // A lamp lights the space below it most: centre its light between bulb and floor.
            const bulb_y = prop.hanging ? prop.y + 42 : prop.y;
            light_y = (bulb_y + (prop.floor_y || bulb_y + 120)) / 2;
        } else if (prop.kind === 'window') {
            light_x = prop.x + (prop.w || 60) / 2;
            light_y = prop.y + (prop.h || 80) / 2;
        }
        const position = world_to_screen(light_x, light_y);
        if (position[0] < -radius * zoom || position[0] > view_width + radius * zoom) {
            continue;
        }
        const glow = split_rgba(prop.color || 'rgba(255,186,105,0.14)');
        lights.push({ x: position[0], y: position[1], radius: radius * zoom, strength: 0.95, glow_color: glow.rgb, glow_strength: glow.alpha * 1.6 });
    }
    return lights;
}

// Splits "rgba(r,g,b,a)" into "r,g,b" and the number a, remembering each answer.
const split_rgba_cache = new Map();
function split_rgba(color) {
    if (!split_rgba_cache.has(color)) {
        const parts = color.replace(/[^0-9.,]/g, '').split(',');
        split_rgba_cache.set(color, { rgb: parts.slice(0, 3).join(','), alpha: parts.length > 3 ? Number(parts[3]) : 1 });
    }
    return split_rgba_cache.get(color);
}

// Draws expanding rings where a call was heard.
function draw_call_markers(draw_context, markers) {
    for (const marker of markers) {
        const progress = 1 - marker.seconds / (marker.own ? 1.2 : 2.4);
        draw_context.strokeStyle = 'rgba(240,182,94,' + (0.7 * (1 - progress)).toFixed(3) + ')';
        draw_context.lineWidth = 2;
        for (const ring of [0, 0.3]) {
            const ring_progress = Math.max(0, progress - ring);
            draw_context.beginPath();
            draw_context.arc(marker.x, marker.y + 10, 12 + ring_progress * 70, 0, Math.PI * 2);
            draw_context.stroke();
        }
    }
}

// When an answering call comes from off screen, a small arrow at the edge points to it.
function draw_offscreen_markers(draw_context, markers) {
    for (const marker of markers) {
        if (marker.own) {
            continue;
        }
        const position = world_to_screen(marker.x, marker.y);
        const off_left = position[0] < 20;
        const off_right = position[0] > view_width - 20;
        if (!off_left && !off_right) {
            continue;
        }
        const arrow_x = off_left ? 26 : view_width - 26;
        const arrow_y = Math.max(60, Math.min(view_height - 60, position[1]));
        const pointing = off_left ? -1 : 1;
        draw_context.fillStyle = 'rgba(240,182,94,' + Math.min(0.9, marker.seconds / 1.5).toFixed(2) + ')';
        draw_context.beginPath();
        draw_context.moveTo(arrow_x + pointing * 12, arrow_y);
        draw_context.lineTo(arrow_x - pointing * 6, arrow_y - 10);
        draw_context.lineTo(arrow_x - pointing * 6, arrow_y + 10);
        draw_context.closePath();
        draw_context.fill();
    }
}

// Draws the small label above what "use" would do, like "E  Restore power".
function draw_prompt(draw_context, prompt) {
    const world = game.world;
    // Fade the label in and out, and keep the last one while it fades, so it never pops.
    if (prompt) {
        world.shown_prompt = prompt;
    }
    world.prompt_alpha = move_toward(world.prompt_alpha || 0, prompt ? 1 : 0, 1 / 60 * (prompt ? 6 : 4));
    const shown = world.shown_prompt;
    if (!shown || world.prompt_alpha <= 0.01) {
        set_touch_glow(null);
        return;
    }
    set_touch_glow(prompt ? prompt.control : null);
    const key_name = shown.control === 'low' ? 'S' : 'E';
    const text = game.last_input === 'touch' ? shown.text : key_name + '   ' + shown.text;
    draw_context.save();
    const text_scale = game.camera.wide_zoom / game.camera.zoom * 1.1;
    draw_context.translate(shown.x, shown.y);
    draw_context.scale(text_scale, text_scale);
    draw_context.translate(-shown.x, -shown.y);
    draw_context.globalAlpha = ease_in_out(world.prompt_alpha);
    draw_context.font = '700 13px "Atkinson Hyperlegible", Arial, sans-serif';
    const text_width = draw_context.measureText(text).width;
    const box_x = shown.x - text_width / 2 - 10;
    const box_y = shown.y - 22 - (1 - world.prompt_alpha) * 6;
    draw_context.fillStyle = 'rgba(13,17,24,0.72)';
    draw_context.beginPath();
    if (draw_context.roundRect) {
        draw_context.roundRect(box_x, box_y, text_width + 20, 24, 12);
    } else {
        draw_context.rect(box_x, box_y, text_width + 20, 24);
    }
    draw_context.fill();
    draw_context.fillStyle = '#efe6d4';
    draw_context.textBaseline = 'middle';
    draw_context.fillText(text, box_x + 10, box_y + 12.5);
    if (game.last_input !== 'touch') {
        draw_context.fillStyle = '#f0b65e';
        draw_context.fillText(key_name, box_x + 10, box_y + 12.5);
    }
    draw_context.restore();
}

// Makes the matching phone button glow when it would do something useful.
function set_touch_glow(control) {
    page.touch_act.classList.toggle('glow', control === 'act');
    page.touch_low.classList.toggle('glow', control === 'low');
}

// The title screen's background: rain over a dark, flooded street.
function draw_title_backdrop(delta_seconds) {
    const time_seconds = game.clock_seconds;
    const ground_y = view_height * 0.86;
    const sky = context.createLinearGradient(0, 0, 0, view_height);
    sky.addColorStop(0, '#0d131c');
    sky.addColorStop(0.75, '#1b2430');
    sky.addColorStop(1, '#0b0e14');
    context.fillStyle = sky;
    context.fillRect(0, 0, view_width, view_height);
    // A far, flooded skyline with a few windows still lit.
    const random = make_seeded_random(42);
    for (let x = -20; x < view_width; x += 60 + random() * 90) {
        const building_height = 80 + random() * 160;
        context.fillStyle = '#121923';
        context.fillRect(x, ground_y - 40 - building_height, 70 + random() * 40, building_height + 40);
        if (random() < 0.5) {
            context.fillStyle = 'rgba(240,182,94,0.45)';
            context.fillRect(x + 12 + random() * 30, ground_y - 30 - building_height * random(), 6, 9);
        }
    }
    context.fillStyle = '#0a0d12';
    context.fillRect(0, ground_y, view_width, view_height - ground_y);
    // One streetlamp, and the two of them standing in its light.
    const lamp_x = view_width * 0.5 - 70;
    const figure_scale = Math.min(2.1, view_height / 300);
    draw_prop(context, { kind: 'lamp', x: lamp_x, y: ground_y - 230 * figure_scale / 2.1 }, {}, time_seconds);
    draw_light_effects(context, [{ kind: 'lamp', x: lamp_x, y: ground_y - 230 * figure_scale / 2.1, floor_y: ground_y }], time_seconds, true, -1e6, 1e6);
    const human = { kind: 'human', x: -13, y: -86, width: 26, height: 86, facing: 1, pose: 'stand', walk_phase: 0, on_ground: true, ponytail_angle: Math.sin(time_seconds * 1.3) * 0.08 };
    const dog = { kind: 'dog', x: -23, y: -38, width: 46, height: 38, facing: -1, pose: 'sit', walk_phase: 0, wag: 0.35, fear: 0, on_ground: true };
    for (const [figure, offset] of [[human, 20], [dog, 78]]) {
        context.save();
        context.translate(lamp_x + offset * figure_scale, ground_y);
        context.scale(figure_scale, figure_scale);
        draw_contact_shadow(context, figure);
        if (figure.kind === 'dog') {
            draw_dog(context, figure, time_seconds);
        } else {
            draw_human(context, figure, time_seconds);
        }
        context.restore();
    }
    draw_mist(context, view_width, time_seconds * 4, time_seconds, 'rgba(150,170,195,0.06)');
    update_and_draw_rain(context, game.rain_drops, view_width, 1, -0.25, delta_seconds, null, false);
    update_and_draw_rain(context, game.near_rain_drops || (game.near_rain_drops = []), view_width, 1, -0.25, delta_seconds, null, true);
    draw_vignette(context, view_width, 0.6);
}

// ---------- Screens ----------

// Fades a screen in.
function show_screen(element) {
    element.classList.remove('faded');
}

// Fades a screen out.
function hide_screen(element) {
    element.classList.add('faded');
}

// Shows the title screen with the chapter list.
function show_title() {
    game.screen = 'title';
    document.body.classList.remove('playing');
    load_progress();
    load_keepsakes();
    page.chapter_list.textContent = '';
    levels.forEach(function add_choice(level, index) {
        const choice = document.createElement('button');
        choice.type = 'button';
        choice.className = 'chapter_choice';
        choice.disabled = index >= game.unlocked_chapters;
        const number = document.createElement('span');
        number.className = 'chapter_number';
        number.textContent = level.eyebrow.replace('Chapter ', '');
        const name = document.createElement('span');
        name.textContent = personalize(level.title);
        const tally = keepsake_tally(level);
        const count = document.createElement('span');
        count.className = 'chapter_keepsakes';
        count.textContent = tally.found + '/' + tally.total;
        choice.append(number, name, count);
        choice.addEventListener('click', function choose_chapter() {
            start_audio();
            start_level(index);
        });
        page.chapter_list.appendChild(choice);
    });
    document.getElementById('begin_button').textContent = game.unlocked_chapters > 1 ? 'Continue' : 'Begin';
    show_screen(page.title_screen);
}

// Pauses or resumes play.
function set_paused(paused) {
    if (game.screen !== 'play') {
        return;
    }
    game.paused = paused;
    if (paused) {
        show_screen(page.pause_screen);
    } else {
        hide_screen(page.pause_screen);
    }
}

// Fits the drawing surface to the window. Phones held upright show more sky above;
// the world is always at least 640 units wide so both routes stay readable.
function resize_canvas() {
    const device_ratio = window.devicePixelRatio || 1;
    if (game.render_scale === undefined) {
        const is_phone = window.matchMedia && window.matchMedia('(pointer: coarse)').matches;
        game.render_scale = Math.min(device_ratio, is_phone ? 1.5 : 2);
    }
    canvas.width = Math.round(window.innerWidth * game.render_scale);
    canvas.height = Math.round(window.innerHeight * game.render_scale);
    game.pixel_scale = Math.min(canvas.height / 540, canvas.width / minimum_view_width);
    view_width = canvas.width / game.pixel_scale;
    view_height = canvas.height / game.pixel_scale;
    // Close framing shows about 290 world units of height: she's roughly 30% of the screen,
    // the size characters are in phone games. Wide framing shows both floors at once.
    game.camera.close_zoom = Math.max(1, Math.min(view_height / 290, view_width / 520));
    game.camera.wide_zoom = Math.max(1, Math.min(view_height / 440, view_width / 640));
    game.camera.base_zoom = game.camera.close_zoom;
    game.camera.zoom = game.camera.close_zoom;
    // Darkness and light are soft, so a third of the resolution looks the same and costs a ninth.
    light_canvas.width = Math.ceil(view_width / 3);
    light_canvas.height = Math.ceil(view_height / 3);
    aftermath_canvas.width = canvas.width;
    aftermath_canvas.height = canvas.height;
    if (game.level) {
        game.backdrop_layers = render_backdrop(game.level, game.pixel_scale, view_width, view_height);
    }
    cut_puppet_parts(false);
}

// Watches how long frames take. If the device keeps falling behind (fewer than about
// 50 frames a second for over a second), draw at a slightly lower resolution. Smoothness
// matters more than sharpness for something in motion.
function adapt_resolution(delta_seconds) {
    if (game.screen !== 'play' || game.paused || delta_seconds <= 0 || delta_seconds > 0.25) {
        return;
    }
    game.average_frame_seconds = (game.average_frame_seconds || 1 / 60) * 0.95 + delta_seconds * 0.05;
    game.resolution_cooldown = Math.max(0, (game.resolution_cooldown || 2) - delta_seconds);
    if (game.average_frame_seconds > 1 / 50 && game.resolution_cooldown <= 0 && game.render_scale > 0.75) {
        game.render_scale = Math.max(0.75, game.render_scale - 0.25);
        game.resolution_cooldown = 2.5;
        game.average_frame_seconds = 1 / 60;
        resize_canvas();
    }
}

// ---------- Controls ----------

const key_controls = {
    ArrowLeft: 'left', KeyA: 'left', ArrowRight: 'right', KeyD: 'right',
    Space: 'jump', ArrowUp: 'jump', KeyW: 'jump', KeyE: 'act', Enter: 'act',
    KeyQ: 'call', KeyS: 'low', ArrowDown: 'low', Tab: 'switch',
};

// Turns a control being pressed or released into the input flags the world reads.
function set_control(control, pressed) {
    if (control === 'left' || control === 'right') {
        input[control] = pressed;
    } else if (pressed) {
        input[control + '_pressed'] = true;
    }
    if (control === 'low') {
        input.low_held = pressed;
    }
    if (control === 'jump') {
        input.jump_held = pressed;
    }
}

// Connects the keyboard and the phone buttons to the controls.
function connect_controls() {
    window.addEventListener('keydown', function on_key_down(event) {
        if (event.code === 'Escape' || event.code === 'KeyP') {
            set_paused(!game.paused);
            return;
        }
        const control = key_controls[event.code];
        if (!control || game.screen !== 'play') {
            return;
        }
        event.preventDefault();
        game.last_input = 'keyboard';
        if (!event.repeat) {
            set_control(control, true);
        }
    });
    window.addEventListener('keyup', function on_key_up(event) {
        const control = key_controls[event.code];
        if (control) {
            set_control(control, false);
        }
    });
    window.addEventListener('blur', function on_blur() {
        input.left = input.right = input.low_held = false;
    });
    // When the phone hides the game (a call, the home screen, the lock button), pause it and
    // silence the sound, so the player comes back to exactly where they were.
    document.addEventListener('visibilitychange', function on_visibility_change() {
        input.left = input.right = input.low_held = input.jump_held = false;
        if (document.hidden) {
            set_paused(true);
            if (audio.context) {
                audio.context.suspend();
            }
        } else if (audio.context) {
            audio.context.resume();
        }
    });
    for (const button of document.querySelectorAll('.touch_button')) {
        const control = button.dataset.control;
        const release = function release_button() {
            button.classList.remove('pressed');
            set_control(control, false);
        };
        button.addEventListener('pointerdown', function on_press(event) {
            event.preventDefault();
            game.last_input = 'touch';
            button.classList.add('pressed');
            if (button.setPointerCapture) {
                button.setPointerCapture(event.pointerId);
            }
            set_control(control, true);
        });
        button.addEventListener('pointerup', release);
        button.addEventListener('pointercancel', release);
        button.addEventListener('lostpointercapture', release);
    }
    window.addEventListener('touchstart', function on_first_touch() {
        document.body.classList.add('touch_mode');
        game.last_input = 'touch';
    }, { passive: true });
    if (window.matchMedia && window.matchMedia('(pointer: coarse)').matches) {
        document.body.classList.add('touch_mode');
        game.last_input = 'touch';
    }

    document.getElementById('begin_button').addEventListener('click', function begin() {
        start_audio();
        start_level(Math.max(0, game.unlocked_chapters - 1));
    });
    document.getElementById('chapters_button').addEventListener('click', function toggle_chapters() {
        page.chapter_list.hidden = !page.chapter_list.hidden;
    });
    document.getElementById('memory_button').addEventListener('click', function open_memories() {
        fill_memory_box();
        show_screen(page.memory_box);
    });
    document.getElementById('memory_close').addEventListener('click', function close_memories() { hide_screen(page.memory_box); });
    document.getElementById('pause_button').addEventListener('click', function pause() { set_paused(true); });
    document.getElementById('resume_button').addEventListener('click', function resume() { set_paused(false); });
    document.getElementById('restart_button').addEventListener('click', function restart() {
        set_paused(false);
        start_level(game.level_index);
    });
    document.getElementById('quit_button').addEventListener('click', function quit() {
        set_paused(false);
        game.world = null;
        game.level = null;
        page.subtitle.classList.remove('shown');
        page.objective.textContent = '';
        show_title();
    });
    page.sound_button.addEventListener('click', function sound() {
        page.sound_button.textContent = toggle_mute() ? 'Sound: off' : 'Sound: on';
    });
    page.aftermath_continue.addEventListener('click', continue_from_aftermath);
}

// ---------- The loop ----------

// Runs once per screen refresh: steps the world at a fixed rate, then draws.
function frame(timestamp) {
    const work_started = performance.now();
    const delta_seconds = Math.min(0.1, Math.max(0, (timestamp - game.last_frame_time) / 1000));
    game.last_frame_time = timestamp;
    game.clock_seconds += delta_seconds;
    if (game.screen === 'play' && !game.paused && game.world) {
        game.accumulator_seconds += delta_seconds * game.speed_multiplier;
        let steps = 0;
        while (game.accumulator_seconds >= fixed_step_seconds && steps < 40) {
            update_world(fixed_step_seconds);
            game.accumulator_seconds -= fixed_step_seconds;
            steps += 1;
        }
    }
    game.interpolation = Math.min(1, game.accumulator_seconds / fixed_step_seconds);
    adapt_resolution(delta_seconds);
    game.fade += (game.fade_target - game.fade) * Math.min(1, delta_seconds * 1.5);
    if (game.screen === 'aftermath') {
        game.aftermath_seconds += delta_seconds;
        draw_vignette_scene(aftermath_context, game.level.aftermath.scene, game.aftermath_seconds, aftermath_canvas.width, aftermath_canvas.height);
    } else {
        render(delta_seconds);
    }
    update_music();
    // Remember how long this frame's work took, so tests can check the game stays smooth.
    game.frame_costs.push(performance.now() - work_started);
    if (game.frame_costs.length > 600) {
        game.frame_costs.shift();
    }
    window.requestAnimationFrame(frame);
}

// A small set of controls for automated tests: they start chapters, walk a character to a
// spot, press buttons and read the state, so a script can play every chapter to the end.
window.until_we_find_home_debug = {
    start_level: function debug_start_level(index) { start_level(index); },
    skip_card: function debug_skip_card() { begin_play(); },
    set_speed: function debug_set_speed(multiplier) { game.speed_multiplier = multiplier; },
    walk_to: function debug_walk_to(x) { game.world.autopilot = { target_x: x }; },
    press: function debug_press(control) { set_control(control, true); set_control(control, false); },
    hold: function debug_hold(control, pressed) { set_control(control, pressed); },
    take_control: function debug_take_control(kind) { take_control(kind); },
    render_scale: function debug_render_scale() { return game.render_scale; },
    drawn_x: function debug_drawn_x() { return game.last_drawn_player_x === undefined ? null : game.last_drawn_player_x; },
    character_state: function debug_character_state(kind) {
        const character = game.world.characters[kind];
        return { x: character.x, y: character.y, velocity_x: character.velocity_x, velocity_y: character.velocity_y, facing: character.facing, facing_blend: character.facing_blend, lean: character.lean ? character.lean.value : 0, on_ground: character.on_ground, pose: character.pose, shown: character.flipbook_state ? character.flipbook_state.shown : null, shown_facing: character.flipbook_state ? character.flipbook_state.shown_facing : null };
    },
    flipbook_ready: function debug_flipbook_ready() { return flipbook.ready; },
    camera: function debug_camera() { return { center_x: game.camera.center_x, center_y: game.camera.center_y, zoom: game.camera.zoom }; },
    state: function debug_state() {
        const world = game.world;
        if (!world) {
            return { screen: game.screen };
        }
        return {
            screen: game.screen, chapter: game.level.id, beat: world.beat, controlled: world.controlled,
            human_x: Math.round(center_x(world.characters.human)), human_feet: Math.round(world.characters.human.y + world.characters.human.height),
            dog_x: Math.round(center_x(world.characters.dog)), dog_feet: Math.round(world.characters.dog.y + world.characters.dog.height),
            walking: Boolean(world.autopilot), prompt: world.prompt ? world.prompt.text : null,
            happened: Array.from(world.happened), complete: world.complete, reunion: world.reunion ? world.reunion.stage : null,
            switching: world.switching_allowed,
            keepsakes_found: Array.from(game.found_keepsakes),
            subtitle: page.subtitle.textContent,
        };
    },
    continue_from_aftermath: continue_from_aftermath,
    frame_costs: function debug_frame_costs() { const costs = game.frame_costs.slice(); game.frame_costs.length = 0; return costs; },
};

resize_canvas();
load_puppet_sheets();
load_flipbook_sheets();
window.addEventListener('resize', resize_canvas);
connect_controls();
show_title();
window.requestAnimationFrame(frame);
