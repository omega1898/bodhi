// Purpose: one whole-body pose for Em, as a handful of numbers (how low her hips are, how far her
// back bends, where each arm points, where each foot is), and her library of poses: walking and
// running worked out from the stride maths, and every action on the title screen (crouching to
// pat snow together, pushing a snowball, lifting it, placing it, throwing, patting Bodhi, hands
// on hips, hugging, looking up at the sky). Poses blend smoothly into one another because every
// pose is the same set of numbers. Angles are in degrees; distances in metres.
using UnityEngine;

namespace Bodhi.Game
{
    // Where one arm points.
    public struct ArmPose
    {
        public float forward;   // Swung forward from hanging down (negative: back).
        public float outward;   // Lifted out to the side, away from her body.
        public float twist;     // Turned about its own length, so the elbow bends inward.
        public float elbow;     // How much the elbow bends.
    }

    // Where one foot is, measured on the snow from the spot under its hip.
    public struct FootPose
    {
        public float side;      // Out to the side (positive: away from her middle).
        public float forward;   // Ahead of the hip (negative: behind).
        public float lift;      // Off the snow.
        public float toe_up;    // Toes tilted up (negative: heel up, toes pushing off).
    }

    public struct HumanPose
    {
        public float hips_drop, hips_forward, hips_side;   // Metres: lower, forward, sideways.
        public float hips_pitch, hips_yaw, hips_roll;      // Degrees: leaning forward, turning, tilting.
        public float spine_pitch, chest_pitch, chest_yaw, chest_roll;
        public float neck_pitch, head_pitch, head_yaw, head_roll;
        public ArmPose left_arm, right_arm;
        public FootPose left_foot, right_foot;

        // Standing easy: feet under the hips, arms hanging a little away from her sides.
        public static HumanPose standing()
        {
            return new HumanPose
            {
                hips_drop = 0.01f,
                left_arm = new ArmPose { forward = 4, outward = 7, elbow = 12, twist = 15 },
                right_arm = new ArmPose { forward = 4, outward = 7, elbow = 12, twist = 15 },
                left_foot = new FootPose { side = 0.015f },
                right_foot = new FootPose { side = 0.015f, forward = 0.03f },
            };
        }

        // `share` of the way from pose `a` to pose `b`, every number in step.
        public static HumanPose blend(HumanPose a, HumanPose b, float share)
        {
            return new HumanPose
            {
                hips_drop = Mathf.Lerp(a.hips_drop, b.hips_drop, share),
                hips_forward = Mathf.Lerp(a.hips_forward, b.hips_forward, share),
                hips_side = Mathf.Lerp(a.hips_side, b.hips_side, share),
                hips_pitch = Mathf.Lerp(a.hips_pitch, b.hips_pitch, share),
                hips_yaw = Mathf.Lerp(a.hips_yaw, b.hips_yaw, share),
                hips_roll = Mathf.Lerp(a.hips_roll, b.hips_roll, share),
                spine_pitch = Mathf.Lerp(a.spine_pitch, b.spine_pitch, share),
                chest_pitch = Mathf.Lerp(a.chest_pitch, b.chest_pitch, share),
                chest_yaw = Mathf.Lerp(a.chest_yaw, b.chest_yaw, share),
                chest_roll = Mathf.Lerp(a.chest_roll, b.chest_roll, share),
                neck_pitch = Mathf.Lerp(a.neck_pitch, b.neck_pitch, share),
                head_pitch = Mathf.Lerp(a.head_pitch, b.head_pitch, share),
                head_yaw = Mathf.Lerp(a.head_yaw, b.head_yaw, share),
                head_roll = Mathf.Lerp(a.head_roll, b.head_roll, share),
                left_arm = blend(a.left_arm, b.left_arm, share),
                right_arm = blend(a.right_arm, b.right_arm, share),
                left_foot = blend(a.left_foot, b.left_foot, share),
                right_foot = blend(a.right_foot, b.right_foot, share),
            };
        }

        // `share` of the way between two arm poses.
        static ArmPose blend(ArmPose a, ArmPose b, float share)
        {
            return new ArmPose
            {
                forward = Mathf.Lerp(a.forward, b.forward, share),
                outward = Mathf.Lerp(a.outward, b.outward, share),
                twist = Mathf.Lerp(a.twist, b.twist, share),
                elbow = Mathf.Lerp(a.elbow, b.elbow, share),
            };
        }

        // `share` of the way between two foot poses.
        public static FootPose blend(FootPose a, FootPose b, float share)
        {
            return new FootPose
            {
                side = Mathf.Lerp(a.side, b.side, share),
                forward = Mathf.Lerp(a.forward, b.forward, share),
                lift = Mathf.Lerp(a.lift, b.lift, share),
                toe_up = Mathf.Lerp(a.toe_up, b.toe_up, share),
            };
        }

