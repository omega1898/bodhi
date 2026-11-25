// Purpose: what the memory box (the collectibles screen) shows: every chapter in story order,
// whether it's open and finished, the keepsakes found in it out of how many, and for each
// keepsake its memory once found or a gentle hint while it's missing. It also lets a finished
// chapter be played again without moving the year on (docs/story.md, "Keepsakes").
//
// It only reads the Campaign; it never changes it or its save.
using System;
using System.Collections.Generic;
using Bodhi.Rules;

namespace Bodhi.Game
{
    // One keepsake in the box.
    public sealed class MemoryBoxKeepsake
    {
        public string id = "";            // Its keepsake name in the chapter, like "first_slipper".
        public string name = "";          // The same, readable: "First slipper".
        public bool found = false;        // True once it's been found in the year.
        public string memory_text = "";   // What she remembers about it, only once found.
        public string hint = "";          // Where to look, only while it's missing.
        public bool dog_only = false;     // True if only Bodhi's nose can find it.
    }

    // One chapter in the box.
    public sealed class MemoryBoxChapter
    {
        public int number = 0;                   // Its place in the year, from 1.
        public string id = "";                   // Like "coming_home".
        public string title = "";                // Like "Coming Home".
        public string eyebrow = "";              // The small line above the title, like "Prologue".
        public bool unlocked = false;            // Reached: finished, or the one being played.
        public bool finished = false;            // Played to its end.
        public bool can_replay = false;          // Finished chapters can be played again.
        public int keepsakes_found = 0;
        public int keepsake_total = 0;
        public MemoryBoxKeepsake[] keepsakes = new MemoryBoxKeepsake[0];
    }

    public sealed class MemoryBox
    {
        public const string dog_hint = "Somewhere {dog}'s nose could find it";
        public const string left_behind_hint = "Something left behind";

        // Every chapter, first to last.
        public MemoryBoxChapter[] chapters { get; private set; } = new MemoryBoxChapter[0];

        // Keepsakes found in the whole year, and how many there are.
        public int keepsakes_found { get; private set; }
        public int keepsake_total { get; private set; }

        // Builds the box from the year so far. No campaign is read as a new year; {dog} in the
        // memories and hints becomes this name (Bodhi if none is given).
        public static MemoryBox build(Campaign campaign, string dog_name)
        {
            var year = campaign ?? new Campaign();
            string name = string.IsNullOrEmpty(dog_name) ? "Bodhi" : dog_name;
            var scripts = Stories.chapters(year.story);
            var box = new MemoryBox { chapters = new MemoryBoxChapter[scripts.Length] };
            for (int index = 0; index < scripts.Length; index++)
            {
                var chapter = chapter_entry(year, scripts[index], index + 1, name);
                box.chapters[index] = chapter;
                box.keepsakes_found += chapter.keepsakes_found;
                box.keepsake_total += chapter.keepsake_total;
            }
            return box;
        }

        // True if this chapter is in the box and can be played again.
        public bool can_replay(string chapter_id)
        {
            foreach (var chapter in chapters)
            {
                if (chapter.id == chapter_id)
                {
                    return chapter.can_replay;
                }
            }
            return false;
        }

        // A fresh copy of a finished chapter to play again, or null if it isn't finished or
        // doesn't exist. The campaign is only read.
        public static ChapterScript replay_script(Campaign campaign, string chapter_id)
        {
            if (campaign == null || string.IsNullOrEmpty(chapter_id))
            {
                return null;
            }
            var scripts = Stories.chapters(campaign.story);
            for (int index = 0; index < scripts.Length; index++)
            {
                if (scripts[index].id == chapter_id)
                {
                    return is_finished(campaign, index + 1) ? scripts[index] : null;
                }
            }
            return null;
        }

        // A run of a finished chapter, ready to start(), with fresh care so the year's isn't
        // touched. The campaign refuses to count it (it isn't the year's chapter), so the year
        // doesn't move on. Null if the chapter can't be replayed.
        public static ChapterRun begin_replay(Campaign campaign, string chapter_id)
        {
            var script = replay_script(campaign, chapter_id);
            return script == null ? null : new ChapterRun(script, new CareState());
        }

        // The keepsakes found in a replay that the year hasn't got yet: what a replay would add
        // once the campaign can save them (it can't yet; see the hand-over notes).
        public static List<string> keepsakes_new_in_replay(Campaign campaign, ChapterRun replay)
        {
            var found = new List<string>();
            if (campaign == null || replay == null)
            {
                return found;
            }
            foreach (var keepsake in replay.found_keepsakes)
            {
                if (!campaign.has_keepsake(replay.chapter.id, keepsake))
                {
                    found.Add(keepsake);
                }
            }
            return found;
        }

        // True if the chapter at this place in the year (from 1) has been played to its end.
        static bool is_finished(Campaign campaign, int number)
        {
            return number < campaign.chapter_number;
        }

        // One chapter's entry: open or not, finished or not, and its keepsakes.
        static MemoryBoxChapter chapter_entry(Campaign campaign, ChapterScript script, int number, string dog_name)
        {
            bool finished = is_finished(campaign, number);
            var keepsakes = new List<MemoryBoxKeepsake>();
            int found_count = 0;
            foreach (var thing in script.things)
            {
                if (string.IsNullOrEmpty(thing.keepsake))
                {
                    continue;
                }
                var keepsake = keepsake_entry(campaign, script.id, thing, dog_name);
                keepsakes.Add(keepsake);
                if (keepsake.found)
                {
                    found_count += 1;
                }
            }
            return new MemoryBoxChapter
            {
                number = number,
                id = script.id,
                title = script.title,
                eyebrow = script.eyebrow,
                unlocked = number <= campaign.chapter_number,
                finished = finished,
                can_replay = finished,
                keepsakes_found = found_count,
                keepsake_total = keepsakes.Count,
                keepsakes = keepsakes.ToArray(),
            };
        }

        // One keepsake's entry: its memory once found, otherwise a hint.
        static MemoryBoxKeepsake keepsake_entry(Campaign campaign, string chapter_id, ChapterThing thing, string dog_name)
        {
            bool found = campaign.has_keepsake(chapter_id, thing.keepsake);
            bool dog_only = thing.hidden || thing.who == Character.dog;
            return new MemoryBoxKeepsake
            {
                id = thing.keepsake,
                name = readable(thing.keepsake),
                found = found,
                memory_text = found ? StoryText.personalize(thing.memory_text, dog_name) : "",
                hint = found ? "" : StoryText.personalize(dog_only ? dog_hint : left_behind_hint, dog_name),
                dog_only = dog_only,
            };
        }

        // "first_slipper" as "First slipper".
        static string readable(string keepsake)
        {
            string spaced = (keepsake ?? "").Replace('_', ' ').Trim();
            if (spaced.Length == 0)
            {
                return "";
            }
            return char.ToUpperInvariant(spaced[0]) + spaced.Substring(1);
        }
    }
}
