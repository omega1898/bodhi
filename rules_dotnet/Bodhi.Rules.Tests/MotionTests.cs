// Purpose: proves the motion maths that makes the characters and camera feel smooth: springs
// settle, the camera follow never overshoots, and the motion is the same at 30, 60 and 120
// frames a second (the browser game's prototype/web/js/motion.js, ported to C#).
using System;
using Bodhi.Look;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class MotionTests
    {
        // Runs a smooth follow toward `target` for `total_seconds`, in frames of 1 / frames_per_second.
        static double follow_for(double total_seconds, int frames_per_second, double target, double smooth_seconds)
        {
            var follow = new SmoothFollow(0.0);
            int frames = (int)Math.Round(total_seconds * frames_per_second);
            for (int frame = 0; frame < frames; frame += 1)
            {
                follow.update(target, smooth_seconds, 1.0 / frames_per_second);
            }
            return follow.value;
        }

        // The camera follow catches up most of the way in its smoothing time, and all the way soon after.
        [Test]
        public void smooth_follow_catches_up()
        {
            Assert.That(follow_for(0.7, 60, 100.0, 0.7), Is.GreaterThan(55.0));
            Assert.That(follow_for(3.0, 60, 100.0, 0.7), Is.EqualTo(100.0).Within(0.5));
        }

        // It never goes past the target: an overshoot reads as the camera wobbling.
        [Test]
        public void smooth_follow_never_overshoots()
        {
            var follow = new SmoothFollow(0.0);
            for (int frame = 0; frame < 600; frame += 1)
            {
                follow.update(100.0, 0.3, 1.0 / 60);
                Assert.That(follow.value, Is.LessThanOrEqualTo(100.0 + 1e-9));
            }
        }

        // The same motion at 30, 60 and 120 frames a second, within 1% of the distance travelled.
        [Test]
        public void smooth_follow_is_the_same_at_any_frame_rate()
        {
            double at_60 = follow_for(0.5, 60, 100.0, 0.4);
            Assert.That(follow_for(0.5, 30, 100.0, 0.4), Is.EqualTo(at_60).Within(1.0));
            Assert.That(follow_for(0.5, 120, 100.0, 0.4), Is.EqualTo(at_60).Within(1.0));
        }

        // Runs a spring toward `target` for `total_seconds` at the given frame rate.
        static double spring_for(double total_seconds, int frames_per_second, double target)
        {
            var spring = new Spring(0.0);
            int frames = (int)Math.Round(total_seconds * frames_per_second);
            for (int frame = 0; frame < frames; frame += 1)
            {
                spring.update(target, 60.0, 6.0, 1.0 / frames_per_second);
            }
            return spring.value;
        }

        // A tail on a spring swings past its target and settles there: secondary motion.
        [Test]
        public void a_spring_swings_and_settles()
        {
            var spring = new Spring(0.0);
            double highest = 0.0;
            for (int frame = 0; frame < 300; frame += 1)
            {
                spring.update(1.0, 60.0, 6.0, 1.0 / 60);
                highest = Math.Max(highest, spring.value);
            }
            Assert.That(highest, Is.GreaterThan(1.0), "a springy tail overshoots a little");
            Assert.That(spring.value, Is.EqualTo(1.0).Within(0.01), "and settles");
        }

        // The spring moves the same at 30, 60 and 120 frames a second: its small fixed steps make
        // all three take the very same steps, so they match to a thousandth.
        [Test]
        public void a_spring_is_the_same_at_any_frame_rate()
        {
            double at_120 = spring_for(0.4, 120, 1.0);
            Assert.That(spring_for(0.4, 60, 1.0), Is.EqualTo(at_120).Within(0.001));
            Assert.That(spring_for(0.4, 30, 1.0), Is.EqualTo(at_120).Within(0.001));
        }

        // Break it: a very long frame (the phone stalled for 2 s) must not make a stiff spring explode.
        [Test]
        public void break_it_a_long_frame_does_not_explode_a_spring()
        {
            var spring = new Spring(0.0);
            spring.update(1.0, 400.0, 10.0, 2.0);
            Assert.That(double.IsFinite(spring.value), Is.True);
            Assert.That(Math.Abs(spring.value), Is.LessThan(10.0));
        }

        // Break it: zero, negative or broken frame times change nothing and never produce NaN.
        [TestCase(0.0)]
        [TestCase(-0.5)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void break_it_bad_frame_times_change_nothing(double delta_seconds)
        {
            var spring = new Spring(0.25);
            spring.update(1.0, 60.0, 6.0, delta_seconds);
            Assert.That(spring.value, Is.EqualTo(0.25));
            var follow = new SmoothFollow(0.25);
            follow.update(1.0, 0.5, delta_seconds);
            Assert.That(follow.value, Is.EqualTo(0.25));
        }

        // Break it: a broken target (NaN) is ignored, so one bad number can't poison the camera.
        [Test]
        public void break_it_a_broken_target_is_ignored()
        {
            var follow = new SmoothFollow(3.0);
            follow.update(double.NaN, 0.5, 1.0 / 60);
            Assert.That(follow.value, Is.EqualTo(3.0));
            var spring = new Spring(3.0);
            spring.update(double.NaN, 60.0, 6.0, 1.0 / 60);
            Assert.That(spring.value, Is.EqualTo(3.0));
        }
    }
}
