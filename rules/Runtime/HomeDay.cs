// Purpose: things to do at home with Bodhi between and after chapters, so players come back
// each day. Each day offers three small tasks ("Brush Bodhi", "Play fetch: 5 catches in a
// row"), chosen from a pool by the day number alone, so the same day gives the same three on
// every phone. Doing them grows the bond (BondJourney). Days in a row with all three done make
// a streak; missing a day resets the streak but never takes bond points away. Also keeps the
// fetch record. Days are numbers the caller passes in; the rules never read the clock.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bodhi.Rules
{
    // The things to do at home. The scene reports each one when the player does it.
    public enum HomeTask
    {
        fetch_five_in_a_row,   // Done by fetch_caught, five catches without a miss.
        brush,
        dinner,
        walk_round_the_block,
        practise_known_trick,  // Only offered once he knows a trick.
        dry_after_garden,
        find_hidden_ball,      // Played as Bodhi, sniffing it out.
        teach_new_trick,       // Only offered when a trick is unlocked but not yet taught.
        cuddle,
    }

    // What happened when the game told HomeDay the date.
    public enum DayChange
    {
        new_day_started,         // A later day: new tasks, the streak checked.
        same_day,                // Nothing to do.
        refused_went_backwards,  // An earlier day (the clock was turned back): nothing changed.
        refused_bad_day,         // Not a usable day number, or no journey given: nothing changed.
    }

    // What one thing done at home changed.
    public class HomeResult
    {
        public bool counted;                       // False if refused or there was nothing to do.
        public bool task_finished;                 // True if this finished one of today's tasks.
        public bool all_done_today;                // True if this finished the last of today's three.
        public BondChange bond = new BondChange(); // The bond points it gave.
    }

    public class HomeDay
    {
        public const int tasks_per_day = 3;
        public const int fetch_catches_needed = 5;
        public const int largest_count = 1000000;  // A ceiling for streaks and fetch records.

        // What a task needs before it can be offered.
        enum TaskNeeds
        {
            nothing,
            a_known_trick,
            a_trick_to_teach,
        }

        // One task in the pool: its words on screen, the care it counts as, and what it needs.
        readonly struct TaskRule
        {
            public readonly HomeTask task;
            public readonly string text;
            public readonly BondCareKind care;
            public readonly TaskNeeds needs;

            // Makes one row of the task table.
            public TaskRule(HomeTask task, string text, BondCareKind care, TaskNeeds needs)
            {
                this.task = task;
                this.text = text;
                this.care = care;
                this.needs = needs;
            }
        }

        // The whole pool in one table, so a designer can add or reword tasks.
        static readonly TaskRule[] task_rules =
        {
            new TaskRule(HomeTask.fetch_five_in_a_row,  "Play fetch: 5 catches in a row",                BondCareKind.play,          TaskNeeds.nothing),
            new TaskRule(HomeTask.brush,                "Brush Bodhi",                                   BondCareKind.brush,         TaskNeeds.nothing),
            new TaskRule(HomeTask.dinner,               "Give him his dinner",                           BondCareKind.feed,          TaskNeeds.nothing),
            new TaskRule(HomeTask.walk_round_the_block, "A walk round the block",                        BondCareKind.walk,          TaskNeeds.nothing),
            new TaskRule(HomeTask.practise_known_trick, "Practise a trick he knows",                     BondCareKind.trick,         TaskNeeds.a_known_trick),
            new TaskRule(HomeTask.dry_after_garden,     "Dry him after the garden",                      BondCareKind.towel_or_bath, TaskNeeds.nothing),
            new TaskRule(HomeTask.find_hidden_ball,     "Find the hidden ball (as Bodhi, sniff it out)", BondCareKind.play,          TaskNeeds.nothing),
            new TaskRule(HomeTask.teach_new_trick,      "Teach him something new",                       BondCareKind.trick,         TaskNeeds.a_trick_to_teach),
            new TaskRule(HomeTask.cuddle,               "Give him a cuddle",                             BondCareKind.pet,           TaskNeeds.nothing),
        };

        // Today's tasks and whether each is done, in the same order.
        readonly List<HomeTask> tasks = new List<HomeTask>();
        readonly List<bool> done = new List<bool>();

        // Today's day number. 0 means the game hasn't said what day it is yet.
        public int day { get; private set; }

        // Days in a row, up to the last finished day, with all three tasks done.
        public int streak { get; private set; }

        // The longest streak ever.
        public int best_streak { get; private set; }

        // The last day all three tasks were done, or 0 for never.
        public int last_complete_day { get; private set; }

        // Fetch: catches without a miss right now, the best today, and the best ever.
        public int fetch_catches_in_a_row { get; private set; }
        public int fetch_best_today { get; private set; }
        public int fetch_best_ever { get; private set; }

        // Calls refused as broken input (an unknown task, a broken day), a sign of a bug.
        public int refused_count { get; private set; }

        // Today's three tasks, in the order to show them. Empty before a day is given.
        public IReadOnlyList<HomeTask> tasks_today => tasks;

        // True once all of today's tasks are done.
        public bool all_done_today => tasks.Count > 0 && !done.Contains(false);

        // A task's words on screen, or "" for a task that isn't real.
        public static string task_text(HomeTask task)
        {
            int index = rule_index(task);
            return index < 0 ? "" : task_rules[index].text;
        }

        // True if this is one of today's tasks and it's done.
        public bool is_done(HomeTask task)
        {
            int place = tasks.IndexOf(task);
            return place >= 0 && done[place];
        }

        // How far along one of today's tasks is, 0 to 1 (only fetch has steps in between).
        public double progress(HomeTask task)
        {
            if (!tasks.Contains(task)) return 0.0;
            if (is_done(task)) return 1.0;
            if (task == HomeTask.fetch_five_in_a_row)
            {
                return (double)Math.Min(fetch_best_today, fetch_catches_needed) / fetch_catches_needed;
            }
            return 0.0;
        }

        // The game says what day it is: at start-up, on coming back to the app, and now and then
        // while it is open, so midnight rolls over. A later day brings new tasks and checks the
        // streak. An earlier day (the clock turned back) changes nothing: today's tasks stay, so
        // nothing can be done twice. `journey` says which trick tasks can be offered.
        public DayChange change_day(int new_day, BondJourney journey)
        {
            if (new_day < 1 || new_day > BondJourney.largest_day || journey == null)
            {
                refused_count += 1;
                return DayChange.refused_bad_day;
            }
            if (new_day == day) return DayChange.same_day;
            if (new_day < day) return DayChange.refused_went_backwards;
            day = new_day;
            if (last_complete_day < day - 1)
            {
                streak = 0;  // A day was missed. Only the streak resets; the bond keeps every point.
            }
            fetch_catches_in_a_row = 0;
            fetch_best_today = 0;
            tasks.Clear();
            done.Clear();
            tasks.AddRange(choose_tasks(day, journey.knows_any_trick, journey.trick_waiting_to_be_taught() != null));
            foreach (var task in tasks) done.Add(false);
            return DayChange.new_day_started;
        }

        // The player did something at home. Gives its bond points (less when repeated, as
        // BondJourney decides) and marks it done if it is one of today's tasks. Fetch is
        // counted by fetch_caught instead; teaching needs a trick waiting; practising needs one
        // he knows.
        public HomeResult do_task(HomeTask task, BondJourney journey)
        {
            var result = new HomeResult();
            int index = rule_index(task);
            if (index < 0 || journey == null)
            {
                refused_count += 1;
                return result;
            }
            if (task == HomeTask.fetch_five_in_a_row || (task == HomeTask.practise_known_trick && !journey.knows_any_trick))
            {
                return result;
            }
            result.bond = task == HomeTask.teach_new_trick ? journey.teach_next_trick(day) : journey.record_care(task_rules[index].care, day);
            result.counted = result.bond.counted;
            if (result.counted)
            {
                mark_done(task, result);
            }
            return result;
        }

        // A ball caught in the fetch game: the run of catches grows, the records with it, and it
        // counts as play. Five in a row finishes today's fetch task.
        public HomeResult fetch_caught(BondJourney journey)
        {
            var result = new HomeResult();
            if (journey == null)
            {
                refused_count += 1;
                return result;
            }
            fetch_catches_in_a_row = Math.Min(largest_count, fetch_catches_in_a_row + 1);
            fetch_best_today = Math.Max(fetch_best_today, fetch_catches_in_a_row);
            fetch_best_ever = Math.Max(fetch_best_ever, fetch_catches_in_a_row);
            result.bond = journey.record_care(BondCareKind.play, day);
            result.counted = true;
            if (fetch_catches_in_a_row >= fetch_catches_needed)
            {
                mark_done(HomeTask.fetch_five_in_a_row, result);
            }
            return result;
        }

        // The ball was dropped or missed: the run starts again. The records stay.
        public void fetch_missed()
        {
            fetch_catches_in_a_row = 0;
        }

        // Chooses a day's tasks from the day number alone, plus whether the trick tasks can be
        // done. Each task gets a score by mixing the day and its place in the pool; the three
        // usable tasks with the lowest scores win. The mixing is plain whole-number arithmetic,
        // so every phone and computer gets the same answer (System.Random is not promised to).
        public static HomeTask[] choose_tasks(int day, bool knows_a_trick, bool trick_to_teach)
        {
            var chosen = new List<HomeTask>();
            while (chosen.Count < tasks_per_day)
            {
                int best_index = -1;
                uint best_score = 0;
                for (int index = 0; index < task_rules.Length; index++)
                {
                    var rule = task_rules[index];
                    bool usable = rule.needs == TaskNeeds.nothing
                        || (rule.needs == TaskNeeds.a_known_trick && knows_a_trick)
                        || (rule.needs == TaskNeeds.a_trick_to_teach && trick_to_teach);
                    uint score = mix(unchecked((uint)day * 31u + (uint)index));
                    if (usable && !chosen.Contains(rule.task) && (best_index < 0 || score < best_score))
                    {
                        best_index = index;
                        best_score = score;
                    }
                }
                chosen.Add(task_rules[best_index].task);
            }
            return chosen.ToArray();
        }

        // Scrambles a number so that nearby days give very different scores. This is the
        // "lowbias32" mixer: shift and multiply steps, wrapping around at 2^32 (unchecked).
        static uint mix(uint value)
        {
            unchecked
            {
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                value *= 0x846ca68bu;
                value ^= value >> 16;
                return value;
            }
        }

        // The home days as text: "home day streak best_streak last_complete_day fetch_best_ever
        // fetch_best_today", then one "home_task name 0_or_1" line for each of today's tasks.
        // The fetch run in progress isn't kept; it starts again after a load.
        public string to_text()
        {
            var text = new StringBuilder("home");
            foreach (int number in new[] { day, streak, best_streak, last_complete_day, fetch_best_ever, fetch_best_today })
            {
                text.Append(' ').Append(number.ToString(CultureInfo.InvariantCulture));
            }
            text.Append('\n');
            for (int place = 0; place < tasks.Count; place++)
            {
                text.Append("home_task ").Append(tasks[place].ToString()).Append(done[place] ? " 1" : " 0").Append('\n');
            }
            return text.ToString();
        }

        // Replaces the home days with the ones in the text. All or nothing: a damaged, unknown,
        // repeated or impossible value refuses it (for example a streak longer than the best, or
        // all three done but not recorded as a complete day), and nothing changes.
        public bool load_text(string text)
        {
            if (text == null || text.Length > 1024 || !text.EndsWith("\n", StringComparison.Ordinal))
            {
                return false;
            }
            string[] lines = text.Substring(0, text.Length - 1).Split('\n');
            string[] parts = lines[0].Split(' ');
            var numbers = new int[6];
            if (parts.Length != 7 || parts[0] != "home") return false;
            for (int index = 0; index < numbers.Length; index++)
            {
                if (!int.TryParse(parts[index + 1], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[index]) || numbers[index] > BondJourney.largest_day)
                {
                    return false;
                }
            }
            int loaded_day = numbers[0], loaded_streak = numbers[1], loaded_best = numbers[2];
            int loaded_last = numbers[3], loaded_fetch_ever = numbers[4], loaded_fetch_today = numbers[5];
            bool numbers_agree = loaded_day <= BondJourney.largest_day && loaded_last <= loaded_day
                && loaded_streak <= loaded_best && loaded_best <= largest_count && loaded_streak <= loaded_last
                && (loaded_streak == 0 || loaded_last >= loaded_day - 1)
                && loaded_fetch_today <= loaded_fetch_ever && loaded_fetch_ever <= largest_count;
            int task_count = loaded_day == 0 ? 0 : tasks_per_day;
            if (!numbers_agree || lines.Length != 1 + task_count)
            {
                return false;
            }
            var loaded_tasks = new List<HomeTask>();
            var loaded_done = new List<bool>();
            for (int index = 1; index < lines.Length; index++)
            {
                string[] task_parts = lines[index].Split(' ');
                int rule = task_parts.Length == 3 && task_parts[0] == "home_task" ? rule_index_by_name(task_parts[1]) : -1;
                if (rule < 0 || loaded_tasks.Contains(task_rules[rule].task) || (task_parts[2] != "0" && task_parts[2] != "1"))
                {
                    return false;
                }
                loaded_tasks.Add(task_rules[rule].task);
                loaded_done.Add(task_parts[2] == "1");
            }
            bool all_done = loaded_tasks.Count > 0 && !loaded_done.Contains(false);
            int fetch_place = loaded_tasks.IndexOf(HomeTask.fetch_five_in_a_row);
            bool fetch_agrees = fetch_place < 0 || !loaded_done[fetch_place] || loaded_fetch_today >= fetch_catches_needed;
            if (all_done != (loaded_day > 0 && loaded_last == loaded_day) || !fetch_agrees)
            {
                return false;
            }
            day = loaded_day;
            streak = loaded_streak;
            best_streak = loaded_best;
            last_complete_day = loaded_last;
            fetch_best_ever = loaded_fetch_ever;
            fetch_best_today = loaded_fetch_today;
            fetch_catches_in_a_row = 0;
            tasks.Clear();
            tasks.AddRange(loaded_tasks);
            done.Clear();
            done.AddRange(loaded_done);
            return true;
        }

        // Finds a task's row by its saved name ("brush"), or -1. Compares names exactly, so
        // numbers or lists that Enum.TryParse would accept are refused.
        static int rule_index_by_name(string name)
        {
            for (int index = 0; index < task_rules.Length; index++)
            {
                if (task_rules[index].task.ToString() == name) return index;
            }
            return -1;
        }

        // Marks one of today's tasks done, and the streak if it was the last of the three.
        void mark_done(HomeTask task, HomeResult result)
        {
            int place = tasks.IndexOf(task);
            if (place < 0 || done[place])
            {
                return;
            }
            done[place] = true;
            result.task_finished = true;
            if (all_done_today)
            {
                streak = last_complete_day == day - 1 ? Math.Min(largest_count, streak + 1) : 1;
                best_streak = Math.Max(best_streak, streak);
                last_complete_day = day;
                result.all_done_today = true;
            }
        }

        // Where this task's row is in the pool, or -1 for a task that isn't real.
        static int rule_index(HomeTask task)
        {
            for (int index = 0; index < task_rules.Length; index++)
            {
                if (task_rules[index].task == task) return index;
            }
            return -1;
        }
    }
}
