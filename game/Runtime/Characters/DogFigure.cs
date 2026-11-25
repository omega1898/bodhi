// Purpose: Bodhi as a 3D figure: a curly goldendoodle built in code from knobbly rounded pieces
// (the knobs read as curls) hung on bones, and the code that poses those bones every frame from
// what the choreography says he's doing. A stand-in until his real model arrives; it aims for
// his silhouette and colours (docs/characters.md): red-apricot curls, darker ginger on his head
// and ears, a cream muzzle, beard and bib, a black nose, dark eyes, pale paws, floppy ears and a
// plumed tail.
//
// His bones: his back half (hips, with the hind legs and the tail), his front half (chest, with
// the front legs and the neck) hinged to it so his back can round and stretch in a gallop, then
// neck, head, ears and jaw. Each leg reaches for its paw's spot with LegReach, so planted paws
// stay put on the snow.
using Bodhi.Look;
using UnityEngine;

namespace Bodhi.Game
{
    public class DogFigure
    {
        // His measurements, in metres: about 0.58 m at the shoulder.
        const float rear_height = 0.46f;
        static readonly Vector3 rear_spot = new Vector3(0, rear_height, -0.2f);
        static readonly Vector3 front_offset = new Vector3(0, 0.02f, 0.36f);      // Front half, from the back half.
        static readonly Vector3 hip_joint = new Vector3(0.075f, -0.06f, -0.08f);   // From the back half.
        static readonly Vector3 shoulder_joint = new Vector3(0.085f, -0.13f, 0.12f);  // From the front half.
        const float thigh = 0.19f, gaskin = 0.19f, hind_foot = 0.12f, hind_paw_height = 0.03f;
        const float upper_arm = 0.15f, forearm = 0.16f, front_paw_height = 0.055f;

        static readonly Color curls = new Color(0.86f, 0.6f, 0.37f);
        static readonly Color ginger = new Color(0.74f, 0.45f, 0.26f);
        static readonly Color cream = new Color(0.96f, 0.9f, 0.8f);
        static readonly Color pale_paws = new Color(0.93f, 0.83f, 0.68f);
        static readonly Color nose_black = new Color(0.05f, 0.04f, 0.04f);
        static readonly Color eye_brown = new Color(0.12f, 0.07f, 0.05f);
        static readonly Color tongue_pink = new Color(0.86f, 0.42f, 0.45f);
        static readonly Color collar_red = new Color(0.65f, 0.12f, 0.12f);

        readonly Transform root, rear, front, neck, head, jaw, left_ear, right_ear, mouth_point;
        readonly Transform[] tail = new Transform[3];
        readonly Limb[] legs = new Limb[4];                  // Front left, front right, hind left, hind right.
        readonly Transform[] feet = new Transform[4];        // The last part of each leg: pastern or hind foot.
        readonly Vector3[] paw_home = new Vector3[4];        // Where each paw stands, from his root, when standing.

        DogPose current = DogPose.standing();
        SkinnedModel model;   // His real model from Blender, once he's wearing it.
        float facing_now, facing_speed, speed_now, phase, wag_clock;
        Vector3 last_position;
        bool placed = false;
        readonly Spring ear_bounce = new Spring(0);
        readonly Spring tail_bounce = new Spring(0);
        float last_height;

        // Where the stick or a caught snowball sits: between his jaws, in world space.
        public Vector3 mouth => mouth_point.position;

        // How his mouth is turned (for laying the stick across it).
        public Quaternion mouth_turn => head.rotation;

        // His root (paws on the snow, turned the way he faces).
        public Transform body => root;

        // The bottom of each paw, in world space (for paw prints in the snow).
        public Vector3[] paw_points
        {
            get
            {
                var points = new Vector3[4];
                for (int leg = 0; leg < 4; leg++)
                {
                    float length = leg < 2 ? front_paw_height : hind_foot + hind_paw_height;
                    points[leg] = feet[leg].position - feet[leg].up * length;
                }
                return points;
            }
        }

