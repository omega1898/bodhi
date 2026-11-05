// Purpose: saving and continuing, the way a regular game does. While you play, the game saves
// itself at every story beat, every half minute, and whenever you pause or the phone hides the
// game. "Continue" on the title screen puts you back where you were, mid-chapter.
//
// How: a save doesn't copy the whole world. It lists the story events that have happened in
// the chapter so far, in order, plus where the two of them stand and how Bodhi is. Loading
// builds the chapter fresh and quietly replays those events through the same story rules, so
// gates open, water drains and control passes exactly as it did the first time.
//
// The save lives on the player's device, where it can be edited, cut short or left by another
// version of the game, so loading treats it as hostile (see read_save). A save that fails any
// check is refused and counted, and the chapter starts from its beginning instead.
'use strict';

const save_storage_key = 'until_we_find_home_save';
const save_version = 1;
const largest_save_characters = 16000;  // A real save is about 3000. Anything far bigger is refused unread.
const largest_saved_events = 400;
const autosave_seconds = 30;

// Story events the engine itself fires (not named in the chapter data), which can appear in
// any chapter's save. Calls and sniffs also fire a version naming the story beat, for example
// "human_called_during_human_section"; those are allowed for every beat the chapter uses.
const engine_event_names = [
    'level_started', 'dog_petted', 'dog_leaned_in', 'knelt_by_dog', 'ball_thrown', 'ball_fetched',
    'human_called', 'dog_called', 'human_answered', 'dog_answered', 'dog_sniffed',
    'reunion_embrace', 'reunion_complete', 'dog_fed', 'dog_cleaned', 'dog_bathed', 'dog_shook_off',
    'dog_hungry', 'food_found', 'cutscene_skipped',
];

// Every event name that can happen in a chapter, worked out from its data. A save naming
// anything else didn't come from this version of the game.
function known_events_of(level) {
    const names = new Set(engine_event_names);
    const beats = new Set(['together']);
    for (const rule of level.rules) {
        if (rule.on) {
            names.add(rule.on);
        }
        for (const name of rule.when_all || []) {
            names.add(name);
        }
        if (rule.schedule) {
            names.add(rule.schedule.event);
        }
        if (rule.beat) {
            beats.add(rule.beat);
        }
    }
    for (const item of level.interactables) {
        names.add(item.event);
    }
    for (const zone of level.zones || []) {
        names.add(zone.event);
    }
    for (const beat of beats) {
        names.add('human_called_during_' + beat);
        names.add('dog_called_during_' + beat);
        names.add('dog_sniffed_during_' + beat);
    }
    return names;
}

// The events that end a chapter. A mid-chapter save never contains one: the game stops saving
// mid-chapter once the chapter is finishing. One in a save means someone edited it.
function chapter_ending_events_of(level) {
    const names = new Set();
    for (const rule of level.rules) {
        if (rule.complete) {
            for (const name of [rule.on].concat(rule.when_all || [])) {
                if (name) {
                    names.add(name);
                }
            }
        }
    }
    return names;
}

// True when now is a good moment to save: playing, nobody in mid-air, no film or reunion
// running, the chapter not already finishing.
function safe_to_save() {
    const world = game.world;
    if (game.screen !== 'play' || !world || world.complete || world.cutscene || world.replaying) {
        return false;
    }
    if (world.reunion && world.reunion.stage !== 'done') {
        return false;
    }
    return world.characters.human.on_ground && world.characters.dog.on_ground;
}

// Writes the save to the device's storage, ignoring storage that refuses (private browsing).
function write_save(save) {
    try {
        window.localStorage.setItem(save_storage_key, JSON.stringify(save));
        game.last_saved_at = Date.now();
        return true;
    } catch (storage_error) {
        return false;
    }
}

