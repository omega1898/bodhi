// Purpose: Bodhi's whole year, played chapter by chapter. Carries what should last from one
// chapter to the next: his care (hunger, mud, meals in the bag), the keepsakes found, and his
// story memory. How he feels (fear, closeness) starts fresh in each chapter, as in the browser
// game. Saved as plain text between chapters; a damaged save is refused whole.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bodhi.Rules
{
    public class Campaign
    {
        public const int format_version = 1;
        public const int maximum_text_length = 64 * 1024;  // Far more than a full save needs.

        readonly ChapterScript[] chapters = StoryChapters.all();
        readonly SortedSet<string> keepsakes = new SortedSet<string>(StringComparer.Ordinal);  // "chapter_id:keepsake".
        int chapter_index = 0;
        ChapterRun current_run = null;

        public StoryMemory memory { get; private set; } = new StoryMemory();
        public CareState care { get; private set; } = new CareState();

        // The chapter to play next, or null once the year is over.
        public ChapterScript current_chapter => is_finished ? null : chapters[chapter_index];

        // The next chapter's place in the story, from 1; one past the last when finished.
        public int chapter_number => chapter_index + 1;

        // True once all twelve chapters are done.
        public bool is_finished => chapter_index >= chapters.Length;

        // Keepsakes found so far, across every chapter.
        public int keepsakes_found => keepsakes.Count;

        // Keepsakes in the whole game.
        public int keepsake_total
        {
            get
            {
                int total = 0;
                foreach (var chapter in chapters)
                {
                    foreach (var thing in chapter.things)
                    {
                        if (!string.IsNullOrEmpty(thing.keepsake)) total += 1;
                    }
                }
                return total;
            }
        }

        // True if this keepsake has been found.
        public bool has_keepsake(string chapter_id, string keepsake)
        {
            return keepsakes.Contains(chapter_id + ":" + keepsake);
        }

        // How Bodhi follows her now, from how he feels and what he remembers.
        public CompanionBehavior companion(BondState bond)
        {
            return CompanionBehavior.choose(bond, memory);
        }

        // Gets the next chapter ready, carrying his care into it. Call start() on the run once
        // the scene is listening. Returns null once the year is over.
        public ChapterRun begin_chapter()
        {
            if (is_finished)
            {
                return null;
            }
            current_run = new ChapterRun(chapters[chapter_index], care);
            return current_run;
        }

        // Ends the chapter: keeps his care, the keepsakes and the remembered moments, then moves
        // on. Only for the run begin_chapter gave out, once it's complete. True if it counted.
        public bool finish_chapter(ChapterRun run)
        {
            if (run == null || run != current_run || !run.is_complete)
            {
                return false;
            }
            string chapter_id = run.chapter.id;
            care = new CareState(run.care.hunger, run.care.dirt, run.care.food);
            foreach (var keepsake in run.found_keepsakes)
            {
                keepsakes.Add(chapter_id + ":" + keepsake);
            }
            foreach (var moment in ChapterMemories.all)
            {
                if (moment.chapter_id == chapter_id && run.has_happened(moment.event_name))
                {
                    memory.remember(moment.fact);
                }
            }
            chapter_index += 1;
            current_run = null;
            return true;
        }

        // The save, as text: a format line, the next chapter, his care, then one line per
        // keepsake and per fact. Numbers are written the same on every computer.
        public string to_text()
        {
            var text = new StringBuilder();
            text.Append("bodhi_campaign ").Append(format_version).Append('\n');
            text.Append("chapter ").Append(chapter_number.ToString(CultureInfo.InvariantCulture)).Append('\n');
            text.Append("care ")
                .Append(care.hunger.ToString("R", CultureInfo.InvariantCulture)).Append(' ')
                .Append(care.dirt.ToString("R", CultureInfo.InvariantCulture)).Append(' ')
                .Append(care.food.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var keepsake in keepsakes)
            {
                text.Append("keepsake ").Append(keepsake).Append('\n');
            }
            foreach (var fact in memory.to_text().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                text.Append("fact ").Append(fact).Append('\n');
            }
            return text.ToString();
        }

        // Loads a save. All or nothing: anything damaged, unknown or repeated refuses the whole
        // text, and the campaign is left exactly as it was. True if it loaded.
        public bool load_text(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > maximum_text_length || !text.EndsWith("\n", StringComparison.Ordinal))
            {
                return false;
            }
            string[] lines = text.Substring(0, text.Length - 1).Split('\n');
            if (lines.Length < 3 || lines[0] != "bodhi_campaign " + format_version)
            {
                return false;
            }
            if (!read_chapter(lines[1], out int loaded_index) || !read_care(lines[2], out CareState loaded_care))
            {
                return false;
            }
            var loaded_keepsakes = new SortedSet<string>(StringComparer.Ordinal);
            var loaded_memory = new StoryMemory();
            var fact_text = new StringBuilder();
            for (int index = 3; index < lines.Length; index++)
            {
                string line = lines[index];
                if (line.StartsWith("keepsake ", StringComparison.Ordinal))
                {
                    string keepsake = line.Substring("keepsake ".Length);
                    if (!is_real_keepsake(keepsake) || !loaded_keepsakes.Add(keepsake))
                    {
                        return false;
                    }
                }
                else if (line.StartsWith("fact ", StringComparison.Ordinal))
                {
                    fact_text.Append(line.Substring("fact ".Length)).Append('\n');
                }
                else
                {
                    return false;
                }
            }
            if (fact_text.Length > 0 && !loaded_memory.load_text(fact_text.ToString()))
            {
                return false;
            }
            chapter_index = loaded_index;
            care = loaded_care;
            memory = loaded_memory;
            keepsakes.Clear();
            keepsakes.UnionWith(loaded_keepsakes);
            current_run = null;
            return true;
        }

        // Reads "chapter N", where N is 1 to one past the last chapter.
        bool read_chapter(string line, out int loaded_index)
        {
            loaded_index = 0;
            if (!line.StartsWith("chapter ", StringComparison.Ordinal)
                || !int.TryParse(line.Substring("chapter ".Length), NumberStyles.None, CultureInfo.InvariantCulture, out int number)
                || number < 1 || number > chapters.Length + 1)
            {
                return false;
            }
            loaded_index = number - 1;
            return true;
        }

        // Reads "care hunger dirt food": two numbers from 0 to 1 and a whole number of meals.
        static bool read_care(string line, out CareState loaded_care)
        {
            loaded_care = null;
            string[] parts = line.Split(' ');
            if (parts.Length != 4 || parts[0] != "care"
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double hunger)
                || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double dirt)
                || !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out int food)
                || !(hunger >= 0 && hunger <= 1) || !(dirt >= 0 && dirt <= 1) || food > CareState.largest_food_carried)
            {
                return false;
            }
            loaded_care = new CareState(hunger, dirt, food);
            return true;
        }

        // True if "chapter_id:keepsake" names a keepsake that exists in the game.
        bool is_real_keepsake(string name)
        {
            foreach (var chapter in chapters)
            {
                foreach (var thing in chapter.things)
                {
                    if (!string.IsNullOrEmpty(thing.keepsake) && name == chapter.id + ":" + thing.keepsake)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
