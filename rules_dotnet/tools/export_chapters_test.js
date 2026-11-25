// Purpose: break-it tests for export_chapters.js. Bad chapter data must stop the export with a
// clear error instead of quietly writing broken or incomplete C#.
//
// Run from the repository root:
//   node rules_dotnet/tools/export_chapters_test.js
'use strict';

const assert = require('assert');
const exporter = require('./export_chapters.js');

const growth = { four_months: { size: 0.6, jump: 0.88 } };

// A small, valid chapter to change one thing at a time.
function good_chapter() {
    return {
        id: 'test_chapter', title: 'Test', eyebrow: '', place: '', dog_age: 'four_months',
        width: 100, height: 100, mood: 'calm', scenery: [], start: {}, solids: [], water: [], props: [],
        interactables: [{ who: 'human', x: 1, y: 2, label: 'Pull it', event: 'pulled', sprite: 'lever' }],
        zones: [{ x: 0, y: 0, w: 10, h: 10, who: 'both', event: 'met' }],
        rules: [{ on: 'pulled', say: ['Done.'], camera: null, schedule: { seconds: 1, event: 'later' } }],
        aftermath: { lines: ['After.'] },
    };
}

// Runs one check and prints its name.
function check(name, body) {
    body();
    console.log('ok - ' + name);
}

check('a valid chapter exports', function () {
    const code = exporter.export_chapter(good_chapter(), 0, growth);
    assert.match(code, /public static ChapterScript test_chapter\(\)/);
    assert.match(code, /schedule_event = "later"/);
    assert.doesNotMatch(code, /camera/);  // Drawing-only fields are left out.
});

check('an unknown field stops the export', function () {
    const chapter = good_chapter();
    chapter.rules[0].teleport = true;
    assert.throws(function () { exporter.export_chapter(chapter, 0, growth); }, /teleport/);
});

check('a position that is not a number stops the export', function () {
    const chapter = good_chapter();
    chapter.interactables[0].x = 'left';
    assert.throws(function () { exporter.export_chapter(chapter, 0, growth); }, /should be a number/);
});

check('a rule that could never run stops the export', function () {
    assert.throws(function () { exporter.export_rule({ say: ['Never.'] }, 'rule'); }, /neither "on" nor "when_all"/);
});

check('an unknown beat or character stops the export', function () {
    assert.throws(function () { exporter.export_rule({ on: 'a', beat: 'party' }, 'rule'); }, /party/);
    assert.throws(function () { exporter.export_thing({ who: 'cat', x: 0, y: 0, label: '', event: 'e' }, 'thing'); }, /cat/);
});

check('an unknown dog age stops the export', function () {
    const chapter = good_chapter();
    chapter.dog_age = 'ancient';
    assert.throws(function () { exporter.export_chapter(chapter, 0, growth); }, /dog_age/);
});

check('a chapter id that is not snake_case stops the export', function () {
    const chapter = good_chapter();
    chapter.id = 'Bad Id(); drop';
    assert.throws(function () { exporter.export_chapter(chapter, 0, growth); }, /snake_case/);
});

check('quotes, backslashes and line separators cannot break out of a C# string', function () {
    assert.strictEqual(exporter.text('a "quote" \\ back'), '"a \\"quote\\" \\\\ back"');
    assert.strictEqual(exporter.text('one two'), '"one\\u2028two"');
    assert.strictEqual(exporter.text('line\nbreak'), '"line\\nbreak"');
});

check('only wait_for steps become checkpoints', function () {
    const source = "const playthroughs = [\n  [['walk', 3], ['wait_for', 'a'], ['act'], ['wait_for', 'b']],\n];\nrest();";
    assert.deepStrictEqual(JSON.parse(JSON.stringify(exporter.read_checkpoints(source))), [['a', 'b']]);
});

console.log('All export_chapters checks passed.');
