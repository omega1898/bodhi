// Purpose: turns the browser game's twelve chapters (prototype/web/js/levels.js) into C# data
// for the Unity rules (rules/Runtime/Chapters/), so the story is never retyped by hand. It also
// writes the browser playtest's checkpoints (prototype/web/tests/playtest.js) into a C# test
// file, so the C# robot must reach the same story events the browser robot does.
//
// Run from the repository root:
//   node rules_dotnet/tools/export_chapters.js [path/to/levels.js] [path/to/playtest.js]
//
// It stops with an error if a chapter uses a field it doesn't know, so a new browser feature
// can't be dropped without anyone noticing.
'use strict';

const fs = require('fs');
const path = require('path');
const vm = require('vm');

const repository_root = path.resolve(__dirname, '..', '..');
const levels_path = path.resolve(process.argv[2] || path.join(repository_root, 'prototype/web/js/levels.js'));
const playtest_path = path.resolve(process.argv[3] || path.join(repository_root, 'prototype/web/tests/playtest.js'));
const chapters_folder = path.join(repository_root, 'rules/Runtime/Chapters');
const checkpoints_path = path.join(repository_root, 'rules_dotnet/Bodhi.Rules.Tests/ChapterCheckpoints.cs');
const stages_folder = path.join(repository_root, 'game/Logic/Chapters');

// Fields that only change how things look or sound. They belong to the Unity scene, not the rules:
// export_stage writes them to game/Logic/Chapters/ for the scene, checking each one's shape.
const drawing_only = {
    chapter: ['width', 'height', 'mood', 'scenery', 'start', 'solids', 'water', 'props', 'guest'],
    thing: ['sound', 'sprite', 'pose'],
    zone: [],
    rule: ['camera', 'caption', 'pose', 'walk', 'face', 'sound', 'place', 'fade_in', 'mood', 'darkness',
        'hide', 'scent', 'effect', 'shake', 'water', 'carry', 'dog_runs_to', 'guest'],
};

// Fields the rules use, for the check below.
const rules_fields = {
    chapter: ['id', 'dog_age', 'eyebrow', 'title', 'place', 'bond', 'care_start', 'ball', 'interactables', 'zones', 'rules', 'aftermath'],
    thing: ['who', 'x', 'y', 'label', 'event', 'requires', 'until', 'needs_dog', 'keepsake', 'hidden', 'memory', 'care', 'food'],
    zone: ['x', 'y', 'w', 'h', 'who', 'event', 'requires', 'unless'],
    rule: ['on', 'when_all', 'unless', 'say', 'voice', 'objective', 'beat', 'control', 'switching', 'follow', 'fetch',
        'schedule', 'reunion', 'complete', 'bond', 'care', 'cutscene', 'remove', 'add'],
};

const beats = ['together', 'separation', 'human_section', 'dog_section', 'near_contact', 'final_obstacle', 'reunion', 'aftermath'];

// Stops the export with a clear message.
function fail(message) {
    throw new Error('export_chapters: ' + message);
}

// Checks every field of an object is known, either to the rules or as drawing-only.
function check_fields(kind, object, where) {
    for (const key of Object.keys(object)) {
        if (!rules_fields[kind].includes(key) && !drawing_only[kind].includes(key)) {
            fail(where + ' has a field the rules don\'t know: "' + key + '". Add it to export_chapters.js.');
        }
    }
}

// Writes a JavaScript string as a C# string literal. JSON's escapes are valid C#; the two
// Unicode line separators are escaped too, because C# reads them as line breaks.
function text(value) {
    return JSON.stringify(String(value)).split('\u2028').join('\\u2028').split('\u2029').join('\\u2029');
}

// Writes a number as C#. Refuses anything that isn't a finite number.
function number(value, where) {
    if (typeof value !== 'number' || !Number.isFinite(value)) {
        fail(where + ' should be a number, but is ' + JSON.stringify(value));
    }
    return String(value);
}

// Writes a list of event names as a C# string array.
function names(list, where) {
    if (!Array.isArray(list)) {
        fail(where + ' should be a list');
    }
    return 'new string[] { ' + list.map(text).join(', ') + ' }';
}

// Picks one of an enum's names, or stops if the value isn't one of them.
function choice(value, allowed, enum_name, where) {
    if (!allowed.includes(value)) {
        fail(where + ' is "' + value + '", which isn\'t one of ' + allowed.join(', '));
    }
    return enum_name + '.' + value;
}

