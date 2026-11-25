// Purpose: proves the leg maths behind the 3D characters: a planted foot slides backward at
// exactly the body's speed (so in the world it stays still and never skates), a swinging foot
// lifts and lands, the gaits put the right legs together, and the limb bender (LegReach) lands
// the foot exactly where it's asked, bends knees the right way, and survives broken numbers.
using System;
using Bodhi.Game;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class MotionMathsTests
    {
        // While a foot is planted, moving the body forward one metre moves the foot back one
        // metre under it: in the world, the foot doesn't move at all.
        [Test]
        public void a_planted_foot_never_slides()
        {
            foreach (var gait in new[] { Stride.human_walk, Stride.human_run, Stride.dog_trot, Stride.dog_gallop })
            {
                double stride = 1.3;
                double phase = gait.duty * 0.2;
                double small = 0.01;  // Metres the body moves.
                var before = Stride.foot_at(phase, gait.duty, stride, gait.lift);
                var after = Stride.foot_at(Stride.advance(phase, small, stride), gait.duty, stride, gait.lift);
                Assert.That(before.planted && after.planted, Is.True);
                Assert.That(before.forward - after.forward, Is.EqualTo(small).Within(1e-9), gait.name);
                Assert.That(after.lift, Is.EqualTo(0));
            }
        }

        // A swinging foot leaves the ground, rises and comes back down where the next stance begins.
        [Test]
        public void a_swinging_foot_lifts_and_lands()
        {
            var gait = Stride.human_walk;
            var lifted = Stride.foot_at(gait.duty + (1 - gait.duty) * 0.45, gait.duty, 1.2, gait.lift);
            Assert.That(lifted.planted, Is.False);
            Assert.That(lifted.lift, Is.GreaterThan(gait.lift * 0.8));
            var landing = Stride.foot_at(0.99999, gait.duty, 1.2, gait.lift);
            var landed = Stride.foot_at(0.0, gait.duty, 1.2, gait.lift);
            Assert.That(landing.forward, Is.EqualTo(landed.forward).Within(1e-4), "it lands where the stance starts");
            for (double phase = 0; phase < 1; phase += 0.01)
            {
                Assert.That(Stride.foot_at(phase, gait.duty, 1.2, gait.lift).lift, Is.GreaterThanOrEqualTo(0), "never below the snow");
            }
        }

        // The gaits move the right legs together: a trot's diagonal pairs, a person's
        // alternating legs, and a gallop with a moment when no foot is down.
        [Test]
        public void the_gaits_pair_the_right_legs()
        {
            var trot = Stride.dog_trot.leg_offsets;
            Assert.That(trot[0], Is.EqualTo(trot[3]), "front left with hind right");
            Assert.That(trot[1], Is.EqualTo(trot[2]), "front right with hind left");
            Assert.That(Math.Abs(Stride.human_walk.leg_offsets[1] - Stride.human_walk.leg_offsets[0]), Is.EqualTo(0.5));
            bool all_in_air = false;
            var gallop = Stride.dog_gallop;
            for (double phase = 0; phase < 1; phase += 0.005)
            {
                bool any_down = false;
                foreach (double offset in gallop.leg_offsets)
                {
                    any_down |= Stride.foot_at(phase + offset, gallop.duty, 2, gallop.lift).planted;
                }
                all_in_air |= !any_down;
            }
            Assert.That(all_in_air, Is.True, "a gallop has a moment in the air");
        }

        // Changing gait blends gradually, taking the short way round the cycle.
        [Test]
        public void gaits_blend_gradually()
        {
            var half = Stride.blend(Stride.dog_trot, Stride.dog_gallop, 0.5);
            Assert.That(half.duty, Is.EqualTo((Stride.dog_trot.duty + Stride.dog_gallop.duty) / 2).Within(1e-9));
            Assert.That(Stride.blend(Stride.dog_trot, Stride.dog_gallop, 0).leg_offsets[2], Is.EqualTo(Stride.dog_trot.leg_offsets[2]).Within(1e-9));
            Assert.That(Stride.blend(Stride.dog_trot, Stride.dog_gallop, 1).leg_offsets[1], Is.EqualTo(Stride.dog_gallop.leg_offsets[1]).Within(1e-9));
            Assert.That(Stride.wrap(1.25), Is.EqualTo(0.25).Within(1e-12));
            Assert.That(Stride.wrap(-0.25), Is.EqualTo(0.75).Within(1e-12));
        }

        // The limb lands exactly on a reachable spot, and the knee bends forward when asked.
        [Test]
        public void the_limb_reaches_the_spot()
        {
            double upper = 0.42, lower = 0.40;
            foreach (var (forward, up) in new[] { (0.1, -0.7), (-0.2, -0.6), (0.3, -0.45), (0.0, -0.81) })
            {
                var bend = LegReach.solve(forward, up, upper, lower, true);
                double lower_radians = bend.lower_angle * Math.PI / 180;
                double end_forward = bend.middle_forward + lower * Math.Sin(lower_radians);
                double end_up = bend.middle_up - lower * Math.Cos(lower_radians);
                Assert.That(bend.reached, Is.True);
                Assert.That(end_forward, Is.EqualTo(forward).Within(1e-6));
                Assert.That(end_up, Is.EqualTo(up).Within(1e-6));
                Assert.That(bend.upper_angle, Is.GreaterThan(bend.lower_angle), "the knee bends forward");
            }
            var elbow = LegReach.solve(0.0, -0.25, 0.15, 0.17, false);
            Assert.That(elbow.middle_forward, Is.LessThan(0), "a backward-bending joint goes back");
        }

        // A spot out of reach gets a straight limb pointing at it; broken numbers hang straight down.
        [Test]
        public void the_limb_survives_impossible_and_broken_spots()
        {
            var far = LegReach.solve(0, -5, 0.42, 0.40, true);
            Assert.That(far.reached, Is.False);
            Assert.That(far.upper_angle, Is.EqualTo(far.lower_angle).Within(2), "straight, or as near as makes no difference");
            var close = LegReach.solve(0, 0, 0.42, 0.40, true);
            Assert.That(double.IsNaN(close.upper_angle), Is.False);
            foreach (var bad in new[] { double.NaN, double.PositiveInfinity })
            {
                var broken = LegReach.solve(bad, -0.5, 0.42, 0.40, true);
                Assert.That(broken.upper_angle, Is.EqualTo(0));
                Assert.That(broken.lower_angle, Is.EqualTo(0));
            }
            var no_length = LegReach.solve(0.1, -0.5, 0, 0.4, true);
            Assert.That(no_length.reached, Is.False);
            Assert.That(Stride.foot_at(double.NaN, 0.5, double.NaN, 0.1).forward, Is.EqualTo(0));
            Assert.That(Stride.advance(0.3, double.PositiveInfinity, 1), Is.EqualTo(0.3).Within(1e-12));
        }
    }
}
