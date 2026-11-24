// Purpose: how trust changes the way Bodhi plays (spec R4.2), in one tested place. With more
// trust he answers the whistle from farther away, holds "wait" longer in puzzles, and comes to
// her for contact more often. The numbers are starting points to tune in playtests.
using System;

namespace Bodhi.Rules
{
    public static class BondEffects
    {
        // How far away (in metres) he still answers her whistle: 8 at no trust, 20 at full trust.
        public static double whistle_answer_range_metres(double trust)
        {
            return 8.0 + 12.0 * safe_trust(trust);
        }

        // How long (in seconds) he holds "wait" in a puzzle: 4 at no trust, 12 at full trust.
        public static double wait_hold_seconds(double trust)
        {
            return 4.0 + 8.0 * safe_trust(trust);
        }

        // How often (in seconds) he comes to her for a touch when nothing else is happening:
        // every 40 seconds at no trust, every 15 at full trust.
        public static double contact_seek_interval_seconds(double trust)
        {
            return 40.0 - 25.0 * safe_trust(trust);
        }

        // Keeps trust between 0 and 1. Not a number becomes 0, so a broken value is never generous.
        static double safe_trust(double trust)
        {
            return double.IsNaN(trust) ? 0.0 : Math.Clamp(trust, 0.0, 1.0);
        }
    }
}