        // How far his head should turn toward what he's looking at; TitleWorld sets it.
        public float look_yaw { get; set; }

        // How fast he's moving now, metres a second (for puffs of snow from his paws).
        public float speed => speed_now;

        // Builds Bodhi under `parent`.
        public static DogFigure build_bodhi(Transform parent)
        {
            return new DogFigure(parent);
        }

        DogFigure(Transform parent)
        {
            var coat = WorldLook.solid(rim: 0.65f, wrap: 0.6f);
            var shiny = WorldLook.solid(rim: 0.2f, gloss: 0.8f);
            root = Limb.bone("Bodhi", parent, Vector3.zero);
            rear = Limb.bone("back half", root, rear_spot);
            front = Limb.bone("front half", rear, front_offset);
            neck = Limb.bone("neck", front, new Vector3(0, 0.06f, 0.16f));
            head = Limb.bone("head", neck, new Vector3(0, 0.115f, 0.075f));
            jaw = Limb.bone("jaw", head, new Vector3(0, -0.035f, 0.06f));
            mouth_point = Limb.bone("mouth", head, new Vector3(0, -0.035f, 0.12f));

            // His back half: hips and loins, all curls.
            var hips = new MeshKit();
            hips.add_ball(new Vector3(0, -0.01f, -0.02f), new Vector3(0.135f, 0.145f, 0.19f), curls, 30, 0.024f, 0.05f);
            hips.add_ball(new Vector3(0, -0.07f, 0.12f), new Vector3(0.115f, 0.105f, 0.15f), curls, 24, 0.02f, 0.05f);
            part("back half", rear, hips, coat);

            // His front half: deep chest with a cream bib, shoulders, and the red collar.
            var chest = new MeshKit();
            chest.add_ball(new Vector3(0, -0.04f, 0.03f), new Vector3(0.145f, 0.17f, 0.2f), curls, 30, 0.024f, 0.05f);
            chest.add_ball(new Vector3(0, -0.09f, 0.16f), new Vector3(0.1f, 0.13f, 0.085f), cream, 22, 0.02f, 0.045f);
            part("front half", front, chest, coat);

            var neck_part = new MeshKit();
            neck_part.add_tube(new Vector3(0, -0.03f, -0.03f), new Vector3(0, 0.12f, 0.07f), 0.1f, 0.088f, curls, ginger, 22, 0.022f, 0.05f);
            part("neck", neck, neck_part, coat);
            // His red collar, a ring round the bottom of his neck, half hidden in his curls.
            var collar = new MeshKit();
            for (int link = 0; link < 14; link++)
            {
                float angle_now = link * Mathf.PI * 2 / 14, angle_next = (link + 1) * Mathf.PI * 2 / 14;
                var around_now = new Vector3(Mathf.Cos(angle_now) * 0.098f, 0.025f - Mathf.Sin(angle_now) * 0.012f, Mathf.Sin(angle_now) * 0.09f + 0.01f);
                var around_next = new Vector3(Mathf.Cos(angle_next) * 0.098f, 0.025f - Mathf.Sin(angle_next) * 0.012f, Mathf.Sin(angle_next) * 0.09f + 0.01f);
                collar.add_tube(around_now, around_next, 0.013f, 0.013f, collar_red, collar_red, 6);
            }
            // For Christmas: a little gold bell and a red bow at the front of his collar.
            collar.add_ball(new Vector3(0, -0.012f, 0.108f), Vector3.one * 0.02f, new Color(0.9f, 0.74f, 0.32f), 8);
            for (int side = -1; side <= 1; side += 2)
            {
                collar.add_ball(new Vector3(side * 0.028f, 0.018f, 0.104f), new Vector3(0.028f, 0.018f, 0.012f), new Color(0.82f, 0.08f, 0.1f), 6);
            }
            part("collar", neck, collar, shiny);

            // His head: ginger crown and topknot, cream muzzle and beard, black nose, dark eyes.
            var face = new MeshKit();
            face.add_ball(new Vector3(0, 0.025f, 0.01f), new Vector3(0.1f, 0.095f, 0.105f), ginger, 26, 0.02f, 0.045f);
            face.add_ball(new Vector3(0, 0.1f, -0.01f), new Vector3(0.085f, 0.06f, 0.085f), ginger, 20, 0.024f, 0.04f);
            face.add_ball(new Vector3(0, -0.005f, 0.115f), new Vector3(0.062f, 0.055f, 0.07f), cream, 20, 0.014f, 0.035f);
            face.add_ball(new Vector3(0, -0.045f, 0.105f), new Vector3(0.066f, 0.045f, 0.064f), cream, 18, 0.02f, 0.035f);
            face.add_ball(new Vector3(0, 0.012f, 0.182f), new Vector3(0.025f, 0.02f, 0.018f), nose_black, 8);
            for (int side = -1; side <= 1; side += 2)
            {
                face.add_ball(new Vector3(side * 0.047f, 0.045f, 0.085f), Vector3.one * 0.015f, eye_brown, 8);
                face.add_ball(new Vector3(side * 0.052f, 0.066f, 0.078f), new Vector3(0.03f, 0.016f, 0.018f), ginger, 8, 0.008f, 0.025f);
            }
            part("head", head, face, coat);
            var nose_shine = new MeshKit();
            nose_shine.add_ball(new Vector3(0, 0.016f, 0.19f), new Vector3(0.013f, 0.008f, 0.009f), nose_black, 4);
            part("nose", head, nose_shine, shiny);

            var lower_jaw = new MeshKit();
            lower_jaw.add_ball(new Vector3(0, -0.008f, 0.045f), new Vector3(0.04f, 0.018f, 0.058f), cream, 8, 0.006f, 0.02f);
            lower_jaw.add_ball(new Vector3(0, 0.004f, 0.06f), new Vector3(0.026f, 0.007f, 0.05f), tongue_pink, 8);
            part("jaw", jaw, lower_jaw, coat);

            left_ear = make_ear("left ear", -1, coat);
            right_ear = make_ear("right ear", 1, coat);

            // The tail: three curly pieces, each on its own bone so it can curl, wag and swing.
            var tail_parent = rear;
            var tail_start = new Vector3(0, 0.07f, -0.18f);
            for (int piece = 0; piece < tail.Length; piece++)
            {
                tail[piece] = Limb.bone("tail " + (piece + 1), tail_parent, piece == 0 ? tail_start : new Vector3(0, -0.1f, 0));
                var plume = new MeshKit();
                float thick = 0.045f - piece * 0.006f;
                plume.add_tube(Vector3.zero, new Vector3(0, -0.105f, 0), thick + 0.006f, thick * 0.9f, curls, piece == 2 ? Color.Lerp(curls, cream, 0.3f) : curls, 16, 0.02f, 0.04f);
                part("tail " + (piece + 1), tail[piece], plume, coat);
                tail_parent = tail[piece];
            }

            for (int leg = 0; leg < 4; leg++)
            {
                make_leg(leg, coat);
            }
            apply(current, 0);
        }

