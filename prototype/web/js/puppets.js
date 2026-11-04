// Purpose: animates the painted characters. Each body part is cut from its painted sheet
// (art/characters/*.svg, mapped by art_atlas.js) and drawn rotated around its joint, driven by
// the same motion system as before: bending joints, springs, leaning and pose blending.
// This is "cut-out" animation, the way many professional 2D games animate painted characters.
// Until the sheets have loaded, the older code-drawn characters are used instead.
'use strict';

const puppets = {
    sheets: {},          // The loaded sheet images, by character name.
    parts: {},           // Cut-out part canvases: puppets.parts.her.thigh, puppets.parts.her.thigh_far
    pixels_per_unit: 0,  // The resolution the parts were cut at.
    ready: false,
};

// Loads both character sheets. Tries the path next to the page first (the published game),
// then the repository's art folder (when playing prototype/web/index.html from a checkout).
function load_puppet_sheets() {
    let remaining = Object.keys(art_atlas).length;
    for (const [name, entry] of Object.entries(art_atlas)) {
        const image = new Image();
        // Playing from the repository (prototype/web/index.html), the art is two folders up;
        // in the published game it sits next to the page. Try the likely one first.
        const from_repository = window.location.pathname.indexOf('/prototype/web/') !== -1;
        const candidates = from_repository ? ['../../' + entry.sheet, entry.sheet] : [entry.sheet, '../../' + entry.sheet];
        let attempt = 0;
        image.onload = function sheet_loaded() {
            puppets.sheets[name] = image;
            remaining -= 1;
            if (remaining === 0) {
                cut_puppet_parts(true);
            }
        };
        image.onerror = function sheet_failed() {
            attempt += 1;
            if (attempt < candidates.length) {
                image.src = candidates[attempt];
            }
        };
        image.src = candidates[0];
    }
}

// Cuts every part out of its sheet at the resolution the screen needs. Done once, and again
// only if the screen's resolution changes a lot. Also makes a darker copy of each part for the
// far-side arm and leg, which sit in the character's own shadow.
function cut_puppet_parts(force) {
    if (Object.keys(puppets.sheets).length < Object.keys(art_atlas).length) {
        return;
    }
    const camera = game.camera;
    const wanted = Math.max(1.5, game.pixel_scale * (camera.close_zoom || 1) * 1.35);
    if (!force && Math.abs(wanted - puppets.pixels_per_unit) / Math.max(wanted, 0.01) < 0.15) {
        return;
    }
    puppets.pixels_per_unit = wanted;
    const scale = wanted / 10;  // The art is drawn at 10 drawing units per game unit.
    for (const [name, entry] of Object.entries(art_atlas)) {
        puppets.parts[name] = {};
        for (const [part_name, part] of Object.entries(entry.parts)) {
            const canvas = make_sprite_canvas(part.w * scale, part.h * scale);
            const part_context = canvas.getContext('2d');
            part_context.drawImage(puppets.sheets[name], part.x, part.y, part.w, part.h, 0, 0, canvas.width, canvas.height);
            puppets.parts[name][part_name] = canvas;
            const far = make_sprite_canvas(canvas.width, canvas.height);
            const far_context = far.getContext('2d');
            far_context.drawImage(canvas, 0, 0);
            far_context.globalCompositeOperation = 'source-atop';
            far_context.fillStyle = 'rgba(12,16,26,0.38)';
            far_context.fillRect(0, 0, far.width, far.height);
            puppets.parts[name][part_name + '_far'] = far;
        }
    }
    puppets.ready = true;
}

// Draws one part with its joint at (x, y), turned by `angle` (radians). Lengths in the art
// are 10 drawing units per game unit, so the part is drawn at a tenth of its sheet size.
function draw_part(context, character_name, part_name, x, y, angle, scale_x, scale_y) {
    const part = art_atlas[character_name].parts[part_name.replace('_far', '')];
    const canvas = puppets.parts[character_name][part_name];
    context.save();
    context.translate(x, y);
    if (angle) {
        context.rotate(angle);
    }
    if (scale_x !== undefined) {
        context.scale(scale_x, scale_y === undefined ? 1 : scale_y);
    }
    context.drawImage(canvas, -part.joint_x / 10, -part.joint_y / 10, part.w / 10, part.h / 10);
    context.restore();
}

// The angle that turns a part hanging straight down so it points from one joint to the next.
function bone_angle(from_point, to_point) {
    return Math.atan2(-(to_point[0] - from_point[0]), to_point[1] - from_point[1]);
}