// One thing you can use, as C#.
function export_thing(thing, where) {
    check_fields('thing', thing, where);
    const parts = [
        'event_name = ' + text(thing.event),
        'who = ' + choice(thing.who, ['human', 'dog'], 'Character', where + '.who'),
        'label = ' + text(thing.label || ''),
    ];
    if (thing.requires) parts.push('requires = ' + names(thing.requires, where + '.requires'));
    if (thing.until) parts.push('until = ' + names(thing.until, where + '.until'));
    if (thing.needs_dog) parts.push('needs_dog = true');
    if (thing.hidden) parts.push('hidden = true');
    if (thing.keepsake) parts.push('keepsake = ' + text(thing.keepsake));
    if (thing.memory) parts.push('memory_text = ' + text(thing.memory));
    if (thing.care) parts.push('care = ' + choice(thing.care, ['meal', 'treat', 'bath'], 'CareKind', where + '.care'));
    if (thing.food) parts.push('food = ' + number(thing.food, where + '.food'));
    parts.push('x = ' + number(thing.x, where + '.x'), 'y = ' + number(thing.y, where + '.y'));
    return 'new ChapterThing { ' + parts.join(', ') + ' }';
}

// One zone, as C#.
function export_zone(zone, where) {
    check_fields('zone', zone, where);
    const parts = [
        'event_name = ' + text(zone.event),
        'who = ' + choice(zone.who, ['human', 'dog', 'both'], 'ZoneWho', where + '.who'),
    ];
    if (zone.requires) parts.push('requires = ' + names(zone.requires, where + '.requires'));
    if (zone.unless) parts.push('unless = ' + names(zone.unless, where + '.unless'));
    parts.push('x = ' + number(zone.x, where + '.x'), 'y = ' + number(zone.y, where + '.y'),
        'width = ' + number(zone.w, where + '.w'), 'height = ' + number(zone.h, where + '.h'));
    return 'new ChapterZone { ' + parts.join(', ') + ' }';
}

// One story rule, as C#.
function export_rule(rule, where) {
    check_fields('rule', rule, where);
    const parts = [];
    if (rule.on) parts.push('on = ' + text(rule.on));
    if (rule.when_all) parts.push('when_all = ' + names(rule.when_all, where + '.when_all'));
    if (rule.unless) parts.push('unless = ' + names(rule.unless, where + '.unless'));
    if (!rule.on && !rule.when_all) fail(where + ' has neither "on" nor "when_all", so it could never run');
    if (rule.say) parts.push('lines = ' + names(rule.say, where + '.say'));
    if (rule.voice) parts.push('voice = ' + choice(rule.voice, ['her', 'dog', 'note'], 'StoryVoice', where + '.voice'));
    if (rule.objective !== undefined) parts.push('objective = ' + text(rule.objective));
    if (rule.beat) parts.push('beat = ' + choice(rule.beat, beats, 'ChapterBeat', where + '.beat'));
    if (rule.control) parts.push('control = ' + choice(rule.control, ['human', 'dog'], 'Character', where + '.control'));
    if (rule.switching !== undefined) parts.push('switching = ' + Boolean(rule.switching));
    if (rule.follow !== undefined) parts.push('follow = ' + Boolean(rule.follow));
    if (rule.fetch !== undefined) parts.push('fetch = ' + Boolean(rule.fetch));
    if (rule.schedule) {
        parts.push('schedule_seconds = ' + number(rule.schedule.seconds, where + '.schedule.seconds'),
            'schedule_event = ' + text(rule.schedule.event));
    }
    if (rule.reunion) parts.push('reunion = true');
    if (rule.complete) parts.push('complete = true');
    if (rule.bond) {
        if (rule.bond.fear) parts.push('fear_change = ' + number(rule.bond.fear, where + '.bond.fear'));
        if (rule.bond.closeness) parts.push('closeness_change = ' + number(rule.bond.closeness, where + '.bond.closeness'));
    }
    if (rule.care) {
        if (rule.care.hunger !== undefined) parts.push('care_hunger = ' + number(rule.care.hunger, where + '.care.hunger'));
        if (rule.care.dirt !== undefined) parts.push('care_dirt = ' + number(rule.care.dirt, where + '.care.dirt'));
        if (rule.care.food !== undefined) parts.push('care_food = ' + number(rule.care.food, where + '.care.food'));
    }
    if (rule.cutscene !== undefined) {
        if (rule.cutscene) {
            parts.push('cutscene = CutsceneChange.start');
            if (typeof rule.cutscene === 'object' && rule.cutscene.skip_to) parts.push('cutscene_skip_to = ' + text(rule.cutscene.skip_to));
        } else {
            parts.push('cutscene = CutsceneChange.end');
        }
    }
    if (rule.remove) parts.push('remove = ' + names(rule.remove, where + '.remove'));
    if (rule.add) parts.push('add = ' + names(rule.add, where + '.add'));
    return 'new StoryRule { ' + parts.join(', ') + ' }';
}

