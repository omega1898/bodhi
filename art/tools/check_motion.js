// Purpose: measures how every animation moves, drawing by drawing, the way an animator flips
// through a scene checking for problems. It reads the poses from draw_frames.js (it draws
// nothing) and measures three things for each animation:
//
//   pop    The biggest sudden change of speed of any body point between drawings, in game units.
//          Smooth motion speeds up and slows down gradually; a point that jumps ahead and then
//          stalls (or snaps back) reads on screen as a twitch. Measured as the change in a
//          point's movement from one drawing to the next (the "second difference"). A foot
//          touching down or lifting off is allowed to stop dead; feet are judged by slide.
//   slide  For walking cycles: how far a foot that is flat on the floor drifts from where the
//          ground under it has moved. The game advances a cycle's drawings by distance walked,
//          so a planted foot must move back exactly cycle_length / drawings per drawing, or it
//          skates.
//   seam   For loops: the pop across the join from the last drawing back to the first, which
//          is where a loop usually twitches.
//
// Run from the repository root:  node art/tools/check_motion.js
// Add --strict to fail (exit code 1) when any animation goes over the limits below.
'use strict';

const tool = require('./draw_frames.js');

// The largest pop allowed (game units). At the game's size a unit is about a screen pixel on a
// phone, so a change of speed under about 1.5 between drawings reads as smooth.
const largest_pop = 1.5;
// The largest foot drift allowed while planted (game units). Under half a unit doesn't show.
const largest_slide = 0.5;

// Which animations loop, and from which drawing (the ones that start with a once-only move,
// like sitting down, loop only their last part).
const loops = {
    her: { idle: 0, walk: 0, run: 0, embrace: 0, sit: 0, kneel: tool.intro_drawings },
    dog: { idle: 0, trot: 0, gallop: 0, sniff: 0, sleep: 0, lean: 0, sit: tool.intro_drawings, embrace: tool.intro_drawings },
};

// The named points of a pose that are watched for pops. The dog's are taken after its build
// (adult proportions); its ear and tail angles count as points on circles of their length,
// so a flick of the tail is measured in how far its tip moves.
function watched_points(character, pose) {
    if (character === 'her') {
        const points = {
            hip: pose.hip, shoulder: pose.shoulder, near_foot: pose.near_foot, far_foot: pose.far_foot,
            near_hand: pose.near_hand, far_hand: pose.far_hand,
        };
        // Hair, scarf and hem are angles; scaled by how long each part is drawn.
        points.ponytail = [pose.hair * 14, 0];
        points.scarf = [pose.scarf * 12, 0];
        points.hem = [pose.hem * 20, 0];
        return points;
    }
    const built = tool.built_dog_pose(pose, tool.dog_builds.bodhi);
    return {
        pelvis: built.pelvis, withers: built.withers, head: built.head,
        near_front: built.legs.near_front, far_front: built.legs.far_front,
        near_back: built.legs.near_back, far_back: built.legs.far_back,
        ear: [pose.ear * 10, 0], tail: [pose.tail * 14, 0],
    };
}

// The watched points that are feet or paws.
const feet_names = new Set(['near_foot', 'far_foot', 'near_front', 'far_front', 'near_back', 'far_back']);

// How far apart two points are.
function distance(first, second) {
    return Math.hypot(first[0] - second[0], first[1] - second[1]);
}

// The worst pop at drawing `index` (comparing it with the drawings either side), and which
// point it was. `at(i)` gives drawing i's watched points.
function pop_at(at, index) {
    const before = at(index - 1), now = at(index), after = at(index + 1);
    let worst = { size: 0, point: '' };
    for (const name of Object.keys(now)) {
        // A foot meeting or leaving the floor stops dead, as real feet do; feet are judged by
        // the slide check instead, and only count here while they're in the air.
        if (feet_names.has(name) && (before[name][1] === 0 || now[name][1] === 0 || after[name][1] === 0)) {
            continue;
        }
        const change = [
            after[name][0] - 2 * now[name][0] + before[name][0],
            after[name][1] - 2 * now[name][1] + before[name][1],
        ];
        const size = Math.hypot(change[0], change[1]);
        if (size > worst.size) {
            worst = { size: size, point: name };
        }
    }
    return worst;
}

