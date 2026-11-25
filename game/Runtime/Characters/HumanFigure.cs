// Purpose: Em as a 3D figure: her body built in code from rounded pieces hung on bones, and the
// code that poses those bones every frame from what the choreography says she's doing. This is a
// stand-in until her real model arrives (made with Unity AI Generators and animated from
// generated motion), so it aims for her silhouette and colours (docs/characters.md): honey-blonde
// hair worn down past her shoulders, parted in the middle, the teal coat, the rust scarf, jeans
// and boots, rosy cheeks and blue-grey eyes.
//
// Her bones, top to bottom: hips; spine, chest, neck, head (with her hair on a springy bone); a
// shoulder, elbow and hand on each side; a hip, knee and ankle on each side. Her feet are placed
// with LegReach, so a planted foot stays put on the snow while her body moves over it.
using Bodhi.Look;
using UnityEngine;

namespace Bodhi.Game
{
    public class HumanFigure
    {
        // Her measurements, in metres. She's about 1.68 m tall.
        const float thigh_length = 0.42f, shin_length = 0.40f, ankle_height = 0.07f;
        const float hips_height = 0.93f;            // Her hips' pivot when standing straight.
        const float hip_joint_drop = 0.04f, hip_joint_side = 0.085f;
        const float upper_arm_length = 0.28f, forearm_length = 0.25f;

        static readonly Color skin = new Color(0.94f, 0.79f, 0.71f);
        static readonly Color cheeks = new Color(0.95f, 0.6f, 0.58f);
        static readonly Color eyes = new Color(0.23f, 0.31f, 0.39f);
        static readonly Color jeans = new Color(0.2f, 0.26f, 0.4f);
        static readonly Color boots = new Color(0.32f, 0.21f, 0.14f);

        readonly Transform root, hips, spine, chest, neck, head, hair, coat_skirt, scarf_end;
        readonly Transform left_hand, right_hand, left_foot_bone, right_foot_bone;
        readonly Limb left_leg, right_leg;
        readonly Transform left_upper_arm, right_upper_arm, left_forearm, right_forearm;

        HumanPose current = HumanPose.standing();
        float facing_now, facing_speed;             // Her turning, eased.
        float speed_now;                            // Her speed, eased.
        float phase;                                // How far through a stride.
        Vector3 last_position;
        bool placed = false;
        readonly Spring hair_swing = new Spring(0);
        readonly Spring hair_sway = new Spring(0);
        readonly Spring skirt_swing = new Spring(0), skirt_sway = new Spring(0);
        readonly Spring scarf_swing = new Spring(0), scarf_sway = new Spring(0);
        float last_hips_roll;
        float last_head_pitch, last_facing;

        // The middle of her two hands, in world space: where a ball she's holding sits.
        public Vector3 hands_middle => (left_hand.position + right_hand.position) / 2 + root.forward * 0.03f;

        // Her right hand, in world space: where a snowball she's about to throw sits.
        public Vector3 right_hand_point => right_hand.position + right_hand.up * -0.05f;

        // Her root (feet on the snow, turned the way she faces).
        public Transform body => root;

        // Her two ankles, in world space (for footprints in the snow).
        public Vector3[] foot_points => new[] { left_foot_bone.position, right_foot_bone.position };

        // Her mouth, in world space (where her breath mists in the cold).
        public Vector3 mouth_point => head.position + head.forward * 0.1f + head.up * 0.04f;

        // Builds Em under `parent`.
        public static HumanFigure build_em(Transform parent)
        {
            return new HumanFigure(parent);
        }

