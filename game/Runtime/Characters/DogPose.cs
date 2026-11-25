// Purpose: one whole-body pose for Bodhi as a handful of numbers (how low and tipped his body is,
// how his back flexes, where his head and tail point, where each paw is), and his library of
// poses: walk, trot and gallop worked out from the stride maths, and his title-screen actions
// (sitting, the play bow, sniffing, picking up the stick, leaning into Em, leaping for a
// snowball). Every pose is the same set of numbers, so they blend smoothly into each other.
// Angles in degrees, distances in metres.
using UnityEngine;

namespace Bodhi.Game
{
    // Where one paw is, measured on the snow from the spot under its leg's top joint.
    public struct PawPose
    {
        public float forward;  // Ahead of that spot (negative: behind).
        public float lift;     // Off the snow.
        public float fold;     // 0 straight, 1 folded right up (a paw tucked under in a gallop).
    }

    public struct DogPose
    {
        public float body_drop, body_forward;   // Metres: lower than standing, forward.
        public float body_pitch, body_roll;     // Degrees: nose down (negative: nose up), leaning sideways.
        public float spine_flex;                // His front half tipping down from his back half: a rounded back.
        public float neck_pitch, head_pitch, head_yaw, head_tilt;  // Neck and head, lowered positive.
        public float jaw_open;                  // How far his mouth is open.
        public float tail_lift, tail_wag, tail_wag_speed;  // Raised, side-to-side swing, swings a second.
        public float ear_perk;                  // Ears lifted forward (negative: blown back).
        public float hock_flat;                 // 0 standing; 1 his back feet lying flat, as when sitting.
        public PawPose front_left, front_right, hind_left, hind_right;

        // Standing easy, tail up and wagging, mouth a little open.
        public static DogPose standing()
        {
            return new DogPose
            {
                neck_pitch = -4,
                jaw_open = 8,
                tail_lift = 18,
                tail_wag = 22,
                tail_wag_speed = 3.2f,
                front_right = new PawPose { forward = 0.03f },
                hind_left = new PawPose { forward = -0.02f },
            };
        }

        // `share` of the way from pose `a` to pose `b`.
        public static DogPose blend(DogPose a, DogPose b, float share)
        {
            return new DogPose
            {
                body_drop = Mathf.Lerp(a.body_drop, b.body_drop, share),
                body_forward = Mathf.Lerp(a.body_forward, b.body_forward, share),
                body_pitch = Mathf.Lerp(a.body_pitch, b.body_pitch, share),
                body_roll = Mathf.Lerp(a.body_roll, b.body_roll, share),
                spine_flex = Mathf.Lerp(a.spine_flex, b.spine_flex, share),
                neck_pitch = Mathf.Lerp(a.neck_pitch, b.neck_pitch, share),
                head_pitch = Mathf.Lerp(a.head_pitch, b.head_pitch, share),
                head_yaw = Mathf.Lerp(a.head_yaw, b.head_yaw, share),
                head_tilt = Mathf.Lerp(a.head_tilt, b.head_tilt, share),
                jaw_open = Mathf.Lerp(a.jaw_open, b.jaw_open, share),
                tail_lift = Mathf.Lerp(a.tail_lift, b.tail_lift, share),
                tail_wag = Mathf.Lerp(a.tail_wag, b.tail_wag, share),
                tail_wag_speed = Mathf.Lerp(a.tail_wag_speed, b.tail_wag_speed, share),
                ear_perk = Mathf.Lerp(a.ear_perk, b.ear_perk, share),
                hock_flat = Mathf.Lerp(a.hock_flat, b.hock_flat, share),
                front_left = blend(a.front_left, b.front_left, share),
                front_right = blend(a.front_right, b.front_right, share),
                hind_left = blend(a.hind_left, b.hind_left, share),
                hind_right = blend(a.hind_right, b.hind_right, share),
            };
        }

        // `share` of the way between two paw poses.
        public static PawPose blend(PawPose a, PawPose b, float share)
        {
            return new PawPose
            {
                forward = Mathf.Lerp(a.forward, b.forward, share),
                lift = Mathf.Lerp(a.lift, b.lift, share),
                fold = Mathf.Lerp(a.fold, b.fold, share),
            };
        }

        // Which gait he uses at `speed`, blending as he changes: walk, then trot, then gallop.
        public static Gait gait_at(float speed)
        {
            if (speed < 1.6f)
            {
                return Stride.blend(Stride.dog_walk, Stride.dog_trot, Mathf.InverseLerp(1.1f, 1.6f, speed));
            }
            return Stride.blend(Stride.dog_trot, Stride.dog_gallop, Mathf.InverseLerp(3.3f, 4.3f, speed));
        }

