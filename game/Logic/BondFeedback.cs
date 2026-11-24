// Purpose: the words the game shows about the bond between Em and Bodhi and about the home days:
// the little heart pop when care counts, "Bodhi learned Sit!", the line when they reach a new
// level, today's tasks with their progress, the streak and the fetch record. Kept out of Unity
// so the wording is tested and can be changed in one place. Short and warm on purpose: these
// lines pop up over play and should never get in the way.
using System.Collections.Generic;
using Bodhi.Rules;

namespace Bodhi.Game
{
    public static class BondFeedback
    {
        // The heart pop for one piece of care, e.g. "♥ +4", or "" when it gave no points (today's
        // allowance for that care is used up, or it was refused).
        public static string heart_pop(BondChange change)
        {
            if (change == null || !change.counted || change.points_gained <= 0)
            {
                return "";
            }
            return "♥ +" + change.points_gained;
        }

        // The lines worth celebrating in one bond change, in the order to show them: a trick he
        // just learned, the new level, then tricks he's now ready to learn. Empty when there's
        // nothing to say.
        public static List<string> lines_for(BondChange change, string dog_name)
        {
            var lines = new List<string>();
            if (change == null || !change.counted)
            {
                return lines;
            }
            string learned = BondJourney.trick_display_name(change.newly_taught);
            if (learned != null)
            {
                lines.Add(dog_name + " learned " + learned + "!");
            }
            string level = BondJourney.level_name_for(change.level_after);
            if (change.levelled_up && level != null)
            {
                lines.Add(StoryText.her_name + " and " + dog_name + " are " + level.ToLowerInvariant() + " now.");
            }
            foreach (string trick in change.new_unlocks ?? new string[0])
            {
                string trick_name = BondJourney.trick_display_name(trick);
                if (trick_name != null)
                {
                    lines.Add(dog_name + " is ready to learn " + trick_name + ". Teach him at home.");
                }
            }
            return lines;
        }

        // The lines for something done at home: the bond's lines, then a happy line once all
        // three of today's tasks are done, and the streak once it's more than one day.
        public static List<string> lines_for_home(HomeResult result, HomeDay home, string dog_name)
        {
            if (result == null || home == null)
            {
                return new List<string>();
            }
            var lines = lines_for(result.bond, dog_name);
            if (result.all_done_today)
            {
                lines.Add("All three done. " + dog_name + "'s tail hasn't stopped.");
                if (home.streak > 1)
                {
                    lines.Add("That's " + home.streak + " days in a row.");
                }
            }
            return lines;
        }

        // What the next level is called, or a quiet line at the top level.
        public static string next_level_text(BondJourney journey)
        {
            if (journey == null)
            {
                return "";
            }
            string next = BondJourney.level_name_for(journey.level_number + 1);
            return next == null ? "The closest there is" : "Next: " + next;
        }

        // The tricks he knows, and the one he's ready to learn: "Knows: Sit. Ready to learn: Spin".
        public static string tricks_text(BondJourney journey)
        {
            if (journey == null)
            {
                return "";
            }
            var known = new List<string>();
            foreach (string trick in journey.unlocks_earned())
            {
                if (journey.knows_trick(trick))
                {
                    known.Add(BondJourney.trick_display_name(trick));
                }
            }
            var parts = new List<string>();
            if (known.Count > 0)
            {
                parts.Add("Knows: " + string.Join(", ", known));
            }
            string waiting = BondJourney.trick_display_name(journey.trick_waiting_to_be_taught());
            if (waiting != null)
            {
                parts.Add("Ready to learn: " + waiting);
            }
            return string.Join(". ", parts);
        }

        // One of today's tasks as it reads on the home screen: its words, how far fetch has got
        // ("(2/5)"), or " - done" once done. "" for a task that isn't real.
        public static string task_label(HomeDay home, HomeTask task)
        {
            string text = HomeDay.task_text(task);
            if (home == null || text == "")
            {
                return "";
            }
            if (home.is_done(task))
            {
                return text + " - done";
            }
            if (task == HomeTask.fetch_five_in_a_row)
            {
                int caught = System.Math.Min(home.fetch_best_today, HomeDay.fetch_catches_needed);
                return text + " (" + caught + "/" + HomeDay.fetch_catches_needed + ")";
            }
            return text;
        }

        // The word on a task's button: what pressing it does, or "Done" once it's done. Fetch can
        // always be played again, for the records.
        public static string task_button(HomeDay home, HomeTask task)
        {
            if (home != null && home.is_done(task))
            {
                return task == HomeTask.fetch_five_in_a_row ? "Play again" : "Done";
            }
            switch (task)
            {
                case HomeTask.fetch_five_in_a_row: return "Play";
                case HomeTask.brush: return "Brush";
                case HomeTask.dinner: return "Feed";
                case HomeTask.walk_round_the_block: return "Walk";
                case HomeTask.practise_known_trick: return "Practise";
                case HomeTask.dry_after_garden: return "Dry";
                case HomeTask.find_hidden_ball: return "Sniff";
                case HomeTask.teach_new_trick: return "Teach";
                case HomeTask.cuddle: return "Cuddle";
                default: return "";
            }
        }

        // The heading over today's tasks: an invitation, or a goodbye once all three are done.
        // "" before the game has said what day it is (no tasks yet).
        public static string today_heading(HomeDay home, string dog_name)
        {
            if (home == null || home.tasks_today.Count == 0)
            {
                return "";
            }
            return home.all_done_today ? "All done today. " + dog_name + " will see you tomorrow." : "Today with " + dog_name;
        }

        // The streak in a few words, with the best ever when it's longer.
        public static string streak_text(HomeDay home)
        {
            if (home == null)
            {
                return "";
            }
            if (home.streak == 0)
            {
                return "Do all three today to start a streak";
            }
            string text = home.streak == 1 ? "1 day in a row" : home.streak + " days in a row";
            return home.best_streak > home.streak ? text + " (best " + home.best_streak + ")" : text;
        }

        // The fetch record under the fetch game.
        public static string fetch_text(HomeDay home)
        {
            if (home == null)
            {
                return "";
            }
            return "In a row: " + home.fetch_catches_in_a_row + "   Best today: " + home.fetch_best_today + "   Best ever: " + home.fetch_best_ever;
        }

        // What one throw did, in a few words.
        public static string fetch_line(FetchResult result)
        {
            switch (result)
            {
                case FetchResult.caught: return "Caught it!";
                case FetchResult.missed: return "Missed. Throw again.";
                default: return "";
            }
        }
    }
}