        HumanFigure(Transform parent)
        {
            var material = WorldLook.solid(rim: 0.55f, wrap: 0.45f);
            var hair_material = WorldLook.solid(rim: 0.7f, wrap: 0.5f, gloss: 0.35f);
            root = Limb.bone("Em", parent, Vector3.zero);
            hips = Limb.bone("hips", root, new Vector3(0, hips_height, 0));
            spine = Limb.bone("spine", hips, new Vector3(0, 0.1f, 0));
            chest = Limb.bone("chest", spine, new Vector3(0, 0.17f, 0));
            neck = Limb.bone("neck", chest, new Vector3(0, 0.235f, 0));
            head = Limb.bone("head", neck, new Vector3(0, 0.085f, 0.01f));
            hair = Limb.bone("hair", head, new Vector3(0, 0.12f, -0.03f));
            coat_skirt = Limb.bone("coat skirt", hips, new Vector3(0, 0.06f, 0));  // Hangs from her waist.
            scarf_end = Limb.bone("scarf end", chest, new Vector3(-0.05f, 0.19f, 0.1f));

            var coat = PaletteColours.her_coat;
            var coat_dark = Color.Lerp(coat, Color.black, 0.25f);
            var scarf = PaletteColours.her_scarf;
            var hair_colour = PaletteColours.her_hair;
            var hair_shade = Color.Lerp(hair_colour, new Color(0.55f, 0.4f, 0.2f), 0.35f);

            // The coat: its skirt flares from the waist to mid-thigh; then the waist and the chest.
            var skirt = new MeshKit();
            skirt.add_round_shape(new Vector3(0, -0.37f, 0), Quaternion.identity, new[]
            {
                new MeshKit.Ring(0, 0, coat_dark),
                new MeshKit.Ring(0.002f, 0.175f, coat_dark),
                new MeshKit.Ring(0.02f, 0.19f, coat),
                new MeshKit.Ring(0.18f, 0.175f, coat),
                new MeshKit.Ring(0.32f, 0.155f, coat),
                new MeshKit.Ring(0.4f, 0.14f, coat),
            }, 16, new Vector3(1, 1, 0.76f));
            skirt.add_ball(new Vector3(0, -0.01f, 0), new Vector3(0.152f, 0.022f, 0.118f), coat_dark, 10);  // Her belt.
            part("coat skirt", coat_skirt, skirt, material);

            var waist = new MeshKit();
            waist.add_ball(new Vector3(0, 0.06f, 0), new Vector3(0.14f, 0.12f, 0.105f), coat, 10);
            part("waist", spine, waist, material);

            var torso = new MeshKit();
            torso.add_ball(new Vector3(0, 0.1f, 0), new Vector3(0.17f, 0.19f, 0.115f), coat, 12);
            torso.add_ball(new Vector3(-0.145f, 0.175f, 0), Vector3.one * 0.065f, coat, 8);
            torso.add_ball(new Vector3(0.145f, 0.175f, 0), Vector3.one * 0.065f, coat, 8);
            for (int button = 0; button < 3; button++)
            {
                torso.add_ball(new Vector3(0.035f, 0.02f + button * 0.08f, 0.112f), Vector3.one * 0.011f, coat_dark, 4);
            }
            // The rust scarf, wound round her neck, its knot and one end hanging down the front.
            torso.add_ball(new Vector3(0, 0.24f, 0), new Vector3(0.1f, 0.055f, 0.092f), scarf, 10, 0.006f, 0.03f);
            torso.add_ball(new Vector3(-0.045f, 0.205f, 0.085f), new Vector3(0.04f, 0.045f, 0.03f), scarf, 6);
            part("chest", chest, torso, material);
            var scarf_tail = new MeshKit();
            scarf_tail.add_tube(Vector3.zero, new Vector3(-0.012f, -0.21f, 0.018f), 0.034f, 0.03f, scarf, Color.Lerp(scarf, Color.black, 0.15f), 10, 0.003f, 0.02f);
            scarf_tail.add_ball(new Vector3(-0.012f, -0.23f, 0.018f), new Vector3(0.034f, 0.02f, 0.03f), Color.Lerp(scarf, new Color(0.9f, 0.8f, 0.6f), 0.3f), 6, 0.006f, 0.01f);  // Fringe.
            part("scarf end", scarf_end, scarf_tail, material);

            var neck_part = new MeshKit();
            neck_part.add_tube(Vector3.zero, new Vector3(0, 0.09f, 0.008f), 0.045f, 0.043f, skin, skin, 8);
            part("neck", neck, neck_part, material);

            // Her face: rounded head and chin, a small nose, blue-grey eyes, brows, rosy cheeks.
            var face = new MeshKit();
            face.add_ball(new Vector3(0, 0.085f, 0.005f), new Vector3(0.082f, 0.108f, 0.094f), skin, 12);
            face.add_ball(new Vector3(0, 0.03f, 0.035f), new Vector3(0.058f, 0.05f, 0.062f), skin, 8);
            face.add_ball(new Vector3(0, 0.08f, 0.097f), new Vector3(0.014f, 0.02f, 0.016f), Color.Lerp(skin, cheeks, 0.2f), 6);
            for (int side = -1; side <= 1; side += 2)
            {
                face.add_ball(new Vector3(side * 0.031f, 0.1f, 0.083f), new Vector3(0.013f, 0.011f, 0.009f), eyes, 6);
                face.add_ball(new Vector3(side * 0.033f, 0.12f, 0.086f), new Vector3(0.019f, 0.005f, 0.007f), hair_shade, 4);
                face.add_ball(new Vector3(side * 0.05f, 0.064f, 0.072f), new Vector3(0.022f, 0.016f, 0.012f), cheeks, 6);
            }
            face.add_ball(new Vector3(0, 0.043f, 0.088f), new Vector3(0.016f, 0.005f, 0.006f), new Color(0.78f, 0.47f, 0.46f), 4);
            // The hair over the top and sides of her head, parted in the middle.
            face.add_ball(new Vector3(0, 0.118f, -0.018f), new Vector3(0.092f, 0.1f, 0.1f), hair_colour, 12);
            face.add_ball(new Vector3(0, 0.205f, 0.01f), new Vector3(0.006f, 0.012f, 0.06f), hair_shade, 4);
            part("head", head, face, material);

            // Her long hair, falling straight past her shoulders, behind and at both sides of
            // her face. It hangs from its own bone, which swings a little as she moves.
            var long_hair = new MeshKit();
            long_hair.add_round_shape(new Vector3(0, -0.36f, -0.035f), Quaternion.identity, new[]
            {
                new MeshKit.Ring(0, 0, hair_shade),
                new MeshKit.Ring(0.01f, 0.08f, hair_shade),
                new MeshKit.Ring(0.08f, 0.098f, hair_colour),
                new MeshKit.Ring(0.22f, 0.1f, hair_colour),
                new MeshKit.Ring(0.33f, 0.093f, hair_colour),
                new MeshKit.Ring(0.4f, 0.07f, hair_colour),
                new MeshKit.Ring(0.42f, 0, hair_colour),
            }, 14, new Vector3(1.05f, 1, 0.42f));
            for (int side = -1; side <= 1; side += 2)
            {
                // The locks that frame her face and fall in front of her shoulders.
                long_hair.add_tube(new Vector3(side * 0.078f, 0.0f, 0.045f), new Vector3(side * 0.1f, -0.29f, 0.06f), 0.03f, 0.022f, hair_colour, hair_shade, 10);
            }
            part("long hair", hair, long_hair, hair_material);

            (left_upper_arm, left_forearm, left_hand) = make_arm("left arm", -1, coat, scarf, material);
            (right_upper_arm, right_forearm, right_hand) = make_arm("right arm", 1, coat, scarf, material);
            left_leg = make_leg("left leg", -1, material, out left_foot_bone);
            right_leg = make_leg("right leg", 1, material, out right_foot_bone);
            apply(current);
        }

