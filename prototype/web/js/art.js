// Purpose: draws everything you see: the woman, the goldendoodle, the places, water, rain,
// light in the dark, and the quiet scenes between chapters. Everything is drawn with code
// (curves, gradients, layered light), so there are no image files to download or license,
// and every picture stays sharp on any screen.
//
// Art direction in one line: warm lamplight in cold, wet dark, soft shapes, strong silhouettes.
'use strict';

// The dog's coat: a cream-apricot goldendoodle, lit from above. Change these to match the real dog.
const dog_shades = {
    light: '#f6dcae', base: '#e2b47b', shade: '#b8875a', deep: '#875c3d',
    muzzle: '#f1d3a1', nose: '#2a1c16', eye: '#1b120c', collar: '#9a2f2a', tag: '#e2bf68',
};

// The woman: blonde hair, blue eyes (from docs/characters.md), a teal raincoat, a rust scarf.
const human_shades = {
    skin: '#f0d0b6', skin_shadow: '#d4ab8f', hair_light: '#f3dc96', hair: '#dcb866', hair_dark: '#a8843f',
    eye: '#3f78b8', coat_light: '#3d7f7c', coat: '#2c6563', coat_dark: '#1d4746',
    scarf: '#b9532a', scarf_dark: '#8c3a1d', jeans: '#2e3a52', jeans_dark: '#222b3d', boots: '#3b2a20', bag: '#6a5236',
};

const outline_color = 'rgba(8,10,16,0.55)';

// A random-number maker that gives the same sequence every time for the same seed,
// so a building's windows don't jump around each time the level loads.
function make_seeded_random(seed) {
    let state = seed >>> 0;
    return function next_random() {
        state = (state * 1664525 + 1013904223) >>> 0;
        return state / 4294967296;
    };
}

// Draws a smooth closed curve through a list of points (used for soft, organic shapes).
function trace_smooth_closed(context, points) {
    const count = points.length;
    context.beginPath();
    const first_mid_x = (points[count - 1][0] + points[0][0]) / 2;
    const first_mid_y = (points[count - 1][1] + points[0][1]) / 2;
    context.moveTo(first_mid_x, first_mid_y);
    for (let index = 0; index < count; index++) {
        const point = points[index];
        const next = points[(index + 1) % count];
        context.quadraticCurveTo(point[0], point[1], (point[0] + next[0]) / 2, (point[1] + next[1]) / 2);
    }
    context.closePath();
}

// Fills a soft blob with a scalloped edge and a top-lit gradient: the building block of
// a curly coat. `bumps` is how many curls ripple around the edge.
function fluffy_blob(context, center_x, center_y, radius_x, radius_y, bumps, seed, top_color, bottom_color) {
    const points = [];
    const steps = bumps * 2;
    for (let index = 0; index < steps; index++) {
        const angle = (index / steps) * Math.PI * 2;
        const ripple = index % 2 === 0 ? 1.08 + 0.04 * Math.sin(seed + index) : 0.94;
        points.push([center_x + Math.cos(angle) * radius_x * ripple, center_y + Math.sin(angle) * radius_y * ripple]);
    }
    const gradient = cached_gradient(context, shared_gradient_owner, 'blob|' + center_y + '|' + radius_y + '|' + top_color + '|' + bottom_color, function make_fur_gradient() {
        const fur = context.createLinearGradient(0, center_y - radius_y, 0, center_y + radius_y);
        fur.addColorStop(0, top_color);
        fur.addColorStop(1, bottom_color);
        return fur;
    });
    context.fillStyle = gradient;
    trace_smooth_closed(context, points);
    context.fill();
}

// Small "c" marks inside a blob that read as curls, without turning the coat into rings.
function curl_marks(context, center_x, center_y, radius_x, radius_y, count, seed, color) {
    const random = make_seeded_random(seed);
    context.strokeStyle = color;
    context.lineWidth = 1.1;
    context.beginPath();
    for (let index = 0; index < count; index++) {
        const angle = random() * Math.PI * 2;
        const distance = Math.sqrt(random()) * 0.72;
        const x = center_x + Math.cos(angle) * radius_x * distance;
        const y = center_y + Math.sin(angle) * radius_y * distance;
        const start = random() * Math.PI * 2;
        context.moveTo(x + Math.cos(start) * 2.2, y + Math.sin(start) * 2.2);
        context.arc(x, y, 2.2, start, start + Math.PI * 0.9);
    }
    context.stroke();
}

// A soft dark ellipse under a character's feet: it anchors them to the ground.
function draw_contact_shadow(context, character) {
    const lift = character.on_ground === false ? Math.min(1, Math.abs(character.velocity_y || 0) / 600) : 0;
    const width = character.width * (character.kind === 'dog' ? 0.95 : 1.15) * (1 - lift * 0.4);
    context.save();
    context.translate(character.x + character.width / 2, (character.shadow_y || character.y + character.height) - 1);
    context.scale(1, 0.22);
    stamp_glow(context, '0,0,0', 0, 0, width, 0.5 * (1 - lift * 0.6));
    context.restore();
}

// ---------- The dog ----------

// Draws the goldendoodle with its feet at the bottom of its collision box.
// `dog` carries pose, facing, walk_phase, wag, fear, ear_lift, which the game updates.
function draw_dog(context, dog, time_seconds) {
    if (typeof flipbook !== 'undefined' && flipbook.ready) {
        // The hand-drawn frames, once loaded: the puppy's or the grown dog's.
        draw_flipbook_character(context, dog, dog.drawing === 'bodhi_puppy' ? 'bodhi_puppy' : 'bodhi', time_seconds);
        return;
    }
    const size = dog.size_scale || 1;
    if (size !== 1) {
        // The drawings below (shown only while the frames load) are made for a grown dog: draw
        // one standing on the same feet, scaled down to her size.
        const feet_x = dog.x + dog.width / 2;
        const feet_y = dog.y + dog.height;
        context.save();
        context.translate(feet_x, feet_y);
        context.scale(size, size);
        context.translate(-feet_x, -feet_y);
        draw_dog(context, Object.assign({}, dog, { x: feet_x - 23, y: feet_y - 38, width: 46, height: 38, size_scale: 1 }), time_seconds);
        context.restore();
        return;
    }
    if (puppets.ready) {
        draw_dog_puppet(context, dog, time_seconds);  // The painted dog, once its art has loaded.
        return;
    }
    const pose = dog.pose || 'stand';
    const fear = dog.fear || 0;
    context.save();
    context.translate(dog.x + dog.width / 2, dog.y + dog.height);
    context.rotate(dog.lean ? dog.lean.value : 0);
    context.scale(turn_scale(dog) * (dog.squash_x || 1), dog.squash_y || 1);
    const shape = blend_pose_shape(dog, pose, dog_shape(pose, dog, time_seconds), time_seconds, 0.18);
    const moving = pose === 'walk';
    const wag_speed = 7 + (dog.wag || 0) * 13;
    const wag = Math.sin(time_seconds * wag_speed) * (0.18 + (dog.wag || 0) * 0.5);
    // The signature head tilt: every so often, when standing still, the dog tilts its head.
    const tilt_cycle = (time_seconds + (dog.x || 0) * 0.001) % 7.5;
    const head_tilt = !moving && pose !== 'sleep' && tilt_cycle > 6 ? Math.sin((tilt_cycle - 6) / 1.5 * Math.PI) * -0.32 : 0;
    const head_x = shape.head[0];
    const head_y = shape.head[1] + (fear > 0.5 && pose !== 'sleep' && pose !== 'embrace' ? 3 : 0);
    const head_angle = shape.head_angle + head_tilt;

    draw_dog_legs(context, [shape.legs[0], shape.legs[1]], dog_shades.shade, dog_shades.base);

    // Tail: a plume on a spring, plus the wag. High and busy when happy, low when afraid.
    if (pose !== 'embrace') {
        context.save();
        context.translate(shape.body[0] - 18, pose === 'sleep' ? -12 : shape.body[1] - 6);
        const tail_angle = pose === 'sleep' ? 2.1 : (dog.tail ? dog.tail.value : -0.7 + fear * 1.3) + wag;
        context.rotate(tail_angle);
        fluffy_blob(context, -9, -2, 10, 6, 6, 3, dog_shades.light, dog_shades.base);
        context.restore();
    }

    // Body: hindquarters, barrel and chest as three soft curly blobs. At a gallop the spine
    // stretches and gathers, so hindquarters and chest move apart and together.
    context.save();
    context.translate(shape.body[0], shape.body[1]);
    context.rotate(shape.body_tilt);
    const sleep_squash = pose === 'sleep' ? 0.75 : 1;
    fluffy_blob(context, shape.hind_x, 0, 12, 12 * sleep_squash, 7, 1, dog_shades.base, dog_shades.shade);
    fluffy_blob(context, (shape.hind_x + shape.chest_x) / 2 + 1, -1, 17, 12 * sleep_squash, 9, 2, dog_shades.light, dog_shades.shade);
    fluffy_blob(context, shape.chest_x, 1, 9, 11 * sleep_squash, 6, 4, dog_shades.light, dog_shades.base);
    curl_marks(context, 0, 0, 20, 10 * sleep_squash, 7, 7, 'rgba(150,104,66,0.28)');
    context.strokeStyle = 'rgba(255,240,210,0.55)';
    context.lineWidth = 1.6;
    context.beginPath();
    context.ellipse(0, -1, 19, 11.5 * sleep_squash, 0, Math.PI * 1.12, Math.PI * 1.88);
    context.stroke();
    context.restore();

    draw_dog_legs(context, [shape.legs[2], shape.legs[3]], dog_shades.base, dog_shades.light);

    // Head: a big soft fluff, a topknot, a lighter bearded muzzle, a dark nose and a bright eye.
    context.save();
    context.translate(head_x, head_y);
    context.rotate(head_angle);
    fluffy_blob(context, 0, 0, 11, 10.5, 8, 5, dog_shades.light, dog_shades.base);
    fluffy_blob(context, -1, -8, 7, 4.5, 5, 6, dog_shades.light, dog_shades.light);
    fluffy_blob(context, 8.5, 3.5, 7, 5.5, 6, 8, dog_shades.muzzle, dog_shades.base);
    context.fillStyle = dog_shades.nose;
    context.beginPath();
    context.ellipse(14.5, 1.2, 2.6, 2.1, 0, 0, Math.PI * 2);
    context.fill();
    context.fillStyle = 'rgba(255,255,255,0.35)';
    context.beginPath();
    context.arc(14, 0.4, 0.8, 0, Math.PI * 2);
    context.fill();
    if (pose === 'bark') {
        context.fillStyle = '#5a2a22';
        context.beginPath();
        context.ellipse(10, 7.5, 3.5, 2, 0.2, 0, Math.PI * 2);
        context.fill();
    }
    const blinking = ((dog.x || 0) * 0.29 + performance.now() / 1000) % 3.7 < 0.11;
    const eyes_closed = pose === 'sleep' || pose === 'lean' || pose === 'embrace' || blinking;
    if (eyes_closed) {
        context.strokeStyle = dog_shades.eye;
        context.lineWidth = 1.3;
        context.beginPath();
        context.arc(4.5, -2, 2.1, 0.25, Math.PI - 0.25);
        context.stroke();
    } else {
        context.fillStyle = dog_shades.eye;
        context.beginPath();
        context.ellipse(4.5, -2.5, 2.1, 2.3, 0, 0, Math.PI * 2);
        context.fill();
        context.fillStyle = 'rgba(255,255,255,0.85)';
        context.beginPath();
        context.arc(5.2, -3.3, 0.75, 0, Math.PI * 2);
        context.fill();
        context.strokeStyle = 'rgba(184,135,90,0.8)';
        context.lineWidth = 1.4;
        context.beginPath();
        context.arc(4.5, -3.5, 3.6, Math.PI * 1.15, Math.PI * 1.75 + fear * 0.2);
        context.stroke();
    }
    // The long floppy ear hangs on a spring: it streams back at speed and flaps on landing.
    context.save();
    context.translate(-4, -4);
    const ear_angle = dog.ear ? dog.ear.value : 0.2 - (dog.ear_lift || 0) * 0.6 + fear * 0.3;
    context.rotate(ear_angle + (moving ? Math.sin((dog.walk_phase || 0) * 2) * 0.08 : 0));
    fluffy_blob(context, -1, 9, 5.5, 10, 6, 9, dog_shades.base, dog_shades.deep);
    context.restore();
    context.restore();

    // Collar with its tag, which swings with the head.
    if (pose !== 'sleep') {
        context.strokeStyle = dog_shades.collar;
        context.lineWidth = 3;
        context.beginPath();
        context.arc(head_x - 5, head_y + 7, 6.5, 0.5, 2.3);
        context.stroke();
        context.fillStyle = dog_shades.tag;
        context.beginPath();
        context.arc(head_x - 4 + Math.sin(time_seconds * 6) * (moving ? 1.2 : 0.2), head_y + 14, 1.8, 0, Math.PI * 2);
        context.fill();
    }
    if (dog.carrying) {
        context.save();
        context.translate(head_x, head_y);
        context.rotate(head_angle);
        draw_carried_item(context, dog.carrying, 12, 7);
        context.restore();
    }
    context.restore();
}

