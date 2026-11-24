// Purpose: works out, drawing by drawing, how Em and the dogs walk, so their walk cycles move
// the way a real person and a real dog do. draw_frames.js turns these numbers into drawings.
//
// Each cycle is built from its "phase": how far through one full cycle (two steps) a drawing
// is, from 0 up to (not including) 1. Every foot follows the same rules at its own phase:
//
//   stance  The foot is on the ground and moves back at an even speed, because the body moves
//           forward over it. In the game the drawings advance with distance walked, so an even
//           speed here means the foot stays put on the ground instead of sliding.
//   swing   The foot is in the air, swinging forward to its next step.
//
// What makes it read as real, from studies of how people and dogs move:
//   - A person's foot is down 60% of the cycle and in the air 40%, so for a moment at each
//     step both feet are down. It lands heel first with the toes up, rolls flat, then the heel
//     peels up and the toes push off. In the air the heel kicks up first (the knee bends most
//     just after push-off), then the foot passes close to the ground and reaches forward.
//   - The hips are highest when one leg is straight under them and lowest when both feet are
//     down. The rise is small, about 5% of leg length. Arms swing opposite the legs.
//   - A walking dog puts its feet down one at a time, evenly spaced, in the order back left,
//     front left, back right, front right (a "lateral sequence walk"). Each paw is down about
//     60% of the time, so two or three paws are always on the ground. The shoulders and hips
//     rise and fall in turn, and the head nods a little with each front step.
//   - A trotting dog moves diagonal pairs together (near front with far back). Each pair is
//     down a little under half the time, so the body floats for a moment between steps.
//   - In the air a dog's front paw folds back at the wrist (the carpus) and its back paw at
//     the hock (the ankle), which is the giveaway a drawn dog needs to look like a dog.
'use strict';

// ---------- Small helpers ----------

// Wraps a phase into 0 (included) to 1 (not included).
function wrap(phase) {
    return phase - Math.floor(phase);
}

// An S-shaped ease from 0 to 1 that starts and ends at rest (no sudden start or stop).
function smooth_step(amount) {
    const clamped = Math.min(1, Math.max(0, amount));
    return clamped * clamped * (3 - 2 * clamped);
}

// Turns a point by an angle (y points down, as in the drawings, so positive turns clockwise).
function turn(point, angle) {
    const cosine = Math.cos(angle);
    const sine = Math.sin(angle);
    return [point[0] * cosine - point[1] * sine, point[0] * sine + point[1] * cosine];
}

// One number on a smooth curve through "knots": [{ at, value, slope }], sorted by `at`. Between
// two knots it is a cubic (Hermite) curve that passes through both values with the given
// slopes, so where two pieces meet there is no kink. A knot without a slope gets the slope of
// the straight line between its neighbours.
function through_knots(knots, at) {
    const slopes = knots.map(function slope_of(knot, index) {
        if (knot.slope !== undefined) {
            return knot.slope;
        }
        const before = knots[Math.max(0, index - 1)];
        const after = knots[Math.min(knots.length - 1, index + 1)];
        return (after.value - before.value) / (after.at - before.at);
    });
    let index = 0;
    while (index < knots.length - 2 && at > knots[index + 1].at) {
        index += 1;
    }
    const start = knots[index];
    const end = knots[index + 1];
    const width = end.at - start.at;
    const t = (at - start.at) / width;
    const t2 = t * t;
    const t3 = t2 * t;
    return (2 * t3 - 3 * t2 + 1) * start.value + (t3 - 2 * t2 + t) * width * slopes[index] +
        (-2 * t3 + 3 * t2) * end.value + (t3 - t2) * width * slopes[index + 1];
}

// A swing forward from `from` to `to` (both with their speeds, per unit of `amount`) whose
// speed rises and falls along half a sine wave. It starts and ends at the given speeds, and
// spreads the speeding up and slowing down evenly instead of bunching it at the ends.
function swing_forward(from, to, start_speed, end_speed, amount) {
    const extra = (to - from) - (start_speed + end_speed) / 2;  // What the sine bump must add.
    return from + start_speed * amount + (end_speed - start_speed) * amount * amount / 2 +
        extra * (1 - Math.cos(Math.PI * amount)) / 2;
}