        // One arm: a coat sleeve in two parts with a darker cuff, and a rust mitten with a thumb.
        (Transform upper, Transform lower, Transform hand) make_arm(string name, int side, Color coat, Color mitten, Material material)
        {
            var (upper, lower) = Limb.make_bones(name, chest, new Vector3(side * 0.175f, 0.2f, -0.01f), upper_arm_length);
            var hand = Limb.bone(name + " hand", lower, new Vector3(0, -forearm_length, 0));
            var sleeve = new MeshKit();
            sleeve.add_tube(Vector3.zero, new Vector3(0, -upper_arm_length, 0), 0.054f, 0.047f, coat, coat, 10);
            part(name + " sleeve", upper, sleeve, material);
            var forearm = new MeshKit();
            forearm.add_tube(Vector3.zero, new Vector3(0, -forearm_length + 0.02f, 0), 0.047f, 0.043f, coat, coat, 10);
            forearm.add_ball(new Vector3(0, -forearm_length + 0.025f, 0), new Vector3(0.05f, 0.022f, 0.05f), Color.Lerp(coat, Color.black, 0.25f), 8);
            part(name + " forearm", lower, forearm, material);
            var mitt = new MeshKit();
            mitt.add_ball(new Vector3(0, -0.055f, 0.005f), new Vector3(0.036f, 0.062f, 0.03f), mitten, 8, 0.004f, 0.02f);
            mitt.add_ball(new Vector3(-side * 0.025f, -0.03f, 0.022f), new Vector3(0.016f, 0.026f, 0.016f), mitten, 6);
            part(name + " mitten", hand, mitt, material);
            return (upper, lower, hand);
        }