// Where each leg's hip and paw are for the current pose and step.
function dog_shape(pose, dog, time_seconds) {
    const speed = Math.abs(dog.velocity_x || 0);
    const moving = pose === 'walk';
    const walking = moving ? clamp_between(speed / 110, 0, 1) : 0;
    const galloping = moving ? clamp_between((speed - 210) / 50, 0, 1) : 0;
    const phase = dog.walk_phase || 0;
    const breath = Math.sin(time_seconds * 2.4) * 0.5;
    // Trot: diagonal legs move together. Gallop: front legs together, back legs together,
    // with the spine stretching and gathering. The two blend as speed rises.
    const offsets = {
        far_front: 0 + galloping * 0.45 + (1 - galloping) * Math.PI,
        far_back: Math.PI + galloping * 0.45 - (1 - galloping) * Math.PI,
        near_front: 0,
        near_back: Math.PI,
    };
    const stride = (5.5 + 3.5 * galloping) * walking;
    const lift = (3.5 + 4 * galloping) * walking;
    const paw = function paw_at(hip_x, leg_phase) {
        return [hip_x + stride * Math.sin(leg_phase), -Math.max(0, Math.cos(leg_phase)) * lift];
    };
    const bob = -walking * (Math.abs(Math.sin(phase)) * 1.6 * (1 - galloping) + (0.5 + 0.5 * Math.sin(phase)) * 4 * galloping);
    const spine = galloping * 2.5 * Math.cos(phase);
    const shape = {
        body: [0, -27 + bob + breath * 0.3], body_tilt: galloping * 0.1 * Math.sin(phase), hind_x: -11 - spine, chest_x: 13 + spine,
        head: [19 + galloping * 2, -39 + bob * 0.45], head_angle: 0,
        legs: [
            [11, -20].concat(paw(11, phase + offsets.far_front)),
            [-11, -20].concat(paw(-11, phase + offsets.far_back)),
            [14, -20].concat(paw(14, phase + offsets.near_front)),
            [-8, -20].concat(paw(-8, phase + offsets.near_back)),
        ],
    };
    if (pose === 'sit') {
        Object.assign(shape, { body: [-3, -19], body_tilt: -0.5, head: [12, -43], legs: [[10, -17, 12, 0], [-6, -9, 2, -1], [13, -17, 15, 0], [-8, -9, 3, 0]] });
    } else if (pose === 'sniff') {
        Object.assign(shape, { body_tilt: 0.14, head: [25, -15], head_angle: 0.35 });
    } else if (pose === 'bark') {
        Object.assign(shape, { head: [20, -38], head_angle: -0.25 });
    } else if (pose === 'jump') {
        // Rising: front legs fold up, back legs push out. Falling: front legs reach for the ground.
        const rising = clamp_between(-(dog.velocity_y || 0) / 690, 0, 1);
        const falling = clamp_between((dog.velocity_y || 0) / 700, 0, 1);
        Object.assign(shape, {
            body_tilt: -0.28 * rising + 0.16 * falling,
            legs: [
                [13, -17, 20 - rising * 4 + falling * 4, -9 * rising - 2 + falling * 2],
                [-12, -17, -22 + falling * 8, -8 + falling * 6],
                [16, -17, 22 - rising * 4 + falling * 5, -10 * rising - 1 + falling * 2],
                [-9, -17, -19 + falling * 8, -7 + falling * 6],
            ],
        });
    } else if (pose === 'embrace') {
        Object.assign(shape, { body: [-7, -30], body_tilt: -1.0, head: [8, -60], head_angle: 0.35, legs: [[8, -45, 15, -49], [-11, -12, -13, 0], [10, -45, 18, -50], [-7, -12, -5, 0]] });
    } else if (pose === 'sleep') {
        Object.assign(shape, { body: [0, -11], head: [21, -10], head_angle: 0.15, legs: [[12, -8, 22, -5], [-8, -8, 0, -3], [14, -8, 24, -4], [-10, -8, 2, -3]] });
    } else if (pose === 'lean') {
        Object.assign(shape, { body_tilt: -0.06, head: [21, -31], head_angle: -0.2 });
    }
    return shape;
}

// Draws dog legs as soft tapered columns with fluffy paws.
function draw_dog_legs(context, legs, leg_color, paw_color) {
    for (const leg of legs) {
        const hip = [leg[0], leg[1]];
        const paw = [leg[2], leg[3] - 2];
        // Each leg bends at one joint, pointing backward (a wrist at the front, a hock at the back).
        const joint = solve_joint(hip, paw, 9.5, 9.5, 1);
        draw_limb(context, hip, joint, paw, leg_color, 7.5, 6);
        context.fillStyle = paw_color;
        context.beginPath();
        context.ellipse(paw[0] + 1, paw[1], 4.6, 3, 0, 0, Math.PI * 2);
        context.fill();
    }
}

// Draws a small item held in the dog's mouth or lying on the ground.
function draw_carried_item(context, item_name, x, y) {
    if (item_name === 'ball') {
        context.fillStyle = '#cdd64f';
        context.beginPath();
        context.arc(x, y, 4.5, 0, Math.PI * 2);
        context.fill();
        context.strokeStyle = 'rgba(255,255,255,0.7)';
        context.lineWidth = 1;
        context.beginPath();
        context.arc(x - 2, y, 3.5, -1, 1);
        context.stroke();
    } else if (item_name === 'glove') {
        context.fillStyle = '#7a5236';
        context.beginPath();
        context.ellipse(x + 1, y + 1, 6, 4, 0.2, 0, Math.PI * 2);
        context.fill();
        context.fillRect(x + 4, y - 4, 2.2, 5);
        context.fillRect(x + 1, y - 5, 2.2, 5);
    } else if (item_name === 'rabbit') {
        context.fillStyle = '#ddd5c6';
        context.beginPath();
        context.ellipse(x + 2, y + 2, 5.5, 4.5, 0, 0, Math.PI * 2);
        context.fill();
        context.beginPath();
        context.ellipse(x + 3, y - 5, 1.6, 4.5, -0.2, 0, Math.PI * 2);
        context.ellipse(x + 6.5, y - 4.5, 1.6, 4.5, 0.25, 0, Math.PI * 2);
        context.fill();
    } else if (item_name === 'photo') {
        context.fillStyle = '#ece4d1';
        context.fillRect(x - 6, y - 4, 12, 9);
        context.fillStyle = '#7f8a6a';
        context.fillRect(x - 5, y - 3, 10, 7);
    }
}

// ---------- The woman ----------

// Works out where her joints are for each pose. Points are relative to her feet.
function human_shape(pose, human, time_seconds) {
    const breath = Math.sin(time_seconds * 1.8) * 0.6;
    if (pose === 'kneel' || pose === 'embrace') {
        const hugging = pose === 'embrace';
        return {
            hip: [-1, -27], foot_back: [-19, 0], foot_front: [13, 0],
            shoulder: [5, -57 + breath], hand_back: hugging ? [22, -40] : [12, -30], hand_front: hugging ? [16, -34] : [17, -30],
            head: [10, -66 + breath],
        };
    }
    if (pose === 'sit') {
        return {
            hip: [0, -22], foot_back: [18, 0], foot_front: [21, 0],
            shoulder: [1, -53 + breath], hand_back: [14, -28], hand_front: [17, -27], head: [2, -62 + breath],
        };
    }
    if (pose === 'jump') {
        // Rising: knees tuck and arms swing up. Falling: legs reach down for the landing, arms out.
        const rising = clamp_between(-(human.velocity_y || 0) / 620, 0, 1);
        const falling = clamp_between((human.velocity_y || 0) / 700, 0, 1);
        return {
            hip: [0, -45], foot_back: [-8 + falling * 3, -15 * rising - 2], foot_front: [9 - falling * 2, -11 * rising],
            shoulder: [2, -72], hand_back: [-7 - falling * 8, -76 + falling * 18], hand_front: [11 + falling * 6, -80 + falling * 20],
            head: [3, -81],
        };
    }
    // Walking, jogging and standing come from one cycle, blended by speed. Each foot swings
    // forward in an arc (lifted) and pushes back along the ground, so feet never slide.
    const speed = Math.abs(human.velocity_x || 0);
    const walking = pose === 'walk' ? clamp_between(speed / 110, 0, 1) : 0;
    const jogging = pose === 'walk' ? clamp_between((speed - 185) / 45, 0, 1) : 0;
    const phase = human.walk_phase || 0;
    const stride = (9 + 6 * jogging) * walking;
    const lift = (3.5 + 6 * jogging) * walking;
    const foot = function foot_at(leg_phase) {
        return [stride * Math.sin(leg_phase) + 1, -Math.max(0, Math.cos(leg_phase)) * lift];
    };
    const legs_apart = Math.abs(Math.sin(phase));
    // Walking, the body rides highest as the legs pass; jogging, it dips at each footfall.
    const bob = -walking * (1 - jogging) * (1 - legs_apart) * 1.4 + jogging * (legs_apart * 2.2 + 1.2);
    const idle_sway = (1 - walking) * Math.sin(time_seconds * 0.55) * 0.8;
    const hip = [jogging * 2 + idle_sway, -43.5 + bob];
    const shoulder = [hip[0] + 2 + jogging * 2.5, hip[1] - 27.5 + breath * 0.35];
    const swing = stride * 0.75 * Math.sin(phase);
    const hand_height = shoulder[1] + 26 - jogging * 10;
    return {
        hip: hip, foot_back: foot(phase + Math.PI), foot_front: foot(phase),
        shoulder: shoulder,
        hand_back: [shoulder[0] + swing - 1, hand_height - Math.abs(swing) * 0.15],
        hand_front: [shoulder[0] - swing + 1, hand_height - Math.abs(swing) * 0.15],
        head: [shoulder[0] + 1 + jogging * 1.5, shoulder[1] - 9.5 - bob * 0.3],
    };
}

// Draws one limb as two tapered segments meeting at a joint.
function draw_limb(context, from_point, joint_point, to_point, color, upper_width, lower_width) {
    context.strokeStyle = color;
    context.lineCap = 'round';
    context.lineJoin = 'round';
    context.lineWidth = upper_width;
    context.beginPath();
    context.moveTo(from_point[0], from_point[1]);
    context.quadraticCurveTo((from_point[0] + joint_point[0]) / 2, (from_point[1] + joint_point[1]) / 2, joint_point[0], joint_point[1]);
    context.stroke();
    context.lineWidth = lower_width;
    context.beginPath();
    context.moveTo(joint_point[0], joint_point[1]);
    context.lineTo(to_point[0], to_point[1]);
    context.stroke();
}

// Draws a boot at a foot position.
function draw_boot(context, foot) {
    context.fillStyle = human_shades.boots;
    context.beginPath();
    context.moveTo(foot[0] - 3.5, foot[1] - 5);
    context.lineTo(foot[0] + 3, foot[1] - 5);
    context.quadraticCurveTo(foot[0] + 8.5, foot[1] - 3, foot[0] + 8, foot[1]);
    context.lineTo(foot[0] - 3.5, foot[1]);
    context.closePath();
    context.fill();
}

// Draws the woman with her feet at the bottom of her collision box.
function draw_human(context, human, time_seconds) {
    if (typeof flipbook !== 'undefined' && flipbook.ready) {
        draw_flipbook_character(context, human, 'her', time_seconds);  // The hand-drawn frames, once loaded.
        return;
    }
    if (puppets.ready) {
        draw_human_puppet(context, human, time_seconds);  // The painted woman, once her art has loaded.
        return;
    }
    const pose = human.pose || 'stand';
    context.save();
    context.translate(human.x + human.width / 2, human.y + human.height);
    context.rotate(human.lean ? human.lean.value : 0);
    context.scale(turn_scale(human) * (human.squash_x || 1), human.squash_y || 1);
    const shape = blend_pose_shape(human, pose, human_shape(pose, human, time_seconds), time_seconds, 0.16);
    // Knees bend forward and elbows backward, worked out from where hips, feet, shoulders
    // and hands are. Leg: thigh 23, shin 22. Arm: upper 14, forearm 13.
    const knee_back = solve_joint(shape.hip, shape.foot_back, 23, 22, -1);
    const knee_front = solve_joint(shape.hip, shape.foot_front, 23, 22, -1);
    const elbow_back = solve_joint(shape.shoulder, shape.hand_back, 14, 13, 1);
    const elbow_front = solve_joint(shape.shoulder, shape.hand_front, 14, 13, 1);
    const speed = Math.abs(human.velocity_x || 0) / 240;
    const scarf_flow = human.scarf ? human.scarf.value : speed + Math.sin(time_seconds * 2.1) * 0.25;
    const hem_flow = human.hem ? human.hem.value : speed;
    const hair_angle = human.hair ? human.hair.value : (human.ponytail_angle || 0);

    // Back arm and back leg: darker, they're further away.
    draw_limb(context, shape.shoulder, elbow_back, shape.hand_back, human_shades.coat_dark, 6, 5);
    draw_limb(context, shape.hip, knee_back, shape.foot_back, human_shades.jeans_dark, 8, 6.5);
    draw_boot(context, shape.foot_back);

    // Satchel on her far hip; it bounces a little behind her.
    context.fillStyle = human_shades.bag;
    context.beginPath();
    context.ellipse(shape.hip[0] - 9 - hem_flow * 2, shape.hip[1] - 4, 6.5, 8, 0.15 + hem_flow * 0.3, 0, Math.PI * 2);
    context.fill();

    draw_limb(context, shape.hip, knee_front, shape.foot_front, human_shades.jeans, 8, 6.5);
    draw_boot(context, shape.foot_front);

    // The raincoat: hangs from the shoulders, the hem swinging back with speed and settling.
    const torso_angle = Math.atan2(shape.shoulder[0] - shape.hip[0], shape.hip[1] - shape.shoulder[1]);
    context.save();
    context.translate(shape.hip[0], shape.hip[1]);
    context.rotate(torso_angle);
    const torso = Math.hypot(shape.shoulder[0] - shape.hip[0], shape.hip[1] - shape.shoulder[1]);
    const crouched = pose === 'kneel' || pose === 'embrace' || pose === 'sit';
    const hem_y = crouched ? 3 : 10;
    const hem_sway = crouched ? 0 : -hem_flow * 5 + Math.sin(time_seconds * 3) * 0.6;
    const coat = context.createLinearGradient(0, -torso, 0, hem_y);
    coat.addColorStop(0, human_shades.coat_light);
    coat.addColorStop(1, human_shades.coat_dark);
    context.fillStyle = coat;
    context.beginPath();
    context.moveTo(-8, -torso + 1);
    context.quadraticCurveTo(0, -torso - 2, 9, -torso + 1);
    context.quadraticCurveTo(12, -torso * 0.4, 13 + hem_sway * 0.4, hem_y - hem_sway * 0.2);
    context.lineTo(-12 + hem_sway, hem_y + 1 + Math.abs(hem_sway) * 0.2);
    context.quadraticCurveTo(-11, -torso * 0.4, -8, -torso + 1);
    context.closePath();
    context.fill();
    context.strokeStyle = outline_color;
    context.lineWidth = 1;
    context.stroke();
    context.strokeStyle = 'rgba(0,0,0,0.25)';
    context.beginPath();
    context.moveTo(3, -torso + 4);
    context.lineTo(4 + hem_sway * 0.3, hem_y - 2);
    context.moveTo(-4, -torso * 0.35);
    context.lineTo(8, -torso * 0.35);
    context.stroke();
    context.strokeStyle = 'rgba(160,220,215,0.35)';
    context.lineWidth = 1.5;
    context.beginPath();
    context.moveTo(9, -torso + 2);
    context.quadraticCurveTo(12, -torso * 0.4, 12.5 + hem_sway * 0.4, hem_y - 2);
    context.stroke();
    context.restore();

    // Bag strap across the chest.
    context.strokeStyle = '#4b3a27';
    context.lineWidth = 1.8;
    context.beginPath();
    context.moveTo(shape.shoulder[0] + 6, shape.shoulder[1] + 1);
    context.lineTo(shape.hip[0] - 8, shape.hip[1] - 9);
    context.stroke();

    // Scarf: wrapped at the neck, the tail streaming back on a spring.
    const scarf_lift = scarf_flow * 6;
    const neck = shape.shoulder;
    context.fillStyle = human_shades.scarf;
    context.beginPath();
    context.ellipse(neck[0] + 1.5, neck[1] - 2.5, 7.5, 3.8, torso_angle, 0, Math.PI * 2);
    context.fill();
    context.fillStyle = human_shades.scarf_dark;
    context.beginPath();
    context.moveTo(neck[0] - 4, neck[1] - 1);
    context.quadraticCurveTo(neck[0] - 11 - scarf_lift * 0.5, neck[1] + 5 - scarf_lift, neck[0] - 15 - scarf_lift * 1.6, neck[1] + 11 - scarf_lift * 1.5);
    context.lineTo(neck[0] - 10 - scarf_lift * 1.5, neck[1] + 13 - scarf_lift * 1.2);
    context.quadraticCurveTo(neck[0] - 6, neck[1] + 6, neck[0] + 1, neck[1] + 1);
    context.fill();

    // Head, tilted down a little when she looks at the dog close by.
    const head = shape.head;
    context.save();
    context.translate(head[0], head[1]);
    context.rotate((human.head_tilt ? human.head_tilt.value : 0) + torso_angle * 0.4);
    context.translate(-head[0], -head[1]);
    draw_human_head(context, human, head, hair_angle, pose);
    context.restore();

    // Front arm last, so it sits over the coat; a bare hand at the end.
    draw_limb(context, shape.shoulder, elbow_front, shape.hand_front, human_shades.coat, 6, 5.2);
    context.fillStyle = human_shades.skin;
    context.beginPath();
    context.arc(shape.hand_front[0], shape.hand_front[1], 2.7, 0, Math.PI * 2);
    context.fill();
    if (human.holding) {
        draw_carried_item(context, human.holding, shape.hand_front[0] + 3, shape.hand_front[1] - 2);
    }
    context.restore();
}

