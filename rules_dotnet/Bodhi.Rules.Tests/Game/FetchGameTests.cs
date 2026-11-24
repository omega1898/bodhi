// Purpose: proves the fetch game at home (FetchGame): Em throws, the ball arcs over to Bodhi,
// and a tap while it is in his reach is a catch; too early, or letting it land, is a miss. It
// gets a little faster with every catch in a row, up to a limit. Also tries to break it: a tap
// with no ball in the air, a second throw mid-flight, and broken frame times.
using Bodhi.Game;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class FetchGameTests
    {
        // Lets time pass in small steps, as the game's frames would, and returns the last result.
        static FetchResult pass(FetchGame game, double seconds)
        {
            var result = FetchResult.nothing;
            for (double passed = 0; passed < seconds - 1e-9; passed += 0.01)
            {
                var step = game.pass_time(0.01);
                if (step != FetchResult.nothing) result = step;
            }
            return result;
        }

        // A tap while the ball is in his reach, near the end of its flight, is a catch.
        [Test]
        public void a_tap_in_the_window_is_a_catch()
        {
            var game = new FetchGame();
            Assert.That(game.throw_ball(0), Is.True);
            Assert.That(game.ball_in_air, Is.True);
            pass(game, game.flight_seconds - game.window_seconds / 2);
            Assert.That(game.in_catch_window, Is.True);
            Assert.That(game.tap(), Is.EqualTo(FetchResult.caught));
            Assert.That(game.ball_in_air, Is.False, "the ball is in his mouth");
        }

        // A tap before the ball reaches him snaps at the air: a miss.
        [Test]
        public void a_tap_too_early_is_a_miss()
        {
            var game = new FetchGame();
            game.throw_ball(0);
            pass(game, 0.2);
            Assert.That(game.in_catch_window, Is.False);
            Assert.That(game.tap(), Is.EqualTo(FetchResult.missed));
            Assert.That(game.ball_in_air, Is.False);
        }

        // With no tap at all the ball lands: a miss, reported once.
        [Test]
        public void a_ball_left_to_land_is_a_miss()
        {
            var game = new FetchGame();
            game.throw_ball(0);
            Assert.That(pass(game, game.flight_seconds + 0.5), Is.EqualTo(FetchResult.missed));
            Assert.That(game.ball_in_air, Is.False);
            Assert.That(game.pass_time(0.1), Is.EqualTo(FetchResult.nothing), "the miss isn't reported again");
        }

        // The ball rises and falls: on the ground at both ends, highest in the middle.
        [Test]
        public void the_ball_arcs()
        {
            var game = new FetchGame();
            game.throw_ball(0);
            Assert.That(game.across, Is.EqualTo(0).Within(1e-9));
            Assert.That(game.height, Is.EqualTo(0).Within(1e-9));
            pass(game, game.flight_seconds / 2);
            Assert.That(game.across, Is.EqualTo(0.5).Within(0.02));
            Assert.That(game.height, Is.EqualTo(1).Within(0.01));
        }

        // Each catch in a row makes the next throw a little faster with a narrower window, up to a limit.
        [Test]
        public void it_gets_harder_with_catches_in_a_row_but_only_so_far()
        {
            var game = new FetchGame();
            game.throw_ball(0);
            double first_flight = game.flight_seconds, first_window = game.window_seconds;
            game.tap();
            game.throw_ball(3);
            Assert.That(game.flight_seconds, Is.LessThan(first_flight));
            Assert.That(game.window_seconds, Is.LessThan(first_window));
            game.tap();
            game.throw_ball(FetchGame.hardest_after_catches);
            double hardest_flight = game.flight_seconds, hardest_window = game.window_seconds;
            Assert.That(hardest_flight, Is.EqualTo(FetchGame.fastest_flight_seconds).Within(1e-9));
            Assert.That(hardest_window, Is.EqualTo(FetchGame.narrowest_window_seconds).Within(1e-9));
            game.tap();
            game.throw_ball(1000);
            Assert.That(game.flight_seconds, Is.EqualTo(hardest_flight), "capped");
            Assert.That(game.window_seconds, Is.EqualTo(hardest_window), "capped");
        }

        // The same throw and the same taps always give the same answer.
        [Test]
        public void it_is_the_same_every_time()
        {
            for (int catches = 0; catches < 12; catches++)
            {
                var first = new FetchGame();
                var second = new FetchGame();
                first.throw_ball(catches);
                second.throw_ball(catches);
                pass(first, 0.85);
                pass(second, 0.85);
                Assert.That(first.tap(), Is.EqualTo(second.tap()));
            }
        }

        // Break it: a tap with no ball in the air does nothing (not a miss, and no throw).
        [Test]
        public void a_tap_with_no_ball_does_nothing()
        {
            var game = new FetchGame();
            Assert.That(game.tap(), Is.EqualTo(FetchResult.nothing));
            Assert.That(game.ball_in_air, Is.False);
            game.throw_ball(0);
            pass(game, game.flight_seconds - 0.05);
            Assert.That(game.tap(), Is.EqualTo(FetchResult.caught));
            Assert.That(game.tap(), Is.EqualTo(FetchResult.nothing), "the ball was already caught");
        }

        // Break it: throwing again while the ball is still flying is refused and changes nothing.
        [Test]
        public void a_second_throw_mid_flight_is_refused()
        {
            var game = new FetchGame();
            game.throw_ball(0);
            pass(game, 0.3);
            double flown = game.seconds_flown, flight = game.flight_seconds;
            Assert.That(game.throw_ball(8), Is.False);
            Assert.That(game.seconds_flown, Is.EqualTo(flown));
            Assert.That(game.flight_seconds, Is.EqualTo(flight));
        }

        // Break it: broken frame times (negative, not a number, endless) are ignored and counted;
        // a negative catch count plays as the first throw.
        [Test]
        public void broken_times_are_ignored()
        {
            var game = new FetchGame();
            Assert.That(game.pass_time(0.5), Is.EqualTo(FetchResult.nothing), "no ball: nothing to move");
            game.throw_ball(-5);
            Assert.That(game.flight_seconds, Is.EqualTo(FetchGame.first_flight_seconds));
            Assert.That(game.pass_time(-1), Is.EqualTo(FetchResult.nothing));
            Assert.That(game.pass_time(double.NaN), Is.EqualTo(FetchResult.nothing));
            Assert.That(game.pass_time(double.PositiveInfinity), Is.EqualTo(FetchResult.nothing));
            Assert.That(game.seconds_flown, Is.EqualTo(0));
            Assert.That(game.ball_in_air, Is.True);
            Assert.That(game.refused_count, Is.EqualTo(3));
        }

        // Break it: one huge frame (the phone stalled) just lands the ball as a miss.
        [Test]
        public void a_huge_frame_lands_the_ball()
        {
            var game = new FetchGame();
            game.throw_ball(0);
            Assert.That(game.pass_time(30), Is.EqualTo(FetchResult.missed));
            Assert.That(game.ball_in_air, Is.False);
            Assert.That(game.tap(), Is.EqualTo(FetchResult.nothing));
        }
    }
}
