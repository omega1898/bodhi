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
    const repainted = definitions.split('#d4925a').join('#123456');
    assert.throws(function () { tool.puppy_definitions(repainted); }, /puppy colours: #d4925a/);
});

check('a Biscuit change that finds nothing stops the tool', function () {
    // The fur filter's edge size lives only in the sheet's definitions, not in drawn frames.
    const repainted = definitions.split('scale="22"').join('scale="23"');
    assert.throws(function () { tool.check_biscuit_look(repainted); }, /biscuit_look: "scale="22""/);
});

check('Biscuit keeps no golden curl shading', function () {
    const grey = tool.biscuit_look(definitions);
    for (const golden of ['#7e4421', '#5e3218', '#c4895a']) {
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
    const slid = frames.map(function skate(pose) { return Object.assign({}, pose, { near_foot: [pose.near_foot[0] * 1.6, pose.near_foot[1]] }); });
    assert.ok(motion.measure('her', 'walk', slid, cycle).slide > motion.largest_slide);
});

// ---------- Walking like a real person and a real dog (see gaits.js) ----------

// The drawings at which a foot comes down: in the air in one drawing, flat on the floor in the
// next. `foot_of(pose)` gives that foot's [x, y].
function landings(frames, foot_of) {
    const found = [];
    for (let index = 0; index < frames.length; index++) {
        const before = foot_of(frames[(index - 1 + frames.length) % frames.length]);
        if (before[1] < 0 && foot_of(frames[index])[1] === 0) {
            found.push(index);
        }
    }
    return found;
}

// The order a dog's paws come down in, with how far apart (as a share of the cycle).
function paw_order(frames) {
    const names = ['near_front', 'far_front', 'near_back', 'far_back'];
    const down = names.map(function first_landing(name) {
        return { name: name, at: landings(frames, function paw(pose) { return pose.legs[name]; })[0] };
    });
    return down.sort(function by_time(first, second) { return first.at - second.at; });
}

// True when a dog's walk is a real one: each paw down on its own, a quarter of a cycle apart,
// in the order back, front on one side, then back, front on the other, with at least two paws
// always down.
function walks_like_a_dog(frames) {
    const order = paw_order(frames);
    if (order.some(function missing(paw) { return paw.at === undefined; })) {
        return false;
    }
    const start = order.findIndex(function back(paw) { return paw.name === 'near_back'; });
    const sequence = order.slice(start).concat(order.slice(0, start)).map(function name(paw) { return paw.name; });
    const quarter = frames.length / 4;
    const evenly = order.every(function spaced(paw, index) {
        const next = order[(index + 1) % 4];
        const gap = (next.at - paw.at + frames.length) % frames.length;
        return Math.abs(gap - quarter) <= 1;
    });
    const always_two_down = frames.every(function supported(pose) {
        return Object.values(pose.legs).filter(function down(paw) { return paw[1] === 0; }).length >= 2;
    });
    return sequence.join(' ') === 'near_back near_front far_back far_front' && evenly && always_two_down;
}

check('Em lands on her heel with her toes up, and pushes off with her heel up', function () {
    const walk = tool.her_animations.walk;
    const strike = landings(walk, function near(pose) { return pose.near_foot; });
    assert.ok(walk[0].near_foot_angle < -0.2, 'toes up as the heel lands');
    assert.strictEqual(walk[0].near_foot[1] < 0, true, 'only the heel is down, so the foot point is just off the floor');
    assert.ok(strike.length === 1 && strike[0] > 0 && strike[0] <= walk.length * 0.12, 'rolls flat within the first tenth of the step');
    const push_off = walk[Math.round(walk.length * 0.58)];
    assert.ok(push_off.near_foot_angle > 0.4, 'heel up as the toes push off');
});

check('each of her feet is down about 60% of the cycle, so both are down for a moment each step', function () {
    const walk = tool.her_animations.walk;
    const near_down = walk.filter(function down(pose) { return pose.near_foot[1] > -1; }).length / walk.length;
    assert.ok(near_down > 0.55 && near_down < 0.72, 'near foot down ' + near_down);
    const both_down = walk.filter(function both(pose) { return pose.near_foot[1] > -1 && pose.far_foot[1] > -1; }).length;
    assert.ok(both_down >= 4, 'both feet down in ' + both_down + ' drawings');
});

check('her hips rise and fall about 2 units, lowest with both feet down', function () {
    const walk = tool.her_animations.walk;
    const heights = walk.map(function hip(pose) { return pose.hip[1]; });
    const rise = Math.max.apply(null, heights) - Math.min.apply(null, heights);
    assert.ok(rise > 1.5 && rise < 2.6, 'hips move ' + rise.toFixed(2));
    const lowest = heights.indexOf(Math.max.apply(null, heights));  // y points down.
    const at_lowest = walk[lowest];
    assert.ok(at_lowest.near_foot[1] > -1.5 && at_lowest.far_foot[1] > -2.5, 'both feet near the floor at the lowest point');
});

check('her arms swing opposite her legs', function () {
    const walk = tool.her_animations.walk;
    const first = walk[0];
    assert.ok(first.near_foot[0] > first.far_foot[0], 'near foot ahead');
    assert.ok(first.near_hand[0] < first.far_hand[0], 'so the near hand is behind');
});

check('Bodhi walks like a dog: one paw at a time, back then front on each side', function () {
    assert.ok(walks_like_a_dog(tool.dog_animations.walk));
});

check('break it: a "walk" with all four paws landing together is caught', function () {
    const bunny_hop = tool.dog_animations.walk.map(function same(pose, index, frames) {
        const lead = frames[index].legs.near_back;
        return Object.assign({}, pose, { legs: { near_front: lead, far_front: lead, near_back: lead, far_back: lead } });
    });
    assert.ok(!walks_like_a_dog(bunny_hop));
});

check('Bodhi trots on diagonal pairs that land together', function () {
    const order = paw_order(tool.dog_animations.trot);
    const at = {};
    for (const paw of order) {
        at[paw.name] = paw.at;
    }
    assert.ok(Math.abs(at.near_front - at.far_back) <= 1 && Math.abs(at.far_front - at.near_back) <= 1, JSON.stringify(at));
    const half = tool.dog_animations.trot.length / 2;
    assert.ok(Math.abs(Math.abs(at.near_front - at.far_front) - half) <= 1, 'the pairs take turns, half a cycle apart');
});

check('a dog paw folds back at the wrist or hock in the air, and is straight on the floor', function () {
    for (const pose of tool.dog_animations.walk.concat(tool.dog_animations.trot)) {
        for (const name of Object.keys(pose.legs)) {
            if (pose.legs[name][1] === 0) {
                assert.strictEqual(pose.leg_fold[name], 0, name + ' folded on the floor');
            }
        }
    }
    const most = Math.max.apply(null, tool.dog_animations.walk.map(function fold(pose) { return pose.leg_fold.near_front; }));
    assert.ok(most > 0.8, 'the front wrist folds well back at the top of the swing');
});