// Her face in profile, ear, blue eye and blonde hair with a ponytail that swings on a spring.
function draw_human_head(context, human, head, hair_angle, pose) {
    context.fillStyle = human_shades.skin;
    context.beginPath();
    context.ellipse(head[0], head[1], 7.6, 8.6, 0, 0, Math.PI * 2);
    context.fill();
    context.beginPath();
    context.moveTo(head[0] + 7, head[1] - 1);
    context.quadraticCurveTo(head[0] + 10, head[1] + 1.5, head[0] + 7, head[1] + 2.5);
    context.fill();
    context.fillStyle = 'rgba(220,120,110,0.25)';
    context.beginPath();
    context.arc(head[0] + 3.5, head[1] + 3, 2.2, 0, Math.PI * 2);
    context.fill();
    context.save();
    context.translate(head[0] - 6, head[1] - 4);
    context.rotate(0.35 + hair_angle);
    const pony = context.createLinearGradient(0, 0, 0, 16);
    pony.addColorStop(0, human_shades.hair);
    pony.addColorStop(1, human_shades.hair_dark);
    context.fillStyle = pony;
    context.beginPath();
    context.moveTo(-2, 0);
    context.quadraticCurveTo(-7 + hair_angle * 4, 6, -4 + hair_angle * 6, 16);
    context.quadraticCurveTo(-1, 12, 3, 1);
    context.closePath();
    context.fill();
    context.restore();
    const hair = context.createLinearGradient(head[0], head[1] - 10, head[0], head[1] + 6);
    hair.addColorStop(0, human_shades.hair_light);
    hair.addColorStop(1, human_shades.hair);
    context.fillStyle = hair;
    context.beginPath();
    context.moveTo(head[0] + 7.5, head[1] - 3);
    context.quadraticCurveTo(head[0] + 6, head[1] - 10.5, head[0] - 1, head[1] - 10);
    context.quadraticCurveTo(head[0] - 9, head[1] - 9, head[0] - 8.5, head[1] + 1);
    context.quadraticCurveTo(head[0] - 8, head[1] + 7, head[0] - 3.5, head[1] + 8);
    context.quadraticCurveTo(head[0] - 3, head[1] + 1, head[0] - 1, head[1] - 3);
    context.quadraticCurveTo(head[0] + 3, head[1] - 6, head[0] + 7.5, head[1] - 3);
    context.closePath();
    context.fill();
    context.strokeStyle = outline_color;
    context.lineWidth = 0.8;
    context.stroke();
    context.strokeStyle = 'rgba(255,245,210,0.65)';
    context.lineWidth = 1;
    context.beginPath();
    context.moveTo(head[0] - 5, head[1] - 7);
    context.quadraticCurveTo(head[0] + 1, head[1] - 10, head[0] + 5, head[1] - 6);
    context.stroke();
    context.fillStyle = human_shades.skin_shadow;
    context.beginPath();
    context.ellipse(head[0] - 1, head[1] + 0.5, 1.8, 2.4, 0, 0, Math.PI * 2);
    context.fill();
    // Blinks every few seconds: a tiny thing that makes a figure feel alive.
    const blinking = ((human.x || 0) * 0.37 + performance.now() / 1000) % 4.3 < 0.12;
    if (human.eyes_closed || pose === 'embrace' || blinking) {
        context.strokeStyle = '#5a4136';
        context.lineWidth = 1.1;
        context.beginPath();
        context.arc(head[0] + 4, head[1] - 1, 1.7, 0.2, Math.PI - 0.2);
        context.stroke();
    } else {
        context.fillStyle = '#ffffff';
        context.beginPath();
        context.ellipse(head[0] + 4.3, head[1] - 1.2, 1.7, 1.4, 0, 0, Math.PI * 2);
        context.fill();
        context.fillStyle = human_shades.eye;
        context.beginPath();
        context.arc(head[0] + 4.9, head[1] - 1.2, 1.15, 0, Math.PI * 2);
        context.fill();
        context.strokeStyle = '#4a3326';
        context.lineWidth = 0.9;
        context.beginPath();
        context.moveTo(head[0] + 2.5, head[1] - 3.4);
        context.lineTo(head[0] + 6.3, head[1] - 3.1);
        context.stroke();
    }
}

// ---------- Background scenery ----------

// Painted scenery images (SVG files in art/scenery/), loaded once and kept, by path.
const scenery_images = {};

// Loads a chapter's painted background layers, if it has any. When they arrive, the backdrop
// is repainted with them; until then the code-drawn layers stand in.
function load_scenery_art(level) {
    const art = level.scenery.art;
    if (!art) {
        return;
    }
    for (const art_path of Object.values(art)) {
        if (scenery_images[art_path]) {
            continue;
        }
        const image = new Image();
        const from_repository = window.location.pathname.indexOf('/prototype/web/') !== -1;
        const candidates = from_repository ? ['../../' + art_path, art_path] : [art_path, '../../' + art_path];
        let attempt = 0;
        scenery_images[art_path] = { image: image, loaded: false };
        image.onload = function scenery_loaded() {
            scenery_images[art_path].loaded = true;
            if (game.level === level) {
                game.backdrop_layers = render_backdrop(level, game.pixel_scale, view_width, view_height);
            }
        };
        image.onerror = function scenery_failed() {
            attempt += 1;
            if (attempt < candidates.length) {
                image.src = candidates[attempt];
            }
        };
        image.src = candidates[0];
    }
}

// The loaded painted image for one background depth ('far', 'mid' or 'near'), or null.
function painted_layer(level, depth_name) {
    const art_path = level.scenery.art && level.scenery.art[depth_name];
    const entry = art_path && scenery_images[art_path];
    return entry && entry.loaded ? entry.image : null;
}

// Paints the far and middle background layers for a level once, onto hidden canvases.
// Drawing them once and copying them each frame is much cheaper on a phone.
function render_backdrop(level, device_scale, screen_width, screen_height) {
    const scenery = level.scenery;
    const layers = [];
    const layer_plans = [
        { kind: scenery.far, factor: 0.16, seed: 11, depth: 0, sharpness: 1, name: 'far' },
        { kind: scenery.mid, factor: 0.42, seed: 23, depth: 1, sharpness: 1, name: 'mid' },
        { kind: scenery.near, factor: 0.7, seed: 37, depth: 2, sharpness: 0.5, name: 'near' },
    ];
    layer_plans.forEach(function paint_layer(plan, index) {
        if (!plan.kind) {
            return;
        }
        const scale = device_scale * plan.sharpness;
        const width_units = screen_width + (level.width + 400) * plan.factor;
        const height_units = Math.max(800, screen_height + 260);
        const canvas = make_sprite_canvas(width_units * scale, height_units * scale);
        const context = canvas.getContext('2d');
        context.scale(scale, scale);
        // A painted layer, if the chapter has one, is repeated across; otherwise code draws it.
        const painting = painted_layer(level, plan.name);
        if (painting) {
            for (let tile_x = 0; tile_x < width_units; tile_x += 1600) {
                context.drawImage(painting, tile_x, 0, 1600, 800);
            }
        } else {
            paint_backdrop_layer(context, plan.kind, width_units, height_units, scenery, make_seeded_random(plan.seed + level.width), plan.depth);
        }
        // Atmospheric perspective: the farther the layer, the more it fades toward the sky.
        // Painted layers carry their own haze, so they get less.
        const haze = context.createLinearGradient(0, 0, 0, height_units);
        const amount = [0.5, 0.24, 0.1][plan.depth] * (painting ? 0.45 : 1);
        haze.addColorStop(0, hex_with_alpha(scenery.sky_bottom, amount));
        haze.addColorStop(0.7, hex_with_alpha(scenery.sky_bottom, amount * 0.6));
        haze.addColorStop(1, hex_with_alpha(scenery.sky_top, 0.15));
        context.globalCompositeOperation = 'source-atop';
        context.fillStyle = haze;
        context.fillRect(0, 0, width_units, height_units);
        if (scenery.fog && plan.depth < 2) {
            context.fillStyle = scenery.fog;
            context.fillRect(0, 0, width_units, height_units);
        }
        context.globalCompositeOperation = 'destination-over';
        if (index === 0 && scenery.horizon_glow) {
            const horizon = context.createRadialGradient(width_units / 2, 520, 0, width_units / 2, 520, width_units * 0.6);
            horizon.addColorStop(0, scenery.horizon_glow);
            horizon.addColorStop(1, 'rgba(0,0,0,0)');
            context.save();
            context.translate(0, 520);
            context.scale(1, 0.35);
            context.translate(0, -520);
            context.fillStyle = horizon;
            context.fillRect(0, 0, width_units, height_units * 3);
            context.restore();
        }
        if (index === 0) {
            // The sky lives behind the farthest layer, so it costs nothing extra each frame.
            const sky = context.createLinearGradient(0, 90, 0, 90 + screen_height);
            sky.addColorStop(0, scenery.sky_top);
            sky.addColorStop(1, scenery.sky_bottom);
            if (scenery.sun) {
                const sun_x = width_units * 0.55;
                const glow = context.createRadialGradient(sun_x, 90 + screen_height * 0.62, 6, sun_x, 90 + screen_height * 0.62, screen_height * 0.9);
                glow.addColorStop(0, 'rgba(255,236,190,1)');
                glow.addColorStop(0.06, 'rgba(255,214,150,0.85)');
                glow.addColorStop(0.3, 'rgba(255,180,120,0.3)');
                glow.addColorStop(1, 'rgba(255,170,110,0)');
                context.fillStyle = glow;
                context.fillRect(0, 0, width_units, height_units);
            }
            context.fillStyle = sky;
            context.fillRect(0, 0, width_units, height_units);
        }
        context.globalCompositeOperation = 'source-over';
        layers.push({ canvas: canvas, factor: plan.factor, scale: scale, first: index === 0 });
    });
    return layers;
}

// Turns "#rrggbb" plus an opacity into a colour string the canvas understands.
function hex_with_alpha(hex_color, alpha) {
    const value = parseInt(hex_color.slice(1), 16);
    return 'rgba(' + ((value >> 16) & 255) + ',' + ((value >> 8) & 255) + ',' + (value & 255) + ',' + alpha + ')';
}