// Turns a point given in a part's own coordinates into the coordinates it's drawn in.
function point_on_part(origin, angle, local_x, local_y) {
    const cosine = Math.cos(angle);
    const sine = Math.sin(angle);
    return [origin[0] + local_x * cosine - local_y * sine, origin[1] + local_x * sine + local_y * cosine];
}

// Draws the woman from her painted parts. Pose, joints, springs and leaning all come from
// the same shape code the motion system already uses.
function draw_human_puppet(context, human, time_seconds) {
    const pose = human.pose || 'stand';
    context.save();
    context.translate(human.x + human.width / 2, human.y + human.height);
    context.rotate(human.lean ? human.lean.value : 0);
    context.scale(turn_scale(human) * (human.squash_x || 1), human.squash_y || 1);
    const shape = blend_pose_shape(human, pose, human_shape(pose, human, time_seconds), time_seconds, 0.16);
    const knee_back = solve_joint(shape.hip, shape.foot_back, 23, 22, -1);
    const knee_front = solve_joint(shape.hip, shape.foot_front, 23, 22, -1);
    const elbow_back = solve_joint(shape.shoulder, shape.hand_back, 14, 13, 1);
    const elbow_front = solve_joint(shape.shoulder, shape.hand_front, 14, 13, 1);
    // The coat hangs from the shoulders down to the hip, so its angle runs shoulder to hip.
    const torso_angle = bone_angle(shape.shoulder, shape.hip);
    const head_angle = (human.head_tilt ? human.head_tilt.value : 0) + torso_angle * 0.4;
    const hair = human.hair ? human.hair.value : (human.ponytail_angle || 0);
    const scarf = human.scarf ? human.scarf.value : 0.3;
    const hem = human.hem ? human.hem.value : 0;

    // Far side first: it's further away, so it's darker and drawn underneath.
    const draw_leg = function draw_leg_parts(knee, foot, suffix) {
        const shin_angle = bone_angle(knee, foot);
        draw_part(context, 'her', 'thigh' + suffix, shape.hip[0], shape.hip[1], bone_angle(shape.hip, knee));
        draw_part(context, 'her', 'shin' + suffix, knee[0], knee[1], shin_angle);
        draw_part(context, 'her', 'boot' + suffix, foot[0], foot[1] - 0.4, shin_angle * 0.25);
    };
    const draw_arm = function draw_arm_parts(elbow, hand, suffix) {
        draw_part(context, 'her', 'upper_arm' + suffix, shape.shoulder[0], shape.shoulder[1], bone_angle(shape.shoulder, elbow));
        draw_part(context, 'her', 'forearm' + suffix, elbow[0], elbow[1], bone_angle(elbow, hand));
    };
    draw_arm(elbow_back, shape.hand_back, '_far');
    draw_leg(knee_back, shape.foot_back, '_far');
    const bag_joint = point_on_part(shape.hip, torso_angle, -6.6, -9.6);
    draw_part(context, 'her', 'bag', bag_joint[0], bag_joint[1], torso_angle * 0.5 + hem * 0.35);
    draw_leg(knee_front, shape.foot_front, '');
    draw_part(context, 'her', 'torso', shape.hip[0], shape.hip[1], torso_angle);
    const scarf_joint = point_on_part(shape.hip, torso_angle, -3.6, -26.4);
    draw_part(context, 'her', 'scarf_tail', scarf_joint[0], scarf_joint[1], 0.25 + scarf * 0.9);
    const tie = point_on_part(shape.shoulder, head_angle, -7.8, -14.8);
    draw_part(context, 'her', 'ponytail', tie[0], tie[1], 0.3 + hair * 1.4);
    draw_part(context, 'her', 'head', shape.shoulder[0], shape.shoulder[1], head_angle);
    draw_arm(elbow_front, shape.hand_front, '');
    if (human.holding) {
        draw_carried_item(context, human.holding, shape.hand_front[0] + 3, shape.hand_front[1] - 2);
    }
    context.restore();
}