// The C# function name for a chapter, from its id (already snake_case).
function function_name(id) {
    if (!/^[a-z][a-z0-9_]*$/.test(id)) {
        fail('chapter id "' + id + '" isn\'t a plain snake_case name');
    }
    return id;
}

// The file name for a chapter: its place in the story and its id in PascalCase.
function file_name(index, id) {
    const pascal = id.split('_').map(function capital(word) { return word[0].toUpperCase() + word.slice(1); }).join('');
    return 'Chapter' + String(index + 1).padStart(2, '0') + pascal + '.cs';
}

const generated_note = '// Generated by rules_dotnet/tools/export_chapters.js from prototype/web/js/levels.js.\n' +
    '// Don\'t edit by hand: change levels.js and run the tool again.\n';

// One chapter's C# file.
function export_chapter(level, index, growth) {
    const where = 'chapter ' + level.id;
    check_fields('chapter', level, where);
    const age = growth[level.dog_age];
    if (!age) fail(where + ' has an unknown dog_age "' + level.dog_age + '"');
    const bond = Object.assign({ fear: 0.05, closeness: 0.3 }, level.bond || {});
    const care = level.care_start || {};
    const lines = [];
    lines.push('// Purpose: the chapter "' + level.title.replace(/[\r\n]/g, ' ') + '" as data for ChapterRun.');
    lines.push(generated_note.trimEnd());
    lines.push('namespace Bodhi.Rules', '{', '    public static partial class StoryChapters', '    {');
    lines.push('        // Chapter ' + (index + 1) + ': ' + level.title.replace(/[\r\n]/g, ' ') + '.');
    lines.push('        public static ChapterScript ' + function_name(level.id) + '()', '        {', '            return new ChapterScript', '            {');
    lines.push('                id = ' + text(level.id) + ',');
    lines.push('                title = ' + text(level.title) + ',');
    lines.push('                eyebrow = ' + text(level.eyebrow || '') + ',');
    lines.push('                place = ' + text(level.place || '') + ',');
    lines.push('                dog_age = ' + text(level.dog_age) + ',');
    lines.push('                dog_size = ' + number(age.size, where + ' dog size') + ',');
    lines.push('                dog_jump = ' + number(age.jump, where + ' dog jump') + ',');
    lines.push('                start_fear = ' + number(bond.fear, where + '.bond.fear') + ',');
    lines.push('                start_closeness = ' + number(bond.closeness, where + '.bond.closeness') + ',');
    if (care.hunger !== undefined) lines.push('                start_hunger = ' + number(care.hunger, where + '.care_start.hunger') + ',');
    if (care.dirt !== undefined) lines.push('                start_dirt = ' + number(care.dirt, where + '.care_start.dirt') + ',');
    if (care.food !== undefined) lines.push('                start_food = ' + number(care.food, where + '.care_start.food') + ',');
    if (level.ball) lines.push('                has_ball = true,');
    lines.push('                things = new ChapterThing[]', '                {');
    (level.interactables || []).forEach(function one(thing, at) { lines.push('                    ' + export_thing(thing, where + ' thing ' + at) + ','); });
    lines.push('                },', '                zones = new ChapterZone[]', '                {');
    (level.zones || []).forEach(function one(zone, at) { lines.push('                    ' + export_zone(zone, where + ' zone ' + at) + ','); });
    lines.push('                },', '                rules = new StoryRule[]', '                {');
    (level.rules || []).forEach(function one(rule, at) { lines.push('                    ' + export_rule(rule, where + ' rule ' + at) + ','); });
    lines.push('                },');
    lines.push('                aftermath = ' + names((level.aftermath && level.aftermath.lines) || [], where + '.aftermath.lines') + ',');
    lines.push('            };', '        }', '    }', '}', '');
    return lines.join('\n');
}