// Draws one background layer of the named kind across the whole width.
function paint_backdrop_layer(context, kind, width, height, scenery, random, depth) {
    const far_color = scenery.far_color || '#1d2531';
    const mid_color = scenery.mid_color || '#262d38';
    const window_color = scenery.window_color || 'rgba(240,182,94,0.55)';
    const horizon = 500;
    if (kind === 'city' || kind === 'hospital_blocks') {
        let x = 0;
        while (x < width) {
            const building_width = 70 + random() * 150;
            const building_height = (kind === 'hospital_blocks' ? 280 : 170) + random() * 280;
            const top = horizon - building_height;
            context.fillStyle = far_color;
            context.fillRect(x, top, building_width, height - top);
            if (random() < 0.3) {
                context.fillRect(x + building_width * 0.6, top - 30, 4, 30);  // An aerial.
            }
            context.fillStyle = 'rgba(255,255,255,0.03)';
            context.fillRect(x, top, 3, building_height);
            for (let window_y = top + 16; window_y < horizon - 10; window_y += 22) {
                for (let window_x = x + 9; window_x < x + building_width - 12; window_x += 16) {
                    if (random() < (kind === 'hospital_blocks' ? 0.05 : 0.03)) {
                        context.fillStyle = window_color;
                        context.fillRect(window_x, window_y, 7, 10);
                    }
                }
            }
            if (kind === 'hospital_blocks' && random() < 0.2) {
                context.fillStyle = 'rgba(190,70,70,0.45)';
                context.fillRect(x + building_width / 2 - 9, top + 12, 18, 5);
                context.fillRect(x + building_width / 2 - 2.5, top + 5, 5, 19);
            }
            x += building_width + random() * 24;
        }
        // Flood water at the foot of the skyline, with the lit windows reflected in it.
        const water = context.createLinearGradient(0, horizon, 0, height);
        water.addColorStop(0, 'rgba(90,110,130,0.5)');
        water.addColorStop(1, 'rgba(15,22,32,0.95)');
        context.fillStyle = water;
        context.fillRect(0, horizon, width, height - horizon);
        context.fillStyle = 'rgba(240,190,120,0.12)';
        for (let streak = 0; streak < width / 60; streak++) {
            context.fillRect(random() * width, horizon + 6 + random() * 60, 2, 10 + random() * 30);
        }
    } else if (kind === 'station_roof') {
        context.strokeStyle = mid_color;
        for (let x = 0; x < width; x += 280) {
            context.lineWidth = 9;
            context.beginPath();
            context.moveTo(x, 560);
            context.lineTo(x, 180);
            context.quadraticCurveTo(x + 140, 50, x + 280, 180);
            context.stroke();
            context.lineWidth = 2;
            for (let rib = 1; rib < 7; rib++) {
                const rib_x = x + rib * 40;
                const rib_top = 180 - Math.sin((rib / 7) * Math.PI) * 95;
                context.beginPath();
                context.moveTo(rib_x, rib_top);
                context.lineTo(rib_x, rib_top + 30);
                context.stroke();
            }
            // Broken glass panes letting the sky through.
            context.fillStyle = 'rgba(160,180,200,0.06)';
            context.beginPath();
            context.moveTo(x + 20, 170);
            context.quadraticCurveTo(x + 140, 60, x + 260, 170);
            context.lineTo(x + 260, 185);
            context.quadraticCurveTo(x + 140, 75, x + 20, 185);
            context.fill();
        }
        for (let x = 140; x < width; x += 560) {
            context.fillStyle = mid_color;
            context.beginPath();
            context.arc(x, 250, 30, 0, Math.PI * 2);
            context.fill();
            context.fillStyle = 'rgba(239,230,212,0.16)';
            context.beginPath();
            context.arc(x, 250, 24, 0, Math.PI * 2);
            context.fill();
            context.strokeStyle = mid_color;
            context.lineWidth = 2;
            context.beginPath();
            context.moveTo(x, 250);
            context.lineTo(x, 234);
            context.moveTo(x, 250);
            context.lineTo(x + 11, 255);
            context.stroke();
        }
    } else if (kind === 'corridor') {
        for (let x = 0; x < width; x += 200) {
            context.fillStyle = mid_color;
            context.fillRect(x, 90, 20, 500);
            context.fillStyle = 'rgba(200,225,220,0.05)';
            context.fillRect(x + 50, 160, 100, 130);
            context.fillStyle = 'rgba(200,225,220,0.04)';
            context.fillRect(x + 50, 300, 100, 4);
        }
    } else if (kind === 'trees' || kind === 'school_blocks') {
        if (kind === 'school_blocks') {
            for (let x = 60; x < width; x += 760) {
                context.fillStyle = mid_color;
                context.fillRect(x, 330, 380, 240);
                context.beginPath();
                context.moveTo(x - 14, 330);
                context.lineTo(x + 190, 262);
                context.lineTo(x + 394, 330);
                context.fill();
                context.fillRect(x + 300, 230, 22, 60);  // Chimney.
                context.fillStyle = 'rgba(239,230,212,0.10)';
                for (let window_x = x + 24; window_x < x + 350; window_x += 52) {
                    context.fillRect(window_x, 365, 32, 44);
                    context.fillRect(window_x, 440, 32, 44);
                }
            }
        }
        const tree_color = depth === 0 ? far_color : mid_color;
        for (let x = -40; x < width; x += 26 + random() * 46) {
            const tree_height = 140 + random() * 180;
            const crown_y = horizon - tree_height * 0.62;
            context.fillStyle = tree_color;
            context.fillRect(x - 3, crown_y, 6, horizon - crown_y + 80);
            for (let puff = 0; puff < 5; puff++) {
                context.beginPath();
                context.arc(x + (random() - 0.5) * 50, crown_y + (random() - 0.6) * tree_height * 0.45, 22 + random() * 26, 0, Math.PI * 2);
                context.fill();
            }
        }
        context.fillStyle = tree_color;
        context.fillRect(0, horizon + 40, width, height);
    } else if (kind.indexOf('near_') === 0) {
        // Close background structures, painted dark and soft: they sit just behind the action.
        const near_color = scenery.near_color || darken(scenery.sky_top, 0.35);
        context.fillStyle = near_color;
        if (kind === 'near_columns' || kind === 'near_doorways') {
            for (let x = 60; x < width; x += 420 + random() * 220) {
                if (kind === 'near_columns') {
                    context.fillRect(x, 120, 34, height);
                    context.fillRect(x - 12, 110, 58, 16);
                    context.beginPath();
                    context.moveTo(x + 34, 140);
                    context.quadraticCurveTo(x + 160, 60, x + 300, 140);
                    context.lineTo(x + 300, 152);
                    context.quadraticCurveTo(x + 160, 74, x + 34, 152);
                    context.fill();
                } else {
                    context.fillRect(x, 200, 16, height);
                    context.fillRect(x + 110, 200, 16, height);
                    context.fillRect(x, 190, 126, 18);
                }
            }
        } else if (kind === 'near_trees' || kind === 'near_hedges') {
            for (let x = 0; x < width; x += 140 + random() * 260) {
                const size = kind === 'near_trees' ? 60 + random() * 50 : 30 + random() * 26;
                const base = kind === 'near_trees' ? 560 : 610;
                if (kind === 'near_trees') {
                    context.fillRect(x - 5, base - size * 2.6, 10, size * 2.6 + 200);
                }
                for (let puff = 0; puff < 6; puff++) {
                    context.beginPath();
                    context.arc(x + (random() - 0.5) * size * 1.6, base - size * (kind === 'near_trees' ? 2.6 : 0.6) + (random() - 0.5) * size, size * (0.5 + random() * 0.4), 0, Math.PI * 2);
                    context.fill();
                }
            }
            context.fillRect(0, kind === 'near_trees' ? 600 : 625, width, height);
        } else if (kind === 'near_street') {
            for (let x = 40; x < width; x += 300 + random() * 260) {
                context.fillRect(x, 330, 7, 400);
                context.beginPath();
                context.arc(x + 3, 330, 9, Math.PI, 0);
                context.fill();
                if (random() < 0.5) {
                    context.beginPath();
                    context.moveTo(x + 60, 640);
                    context.quadraticCurveTo(x + 80, 585, x + 140, 585);
                    context.lineTo(x + 200, 585);
                    context.quadraticCurveTo(x + 240, 600, x + 250, 640);
                    context.fill();
                }
            }
            context.fillRect(0, 630, width, height);
        }
    } else if (kind === 'hills' || kind === 'river_hills') {
        const bands = [[360, 70, far_color], [430, 46, mid_color]];
        for (const [base, amplitude, color] of bands) {
            context.fillStyle = color;
            context.beginPath();
            context.moveTo(0, height);
            for (let x = 0; x <= width; x += 16) {
                context.lineTo(x, base - Math.sin(x * 0.0035 + base) * amplitude - Math.sin(x * 0.011 + 2) * amplitude * 0.3);
            }
            context.lineTo(width, height);
            context.fill();
        }
        if (kind === 'river_hills') {
            context.strokeStyle = far_color;
            context.lineWidth = 7;
            for (let x = 300; x < width; x += 1300) {
                context.beginPath();
                context.moveTo(x, 470);
                context.quadraticCurveTo(x + 150, 370, x + 300, 470);
                context.moveTo(x + 340, 470);
                context.quadraticCurveTo(x + 390, 440, x + 430, 460);
                context.stroke();
            }
        } else {
            // Hedgerows and small trees along the ridges.
            for (let x = 0; x < width; x += 40 + random() * 90) {
                const ridge = 430 - Math.sin(x * 0.0035 + 430) * 46 - Math.sin(x * 0.011 + 2) * 14;
                context.fillStyle = mid_color;
                context.beginPath();
                context.arc(x, ridge - 6, 8 + random() * 10, 0, Math.PI * 2);
                context.fill();
            }
        }
    }
}

// Draws the sky and the pre-painted background layers, shifted for depth (parallax).
function draw_backdrop(context, level, layers, camera_left, camera_top, view_width, time_seconds, lightning) {
    const scenery = level.scenery;
    const screen_scale = context.getTransform().a;
    if (!layers.length || !layers[0].first) {
        context.fillStyle = scenery.sky_bottom;
        context.fillRect(0, 0, view_width, view_height);
    }
    // Copy only the visible part of each pre-painted layer, pixel for pixel where possible.
    context.save();
    context.setTransform(1, 0, 0, 1, 0, 0);
    const screen_width_pixels = Math.ceil(view_width * screen_scale);
    const screen_height_pixels = Math.ceil(view_height * screen_scale);
    for (const layer of layers) {
        const source_scale = layer.scale;
        const source_x = Math.max(0, camera_left * layer.factor * source_scale);
        const source_y = Math.max(0, (camera_top * layer.factor * 0.5 + 90) * source_scale);
        const source_width = Math.min(layer.canvas.width - source_x, screen_width_pixels * source_scale / screen_scale);
        const source_height = Math.min(layer.canvas.height - source_y, screen_height_pixels * source_scale / screen_scale);
        if (source_width <= 0 || source_height <= 0) {
            continue;
        }
        context.drawImage(layer.canvas, Math.round(source_x), Math.round(source_y), Math.floor(source_width), Math.floor(source_height),
            0, 0, Math.floor(source_width) * screen_scale / source_scale, Math.floor(source_height) * screen_scale / source_scale);
    }
    context.restore();
    if (lightning > 0) {
        context.fillStyle = 'rgba(200,215,255,' + (lightning * 0.5).toFixed(2) + ')';
        context.fillRect(0, 0, view_width, view_height);
    }
    if (scenery.smoke_x) {
        draw_smoke_column(context, scenery.smoke_x - camera_left * 0.16, 330 - camera_top * 0.08, time_seconds);
    }
}

// A thin column of chimney smoke on a far hill: the sign that someone is alive out there.
function draw_smoke_column(context, x, ground_y, time_seconds) {
    for (let index = 0; index < 16; index++) {
        const rise = index * 17;
        const drift = Math.sin(time_seconds * 0.5 + index * 0.6) * (3 + index * 1.4) + index * 3.5;
        context.fillStyle = 'rgba(225,220,212,' + (0.24 - index * 0.013).toFixed(3) + ')';
        context.beginPath();
        context.arc(x + drift, ground_y - rise, 4 + index * 1.4, 0, Math.PI * 2);
        context.fill();
    }
    const ember = 0.7 + 0.3 * Math.sin(time_seconds * 9);
    context.fillStyle = 'rgba(255,180,100,' + ember.toFixed(2) + ')';
    context.beginPath();
    context.arc(x, ground_y + 1, 2.5, 0, Math.PI * 2);
    context.fill();
}

// ---------- The world's solid parts ----------

// Draws one solid piece of the world, moving it physically when it opens, closes or arrives:
// gates slide up into the ceiling, fire doors drop and bounce, gangplanks swing down from
// upright, crates fall and land. (The collision changes at once; this is only the picture.)
function draw_solid(context, solid, palette, time_seconds) {
    const motion = solid.motion;
    if (!motion) {
        draw_solid_shape(context, solid, palette, time_seconds);
        return;
    }
    context.save();
    if (motion.type === 'slide_open' || motion.type === 'drop_shut') {
        const progress = motion.type === 'slide_open' ? ease_in_out(motion.seconds / 0.9) : 1 - ease_out_bounce(motion.seconds / 0.45);
        if (progress >= 1) {
            context.restore();
            return;
        }
        context.beginPath();
        context.rect(solid.x - 6, solid.y, solid.w + 12, solid.h);
        context.clip();
        context.translate(0, -solid.h * progress);
    } else if (motion.type === 'fall') {
        context.translate(0, -170 * (1 - ease_out_bounce(motion.seconds / 0.45)));
    } else if (motion.type === 'swing_down') {
        const angle = -Math.PI / 2 * (1 - ease_out_bounce(motion.seconds / 0.8));
        context.translate(solid.x, solid.y + solid.h / 2);
        context.rotate(angle);
        context.translate(-solid.x, -(solid.y + solid.h / 2));
    }
    draw_solid_shape(context, solid, palette, time_seconds);
    context.restore();
}

// Draws the shape of one solid piece: floors, walls, gates, fences, planks, crates.
function draw_solid_shape(context, solid, palette, time_seconds) {
    if (solid.kind === 'invisible' || solid.fade <= 0.01) {
        return;
    }
    context.save();
    context.globalAlpha = solid.fade;
    const { x, y, w, h } = solid;
    switch (solid.kind) {
        case 'floor': {
            // The top surface, seen slightly from above: a band that recedes into the scene,
            // so floors read as places you stand on, not lines you stand on.
            const surface_depth = 12;
            context.fillStyle = cached_gradient(context, solid, 'surface', function make_surface() {
                const surface = context.createLinearGradient(0, y - surface_depth, 0, y);
                surface.addColorStop(0, hex_with_alpha(palette.ground_top, 0));
                surface.addColorStop(0.35, hex_with_alpha(palette.ground_top, 0.35));
                surface.addColorStop(1, hex_with_alpha(palette.ground_top, 0.75));
                return surface;
            });
            context.fillRect(x, y - surface_depth, w, surface_depth);
            context.strokeStyle = 'rgba(0,0,0,0.12)';
            context.lineWidth = 1;
            context.beginPath();
            for (let line_x = x + 48 - (((x % 48) + 48) % 48); line_x < x + w; line_x += 48) {
                context.moveTo(line_x, y);
                context.lineTo(line_x + 6, y - surface_depth);
            }
            context.stroke();
            // Body fades into darkness below; the front edge has a lip and a wet sheen.
            context.fillStyle = cached_gradient(context, solid, 'body', function make_body() {
                const body = context.createLinearGradient(0, y, 0, y + Math.min(h, 140));
                body.addColorStop(0, palette.ground_body);
                body.addColorStop(1, palette.darkness_floor || 'rgb(8,10,14)');
                return body;
            });
            context.fillRect(x, y, w, h);
            context.fillStyle = palette.ground_top;
            context.fillRect(x, y, w, 4);
            context.fillStyle = 'rgba(255,255,255,0.12)';
            context.fillRect(x, y, w, 1);
            context.fillStyle = 'rgba(0,0,0,0.25)';
            context.fillRect(x, y + 4, w, 3);
            context.fillStyle = 'rgba(0,0,0,0.16)';
            for (let tile_x = x + 48 - (((x % 48) + 48) % 48); tile_x < x + w; tile_x += 48) {
                context.fillRect(tile_x, y + 7, 1, Math.min(h - 7, 20));
            }
            if (h <= 34) {
                context.fillStyle = 'rgba(0,0,0,0.5)';
                context.fillRect(x, y + h - 3, w, 3);  // The slab's shadowed underside.
            }
            break;
        }
        case 'wall': {
            const wall = context.createLinearGradient(x, 0, x + w, 0);
            wall.addColorStop(0, palette.wall);
            wall.addColorStop(1, 'rgba(0,0,0,0.6)');
            context.fillStyle = palette.wall;
            context.fillRect(x, y, w, h);
            context.fillStyle = wall;
            context.fillRect(x, y, w, h);
            context.fillStyle = 'rgba(0,0,0,0.22)';
            for (let row = 0; row * 12 < h; row++) {
                context.fillRect(x, y + row * 12, w, 1);
            }
            context.fillStyle = 'rgba(255,255,255,0.08)';
            context.fillRect(x, y, w, 2);
            break;
        }
        case 'plank': {
            context.fillStyle = '#4e3c2c';
            context.fillRect(x, y, w, h);
            context.fillStyle = '#6a523c';
            context.fillRect(x, y, w, 3);
            context.fillStyle = 'rgba(0,0,0,0.3)';
            for (let plank_x = x + 22; plank_x < x + w; plank_x += 22) {
                context.fillRect(plank_x, y + 3, 1, h - 3);
            }
            context.strokeStyle = 'rgba(30,30,32,0.9)';
            context.lineWidth = 2;
            context.beginPath();
            context.moveTo(x, y - 20);
            context.lineTo(x + w, y - 20);
            for (let post_x = x; post_x <= x + w; post_x += 44) {
                context.moveTo(post_x, y);
                context.lineTo(post_x, y - 20);
            }
            context.stroke();
            break;
        }
        case 'gate': {
            const gate = context.createLinearGradient(x, 0, x + w, 0);
            gate.addColorStop(0, '#4b555e');
            gate.addColorStop(1, '#2b3238');
            context.fillStyle = gate;
            context.fillRect(x, y, w, h);
            context.fillStyle = 'rgba(0,0,0,0.3)';
            for (let rib_y = y + 6; rib_y < y + h; rib_y += 9) {
                context.fillRect(x, rib_y, w, 2);
            }
            context.fillStyle = 'rgba(210,90,50,0.75)';
            context.fillRect(x, y + h * 0.45, w, 5);
            context.fillStyle = 'rgba(255,255,255,0.1)';
            context.fillRect(x, y, 1.5, h);
            break;
        }
        case 'bars': {
            context.strokeStyle = '#5b646d';
            context.lineWidth = 3;
            context.beginPath();
            for (let bar_x = x + 2; bar_x < x + w; bar_x += 7) {
                context.moveTo(bar_x, y);
                context.lineTo(bar_x, y + h);
            }
            context.stroke();
            context.strokeStyle = 'rgba(255,255,255,0.15)';
            context.lineWidth = 1;
            context.beginPath();
            for (let bar_x = x + 1.5; bar_x < x + w; bar_x += 7) {
                context.moveTo(bar_x, y);
                context.lineTo(bar_x, y + h);
            }
            context.stroke();
            context.fillStyle = '#4b535b';
            context.fillRect(x - 3, y, w + 6, 6);
            context.fillRect(x - 3, y + h * 0.45, w + 6, 5);
            break;
        }
        case 'glass': {
            context.fillStyle = 'rgba(150,190,205,0.16)';
            context.fillRect(x, y, w, h);
            context.strokeStyle = 'rgba(200,225,235,0.5)';
            context.lineWidth = 2;
            context.strokeRect(x, y, w, h);
            context.strokeStyle = 'rgba(230,245,250,0.35)';
            context.lineWidth = 1.5;
            context.beginPath();
            context.moveTo(x + 3, y + 30);
            context.lineTo(x + w - 3, y + 70);
            context.moveTo(x + 3, y + 46);
            context.lineTo(x + w - 3, y + 80);
            context.stroke();
            break;
        }
        case 'crate': {
            context.fillStyle = '#6e5337';
            context.fillRect(x, y, w, h);
            context.strokeStyle = '#4a3825';
            context.lineWidth = 3;
            context.strokeRect(x + 1.5, y + 1.5, w - 3, h - 3);
            context.beginPath();
            context.moveTo(x + 3, y + 3);
            context.lineTo(x + w - 3, y + h - 3);
            context.stroke();
            context.fillStyle = 'rgba(255,255,255,0.1)';
            context.fillRect(x, y, w, 2);
            break;
        }
        case 'suitcase': {
            context.fillStyle = solid.color || '#6a3f35';
            context.beginPath();
            if (context.roundRect) {
                context.roundRect(x, y + 4, w, h - 4, 4);
            } else {
                context.rect(x, y + 4, w, h - 4);
            }
            context.fill();
            context.fillStyle = 'rgba(255,255,255,0.12)';
            context.fillRect(x + 2, y + 5, w - 4, 2);
            context.fillStyle = 'rgba(0,0,0,0.35)';
            context.fillRect(x + w / 2 - 7, y, 14, 5);
            context.fillRect(x, y + h / 2, w, 2);
            break;
        }
        default:
            context.fillStyle = palette.wall;
            context.fillRect(x, y, w, h);
    }
    context.restore();
}

