// Purpose: break-it tests for the drawing tool's guards. The puppy and Biscuit are recoloured
// copies of Bodhi's painted sheet, found by colour. If a repaint of bodhi.svg drops one of those
// colours, the tool must stop and say so, not quietly leave the puppy or Biscuit half golden.
// Run from the repository root:  node art/tools/draw_frames_test.js
'use strict';

const assert = require('assert');
const tool = require('./draw_frames.js');

// Runs one check and prints its name, so a failure says which one.
function check(name, body) {
    body();
    console.log('ok  ' + name);
}

const definitions = tool.extract_definitions(tool.dog_sheet);

check('the painted sheet still has every colour the puppy and Biscuit swap', function () {
    tool.puppy_definitions(definitions);
    tool.check_biscuit_look(definitions);
});

check('a puppy colour missing from the sheet stops the tool', function () {
    const repainted = definitions.split('#e0a867').join('#123456');
    assert.throws(function () { tool.puppy_definitions(repainted); }, /puppy colours: #e0a867/);
});

check('a Biscuit change that finds nothing stops the tool', function () {
    // The fur filter's edge size lives only in the sheet's definitions, not in drawn frames.
    const repainted = definitions.split('scale="22"').join('scale="23"');
    assert.throws(function () { tool.check_biscuit_look(repainted); }, /biscuit_look: "scale="22""/);
});

check('Biscuit keeps no golden curl shading', function () {
    const grey = tool.biscuit_look(definitions);
    for (const golden of ['#8a5528', '#6e4220', '#c99a62']) {
        assert.strictEqual(grey.indexOf(golden), -1, golden + ' is still in Biscuit\'s coat');
    }
});

// ---------- Follow-through (springy ears, tail, hair and scarf) ----------

// Twenty drawings whose keyed tail angle jumps from 0 to 1 at drawing 5 and stays there.
const stepped = [];
for (let index = 0; index < 20; index++) {
    stepped.push({ tail: index < 5 ? 0 : 1 });
}

check('a loose part arrives late when the body changes', function () {
    const swung = tool.follow_through(stepped, { tail: 2.4 }, 4, false);
    assert.ok(swung[6].tail < 0.5, 'the tail should still be on its way one drawing after the change, was ' + swung[6].tail);
});

check('a loose part swings past where it is going, then settles', function () {
    const swung = tool.follow_through(stepped.concat(stepped.slice(5)), { tail: 2.4 }, 4, false);
    const highest = Math.max.apply(null, swung.map(function angle(pose) { return pose.tail; }));
    assert.ok(highest > 1.02, 'no swing past the target (highest ' + highest + ')');
});

check('a one-off move ends exactly on its keyed angle, so the next move starts there', function () {
    const swung = tool.follow_through(stepped, { tail: 2.4 }, 4, false);
    assert.strictEqual(swung[swung.length - 1].tail, 1);
});

check('a looping part joins up: the last drawing flows into the first', function () {
    const loop = [];
    for (let index = 0; index < 16; index++) {
        loop.push({ ear: Math.sin(2 * Math.PI * index / 16) });
    }
    const swung = tool.follow_through(loop, { ear: 1.8 }, 4, true);
    const steps = swung.map(function step(pose, index) { return Math.abs(swung[(index + 1) % 16].ear - pose.ear); });
    const largest_inside = Math.max.apply(null, steps.slice(0, 15));
    assert.ok(steps[15] <= largest_inside * 1.05, 'the join (' + steps[15] + ') jumps more than any other step (' + largest_inside + ')');
});

check('nothing the spring is not told about is changed', function () {
    const swung = tool.follow_through([{ tail: 0, head: [1, 2] }, { tail: 1, head: [3, 4] }], { tail: 1 }, 2, false);
    assert.deepStrictEqual(swung[1].head, [3, 4]);
});

check('a wag starts still, at its resting angle', function () {
    assert.strictEqual(tool.wag(0, 0.45, 0.8), 0.45);
    const first_step = tool.wag(0.01, 0.45, 0.8) - 0.45;
    const middle_step = tool.wag(0.26, 0.45, 0.8) - tool.wag(0.25, 0.45, 0.8);
    assert.ok(first_step < middle_step / 10, 'the wag sets off fast');
});

// ---------- The motion checker (check_motion.js) ----------

const motion = require('./check_motion.js');

check('every animation moves within the smoothness limits', function () {
    for (const [character, animations] of [['her', tool.her_animations], ['bodhi', tool.dog_animations]]) {
        for (const [name, frames] of Object.entries(animations)) {
            const m = motion.measure(character, name, frames, (tool.cycle_lengths[character] || {})[name]);
            assert.ok(m.pop <= motion.largest_pop, character + ' ' + name + ' pops ' + m.pop.toFixed(2) + ' at ' + m.pop_where);
            assert.ok(m.seam <= motion.largest_pop, character + ' ' + name + ' twitches at its loop join: ' + m.seam.toFixed(2));
            assert.ok(m.slide <= motion.largest_slide, character + ' ' + name + ' feet slide ' + m.slide.toFixed(2));
        }
    }
});

check('the checker catches a drawing that jumps out of line', function () {
    const frames = tool.dog_animations.stop.map(function copy(pose) { return Object.assign({}, pose); });
    frames[4] = Object.assign({}, frames[4], { head: [frames[4].head[0] + 4, frames[4].head[1]] });
    assert.ok(motion.measure('bodhi', 'stop', frames).pop > motion.largest_pop);
});

check('the checker catches a planted foot that skates', function () {
    const frames = tool.her_animations.walk.map(function copy(pose) { return Object.assign({}, pose); });
    const cycle = tool.cycle_lengths.her.walk;
    const slid = frames.map(function skate(pose) { return Object.assign({}, pose, { near_foot: [pose.near_foot[0] * 1.3, pose.near_foot[1]] }); });
    assert.ok(motion.measure('her', 'walk', slid, cycle).slide > motion.largest_slide);
});