// The slope (change per unit of phase) of a function at a phase, measured over a tiny step.
function slope_at(function_of_phase, phase) {
    const step = 0.0005;
    return (function_of_phase(phase + step) - function_of_phase(phase - step)) / (2 * step);
}

// ---------- Em's walk ----------

// Her boot, in game units from the ankle (see part_boot in art/characters/her.svg): the bottom
// of the sole is 4 units below the ankle, the heel 3.2 behind it and the ball of the foot 5
// in front. Her "foot point" is 3.6 below the ankle, so the sole sits 0.4 under it.
const her_heel = [-3.2, 4.0];
const her_ball = [5.0, 4.0];
const her_ankle_drop = 3.6;
const her_sole = 0.4;

// The numbers that shape her walk.
const her_walk = {
    stance: 0.6,          // Share of the cycle each foot is on the ground.
    reach: 15.5,          // How far ahead of the hips the foot is when it lands (flat point).
    heel_strike: -0.30,   // Foot angle as the heel lands: toes up (about 17 degrees).
    flat_by: 0.10,        // Phase by which the foot has rolled flat.
    heel_rise_from: 0.40, // Phase the heel starts to peel up.
    toe_off: 0.70,        // Foot angle as the toes leave the ground: heel up (about 40 degrees).
    hip_height: -42.4,    // Average hip height (her standing hip is at -43.6).
    hip_rise: 1.0,        // Up and down from that average: 2 units in all.
    lean: 0.04,           // Shoulders a touch ahead of the hips.
    arm_swing: 6,         // How far each hand swings forward and back of the shoulder.
    arm_lag: 0.04,        // The arms trail the legs a little.
    hand_drop: 25,        // How far below the shoulder the hands hang.
};
// How far she travels in one cycle: the foot travels 2 x reach while down, 60% of the cycle.
her_walk.cycle_length = 2 * her_walk.reach / her_walk.stance;

// Where the foot's ground point (moving back at an even speed) is at a phase of its stance.
function her_ground_x(phase) {
    return her_walk.reach - phase / her_walk.stance * 2 * her_walk.reach;
}

// The foot while it's on the ground: [x, y] of the foot point and the foot's angle. Landing, it
// rocks over the heel; in the middle it's flat; pushing off, it rocks over the ball of the foot.
// Each rocking point stays still on the ground (moving back with it), so nothing slides.
function her_stance_foot(phase) {
    const ground = her_ground_x(phase);
    if (phase < her_walk.flat_by) {
        const angle = her_walk.heel_strike * (1 - smooth_step(phase / her_walk.flat_by));
        const turned = turn(her_heel, angle);
        const ankle = [ground + her_heel[0] - turned[0], her_sole - turned[1]];
        return { point: [ankle[0], ankle[1] + her_ankle_drop], angle: angle };
    }
    if (phase < her_walk.heel_rise_from) {
        return { point: [ground, 0], angle: 0 };
    }
    const rise = (phase - her_walk.heel_rise_from) / (her_walk.stance - her_walk.heel_rise_from);
    const angle = her_walk.toe_off * rise * rise;  // Starts gently, quickens toward push-off.
    const ball_on_ground = [ground + her_ball[0], her_sole];
    const turned = turn(her_ball, angle);
    const ankle = [ball_on_ground[0] - turned[0], ball_on_ground[1] - turned[1]];
    return { point: [ankle[0], ankle[1] + her_ankle_drop], angle: angle };
}