// Draws decoration that doesn't block movement: back walls, windows, lamps, signs, furniture.
function draw_prop(context, prop, palette, time_seconds) {
    const { x, y } = prop;
    switch (prop.kind) {
        case 'room_wall': {
            context.fillStyle = cached_gradient(context, prop, 'wall', function make_wall() {
                const wall = context.createLinearGradient(0, y, 0, y + prop.h);
                wall.addColorStop(0, 'rgba(0,0,0,0.6)');
                wall.addColorStop(0.25, prop.color || palette.room_wall || '#2a2f36');
                wall.addColorStop(0.85, prop.color || palette.room_wall || '#2a2f36');
                wall.addColorStop(1, 'rgba(0,0,0,0.55)');
                return wall;
            });
            context.fillRect(x, y, prop.w, prop.h);
            context.fillStyle = 'rgba(255,255,255,0.025)';
            for (let panel_x = x + 30; panel_x < x + prop.w; panel_x += 90) {
                context.fillRect(panel_x, y + 10, 1.5, prop.h - 24);
            }
            context.fillStyle = 'rgba(0,0,0,0.3)';
            context.fillRect(x, y + prop.h - 12, prop.w, 12);
            break;
        }
        case 'window': {
            const w = prop.w || 60;
            const h = prop.h || 80;
            const glass = context.createLinearGradient(0, y, 0, y + h);
            if (prop.lit) {
                glass.addColorStop(0, 'rgba(255,214,150,0.95)');
                glass.addColorStop(1, 'rgba(230,150,80,0.85)');
            } else {
                glass.addColorStop(0, 'rgba(140,165,190,0.38)');
                glass.addColorStop(1, 'rgba(80,100,125,0.25)');
            }
            context.fillStyle = glass;
            context.fillRect(x, y, w, h);
            context.strokeStyle = '#141820';
            context.lineWidth = 4;
            context.strokeRect(x, y, w, h);
            context.beginPath();
            context.moveTo(x + w / 2, y);
            context.lineTo(x + w / 2, y + h);
            context.moveTo(x, y + h / 2);
            context.lineTo(x + w, y + h / 2);
            context.stroke();
            if (!prop.lit) {
                context.strokeStyle = 'rgba(210,228,240,0.35)';
                context.lineWidth = 1;
                context.beginPath();
                for (let drop = 0; drop < 7; drop++) {
                    const drop_x = x + 3 + ((drop * 13.7) % (w - 6));
                    const drop_y = y + ((drop * 29 + time_seconds * (30 + drop * 6)) % h);
                    context.moveTo(drop_x, drop_y);
                    context.lineTo(drop_x + 0.5, Math.min(y + h, drop_y + 6));
                }
                context.stroke();
            }
            break;
        }
        case 'lamp': {
            if (prop.sway) {
                context.save();
                context.translate(x, y);
                context.rotate(prop.sway.value);
                context.translate(-x, -y);
                draw_prop(context, Object.assign({}, prop, { sway: null }), palette, time_seconds);
                context.restore();
                break;
            }
            context.strokeStyle = '#151a20';
            context.lineWidth = 2.5;
            context.beginPath();
            context.moveTo(x, y);
            context.lineTo(x, y + (prop.hanging ? 40 : 130));
            context.stroke();
            const bulb_y = prop.hanging ? y + 40 : y;
            context.fillStyle = '#151a20';
            context.beginPath();
            context.moveTo(x - 12, bulb_y);
            context.lineTo(x + 12, bulb_y);
            context.lineTo(x + 6, bulb_y - 8);
            context.lineTo(x - 6, bulb_y - 8);
            context.closePath();
            context.fill();
            if (!prop.off) {
                context.fillStyle = 'rgba(255,236,190,1)';
                context.beginPath();
                context.ellipse(x, bulb_y + 1.5, 6, 2.5, 0, 0, Math.PI * 2);
                context.fill();
            }
            break;
        }
        case 'sign': {
            const text = personalize_if_available(prop.text);
            context.font = '700 12px "Atkinson Hyperlegible", Arial, sans-serif';
            if (prop.measured_width === undefined) {
                prop.measured_width = context.measureText(text).width;
            }
            const sign_width = prop.measured_width + 22;
            context.fillStyle = prop.color || '#1f3a4f';
            context.fillRect(x, y, sign_width, 26);
            context.strokeStyle = 'rgba(255,255,255,0.18)';
            context.lineWidth = 1;
            context.strokeRect(x + 2.5, y + 2.5, sign_width - 5, 21);
            context.fillStyle = '#ebe4d3';
            context.textBaseline = 'middle';
            context.fillText(text, x + 11, y + 13.5);
            break;
        }
        case 'scrawl': {
            context.fillStyle = prop.color || 'rgba(236,232,220,0.75)';
            context.font = 'italic 15px "Newsreader", Georgia, serif';
            context.textBaseline = 'alphabetic';
            const lines = personalize_if_available(prop.text).split('\n');
            lines.forEach(function draw_scrawl_line(line, index) {
                context.fillText(line, x, y + index * 18);
            });
            break;
        }
        case 'bench': {
            context.fillStyle = '#3e3328';
            context.fillRect(x, y - 22, 90, 6);
            context.fillRect(x, y - 42, 90, 5);
            context.fillRect(x, y - 34, 90, 4);
            context.fillStyle = '#1e1c1a';
            context.fillRect(x + 6, y - 22, 5, 22);
            context.fillRect(x + 79, y - 22, 5, 22);
            break;
        }
        case 'train': {
            const body = context.createLinearGradient(0, y - 120, 0, y);
            body.addColorStop(0, '#34494a');
            body.addColorStop(1, '#1b2626');
            context.fillStyle = body;
            context.beginPath();
            if (context.roundRect) {
                context.roundRect(x, y - 116, 430, 104, [16, 16, 4, 4]);
            } else {
                context.rect(x, y - 116, 430, 104);
            }
            context.fill();
            context.fillStyle = 'rgba(150,180,190,0.22)';
            for (let window_x = x + 22; window_x < x + 400; window_x += 62) {
                context.fillRect(window_x, y - 96, 42, 34);
            }
            context.fillStyle = 'rgba(200,120,60,0.5)';
            context.fillRect(x, y - 54, 430, 4);
            context.fillStyle = '#0f1414';
            for (const wheel_x of [x + 50, x + 110, x + 320, x + 380]) {
                context.beginPath();
                context.arc(wheel_x, y - 7, 14, 0, Math.PI * 2);
                context.fill();
            }
            break;
        }
        case 'pillar': {
            const pillar = context.createLinearGradient(x, 0, x + (prop.w || 26), 0);
            pillar.addColorStop(0, 'rgba(255,255,255,0.06)');
            pillar.addColorStop(1, 'rgba(0,0,0,0.4)');
            context.fillStyle = palette.wall;
            context.fillRect(x, y, prop.w || 26, prop.h || 200);
            context.fillStyle = pillar;
            context.fillRect(x, y, prop.w || 26, prop.h || 200);
            break;
        }
        case 'bed': {
            context.fillStyle = '#8c969a';
            context.fillRect(x, y - 30, 110, 10);
            context.fillStyle = '#d8d6cf';
            context.beginPath();
            if (context.roundRect) {
                context.roundRect(x + 3, y - 40, 104, 12, 4);
            } else {
                context.rect(x + 3, y - 40, 104, 12);
            }
            context.fill();
            context.fillStyle = '#b9c7d6';
            context.fillRect(x + 40, y - 40, 66, 12);
            context.fillStyle = '#efece5';
            context.fillRect(x + 6, y - 46, 26, 8);
            context.fillStyle = '#4b5256';
            context.fillRect(x + 2, y - 20, 4, 20);
            context.fillRect(x + 104, y - 20, 4, 20);
            context.fillRect(x, y - 58, 4, 30);
            break;
        }
        case 'locker': {
            for (let index = 0; index < (prop.count || 4); index++) {
                context.fillStyle = index % 2 === 0 ? '#3d5a6b' : '#38525f';
                context.fillRect(x + index * 30, y - 120, 28, 120);
                context.fillStyle = 'rgba(0,0,0,0.3)';
                context.fillRect(x + index * 30 + 6, y - 104, 16, 2);
                context.fillRect(x + index * 30 + 6, y - 98, 16, 2);
                context.fillStyle = 'rgba(255,255,255,0.08)';
                context.fillRect(x + index * 30, y - 120, 2, 120);
            }
            break;
        }
        case 'chalkboard': {
            context.fillStyle = '#5b4632';
            context.fillRect(x - 6, y - 6, 232, 122);
            context.fillStyle = '#25362e';
            context.fillRect(x, y, 220, 110);
            context.fillStyle = 'rgba(235,232,222,0.85)';
            context.font = 'italic 14px "Newsreader", Georgia, serif';
            context.textBaseline = 'alphabetic';
            const lines = (prop.text || '').split('\n');
            lines.forEach(function draw_chalk_line(line, index) {
                context.fillText(line, x + 12, y + 24 + index * 19);
            });
            context.fillStyle = 'rgba(235,232,222,0.12)';
            context.fillRect(x + 4, y + 90, 80, 12);
            break;
        }
        case 'drawing': {
            context.save();
            context.translate(x + 20, y + 16);
            context.rotate(-0.05);
            context.fillStyle = '#ebe5d3';
            context.fillRect(-20, -16, 40, 32);
            context.strokeStyle = prop.color || '#c0582d';
            context.lineWidth = 1.5;
            context.beginPath();
            context.ellipse(-2, 2, 9, 5, 0, 0, Math.PI * 2);
            context.moveTo(13, -2);
            context.arc(9, -3, 4, 0, Math.PI * 2);
            context.moveTo(-8, 6);
            context.lineTo(-9, 12);
            context.moveTo(4, 6);
            context.lineTo(5, 12);
            context.stroke();
            context.fillStyle = '#e8c35a';
            context.beginPath();
            context.arc(13, -11, 3.5, 0, Math.PI * 2);
            context.fill();
            context.restore();
            break;
        }
        case 'cot': {
            context.fillStyle = '#4b5a4b';
            context.fillRect(x, y - 20, 90, 8);
            context.fillStyle = '#6b6a5c';
            context.fillRect(x + 4, y - 27, 82, 7);
            context.fillStyle = '#8f7d5e';
            context.fillRect(x + 50, y - 27, 36, 7);
            context.strokeStyle = '#2c3229';
            context.lineWidth = 2;
            context.beginPath();
            context.moveTo(x + 4, y);
            context.lineTo(x + 20, y - 12);
            context.moveTo(x + 86, y);
            context.lineTo(x + 70, y - 12);
            context.stroke();
            break;
        }
        case 'ribbon': {
            context.strokeStyle = '#59616a';
            context.lineWidth = 2;
            context.beginPath();
            context.moveTo(x, y);
            context.lineTo(x, y + 90);
            context.stroke();
            const flutter = Math.sin(time_seconds * 7 + x) * 6;
            context.fillStyle = prop.color || '#d4b13c';
            context.beginPath();
            context.moveTo(x, y + 6);
            context.quadraticCurveTo(x + 12, y + 8 + flutter, x + 24, y + 13 + flutter);
            context.lineTo(x + 21, y + 20 + flutter);
            context.quadraticCurveTo(x + 10, y + 15, x, y + 14);
            context.fill();
            break;
        }
        case 'tree': {
            context.fillStyle = '#2a2119';
            context.beginPath();
            context.moveTo(x - 7, y);
            context.quadraticCurveTo(x - 3, y - 70, x - 4, y - 130);
            context.lineTo(x + 4, y - 130);
            context.quadraticCurveTo(x + 3, y - 70, x + 7, y);
            context.fill();
            const leaf_color = prop.color || '#3b4d33';
            const sway = Math.sin(time_seconds * 0.9 + x) * 2;
            for (const [offset_x, offset_y, radius, shade] of [[0, -158, 46, 0], [-36, -130, 36, -0.1], [34, -134, 38, -0.1], [-14, -186, 30, 0.1], [18, -112, 30, -0.2]]) {
                context.fillStyle = shade > 0 ? lighten(leaf_color, 0.15) : shade < -0.15 ? darken(leaf_color, 0.25) : shade < 0 ? darken(leaf_color, 0.1) : leaf_color;
                context.beginPath();
                context.arc(x + offset_x + sway, y + offset_y, radius, 0, Math.PI * 2);
                context.fill();
            }
            break;
        }
        case 'house': {
            // The house in the photograph, years later: walls standing, roof gone, ivy everywhere.
            const wall = context.createLinearGradient(x, 0, x + 300, 0);
            wall.addColorStop(0, '#7f7464');
            wall.addColorStop(1, '#5d5446');
            context.fillStyle = wall;
            context.fillRect(x, y - 190, 300, 190);
            context.fillStyle = '#4e463d';
            context.beginPath();
            context.moveTo(x - 10, y - 190);
            context.lineTo(x + 60, y - 242);
            context.lineTo(x + 108, y - 206);
            context.lineTo(x + 140, y - 230);
            context.lineTo(x + 170, y - 190);
            context.fill();
            context.fillStyle = '#2f5a3a';  // The green door, faded.
            context.fillRect(x + 120, y - 120, 56, 120);
            context.fillStyle = 'rgba(0,0,0,0.25)';
            context.fillRect(x + 124, y - 116, 22, 50);
            context.fillRect(x + 150, y - 116, 22, 50);
            context.fillStyle = '#6b6152';
            context.fillRect(x + 110, y - 8, 76, 8);  // The step.
            context.fillStyle = 'rgba(255,224,170,0.35)';
            context.fillRect(x + 30, y - 150, 50, 50);
            context.fillRect(x + 220, y - 150, 50, 50);
            const random = make_seeded_random(9);
            for (let leaf = 0; leaf < 140; leaf++) {
                const leaf_x = x + random() * 300;
                const leaf_y = y - 190 + Math.pow(random(), 1.8) * 190;
                context.fillStyle = random() < 0.4 ? '#4c6e3f' : random() < 0.7 ? '#3d5c35' : '#5e8049';
                context.beginPath();
                context.ellipse(leaf_x, leaf_y, 3 + random() * 4, 2 + random() * 3, random() * 3, 0, Math.PI * 2);
                context.fill();
            }
            break;
        }
        case 'fire': {
            draw_fire(context, x, y, 1, time_seconds);
            break;
        }
        case 'rail': {
            context.strokeStyle = '#3d454d';
            context.lineWidth = 3;
            context.beginPath();
            context.moveTo(x, y - 28);
            context.lineTo(x + prop.w, y - 28);
            for (let post_x = x; post_x <= x + prop.w; post_x += 30) {
                context.moveTo(post_x, y);
                context.lineTo(post_x, y - 28);
            }
            context.stroke();
            break;
        }
        case 'flowers': {
            const random = make_seeded_random(x);
            for (let index = 0; index < 18; index++) {
                const flower_x = x + random() * (prop.w || 120);
                const stem = 10 + random() * 9;
                const sway = Math.sin(time_seconds * 2 + index) * 1.5;
                context.strokeStyle = '#4c6b3c';
                context.lineWidth = 1.2;
                context.beginPath();
                context.moveTo(flower_x, y);
                context.quadraticCurveTo(flower_x, y - stem * 0.6, flower_x + sway, y - stem);
                context.stroke();
                context.fillStyle = random() < 0.5 ? '#ecd47e' : '#f1e9d8';
                context.beginPath();
                context.arc(flower_x + sway, y - stem - 1, 2.4, 0, Math.PI * 2);
                context.fill();
            }
            break;
        }
        default:
            break;
    }
}