        // One long floppy curly ear, hanging from the side of his head.
        Transform make_ear(string name, int side, Material material)
        {
            var ear = Limb.bone(name, head, new Vector3(side * 0.088f, 0.06f, 0.0f));
            var kit = new MeshKit();
            kit.add_round_shape(new Vector3(0, -0.17f, 0), Quaternion.identity, new[]
            {
                new MeshKit.Ring(0, 0, ginger),
                new MeshKit.Ring(0.015f, 0.04f, ginger),
                new MeshKit.Ring(0.09f, 0.055f, ginger),
                new MeshKit.Ring(0.15f, 0.045f, ginger),
                new MeshKit.Ring(0.185f, 0, ginger),
            }, 16, new Vector3(0.5f, 1, 1), 0.016f, 0.035f);
            part(name, ear, kit, material);
            return ear;
        }

        // One leg: its bones, its curly pieces, a pale paw, and where its paw stands.
        void make_leg(int leg, Material material)
        {
            bool front_leg = leg < 2;
            int side = leg % 2 == 0 ? -1 : 1;
            string name = (front_leg ? "front " : "hind ") + (side < 0 ? "left" : "right");
            var joint = front_leg ? new Vector3(side * shoulder_joint.x, shoulder_joint.y, shoulder_joint.z) : new Vector3(side * hip_joint.x, hip_joint.y, hip_joint.z);
            float upper = front_leg ? upper_arm : thigh;
            float lower = front_leg ? forearm : gaskin;
            var (top, middle) = Limb.make_bones(name, front_leg ? front : rear, joint, upper);
            var foot = Limb.bone(name + " foot", middle, new Vector3(0, -lower, 0));
            float foot_length = front_leg ? front_paw_height : hind_foot;

            var upper_part = new MeshKit();
            upper_part.add_tube(Vector3.zero, new Vector3(0, -upper, 0), front_leg ? 0.068f : 0.085f, 0.058f, curls, curls, 16, 0.018f, 0.04f);
            part(name + " upper", top, upper_part, material);
            var lower_part = new MeshKit();
            lower_part.add_tube(Vector3.zero, new Vector3(0, -lower, 0), 0.058f, 0.05f, curls, Color.Lerp(curls, pale_paws, 0.4f), 16, 0.016f, 0.035f);
            part(name + " lower", middle, lower_part, material);
            var paw_part = new MeshKit();
            paw_part.add_tube(Vector3.zero, new Vector3(0, -foot_length + 0.02f, 0), 0.05f, 0.048f, pale_paws, pale_paws, 12, 0.012f, 0.03f);
            paw_part.add_ball(new Vector3(0, -foot_length + 0.02f, 0.025f), new Vector3(0.05f, 0.028f, 0.062f), pale_paws, 12, 0.012f, 0.025f);
            part(name + " paw", foot, paw_part, material);

            legs[leg] = new Limb(top, middle, upper, lower, !front_leg);
            feet[leg] = foot;
            var joint_in_root = rear_spot + (front_leg ? front_offset : Vector3.zero) + joint;
            paw_home[leg] = new Vector3(joint_in_root.x * 1.05f, 0, joint_in_root.z + (front_leg ? 0.02f : -0.02f));
        }

