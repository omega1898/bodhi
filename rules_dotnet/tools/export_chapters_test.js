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

// A small, valid stage to change one thing at a time.
function good_stage() {
    const chapter = good_chapter();
    chapter.scenery = { sky_top: '#fff', rain: 0.3, art: { far: 'far.svg' }, mid: null, wet: true };
    chapter.start = { human: [10, 90], dog: [20, 90] };
    chapter.solids = [{ kind: 'floor', x: 0, y: 90, w: 100, h: 10 }, { id: 'gate', kind: 'gate', x: 50, y: 60, w: 6, h: 30, blocks: 'dog' }];
    chapter.water = [{ id: 'pond', x: 60, y: 80, w: 20, h: 20, flow: -40, mud: true }];
    chapter.props = [{ kind: 'lamp', x: 5, y: 5, radius: 200, until: 'pulled', marks: [[18, 'a mark']] }];
    chapter.rules[0].camera = { x: 1, y: 2, zoom: 1.5 };
    chapter.rules[0].water = [{ id: 'pond', top: 70 }];
    chapter.rules[0].scent = null;
    chapter.rules[0].carry = { dog: 'glove' };
    chapter.rules[0].face = { guest: -3 };
    chapter.aftermath.scene = 'bedtime';
    return chapter;
}

check('a valid stage exports every drawing field', function () {
    const code = exporter.export_stage(good_stage(), 0);
    assert.match(code, /public static ChapterStage test_chapter\(\)/);
    assert.match(code, /StageSetting\.of_text\("art\.far", "far\.svg"\)/);
    assert.match(code, /StageSetting\.of_nothing\("mid"\)/);
    assert.match(code, /StageSetting\.of_flag\("wet", true\)/);
    assert.match(code, /human_start_x = 10, human_start_feet_y = 90/);
    assert.match(code, /blocks = SolidBlocks\.dog/);
    assert.match(code, /flow = -40, mud = true/);
    assert.match(code, /until = new string\[\] \{ "pulled" \}/);
    assert.match(code, /StageSetting\.of_number\("marks\.0\.0", 18\), StageSetting\.of_text\("marks\.0\.1", "a mark"\)/);
    assert.match(code, /camera = CameraChange\.hold, camera_x = 1, camera_y = 2, camera_zoom = 1\.5/);
    assert.match(code, /water_id = "pond", top = 70/);
    assert.match(code, /scent = ScentChange\.clear/);
    assert.match(code, /carry = CarryChange\.carry, carry_item = "glove"/);
    assert.match(code, /who = StageWho\.guest, direction = -1/);
    assert.match(code, /sprite = "lever"/);
    assert.match(code, /scene = "bedtime"/);
});

check('an unknown field inside a drawing field stops the stage export', function () {
    const bad_camera = good_stage();
    bad_camera.rules[0].camera.tilt = 3;
    assert.throws(function () { exporter.export_stage(bad_camera, 0); }, /tilt/);
    const bad_solid = good_stage();
    bad_solid.solids[0].bouncy = true;
    assert.throws(function () { exporter.export_stage(bad_solid, 0); }, /bouncy/);
    const bad_who = good_stage();
    bad_who.rules[0].place = { cat: [1, 2] };
    assert.throws(function () { exporter.export_stage(bad_who, 0); }, /cat/);
    const bad_guest = good_stage();
    bad_guest.rules[0].guest = 'dance';
    assert.throws(function () { exporter.export_stage(bad_guest, 0); }, /dance/);
});

check('a missing start or a broken place stops the stage export', function () {
    const no_start = good_stage();
    delete no_start.start.dog;
    assert.throws(function () { exporter.export_stage(no_start, 0); }, /start\.dog/);
    const bad_place = good_stage();
    bad_place.rules[0].place = { human: [1] };
    assert.throws(function () { exporter.export_stage(bad_place, 0); }, /\[x, y\]/);
    const bad_prop = good_stage();
    bad_prop.props[0].until = [''];
    assert.throws(function () { exporter.export_stage(bad_prop, 0); }, /event name/);
    const kindless = good_stage();
    delete kindless.props[0].kind;
    assert.throws(function () { exporter.export_stage(kindless, 0); }, /kind/);
});

check('text in a prop cannot break out of its C# string', function () {
    const chapter = good_stage();
    chapter.props[0].text = '"); System.IO.File.Delete("x';
    assert.match(exporter.export_stage(chapter, 0), /of_text\("text", "\\"\); System\.IO\.File\.Delete\(\\"x"\)/);
});

console.log('All export_chapters checks passed.');