// Story text on signs can contain {dog}; this fills it in when the game's helper exists.
function personalize_if_available(text) {
    return typeof personalize === 'function' ? personalize(text) : text;
}

// Makes a "#rrggbb" colour lighter or darker by a fraction.
function lighten(hex_color, amount) {
    const value = parseInt(hex_color.slice(1), 16);
    const channel = function mix(part) { return Math.round(part + (255 - part) * amount); };
    return 'rgb(' + channel((value >> 16) & 255) + ',' + channel((value >> 8) & 255) + ',' + channel(value & 255) + ')';
}
function darken(hex_color, amount) {
    const value = parseInt(hex_color.slice(1), 16);
    const channel = function mix(part) { return Math.round(part * (1 - amount)); };
    return 'rgb(' + channel((value >> 16) & 255) + ',' + channel((value >> 8) & 255) + ',' + channel(value & 255) + ')';
}

// A living flame: layered tongues that flicker, with a bright core.
function draw_fire(context, x, y, size, time_seconds) {
    for (const [offset, height, color] of [[-7, 30, 'rgba(230,90,40,0.85)'], [6, 26, 'rgba(240,120,50,0.85)'], [0, 38, 'rgba(255,150,60,0.9)'], [0, 22, 'rgba(255,215,130,0.95)']]) {
        const flicker = Math.sin(time_seconds * 11 + offset) * 3 + Math.sin(time_seconds * 17.3 + offset * 2) * 2;
        const tongue = (height + flicker) * size;
        context.fillStyle = color;
        context.beginPath();
        context.moveTo(x + (offset - 10) * size, y);
        context.quadraticCurveTo(x + (offset - 6) * size, y - tongue * 0.6, x + (offset + Math.sin(time_seconds * 5 + offset) * 2) * size, y - tongue);
        context.quadraticCurveTo(x + (offset + 6) * size, y - tongue * 0.55, x + (offset + 10) * size, y);
        context.closePath();
        context.fill();
    }
}

// Draws something the player can use (a lever, a glove, a window...), with a soft glint
// when it's available, brighter when in reach, so players find things without arrows everywhere.
function draw_interactable(context, item, time_seconds, in_reach, available) {
    const { x, y } = item;
    context.save();
    switch (item.sprite) {
        case 'lever':
        case 'wheel':
        case 'crank': {
            context.fillStyle = '#353c43';
            context.fillRect(x - 13, y - 52, 26, 52);
            context.fillStyle = 'rgba(255,255,255,0.08)';
            context.fillRect(x - 13, y - 52, 26, 2);
            context.strokeStyle = '#c96b3c';
            context.lineWidth = 4;
            context.lineCap = 'round';
            context.beginPath();
            const since_used = item.used_at !== undefined ? time_seconds - item.used_at : 0;
            if (item.sprite === 'wheel' || item.sprite === 'crank') {
                const turn = item.done ? Math.PI * 3 * ease_in_out(since_used / 1.4) : 0;
                context.arc(x, y - 34, 13, 0, Math.PI * 2);
                for (const spoke of [0, Math.PI / 2]) {
                    context.moveTo(x + Math.cos(spoke + turn) * 13, y - 34 + Math.sin(spoke + turn) * 13);
                    context.lineTo(x - Math.cos(spoke + turn) * 13, y - 34 - Math.sin(spoke + turn) * 13);
                }
            } else {
                const pull = item.done ? ease_out_bounce(since_used / 0.4) : 0;
                const lever_angle = -1.1 + pull * 2.1;
                context.moveTo(x, y - 30);
                context.lineTo(x + Math.sin(lever_angle) * 29, y - 30 - Math.cos(lever_angle) * 29);
            }
            context.stroke();
            break;
        }
        case 'breaker':
        case 'button':
        case 'latch': {
            context.fillStyle = '#2b3239';
            context.fillRect(x - 16, y - 72, 32, 42);
            context.fillStyle = 'rgba(255,255,255,0.08)';
            context.fillRect(x - 16, y - 72, 32, 2);
            context.fillStyle = item.done ? '#86c77f' : '#d0503d';
            context.beginPath();
            context.arc(x, y - 51, 5, 0, Math.PI * 2);
            context.fill();
            break;
        }
        case 'glove': {
            if (!item.done) {
                context.save();
                context.translate(x, y - 40);
                context.rotate(0.3);
                draw_carried_item(context, 'glove', 0, 0);
                context.restore();
                context.strokeStyle = '#59616a';
                context.lineWidth = 2;
                context.beginPath();
                context.moveTo(x - 40, y - 46);
                context.lineTo(x + 40, y - 46);
                context.stroke();
            }
            break;
        }
        case 'photo':
        case 'note': {
            if (!item.done || item.sprite === 'note') {
                context.save();
                context.translate(x, y - 36);
                context.rotate(-0.12);
                context.fillStyle = '#ebe3d0';
                context.fillRect(-10, -7, 20, 14);
                if (item.sprite === 'photo') {
                    context.fillStyle = '#7f8a6a';
                    context.fillRect(-8, -5, 16, 10);
                    context.fillStyle = '#2f5a3a';
                    context.fillRect(-2, -1, 3, 6);
                } else {
                    context.fillStyle = 'rgba(60,60,70,0.5)';
                    for (let line = 0; line < 3; line++) {
                        context.fillRect(-7, -4 + line * 4, 14, 1);
                    }
                }
                context.restore();
            }
            break;
        }
        case 'debris': {
            if (!item.done) {
                context.fillStyle = '#4e3f30';
                for (const [offset_x, offset_y, width, height, angle] of [[-30, -16, 60, 8, 0.3], [-20, -28, 50, 7, -0.5], [-10, -10, 40, 10, 0.1], [-26, -38, 44, 6, 0.9]]) {
                    context.save();
                    context.translate(x + offset_x + width / 2, y + offset_y);
                    context.rotate(angle);
                    context.fillRect(-width / 2, -height / 2, width, height);
                    context.fillStyle = 'rgba(255,255,255,0.08)';
                    context.fillRect(-width / 2, -height / 2, width, 1.5);
                    context.fillStyle = '#4e3f30';
                    context.restore();
                }
            }
            break;
        }
        case 'cord':
        case 'rope': {
            context.strokeStyle = '#b89b6a';
            context.lineWidth = 2.5;
            context.beginPath();
            context.moveTo(x, y - 150);
            context.quadraticCurveTo(x + Math.sin(time_seconds * 1.5) * 4, y - 90, x, y - 36);
            context.stroke();
            context.fillStyle = '#c84b3a';
            context.beginPath();
            context.arc(x, y - 33, 4.5, 0, Math.PI * 2);
            context.fill();
            break;
        }
        case 'desk': {
            if (!item.done) {
                context.fillStyle = '#6b5136';
                context.fillRect(x - 30, y - 40, 60, 8);
                context.fillStyle = '#4e3a26';
                context.fillRect(x - 26, y - 32, 6, 32);
                context.fillRect(x + 20, y - 32, 6, 32);
            }
            break;
        }
        case 'pin':
        case 'post': {
            context.fillStyle = '#59616a';
            context.fillRect(x - 4, y - 42, 8, 42);
            context.fillStyle = item.done ? '#59616a' : '#c96b3c';
            context.fillRect(x - 9, y - 30, 18, 5);
            break;
        }
        case 'doorframe': {
            context.strokeStyle = 'rgba(40,35,30,0.9)';
            context.lineWidth = 1.5;
            for (const mark_y of [y - 30, y - 48]) {
                context.beginPath();
                context.moveTo(x - 6, mark_y);
                context.lineTo(x + 6, mark_y);
                context.stroke();
            }
            break;
        }
        case 'keepsake': {
            if (!item.done) {
                const bob = Math.sin(time_seconds * 2 + x) * 1.5;
                context.save();
                context.translate(x, y - 8 + bob);
                context.rotate(0.2);
                context.fillStyle = '#ece0c4';
                context.fillRect(-6, -4, 12, 8);
                context.fillStyle = '#c9a25c';
                context.fillRect(-6, -4, 12, 2);
                context.restore();
            }
            break;
        }
        case 'crate_top': {
            if (!item.done) {
                draw_solid(context, { kind: 'crate', x: x - 20, y: y - 52, w: 52, h: 52, fade: 1 }, {}, time_seconds);
            }
            break;
        }
        default:
            break;
    }
    context.restore();
    if (available) {
        // The glint: a small pulsing point of light, brighter when the character can reach it.
        const pulse = 0.5 + 0.5 * Math.sin(time_seconds * 3 + x);
        const radius = in_reach ? 9 + pulse * 4 : 4 + pulse * 2;
        const glint_y = item.sprite === 'keepsake' ? y - 22 : y - 62;
        const glow = context.createRadialGradient(x, glint_y, 0, x, glint_y, radius * 2.5);
        glow.addColorStop(0, 'rgba(255,236,190,' + (in_reach ? 0.95 : 0.55) + ')');
        glow.addColorStop(0.3, 'rgba(255,214,150,' + (in_reach ? 0.35 : 0.18) + ')');
        glow.addColorStop(1, 'rgba(255,214,150,0)');
        context.fillStyle = glow;
        context.beginPath();
        context.arc(x, glint_y, radius * 2.5, 0, Math.PI * 2);
        context.fill();
    }
}

// Draws a body of water: a see-through body, a moving bright surface line, and rushing streaks.
function draw_water(context, water, time_seconds) {
    const top = water.current_top;
    const bottom = water.y + water.h;
    if (bottom <= top) {
        return;
    }
    const speed = water.flow ? 5 : 1.6;
    const body = context.createLinearGradient(0, top, 0, bottom);
    body.addColorStop(0, 'rgba(110,140,160,0.5)');
    body.addColorStop(0.3, 'rgba(50,72,92,0.7)');
    body.addColorStop(1, 'rgba(18,28,40,0.9)');
    context.fillStyle = body;
    context.beginPath();
    context.moveTo(water.x, bottom);
    for (let x = water.x; x <= water.x + water.w; x += 10) {
        context.lineTo(x, top + Math.sin(x * 0.045 + time_seconds * speed) * 2 + Math.sin(x * 0.11 - time_seconds * speed * 0.7) * 1);
    }
    context.lineTo(water.x + water.w, bottom);
    context.closePath();
    context.fill();
    context.strokeStyle = 'rgba(215,235,245,0.5)';
    context.lineWidth = 1.4;
    context.beginPath();
    for (let x = water.x; x <= water.x + water.w; x += 10) {
        const surface_y = top + Math.sin(x * 0.045 + time_seconds * speed) * 2 + Math.sin(x * 0.11 - time_seconds * speed * 0.7) * 1;
        if (x === water.x) {
            context.moveTo(x, surface_y);
        } else {
            context.lineTo(x, surface_y);
        }
    }
    context.stroke();
    if (water.flow) {
        context.strokeStyle = 'rgba(225,240,245,0.28)';
        context.lineWidth = 1;
        context.beginPath();
        for (let streak = 0; streak < water.w / 26; streak++) {
            const streak_x = water.x + ((streak * 53 + time_seconds * 200 * water.flow) % water.w + water.w) % water.w;
            const streak_y = top + 5 + ((streak * 17) % Math.max(4, bottom - top - 8));
            context.moveTo(streak_x, streak_y);
            context.lineTo(Math.min(water.x + water.w, streak_x + 16 + (streak % 3) * 6), streak_y);
        }
        context.stroke();
    }
}

// Draws the tennis ball.
function draw_ball(context, ball) {
    draw_carried_item(context, 'ball', ball.x, ball.y);
}

