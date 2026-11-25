// Purpose: the motion maths behind smooth characters and camera: springs for ears, tail, hair
// and scarf, and a follow that eases the camera after a target without overshooting. Both give
// the same motion at 30, 60 or 120 frames a second, which is a large part of why a game feels
// smooth on every screen. Ported from prototype/web/js/motion.js. Plain C#: no Unity code.
using System;

namespace Bodhi.Look
{
    // Checks shared by both kinds of motion.
    static class MotionInput
    {
        // The longest frame we simulate. If the phone stalls for seconds, carrying on from
        // where it was is better than replaying all that time in one frame.
        public const double longest_frame_seconds = 0.25;

        // True for a frame time worth simulating: a real number above zero.
        public static bool usable(double delta_seconds, double target)
        {
            return delta_seconds > 0.0 && !double.IsNaN(delta_seconds) && !double.IsInfinity(delta_seconds)
                && !double.IsNaN(target) && !double.IsInfinity(target);
        }
    }

    // A value pulled toward a target by a spring, so it lags, swings a little past and settles.
    public class Spring
    {
        // Small steps keep stiff springs steady however long a frame is. At 1/120 s, 30, 60
        // and 120 frames a second all take the very same steps, so they move identically.
        const double step_seconds_at_most = 1.0 / 120.0;

        public double value { get; private set; }
        public double velocity { get; private set; }

        // Makes a spring resting at `starting_value`.
        public Spring(double starting_value)
        {
            value = starting_value;
        }

        // Moves the spring on by one frame. `stiffness` is how hard it pulls toward the target;
        // `damping` is how quickly the swinging dies away. Bad input changes nothing.
        public void update(double target, double stiffness, double damping, double delta_seconds)
        {
            if (!MotionInput.usable(delta_seconds, target))
            {
                return;
            }
            double seconds = Math.Min(delta_seconds, MotionInput.longest_frame_seconds);
            int steps = Math.Max(1, (int)Math.Ceiling(seconds / step_seconds_at_most - 1e-9));
            double step_seconds = seconds / steps;
            for (int step = 0; step < steps; step += 1)
            {
                double pull = (target - value) * stiffness - velocity * damping;
                velocity += pull * step_seconds;
                value += velocity * step_seconds;
            }
        }
    }

    // A value that eases after a moving target and never goes past it (a "critically damped"
    // spring). Used for the camera, where an overshoot reads as a wobble.
    public class SmoothFollow
    {
        public double value { get; private set; }
        double velocity;

        // Makes a follow resting at `starting_value`.
        public SmoothFollow(double starting_value)
        {
            value = starting_value;
        }

        // Moves on by one frame. `smooth_seconds` is roughly how long it takes to catch up.
        // The formula is the standard fast approximation of a critically damped spring (the
        // one the browser game and Unity's own SmoothDamp use). Bad input changes nothing.
        public void update(double target, double smooth_seconds, double delta_seconds)
        {
            if (!MotionInput.usable(delta_seconds, target))
            {
                return;
            }
            double seconds = Math.Min(delta_seconds, MotionInput.longest_frame_seconds);
            double omega = 2.0 / Math.Max(0.0001, smooth_seconds);
            double x = omega * seconds;
            double decay = 1.0 / (1.0 + x + 0.48 * x * x + 0.235 * x * x * x);
            double change = value - target;
            double temporary = (velocity + omega * change) * seconds;
            velocity = (velocity - omega * temporary) * decay;
            double next_value = target + (change + temporary) * decay;

            // The approximation can step a hair past the target on a long frame. If it would
            // cross the target, stop exactly on it instead.
            if ((value - target) * (next_value - target) < 0.0)
            {
                next_value = target;
                velocity = 0.0;
            }
            value = next_value;
        }
    }
}