// The list of every chapter, in story order.
function export_index(levels) {
    const lines = [
        '// Purpose: every chapter, in story order, from Coming Home to Home.',
        generated_note.trimEnd(),
        'namespace Bodhi.Rules', '{', '    public static partial class StoryChapters', '    {',
        '        // Makes a fresh copy of every chapter, first to last.',
        '        public static ChapterScript[] all()', '        {', '            return new[]', '            {',
    ];
    for (const level of levels) {
        lines.push('                ' + function_name(level.id) + '(),');
    }
    lines.push('            };', '        }', '    }', '}', '');
    return lines.join('\n');
}

// ---------- The stage: what each chapter looks like, for the Unity scene ----------

const stage_who = ['human', 'dog', 'guest'];

// Checks an object has only the keys listed, so a new drawing field can't be dropped quietly.
function only_keys(object, allowed, where) {
    if (object === null || typeof object !== 'object' || Array.isArray(object)) {
        fail(where + ' should be an object, but is ' + JSON.stringify(object));
    }
    for (const key of Object.keys(object)) {
        if (!allowed.includes(key)) {
            fail(where + ' has a field the stage doesn\'t know: "' + key + '". Add it to export_chapters.js.');
        }
    }
}

// A text field that may be missing ("" then).
function optional_text(value, where) {
    if (value === undefined) return '""';
    if (typeof value !== 'string') fail(where + ' should be text, but is ' + JSON.stringify(value));
    return text(value);
}

// An [x, y] pair, as two C# numbers.
function pair(value, where) {
    if (!Array.isArray(value) || value.length !== 2) fail(where + ' should be [x, y], but is ' + JSON.stringify(value));
    return [number(value[0], where + '[0]'), number(value[1], where + '[1]')];
}

// Turns any value into StageSetting entries, naming nested values with dots ("marks.0.1").
function settings(value, name, where, out) {
    if (value === undefined) return out;
    if (value === null) {
        out.push('StageSetting.of_nothing(' + text(name) + ')');
    } else if (typeof value === 'boolean') {
        out.push('StageSetting.of_flag(' + text(name) + ', ' + value + ')');
    } else if (typeof value === 'number') {
        out.push('StageSetting.of_number(' + text(name) + ', ' + number(value, where) + ')');
    } else if (typeof value === 'string') {
        out.push('StageSetting.of_text(' + text(name) + ', ' + text(value) + ')');
    } else if (Array.isArray(value)) {
        value.forEach(function one(item, at) { settings(item, name + '.' + at, where + '[' + at + ']', out); });
    } else if (typeof value === 'object') {
        for (const key of Object.keys(value)) {
            settings(value[key], name ? name + '.' + key : key, where + '.' + key, out);
        }
    } else {
        fail(where + ' is a ' + typeof value + ', which the stage can\'t hold');
    }
    return out;
}

// A list of settings as a C# array.
function settings_array(list) {
    return list.length === 0 ? 'System.Array.Empty<StageSetting>()' : 'new StageSetting[] { ' + list.join(', ') + ' }';
}

// One floor, wall or gate.
function export_solid(solid, where) {
    only_keys(solid, ['id', 'kind', 'x', 'y', 'w', 'h', 'blocks', 'hidden', 'color'], where);
    const parts = ['kind = ' + text(solid.kind || 'floor')];
    if (solid.id !== undefined) parts.push('id = ' + optional_text(solid.id, where + '.id'));
    parts.push('x = ' + number(solid.x, where + '.x'), 'y = ' + number(solid.y, where + '.y'),
        'width = ' + number(solid.w, where + '.w'), 'height = ' + number(solid.h, where + '.h'));
    if (solid.blocks !== undefined) parts.push('blocks = ' + choice(solid.blocks, ['human', 'dog'], 'SolidBlocks', where + '.blocks'));
    if (solid.hidden) parts.push('hidden = true');
    if (solid.color !== undefined) parts.push('color = ' + optional_text(solid.color, where + '.color'));
    return 'new StageSolid { ' + parts.join(', ') + ' }';
}