        // Adds one body part's mesh to a bone.
        static void part(string name, Transform bone, MeshKit kit, Material material)
        {
            MeshKit.show(name, kit.build(name), material, bone, true);
        }

        // Moves him on one frame to match `pose` from the choreography.
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
            facing_now = Mathf.SmoothDampAngle(facing_now, (float)pose.facing, ref facing_speed, 0.12f, 900f, Mathf.Max(delta, 0.0001f));
            speed_now = Mathf.Lerp(speed_now, (float)pose.speed, 1 - Mathf.Exp(-delta / 0.12f));
            phase = (float)Stride.advance(phase, moved, Stride.dog_stride_length(speed_now));
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0, facing_now, 0);

            var target = choose_pose(pose, (float)seconds);
            target.head_yaw += Mathf.Clamp(look_yaw, -75, 75) * 0.8f;
            bool moving = speed_now > 0.05f || pose.action == "leap";
            float body_ease = 1 - Mathf.Exp(-delta / 0.11f);
            float paw_ease = moving ? 1 : 1 - Mathf.Exp(-delta / 0.08f);
            var paws = new[] { current.front_left, current.front_right, current.hind_left, current.hind_right };
            var target_paws = new[] { target.front_left, target.front_right, target.hind_left, target.hind_right };
            current = DogPose.blend(current, target, delta <= 0 ? 1 : body_ease);
            current.front_left = DogPose.blend(paws[0], target_paws[0], delta <= 0 ? 1 : paw_ease);
            current.front_right = DogPose.blend(paws[1], target_paws[1], delta <= 0 ? 1 : paw_ease);
            current.hind_left = DogPose.blend(paws[2], target_paws[2], delta <= 0 ? 1 : paw_ease);
            current.hind_right = DogPose.blend(paws[3], target_paws[3], delta <= 0 ? 1 : paw_ease);
            wag_clock += delta * current.tail_wag_speed;
            apply(current, delta);
            model?.follow();
        }

        // Puts on his real 3D model (art/blender/build_bodhi.py, imported from game/Resources/Models):
        // the code-built pieces hide, and every frame the model's bones point the way this figure's
        // parts do (SkinnedModel). Without a model he simply stays as he is.
        public void wear(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }
            model = new SkinnedModel(prefab, root, WorldLook.solid(rim: 0.6f, wrap: 0.6f, grain: 0.04f));
            foreach (var piece in root.GetComponentsInChildren<MeshRenderer>())
            {
                piece.enabled = false;
            }
            model.face_forward(model.bone("head"), model.bone("back_half"));
            model.hips_follow(model.bone("back_half"), rear);
            // Each bone, the next bone in its chain (Blender adds "_end" to the tip of a chain),
            // and the two points on this figure it should point between.
            model.link(model.bone("back_half"), "front_half", rear, front);
            model.link(model.bone("front_half"), "neck", front, neck);
            model.link(model.bone("neck"), "head", neck, head);
            model.link(model.bone("head"), "head_end", head, mouth_point);
            model.link(model.bone("tail_1"), "tail_2", tail[0], tail[1]);
            model.link(model.bone("tail_2"), "tail_2_end", tail[1], tail[2]);
            foreach (int side in new[] { -1, 1 })
            {
                var ear = model.bone_on_side("ear", side);
                model.link(ear, ear != null ? ear.name + "_end" : "", side < 0 ? left_ear : right_ear, null, new Vector3(0, -0.17f, 0));
            }
            for (int leg = 0; leg < 4; leg++)
            {
                bool front_leg = leg < 2;
                int side = leg % 2 == 0 ? -1 : 1;
                string kind = front_leg ? "front_" : "hind_";
                float foot_length = front_leg ? front_paw_height : hind_foot;
                var upper = model.bone_on_side(kind + "upper", side);
                string ending = upper != null ? upper.name.Substring(upper.name.Length - 2) : "";  // ".L" or ".R"
                model.link(upper, kind + "lower" + ending, legs[leg].upper, legs[leg].lower);
                model.link(model.bone(kind + "lower" + ending), kind + "foot" + ending, legs[leg].lower, feet[leg]);
                model.link(model.bone(kind + "foot" + ending), kind + "foot" + ending + "_end", feet[leg], null, new Vector3(0, -foot_length, 0));
            }
            model.follow();
        }

        // The pose for what he's doing now.
        DogPose choose_pose(PlayPose pose, float seconds)
        {
            var moving = DogPose.moving(speed_now, phase);
            float share = (float)pose.action_share;
            switch (pose.action)
            {
                case "move": return DogPose.panting(moving, seconds, 1);
                case "carry": return DogPose.carrying(moving);
                case "sit": return DogPose.panting(DogPose.sitting(seconds), seconds, 1);
                case "bow": return DogPose.bowing(seconds);
                case "sniff": return DogPose.sniffing(seconds);
                case "pick": return DogPose.picking(share, seconds);
                case "lean": return DogPose.panting(DogPose.leaning(seconds), seconds, 0.7f);
                case "leap": return DogPose.leaping(share);
                default: return DogPose.panting(DogPose.standing(), seconds, 1);
            }
        }

        // Sets every bone from a pose.
        void apply(DogPose pose, float delta)
        {
            rear.localPosition = rear_spot + new Vector3(0, -pose.body_drop, pose.body_forward);
            rear.localRotation = Quaternion.Euler(pose.body_pitch - pose.spine_flex * 0.4f, 0, pose.body_roll);
            front.localRotation = Quaternion.Euler(pose.spine_flex, pose.head_yaw * 0.15f, 0);
            neck.localRotation = Quaternion.Euler(pose.neck_pitch - (pose.body_pitch + pose.spine_flex * 0.6f) * 0.6f, pose.head_yaw * 0.35f, 0);
            head.localRotation = Quaternion.Euler(pose.head_pitch, pose.head_yaw * 0.5f, pose.head_tilt);
            jaw.localRotation = Quaternion.Euler(Mathf.Max(0, pose.jaw_open), 0, 0);

            // Ears hang down whatever his head does, bounce when he lands and blow back at speed.
            float head_pitch = Vector3.SignedAngle(root.forward, Vector3.ProjectOnPlane(head.forward, root.right), root.right);
            if (delta > 0)
            {
                float vertical_speed = (root.localPosition.y - last_height) / delta;
                ear_bounce.update(vertical_speed * 25, 160, 7, delta);
                tail_bounce.update(-vertical_speed * 18, 120, 6, delta);
            }
            last_height = root.localPosition.y;
            float ear_hang = -head_pitch * 0.8f - pose.ear_perk + (float)ear_bounce.value;
            left_ear.localRotation = Quaternion.Euler(ear_hang, 0, -10 - pose.ear_perk * 0.2f);
            right_ear.localRotation = Quaternion.Euler(ear_hang, 0, 10 + pose.ear_perk * 0.2f);

            // The tail: up and back from his rump, each piece curling a little more, wagging.
            float wag = Mathf.Sin(wag_clock * 2 * Mathf.PI) * pose.tail_wag;
            tail[0].localRotation = Quaternion.Euler(0, wag, 0) * Quaternion.Euler(118 + pose.tail_lift + (float)tail_bounce.value, 0, 0);
            tail[1].localRotation = Quaternion.Euler(-18 - pose.tail_lift * 0.2f, wag * 0.4f, 0);
            tail[2].localRotation = Quaternion.Euler(-22 - pose.tail_lift * 0.2f, wag * 0.6f, 0);

            var paws = new[] { pose.front_left, pose.front_right, pose.hind_left, pose.hind_right };
            for (int leg = 0; leg < 4; leg++)
            {
                place_paw(leg, paws[leg], pose.hock_flat);
            }
        }

        // Puts one paw on its spot. A front leg reaches its wrist to just above the paw (the
        // elbow bends back) and the paw folds under as it swings. A hind leg reaches its hock (the
        // stifle bends forward), with the long hind foot sloping from hock to paw, flatter as it
        // swings, flat on the snow when he sits.
        void place_paw(int leg, PawPose paw, float hock_flat)
        {
            bool front_leg = leg < 2;
            var spot = paw_home[leg] + new Vector3(0, paw.lift, paw.forward);
            if (front_leg)
            {
                var wrist = spot + new Vector3(0, front_paw_height, -0.01f);
                legs[leg].reach(root, wrist);
                feet[leg].rotation = root.rotation * Quaternion.Euler(paw.fold * 80, 0, 0);
            }
            else
            {
                float slope = Mathf.Lerp(14 + paw.fold * 40, 84, hock_flat);
                float radians = slope * Mathf.Deg2Rad;
                var hock = spot + new Vector3(0, hind_paw_height + hind_foot * Mathf.Cos(radians), -hind_foot * Mathf.Sin(radians));
                legs[leg].reach(root, hock);
                feet[leg].rotation = root.rotation * Quaternion.Euler(-slope, 0, 0);
            }
        }
    }
}