// Draws short-lived effects: splashes, debris, scent motes, dust, fireflies, landing puffs.
function draw_particles(context, particles) {
    for (const particle of particles) {
        const life_left = Math.max(0, particle.life / particle.max_life);
        if (particle.kind === 'scent') {
            // A fine golden mote, fading in and out. Many of them together read as a trail.
            const alpha = 0.75 * Math.sin(life_left * Math.PI);
            context.fillStyle = 'rgba(255,226,160,' + alpha.toFixed(3) + ')';
            context.beginPath();
            context.arc(particle.x, particle.y, particle.size * 0.55, 0, Math.PI * 2);
            context.fill();
            context.fillStyle = 'rgba(255,214,140,' + (alpha * 0.18).toFixed(3) + ')';
            context.beginPath();
            context.arc(particle.x, particle.y, particle.size * 2, 0, Math.PI * 2);
            context.fill();
        } else if (particle.kind === 'firefly') {
            const alpha = 0.85 * Math.sin(life_left * Math.PI);
            context.fillStyle = 'rgba(255,236,160,' + alpha.toFixed(3) + ')';
            context.beginPath();
            context.arc(particle.x, particle.y, 1.8, 0, Math.PI * 2);
            context.fill();
            context.fillStyle = 'rgba(255,236,160,' + (alpha * 0.2).toFixed(3) + ')';
            context.beginPath();
            context.arc(particle.x, particle.y, 6, 0, Math.PI * 2);
            context.fill();
        } else if (particle.kind === 'photo') {
            context.save();
            context.translate(particle.x, particle.y);
            context.rotate(particle.spin * particle.life * 2);
            context.globalAlpha = life_left;
            draw_carried_item(context, 'photo', 0, 0);
            context.restore();
        } else if (particle.kind === 'board') {
            context.save();
            context.translate(particle.x, particle.y);
            context.rotate(particle.angle || 0);
            context.globalAlpha = Math.min(1, life_left * 3);
            context.fillStyle = '#4e3c2c';
            context.fillRect(-particle.width / 2, -particle.height / 2, particle.width, particle.height);
            context.fillStyle = '#6a523c';
            context.fillRect(-particle.width / 2, -particle.height / 2, particle.width, 3);
            context.restore();
        } else if (particle.kind === 'puff') {
            context.fillStyle = 'rgba(190,190,185,' + (0.35 * life_left).toFixed(3) + ')';
            context.beginPath();
            context.arc(particle.x, particle.y, particle.size * (1.6 - life_left), 0, Math.PI * 2);
            context.fill();
        } else if (particle.kind === 'ripple') {
            context.strokeStyle = 'rgba(210,230,240,' + (0.5 * life_left).toFixed(3) + ')';
            context.lineWidth = 1;
            context.beginPath();
            context.ellipse(particle.x, particle.y, (1 - life_left) * 9 + 1, ((1 - life_left) * 9 + 1) * 0.3, 0, 0, Math.PI * 2);
            context.stroke();
        } else {
            context.globalAlpha = particle.color ? life_left : 1;
            context.fillStyle = particle.color || 'rgba(200,220,235,' + (0.7 * life_left).toFixed(2) + ')';
            if (particle.angle !== undefined) {
                context.save();
                context.translate(particle.x, particle.y);
                context.rotate(particle.angle);
                context.fillRect(-particle.size, -particle.size / 3, particle.size * 2, particle.size * 0.7);
                context.restore();
            } else {
                context.fillRect(particle.x, particle.y, particle.size, particle.size);
            }
            context.globalAlpha = 1;
        }
    }
}

// ---------- Light ----------

// Draws the light that hangs in the air: cones under hanging lamps, halos around bulbs, and
// lamplight reflected in wet floors. Drawn in world space, added on top ("lighter" blending).
function draw_light_effects(context, props, time_seconds, wet, left_edge, right_edge) {
    context.save();
    context.globalCompositeOperation = 'lighter';
    for (const prop of props) {
        if (prop.kind !== 'lamp' || prop.off || prop.x < left_edge || prop.x > right_edge) {
            continue;
        }
        let strength = 1;
        if (prop.flicker) {
            const flicker = Math.sin(time_seconds * 23 + prop.x) * Math.sin(time_seconds * 3.1 + prop.x);
            strength = flicker > 0.85 ? 0.12 : 0.85 + flicker * 0.15;
        }
        const bulb_y = prop.hanging ? prop.y + 42 : prop.y + 2;
        const floor_y = prop.floor_y || bulb_y + 200;
        const tint = prop.light_tint || '255,206,140';
        const spread = (floor_y - bulb_y) * 0.42;
        // A swinging lamp swings its light: rotate the cone and halo around the hook,
        // and slide the pool of light along the floor.
        const sway = prop.sway ? prop.sway.value : 0;
        const pool_x = prop.x + Math.sin(sway) * (floor_y - prop.y);
        context.save();
        context.translate(prop.x, prop.y);
        context.rotate(sway);
        context.translate(-prop.x, -prop.y);
        context.globalAlpha = strength;
        context.fillStyle = cached_gradient(context, prop, 'cone', function make_cone() {
            const cone = context.createLinearGradient(0, bulb_y, 0, floor_y);
            cone.addColorStop(0, 'rgba(' + tint + ',0.22)');
            cone.addColorStop(1, 'rgba(' + tint + ',0)');
            return cone;
        });
        context.beginPath();
        context.moveTo(prop.x - 9, bulb_y);
        context.lineTo(prop.x + 9, bulb_y);
        context.lineTo(prop.x + spread, floor_y);
        context.lineTo(prop.x - spread, floor_y);
        context.closePath();
        context.fill();
        stamp_glow(context, tint, prop.x, bulb_y, 46, 0.55 * strength);
        context.restore();
        // The pool of light on the floor (a flattened glow) and its streaky reflection when wet.
        context.save();
        context.translate(pool_x, floor_y);
        context.scale(1, 0.25);
        stamp_glow(context, tint, 0, 0, spread, 0.22 * strength);
        context.restore();
        if (wet) {
            context.globalAlpha = strength;
            context.fillStyle = cached_gradient(context, prop, 'reflection', function make_reflection() {
                const reflection = context.createLinearGradient(0, floor_y, 0, floor_y + 34);
                reflection.addColorStop(0, 'rgba(' + tint + ',0.3)');
                reflection.addColorStop(1, 'rgba(' + tint + ',0)');
                return reflection;
            });
            const shimmer = Math.sin(time_seconds * 3 + prop.x) * 1.5;
            context.fillRect(pool_x - 4 + shimmer, floor_y, 8, 34);
            context.fillRect(pool_x - 12 - shimmer, floor_y + 2, 3, 20);
            context.fillRect(pool_x + 9 + shimmer, floor_y + 1, 3, 24);
            context.globalAlpha = 1;
        }
    }
    context.restore();
}

// Shafts of light falling through windows: pale, slanted, slowly breathing. Cheap depth.
function draw_light_shafts(context, props, time_seconds, left_edge, right_edge) {
    context.save();
    context.globalCompositeOperation = 'lighter';
    for (const prop of props) {
        if (prop.kind !== 'window' || !(prop.glow || prop.lit) || prop.x < left_edge || prop.x > right_edge) {
            continue;
        }
        const w = prop.w || 60;
        const h = prop.h || 80;
        const reach = prop.shaft_reach || 160;
        if (prop.skylight) {
            context.globalAlpha = 0.7 + 0.3 * Math.sin(time_seconds * 0.4 + prop.x);
            context.fillStyle = cached_gradient(context, prop, 'skylight', function make_skylight() {
                const shaft = context.createLinearGradient(0, prop.y, 0, prop.y + reach);
                shaft.addColorStop(0, 'rgba(170,195,230,0.16)');
                shaft.addColorStop(1, 'rgba(170,195,230,0)');
                return shaft;
            });
            context.beginPath();
            context.moveTo(prop.x, prop.y);
            context.lineTo(prop.x + w, prop.y);
            context.lineTo(prop.x + w + reach * 0.22, prop.y + reach);
            context.lineTo(prop.x + reach * 0.1, prop.y + reach);
            context.closePath();
            context.fill();
            continue;
        }
        const breathe = 0.75 + 0.25 * Math.sin(time_seconds * 0.6 + prop.x);
        context.globalAlpha = breathe;
        context.fillStyle = cached_gradient(context, prop, 'shaft', function make_shaft() {
            const shaft = context.createLinearGradient(prop.x, prop.y, prop.x + reach * 0.5, prop.y + h + reach);
            shaft.addColorStop(0, prop.lit ? 'rgba(255,200,130,0.16)' : 'rgba(190,210,235,0.10)');
            shaft.addColorStop(1, 'rgba(0,0,0,0)');
            return shaft;
        });
        context.beginPath();
        context.moveTo(prop.x, prop.y);
        context.lineTo(prop.x + w, prop.y);
        context.lineTo(prop.x + w + reach * 0.55, prop.y + h + reach);
        context.lineTo(prop.x + reach * 0.35, prop.y + h + reach);
        context.closePath();
        context.fill();
    }
    context.restore();
}

// Covers the screen in darkness, then cuts soft holes where there's light.
// Drawn on its own small canvas (half resolution, cheap on phones) so the holes don't
// punch through the world itself, then stretched over the screen.
function draw_lighting(context, light_canvas, lights, darkness, darkness_color, view_width, vignette_strength) {
    const light_context = light_canvas.getContext('2d');
    const scale = light_canvas.width / view_width;
    light_context.setTransform(1, 0, 0, 1, 0, 0);
    light_context.globalCompositeOperation = 'source-over';
    light_context.clearRect(0, 0, light_canvas.width, light_canvas.height);
    if (darkness > 0.01) {
        light_context.globalAlpha = darkness;
        light_context.fillStyle = darkness_color;
        light_context.fillRect(0, 0, light_canvas.width, light_canvas.height);
        light_context.globalAlpha = 1;
        // Cut a soft hole of light for each lamp, window and character, by stamping a soft dot.
        light_context.globalCompositeOperation = 'destination-out';
        const hole = hole_sprite();
        for (const light of lights) {
            const radius = light.radius * scale;
            light_context.globalAlpha = Math.min(1, light.strength);
            light_context.drawImage(hole, light.x * scale - radius, light.y * scale - radius, radius * 2, radius * 2);
        }
        light_context.globalAlpha = 1;
        light_context.globalCompositeOperation = 'source-over';
    }
    // The edge darkening is painted into the same small layer, so it costs no extra full-screen pass.
    light_context.globalAlpha = vignette_strength;
    light_context.drawImage(vignette_sprite(), 0, 0, light_canvas.width, light_canvas.height);
    light_context.globalAlpha = 1;
    context.drawImage(light_canvas, 0, 0, view_width, view_height);
    // Warm colour over the lights, so lamplight looks warm and not just bright.
    context.save();
    context.globalCompositeOperation = 'lighter';
    for (const light of lights) {
        if (light.glow_color) {
            stamp_glow(context, light.glow_color, light.x, light.y, light.radius * 0.75, light.glow_strength);
        }
    }
    context.restore();
}

// A soft darkening toward the screen's edges, painted once as a small picture.
function vignette_sprite() {
    if (!render_cache.vignette) {
        const canvas = make_sprite_canvas(160, 90);
        const vignette_context = canvas.getContext('2d');
        const gradient = vignette_context.createRadialGradient(80, 50, 26, 80, 50, 96);
        gradient.addColorStop(0, 'rgba(0,0,0,0)');
        gradient.addColorStop(1, 'rgba(0,0,0,1)');
        vignette_context.fillStyle = gradient;
        vignette_context.fillRect(0, 0, 160, 90);
        render_cache.vignette = canvas;
    }
    return render_cache.vignette;
}

// Rain in two depths: faint far streaks and brighter near ones, slanted by the wind.
// Drops are skipped where they'd fall indoors (`is_sheltered` says where roofs are).
function update_and_draw_rain(context, rain_drops, view_width, intensity, wind, delta_seconds, is_sheltered, near) {
    // Far rain: many thin, slow, faint streaks drawn behind the characters.
    // Near rain: fewer long, fast, brighter streaks drawn in front of them.
    const wanted = Math.floor(view_width * (near ? 0.07 : 0.14) * intensity);
    while (rain_drops.length < wanted) {
        rain_drops.push({
            x: Math.random() * (view_width + 300) - 150, y: Math.random() * view_height,
            speed: near ? 1150 + Math.random() * 350 : 620 + Math.random() * 220, length: near ? 20 + Math.random() * 12 : 7 + Math.random() * 6,
        });
    }
    rain_drops.length = Math.min(rain_drops.length, wanted);
    context.strokeStyle = near ? 'rgba(200,214,230,0.42)' : 'rgba(150,170,195,0.2)';
    context.lineWidth = near ? 1.4 : 0.8;
    context.beginPath();
    for (const drop of rain_drops) {
        drop.y += drop.speed * delta_seconds;
        drop.x += wind * drop.speed * 0.25 * delta_seconds;
        if (drop.y > view_height) {
            drop.y = -30;
            drop.x = Math.random() * (view_width + 300) - 150;
        }
        if (is_sheltered && is_sheltered(drop.x, drop.y)) {
            continue;
        }
        context.moveTo(drop.x, drop.y);
        context.lineTo(drop.x + wind * drop.length * 0.3, drop.y + drop.length);
    }
    context.stroke();
}

// Darkens the edges of the screen, which draws the eye to the middle.
function draw_vignette(context, view_width, strength) {
    context.globalAlpha = strength;
    context.drawImage(vignette_sprite(), 0, 0, view_width, view_height);
    context.globalAlpha = 1;
}

// Slow banks of mist drifting just above the floors: depth and weather in one cheap layer.
function draw_mist(context, view_width, camera_left, time_seconds, color) {
    const sprite = mist_sprite(color);
    const cloud_width = view_width * 0.9;
    const cloud_height = cloud_width * 0.25;
    for (let band = 0; band < 3; band++) {
        const band_y = view_height * (0.55 + band * 0.14);
        const loop = view_width * 1.5;
        const drift = ((time_seconds * (8 + band * 5) - camera_left * (0.6 + band * 0.2)) % loop + loop) % loop;
        for (const offset of [-loop, 0]) {
            const center_x = drift + offset + view_width * 0.4;
            context.drawImage(sprite, center_x - cloud_width / 2, band_y - cloud_height / 2, cloud_width, cloud_height);
        }
    }
}

// Dark shapes very close to the camera, sliding past faster than the world: a cheap,
// strong sense of depth. Placed at the screen's edges so they never hide the characters.
function draw_foreground(context, kind, view_width, camera_left, time_seconds, color, player_screen_x) {
    if (!kind) {
        return;
    }
    const sprite = blur_sprite(kind, color || 'rgba(4,6,10,0.92)');
    const factor = 1.45;
    const spacing = 1000;
    const shift = camera_left * factor;
    const tall = kind === 'pillars' || kind === 'beams';
    const draw_height = tall ? view_height * 1.15 : view_height * 0.5;
    const draw_width = sprite.width * (draw_height / sprite.height) * (tall ? 1.6 : 1.4);
    for (let index = Math.floor(shift / spacing) - 1; index <= Math.floor((shift + view_width) / spacing) + 1; index++) {
        const x = index * spacing - shift + ((index * 397) % 300);
        // Fade any shape that passes in front of the player, so it never hides the action.
        const distance = Math.abs(x + draw_width / 2 - player_screen_x);
        context.globalAlpha = Math.max(0.15, Math.min(1, (distance - 50) / 220));
        const sway = tall ? 0 : Math.sin(time_seconds * 1.2 + index) * 4;
        context.drawImage(sprite, x + sway, view_height - draw_height + (tall ? -view_height * 0.08 : 6), draw_width, draw_height);
    }
    context.globalAlpha = 1;
}