// One pool, river or flood.
function export_water(water, where) {
    only_keys(water, ['id', 'x', 'y', 'w', 'h', 'flow', 'mud'], where);
    const parts = ['id = ' + optional_text(water.id, where + '.id'),
        'x = ' + number(water.x, where + '.x'), 'y = ' + number(water.y, where + '.y'),
        'width = ' + number(water.w, where + '.w'), 'height = ' + number(water.h, where + '.h')];
    if (water.flow !== undefined) parts.push('flow = ' + number(water.flow, where + '.flow'));
    if (water.mud) parts.push('mud = true');
    return 'new StageWater { ' + parts.join(', ') + ' }';
}

// One piece of furniture or scenery. Its own fields vary by kind, so they become settings.
function export_prop(prop, where) {
    if (prop === null || typeof prop !== 'object' || typeof prop.kind !== 'string' || prop.kind === '') {
        fail(where + ' needs a "kind"');
    }
    const until = prop.until === undefined ? [] : (Array.isArray(prop.until) ? prop.until : [prop.until]);
    until.forEach(function check_until(name, at) {
        if (typeof name !== 'string' || name === '') fail(where + '.until[' + at + '] should be an event name');
    });
    const extras = [];
    for (const key of Object.keys(prop)) {
        if (!['kind', 'x', 'y', 'until', 'after'].includes(key)) settings(prop[key], key, where + '.' + key, extras);
    }
    const parts = ['kind = ' + text(prop.kind), 'x = ' + number(prop.x, where + '.x'), 'y = ' + number(prop.y, where + '.y')];
    if (until.length) parts.push('until = ' + names(until, where + '.until'));
    if (prop.after !== undefined) parts.push('after = ' + optional_text(prop.after, where + '.after'));
    if (extras.length) parts.push('settings = ' + settings_array(extras));
    return 'new StageProp { ' + parts.join(', ') + ' }';
}

// How one thing you can use looks and sounds.
function export_look(thing, where) {
    return 'new StageThingLook { event_name = ' + text(thing.event) + ', sprite = ' + optional_text(thing.sprite, where + '.sprite') +
        ', sound = ' + optional_text(thing.sound, where + '.sound') + ', pose = ' + optional_text(thing.pose, where + '.pose') + ' }';
}

// The people named in an { human: ..., dog: ..., guest: ... } field, checked.
function each_who(object, allowed, where, make) {
    only_keys(object, allowed, where);
    return Object.keys(object).map(function one(who) { return make(who, object[who], where + '.' + who); });
}

