// Purpose: Chapter Two of the Christmas story, "The Lamp in the Window" (the night of 14 December,
// to dawn), as data for ChapterRun. Bodhi finds old Biscuit in the blizzard and keeps him alive;
// Em keeps the lamp lit and whistles from the porch every hour; at dawn she finds Biscuit's collar
// on the yard fence while the lorry with both dogs in it drives away below her. Every word comes
// from docs/christmas_script.md, Chapter Two; when the script changes, change it here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: the cabin porch
// (0 to 900, walled off from the woods until dawn), the pines in the blizzard (900 to 3400), the
// lower pines down to the timber yard (3400 to 4800), the yard fence (4800) and the yard with the
// log pile and Dougie's lorry (4800 to 6400). The floor is at y = 520 everywhere.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter Two: The Lamp in the Window.
        public static ChapterScript lamp_in_window()
        {
            return new ChapterScript
            {
                id = "lamp_in_window",
                title = "The Lamp in the Window",
                eyebrow = "Chapter Two: 14 December, night",
                place = "The pines above Hollin, in the blizzard",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0.5,
                start_closeness = 0.6,
                start_hunger = 0.7,
                things = new ChapterThing[]
                {
                    // The blizzard: his nose finds the old dog's scent again each time the wind takes it.
                    new ChapterThing { event_name = "scent_1", who = Character.dog, label = "Follow the scent", requires = new string[] { "opened" }, hidden = true, x = 1400, y = 520 },
                    new ChapterThing { event_name = "scent_2", who = Character.dog, label = "Follow the scent", requires = new string[] { "scent_1" }, hidden = true, x = 1900, y = 520 },
                    new ChapterThing { event_name = "scent_3", who = Character.dog, label = "Follow the scent", requires = new string[] { "scent_2" }, hidden = true, x = 2300, y = 520 },
                    new ChapterThing { event_name = "biscuit_dug_out", who = Character.dog, label = "Dig him out", requires = new string[] { "reached_hollow" }, x = 2650, y = 520 },
                    new ChapterThing { event_name = "lay_close", who = Character.dog, label = "Lie close to him", requires = new string[] { "biscuit_dug_out" }, x = 2650, y = 520 },
                    // The porch.
                    new ChapterThing { event_name = "lamp_turned_up", who = Character.human, label = "Turn up the lamp", requires = new string[] { "porch" }, x = 250, y = 520 },
                    // The hill and the yard.
                    new ChapterThing { event_name = "biscuit_nudged", who = Character.dog, label = "Nudge him up", requires = new string[] { "biscuit_sat" }, x = 4200, y = 520 },
                    new ChapterThing { event_name = "climbed_on_lorry", who = Character.dog, label = "Climb onto the lorry", requires = new string[] { "under_the_fence" }, x = 5450, y = 520 },
                    new ChapterThing { event_name = "keepsake_dougies_note", who = Character.dog, label = "Sniff the cab window", requires = new string[] { "climbed_on_lorry" }, hidden = true, keepsake = "dougies_note", memory_text = "Taped to Dougie's dashboard, in a child's capitals: 'DRIVE SLOW. HOME BY CHRISTMAS. M x'", x = 5980, y = 520 },
                    new ChapterThing { event_name = "sandwich_shared", who = Character.dog, label = "Reach for the sandwich", requires = new string[] { "climbed_on_lorry" }, x = 5950, y = 520 },
                    new ChapterThing { event_name = "stayed", who = Character.dog, label = "Lie down beside him", requires = new string[] { "her_light_seen" }, x = 5700, y = 520 },
                    // Dawn.
                    new ChapterThing { event_name = "keepsake_bird_feeder", who = Character.human, label = "Look at the bird feeder", requires = new string[] { "dawn" }, keepsake = "bird_feeder", memory_text = "A bird feeder on a pine by the lane, full to the top. Someone fills it every morning. Even this one.", x = 2000, y = 520 },
                    new ChapterThing { event_name = "keepsake_yard_mitten", who = Character.human, label = "Look at the mitten", requires = new string[] { "dawn" }, keepsake = "yard_mitten", memory_text = "A child's red mitten on the yard gate post, put up high so its owner would see it.", x = 4600, y = 520 },
                    new ChapterThing { event_name = "collar_found", who = Character.human, label = "Pull it off the wire", requires = new string[] { "dawn" }, x = 4700, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "reached_hollow", who = ZoneWho.dog, requires = new string[] { "scent_3" }, x = 2550, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "yard_light_seen", who = ZoneWho.dog, requires = new string[] { "wind_dropped" }, x = 3500, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "biscuit_sat", who = ZoneWho.dog, requires = new string[] { "yard_light_seen" }, x = 4050, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "under_the_fence", who = ZoneWho.dog, requires = new string[] { "biscuit_nudged" }, x = 4850, y = 300, width = 150, height = 240 },
                    // Walking away from the lorry toward her light: he tries, and comes back.
                    new ChapterZone { event_name = "tried_to_leave", who = ZoneWho.dog, requires = new string[] { "her_light_seen" }, unless = new string[] { "stayed" }, x = 5050, y = 300, width = 150, height = 240 },
                },
                rules = new StoryRule[]
                {
                    // The blizzard (Bodhi).
                    new StoryRule { on = "level_started", control = Character.dog, beat = ChapterBeat.dog_section, follow = false, fetch = false, schedule_seconds = 3, schedule_event = "open_1", cutscene = CutsceneChange.start, cutscene_skip_to = "opened" },
                    new StoryRule { on = "open_1", lines = new string[] { "Snow to the chest. Wind in the ears. The old dog's smell: here. Gone. Here.", "Behind: her whistle, torn up small. Ahead: the old dog, thin as a thread." }, voice = StoryVoice.dog, schedule_seconds = 7, schedule_event = "opened" },
                    new StoryRule { on = "opened", lines = new string[] { "Ahead, then." }, voice = StoryVoice.dog, objective = "Follow Biscuit's scent. Sniff when the wind takes it.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "scent_1", lines = new string[] { "Here. The old dog stood here. Not long.", "Wind steals it. Wait. Nose low." }, voice = StoryVoice.dog },
                    new StoryRule { on = "scent_2", lines = new string[] { "Wind gives it back. Fear, and old blanket, and toast. That way." }, voice = StoryVoice.dog },
                    new StoryRule { on = "scent_3", lines = new string[] { "Closer. Warm now. Warm and very tired." }, voice = StoryVoice.dog },
                    new StoryRule { on = "reached_hollow", lines = new string[] { "Under the fallen tree. The old dog. Cold, cold, shaking.", "He doesn't get up. He always gets up." }, voice = StoryVoice.dog, objective = "Dig Biscuit out." },
                    new StoryRule { on = "biscuit_dug_out", lines = new string[] { "Dig. Dig. Snow in the eyes. Dig." }, voice = StoryVoice.dog, objective = "Lie close to Biscuit. Keep him warm." },
                    // Lying close (a film).
                    new StoryRule { on = "lay_close", lines = new string[] { "Lie close. Closer. Be the blanket." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 4, schedule_event = "warming_2", cutscene = CutsceneChange.start, cutscene_skip_to = "porch" },
                    new StoryRule { on = "warming_2", lines = new string[] { "His heart, slow and far away.", "Shaking. Less. Less." }, voice = StoryVoice.dog, schedule_seconds = 5.5, schedule_event = "warming_3" },
                    new StoryRule { on = "warming_3", lines = new string[] { "Stopped." }, voice = StoryVoice.dog, schedule_seconds = 3.5, schedule_event = "porch" },
                    // The porch (Em), two in the morning.
                    new StoryRule { on = "porch", lines = new string[] { "'We'll freeze, Em,' Ama said. 'First light.' She was right. I hated her for it.", "They went down the lane with the torch. I went in and didn't take my coat off." }, objective = "Turn up the lamp.", control = Character.human, beat = ChapterBeat.human_section, cutscene = CutsceneChange.end },
                    new StoryRule { on = "lamp_turned_up", lines = new string[] { "I turned the lamp up. Then I turned it up again.", "His collar was in my coat pocket. Every time I moved, the bell rang." }, objective = "Whistle for {dog} from the porch." },
                    // The hours (a film). It waits for the lamp, even if she whistled first.
                    new StoryRule { when_all = new string[] { "lamp_turned_up", "human_called_during_human_section" }, lines = new string[] { "Every hour I went out on the porch and whistled." }, objective = "", schedule_seconds = 4, schedule_event = "hours_2", cutscene = CutsceneChange.start, cutscene_skip_to = "wind_dropped" },
                    new StoryRule { on = "hours_2", lines = new string[] { "Every hour, the wind took it before it was finished.", "Three o'clock. Four. Five. The stove went out, and I didn't notice." }, schedule_seconds = 7.5, schedule_event = "wind_dropped" },
                    // The hill (Bodhi).
                    new StoryRule { on = "wind_dropped", lines = new string[] { "The wind lies down. All at once, like a dog.", "The old dog lifts his head. Stands. Falls. Stands." }, voice = StoryVoice.dog, objective = "Lead Biscuit down the hill. Wait for him.", control = Character.dog, beat = ChapterBeat.dog_section, cutscene = CutsceneChange.end },
                    new StoryRule { on = "yard_light_seen", lines = new string[] { "Down there: a light. Orange. Wrong orange. Not hers. But warm. Go." }, voice = StoryVoice.dog },
                    new StoryRule { on = "biscuit_sat", lines = new string[] { "He sits down in the snow. He won't." }, voice = StoryVoice.dog, objective = "Nudge Biscuit up." },
                    new StoryRule { on = "biscuit_nudged", lines = new string[] { "Nose under his chin. Up. Up, old dog.", "Up." }, voice = StoryVoice.dog, objective = "Lead him to the light." },
                    new StoryRule { on = "under_the_fence", lines = new string[] { "Under the wire. Belly flat. The old dog, slower. Through." }, voice = StoryVoice.dog, objective = "Climb the logs onto the lorry." },
                    new StoryRule { on = "climbed_on_lorry", lines = new string[] { "Logs. Up. Under the flapping cloth: trees, lying down.", "Out of the wind. Pine and diesel. The old dog, beside." }, voice = StoryVoice.dog, objective = "Find something to eat." },
                    new StoryRule { on = "sandwich_shared", lines = new string[] { "The cab. A crack of window. Ham, under the seat. Reach. Reach." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 3, schedule_event = "sandwich_halved" },
                    new StoryRule { on = "sandwich_halved", lines = new string[] { "Half for the old dog. The bigger half." }, voice = StoryVoice.dog, care_hunger = 0.3, schedule_seconds = 4, schedule_event = "her_light" },
                    // Her light (a film), then the choice.
                    new StoryRule { on = "her_light", lines = new string[] { "Up the hill, through the trees. A light. Very small. Very far. Hers." }, voice = StoryVoice.dog, schedule_seconds = 5, schedule_event = "her_light_seen", cutscene = CutsceneChange.start, cutscene_skip_to = "her_light_seen" },
                    new StoryRule { on = "her_light_seen", lines = new string[] { "Go to it?" }, voice = StoryVoice.dog, objective = "Go to her light, or stay with Biscuit.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "tried_to_leave", lines = new string[] { "Down. Go. Her light. Behind: the old dog, trying to stand. Falling.", "Back." }, voice = StoryVoice.dog },
                    new StoryRule { on = "stayed", lines = new string[] { "The old dog can't stand.", "Stay, then. Stay." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 6, schedule_event = "dawn", cutscene = CutsceneChange.start, cutscene_skip_to = "dawn" },
                    // Dawn (Em).
                    new StoryRule { on = "dawn", lines = new string[] { "The snow stopped at six. The world was new and smooth and blue, and empty.", "Ama came up with Mina at first light. Nobody said anything. We went down the lane." }, objective = "Search the lane.", control = Character.human, beat = ChapterBeat.human_section, cutscene = CutsceneChange.end, remove = new string[] { "night_wall" } },
                    new StoryRule { on = "collar_found", lines = new string[] { "On the fence wire at the top of the yard: Biscuit's blue collar. 'If found, call Ama.'", "Ama read it twice, as if it might say something else the second time.", "Mina held out her hand for it. I didn't give it to her." }, objective = "", schedule_seconds = 10, schedule_event = "the_promise", cutscene = CutsceneChange.start, cutscene_skip_to = "lorry_gone" },
                    new StoryRule { on = "the_promise", lines = new string[] { "'I'll bring him home in this,' I told her. I had no right to promise it.", "'A light for the hill,' I said. It was all I had.", "'And the hill brings them home,' said Mina.", "'There's a second half,' she said. 'Gran taught me. Nobody says it now.'" }, schedule_seconds = 13, schedule_event = "engine_started" },
                    new StoryRule { on = "engine_started", lines = new string[] { "Down in the yard an engine started. A lorry went down the valley road.", "I didn't even look up." }, schedule_seconds = 6, schedule_event = "lorry_gone" },
                    new StoryRule { on = "lorry_gone", lines = new string[] { "The floor shakes. Pine and diesel. Her light, getting small." }, voice = StoryVoice.dog, schedule_seconds = 5, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "Chapter Three: Kell Bridge.", "The fifteenth of December." },
            };
        }
    }
}