// Saves the chapter in progress, if now is a safe moment. Returns true when it saved.
function save_checkpoint() {
    if (!safe_to_save()) {
        return false;
    }
    const world = game.world;
    const human = world.characters.human;
    const dog = world.characters.dog;
    const rounded = function rounded(value) { return Math.round(value * 10) / 10; };
    return write_save({
        version: save_version,
        chapter: game.level.id,
        events: world.event_order.slice(0, largest_saved_events),
        // Positions at full precision: rounded, a character standing flush against a wall
        // could come back a hair inside it.
        human: [center_x(human), human.y + human.height],
        dog: [center_x(dog), dog.y + dog.height],
        controlled: world.controlled,
        dog_run_target: world.dog_run_target,
        care: checked_care(world.care),
        bond: { fear: rounded(world.bond.fear * 100) / 100, closeness: rounded(world.bond.closeness * 100) / 100 },
        seconds: rounded(world.elapsed_seconds),
    });
}

// Saves "at the start of this chapter" (after finishing the one before), with how Bodhi is now.
function save_chapter_start(level_index) {
    const level = levels[level_index];
    if (!level) {
        return false;
    }
    return write_save({ version: save_version, chapter: level.id, events: [], care: checked_care(game.care) });
}

// Forgets the save (a new game, or the story finished).
function clear_save() {
    try {
        window.localStorage.removeItem(save_storage_key);
    } catch (storage_error) {
        // Nothing to clear.
    }
}

// Counts a refused save, the way core/ counts refused input, so a test or a curious player can
// see that it happened.
function refuse_save(reason) {
    game.refused_saves = (game.refused_saves || 0) + 1;
    game.last_refusal = reason;
    return null;
}

// True when `point` is two finite numbers inside the chapter: [x, feet y].
function point_inside_level(point, level) {
    return Array.isArray(point) && point.length === 2
        && typeof point[0] === 'number' && Number.isFinite(point[0]) && point[0] >= 0 && point[0] <= level.width
        && typeof point[1] === 'number' && Number.isFinite(point[1]) && point[1] >= 0 && point[1] <= level.height;
}

// Reads and checks the save. Returns a clean copy built only from checked values, or null
// (with the reason counted) when there's no save or any check fails. `text` lets tests pass a
// save directly instead of the stored one.
function read_save(text) {
    let raw = text;
    if (raw === undefined) {
        try {
            raw = window.localStorage.getItem(save_storage_key);
        } catch (storage_error) {
            return null;
        }
    }
    if (raw === null || raw === undefined || raw === '') {
        return null;
    }
    if (typeof raw !== 'string' || raw.length > largest_save_characters) {
        return refuse_save('too big');
    }
    let parsed;
    try {
        parsed = JSON.parse(raw);
    } catch (parse_error) {
        return refuse_save('not readable');
    }
    if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) {
        return refuse_save('not a save');
    }
    if (parsed.version !== save_version) {
        return refuse_save('unknown version');
    }
    const level_index = levels.findIndex(function same_id(level) { return level.id === parsed.chapter; });
    if (level_index === -1) {
        return refuse_save('unknown chapter');
    }
    const level = levels[level_index];
    if (!Array.isArray(parsed.events) || parsed.events.length > largest_saved_events) {
        return refuse_save('bad event list');
    }
    const known = known_events_of(level);
    const ending = chapter_ending_events_of(level);
    const seen = new Set();
    for (const name of parsed.events) {
        if (typeof name !== 'string' || name.length > 80 || !known.has(name)) {
            return refuse_save('unknown event');
        }
        if (seen.has(name)) {
            return refuse_save('repeated event');
        }
        if (ending.has(name)) {
            return refuse_save('chapter-ending event');
        }
        seen.add(name);
    }
    const clean = { level_index: level_index, events: parsed.events.slice(), care: checked_care(parsed.care), at_start: parsed.events.length === 0 };
    if (clean.at_start) {
        return clean;
    }
    if (parsed.events[0] !== 'level_started') {
        return refuse_save('does not start at the start');
    }
    if (!point_inside_level(parsed.human, level) || !point_inside_level(parsed.dog, level)) {
        return refuse_save('position outside the chapter');
    }
    if (parsed.controlled !== 'human' && parsed.controlled !== 'dog') {
        return refuse_save('unknown character');
    }
    const fraction = function fraction(value, fallback) {
        return typeof value === 'number' && Number.isFinite(value) ? clamp_between(value, 0, 1) : fallback;
    };
    const bond = parsed.bond !== null && typeof parsed.bond === 'object' ? parsed.bond : {};
    clean.human = parsed.human.slice();
    clean.dog = parsed.dog.slice();
    clean.controlled = parsed.controlled;
    clean.bond = { fear: fraction(bond.fear, 0.05), closeness: fraction(bond.closeness, 0.3) };
    clean.seconds = typeof parsed.seconds === 'number' && Number.isFinite(parsed.seconds) ? clamp_between(parsed.seconds, 0, 1e6) : 0;
    const target = parsed.dog_run_target;
    clean.dog_run_target = typeof target === 'number' && Number.isFinite(target) && target >= 0 && target <= level.width ? target : null;
    return clean;
}

