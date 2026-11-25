// Purpose: Chapter Eight of the Christmas story, "Blackwater" (20 December), as data for
// ChapterRun. Across the frozen tarn close to the shore; a gust takes the sled, with Biscuit in it,
// out onto black ice too thin for Em. Bodhi crawls out and holds the sled rope in his teeth while
// she frees the fishing hut's pulley and cranks them both in. That night, a candle in the hut
// window that nobody will see. Every word comes from docs/christmas_script.md, Chapter Eight;
// when the script changes, change it here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: the shore where the
// cornice came down (0 to 600), the white ice close to the shore with the fishing hut on stilts
// (600 to 2350), and the black ice where the sled skids (2350 to 3200). The floor is at y = 520.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter Eight: Blackwater.
        public static ChapterScript blackwater()
        {
            return new ChapterScript
            {
                id = "blackwater",
                title = "Blackwater",
                eyebrow = "Chapter Eight: 20 December",
                place = "Blackwater Tarn, high on the fell",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0.15,
                start_closeness = 0.7,
                start_hunger = 0.4,
                things = new ChapterThing[]
                {
                    // The fishing hut, passed on the way over (nothing can be used once a reunion begins).
                    new ChapterThing { event_name = "keepsake_fishing_log", who = Character.human, label = "Read the fishing log", requires = new string[] { "at_the_tarn" }, keepsake = "fishing_log", memory_text = "The hut's fishing log, last entry: 'Caught nothing. Saw an otter. Perfect day.'", x = 1800, y = 520 },
                    new ChapterThing { event_name = "keepsake_biscuit_tin", who = Character.human, label = "Look at the tin", requires = new string[] { "at_the_tarn" }, keepsake = "next_one_tin", memory_text = "A biscuit tin frozen into the step, 'FOR THE NEXT ONE' painted on its lid. Inside: matches.", x = 1700, y = 520 },
                    new ChapterThing { event_name = "keepsake_child_skate", who = Character.dog, label = "Sniff the skate", requires = new string[] { "at_the_tarn" }, hidden = true, keepsake = "child_skate", memory_text = "A child's ice skate on a nail by the hut door, left for the next one who needs it.", x = 1880, y = 520 },
                    // The black ice.
                    new ChapterThing { event_name = "rope_held", who = Character.dog, label = "Take the rope in your teeth", requires = new string[] { "crawled_out" }, x = 2700, y = 520 },
                    new ChapterThing { event_name = "pulley_freed", who = Character.human, label = "Kick the pulley loose", requires = new string[] { "rope_held" }, x = 1820, y = 520 },
                    new ChapterThing { event_name = "crank_turned", who = Character.human, label = "Turn the crank", requires = new string[] { "pulley_freed" }, x = 1860, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "on_the_ice", who = ZoneWho.both, requires = new string[] { "at_the_tarn" }, x = 700, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "halfway", who = ZoneWho.human, requires = new string[] { "on_the_ice" }, x = 2150, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "crawled_out", who = ZoneWho.dog, requires = new string[] { "too_heavy" }, x = 2600, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "into_the_hut", who = ZoneWho.human, requires = new string[] { "reunion_complete" }, x = 1750, y = 300, width = 150, height = 240 },
                },
                rules = new StoryRule[]
                {
                    // The tarn (a film).
                    new StoryRule { on = "level_started", follow = true, switching = false, fetch = false, schedule_seconds = 3, schedule_event = "tarn_1", cutscene = CutsceneChange.start, cutscene_skip_to = "at_the_tarn" },
                    new StoryRule { on = "tarn_1", lines = new string[] { "Blackwater Tarn was frozen from shore to shore, and it was singing." }, schedule_seconds = 4.5, schedule_event = "tarn_2" },
                    new StoryRule { on = "tarn_2", lines = new string[] { "Tam's map said 'keep to the shore'. A cornice had come down across the shore.", "There was only the ice, close in, where it was white. Never the black." }, schedule_seconds = 8, schedule_event = "at_the_tarn" },
                    new StoryRule { on = "at_the_tarn", objective = "Cross close to the shore. Keep to the white ice.", switching = true, cutscene = CutsceneChange.end },
                    new StoryRule { on = "on_the_ice", lines = new string[] { "Ice! Slide. Legs go everywhere. Again!" }, voice = StoryVoice.dog },
                    // The gust (a film).
                    new StoryRule { on = "halfway", lines = new string[] { "Halfway over, the wind came down off the fell like a slammed door.", "It took the sled out onto the black ice like it had been waiting to." }, objective = "", control = Character.human, beat = ChapterBeat.separation, follow = false, fear_change = 0.4, schedule_seconds = 8, schedule_event = "too_heavy", cutscene = CutsceneChange.start, cutscene_skip_to = "too_heavy" },
                    new StoryRule { on = "too_heavy", lines = new string[] { "I was too heavy for black ice. He wasn't." }, objective = "Switch to {dog}. Crawl out to the sled. Slowly.", beat = ChapterBeat.near_contact, switching = true, cutscene = CutsceneChange.end },
                    // The black ice.
                    new StoryRule { on = "crawled_out", lines = new string[] { "Belly low. Ice talks. Ice complains. Slow. Slow.", "The old dog, in the sled, very still. Good. Stay still." }, voice = StoryVoice.dog, objective = "Take the sled rope in your teeth." },
                    new StoryRule { on = "rope_held", lines = new string[] { "The rope. Teeth. Hold.", "Her voice, behind. Calm. Too calm." }, voice = StoryVoice.dog, objective = "Switch to Em. Free the hut's pulley.", beat = ChapterBeat.final_obstacle },
                    new StoryRule { on = "pulley_freed", lines = new string[] { "The hut's boat rope ran through a pulley, rusted stiff. I kicked it loose." }, objective = "Turn the crank." },
                    new StoryRule { on = "crank_turned", lines = new string[] { "I threw him the line, and turned the crank, and turned it." } },
                    // Hauled in: his rope and her crank.
                    new StoryRule { when_all = new string[] { "rope_held", "crank_turned" }, lines = new string[] { "The ice cracked behind them, a long black line.", "It held." }, beat = ChapterBeat.reunion, reunion = true, fear_change = -0.5 },
                    new StoryRule { on = "reunion_embrace", lines = new string[] { "I had them both on the shore before I let myself breathe." } },
                    new StoryRule { on = "reunion_complete", objective = "Shelter in the fishing hut.", beat = ChapterBeat.aftermath },
                    // The fishing hut (a film).
                    new StoryRule { on = "into_the_hut", lines = new string[] { "The hut had a stove the size of a bread bin. It was the best stove in the world.", "I dried his belly by it while he steamed and grumbled." }, objective = "", care_dirt = 0, schedule_seconds = 8, schedule_event = "candle", cutscene = CutsceneChange.start, cutscene_skip_to = "booming" },
                    new StoryRule { on = "candle", lines = new string[] { "I put a candle in the hut window. Nobody would see it. I lit it anyway." }, schedule_seconds = 6, schedule_event = "booming" },
                    new StoryRule { on = "booming", lines = new string[] { "All night the ice boomed, like a door very far away." }, schedule_seconds = 6, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "Chapter Nine: The Bothy.", "The twenty-first of December." },
            };
        }
    }
}
