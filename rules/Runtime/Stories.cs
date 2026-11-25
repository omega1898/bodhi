// Purpose: names each story in the series and finds its chapters. The game plays one story (the
// Christmas one today); the flood story is kept for a later game, and because the story engine's
// tests are written around its twelve chapters. A new game in the series adds a name here.
namespace Bodhi.Rules
{
    // Which story a year is part of.
    public enum Story
    {
        flood,      // "Until We Find Home": the flood, the long walk home (docs/story.md).
        christmas,  // "A Light in the Window": twelve days to Christmas (docs/christmas_story.md).
    }

    // "static" means you use it without making one: Stories.chapters(Story.christmas).
    public static class Stories
    {
        // A fresh copy of every chapter of a story, first to last.
        public static ChapterScript[] chapters(Story story)
        {
            switch (story)
            {
                case Story.christmas: return ChristmasChapters.all();
                default: return StoryChapters.all();
            }
        }
    }
}