// What one story rule does on screen.
function export_staging(rule, where) {
    const parts = [];
    if (rule.place !== undefined) {
        parts.push('place = new[] { ' + each_who(rule.place, stage_who, where + '.place', function make(who, value, at) {
            const xy = pair(value, at);
            return 'new StagePlacement { who = StageWho.' + who + ', x = ' + xy[0] + ', feet_y = ' + xy[1] + ' }';
        }).join(', ') + ' }');
    }
    if (rule.face !== undefined) {
        parts.push('face = new[] { ' + each_who(rule.face, stage_who, where + '.face', function make(who, value, at) {
            return 'new StageFacing { who = StageWho.' + who + ', direction = ' + (Number(number(value, at)) < 0 ? -1 : 1) + ' }';
        }).join(', ') + ' }');
    }
    if (rule.pose !== undefined) {
        parts.push('pose = new[] { ' + each_who(rule.pose, stage_who, where + '.pose', function make(who, value, at) {
            if (!Array.isArray(value) || value.length !== 2 || typeof value[0] !== 'string') fail(at + ' should be [pose, seconds]');
            return 'new StagePose { who = StageWho.' + who + ', pose = ' + text(value[0]) + ', seconds = ' + number(value[1], at + '[1]') + ' }';
        }).join(', ') + ' }');
    }
    if (rule.walk !== undefined) {
        parts.push('walk = new[] { ' + each_who(rule.walk, ['human', 'dog'], where + '.walk', function make(who, value, at) {
            return 'new StageWalk { who = StageWho.' + who + ', x = ' + number(value, at) + ' }';
        }).join(', ') + ' }');
    }
    if (rule.hide !== undefined) {
        parts.push('hide = new[] { ' + each_who(rule.hide, stage_who, where + '.hide', function make(who, value) {
            return 'new StageHiding { who = StageWho.' + who + ', hidden = ' + Boolean(value) + ' }';
        }).join(', ') + ' }');
    }
    if (rule.water !== undefined) {
        if (!Array.isArray(rule.water)) fail(where + '.water should be a list');
        parts.push('water = new[] { ' + rule.water.map(function one(change, at) {
            only_keys(change, ['id', 'top'], where + '.water[' + at + ']');
            return 'new StageWaterLevel { water_id = ' + text(change.id) + ', top = ' + number(change.top, where + '.water[' + at + '].top') + ' }';
        }).join(', ') + ' }');
    }
    if (rule.camera !== undefined) {
        if (rule.camera === null) {
            parts.push('camera = CameraChange.release');
        } else {
            only_keys(rule.camera, ['x', 'y', 'zoom'], where + '.camera');
            parts.push('camera = CameraChange.hold', 'camera_x = ' + number(rule.camera.x, where + '.camera.x'),
                'camera_y = ' + number(rule.camera.y, where + '.camera.y'));
            if (rule.camera.zoom !== undefined) parts.push('camera_zoom = ' + number(rule.camera.zoom, where + '.camera.zoom'));
        }
    }
    if (rule.darkness !== undefined) parts.push('darkness = ' + number(rule.darkness, where + '.darkness'));
    if (rule.mood !== undefined) parts.push('mood = ' + optional_text(rule.mood, where + '.mood'));
    if (rule.scent !== undefined) {
        if (rule.scent === null) {
            parts.push('scent = ScentChange.clear');
        } else {
            const xy = pair(rule.scent, where + '.scent');
            parts.push('scent = ScentChange.point', 'scent_x = ' + xy[0], 'scent_y = ' + xy[1]);
        }
    }
    if (rule.carry !== undefined) {
        only_keys(rule.carry, ['dog'], where + '.carry');
        if (rule.carry.dog === null) {
            parts.push('carry = CarryChange.drop');
        } else {
            parts.push('carry = CarryChange.carry', 'carry_item = ' + optional_text(rule.carry.dog, where + '.carry.dog'));
        }
    }
    if (rule.dog_runs_to !== undefined) parts.push('dog_runs_to = ' + number(rule.dog_runs_to, where + '.dog_runs_to'));
    if (rule.guest !== undefined) parts.push('guest_mode = ' + choice(rule.guest, ['still', 'follow', 'gone'], 'X', where + '.guest').replace(/^X\.(.*)$/, '"$1"'));
    if (rule.caption !== undefined) parts.push('caption = ' + optional_text(rule.caption, where + '.caption'));
    if (rule.sound !== undefined) parts.push('sound = ' + optional_text(rule.sound, where + '.sound'));
    if (rule.shake !== undefined) parts.push('shake_seconds = ' + number(rule.shake, where + '.shake'));
    if (rule.fade_in) parts.push('fade_in = true');
    if (rule.effect !== undefined) {
        only_keys(rule.effect, ['kind', 'x', 'y', 'width'], where + '.effect');
        const effect = ['kind = ' + optional_text(rule.effect.kind, where + '.effect.kind')];
        if (rule.effect.x !== undefined || rule.effect.y !== undefined) {
            effect.push('x = ' + number(rule.effect.x, where + '.effect.x'), 'y = ' + number(rule.effect.y, where + '.effect.y'), 'has_place = true');
        }
        if (rule.effect.width !== undefined) effect.push('width = ' + number(rule.effect.width, where + '.effect.width'));
        parts.push('effect = new StageEffect { ' + effect.join(', ') + ' }');
    }
    return 'new RuleStaging { ' + parts.join(', ') + ' }';
}

// The file name for a chapter's stage, like its story file's.
function stage_file_name(index, id) {
    return file_name(index, id).replace(/^Chapter/, 'Stage');
}