        // One leg: jeans over thigh and shin, and a brown boot.
        Limb make_leg(string name, int side, Material material, out Transform foot)
        {
            var (upper, lower) = Limb.make_bones(name, hips, new Vector3(side * hip_joint_side, -hip_joint_drop, 0), thigh_length);
            foot = Limb.bone(name + " foot", lower, new Vector3(0, -shin_length, 0));
            var thigh = new MeshKit();
            thigh.add_tube(Vector3.zero, new Vector3(0, -thigh_length, 0), 0.075f, 0.056f, jeans, jeans, 10);
            part(name + " thigh", upper, thigh, material);
            var shin = new MeshKit();
            shin.add_tube(Vector3.zero, new Vector3(0, -shin_length + 0.03f, 0), 0.055f, 0.046f, jeans, Color.Lerp(jeans, Color.black, 0.1f), 10);
            part(name + " shin", lower, shin, material);
            var boot = new MeshKit();
            boot.add_tube(new Vector3(0, 0.04f, 0), new Vector3(0, -0.035f, 0), 0.05f, 0.048f, boots, boots, 10);
            boot.add_ball(new Vector3(0, -0.04f, 0.05f), new Vector3(0.047f, 0.038f, 0.11f), boots, 10);
            boot.add_ball(new Vector3(0, -0.064f, 0.045f), new Vector3(0.05f, 0.012f, 0.12f), Color.Lerp(boots, Color.black, 0.5f), 8);
            part(name + " boot", foot, boot, material);
            return new Limb(upper, lower, thigh_length, shin_length, true);
        }

        // Adds one body part's mesh to a bone.
        static void part(string name, Transform bone, MeshKit kit, Material material)
        {
            MeshKit.show(name, kit.build(name), material, bone, true);
        }

        // Moves her on one frame to match `pose` from the choreography.
        public void update(PlayPose pose, float delta, double seconds)
        {
            var position = new Vector3((float)pose.x, (float)pose.height, (float)pose.z);
            if (!placed || delta <= 0)
            {
                placed = true;
                facing_now = (float)pose.facing;
                last_position = position;
                speed_now = (float)pose.speed;
            }
            float moved = Vector3.Distance(new Vector3(position.x, 0, position.z), new Vector3(last_position.x, 0, last_position.z));
            last_position = position;
            facing_now = Mathf.SmoothDampAngle(facing_now, (float)pose.facing, ref facing_speed, 0.16f, 720f, Mathf.Max(delta, 0.0001f));
            speed_now = Mathf.Lerp(speed_now, (float)pose.speed, 1 - Mathf.Exp(-delta / 0.15f));
            phase = (float)Stride.advance(phase, moved, Stride.human_stride_length(speed_now));
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0, facing_now, 0);

