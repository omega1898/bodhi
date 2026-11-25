// Purpose: Bodhi's feelings toward her, stored as numbers. Animation and his companion
// behaviour read these numbers; story events change them. For example, after a frightening
// separation his closeness need stays high, so he stays close.
// A port of core/src/bond_state.cpp. Plain C#: no Unity code.
using System;

namespace Bodhi.Rules
{
    // Each feeling is kept between 0.0 (none) and 1.0 (as strong as possible).
    public class BondState
    {
        public double closeness_need = 0.2;  // How strongly he wants to stay near her.
        public double fear = 0.0;            // How frightened he is right now.
        public double trust = 0.8;           // How readily he follows her calls.

        // How one story event shifts his feelings. Positive numbers raise a feeling.
        readonly struct BondRule
        {
            public readonly string event_name;
            public readonly double fear_change;
            public readonly double closeness_need_change;
            public readonly double trust_change;
            public readonly double minimum_closeness_need;  // After this event, closeness need is at least this.

            // Makes one row of the rule table.
            public BondRule(string event_name, double fear_change, double closeness_need_change, double trust_change, double minimum_closeness_need)
            {
                this.event_name = event_name;
                this.fear_change = fear_change;
                this.closeness_need_change = closeness_need_change;
                this.trust_change = trust_change;
                this.minimum_closeness_need = minimum_closeness_need;
            }
        }

        // The whole rule set in one table, so a designer can tune it without reading logic.
        // The same numbers as the C++ core.
        static readonly BondRule[] bond_rules =
        {
            //            event                   fear  closeness trust  minimum closeness
            new BondRule("walkway_collapsed",     0.6,   0.3,     0.0,   0.0),
            new BondRule("whistle_heard",        -0.1,   0.0,     0.05,  0.0),
            new BondRule("glove_found",          -0.1,   0.0,     0.0,   0.0),
            new BondRule("fence_touch_finished", -0.2,   0.0,     0.1,   0.0),
            new BondRule("reunion_contact_made", -0.5,   0.0,     0.1,   0.9),
            new BondRule("rested_together",      -0.2,  -0.2,     0.0,   0.0),
        };

        // Changes the bond in response to a story event. Unknown or missing names change nothing.
        public void apply_event(string event_name)
        {
            foreach (var rule in bond_rules)
            {
                if (event_name != rule.event_name)
                {
                    continue;
                }
                fear = keep_in_unit_range(fear + rule.fear_change);
                closeness_need = keep_in_unit_range(Math.Max(closeness_need + rule.closeness_need_change, rule.minimum_closeness_need));
                trust = keep_in_unit_range(trust + rule.trust_change);
                return;
            }
        }

        // Where his feelings drift back to when nothing is happening (spec R3.2).
        public const double resting_closeness_need = 0.2;
        public const double resting_fear = 0.0;

        // How long it takes a raised feeling to get halfway back to rest. Fear fades faster
        // than his need to stay near her, so after a scare he stays close a while after he calms.
        public const double closeness_need_half_life_seconds = 180.0;
        public const double fear_half_life_seconds = 60.0;

        // Lets time pass: raised feelings drift back toward rest. Feelings already calmer than
        // rest, and trust, are left alone. Broken, negative or endless times change nothing.
        // Halving by a fixed fraction per second means many small steps give the same answer as
        // one big step, so the frame rate can't change how fast he settles.
        public void settle(double delta_seconds)
        {
            if (!double.IsFinite(delta_seconds) || delta_seconds <= 0)
            {
                return;
            }
            closeness_need = drift_toward_rest(closeness_need, resting_closeness_need, delta_seconds, closeness_need_half_life_seconds);
            fear = drift_toward_rest(fear, resting_fear, delta_seconds, fear_half_life_seconds);
        }

        // Moves a feeling that is above rest part of the way back down. Math.Pow(0.5, n) is
        // "halve n times", so after one half life the gap above rest is half as big.
        static double drift_toward_rest(double value, double rest, double delta_seconds, double half_life_seconds)
        {
            if (!(value > rest))
            {
                return value;  // Already at or below rest, or not a number: leave it alone.
            }
            double remaining_fraction = Math.Pow(0.5, delta_seconds / half_life_seconds);
            return rest + (value - rest) * remaining_fraction;
        }

        // Keeps a feeling inside 0.0 to 1.0 however many events pile up. A broken number
        // (NaN, "not a number", for example from a damaged save) becomes 0.0 so it can't spread.
        static double keep_in_unit_range(double value)
        {
            if (double.IsNaN(value))
            {
                return 0.0;
            }
            return Math.Clamp(value, 0.0, 1.0);
        }
    }
}