// A plain wooden chair, for the quiet scenes.
function draw_chair(context, x, floor_y, scale) {
    context.save();
    context.translate(x, floor_y);
    context.scale(scale, scale);
    context.fillStyle = '#4a3626';
    context.fillRect(-14, -24, 30, 4);
    context.fillRect(-14, -24, 3, 24);
    context.fillRect(13, -24, 3, 24);
    context.fillRect(-16, -52, 4, 52);
    context.restore();
}

// ---------- The quiet scenes between chapters ----------

// Draws one of the still, slowly moving scenes shown after a chapter ends. The characters
// are drawn large here: this is where the player gets to look at them properly.
// `dog_look` is Bodhi's growth stage in that chapter ({ size, drawing }), so she is the size
// she was in the chapter just played.
function draw_vignette_scene(context, scene_name, time_seconds, width, height, dog_look) {
    const dog_size = dog_look ? dog_look.size : 1;
    const dog_drawing = dog_look ? dog_look.drawing : 'bodhi';
    const scale = height / 540;
    context.save();
    context.setTransform(1, 0, 0, 1, 0, 0);
    context.clearRect(0, 0, width, height);
    context.scale(scale, scale);
    const scene_width = width / scale;
    const center_x = scene_width / 2;
    const floor_y = 400;
    const figure_scale = 2.3;

    // Places a character drawing at (x, feet_y), scaled up.
    const draw_figure = function draw_figure_at(kind, pose, x, feet_y, facing, extra) {
        context.save();
        context.translate(x, feet_y);
        context.scale(figure_scale, figure_scale);
        const spec = kind === 'dog' ? { width: 46 * dog_size, height: 38 * dog_size } : { width: 26, height: 86 };
        const growth = kind === 'dog' ? { size_scale: dog_size, drawing: dog_drawing } : {};
        const figure = Object.assign({ x: -spec.width / 2, y: -spec.height, width: spec.width, height: spec.height, facing: facing, pose: pose, walk_phase: time_seconds * 5, wag: 0.25, fear: 0, on_ground: true, kind: kind, velocity_x: pose === 'walk' ? 140 : 0 }, growth, extra || {});
        draw_contact_shadow(context, figure);
        if (kind === 'dog') {
            draw_dog(context, figure, time_seconds);
        } else {
            draw_human(context, figure, time_seconds);
        }
        context.restore();
    };

    const background = context.createLinearGradient(0, 0, 0, 540);
    const palettes = {
        road_dawn: ['#3d3c55', '#e0a675'], hill_sunrise: ['#4b5a86', '#f2bf86'], river_dawn: ['#2f3d4f', '#b8b0a6'],
        ward_stars: ['#121a2c', '#0a0d16'], gym_night: ['#1d1a16', '#0c0b09'], cabin_fire: ['#1b1411', '#0c0807'],
    };
    const colors = palettes[scene_name] || palettes.cabin_fire;
    background.addColorStop(0, colors[0]);
    background.addColorStop(1, colors[1]);
    context.fillStyle = background;
    context.fillRect(0, 0, scene_width, 540);

    if (scene_name === 'cabin_fire') {
        // Wooden walls, rain on a blue-black window, a stove glowing, the glove drying, both asleep.
        for (let plank_x = 0; plank_x < scene_width; plank_x += 38) {
            context.fillStyle = plank_x % 76 === 0 ? '#2b1f18' : '#261b15';
            context.fillRect(plank_x, 0, 37, floor_y);
        }
        draw_prop(context, { kind: 'window', x: center_x - 330, y: 120, w: 120, h: 140 }, {}, time_seconds);
        context.fillStyle = '#3a2a1f';
        context.fillRect(0, floor_y, scene_width, 140);
        context.fillStyle = '#5a3328';
        context.beginPath();
        context.ellipse(center_x - 40, floor_y + 40, 220, 26, 0, 0, Math.PI * 2);
        context.fill();
        context.fillStyle = '#1e1a18';
        context.fillRect(center_x + 160, floor_y - 120, 100, 120);
        context.fillRect(center_x + 196, 0, 26, floor_y - 120);
        const grate_glow = 0.7 + 0.3 * Math.sin(time_seconds * 7) * Math.sin(time_seconds * 3.3);
        context.fillStyle = 'rgba(255,140,60,' + grate_glow.toFixed(2) + ')';
        context.fillRect(center_x + 178, floor_y - 70, 64, 34);
        draw_fire(context, center_x + 210, floor_y - 38, 0.9, time_seconds);
        context.save();
        context.translate(center_x + 290, floor_y - 6);
        context.scale(2, 2);
        draw_carried_item(context, 'glove', 0, 0);
        context.restore();
        for (let wisp = 0; wisp < 3; wisp++) {
            const rise = (time_seconds * 12 + wisp * 14) % 40;
            context.fillStyle = 'rgba(230,220,210,' + (0.18 * (1 - rise / 40)).toFixed(3) + ')';
            context.beginPath();
            context.arc(center_x + 292 + Math.sin(time_seconds + wisp) * 4, floor_y - 18 - rise, 4 + rise * 0.2, 0, Math.PI * 2);
            context.fill();
        }
        draw_chair(context, center_x - 120, floor_y, 2.3);
        draw_figure('human', 'sit', center_x - 120, floor_y, 1, { eyes_closed: true });
        draw_figure('dog', 'sleep', center_x - 10, floor_y + 4, -1, {});
        add_warm_glow(context, center_x + 210, floor_y - 50, 520, 0.42 + Math.sin(time_seconds * 9) * 0.03);
    } else if (scene_name === 'ward_stars') {
        const random = make_seeded_random(3);
        for (let index = 0; index < 46; index++) {
            const twinkle = 0.5 + 0.5 * Math.sin(time_seconds * 0.8 + index);
            context.fillStyle = 'rgba(244,218,140,' + (0.35 + 0.4 * twinkle).toFixed(2) + ')';
            draw_star(context, random() * scene_width, 20 + random() * 170, 4 + random() * 5);
        }
        draw_prop(context, { kind: 'window', x: center_x + 200, y: 150, w: 110, h: 150 }, {}, time_seconds);
        context.save();
        context.globalCompositeOperation = 'lighter';
        const beam = context.createLinearGradient(center_x + 250, 160, center_x - 100, floor_y);
        beam.addColorStop(0, 'rgba(150,180,230,0.22)');
        beam.addColorStop(1, 'rgba(150,180,230,0)');
        context.fillStyle = beam;
        context.beginPath();
        context.moveTo(center_x + 200, 150);
        context.lineTo(center_x + 310, 300);
        context.lineTo(center_x, floor_y + 60);
        context.lineTo(center_x - 200, floor_y + 60);
        context.closePath();
        context.fill();
        context.restore();
        context.fillStyle = '#161c2c';
        context.fillRect(0, floor_y, scene_width, 140);
        context.save();
        context.translate(center_x - 170, floor_y);
        context.scale(2.3, 2.3);
        draw_prop(context, { kind: 'bed', x: 0, y: 0 }, {}, time_seconds);
        context.restore();
        draw_figure('dog', 'sleep', center_x - 10, floor_y - 86, 1, { carrying: null });
        context.save();
        context.translate(center_x + 52, floor_y - 104);
        context.scale(2.3, 2.3);
        draw_carried_item(context, 'rabbit', 0, 0);
        context.restore();
        draw_chair(context, center_x - 270, floor_y, 2.3);
        draw_figure('human', 'sit', center_x - 270, floor_y, 1, { eyes_closed: true });
    } else if (scene_name === 'gym_night') {
        context.fillStyle = '#3d2c1c';
        context.fillRect(0, floor_y, scene_width, 140);
        context.strokeStyle = 'rgba(230,200,120,0.25)';
        context.lineWidth = 3;
        context.beginPath();
        context.arc(center_x, floor_y + 70, 160, Math.PI * 1.05, Math.PI * 1.95);
        context.stroke();
        draw_prop(context, { kind: 'chalkboard', x: center_x - 360, y: 90, text: 'NORTHGATE SHELTER\nwe are alive\nfollow the river north' }, {}, time_seconds);
        context.fillStyle = '#2a2a2a';
        context.fillRect(center_x + 200, floor_y - 60, 70, 60);
        draw_fire(context, center_x + 235, floor_y - 58, 1.2, time_seconds);
        draw_figure('human', 'sit', center_x - 90, floor_y, 1, { holding: 'photo' });
        draw_figure('dog', 'sleep', center_x + 30, floor_y + 4, -1, {});
        context.save();
        context.translate(center_x + 120, floor_y - 10);
        context.scale(2.3, 2.3);
        draw_carried_item(context, 'ball', 0, 0);
        context.restore();
        add_warm_glow(context, center_x + 235, floor_y - 80, 480, 0.4 + Math.sin(time_seconds * 8) * 0.03);
    } else if (scene_name === 'river_dawn') {
        const glow = context.createRadialGradient(center_x + 260, 300, 10, center_x + 260, 300, 500);
        glow.addColorStop(0, 'rgba(255,220,170,0.75)');
        glow.addColorStop(1, 'rgba(255,200,150,0)');
        context.fillStyle = glow;
        context.fillRect(0, 0, scene_width, 540);
        context.fillStyle = '#4b5649';
        context.beginPath();
        context.moveTo(0, floor_y - 40);
        for (let x = 0; x <= scene_width; x += 24) {
            context.lineTo(x, floor_y - 110 - Math.sin(x * 0.006) * 50);
        }
        context.lineTo(scene_width, floor_y - 40);
        context.fill();
        draw_smoke_column(context, center_x + 250, floor_y - 150, time_seconds);
        const river = context.createLinearGradient(0, floor_y - 40, 0, floor_y + 20);
        river.addColorStop(0, '#a6aba6');
        river.addColorStop(1, '#5b6a70');
        context.fillStyle = river;
        context.fillRect(0, floor_y - 40, scene_width, 60);
        context.fillStyle = 'rgba(255,230,190,0.4)';
        for (let streak = 0; streak < 9; streak++) {
            context.fillRect(center_x + 160 + Math.sin(time_seconds + streak) * 10, floor_y - 34 + streak * 6, 160 - streak * 14, 1.5);
        }
        context.fillStyle = '#2d3427';
        context.fillRect(0, floor_y + 20, scene_width, 140);
        draw_figure('human', 'sit', center_x - 120, floor_y + 22, 1, {});
        draw_figure('dog', 'sit', center_x - 20, floor_y + 24, 1, { wag: 0.4 });
        draw_mist(context, scene_width, 0, time_seconds, 'rgba(230,230,225,0.10)');
    } else {
        // road_dawn and hill_sunrise: the two of them walking into the light.
        const sun_x = center_x + 200;
        const glow = context.createRadialGradient(sun_x, 330, 8, sun_x, 330, 560);
        glow.addColorStop(0, 'rgba(255,238,200,1)');
        glow.addColorStop(0.08, 'rgba(255,214,160,0.85)');
        glow.addColorStop(0.4, 'rgba(255,190,130,0.3)');
        glow.addColorStop(1, 'rgba(255,180,120,0)');
        context.fillStyle = glow;
        context.fillRect(0, 0, scene_width, 540);
        const hill_colors = scene_name === 'hill_sunrise' ? ['#8a7d78', '#6b6a55', '#4a5236'] : ['#7e6f72', '#5f5752', '#3d3733'];
        hill_colors.forEach(function draw_hill(color, layer) {
            context.fillStyle = color;
            context.beginPath();
            context.moveTo(0, 540);
            for (let x = 0; x <= scene_width; x += 16) {
                context.lineTo(x, 300 + layer * 45 - Math.sin(x * (0.004 + layer * 0.002) + layer) * (40 - layer * 8));
            }
            context.lineTo(scene_width, 540);
            context.fill();
        });
        if (scene_name === 'hill_sunrise') {
            draw_smoke_column(context, sun_x + 90, 300, time_seconds);
            context.fillStyle = 'rgba(40,35,30,0.85)';
            for (const person_x of [sun_x + 70, sun_x + 82, sun_x + 104]) {
                context.fillRect(person_x, 292, 3, 9);
                context.beginPath();
                context.arc(person_x + 1.5, 290, 2, 0, Math.PI * 2);
                context.fill();
            }
        }
        const road = context.createLinearGradient(0, floor_y, 0, 540);
        road.addColorStop(0, scene_name === 'hill_sunrise' ? '#5d5e3f' : '#4a4038');
        road.addColorStop(1, '#2a2520');
        context.fillStyle = road;
        context.fillRect(0, floor_y, scene_width, 140);
        const walk_x = center_x - 150 + ((time_seconds * 18) % 120);
        draw_figure('human', 'walk', walk_x, floor_y + 6, 1, { speed_ratio: 0.5, walk_phase: time_seconds * 5.2 });
        draw_figure('dog', 'walk', walk_x + 90, floor_y + 8, 1, { wag: 0.9, walk_phase: time_seconds * 7 });
        context.save();
        context.globalCompositeOperation = 'lighter';
        context.fillStyle = 'rgba(255,200,140,0.08)';
        context.fillRect(0, 0, scene_width, 540);
        context.restore();
    }
    // A soft vignette over every quiet scene.
    const vignette = context.createRadialGradient(center_x, 300, 140, center_x, 300, scene_width * 0.7);
    vignette.addColorStop(0, 'rgba(0,0,0,0)');
    vignette.addColorStop(1, 'rgba(0,0,0,0.6)');
    context.fillStyle = vignette;
    context.fillRect(0, 0, scene_width, 540);
    context.restore();
}

// Draws a five-pointed star, used for the painted ceiling.
function draw_star(context, x, y, radius) {
    context.beginPath();
    for (let point = 0; point < 10; point++) {
        const angle = (point / 10) * Math.PI * 2 - Math.PI / 2;
        const distance = point % 2 === 0 ? radius : radius * 0.45;
        context.lineTo(x + Math.cos(angle) * distance, y + Math.sin(angle) * distance);
    }
    context.closePath();
    context.fill();
}

// Adds a warm light that brightens whatever is underneath it.
function add_warm_glow(context, x, y, radius, strength) {
    context.save();
    context.globalCompositeOperation = 'lighter';
    const glow = context.createRadialGradient(x, y, 0, x, y, radius);
    glow.addColorStop(0, 'rgba(255,170,90,' + strength.toFixed(3) + ')');
    glow.addColorStop(1, 'rgba(255,140,60,0)');
    context.fillStyle = glow;
    context.fillRect(x - radius, y - radius, radius * 2, radius * 2);
    context.restore();
}