        // Walking or running at `speed` metres a second, `phase` of the way through a stride.
        // Everything follows the legs: the hips rise over each planted foot when walking (and sink
        // into it when running), sway toward it, and turn with the swinging leg; the shoulders
        // turn the other way; the arms swing opposite the legs; she leans forward more as she
        // speeds up; and her head stays level, as people's heads do.
        public static HumanPose moving(float speed, float phase)
        {
            var pose = standing();
            float run = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.7f, 2.6f, speed));
            float going = Mathf.Clamp01(speed / 0.5f);  // From standing to a full stride.
            var gait = Stride.blend(Stride.human_walk, Stride.human_run, run);
            float stride = (float)Stride.human_stride_length(speed);
            pose.left_foot = foot(phase + (float)gait.leg_offsets[0], gait, stride, going);
            pose.right_foot = foot(phase + (float)gait.leg_offsets[1], gait, stride, going);

            float turn = 2 * Mathf.PI;
            float walk_bob = -0.022f * Mathf.Cos(2 * turn * (phase - 0.31f));
            float run_bob = 0.05f + 0.035f * Mathf.Cos(2 * turn * (phase - 0.18f));
            pose.hips_drop = Mathf.Lerp(0.01f, Mathf.Lerp(0.012f + walk_bob, run_bob, run), going);
            pose.hips_side = -Mathf.Lerp(0.022f, 0.01f, run) * Mathf.Cos(turn * (phase - 0.31f)) * going;
            pose.hips_yaw = -Mathf.Lerp(5, 8, run) * Mathf.Cos(turn * (phase - 0.5f)) * going;
            pose.hips_roll = Mathf.Lerp(3, 2, run) * Mathf.Sin(turn * (phase - 0.31f)) * going;
            pose.chest_yaw = -pose.hips_yaw * 1.4f;
            pose.spine_pitch = Mathf.Lerp(2, 8, run) * going;
            pose.chest_pitch = Mathf.Lerp(1, 4, run) * going;
            pose.head_pitch = -(pose.spine_pitch + pose.chest_pitch) * 0.7f;