        // Moving at `speed` metres a second, `phase` of the way through a stride. Walking and
        // trotting his body bobs gently over the planted legs; galloping, his back gathers up
        // (rounds) as his back legs swing under him and stretches out as his front legs reach,
        // he rises during the moment in the air, his tail streams out behind and his ears blow back.
        public static DogPose moving(float speed, float phase)
        {
            var pose = standing();
            var gait = gait_at(speed);
            float going = Mathf.Clamp01(speed / 0.4f);
            float gallop = Mathf.InverseLerp(3.3f, 4.3f, speed);
            float trot = Mathf.InverseLerp(1.1f, 1.6f, speed) * (1 - gallop);
            float stride = (float)Stride.dog_stride_length(speed) * going;
            float lift = (float)gait.lift * going;
            pose.front_left = paw(phase + (float)gait.leg_offsets[0], gait, stride, lift, true);
            pose.front_right = paw(phase + (float)gait.leg_offsets[1], gait, stride, lift, true);
            pose.hind_left = paw(phase + (float)gait.leg_offsets[2], gait, stride, lift, false);
            pose.hind_right = paw(phase + (float)gait.leg_offsets[3], gait, stride, lift, false);

            float turn = 2 * Mathf.PI;
            float walk_bob = 0.008f * Mathf.Cos(2 * turn * phase);
            float trot_bob = 0.018f * Mathf.Cos(2 * turn * (phase - 0.22f));
            float flight = Mathf.Exp(-Mathf.Pow(Mathf.DeltaAngle(phase * 360, 0.93f * 360) / 50f, 2));
            float gallop_bob = 0.015f - 0.055f * flight;
            pose.body_drop = Mathf.Lerp(Mathf.Lerp(walk_bob, trot_bob, trot), gallop_bob, gallop) * going;
            pose.spine_flex = 15 * Mathf.Cos(turn * (phase - 0.92f)) * gallop;
            pose.body_pitch = 5 * Mathf.Sin(turn * (phase - 0.15f)) * gallop;
            pose.neck_pitch = Mathf.Lerp(-4 + 3 * Mathf.Cos(2 * turn * phase) * trot, 10 + 7 * Mathf.Cos(turn * (phase - 0.42f)), gallop);
            pose.head_pitch = Mathf.Lerp(0, -6, gallop);
            pose.tail_lift = Mathf.Lerp(16, -38, gallop);
            pose.tail_wag = Mathf.Lerp(20, 6, gallop);
            pose.ear_perk = -30 * gallop - 8 * trot;
            pose.jaw_open = Mathf.Lerp(10, 18, gallop + trot * 0.5f);
            return pose;
        }

        // One paw during a stride: planted, it slides back under him at his speed (so it stays
        // put on the snow); swinging, it lifts and folds up, then reaches forward to land.
        static PawPose paw(float phase, Gait gait, float stride, float lift, bool front)
        {
            var step = Stride.foot_at(phase, gait.duty, stride, lift);
            float fold = step.planted ? 0 : Mathf.Sin((float)step.swing_share * Mathf.PI) * (front ? 1f : 0.8f);
            return new PawPose { forward = (float)step.forward, lift = (float)step.lift, fold = fold * Mathf.Clamp01(stride / 0.5f) };
        }

        // Sitting: back end down on the snow, back feet flat, front legs straight, chest up.
        public static DogPose sitting(float seconds)
        {
            var pose = standing();
            pose.body_drop = 0.2f;
            pose.body_pitch = -30;
            pose.body_forward = -0.05f;
            pose.spine_flex = 4;
            pose.neck_pitch = -2;
            pose.hock_flat = 1;
            pose.hind_left = new PawPose { forward = 0.14f };
            pose.hind_right = new PawPose { forward = 0.14f };
            pose.front_left = new PawPose { forward = -0.1f };
            pose.front_right = new PawPose { forward = -0.08f };
            pose.tail_lift = -12;    // Lying out behind him on the snow, sweeping it as he wags.
            pose.tail_wag = 25;
            pose.tail_wag_speed = 2.6f;
            pose.head_tilt = 12 * Mathf.Sin(seconds * 0.7f) * Mathf.Clamp01(Mathf.Sin(seconds * 0.31f) * 3);  // The head tilt.
            return pose;
        }

        // The play bow: front end down on his elbows, back end up, tail high and wagging hard,
        // bouncing a little: "play with me".
        public static DogPose bowing(float seconds)
        {
            var pose = standing();
            pose.body_pitch = 20;
            pose.body_drop = 0.05f + 0.012f * Mathf.Sin(seconds * 9f);
            pose.body_forward = -0.04f;
            pose.neck_pitch = -32;
            pose.head_pitch = -8;
            pose.front_left = new PawPose { forward = 0.2f };
            pose.front_right = new PawPose { forward = 0.18f };
            pose.hind_left = new PawPose { forward = 0.02f };
            pose.hind_right = new PawPose { forward = -0.02f };
            pose.tail_lift = 40;
            pose.tail_wag = 38;
            pose.tail_wag_speed = 7f;
            pose.ear_perk = 12;
            pose.jaw_open = 16;
            return pose;
        }