// The foot in the air, from push-off back round to the next heel strike. Forward it speeds up
// and slows down smoothly; up, the heel kicks high first, the foot skims low at mid-swing, then
// drops onto the heel with the toes raised.
function her_swing_foot(phase) {
    const leave = her_walk.stance;
    const length = 1 - leave;
    const amount = (phase - leave) / length;
    const start = her_stance_foot(leave - 0.0001);
    const end = her_stance_foot(0);
    const x_of = function x_of(at) { return her_stance_foot(wrap(at)).point[0]; };
    const y_of = function y_of(at) { return her_stance_foot(wrap(at)).point[1]; };
    const angle_of = function angle_of(at) { return her_stance_foot(wrap(at)).angle; };
    const start_speed = slope_at(x_of, leave - 0.001) * length;
    const end_speed = slope_at(x_of, 0.001) * length;
    const x = swing_forward(start.point[0], end.point[0], start_speed, end_speed, amount);
    const y = through_knots([
        { at: 0, value: start.point[1], slope: slope_at(y_of, leave - 0.001) * length },
        { at: 0.28, value: -5.2, slope: 0 },
        { at: 0.68, value: -1.6 },
        { at: 1, value: end.point[1], slope: slope_at(y_of, 0.001) * length },
    ], amount);
    const angle = through_knots([
        { at: 0, value: start.angle, slope: slope_at(angle_of, leave - 0.001) * length },
        { at: 0.3, value: 0.5 },
        { at: 0.7, value: -0.05 },
        { at: 1, value: end.angle, slope: slope_at(angle_of, 0.001) * length },
    ], amount);
    return { point: [x, Math.min(0, y)], angle: angle };
}

// One foot at any phase of its own cycle.
function her_foot(phase) {
    phase = wrap(phase);
    return phase < her_walk.stance ? her_stance_foot(phase) : her_swing_foot(phase);
}

// Her whole walk as `frame_count` sets of values for draw_frames.js's her_pose. The near leg
// lands at phase 0 and the far leg half a cycle later.
function her_walk_values(frame_count) {
    const values = [];
    for (let frame = 0; frame < frame_count; frame++) {
        const phase = frame / frame_count;
        const near = her_foot(phase);
        const far = her_foot(phase + 0.5);
        // Highest with one leg straight under her (about 0.3 and 0.8), lowest with both feet down.
        const hip_y = her_walk.hip_height - her_walk.hip_rise * Math.cos(4 * Math.PI * (phase - 0.3));
        // The near arm is furthest back as the near heel lands, forward as the far heel lands.
        const swing = -her_walk.arm_swing * Math.cos(2 * Math.PI * (phase - her_walk.arm_lag));
        const hand_lift = function hand_lift(forward) { return 0.12 * Math.abs(forward) + 0.25 * Math.max(0, forward); };
        values.push({
            hip_y: hip_y, lean: her_walk.lean,
            near_foot: near.point, far_foot: far.point,
            near_foot_angle: near.angle, far_foot_angle: far.angle,
            near_hand: [swing + 1, her_walk.hand_drop - hand_lift(swing)],
            far_hand: [-swing + 1, her_walk.hand_drop - hand_lift(-swing)],
            // Hair, scarf and coat hem bounce twice a cycle, with the hips (follow-through adds lag).
            hair: 0.05 + 0.04 * Math.sin(4 * Math.PI * phase),
            scarf: 0.4 + 0.04 * Math.sin(4 * Math.PI * phase),
            hem: 0.01 + 0.03 * Math.sin(2 * Math.PI * phase),
        });
    }
    return values;
}

// ---------- The dog's walk and trot ----------

// One paw's path through its cycle, for a gait with these numbers:
//   stance:  share of the cycle the paw is down
//   reach:   how far ahead of its standing spot the paw lands (it leaves as far behind)
//   lift:    how high the paw rises at the top of its swing
//   lift_at: where in the swing (0 to 1) it is highest
// Returns the paw's [x, y] offset from where it stands still, and how far the wrist or hock
// folds (0 straight, 1 fully folded).
function paw_at(phase, gait) {
    phase = wrap(phase);
    const travel = 2 * gait.reach;
    if (phase < gait.stance) {
        return { offset: [gait.reach - phase / gait.stance * travel, 0], fold: 0 };
    }
    const length = 1 - gait.stance;
    const amount = (phase - gait.stance) / length;
    const ground_speed = -travel / gait.stance * length;  // Its speed on the ground, per swing.
    const x = swing_forward(-gait.reach, gait.reach, ground_speed, ground_speed * 0.4, amount);
    const y = -gait.lift * Math.pow(Math.sin(Math.PI * Math.pow(amount, gait.lift_bias)), 1.3);
    const fold = Math.pow(Math.sin(Math.PI * Math.min(1, amount / gait.fold_until)), 1.5);
    return { offset: [x, Math.min(0, y)], fold: amount < gait.fold_until ? fold : 0 };
}

