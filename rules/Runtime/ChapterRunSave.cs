// Purpose: saving in the middle of a chapter and carrying on later, as the browser game's
// save_game.js does. A phone can close the game at any moment, so the player must come back to
// where they were. A save doesn't copy the whole chapter: it lists the story events so far, in
// order, plus who the player controls, how Bodhi feels, his care and where the two of them
// stand. Resuming builds the chapter fresh and quietly replays those events through the same
// rules, so gates open and control passes exactly as the first time.
//
// The save lives on the player's phone, where it can be edited, cut short or left by another
// version of the game, so resuming treats it as hostile: anything damaged refuses the whole save.
//
// Example save:
//   bodhi_chapter 1
//   chapter the_storm
//   controlled dog
//   bond 0.35 0.6
//   care 0.3 0.2 2
//   place human 1385 300
//   place dog 1640 410
//   revealed keepsake_lead
//   event level_started
//   event storm_done
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bodhi.Rules
{
    // Where the two of them stood when the chapter was saved, in the chapter's layout. The scene
    // checks these are somewhere a character can stand before putting them there.
    public readonly struct SavedPlaces
    {
        public readonly double human_x;
        public readonly double human_feet_y;
        public readonly double dog_x;
        public readonly double dog_feet_y;

        // Makes a set of places.
        public SavedPlaces(double human_x, double human_feet_y, double dog_x, double dog_feet_y)
        {
            this.human_x = human_x;
            this.human_feet_y = human_feet_y;
            this.dog_x = dog_x;
            this.dog_feet_y = dog_feet_y;
        }
    }

    public partial class ChapterRun
    {
        public const int save_format_version = 1;
        public const int maximum_save_characters = 16000;  // A real save is about 2000.
        const double largest_place = 1000000;               // Far bigger than any chapter.

        // True when now can be saved: no film scene playing, no reunion under way, not over.
        // Those moments can't be rebuilt from a list of events.
        public bool can_save => !cutscene_playing && !is_complete
            && (reunion_stage == ReunionStage.none || reunion_stage == ReunionStage.done);

        // True if Bodhi has sniffed out this hidden thing.
        public bool is_revealed(string reveal_name)
        {
            return reveal_name != null && revealed.Contains(reveal_name);
        }

        // The save, as text, or null when now isn't a moment that can be saved. Events no story
        // rule reads are left out: they change nothing, and a save naming them wouldn't load.
        public string to_save_text(SavedPlaces places)
        {
            if (!can_save)
            {
                return null;
            }
            var known = known_events(script);
            var text = new StringBuilder();
            text.Append("bodhi_chapter ").Append(save_format_version).Append('\n');
            text.Append("chapter ").Append(script.id).Append('\n');
            text.Append("controlled ").Append(controlled_character).Append('\n');
            text.Append("bond ").Append(number(bond.fear)).Append(' ').Append(number(bond.closeness_need)).Append('\n');
            text.Append("care ").Append(number(care.hunger)).Append(' ').Append(number(care.dirt)).Append(' ')
                .Append(care.food.ToString(CultureInfo.InvariantCulture)).Append('\n');
            text.Append("place human ").Append(number(places.human_x)).Append(' ').Append(number(places.human_feet_y)).Append('\n');
            text.Append("place dog ").Append(number(places.dog_x)).Append(' ').Append(number(places.dog_feet_y)).Append('\n');
            var sorted_revealed = new List<string>(revealed);
            sorted_revealed.Sort(StringComparer.Ordinal);
            foreach (var name in sorted_revealed)
            {
                text.Append("revealed ").Append(name).Append('\n');
            }
            foreach (var name in order_of_events)
            {
                if (known.Contains(name))
                {
                    text.Append("event ").Append(name).Append('\n');
                }
            }
            return text.ToString();
        }

        // Builds this chapter again from a save. Returns null, and gives back no places, if any
        // check fails: wrong chapter, unknown or repeated events, values out of range, or a
        // story state that saving never leaves (in a film scene, or playing him too early).
        public static ChapterRun resume(ChapterScript script, string text, out SavedPlaces places)
        {
            places = default;
            if (script == null || string.IsNullOrEmpty(text) || text.Length > maximum_save_characters
                || !text.EndsWith("\n", StringComparison.Ordinal))
            {
                return null;
            }
            string[] lines = text.Substring(0, text.Length - 1).Split('\n');
            if (lines.Length < 8 || lines[0] != "bodhi_chapter " + save_format_version || lines[1] != "chapter " + script.id
                || !read_character(lines[2], out Character controlled)
                || !read_numbers(lines[3], "bond ", 2, out double[] feelings)
                || !read_care(lines[4], out CareState saved_care)
                || !read_numbers(lines[5], "place human ", 2, out double[] human_place)
                || !read_numbers(lines[6], "place dog ", 2, out double[] dog_place)
                || !within_unit(feelings[0]) || !within_unit(feelings[1])
                || !within_place(human_place) || !within_place(dog_place))
            {
                return null;
            }
            var hidden_names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var thing in script.things ?? Array.Empty<ChapterThing>())
            {
                if (thing != null && thing.hidden) hidden_names.Add(thing.reveal_name ?? "");
            }
            var known = known_events(script);
            var ending = ending_events(script);
            var saved_revealed = new HashSet<string>(StringComparer.Ordinal);
            var events = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 7; index < lines.Length; index++)
            {
                string line = lines[index];
                if (line.StartsWith("revealed ", StringComparison.Ordinal))
                {
                    string name = line.Substring("revealed ".Length);
                    if (!hidden_names.Contains(name) || !saved_revealed.Add(name)) return null;
                }
                else if (line.StartsWith("event ", StringComparison.Ordinal))
                {
                    string name = line.Substring("event ".Length);
                    if (!known.Contains(name) || ending.Contains(name) || !seen.Add(name)) return null;
                    events.Add(name);
                }
                else
                {
                    return null;
                }
            }
            if (events.Count == 0 || events[0] != "level_started")
            {
                return null;
            }
            var run = new ChapterRun(script);
            run.replay(events);
            // A save is only ever made outside films and mid-reunion moments; and the player
            // controls him only where the story has handed him over or allows switching.
            bool reunion_ok = run.reunion_stage == ReunionStage.none || run.reunion_stage == ReunionStage.done;
            if (run.cutscene_playing || !reunion_ok || (controlled != run.controlled_character && !run.switching_allowed))
            {
                return null;
            }
            run.controlled_character = controlled;
            run.bond.fear = feelings[0];
            run.bond.closeness_need = feelings[1];
            run.care = saved_care;
            run.revealed.UnionWith(saved_revealed);
            places = new SavedPlaces(human_place[0], human_place[1], dog_place[0], dog_place[1]);
            return run;
        }

        // Fires the saved events quietly: lasting changes only, no lines. Keepsakes come back
        // from the events of the things that hold them. A finished reunion stays finished.
        void replay(List<string> events)
        {
            replaying = true;
            foreach (var name in events)
            {
                fire_event(name);
                var thing = find_thing(name);
                if (thing != null && !string.IsNullOrEmpty(thing.keepsake))
                {
                    keepsakes.Add(thing.keepsake);
                }
            }
            replaying = false;
            if (happened.Contains("reunion_complete"))
            {
                reunion_stage = ReunionStage.done;
                dog_follows = true;
            }
        }

        // Every event that can matter in this chapter: what its things, zones, timers and rules
        // name, plus what this class announces (calls, answers and sniffs for every beat).
        static HashSet<string> known_events(ChapterScript script)
        {
            var names = new HashSet<string>(engine_event_names, StringComparer.Ordinal);
            foreach (var thing in script.things ?? Array.Empty<ChapterThing>())
            {
                if (thing != null) names.Add(thing.event_name ?? "");
            }
            foreach (var zone in script.zones ?? Array.Empty<ChapterZone>())
            {
                if (zone != null) names.Add(zone.event_name ?? "");
            }
            foreach (var rule in script.rules ?? Array.Empty<StoryRule>())
            {
                if (rule == null) continue;
                names.Add(rule.on ?? "");
                names.Add(rule.schedule_event ?? "");
                names.Add(rule.cutscene_skip_to ?? "");
                names.UnionWith(rule.when_all ?? Array.Empty<string>());
                names.UnionWith(rule.unless ?? Array.Empty<string>());
            }
            foreach (Character who in Enum.GetValues(typeof(Character)))
            {
                names.Add(who + "_called");
                names.Add(who + "_answered");
                foreach (ChapterBeat beat in Enum.GetValues(typeof(ChapterBeat)))
                {
                    names.Add(who + "_called_during_" + beat);
                }
            }
            foreach (ChapterBeat beat in Enum.GetValues(typeof(ChapterBeat)))
            {
                names.Add("dog_sniffed_during_" + beat);
            }
            names.Remove("");
            return names;
        }

        // The events that end the chapter. A save never holds one: nothing is saved once the
        // chapter has ended, so one in a save means it was edited. This also means a replay can
        // never end the chapter.
        static HashSet<string> ending_events(ChapterScript script)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in script.rules ?? Array.Empty<StoryRule>())
            {
                if (rule == null || !rule.complete) continue;
                if (!string.IsNullOrEmpty(rule.on)) names.Add(rule.on);
                names.UnionWith(rule.when_all ?? Array.Empty<string>());
            }
            return names;
        }

        // Writes a number the same way on every computer, exactly enough to read back the same.
        static string number(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        // Reads "controlled human" or "controlled dog".
        static bool read_character(string line, out Character who)
        {
            who = Character.human;
            if (line == "controlled human") return true;
            who = Character.dog;
            return line == "controlled dog";
        }

        // Reads a line of `count` numbers after `start`, for example "bond 0.1 0.3". The callers
        // check each number's range.
        static bool read_numbers(string line, string start, int count, out double[] values)
        {
            values = new double[count];
            if (!line.StartsWith(start, StringComparison.Ordinal))
            {
                return false;
            }
            string[] parts = line.Substring(start.Length).Split(' ');
            if (parts.Length != count)
            {
                return false;
            }
            for (int index = 0; index < count; index++)
            {
                if (!double.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out values[index]))
                {
                    return false;
                }
            }
            return true;
        }

        // Reads "care hunger dirt food": two numbers from 0 to 1 and the meals in the bag.
        static bool read_care(string line, out CareState saved_care)
        {
            saved_care = null;
            string[] parts = line.Split(' ');
            if (parts.Length != 4 || !read_numbers("care " + parts[1] + " " + parts[2], "care ", 2, out double[] needs)
                || !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out int food)
                || parts[0] != "care" || !within_unit(needs[0]) || !within_unit(needs[1]) || food > CareState.largest_food_carried)
            {
                return false;
            }
            saved_care = new CareState(needs[0], needs[1], food);
            return true;
        }

        // True for a number from 0 to 1. "Not a number" (NaN) fails every comparison, so it's
        // refused here too, and so is infinity.
        static bool within_unit(double value)
        {
            return value >= 0 && value <= 1;
        }

        // True for a place no further from the origin than any chapter could be (NaN and infinity
        // fail this too).
        static bool within_place(double[] place)
        {
            return Math.Abs(place[0]) <= largest_place && Math.Abs(place[1]) <= largest_place;
        }
    }
}
