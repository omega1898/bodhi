// Purpose: works out how a two-part limb bends so its end lands on a chosen spot: given where a
// hip is and where the foot should be, how far the thigh swings and how much the knee bends.
// Animators call this "inverse kinematics". It's what keeps a planted foot on the snow while the
// body moves over it, and it works for an arm reaching for a snowball too.
//
// The limb is worked out side-on, in the plane it swings in: "forward" and "up" measured from
// the joint at the top. Angles are in degrees, measured from hanging straight down, positive
// when swung forward. Plain C#, no Unity: tested with dotnet.
using System;

namespace Bodhi.Game
{
    // How a limb bends to reach a spot.
    public struct Bend
    {
        public double upper_angle;     // The top part (thigh, upper arm), from straight down.
        public double lower_angle;     // The bottom part (shin, forearm), from straight down.
        public double middle_forward;  // Where the middle joint (knee, elbow) ends up.
        public double middle_up;
        public bool reached;           // False if the spot was out of reach (the limb points at it, straight).
    }

    public static class LegReach
    {
        // Bends a limb whose top part is `upper` long and bottom part `lower` long so its end
        // lands at (`forward`, `up`) from its top joint. `middle_forward` true bends the middle
        // joint forward (a person's knee, a dog's stifle); false bends it backward (an elbow, a
        // dog's front "elbow"). Spots too far or too close are reached as nearly as the limb can.
        // Broken numbers give the limb hanging straight down.
        public static Bend solve(double forward, double up, double upper, double lower, bool middle_forward)
        {
            if (!(upper > 0) || !(lower > 0) || double.IsNaN(forward) || double.IsNaN(up) || double.IsInfinity(forward) || double.IsInfinity(up))
            {
                return new Bend { middle_up = -Math.Max(0, double.IsNaN(upper) ? 0 : upper), reached = false };
            }
            double distance = Math.Sqrt(forward * forward + up * up);
            double longest = (upper + lower) * 0.9999;
            double shortest = Math.Abs(upper - lower) + 1e-4;
            bool reached = distance <= longest && distance >= shortest;
            double used = Math.Max(shortest, Math.Min(longest, distance));
            double toward = distance > 1e-9 ? Math.Atan2(forward, -up) : 0;  // The line to the spot, from straight down.

            // The law of cosines gives the angle between the top part and the line to the spot.
            double cosine = (upper * upper + used * used - lower * lower) / (2 * upper * used);
            double spread = Math.Acos(Math.Max(-1, Math.Min(1, cosine)));
            double upper_angle = middle_forward ? toward + spread : toward - spread;
            double middle_forward_at = upper * Math.Sin(upper_angle);
            double middle_up_at = -upper * Math.Cos(upper_angle);
            double end_forward = used * Math.Sin(toward);
            double end_up = -used * Math.Cos(toward);
            double lower_angle = Math.Atan2(end_forward - middle_forward_at, -(end_up - middle_up_at));
            return new Bend
            {
                upper_angle = upper_angle * 180 / Math.PI,
                lower_angle = lower_angle * 180 / Math.PI,
                middle_forward = middle_forward_at,
                middle_up = middle_up_at,
                reached = reached,
            };
        }
    }
}