// Starts the saved chapter. The chapter card shows as usual; then, instead of starting fresh,
// begin_play hands over to restore_progress.
function continue_from_save(save) {
    game.care = checked_care(save.care);
    game.pending_restore = save.at_start ? null : save;
    start_level(save.level_index);
}

// Replays the saved events into the freshly built chapter, silently, then puts the characters
// and numbers where they were. Returns false (and the chapter starts over) if the result
// doesn't make sense, for example someone standing inside a wall.
function restore_progress(save) {
    const world = game.world;
    world.replaying = true;
    for (const name of save.events) {
        fire_event(name);
    }
    world.replaying = false;
    // Everything that animates when it happens is shown as already done.
    for (const water of world.water) {
        water.current_top = water.target_top;
    }
    for (const solid of world.solids) {
        solid.motion = null;
        solid.fade = solid.active ? 1 : 0;
    }
    world.particles = [];
    world.subtitle_queue = [];
    if (world.happened.has('reunion_complete')) {
        world.reunion = { stage: 'done', seconds: 0 };
        world.follow = true;
    }
    // The characters go back where they stood, if that's somewhere they could stand.
    for (const kind of ['human', 'dog']) {
        const character = world.characters[kind];
        const x = free_spot_near(character, save[kind][0] - character.width / 2, save[kind][1] - character.height);
        const y = save[kind][1] - character.height;
        if (x === null) {
            refuse_save('someone inside a wall');
            return false;
        }
        character.x = x;
        character.y = y;
        character.previous_x = x;
        character.previous_y = y;
        character.velocity_x = 0;
        character.velocity_y = 0;
        character.kneeling = false;
        character.safe_x = x;
        character.safe_y = y;
    }
    world.autopilot = null;
    world.dog_run_target = save.dog_run_target;
    take_control(save.controlled);
    world.bond = Object.assign({}, save.bond);
    world.care = checked_care(save.care);
    world.elapsed_seconds = save.seconds;
    set_objective(world.objective);
    set_music_mood(world.mood);
    update_touch_labels();
    snap_camera();
    show_toast('Continued from your last save');
    return true;
}

// Where a character can stand at height `y`, at `x` or within two units of it (someone saved
// while touching a wall), or null if there's nowhere: they'd be inside something solid.
function free_spot_near(character, x, y) {
    for (const nudge of [0, 0.5, -0.5, 1, -1, 2, -2]) {
        if (!position_is_blocked(character, x + nudge, y)) {
            return x + nudge;
        }
    }
    return null;
}

// Saves every so often while playing, and right after story moments asked for it.
function update_autosave(delta_seconds) {
    const world = game.world;
    world.autosave_timer = (world.autosave_timer === undefined ? autosave_seconds : world.autosave_timer) - delta_seconds;
    if ((world.save_wanted || world.autosave_timer <= 0) && safe_to_save()) {
        save_checkpoint();
        world.save_wanted = false;
        world.autosave_timer = autosave_seconds;
    }
}

// "Saved just now", "Saved 3 minutes ago": for the pause screen.
function saved_how_long_ago() {
    if (!game.last_saved_at) {
        return 'Not saved yet';
    }
    const minutes = Math.floor((Date.now() - game.last_saved_at) / 60000);
    return minutes < 1 ? 'Saved just now' : 'Saved ' + minutes + (minutes === 1 ? ' minute ago' : ' minutes ago');
}