// One chapter's stage, as a C# file.
function export_stage(level, index) {
    const where = 'chapter ' + level.id;
    check_fields('chapter', level, where);
    const lines = [];
    lines.push('// Purpose: how the chapter "' + level.title.replace(/[\r\n]/g, ' ') + '" looks: its floors, water, props and staging.');
    lines.push(generated_note.trimEnd());
    lines.push('namespace Bodhi.Game', '{', '    public static partial class ChapterStages', '    {');
    lines.push('        // Chapter ' + (index + 1) + ': ' + level.title.replace(/[\r\n]/g, ' ') + '.');
    lines.push('        public static ChapterStage ' + function_name(level.id) + '()', '        {', '            return new ChapterStage', '            {');
    lines.push('                id = ' + text(level.id) + ',');
    lines.push('                width = ' + number(level.width, where + '.width') + ',');
    lines.push('                height = ' + number(level.height, where + '.height') + ',');
    lines.push('                mood = ' + optional_text(level.mood, where + '.mood') + ',');
    if (level.scenery !== undefined && (level.scenery === null || typeof level.scenery !== 'object' || Array.isArray(level.scenery))) {
        fail(where + '.scenery should be an object');
    }
    const scenery = settings(level.scenery, '', where + '.scenery', []);  // Free-form: sky colours, rain, fog...
    lines.push('                scenery = ' + settings_array(scenery) + ',');
    only_keys(level.start, ['human', 'dog'], where + '.start');
    const human = pair(level.start.human, where + '.start.human');
    const dog = pair(level.start.dog, where + '.start.dog');
    lines.push('                human_start_x = ' + human[0] + ', human_start_feet_y = ' + human[1] + ',');
    lines.push('                dog_start_x = ' + dog[0] + ', dog_start_feet_y = ' + dog[1] + ',');
    if (level.guest) {
        only_keys(level.guest, ['start', 'size', 'mode', 'pose'], where + '.guest');
        const start = pair(level.guest.start, where + '.guest.start');
        const guest = ['x = ' + start[0], 'feet_y = ' + start[1]];
        if (level.guest.size !== undefined) guest.push('size = ' + number(level.guest.size, where + '.guest.size'));
        if (level.guest.mode !== undefined) guest.push('mode = ' + choice(level.guest.mode, ['still', 'follow', 'gone'], 'X', where + '.guest.mode').replace(/^X\.(.*)$/, '"$1"'));
        if (level.guest.pose !== undefined) guest.push('pose = ' + optional_text(level.guest.pose, where + '.guest.pose'));
        lines.push('                guest = new StageGuest { ' + guest.join(', ') + ' },');
    }
    lines.push('                solids = new StageSolid[]', '                {');
    (level.solids || []).forEach(function one(solid, at) { lines.push('                    ' + export_solid(solid, where + ' solid ' + at) + ','); });
    lines.push('                },', '                water = new StageWater[]', '                {');
    (level.water || []).forEach(function one(water, at) { lines.push('                    ' + export_water(water, where + ' water ' + at) + ','); });
    lines.push('                },', '                props = new StageProp[]', '                {');
    (level.props || []).forEach(function one(prop, at) { lines.push('                    ' + export_prop(prop, where + ' prop ' + at) + ','); });
    lines.push('                },', '                things = new StageThingLook[]', '                {');
    (level.interactables || []).forEach(function one(thing, at) { lines.push('                    ' + export_look(thing, where + ' thing ' + at) + ','); });
    lines.push('                },', '                rules = new RuleStaging[]', '                {');
    (level.rules || []).forEach(function one(rule, at) { lines.push('                    ' + export_staging(rule, where + ' rule ' + at) + ','); });
    lines.push('                },');
    const after = level.aftermath || {};
    only_keys(after, ['scene', 'mood', 'rain', 'lines'], where + '.aftermath');
    const aftermath = ['scene = ' + optional_text(after.scene, where + '.aftermath.scene'), 'mood = ' + optional_text(after.mood, where + '.aftermath.mood')];
    if (after.rain !== undefined) aftermath.push('rain = ' + number(after.rain, where + '.aftermath.rain'));
    lines.push('                aftermath = new StageAftermath { ' + aftermath.join(', ') + ' },');
    lines.push('            };', '        }', '    }', '}', '');
    return lines.join('\n');
}

// The list of every stage, in story order, and a lookup by chapter id.
function export_stage_index(levels) {
    const lines = [
        '// Purpose: every chapter\'s stage, in story order, and a way to find one by its chapter.',
        generated_note.trimEnd(),
        'namespace Bodhi.Game', '{', '    public static partial class ChapterStages', '    {',
        '        // Makes a fresh copy of every stage, first to last.',
        '        public static ChapterStage[] all()', '        {', '            return new[]', '            {',
    ];
    for (const level of levels) {
        lines.push('                ' + function_name(level.id) + '(),');
    }
    lines.push('            };', '        }', '',
        '        // The stage for the chapter with this id, or null if there is none.',
        '        public static ChapterStage for_chapter(string chapter_id)', '        {',
        '            switch (chapter_id)', '            {');
    for (const level of levels) {
        lines.push('                case ' + text(level.id) + ': return ' + function_name(level.id) + '();');
    }
    lines.push('                default: return null;', '            }', '        }', '    }', '}', '');
    return lines.join('\n');
}

