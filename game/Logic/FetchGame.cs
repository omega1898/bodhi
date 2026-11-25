// Purpose: the fetch game at home, as plain timing rules with no Unity in it. Em throws the ball,
// it arcs over to Bodhi, and the player taps to have him catch it. A tap while the ball is in
// his reach (the last part of its flight) is a catch; a tap too early, or letting it land, is a
// miss. Every catch in a row makes the next throw a little faster with a narrower window, up
// to a limit, so a long run feels earned but never becomes impossible. Time is passed in by
// the scene each frame, so the same taps at the same moments always give the same answer.
using System;

namespace Bodhi.Game
{
    // What a tap, or time passing, did to the ball.
    public enum FetchResult
    {
        nothing,  // No ball in the air, or it's still flying.
        caught,   // He caught it.
        missed,   // Snapped too early, or it landed.
    }

    public class FetchGame
    {
        // The first throw: how long the ball flies, and how long at the end of its flight it is
        // in his reach. Starting values, to tune in playtests.
        public const double first_flight_seconds = 1.4;
        public const double first_window_seconds = 0.45;

        // The hardest throw, reached after this many catches in a row and never passed.
        public const double fastest_flight_seconds = 0.9;
        public const double narrowest_window_seconds = 0.22;
        public const int hardest_after_catches = 8;

        // True while a ball is flying toward him.
        public bool ball_in_air { get; private set; }

        // This throw's whole flight and its catch window (the last part of the flight).
        public double flight_seconds { get; private set; } = first_flight_seconds;
        public double window_seconds { get; private set; } = first_window_seconds;

        // How long this ball has been flying.
        public double seconds_flown { get; private set; }

        // Frame times refused as broken (negative, not a number, endless), a sign of a bug.
        public int refused_count { get; private set; }

        // How far across from her hand to his mouth the ball is, 0 to 1.
        public double across => Math.Min(1.0, seconds_flown / flight_seconds);

        // How high the ball is, 0 on the ground to 1 at the top of its arc (a parabola).
        public double height => 4.0 * across * (1.0 - across);

        // True while the ball is in his reach: a tap now is a catch. The scene can light it up.
        public bool in_catch_window => ball_in_air && seconds_flown >= flight_seconds - window_seconds;

        // She throws. `catches_in_a_row` is how many he has caught without a miss so far
        // (HomeDay.fetch_catches_in_a_row), which sets how fast this throw is. False, changing
        // nothing, if a ball is already in the air.
        public bool throw_ball(int catches_in_a_row)
        {
            if (ball_in_air)
            {
                return false;
            }
            double hardness = (double)Math.Max(0, Math.Min(catches_in_a_row, hardest_after_catches)) / hardest_after_catches;
            flight_seconds = first_flight_seconds - (first_flight_seconds - fastest_flight_seconds) * hardness;
            window_seconds = first_window_seconds - (first_window_seconds - narrowest_window_seconds) * hardness;
            seconds_flown = 0;
            ball_in_air = true;
            return true;
        }

        // One frame of time. Returns missed once, on the frame the ball lands untouched.
        public FetchResult pass_time(double delta_seconds)
        {
            if (!ball_in_air)
            {
                return FetchResult.nothing;
            }
            if (double.IsNaN(delta_seconds) || double.IsInfinity(delta_seconds) || delta_seconds < 0)
            {
                refused_count += 1;
                return FetchResult.nothing;
            }
            seconds_flown = Math.Min(flight_seconds, seconds_flown + delta_seconds);
            if (seconds_flown >= flight_seconds)
            {
                ball_in_air = false;  // It landed.
                return FetchResult.missed;
            }
            return FetchResult.nothing;
        }

        // The player left the game mid-throw: the ball is put away, counting as neither a catch
        // nor a miss.
        public void put_away()
        {
            ball_in_air = false;
        }

        // The player taps: he jumps for the ball. A catch in the window, a miss before it, and
        // nothing at all with no ball in the air (so a stray tap never costs a run).
        public FetchResult tap()
        {
            if (!ball_in_air)
            {
                return FetchResult.nothing;
            }
            bool caught = in_catch_window;
            ball_in_air = false;
            return caught ? FetchResult.caught : FetchResult.missed;
        }
    }
}
