// Purpose: turns Bodhi's feelings into how he follows her (spec R3.1, R3.2): how far behind
// he trails, how often he glances back, whether he may run ahead, and whether he is wary of
// water because he remembers being swept away. Port of core/src/companion_behavior.cpp,
// plus the story-memory part. Unity reads these numbers to steer him; this file decides them.
using System;

namespace Bodhi.Rules
{
    // A "readonly struct" is a small value that can't change after it is made, so a caller
    // can't accidentally edit the answer.
    public readonly struct CompanionBehavior
    {
        // The safe ranges every answer stays inside, the same numbers as the C++ core.
        public const double closest_follow_distance_metres = 1.0;
        public const double farthest_follow_distance_metres = 6.0;
        public const double shortest_check_back_interval_seconds = 2.0;
        public const double longest_check_back_interval_seconds = 12.0;
        // Below this much anxiety he may run ahead to explore.
        public const double run_ahead_anxiety_limit = 0.5;
        // The story-memory fact that makes him wary of water in later chapters.
        public const string swept_away_fact = "swept_away_by_river";

        public readonly double follow_distance_metres;        // How far behind her he trails.
        public readonly double check_back_interval_seconds;   // How often he glances back.
        public readonly bool may_run_ahead;                   // True if he may explore ahead.
        public readonly bool approaches_water_warily;         // True once he was swept away.

        // Makes one answer. Private: only choose() makes them, so every answer is in range.
        CompanionBehavior(double follow_distance_metres, double check_back_interval_seconds, bool may_run_ahead, bool approaches_water_warily)
        {
            this.follow_distance_metres = follow_distance_metres;
            this.check_back_interval_seconds = check_back_interval_seconds;
            this.may_run_ahead = may_run_ahead;
            this.approaches_water_warily = approaches_water_warily;
        }

        // Chooses his behaviour from his feelings and what he remembers. The stronger of fear
        // and closeness need is his "anxiety"; calm means far and relaxed, anxious means close
        // and watchful, with a straight-line blend between. Trust doesn't change following.
        // A missing bond or a broken feeling counts as most anxious: when in doubt, stay close.
        public static CompanionBehavior choose(BondState bond, StoryMemory memory)
        {
            double anxiety = 1.0;
            if (bond != null)
            {
                anxiety = Math.Max(feeling_or_most_anxious(bond.closeness_need), feeling_or_most_anxious(bond.fear));
            }
            double follow_distance = farthest_follow_distance_metres + (closest_follow_distance_metres - farthest_follow_distance_metres) * anxiety;
            double check_back_interval = longest_check_back_interval_seconds + (shortest_check_back_interval_seconds - longest_check_back_interval_seconds) * anxiety;
            bool wary_of_water = memory != null && memory.knows(swept_away_fact);
            return new CompanionBehavior(follow_distance, check_back_interval, anxiety < run_ahead_anxiety_limit, wary_of_water);
        }

        // Keeps a feeling between 0 and 1. NaN ("not a number") becomes 1, most anxious.
        static double feeling_or_most_anxious(double feeling)
        {
            if (double.IsNaN(feeling))
            {
                return 1.0;
            }
            return Math.Clamp(feeling, 0.0, 1.0);
        }
    }
}