// Reads the browser robot's playthroughs and keeps each chapter's "wait_for" events.
function read_checkpoints(playtest_source) {
    const start = playtest_source.indexOf('const playthroughs = [');
    if (start < 0) fail('couldn\'t find "const playthroughs = [" in playtest.js');
    let depth = 0;
    let end = -1;
    for (let at = playtest_source.indexOf('[', start); at < playtest_source.length; at++) {
        if (playtest_source[at] === '[') depth++;
        if (playtest_source[at] === ']') depth--;
        if (depth === 0) { end = at + 1; break; }
    }
    if (end < 0) fail('the playthroughs list in playtest.js never closes');
    const context = {};
    vm.createContext(context);
    const playthroughs = vm.runInContext('(' + playtest_source.slice(playtest_source.indexOf('[', start), end) + ')', context, { timeout: 1000 });
    return playthroughs.map(function events(steps) {
        return steps.filter(function is_wait(step) { return step[0] === 'wait_for'; }).map(function name(step) { return step[1]; });
    });
}

// The test file holding each chapter's checkpoints.
function export_checkpoints(levels, checkpoints) {
    if (checkpoints.length !== levels.length) {
        fail('playtest.js has ' + checkpoints.length + ' playthroughs but levels.js has ' + levels.length + ' chapters');
    }
    const lines = [
        '// Purpose: the story events the browser robot (prototype/web/tests/playtest.js) waits for in',
        '// each chapter. The C# robot must reach every one, so both games tell the same story.',
        '// Generated by rules_dotnet/tools/export_chapters.js from prototype/web/tests/playtest.js.',
        '// Don\'t edit by hand: change playtest.js and run the tool again.',
        'namespace Bodhi.Rules.Tests', '{', '    static class ChapterCheckpoints', '    {',
        '        // Each chapter\'s id and the events its playthrough waits for, in order.',
        '        public static readonly (string chapter_id, string[] events)[] all =',
        '        {',
    ];
    levels.forEach(function one(level, index) {
        lines.push('            (' + text(level.id) + ', ' + names(checkpoints[index], 'checkpoints') + '),');
    });
    lines.push('        };', '    }', '}', '');
    return lines.join('\n');
}

// Reads levels.js in a sandbox and writes every file.
function main() {
    const context = {};
    vm.createContext(context);
    vm.runInContext(fs.readFileSync(levels_path, 'utf8') + '\n;this.exported_levels = levels; this.exported_growth = dog_growth;', context, { timeout: 2000 });
    const levels = context.exported_levels;
    const growth = context.exported_growth;
    if (!Array.isArray(levels) || levels.length === 0) fail('levels.js didn\'t define any chapters');
    fs.mkdirSync(chapters_folder, { recursive: true });
    for (const old of fs.readdirSync(chapters_folder)) {
        if (/^Chapter\d\d\w+\.cs$/.test(old)) fs.unlinkSync(path.join(chapters_folder, old));
    }
    levels.forEach(function write(level, index) {
        fs.writeFileSync(path.join(chapters_folder, file_name(index, level.id)), export_chapter(level, index, growth));
    });
    fs.writeFileSync(path.join(chapters_folder, 'StoryChapters.cs'), export_index(levels));
    fs.writeFileSync(checkpoints_path, export_checkpoints(levels, read_checkpoints(fs.readFileSync(playtest_path, 'utf8'))));
    fs.mkdirSync(stages_folder, { recursive: true });
    for (const old of fs.readdirSync(stages_folder)) {
        if (/^Stage\d\d\w+\.cs$/.test(old)) fs.unlinkSync(path.join(stages_folder, old));
    }
    levels.forEach(function write_stage(level, index) {
        fs.writeFileSync(path.join(stages_folder, stage_file_name(index, level.id)), export_stage(level, index));
    });
    fs.writeFileSync(path.join(stages_folder, 'ChapterStages.cs'), export_stage_index(levels));
    console.log('Wrote ' + levels.length + ' chapters to ' + path.relative(repository_root, chapters_folder) + ', their stages to ' +
        path.relative(repository_root, stages_folder) + ' and their checkpoints to ' + path.relative(repository_root, checkpoints_path));
}

// Run as a command it writes the files; loaded by the tests it only shares its functions.
if (require.main === module) {
    main();
}
module.exports = { export_chapter, export_rule, export_thing, export_zone, export_stage, export_staging, export_prop, read_checkpoints, settings, text };
