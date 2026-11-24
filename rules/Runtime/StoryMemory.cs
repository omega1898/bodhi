// Purpose: the story memory (spec 3.6). Facts such as "swept_away_by_river" that one chapter
// records and later chapters read, so Bodhi can remember what happened to him. Saved as plain
// text, one fact per line, and a damaged file is refused whole.
using System;
using System.Collections.Generic;
using System.Text;

namespace Bodhi.Rules
{
    public class StoryMemory
    {
        // The most facts kept, so a runaway loop can't grow the memory (and the save) forever.
        public const int maximum_fact_count = 256;
        // The longest fact name, the same rule as chapter event names.
        public const int maximum_fact_name_length = 64;
        // The longest memory text accepted: every fact at full length plus its line break.
        public const int maximum_text_length = maximum_fact_count * (maximum_fact_name_length + 1);

        // A SortedSet keeps each fact once, in a fixed alphabetical order, so the same memory
        // always saves to the same text. StringComparer.Ordinal sorts by character code, the
        // same on every computer and language setting.
        readonly SortedSet<string> facts = new SortedSet<string>(StringComparer.Ordinal);

        // How many facts are remembered.
        public int count => facts.Count;

        // How many fact names were refused for breaking the name rule (a sign of a bug).
        public int refused_fact_count { get; private set; }

        // Remembers a fact. True if it was new. False for a fact already known, a bad name
        // (counted), or a full memory.
        public bool remember(string fact_name)
        {
            if (!is_valid_fact_name(fact_name))
            {
                refused_fact_count += 1;
                return false;
            }
            if (facts.Count >= maximum_fact_count && !facts.Contains(fact_name))
            {
                return false;
            }
            return facts.Add(fact_name);
        }

        // True if the fact was remembered.
        public bool knows(string fact_name)
        {
            return fact_name != null && facts.Contains(fact_name);
        }

        // The memory as text: each fact on its own line, in alphabetical order.
        public string to_text()
        {
            var text = new StringBuilder();
            foreach (string fact in facts)
            {
                text.Append(fact).Append('\n');
            }
            return text.ToString();
        }

        // Replaces the memory with the facts in the text. Every line is checked first; if any
        // is wrong (bad name, repeated, too many, cut short) nothing changes and it returns false.
        public bool load_text(string text)
        {
            if (text == null || text.Length > maximum_text_length)
            {
                return false;
            }
            if (text.Length > 0 && text[text.Length - 1] != '\n')
            {
                return false;  // Cut short: the last line has no line break.
            }
            var loaded = new SortedSet<string>(StringComparer.Ordinal);
            string[] lines = text.Split('\n');
            // The text ends with a line break, so the last piece is always empty: skip it.
            for (int index = 0; index < lines.Length - 1; index += 1)
            {
                string line = lines[index];
                if (!is_valid_fact_name(line) || !loaded.Add(line) || loaded.Count > maximum_fact_count)
                {
                    return false;
                }
            }
            facts.Clear();
            facts.UnionWith(loaded);
            return true;
        }

        // The name rule: 1 to 64 characters, each a lowercase letter, a digit or an underscore.
        static bool is_valid_fact_name(string fact_name)
        {
            if (string.IsNullOrEmpty(fact_name) || fact_name.Length > maximum_fact_name_length)
            {
                return false;
            }
            foreach (char character in fact_name)
            {
                bool allowed = (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '_';
                if (!allowed)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
