// Purpose: finds a chapter's stage in the right story. Two stories can use the same chapter id
// (both have a "first_snow"), so the scene always asks for a stage by story and chapter together.
using Bodhi.Rules;

namespace Bodhi.Game
{
    public static class StoryStages
    {
        // The stage for this chapter of this story, or null if there is none.
        public static ChapterStage for_chapter(Story story, string chapter_id)
        {
            switch (story)
            {
                case Story.christmas: return ChristmasStages.for_chapter(chapter_id);
                default: return ChapterStages.for_chapter(chapter_id);
            }
        }
    }
}
