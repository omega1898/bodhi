// Purpose: Chapter Ten of the Christmas story, "Windy Gap" (23 December), as data for ChapterRun.
// The hardest climb, to the pass. On the ledge below the frozen waterfall a gust sends the sled,
// with Biscuit in it, down a gully. In Chapter One Bodhi lunged without a look; now he must look
// at Em first, and the gully edge holds him until she says go. He holds the sled and gets Biscuit
// onto a shelf; she cuts steps down the frozen waterfall and fixes Tam's rope; they meet at the
// sheepfold, and from the top of the pass every window in Hollin is dark. Every word comes from
// docs/christmas_script.md, Chapter Ten; when the script changes, change it here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: the climb, with its
// cairn, bench, frozen waterfall and marker posts (0 to 1700), the ledge (1700 to 1950), the gully
// and the sheepfold at its foot (1950 to 2800) and the haul up to the top of the pass (2800 to
// 3800). The floor is at y = 520 everywhere.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter Ten: Windy Gap.
        public static ChapterScript windy_gap()
        {
            return new ChapterScript
            {
                id = "windy_gap",
                title = "Windy Gap",
                eyebrow = "Chapter Ten: 23 December",
                place = "Windy Gap, the high pass",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0.2,
                start_closeness = 0.8,
                start_hunger = 0.4,
                things = new ChapterThing[]
                {
                    // The climb.
                    new ChapterThing { event_name = "keepsake_cairn_bowl", who = Character.dog, label = "Sniff the cairn", requires = new string[] { "climbing" }, hidden = true, keepsake = "cairn_bowl", memory_text = "A dog bowl wedged into a cairn, full of snow, for any dog passing over the Gap.", x = 1200, y = 520 },
                    new ChapterThing { event_name = "keepsake_memorial_bench", who = Character.human, label = "Read the bench", requires = new string[] { "climbing" }, keepsake = "memorial_bench", memory_text = "A bench on the shoulder of the climb, cleared of snow this morning: 'For Iain, who loved this view.'", x = 1300, y = 520 },
                    new ChapterThing { event_name = "keepsake_prayer_flags", who = Character.human, label = "Look at the flags", requires = new string[] { "climbing" }, keepsake = "prayer_flags", memory_text = "Prayer flags tied to a marker post, faded white by years of this wind. Still there.", x = 1650, y = 520 },
                    // The look, and the gully (Bodhi).
                    new ChapterThing { event_name = "looked_at_her", who = Character.dog, label = "Look at her", requires = new string[] { "the_look" }, x = 1850, y = 520 },
                    new ChapterThing { event_name = "biscuit_on_shelf", who = Character.dog, label = "Nudge him onto the shelf", requires = new string[] { "reached_sled" }, x = 2650, y = 520 },
                    // The waterfall (Em).
                    new ChapterThing { event_name = "steps_cut", who = Character.human, label = "Cut steps in the ice", requires = new string[] { "em_climbs" }, x = 1500, y = 520 },
                    new ChapterThing { event_name = "rope_fixed", who = Character.human, label = "Fix the rope", requires = new string[] { "steps_cut" }, x = 1560, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "first_pitch", who = ZoneWho.both, requires = new string[] { "climbing" }, x = 950, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "ledge", who = ZoneWho.human, requires = new string[] { "first_pitch" }, x = 1780, y = 300, width = 120, height = 240 },
                    new ChapterZone { event_name = "reached_sled", who = ZoneWho.dog, requires = new string[] { "she_said_go" }, x = 2550, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "across_the_gully", who = ZoneWho.human, requires = new string[] { "rope_fixed" }, x = 2050, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "at_the_fold", who = ZoneWho.human, requires = new string[] { "across_the_gully" }, x = 2700, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "the_top", who = ZoneWho.human, requires = new string[] { "reunion_complete" }, x = 3550, y = 300, width = 150, height = 240 },
                },
                rules = new StoryRule[]
                {
                    // The climb.
                    new StoryRule { on = "level_started", follow = true, switching = false, fetch = false, schedule_seconds = 3, schedule_event = "open_1", cutscene = CutsceneChange.start, cutscene_skip_to = "climbing" },
                    new StoryRule { on = "open_1", lines = new string[] { "Windy Gap earned its name in the first five minutes." }, schedule_seconds = 4, schedule_event = "open_2" },
                    new StoryRule { on = "open_2", lines = new string[] { "Wind full of ice. Harness tight. Her, behind. Pull." }, voice = StoryVoice.dog, schedule_seconds = 4, schedule_event = "climbing" },
                    new StoryRule { on = "climbing", objective = "Pull together up to the pass.", switching = true, cutscene = CutsceneChange.end },
                    new StoryRule { on = "first_pitch", lines = new string[] { "The map said two days. The map had never pulled a sled." } },
                    new StoryRule { on = "ledge", lines = new string[] { "Below the frozen waterfall the path narrowed to a ledge, and the wind found us." }, objective = "", schedule_seconds = 4, schedule_event = "gust" },
                    new StoryRule { on = "gust", lines = new string[] { "The sled went down the gully with Biscuit in it, and he didn't even bark." }, beat = ChapterBeat.separation, follow = false, fear_change = 0.4, schedule_seconds = 5, schedule_event = "the_look", cutscene = CutsceneChange.start, cutscene_skip_to = "the_look" },
                    // The look: this time he asks first.
                    new StoryRule { on = "the_look", objective = "Look at Em first.", control = Character.dog, switching = false, cutscene = CutsceneChange.end },
                    new StoryRule { on = "looked_at_her", lines = new string[] { "The sled, going. Go? Look at her first." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 2.5, schedule_event = "she_said_go" },
                    new StoryRule { on = "she_said_go", lines = new string[] { "He looked at me. I said go.", "Nine days ago he would have gone without looking." }, objective = "Chase the sled down the gully. Hold it.", beat = ChapterBeat.dog_section, remove = new string[] { "gully_edge" } },
                    new StoryRule { on = "reached_sled", lines = new string[] { "Down. Down. Snow in a river. The sled, sliding. Teeth. Rope. HOLD." }, voice = StoryVoice.dog, objective = "Nudge Biscuit onto the shelf." },
                    new StoryRule { on = "biscuit_on_shelf", lines = new string[] { "The old dog, out. Onto the flat rock. Stay. Good." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 3, schedule_event = "em_climbs" },
                    // The waterfall (Em).
                    new StoryRule { on = "em_climbs", lines = new string[] { "I went round by the frozen waterfall. There was no other way down." }, objective = "Cut steps in the frozen waterfall.", control = Character.human, beat = ChapterBeat.human_section },
                    new StoryRule { on = "steps_cut", lines = new string[] { "The waterfall was organ pipes of ice. I cut steps with the edge of my boot." }, objective = "Fix Tam's rope." },
                    new StoryRule { on = "rope_fixed", lines = new string[] { "I tied Tam's rope round an iron post furred white with rime." }, objective = "Climb down to the sheepfold.", remove = new string[] { "waterfall_route" } },
                    new StoryRule { on = "across_the_gully", lines = new string[] { "Across the gully I could see him, holding the sled, not letting go.", "'Good boy,' I shouted. The wind took it. He heard it anyway." }, beat = ChapterBeat.near_contact },
                    // The sheepfold: his shelf and her rope.
                    new StoryRule { when_all = new string[] { "biscuit_on_shelf", "rope_fixed", "at_the_fold" }, lines = new string[] { "We met at an old sheepfold at the foot of the gully." }, beat = ChapterBeat.reunion, reunion = true, fear_change = -0.4 },
                    new StoryRule { on = "reunion_embrace", lines = new string[] { "He leaned his whole weight against my legs. We stayed like that a while." } },
                    new StoryRule { on = "reunion_complete", objective = "Haul the sled up to the top of the pass.", beat = ChapterBeat.aftermath },
                    // The top (a film).
                    new StoryRule { on = "the_top", lines = new string[] { "We hauled it all back up together, and came over the top of Windy Gap.", "Below us, the whole valley, and Hollin at the end of it." }, objective = "", schedule_seconds = 8, schedule_event = "dark_windows", cutscene = CutsceneChange.start, cutscene_skip_to = "dark_windows" },
                    new StoryRule { on = "dark_windows", lines = new string[] { "Every window in Hollin was dark." }, fear_change = 0.3, schedule_seconds = 6, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "Chapter Eleven: Christmas Eve.", "The twenty-fourth of December." },
            };
        }
    }
}
