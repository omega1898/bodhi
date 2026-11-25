// Purpose: every stage of the Christmas story, in story order, and a way to find one by its
// chapter. Lined up with rules/Runtime/Christmas/ChristmasChapters.cs: one stage per chapter.
namespace Bodhi.Game
{
    public static partial class ChristmasStages
    {
        // Makes a fresh copy of every stage built so far, first to last.
        public static ChapterStage[] all()
        {
            return new[]
            {
                first_snow(),
                lamp_in_window(),
                kell_bridge(),
                strand(),
            };
        }

        // The stage for the chapter with this id, or null if there is none.
        public static ChapterStage for_chapter(string chapter_id)
        {
            switch (chapter_id)
            {
                case "first_snow": return first_snow();
                case "lamp_in_window": return lamp_in_window();
                case "kell_bridge": return kell_bridge();
                case "strand": return strand();
                default: return null;
            }
        }
    }
}
