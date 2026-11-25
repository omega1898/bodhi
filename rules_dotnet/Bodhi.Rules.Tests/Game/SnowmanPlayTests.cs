// Purpose: proves the title screen's choreography (PlayTrack and SnowmanPlay) holds together:
// nobody ever jumps from place to place (not between beats, not when building turns into play,
// not when a round of play starts again), nobody moves faster than a person or a dog can, the
// snowman is built in order and stays built, the thrown snowball reaches Bodhi's mouth when he
// catches it, and broken times never crash it.
using System;
using Bodhi.Game;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class SnowmanPlayTests
    {
        const double step = 1.0 / 60.0;  // One frame at 60 frames a second.

        // A track's beats join up: each starts where the one before ended.
        [Test]
        public void a_track_never_jumps_between_beats()
        {
            var track = new PlayTrack(0, 0, 0)
                .go_to(1, "move", 2, 0, Ease.start_and_stop)
                .circle(2, "move", 2, 1, 270, Ease.steady)
                .leap_to(0.5, 0, 0, 0.5)
                .stay(1, "sit", LookAt.partner, 45);
            double last_x = 0, last_z = 0;
            for (double time = 0; time <= track.length + 0.5; time += step)
            {
                var pose = track.pose_at(time);
                double moved = Math.Sqrt((pose.x - last_x) * (pose.x - last_x) + (pose.z - last_z) * (pose.z - last_z));
                Assert.That(moved, Is.LessThan(0.2), "a jump at " + time);
                last_x = pose.x;
                last_z = pose.z;
            }
            Assert.That(track.pose_at(99).action, Is.EqualTo("sit"), "after the end it holds the last beat");
            Assert.That(track.pose_at(99).facing, Is.EqualTo(45));
        }

        // A circle keeps its radius all the way round, and a leap rises and comes back down.
        [Test]
        public void circles_stay_round_and_leaps_land()
        {
            var track = new PlayTrack(3, 0, 0).circle(4, "move", 0, 0, 360, Ease.steady).leap_to(1, 5, 0, 0.6);
            for (double time = 0; time < 4; time += 0.1)
            {
                var pose = track.pose_at(time);
                Assert.That(Math.Sqrt(pose.x * pose.x + pose.z * pose.z), Is.EqualTo(3).Within(1e-9));
                Assert.That(pose.speed, Is.EqualTo(2 * Math.PI * 3 / 4).Within(1e-9));
            }
            Assert.That(track.pose_at(4.5).height, Is.EqualTo(0.6).Within(1e-9), "the top of the leap");
            Assert.That(track.pose_at(5.0).height, Is.EqualTo(0).Within(1e-9), "back on the snow");
        }

        // A move that eases to a stop really stops: no speed left at its end.
        [Test]
        public void easing_starts_and_stops_softly()
        {
            var track = new PlayTrack(0, 0, 0).go_to(2, "move", 4, 0, Ease.start_and_stop);
            Assert.That(track.pose_at(0).speed, Is.EqualTo(0).Within(1e-9));
            Assert.That(track.pose_at(1).speed, Is.EqualTo(3).Within(1e-9), "fastest in the middle");
            Assert.That(track.pose_at(2).speed, Is.EqualTo(0).Within(1e-9));
            Assert.That(PlayTrack.eased(Ease.stop, 1), Is.EqualTo(1));
            Assert.That(PlayTrack.eased(Ease.start, 0), Is.EqualTo(0));
        }

        // Break it: beats with no time, negative or endless time are skipped; broken times give
        // the start; a track with no beats still answers.
        [Test]
        public void a_track_survives_broken_input()
        {
            var track = new PlayTrack(1, 2, 30)
                .stay(0, "sit", LookAt.partner)
                .stay(-4, "sit", LookAt.partner)
                .go_to(double.PositiveInfinity, "move", 9, 9, Ease.steady)
                .go_to(double.NaN, "move", 9, 9, Ease.steady);
            Assert.That(track.length, Is.EqualTo(0));
            var pose = track.pose_at(double.NaN);
            Assert.That(pose.x, Is.EqualTo(1));
            Assert.That(pose.z, Is.EqualTo(2));
            Assert.That(pose.action, Is.EqualTo("stand"));
            var real = new PlayTrack(0, 0, 0).go_to(1, "move", 1, 0, Ease.steady);
            Assert.That(real.pose_at(-3).x, Is.EqualTo(0));
            Assert.That(real.pose_at(double.NaN).x, Is.EqualTo(0));
        }

        // Over building and three rounds of play, frame by frame: nobody jumps, nobody moves
        // faster than they could, and every number is a real number.
        [Test]
        public void nobody_ever_jumps_or_runs_too_fast()
        {
            var play = new SnowmanPlay();
            var last = play.frame_at(0);
            double end = play.building_seconds + 3 * play.round_seconds;
            for (double time = step; time <= end; time += step)
            {
                var frame = play.frame_at(time);
                check_moved(last.em, frame.em, 4.0, "Em", time);
                check_moved(last.bodhi, frame.bodhi, 8.0, "Bodhi", time);
                Assert.That(frame.em.speed, Is.LessThan(3.5), "Em runs no faster than a jog, at " + time);
                Assert.That(frame.bodhi.speed, Is.LessThan(7.5), "Bodhi gallops no faster than a dog can, at " + time);
                Assert.That(double.IsNaN(frame.em.facing) || double.IsNaN(frame.bodhi.facing), Is.False);
                last = frame;
            }
        }

        // Fails if a character moved further in one frame than `top_speed` allows.
        static void check_moved(PlayPose before, PlayPose after, double top_speed, string who, double time)
        {
            double moved = Math.Sqrt((after.x - before.x) * (after.x - before.x) + (after.z - before.z) * (after.z - before.z));
            Assert.That(moved, Is.LessThan(top_speed * step * 1.5 + 1e-6), who + " jumped at " + time);
        }

        // A round of play ends exactly where it began, and building ends where play begins.
        [Test]
        public void the_play_loop_joins_up()
        {
            var play = new SnowmanPlay();
            var end_of_building = play.frame_at(play.building_seconds - 1e-6);
            var start_of_play = play.frame_at(play.building_seconds);
            var end_of_round = play.frame_at(play.building_seconds + play.round_seconds - 1e-6);
            Assert.That(start_of_play.em.x, Is.EqualTo(end_of_building.em.x).Within(0.01));
            Assert.That(start_of_play.bodhi.z, Is.EqualTo(end_of_building.bodhi.z).Within(0.01));
            Assert.That(end_of_round.em.x, Is.EqualTo(start_of_play.em.x).Within(0.01));
            Assert.That(end_of_round.em.z, Is.EqualTo(start_of_play.em.z).Within(0.01));
            Assert.That(end_of_round.bodhi.x, Is.EqualTo(start_of_play.bodhi.x).Within(0.01));
            Assert.That(end_of_round.bodhi.z, Is.EqualTo(start_of_play.bodhi.z).Within(0.01));
            Assert.That(start_of_play.playing, Is.True);
            Assert.That(end_of_building.playing, Is.False);
        }

        // The snowman is built bottom, middle, head; it grows and never shrinks; its face and arms
        // come last; and once built it stays built through every round of play.
        [Test]
        public void the_snowman_is_built_in_order_and_stays_built()
        {
            var play = new SnowmanPlay();
            Assert.That(play.frame_at(0.5).middle.state, Is.EqualTo(BallState.hidden));
            Assert.That(play.frame_at(0.5).bottom.radius, Is.LessThan(SnowmanPlay.bottom_radius / 2), "it starts small");
            double last_radius = 0;
            for (double time = 0; time < SnowmanPlay.bottom_rolled; time += 0.25)
            {
                double radius = play.frame_at(time).bottom.radius;
                Assert.That(radius, Is.GreaterThanOrEqualTo(last_radius), "the bottom ball grows as it rolls");
                last_radius = radius;
            }
            Assert.That(play.frame_at(SnowmanPlay.bottom_rolled).bottom.state, Is.EqualTo(BallState.stacked));
            Assert.That(play.frame_at(SnowmanPlay.middle_held + 0.5).middle.state, Is.EqualTo(BallState.held));
            Assert.That(play.frame_at(SnowmanPlay.middle_placed).middle.state, Is.EqualTo(BallState.stacked));
            Assert.That(play.frame_at(SnowmanPlay.head_packing + 0.5).head.state, Is.EqualTo(BallState.held));
            Assert.That(play.frame_at(SnowmanPlay.eyes_at - 0.1).has_eyes, Is.False);
            Assert.That(play.frame_at(SnowmanPlay.right_arm_at + 0.1).has_right_arm, Is.True);
            Assert.That(SnowmanPlay.middle_height, Is.GreaterThan(SnowmanPlay.bottom_height));
            Assert.That(SnowmanPlay.head_height, Is.InRange(1.2, 1.6), "a snowman a bit shorter than her");
            foreach (double round in new[] { 0.0, 1.0, 5.0 })
            {
                var later = play.frame_at(play.building_seconds + round * play.round_seconds + 7.3);
                Assert.That(later.bottom.state, Is.EqualTo(BallState.stacked));
                Assert.That(later.middle.state, Is.EqualTo(BallState.stacked));
                Assert.That(later.head.state, Is.EqualTo(BallState.stacked));
                Assert.That(later.has_eyes && later.has_nose && later.has_left_arm && later.has_right_arm, Is.True);
            }
        }

        // The snowball she rolls stays just in front of her the whole time she pushes it.
        [Test]
        public void the_rolled_ball_stays_at_her_hands()
        {
            var play = new SnowmanPlay();
            for (double time = SnowmanPlay.bottom_made; time < SnowmanPlay.bottom_rolled; time += 0.3)
            {
                var frame = play.frame_at(time);
                double gap = Math.Sqrt(Math.Pow(frame.bottom.x - frame.em.x, 2) + Math.Pow(frame.bottom.z - frame.em.z, 2));
                Assert.That(gap, Is.EqualTo(frame.bottom.radius + 0.3).Within(1e-9));
            }
            var done = play.frame_at(SnowmanPlay.bottom_rolled - 0.01);
            Assert.That(Math.Abs(done.bottom.x) + Math.Abs(done.bottom.z), Is.LessThan(0.05), "it ends where the snowman stands");
        }

        // The thrown snowball arrives where Bodhi is at the top of his leap.
        [Test]
        public void he_catches_the_snowball_at_the_top_of_his_leap()
        {
            var play = new SnowmanPlay();
            double catch_time = play.building_seconds + play.round_seconds + SnowmanPlay.ball_caught - 0.01;
            var frame = play.frame_at(catch_time);
            Assert.That(frame.bodhi.action, Is.EqualTo("leap"));
            Assert.That(frame.bodhi.height, Is.GreaterThan(0.45));
            double gap = Math.Sqrt(Math.Pow(frame.thrown.x - frame.bodhi.x, 2) + Math.Pow(frame.thrown.z - frame.bodhi.z, 2));
            Assert.That(gap, Is.LessThan(0.7), "the ball is at his nose");
            Assert.That(frame.thrown_height, Is.InRange(0.8, 1.4));
            var after = play.frame_at(catch_time + 0.05);
            Assert.That(after.thrown.state, Is.EqualTo(BallState.hidden), "caught");
        }

        // Each puff of snow is reported once, in the frame it happens.
        [Test]
        public void each_puff_comes_once()
        {
            var play = new SnowmanPlay();
            int puffs = 0;
            double end = play.building_seconds + 2 * play.round_seconds;
            for (double time = 0; time <= end; time += step)
            {
                puffs += play.frame_at(time).puffs.Count;
            }
            Assert.That(puffs, Is.EqualTo(5 + 3 * 2), "five while building, three in each round");
        }

        // Break it: broken and huge times still give a frame with real numbers and a built snowman.
        [Test]
        public void broken_times_are_safe()
        {
            foreach (double time in new[] { double.NaN, double.NegativeInfinity, -10.0, double.PositiveInfinity })
            {
                var frame = new SnowmanPlay().frame_at(time);
                Assert.That(double.IsNaN(frame.em.x), Is.False);
                Assert.That(frame.bottom.state, Is.Not.EqualTo(BallState.stacked), "treated as the very start");
            }
            var late = new SnowmanPlay().frame_at(1e7);
            Assert.That(late.playing, Is.True);
            Assert.That(double.IsNaN(late.bodhi.x), Is.False);
            Assert.That(late.head.state, Is.EqualTo(BallState.stacked));
        }
    }
}