// Draws the goldendoodle from its painted parts.
function draw_dog_puppet(context, dog, time_seconds) {
    const pose = dog.pose || 'stand';
    const fear = dog.fear || 0;
    context.save();
    context.translate(dog.x + dog.width / 2, dog.y + dog.height);
    context.rotate(dog.lean ? dog.lean.value : 0);
    context.scale(turn_scale(dog) * (dog.squash_x || 1), dog.squash_y || 1);
    const shape = blend_pose_shape(dog, pose, dog_shape(pose, dog, time_seconds), time_seconds, 0.18);
    const moving = pose === 'walk';
    const wag_speed = 7 + (dog.wag || 0) * 13;
    const wag = Math.sin(time_seconds * wag_speed) * (0.16 + (dog.wag || 0) * 0.42);
    // The signature head tilt: every so often, standing still, the dog tilts its head.
    const tilt_cycle = (time_seconds + (dog.x || 0) * 0.001) % 7.5;
    const head_tilt = !moving && pose !== 'sleep' && tilt_cycle > 6 ? Math.sin((tilt_cycle - 6) / 1.5 * Math.PI) * -0.32 : 0;
    const head = [shape.head[0], shape.head[1] + (fear > 0.5 && pose !== 'sleep' && pose !== 'embrace' ? 3 : 0)];
    const head_angle = shape.head_angle + head_tilt;
    const sleeping = pose === 'sleep';

    const draw_leg = function draw_leg_parts(leg, upper, lower, suffix) {
        const hip = [leg[0], leg[1]];
        const paw = [leg[2], leg[3] - 0.6];
        // Legs are 10.5 + 10.5 units; the painted segments (9.5 each) are stretched to fit.
        const joint = solve_joint(hip, paw, 10.5, 10.5, 1);
        draw_part(context, 'bodhi', upper + suffix, hip[0], hip[1], bone_angle(hip, joint), 1, 1.1);
        draw_part(context, 'bodhi', lower + suffix, joint[0], joint[1], bone_angle(joint, paw), 1, 1.1);
    };
    draw_leg(shape.legs[0], 'front_upper', 'front_lower', '_far');
    draw_leg(shape.legs[1], 'back_upper', 'back_lower', '_far');

    // The tail: up and busy when happy, tucked low when afraid, always on a spring.
    if (pose !== 'embrace') {
        const tail_base = point_on_part(shape.body, shape.body_tilt, (shape.hind_x - 8) * 0.88, -6);
        const tail_spring = dog.tail ? dog.tail.value : -0.7 + fear * 1.3;
        const tail_angle = sleeping ? -0.6 : 0.45 - (tail_spring + 0.75) + wag;
        draw_part(context, 'bodhi', 'tail', tail_base[0], tail_base[1], tail_angle, 0.62, 0.62);
    }
    // The body stretches a little at a gallop, as the spine extends and gathers.
    const stretch = (shape.chest_x - shape.hind_x) / 24;
    draw_part(context, 'bodhi', 'body', shape.body[0], shape.body[1], shape.body_tilt, stretch * 0.86, sleeping ? 0.78 : 1.02);

    draw_leg(shape.legs[2], 'front_upper', 'front_lower', '');
    draw_leg(shape.legs[3], 'back_upper', 'back_lower', '');

    // Head, then the floppy ear on top of it, swinging on its spring.
    draw_part(context, 'bodhi', 'head', head[0], head[1], head_angle, 0.98, 0.98);
    const ear_root = point_on_part(head, head_angle, -4.2, -3.6);
    const ear_angle = (dog.ear ? dog.ear.value : 0.2) + (moving ? Math.sin((dog.walk_phase || 0) * 2) * 0.08 : 0);
    draw_part(context, 'bodhi', 'ear', ear_root[0], ear_root[1], head_angle + ear_angle * 0.9, 0.9, 0.9);
    // Closed eyes for sleeping, leaning in and the embrace: a soft lid over the painted eye.
    const blinking = ((dog.x || 0) * 0.29 + performance.now() / 1000) % 3.7 < 0.11;
    if (sleeping || pose === 'lean' || pose === 'embrace' || blinking) {
        const eye = point_on_part(head, head_angle, 4.4, -2.2);
        context.fillStyle = '#ecc791';
        context.beginPath();
        context.ellipse(eye[0], eye[1], 2.3, 2.4, head_angle, 0, Math.PI * 2);
        context.fill();
        context.strokeStyle = '#3a2418';
        context.lineWidth = 0.5;
        context.beginPath();
        context.arc(eye[0], eye[1] + 0.2, 1.7, 0.25, Math.PI - 0.25);
        context.stroke();
    }
    if (dog.carrying) {
        const mouth = point_on_part(head, head_angle, 11.5, 6.5);
        draw_carried_item(context, dog.carrying, mouth[0], mouth[1]);
    }
    context.restore();
}
