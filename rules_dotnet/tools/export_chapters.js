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

// Fields that only change how things look or sound. They belong to the Unity scene, not the rules.
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
    console.log('Wrote ' + levels.length + ' chapters to ' + path.relative(repository_root, chapters_folder) + ' and their checkpoints to ' + path.relative(repository_root, checkpoints_path));
}

// Run as a command it writes the files; loaded by the tests it only shares its functions.
if (require.main === module) {
    main();
}
module.exports = { export_chapter, export_rule, export_thing, export_zone, read_checkpoints, text };
