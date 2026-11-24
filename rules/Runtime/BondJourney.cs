// Purpose: the long bond between Em and Bodhi across the whole year. BondState is how he feels
// right now inside one chapter and starts fresh each chapter; this is the slow, lasting part:
// points that grow from ordinary affection (feeding, brushing, a cuddle, a game of fetch),
// five warmly named levels, and a trick unlocked at each level. The same care on the same day
// gives less and then nothing, so it can't be farmed. Days are numbers the caller passes in
// (the phone's local date as a count of days); the rules never read the clock.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bodhi.Rules
{
    // The kinds of affection that grow the bond.
    public enum BondCareKind
    {
        feed,              // A meal, in a chapter or at home.
        towel_or_bath,     // Towelled dry or bathed.
        brush,             // A grooming moment.
        pet,               // A cuddle: she kneels and he leans in.
        play,              // A game of fetch caught, or the hidden ball found.
        walk,              // A walk at home finished.
        trick,             // A trick practised or taught.
        chapter_finished,  // A chapter of the story finished together.
        reunion_made,      // Found each other again after a separation.
        keepsake_found,    // A keepsake found.
    }

    // What one piece of care changed, so the game can show a heart pop or "Bodhi learned Spin!".
    public class BondChange
    {
        public bool counted;                          // False if it was refused or there was nothing to do.
        public int points_gained;                     // 0 once today's allowance for this care is used.
        public int level_before;                      // The level before this care.
        public int level_after;                       // The level after it.
        public string[] new_unlocks = new string[0];  // Tricks this care unlocked (ids, e.g. "spin").
        public string newly_taught;                   // The trick he just learned, or null.

        // True if this care took him up a level, which the game can celebrate.
        public bool levelled_up => level_after > level_before;
    }

    public class BondJourney
    {
        // The largest day number accepted. Days counted from 1 January in year 1 reach about
        // 3.65 million by the year 9999, so this leaves room and still refuses nonsense.
        public const int largest_day = 4000000;

        // The most points kept, so years of play (or a damaged save) can't overflow the number.
        public const int largest_points = 1000000;

        // How much one kind of care is worth, and how often per day it gives that much.
        readonly struct CareRule
        {
            public readonly BondCareKind kind;
            public readonly int full_points;            // Points each of the first times today.
            public readonly int full_times_per_day;     // How many times today give full points.
            public readonly int reduced_points;         // Points each time after that...
            public readonly int reduced_times_per_day;  // ...for this many more times; then nothing.

            // Makes one row of the care table.
            public CareRule(BondCareKind kind, int full_points, int full_times_per_day, int reduced_points, int reduced_times_per_day)
            {
                this.kind = kind;
                this.full_points = full_points;
                this.full_times_per_day = full_times_per_day;
                this.reduced_points = reduced_points;
                this.reduced_times_per_day = reduced_times_per_day;
            }
        }

        // Every kind of care in one table, so a designer can tune the bond without reading logic.
        // Story moments (chapters, reunions, keepsakes) happen once each, so they never shrink.
        static readonly CareRule[] care_rules =
        {
            //            kind                         full  times  reduced  times
            new CareRule(BondCareKind.feed,               4,    2,      2,      1),
            new CareRule(BondCareKind.towel_or_bath,      4,    2,      2,      1),
            new CareRule(BondCareKind.brush,              4,    2,      2,      2),
            new CareRule(BondCareKind.pet,                2,    3,      1,      5),
            new CareRule(BondCareKind.play,               3,    3,      1,      5),
            new CareRule(BondCareKind.walk,               5,    2,      2,      1),
            new CareRule(BondCareKind.trick,              3,    3,      1,      3),
            new CareRule(BondCareKind.chapter_finished,  30,   12,      0,      0),
            new CareRule(BondCareKind.reunion_made,      15,   12,      0,      0),
            new CareRule(BondCareKind.keepsake_found,     5,   40,      0,      0),
        };

        // One level of the bond: the points it starts at, its name, and the trick it unlocks.
        readonly struct BondLevel
        {
            public readonly int points_needed;
            public readonly string name;
            public readonly string trick;          // The trick's id, as saved.
            public readonly string trick_display;  // The trick's name on screen.

            // Makes one row of the level table.
            public BondLevel(int points_needed, string name, string trick, string trick_display)
            {
                this.points_needed = points_needed;
                this.name = name;
                this.trick = trick;
                this.trick_display = trick_display;
            }
        }

        // The levels in order, lowest first. Level 1 starts at 0 points.
        static readonly BondLevel[] levels =
        {
            //            points  name                    trick        on screen
            new BondLevel(   0, "Getting to know you",  "sit",       "Sit"),
            new BondLevel(  60, "Friends",              "high_five", "High five"),
            new BondLevel( 200, "Best friends",         "spin",      "Spin"),
            new BondLevel( 450, "Family",               "bow",       "Bow"),
            new BondLevel( 800, "Inseparable",          "hug",       "Hug"),
        };

        // The highest level there is.
        public static int highest_level => levels.Length;

        // How many times each kind of care has been given on the counting day, in table order.
        readonly int[] times_today = new int[care_rules.Length];

        // The tricks he has been taught, kept in a fixed order so the save is always the same.
        readonly SortedSet<string> taught_tricks = new SortedSet<string>(StringComparer.Ordinal);

        // All the bond points earned so far.
        public int points { get; private set; }

        // The newest day seen; today's allowances belong to it. 0 means no day has been given yet.
        public int counting_day { get; private set; }

        // Calls refused as broken input (an unknown kind, a broken day), a sign of a bug.
        public int refused_count { get; private set; }

        // The current level, from 1 to highest_level.
        public int level_number => level_for(points);

        // The current level's name, e.g. "Best friends".
        public string level_name => levels[level_number - 1].name;

        // How far from this level to the next, 0 to 1. The top level reads as full (1).
        public double progress_to_next_level
        {
            get
            {
                int level = level_number;
                if (level >= levels.Length)
                {
                    return 1.0;
                }
                int start = levels[level - 1].points_needed;
                int next = levels[level].points_needed;
                return (double)(points - start) / (next - start);
            }
        }

        // True once he has been taught at least one trick (for "Practise a trick he knows").
        public bool knows_any_trick => taught_tricks.Count > 0;

        // The full points one kind of care is worth, for the game's hints and for tests.
        public static int full_points_for(BondCareKind kind)
        {
            int index = rule_index(kind);
            return index < 0 ? 0 : care_rules[index].full_points;
        }

        // A trick's name on screen ("spin" gives "Spin"), or null for a name that isn't a trick.
        public static string trick_display_name(string trick)
        {
            foreach (var level in levels)
            {
                if (level.trick == trick) return level.trick_display;
            }
            return null;
        }

        // Which kind of care a finished care moment was, so the scene can record it here.
        public static BondCareKind kind_for_care_moment(CareMomentKind moment)
        {
            switch (moment)
            {
                case CareMomentKind.feeding: return BondCareKind.feed;
                case CareMomentKind.grooming: return BondCareKind.brush;
                default: return BondCareKind.towel_or_bath;  // Drying and bathing.
            }
        }

        // How many times this kind of care has been given today (0 for an unknown kind).
        public int times_recorded_today(BondCareKind kind)
        {
            int index = rule_index(kind);
            return index < 0 ? 0 : times_today[index];
        }

        // The tricks unlocked so far, by level, as ids ("sit", "high_five", ...).
        public List<string> unlocks_earned()
        {
            var unlocked = new List<string>();
            for (int level = 1; level <= level_number; level++)
            {
                unlocked.Add(levels[level - 1].trick);
            }
            return unlocked;
        }

        // True if he has been taught this trick.
        public bool knows_trick(string trick)
        {
            return trick != null && taught_tricks.Contains(trick);
        }

        // The first trick unlocked but not yet taught, or null if there's nothing new to teach.
        public string trick_waiting_to_be_taught()
        {
            foreach (string trick in unlocks_earned())
            {
                if (!taught_tricks.Contains(trick)) return trick;
            }
            return null;
        }

        // Records one piece of care on a day and returns what it changed. Refuses (and counts)
        // an unknown kind or a day outside 0 to largest_day. A day earlier than the newest one
        // seen (the phone's clock turned back) counts against the newest day's allowance, so
        // going back and forth between days can't open fresh allowances.
        public BondChange record_care(BondCareKind kind, int day)
        {
            var change = new BondChange { level_before = level_number, level_after = level_number };
            int index = rule_index(kind);
            if (index < 0 || !is_usable_day(day))
            {
                refused_count += 1;
                return change;
            }
            move_to_day(day);
            var rule = care_rules[index];
            int times = times_today[index];
            int earned = 0;
            if (times < rule.full_times_per_day)
            {
                earned = rule.full_points;
            }
            else if (times < rule.full_times_per_day + rule.reduced_times_per_day)
            {
                earned = rule.reduced_points;
            }
            // Stop counting once the allowance is used, so the count can't grow without end.
            times_today[index] = Math.Min(times + 1, rule.full_times_per_day + rule.reduced_times_per_day);
            int new_points = Math.Min(largest_points, points + earned);
            change.counted = true;
            change.points_gained = new_points - points;
            points = new_points;
            change.level_after = level_number;
            change.new_unlocks = tricks_between(change.level_before, change.level_after);
            return change;
        }

        // Teaches him the next trick waiting (see trick_waiting_to_be_taught), which counts as
        // practising a trick. Not counted if nothing is waiting or the day is broken.
        public BondChange teach_next_trick(int day)
        {
            string trick = trick_waiting_to_be_taught();
            if (trick == null && is_usable_day(day))
            {
                return new BondChange { level_before = level_number, level_after = level_number };
            }
            var change = record_care(BondCareKind.trick, day);
            if (change.counted)
            {
                taught_tricks.Add(trick);
                change.newly_taught = trick;
            }
            return change;
        }

        // The journey as text: "bond points day" then today's count for each kind in table
        // order, then one "trick name" line for each trick he knows.
        public string to_text()
        {
            var text = new StringBuilder();
            text.Append("bond ").Append(points.ToString(CultureInfo.InvariantCulture))
                .Append(' ').Append(counting_day.ToString(CultureInfo.InvariantCulture));
            foreach (int times in times_today)
            {
                text.Append(' ').Append(times.ToString(CultureInfo.InvariantCulture));
            }
            text.Append('\n');
            foreach (string trick in taught_tricks)
            {
                text.Append("trick ").Append(trick).Append('\n');
            }
            return text.ToString();
        }

        // Replaces the journey with the one in the text. All or nothing: a damaged, unknown,
        // repeated or impossible value (a trick not yet unlocked) refuses it, and nothing changes.
        public bool load_text(string text)
        {
            if (text == null || text.Length > 4096 || !text.EndsWith("\n", StringComparison.Ordinal))
            {
                return false;
            }
            string[] lines = text.Substring(0, text.Length - 1).Split('\n');
            string[] parts = lines[0].Split(' ');
            if (parts.Length != 3 + care_rules.Length || parts[0] != "bond"
                || !read_whole_number(parts[1], largest_points, out int loaded_points)
                || !read_whole_number(parts[2], largest_day, out int loaded_day))
            {
                return false;
            }
            var loaded_times = new int[care_rules.Length];
            for (int index = 0; index < care_rules.Length; index++)
            {
                var rule = care_rules[index];
                if (!read_whole_number(parts[3 + index], rule.full_times_per_day + rule.reduced_times_per_day, out loaded_times[index]))
                {
                    return false;
                }
            }
            var loaded_tricks = new SortedSet<string>(StringComparer.Ordinal);
            int loaded_level = level_for(loaded_points);
            for (int index = 1; index < lines.Length; index++)
            {
                string line = lines[index];
                if (!line.StartsWith("trick ", StringComparison.Ordinal))
                {
                    return false;
                }
                string trick = line.Substring("trick ".Length);
                if (unlock_level(trick) < 1 || unlock_level(trick) > loaded_level || !loaded_tricks.Add(trick))
                {
                    return false;
                }
            }
            points = loaded_points;
            counting_day = loaded_day;
            Array.Copy(loaded_times, times_today, times_today.Length);
            taught_tricks.Clear();
            taught_tricks.UnionWith(loaded_tricks);
            return true;
        }

        // Starts a fresh day's allowances when the day is newer than the counting day.
        void move_to_day(int day)
        {
            if (day > counting_day)
            {
                counting_day = day;
                Array.Clear(times_today, 0, times_today.Length);
            }
        }

        // True for a day number the journey accepts.
        static bool is_usable_day(int day)
        {
            return day >= 0 && day <= largest_day;
        }

        // Where this kind's row is in the care table, or -1 for a kind that isn't real.
        static int rule_index(BondCareKind kind)
        {
            for (int index = 0; index < care_rules.Length; index++)
            {
                if (care_rules[index].kind == kind) return index;
            }
            return -1;
        }

        // The level these points reach: the highest level whose starting points they cover.
        static int level_for(int points)
        {
            int level = 1;
            for (int index = 0; index < levels.Length; index++)
            {
                if (points >= levels[index].points_needed) level = index + 1;
            }
            return level;
        }

        // The level that unlocks this trick, or 0 if it isn't a trick.
        static int unlock_level(string trick)
        {
            for (int index = 0; index < levels.Length; index++)
            {
                if (levels[index].trick == trick) return index + 1;
            }
            return 0;
        }

        // The tricks unlocked by climbing from one level to a higher one (usually none or one).
        static string[] tricks_between(int level_before, int level_after)
        {
            var unlocked = new List<string>();
            for (int level = level_before + 1; level <= level_after; level++)
            {
                unlocked.Add(levels[level - 1].trick);
            }
            return unlocked.ToArray();
        }

        // Reads a whole number from 0 to `largest`, written with digits only (no sign, no spaces).
        static bool read_whole_number(string text, int largest, out int value)
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value <= largest;
        }
    }
}