            var target = choose_pose(pose, (float)seconds);
            target = look(target, pose, pose.action == "crouch" || pose.action == "push" ? 0.3f : 1f);
            bool walking = speed_now > 0.05f;
            float body_ease = 1 - Mathf.Exp(-delta / 0.14f);
            float foot_ease = walking ? 1 : 1 - Mathf.Exp(-delta / 0.1f);
            var left_foot = HumanPose.blend(current.left_foot, target.left_foot, foot_ease);
            var right_foot = HumanPose.blend(current.right_foot, target.right_foot, foot_ease);
            current = HumanPose.blend(current, target, delta <= 0 ? 1 : body_ease);
            current.left_foot = delta <= 0 ? target.left_foot : left_foot;
            current.right_foot = delta <= 0 ? target.right_foot : right_foot;
            apply(current);
            swing_hair(delta);
            swing_cloth(delta);
        }

        // Her coat skirt and the end of her scarf swing behind her movement: they trail back as
        // she speeds up, sway as her hips rock from step to step, and settle when she stops.
        // Springs give that lag and settle (overlapping action, as animators call it).
        void swing_cloth(float delta)
        {
            if (delta <= 0)
            {
                return;
            }
            float hips_rock = (current.hips_roll - last_hips_roll) / delta;
            last_hips_roll = current.hips_roll;
            skirt_swing.update(speed_now * 5 - current.hips_pitch * 0.5f, 90, 7, delta);
            skirt_sway.update(-current.hips_roll * 0.9f - hips_rock * 0.02f, 80, 6, delta);
            coat_skirt.localRotation = Quaternion.Euler((float)skirt_swing.value, 0, (float)skirt_sway.value);
            float lean = current.spine_pitch + current.chest_pitch + current.hips_pitch;
            scarf_swing.update(-lean * 0.85f + speed_now * 14, 60, 4, delta);
            scarf_sway.update(-current.chest_yaw * 0.4f - hips_rock * 0.03f, 50, 3.5f, delta);
            scarf_end.localRotation = Quaternion.Euler((float)scarf_swing.value, 0, (float)scarf_sway.value);
        }

        // The pose for what she's doing now.
        HumanPose choose_pose(PlayPose pose, float seconds)
        {
            var walking = HumanPose.moving(speed_now, phase);
            float share = (float)pose.action_share;
            float since = (float)pose.action_seconds;
            HumanPose chosen;
            switch (pose.action)
            {
                case "move": return walking;
                case "push": return HumanPose.pushing(walking, Mathf.Lerp(0.12f, 0.3f, share));
                case "carry": return HumanPose.holding(walking);
                case "crouch": chosen = HumanPose.crouching(seconds, true); break;
                case "lift": chosen = HumanPose.lifting(share, seconds); break;
                case "place": chosen = HumanPose.placing(share, false); break;
                case "place_high": chosen = HumanPose.placing(share, true); break;
                case "pack": chosen = HumanPose.packing(seconds); break;
                case "throw": chosen = HumanPose.throwing(share); break;
                case "pat": chosen = HumanPose.patting(seconds); break;
                case "admire": chosen = HumanPose.admiring(seconds); break;
                case "open_arms": chosen = HumanPose.arms_open(seconds); break;
                case "hug": chosen = HumanPose.hugging(seconds); break;
                case "look_up": chosen = HumanPose.looking_up(seconds); break;
                default: chosen = HumanPose.standing(); break;
            }
            return HumanPose.breathing(chosen, seconds, 1);
        }

        // Turns her head (and a little of her chest) toward what she's looking at.
        HumanPose look(HumanPose target, PlayPose pose, float how_much)
        {
            if (pose.look != LookAt.partner && pose.look != LookAt.snowman)
            {
                return target;
            }
            // The choreography's look target is turned into a direction by TitleWorld; here we
            // only know her facing, so she glances toward the snowman's side or her partner's.
            float turn = Mathf.Clamp(look_yaw, -70, 70) * how_much;  // Bent over, she barely turns her head.
            target.head_yaw += turn * 0.7f;
            target.chest_yaw += turn * 0.2f;
            return target;
        }

