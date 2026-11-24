// Purpose: the moments each chapter leaves in Bodhi's story memory (spec R3.1: every chapter has
// a moment a later chapter remembers). When a chapter ends, the campaign records the fact for
// each moment whose event happened. Later chapters, dialogue and Bodhi's behaviour read the
// facts; for example, after the river takes him he approaches water warily (CompanionBehavior).
//
// These are proposals drawn from docs/story.md (the Mina and Biscuit thread and the bookends),
// for the designer to confirm or change. Only "swept_away_by_river" changes behaviour so far.
namespace Bodhi.Rules
{
    // One remembered moment: in this chapter, this event leaves this fact, recalled in that chapter.
    public readonly struct ChapterMemory
    {
        public readonly string chapter_id;
        public readonly string event_name;
        public readonly string fact;
        public readonly string recalled_in;  // The later chapter that calls back to it.

        // Makes one row of the table.
        public ChapterMemory(string chapter_id, string event_name, string fact, string recalled_in)
        {
            this.chapter_id = chapter_id;
            this.event_name = event_name;
            this.fact = fact;
            this.recalled_in = recalled_in;
        }
    }

    public static class ChapterMemories
    {
        // The whole table, in story order, so a designer can read it at a glance.
        public static readonly ChapterMemory[] all =
        {
            //                 chapter           event                  fact                           recalled in
            new ChapterMemory("coming_home",    "height_marked",       "marked_height_at_eight_weeks", "home"),        // The second pencil mark.
            new ChapterMemory("coming_home",    "biscuit_hello",       "met_biscuit",                  "high_farm"),   // Biscuit found.
            new ChapterMemory("the_storm",      "sheet_grabbed",       "pulled_from_the_flood",        "crossing"),    // Water again.
            new ChapterMemory("last_morning",   "poster_read",         "read_mina_poster",             "high_farm"),   // Mina asks for him.
            new ChapterMemory("other_side",     "glove_found",         "found_mina_glove",             "high_farm"),
            new ChapterMemory("motorway",       "mina_note",           "read_mina_note",               "lessons"),     // Hearts, then her name.
            new ChapterMemory("visiting_hours", "ama_letter",          "read_ama_letter",              "first_snow"),  // "If found, call Ama."
            new ChapterMemory("orchard",        "biscuit_bed",         "found_biscuit_bed",            "lessons"),     // Bodhi bolts after him.
            new ChapterMemory("lessons",        "board_read",          "read_go_north",                "crossing"),    // Northgate.
            new ChapterMemory("first_snow",     "collar_found",        "found_biscuit_collar",         "high_farm"),   // The empty collar, then him.
            new ChapterMemory("crossing",       "dog_surfaced",        CompanionBehavior.swept_away_fact, "high_farm"),
            new ChapterMemory("high_farm",      "biscuit_free",        "biscuit_back_with_mina",       "home"),        // The light behind the blue door.
        };
    }
}