// Where each paw stands still (from draw_frames.js's standing dog), and the order the paws come
// down in, as phases.
const dog_standing_paws = { near_front: [13, 0], far_front: [11, 0], near_back: [-8, 0], far_back: [-11, 0] };

// The walk: one paw at a time, back left (near back), front left, back right, front right.
const dog_walk = {
    landings: { near_back: 0, near_front: 0.25, far_back: 0.5, far_front: 0.75 },
    front: { stance: 0.62, reach: 10, lift: 3.6, lift_bias: 0.8, fold_until: 0.85 },
    back: { stance: 0.62, reach: 10, lift: 2.2, lift_bias: 1.0, fold_until: 0.8 },
    body_rise: 0.45,      // How far the shoulders and hips each rise and fall.
    head_nod: 0.04,       // How far the head tips down as a front paw lands.
};
dog_walk.cycle_length = 2 * dog_walk.front.reach / dog_walk.front.stance;

// The trot: diagonal pairs together, each down a little under half the time.
const dog_trot = {
    landings: { near_front: 0, far_back: 0, far_front: 0.5, near_back: 0.5 },
    front: { stance: 0.46, reach: 12.5, lift: 4.8, lift_bias: 0.8, fold_until: 0.9 },
    back: { stance: 0.46, reach: 12.5, lift: 3.2, lift_bias: 1.0, fold_until: 0.85 },
    body_rise: 0.7,
    head_nod: 0.03,
};
dog_trot.cycle_length = 2 * dog_trot.front.reach / dog_trot.front.stance;

// A dog's walk or trot as `frame_count` sets of pose changes for draw_frames.js's dog_pose.
// The shoulders rise as a front leg passes under them and dip as a front paw lands; the hips do
// the same with the back legs. In a walk these come at different times (the body seesaws a
// little); in a trot the diagonal pairs land together, so the whole back rises and falls at once.
function dog_gait_values(gait, frame_count) {
    const values = [];
    for (let frame = 0; frame < frame_count; frame++) {
        const phase = frame / frame_count;
        const legs = {};
        const leg_fold = {};
        for (const [name, landing] of Object.entries(gait.landings)) {
            const shape = name.endsWith('front') ? gait.front : gait.back;
            const paw = paw_at(phase - landing, shape);
            legs[name] = [dog_standing_paws[name][0] + paw.offset[0], paw.offset[1]];
            leg_fold[name] = paw.fold;
        }
        // A body point is highest as a leg under it passes the middle of its stance.
        const front_middle = wrap(gait.landings.near_front + gait.front.stance / 2);
        const back_middle = wrap(gait.landings.near_back + gait.back.stance / 2);
        const withers_rise = gait.body_rise * Math.cos(4 * Math.PI * (phase - front_middle));
        const pelvis_rise = gait.body_rise * Math.cos(4 * Math.PI * (phase - back_middle));
        // The head nods down as each front paw lands, a moment after the shoulders dip.
        const nod = gait.head_nod * Math.cos(4 * Math.PI * (phase - wrap(gait.landings.near_front + 0.06)));
        values.push({
            pelvis: [-11, -27 - pelvis_rise], withers: [11, -28 - withers_rise],
            head: [19, -39 - withers_rise * 0.6], head_angle: nod,
            ear: 0.3 + 0.08 * Math.sin(4 * Math.PI * phase), tail: 0.5 + 0.08 * Math.sin(2 * Math.PI * phase),
            legs: legs, leg_fold: leg_fold,
        });
    }
    return values;
}

module.exports = {
    her_walk, her_walk_values, her_foot, dog_walk, dog_trot, dog_gait_values, paw_at, through_knots, swing_forward, wrap,
};