        // Nose down to the snow, sniffing, tail swinging slowly.
        public static DogPose sniffing(float seconds)
        {
            var pose = standing();
            pose.body_pitch = 6;
            pose.neck_pitch = 58 + 4 * Mathf.Sin(seconds * 7f);
            pose.head_pitch = 22;
            pose.jaw_open = 2;
            pose.tail_lift = 8;
            pose.tail_wag = 14;
            pose.tail_wag_speed = 3.5f;
            return pose;
        }

        // Picking the stick up: nose down, mouth opens, closes on it, head comes up.
        public static DogPose picking(float share, float seconds)
        {
            var pose = sniffing(seconds);
            pose.jaw_open = share < 0.55f ? 22 * Mathf.Sin(Mathf.Clamp01(share / 0.55f) * Mathf.PI) : 6;
            pose.neck_pitch = Mathf.Lerp(62, 0, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.55f, 1f, share)));
            return pose;
        }

        // Carrying the stick proudly: head high, tail up and wagging, mouth closed on the stick.
        public static DogPose carrying(DogPose moving)
        {
            var pose = moving;
            pose.neck_pitch -= 14;
            pose.tail_lift = Mathf.Max(pose.tail_lift, 30);
            pose.tail_wag = 26;
            pose.tail_wag_speed = 5;
            pose.jaw_open = 6;
            return pose;
        }

        // Leaning into Em while she pats or hugs him: head pressed against her, eyes half shut,
        // tail going like mad.
        public static DogPose leaning(float seconds)
        {
            var pose = standing();
            pose.body_roll = 5;
            pose.neck_pitch = -16;
            pose.head_tilt = 16 + 3 * Mathf.Sin(seconds * 2.1f);
            pose.tail_lift = 25;
            pose.tail_wag = 42;
            pose.tail_wag_speed = 8f;
            pose.jaw_open = 14;
            pose.ear_perk = -6;
            return pose;
        }

        // A leap for the snowball, `share` of the way through: nose up and back legs pushing off,
        // front legs tucked; stretched out at the top, mouth open for the catch; then front legs
        // reaching down to land, nose down.
        public static DogPose leaping(float share)
        {
            var pose = standing();
            float rise = Mathf.InverseLerp(0f, 0.35f, share);
            float fall = Mathf.InverseLerp(0.6f, 1f, share);
            pose.body_pitch = Mathf.Lerp(Mathf.Lerp(-24, -6, rise), 18, fall);
            pose.spine_flex = Mathf.Lerp(-10, 8, fall);
            pose.neck_pitch = Mathf.Lerp(-30, 0, fall);
            pose.jaw_open = 30 * Mathf.Exp(-Mathf.Pow((share - 0.5f) / 0.15f, 2)) + 6;
            var tucked = new PawPose { forward = 0.06f, lift = 0.16f, fold = 1 };
            var reaching = new PawPose { forward = 0.16f, lift = 0.02f, fold = 0.1f };
            var pushing = new PawPose { forward = -0.22f, lift = 0.02f, fold = 0.2f };
            var gathered = new PawPose { forward = 0.1f, lift = 0.14f, fold = 0.8f };
            pose.front_left = paw_through(tucked, reaching, rise, fall);
            pose.front_right = paw_through(tucked, reaching, rise, fall);
            pose.hind_left = paw_through(pushing, gathered, rise, fall);
            pose.hind_right = paw_through(pushing, gathered, rise, fall);
            pose.tail_lift = -30;
            pose.tail_wag = 4;
            pose.ear_perk = -20;
            return pose;
        }

        // A paw going from `start` to `middle` while `rise` goes from 0 to 1, then easing back
        // toward a landing (half way between) while `fall` goes from 0 to 1.
        static PawPose paw_through(PawPose start, PawPose middle, float rise, float fall)
        {
            var up = blend(start, middle, Mathf.SmoothStep(0, 1, rise));
            var landing = new PawPose { forward = middle.forward * 0.6f, lift = 0, fold = 0 };
            return blend(up, landing, Mathf.SmoothStep(0, 1, fall));
        }

        // Panting: his mouth opens and closes quickly, and his chest with it.
        public static DogPose panting(DogPose pose, float seconds, float strength)
        {
            pose.jaw_open += Mathf.Sin(seconds * 2 * Mathf.PI * 2.8f) * 4 * strength;
            pose.body_drop += Mathf.Sin(seconds * 2 * Mathf.PI * 2.8f) * 0.002f * strength;
            return pose;
        }
    }
}
