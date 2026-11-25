// Purpose: the Christmas story, "A Light in the Window" (docs/christmas_story.md), as a list of
// chapters for ChapterRun. It is kept apart from the flood story in Chapters/ so that each game in
// the series has its own chapters while all of them share one tested engine. Chapters are added
// here, in story order, as they are built.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Makes a fresh copy of every chapter built so far, first to last.
        public static ChapterScript[] all()
        {
            return new[]
            {
                first_snow(),
                lamp_in_window(),
                kell_bridge(),
                strand(),
                lantern_train(),
                reindeer_hill(),
                tams_sled(),
                blackwater(),
            };
        }
    }
}
