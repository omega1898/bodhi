// Purpose: the timing of legs in a walk, trot or gallop, for both the person and the dog. Every
// leg goes round a cycle: planted on the ground (the "stance"), then lifted and swung forward
// (the "swing"). How the legs take turns is the gait: a person's legs are half a cycle apart; a
// trotting dog moves diagonal pairs together; a galloping dog's four feet land one after another
// with a moment in the air. These patterns come from studies of how dogs and people really move.
//
// The key to feet that never slide: the cycle moves on by distance travelled, not by time. A
// planted foot moves backward under the body at exactly the body's speed, so in the world it
// stays still. Plain C#, no Unity: tested with dotnet.
using System;

namespace Bodhi.Game
{
    // Where one foot is in its cycle.
    public struct FootStep
    {
        public double forward;      // Metres ahead of the spot under its hip (negative: behind).
        public double lift;         // Metres off the ground.
        public bool planted;        // True while it's on the ground bearing weight.
        public double swing_share;  // While swinging: 0 just lifted, 1 about to land.
    }

    // A gait: how long each foot stays down and when each one's cycle starts.
    public struct Gait
    {
        public string name;
        public double duty;               // Share of the cycle each foot is planted.
        public double[] leg_offsets;      // When each leg's cycle starts, 0 to 1.
        public double lift;               // How high a swinging foot rises, metres.
    }

    public static class Stride
    {
        // A person: left and right legs half a cycle apart. Walking, each foot is down 62% of
        // the time (so both are down at once for a moment); running, only 38% (so there's a
        // moment with both off the ground).
        public static readonly Gait human_walk = new Gait { name = "walk", duty = 0.62, leg_offsets = new[] { 0.0, 0.5 }, lift = 0.07 };
        public static readonly Gait human_run = new Gait { name = "run", duty = 0.36, leg_offsets = new[] { 0.0, 0.5 }, lift = 0.16 };

        // A dog's four legs in this order: front left, front right, hind left, hind right.
        // Walk: one foot at a time, hind left, front left, hind right, front right.
        // Trot: diagonal pairs together (front left with hind right).
        // Rotary gallop: hind left, hind right, front right, front left, then a moment in the
        // air with all four gathered under the body.
        public static readonly Gait dog_walk = new Gait { name = "walk", duty = 0.64, leg_offsets = new[] { 0.25, 0.75, 0.0, 0.5 }, lift = 0.05 };
        public static readonly Gait dog_trot = new Gait { name = "trot", duty = 0.44, leg_offsets = new[] { 0.0, 0.5, 0.5, 0.0 }, lift = 0.08 };
        public static readonly Gait dog_gallop = new Gait { name = "gallop", duty = 0.3, leg_offsets = new[] { 0.58, 0.46, 0.0, 0.1 }, lift = 0.13 };

        // How far one whole cycle carries a person at `speed` metres a second. Longer strides
        // and quicker steps both come with speed, as in real walking and running.
        public static double human_stride_length(double speed)
        {
            return 0.8 + 0.45 * Clamp(speed, 0, 6);
        }

        // How far one whole cycle carries the dog at `speed` metres a second.
        public static double dog_stride_length(double speed)
        {
            return 0.42 + 0.28 * Clamp(speed, 0, 9);
        }

        // Where a foot is at `phase` of its cycle (0 to 1, wrapping round). `duty` is the share
        // of the cycle it's planted; `stride_length` how far the body goes in one cycle; `lift`
        // how high it rises while swinging.
        public static FootStep foot_at(double phase, double duty, double stride_length, double lift)
        {
            phase = wrap(phase);
            duty = Clamp(duty, 0.05, 0.95);
            stride_length = Math.Max(0, double.IsNaN(stride_length) ? 0 : stride_length);
            double reach = stride_length * duty;  // How far the body moves while this foot is down.
            if (phase < duty)
            {
                double share = phase / duty;
                return new FootStep { forward = reach * (0.5 - share), lift = 0, planted = true };
            }
            double swing = (phase - duty) / (1 - duty);
            double carried = swing * swing * (3 - 2 * swing);   // Eases off the ground and down again.
            double arc = Math.Sin(Math.PI * Math.Pow(swing, 0.8));  // Highest a little before halfway.
            return new FootStep
            {
                forward = reach * (carried - 0.5),
                lift = Math.Max(0, lift) * arc,
                planted = false,
                swing_share = swing,
            };
        }

        // Moves a gait cycle on by `distance` metres at a stride of `stride_length`.
        public static double advance(double phase, double distance, double stride_length)
        {
            if (!(stride_length > 0) || double.IsNaN(distance) || double.IsInfinity(distance))
            {
                return wrap(phase);
            }
            return wrap(phase + distance / stride_length);
        }

        // Blends two gaits, `share` of the way from `from` to `to`. Used while the dog changes
        // from a trot to a gallop, so the switch is gradual instead of a snap.
        public static Gait blend(Gait from, Gait to, double share)
        {
            share = Clamp(share, 0, 1);
            var offsets = new double[from.leg_offsets.Length];
            for (int leg = 0; leg < offsets.Length; leg++)
            {
                // The shortest way round the cycle from one offset to the other.
                double gap = wrap(to.leg_offsets[leg] - from.leg_offsets[leg] + 0.5) - 0.5;
                offsets[leg] = wrap(from.leg_offsets[leg] + gap * share);
            }
            return new Gait
            {
                name = share < 0.5 ? from.name : to.name,
                duty = from.duty + (to.duty - from.duty) * share,
                leg_offsets = offsets,
                lift = from.lift + (to.lift - from.lift) * share,
            };
        }

        // Keeps a phase between 0 and 1, wrapping round (1.25 becomes 0.25). Broken numbers give 0.
        public static double wrap(double phase)
        {
            if (double.IsNaN(phase) || double.IsInfinity(phase))
            {
                return 0;
            }
            phase %= 1;
            return phase < 0 ? phase + 1 : phase;
        }

        // Keeps a number between low and high (not-a-number gives low).
        static double Clamp(double value, double low, double high)
        {
            if (double.IsNaN(value) || value < low)
            {
                return low;
            }
            return value > high ? high : value;
        }
    }
}
