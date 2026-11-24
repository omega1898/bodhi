// Purpose: the story stages (beats) of a chapter and which order they may come in.
// Ported from core/src/chapter_beat.cpp, so the Unity game keeps the tested story order.

namespace Bodhi.Rules
{
    // One stage of the chapter's story, in the order the player normally meets them.
    public enum ChapterBeat
    {
        together,        // She and Bodhi walk together.
        separation,      // Something splits them up.
        human_section,   // The player controls her.
        dog_section,     // The player controls Bodhi.
        near_contact,    // They can see each other but not reach each other.
        final_obstacle,  // Both must act to get through.
        reunion,         // They are together again.
        aftermath,       // The quiet moment after.
    }

    // Rules about beats. "static" means you use it without making one: ChapterBeats.name_of(...).
    public static class ChapterBeats
    {
        // How many real beats there are, used to check a value is one of them.
        public const int beat_count = 8;

        // True if a value is one of the eight real beats (a forced cast like (ChapterBeat)99 is not).
        public static bool is_real_beat(ChapterBeat beat)
        {
            int beat_number = (int)beat;
            return beat_number >= 0 && beat_number < beat_count;
        }

        // True if the story may move from one beat straight to the next one.
        public static bool is_allowed_transition(ChapterBeat from_beat, ChapterBeat to_beat)
        {
            switch (from_beat)
            {
                case ChapterBeat.together: return to_beat == ChapterBeat.separation;
                case ChapterBeat.separation: return to_beat == ChapterBeat.human_section;
                case ChapterBeat.human_section: return to_beat == ChapterBeat.dog_section || to_beat == ChapterBeat.near_contact;
                case ChapterBeat.dog_section: return to_beat == ChapterBeat.human_section || to_beat == ChapterBeat.near_contact;
                case ChapterBeat.near_contact: return to_beat == ChapterBeat.final_obstacle;
                case ChapterBeat.final_obstacle: return to_beat == ChapterBeat.reunion;
                case ChapterBeat.reunion: return to_beat == ChapterBeat.aftermath;
                default: return false;
            }
        }

        // The beat's name as text, for logs. A value that isn't a real beat gives "unknown".
        public static string name_of(ChapterBeat beat)
        {
            return is_real_beat(beat) ? beat.ToString() : "unknown";
        }
    }
}