// Measures one animation. Returns { pop, pop_where, seam, slide, slide_where }.
function measure(character, name, frames, cycle_length) {
    const kind = character === 'her' ? 'her' : 'dog';
    const loop_start = loops[kind][name];
    const looping = loop_start !== undefined;
    const points = frames.map(function watched(pose) { return watched_points(character, pose); });
    const count = frames.length;
    // A drawing's neighbours: wrapping round inside the looping part, held still at the ends
    // of a one-off move (so its first and last drawings aren't judged against nothing).
    const at = function at(index) {
        if (looping && index >= loop_start) {
            const length = count - loop_start;
            return points[loop_start + ((index - loop_start) % length + length) % length];
        }
        if (looping && index === count) {
            return points[loop_start];
        }
        return points[Math.max(0, Math.min(count - 1, index))];
    };
    const result = { pop: 0, pop_where: '', seam: 0, slide: 0, slide_where: '' };
    for (let index = 1; index < count - (looping ? 0 : 1); index++) {
        const worst = pop_at(at, index);
        if (worst.size > result.pop) {
            result.pop = worst.size;
            result.pop_where = 'drawing ' + index + ' ' + worst.point;
        }
    }
    if (looping) {
        result.seam = Math.max(pop_at(at, count - 1).size, pop_at(at, loop_start).size);
    }
    if (cycle_length) {
        const step_back = cycle_length / count;
        const feet = character === 'her' ? ['near_foot', 'far_foot'] : ['near_front', 'far_front', 'near_back', 'far_back'];
        for (let index = 0; index < count; index++) {
            const now = at(index), next = points[(index + 1) % count];
            for (const foot of feet) {
                if (now[foot][1] === 0 && next[foot][1] === 0) {
                    const drift = Math.abs(next[foot][0] - now[foot][0] + step_back);
                    if (drift > result.slide) {
                        result.slide = drift;
                        result.slide_where = 'drawing ' + index + ' ' + foot;
                    }
                }
            }
        }
    }
    return result;
}

// Measures every animation, prints a table, and returns how many went over a limit.
function report() {
    const sets = { her: tool.her_animations, bodhi: tool.dog_animations };
    const lines = [];
    let over = 0;
    for (const [character, animations] of Object.entries(sets)) {
        for (const [name, frames] of Object.entries(animations)) {
            const cycle_length = (tool.cycle_lengths[character] || {})[name];
            const m = measure(character, name, frames, cycle_length);
            const bad = m.pop > largest_pop || m.seam > largest_pop || m.slide > largest_slide;
            over += bad ? 1 : 0;
            lines.push((bad ? 'FIX  ' : 'ok   ') + (character + ' ' + name).padEnd(16) +
                ' pop ' + m.pop.toFixed(2).padStart(5) + ' (' + m.pop_where + ')' +
                (loops[character === 'her' ? 'her' : 'dog'][name] !== undefined ? '  seam ' + m.seam.toFixed(2) : '') +
                (cycle_length ? '  slide ' + m.slide.toFixed(2) + (m.slide_where ? ' (' + m.slide_where + ')' : '') : ''));
        }
    }
    console.log(lines.join('\n'));
    console.log(over + ' animation(s) over the limits (pop and seam ' + largest_pop + ', slide ' + largest_slide + ' units).');
    return over;
}

if (require.main === module) {
    const over = report();
    if (process.argv.includes('--strict') && over > 0) {
        process.exit(1);
    }
} else {
    module.exports = { measure, largest_pop, largest_slide };
}