        // How far her head should turn, in degrees from her facing, toward what she's looking at.
        // TitleWorld sets this each frame from where the thing is.
        public float look_yaw { get; set; }

        // Sets every bone from a pose.
        void apply(HumanPose pose)
        {
            hips.localPosition = new Vector3(pose.hips_side, hips_height - pose.hips_drop, pose.hips_forward);
            hips.localRotation = Quaternion.Euler(pose.hips_pitch, pose.hips_yaw, pose.hips_roll);
            spine.localRotation = Quaternion.Euler(pose.spine_pitch, pose.chest_yaw * 0.4f - pose.hips_yaw * 0.5f, pose.chest_roll * 0.5f);
            chest.localRotation = Quaternion.Euler(pose.chest_pitch, pose.chest_yaw * 0.6f - pose.hips_yaw * 0.5f, pose.chest_roll * 0.5f - pose.hips_roll);
            neck.localRotation = Quaternion.Euler(pose.neck_pitch, pose.head_yaw * 0.35f, 0);
            head.localRotation = Quaternion.Euler(pose.head_pitch, pose.head_yaw * 0.65f, pose.head_roll);
            pose_arm(left_upper_arm, left_forearm, pose.left_arm, -1);
            pose_arm(right_upper_arm, right_forearm, pose.right_arm, 1);
            place_foot(left_leg, left_foot_bone, pose.left_foot, -1);
            place_foot(right_leg, right_foot_bone, pose.right_foot, 1);
        }

        // Points one arm: swing it forward, lift it out to the side, turn it so the elbow bends
        // the right way, then bend the elbow.
        static void pose_arm(Transform upper, Transform lower, ArmPose arm, int side)
        {
            upper.localRotation = Quaternion.Euler(-arm.forward, 0, 0) * Quaternion.Euler(0, 0, side * arm.outward) * Quaternion.Euler(0, -side * arm.twist, 0);
            lower.localRotation = Quaternion.Euler(-arm.elbow, 0, 0);
        }

        // Puts one foot on its spot (the leg bends to reach it) and tilts it heel-to-toe.
        void place_foot(Limb leg, Transform foot, FootPose spot, int side)
        {
            float heel_rise = spot.toe_up < 0 ? 0.13f * Mathf.Sin(-spot.toe_up * Mathf.Deg2Rad) : 0;
            var target = new Vector3(side * (hip_joint_side + spot.side), ankle_height + spot.lift + heel_rise, spot.forward);
            leg.reach(root, target, side * 4);
            foot.rotation = root.rotation * Quaternion.Euler(-spot.toe_up, side * 6, 0);
        }

        // Her long hair hangs straight down whatever her head does, lags behind when she turns,
        // and swings back and forth as she runs: two springs, one for each way it can swing.
        void swing_hair(float delta)
        {
            if (delta <= 0)
            {
                return;
            }
            float head_pitch = Vector3.SignedAngle(root.forward, Vector3.ProjectOnPlane(head.forward, root.right), root.right);
            float turn_rate = Mathf.DeltaAngle(last_facing, facing_now) / delta;
            float nod_rate = (head_pitch - last_head_pitch) / delta;
            last_facing = facing_now;
            last_head_pitch = head_pitch;
            float hang = -head_pitch * 0.85f;   // Stay hanging down when the head tips.
            hair_swing.update(hang - nod_rate * 0.05f + speed_now * 6, 140, 9, delta);  // Streams back as she runs.
            hair_sway.update(-turn_rate * 0.04f, 120, 8, delta);
            hair.localRotation = Quaternion.Euler((float)hair_swing.value, 0, (float)hair_sway.value);
        }
    }
}