            float swing = Mathf.Lerp(17, 40, run) * going;
            float left_swing = -Mathf.Cos(turn * phase);
            pose.left_arm = new ArmPose { forward = 4 + swing * left_swing, outward = Mathf.Lerp(7, 12, run), twist = 15, elbow = Mathf.Lerp(14 + 10 * Mathf.Max(0, left_swing), 88 + 8 * left_swing, run) };
            pose.right_arm = new ArmPose { forward = 4 - swing * left_swing, outward = Mathf.Lerp(7, 12, run), twist = 15, elbow = Mathf.Lerp(14 + 10 * Mathf.Max(0, -left_swing), 88 - 8 * left_swing, run) };
            return pose;
        }

        // One foot during a stride: planted feet roll from heel to toe; swinging feet lift and
        // reach forward, toes up, ready to land on the heel.
        static FootPose foot(float phase, Gait gait, float stride, float going)
        {
            var step = Stride.foot_at(phase, gait.duty, stride * going, gait.lift * going);
            float toe_up;
            if (step.planted)
            {
                float share = stance_share(phase, (float)gait.duty);
                toe_up = share < 0.15f ? 14 * (1 - share / 0.15f) : (share > 0.6f ? -32 * (share - 0.6f) / 0.4f : 0);
            }
            else
            {
                float swing = (float)step.swing_share;
                toe_up = Mathf.Lerp(-32, 14, Mathf.SmoothStep(0, 1, swing));
            }
            return new FootPose { side = 0.01f, forward = (float)step.forward, lift = (float)step.lift, toe_up = toe_up * going };
        }

        // How far through its stance a planted foot is, 0 to 1.
        static float stance_share(float phase, float duty)
        {
            float wrapped = (float)Stride.wrap(phase);
            return Mathf.Clamp01(wrapped / duty);
        }

        // Crouching low with both feet planted, knees forward, reaching down in front: patting
        // snow together, scooping, taking the stick. `pat` moves her hands as if pressing snow.
        public static HumanPose crouching(float seconds, bool pat)
        {
            var pose = standing();
            pose.hips_drop = 0.43f;
            pose.hips_forward = -0.12f;
            pose.hips_pitch = 22;
            pose.spine_pitch = 22;
            pose.chest_pitch = 12;
            pose.head_pitch = -12;
            float press = pat ? Mathf.Sin(seconds * 11f) * 9 : 0;
            pose.left_arm = new ArmPose { forward = 62 + press, outward = 14, twist = 40, elbow = 38 };
            pose.right_arm = new ArmPose { forward = 62 - press, outward = 14, twist = 40, elbow = 38 };
            pose.left_foot = new FootPose { side = 0.06f, forward = 0.08f };
            pose.right_foot = new FootPose { side = 0.06f, forward = -0.04f };
            return pose;
        }

        // Pushing a snowball along: bent over it, both hands flat on it. The bigger the ball,
        // the higher her hands. The legs come from walking.
        public static HumanPose pushing(HumanPose walking, float ball_radius)
        {
            var pose = walking;
            float low = Mathf.InverseLerp(0.36f, 0.08f, ball_radius);  // 1 for a small ball.
            pose.hips_pitch = 12 + 10 * low;
            pose.spine_pitch = 18 + 10 * low;
            pose.chest_pitch = 10 + 6 * low;
            pose.hips_drop += 0.04f + 0.1f * low;
            pose.head_pitch = -18;
            pose.left_arm = new ArmPose { forward = 52 + 8 * low, outward = 18, twist = 45, elbow = 18 };
            pose.right_arm = new ArmPose { forward = 52 + 8 * low, outward = 18, twist = 45, elbow = 18 };
            return pose;
        }

        // Holding a big snowball against her chest, leaning back a little under its weight.
        public static HumanPose holding(HumanPose legs)
        {
            var pose = legs;
            pose.spine_pitch = -4;
            pose.chest_pitch = -3;
            pose.head_pitch = 6;
            pose.left_arm = new ArmPose { forward = 48, outward = 26, twist = 70, elbow = 72 };
            pose.right_arm = new ArmPose { forward = 48, outward = 26, twist = 70, elbow = 72 };
            return pose;
        }

        // Rising from a crouch with the ball in her arms, `share` of the way up.
        public static HumanPose lifting(float share, float seconds)
        {
            float eased = Mathf.SmoothStep(0, 1, share);
            var low = crouching(seconds, false);
            low.left_arm = new ArmPose { forward = 55, outward = 26, twist = 70, elbow = 55 };
            low.right_arm = low.left_arm;
            return blend(low, holding(standing()), eased);
        }

        // Reaching up and forward to set something on the snowman: a ball, its face, its arms.
        // `high` reaches up to its head. The reach goes out and comes back over the action.
        public static HumanPose placing(float share, bool high)
        {
            var pose = standing();
            float reach = Mathf.Sin(Mathf.Clamp01(share) * Mathf.PI);
            pose.spine_pitch = 8 * reach;
            pose.hips_drop = -0.02f * reach * (high ? 2 : 1);
            pose.head_pitch = high ? -18 * reach : 4;
            float lift = high ? 112 : 82;
            var arm = new ArmPose { forward = Mathf.Lerp(48, lift, reach), outward = 18, twist = 55, elbow = Mathf.Lerp(72, 24, reach) };
            pose.left_arm = arm;
            pose.right_arm = arm;
            pose.left_foot = new FootPose { side = 0.02f, forward = 0.08f, toe_up = high ? -18 * reach : 0, lift = 0 };
            return pose;
        }

        // Packing the snowman's head between her hands, standing: palms pressing in turn.
        public static HumanPose packing(float seconds)
        {
            var pose = standing();
            pose.head_pitch = 14;
            pose.spine_pitch = 4;
            float press = Mathf.Sin(seconds * 9f) * 10;
            pose.left_arm = new ArmPose { forward = 42 + press, outward = 22, twist = 75, elbow = 92 };
            pose.right_arm = new ArmPose { forward = 42 - press, outward = 22, twist = 75, elbow = 92 };
            return pose;
        }

        // Throwing a snowball overarm with her right hand: weight back and arm cocked behind her
        // head, then a step onto her left foot as the arm whips over, then the follow-through.
        public static HumanPose throwing(float share)
        {
            var pose = standing();
            float wind = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0f, 0.45f, share));
            float whip = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.42f, 0.6f, share));
            float settle = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.7f, 1f, share));
            pose.chest_yaw = Mathf.Lerp(Mathf.Lerp(0, 32, wind), -28, whip) * (1 - settle);
            pose.hips_yaw = pose.chest_yaw * 0.4f;
            pose.spine_pitch = Mathf.Lerp(Mathf.Lerp(0, -8, wind), 16, whip) * (1 - settle);
            pose.hips_forward = Mathf.Lerp(-0.05f * wind, 0.08f, whip) * (1 - settle);
            pose.right_arm = new ArmPose
            {
                forward = Mathf.Lerp(Mathf.Lerp(4, 150, wind), 55, whip),
                outward = Mathf.Lerp(Mathf.Lerp(7, 35, wind), 12, whip),
                twist = 20,
                elbow = Mathf.Lerp(Mathf.Lerp(12, 95, wind), 15, whip),
            };
            pose.right_arm = blend(pose.right_arm, standing().right_arm, settle);
            pose.left_arm = new ArmPose { forward = Mathf.Lerp(Mathf.Lerp(4, 60, wind), 10, whip), outward = 20, twist = 20, elbow = 30 };
            pose.left_foot = new FootPose { side = 0.03f, forward = Mathf.Lerp(0, 0.3f, whip) * (1 - settle * 0.7f), lift = 0.06f * Mathf.Sin(Mathf.Clamp01((share - 0.3f) / 0.3f) * Mathf.PI) };
            pose.right_foot = new FootPose { side = 0.02f, forward = -0.08f, toe_up = -20 * whip * (1 - settle) };
            return pose;
        }

        // Crouched beside Bodhi, her right hand patting and stroking his head and back.
        public static HumanPose patting(float seconds)
        {
            var pose = crouching(seconds, false);
            pose.spine_pitch = 14;
            pose.head_pitch = -6;
            pose.head_roll = 8;
            float stroke = Mathf.Sin(seconds * 6.5f);
            pose.right_arm = new ArmPose { forward = 70 + stroke * 8, outward = 10, twist = 30, elbow = 18 + stroke * 6 };
            pose.left_arm = new ArmPose { forward = 30, outward = 18, twist = 50, elbow = 70 };
            return pose;
        }

        // Standing back to admire the snowman, hands on her hips, weight on one leg, head tilted.
        public static HumanPose admiring(float seconds)
        {
            var pose = standing();
            pose.hips_side = 0.035f;
            pose.hips_roll = -4;
            pose.chest_roll = 3;
            pose.head_roll = -9 + Mathf.Sin(seconds * 0.8f) * 2;
            var hand_on_hip = new ArmPose { forward = -12, outward = 38, twist = 85, elbow = 105 };
            pose.left_arm = hand_on_hip;
            pose.right_arm = hand_on_hip;
            pose.right_foot = new FootPose { side = 0.06f, forward = 0.1f, toe_up = 6 };
            return pose;
        }

        // Crouched low with her arms wide open, calling him in.
        public static HumanPose arms_open(float seconds)
        {
            var pose = crouching(seconds, false);
            pose.spine_pitch = 6;
            pose.chest_pitch = -4;
            pose.head_pitch = 4;
            var open = new ArmPose { forward = 48, outward = 52, twist = 10, elbow = 22 };
            pose.left_arm = open;
            pose.right_arm = open;
            return pose;
        }

        // Crouched, hugging him: arms wrapped round, cheek tilted against his head.
        public static HumanPose hugging(float seconds)
        {
            var pose = crouching(seconds, false);
            pose.spine_pitch = 16;
            pose.chest_pitch = 8;
            pose.head_pitch = 10;
            pose.head_roll = 14;
            float squeeze = Mathf.Sin(seconds * 1.6f) * 4;
            var wrap = new ArmPose { forward = 64 + squeeze, outward = 30, twist = 80, elbow = 78 };
            pose.left_arm = wrap;
            pose.right_arm = wrap;
            return pose;
        }

        // Standing still, looking up at the aurora, breathing slowly, hands clasped behind her.
        public static HumanPose looking_up(float seconds)
        {
            var pose = standing();
            pose.chest_pitch = -7;
            pose.neck_pitch = -12;
            pose.head_pitch = -24 + Mathf.Sin(seconds * 0.5f) * 3;
            var behind = new ArmPose { forward = -16, outward = 10, twist = -40, elbow = 40 };
            pose.left_arm = behind;
            pose.right_arm = behind;
            return pose;
        }

        // Adds breathing (a slow rise and fall of the chest and shoulders) and a gentle weight
        // shift, so she's never frozen when she stands still.
        public static HumanPose breathing(HumanPose pose, float seconds, float stillness)
        {
            float breath = Mathf.Sin(seconds * 1.7f);
            pose.chest_pitch += breath * 1.4f * stillness;
            pose.neck_pitch -= breath * 0.8f * stillness;
            pose.left_arm.outward += breath * 1.2f * stillness;
            pose.right_arm.outward += breath * 1.2f * stillness;
            pose.hips_side += Mathf.Sin(seconds * 0.45f) * 0.008f * stillness;
            return pose;
        }
    }
}
