// Purpose: Chapter Eleven of the Christmas story, "Christmas Eve" (24 December), as data for
// ChapterRun. The climax: down from Windy Gap into the worst storm in fifty years. The lantern
// blows out, and the roles reverse: the player is Bodhi, leading Em on the harness down the old
// drovers' path by its smell, barking so she knows he's there, toward the church bell Mina has had
// rung. The storm thins over a village with every window lit, and Em's lamp highest of all. They
// cross the iced footbridge together and come home through the pines to Mina on the porch. Every
// word comes from docs/christmas_script.md, Chapter Eleven; when the script changes, change it
// here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: the high path in
// the white-out, with its marker posts (0 to 2400), the slope where the storm thins over the
// village (2400 to 3200), the iced footbridge over the burn (3200 to 3600) and the pines down to
// the cabin porch (3600 to 4400). The floor is at y = 520 everywhere.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter Eleven: Christmas Eve.
        public static ChapterScript christmas_eve()
        {
            return new ChapterScript
            {
                id = "christmas_eve",
                title = "Christmas Eve",
                eyebrow = "Chapter Eleven: 24 December",
                place = "The drovers' path, down into Hollin",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0.4,
                start_closeness = 0.9,
                start_hunger = 0.5,
                things = new ChapterThing[]
                {
                    // The old path, found by his nose.
                    new ChapterThing { event_name = "old_scent_1", who = Character.dog, label = "Follow the old path", requires = new string[] { "he_leads" }, hidden = true, x = 1000, y = 520 },
                    new ChapterThing { event_name = "keepsake_drovers_wool", who = Character.dog, label = "Sniff the post", requires = new string[] { "he_leads" }, hidden = true, keepsake = "drovers_wool", memory_text = "Sheep's wool snagged on a marker post, grey with age: the old drovers' road, still there under the snow.", x = 1200, y = 520 },
                    new ChapterThing { event_name = "old_scent_2", who = Character.dog, label = "Follow the old path", requires = new string[] { "old_scent_1" }, hidden = true, x = 1400, y = 520 },
                    // The footbridge.
                    new ChapterThing { event_name = "keepsake_far_candle", who = Character.human, label = "Look at the farthest window", requires = new string[] { "footbridge_reached" }, keepsake = "far_candle", memory_text = "In the farthest window of the village, an old man's house, a candle. Mina had gone up and lit it for him.", x = 3200, y = 520 },
                    new ChapterThing { event_name = "handrail_cleared", who = Character.human, label = "Knock the ice off the handrail", requires = new string[] { "footbridge_reached" }, x = 3250, y = 520 },
                    new ChapterThing { event_name = "rope_across", who = Character.dog, label = "Take the rope across", requires = new string[] { "footbridge_reached" }, x = 3330, y = 520 },
                    // Home.
                    new ChapterThing { event_name = "keepsake_vicars_note", who = Character.human, label = "Read the note on the post", requires = new string[] { "crossed" }, keepsake = "vicars_note", memory_text = "Pinned to my porch post: 'Bell every quarter hour until they're down. Volunteers to Mina.'", x = 3900, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "lantern_out", who = ZoneWho.human, requires = new string[] { "white_out" }, x = 600, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "mercy_heard", who = ZoneWho.dog, requires = new string[] { "old_scent_2" }, x = 1800, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "village_lights", who = ZoneWho.dog, requires = new string[] { "mercy_named" }, x = 2600, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "porch_seen", who = ZoneWho.human, requires = new string[] { "crossed" }, x = 4000, y = 300, width = 150, height = 240 },
                },
                rules = new StoryRule[]
                {
                    // The white-out.
                    new StoryRule { on = "level_started", follow = true, switching = false, fetch = false, schedule_seconds = 3, schedule_event = "open_1", cutscene = CutsceneChange.start, cutscene_skip_to = "white_out" },
                    new StoryRule { on = "open_1", lines = new string[] { "We started down at noon, to beat the storm. The storm won." }, schedule_seconds = 4.5, schedule_event = "open_2" },
                    new StoryRule { on = "open_2", lines = new string[] { "White. Loud. Her hand on the harness, holding tight. Slow." }, voice = StoryVoice.dog, schedule_seconds = 4.5, schedule_event = "white_out" },
                    new StoryRule { on = "white_out", objective = "Keep going down. Keep the marker posts close.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "lantern_out", lines = new string[] { "The wind blew the lantern out. After that there was no up, no down, no hand.", "I held his harness, and I let him lead." }, objective = "", fear_change = 0.3, schedule_seconds = 8, schedule_event = "he_leads", cutscene = CutsceneChange.start, cutscene_skip_to = "he_leads" },
                    // He leads: the player is Bodhi, and Em follows on the harness.
                    new StoryRule { on = "he_leads", lines = new string[] { "White on top. Underneath: sheep, boots, old dogs. The old path.", "Snow forgets. Noses don't." }, voice = StoryVoice.dog, objective = "Lead Em down. Find the old path by its smell.", control = Character.dog, beat = ChapterBeat.dog_section, follow = true, cutscene = CutsceneChange.end },
                    new StoryRule { on = "old_scent_1", lines = new string[] { "Here. Sheep, a hundred winters of sheep. This way." }, voice = StoryVoice.dog },
                    new StoryRule { on = "old_scent_2", lines = new string[] { "And dogs, long ago, a hundred of them, going home this way." }, voice = StoryVoice.dog, objective = "Bark, so she knows you're there." },
                    new StoryRule { on = "old_scent_2", lines = new string[] { "Every few minutes I whistled, so he'd know I was still holding on." } },
                    new StoryRule { when_all = new string[] { "old_scent_2", "dog_called_during_dog_section" }, lines = new string[] { "Her whistle. Behind. Holding on. Bark back. Here. Here." }, voice = StoryVoice.dog, objective = "Keep leading her down.", fear_change = -0.1 },
                    new StoryRule { on = "mercy_heard", lines = new string[] { "A sound. Deep. Far. Again. Again. Like the old dog's bark, but huge." }, voice = StoryVoice.dog, schedule_seconds = 4, schedule_event = "mercy_named" },
                    new StoryRule { on = "mercy_named", lines = new string[] { "Mercy. Someone was ringing Mercy, every quarter hour, for the hill." }, objective = "Lead her toward the bell.", fear_change = -0.2 },
                    // The lights (a film).
                    new StoryRule { on = "village_lights", lines = new string[] { "The storm thinned, and Hollin was there below us. Every window was lit.", "Candles, lamps, torches in jam jars. Nobody had power. Everybody had a light.", "And highest of all, under the pines, one brighter than the rest. Mine." }, objective = "", schedule_seconds = 13, schedule_event = "footbridge_reached", cutscene = CutsceneChange.start, cutscene_skip_to = "footbridge_reached" },
                    // The footbridge: his rope and her handrail.
                    new StoryRule { on = "footbridge_reached", objective = "Cross the iced footbridge together.", beat = ChapterBeat.final_obstacle, switching = true, cutscene = CutsceneChange.end },
                    new StoryRule { on = "rope_across", lines = new string[] { "Ice underfoot. The rope in the teeth. Across. Across." }, voice = StoryVoice.dog },
                    new StoryRule { on = "handrail_cleared", lines = new string[] { "I knocked the ice off the handrail rope with my fist, and steadied the sled." } },
                    new StoryRule { when_all = new string[] { "rope_across", "handrail_cleared" }, lines = new string[] { "Over the burn, over the gorge, one step and then the next." }, objective = "Down through the pines. Home.", control = Character.human, beat = ChapterBeat.aftermath, schedule_seconds = 0.5, schedule_event = "crossed", remove = new string[] { "footbridge_ice" } },
                    // Home (a film).
                    new StoryRule { on = "porch_seen", lines = new string[] { "The girl. On the porch. The girl's smell, crying." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 4, schedule_event = "bell_first", cutscene = CutsceneChange.start, cutscene_skip_to = "closing" },
                    new StoryRule { on = "bell_first", lines = new string[] { "Mina was on my porch with a torch. She heard his bell before she saw anything.", "Biscuit barked once. Mina ran." }, fear_change = -0.5, schedule_seconds = 8, schedule_event = "mina_held" },
                    new StoryRule { on = "mina_held", lines = new string[] { "'I kept it lit,' she said into Biscuit's fur. 'Every night. Like you said.'" }, schedule_seconds = 6, schedule_event = "closing" },
                    new StoryRule { on = "closing", lines = new string[] { "The lamp was still in my window. Mina had kept it lit for ten nights.", "Then Mercy rang twelve, and it was Christmas." }, schedule_seconds = 9, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "Chapter Twelve: Christmas Day.", "The twenty-fifth of December." },
            };
        }
    }
}
