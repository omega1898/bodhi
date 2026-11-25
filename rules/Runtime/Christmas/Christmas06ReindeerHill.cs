// Purpose: Chapter Six of the Christmas story, "The Reindeer Hill" (the night of 17 December), as
// data for ChapterRun. The midpoint: Bodhi climbs to Tam's farm with Biscuit, turns the runaway
// calf back into her pen and barks for help; Tam carries Biscuit to his stove. Em climbs after
// them to a yard light under the aurora, forces the iced field gate and whistles, and he comes.
// The reunion, the collar back on (the right notch), and in Tam's kitchen the old herder knows
// the old dog, and the radio says the storm is coming for Christmas Eve. Every word comes from
// docs/christmas_script.md, Chapter Six; when the script changes, change it here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: the forest climbing
// up the hill (0 to 1700), the field gate (1700 to 1900), the reindeer pens and barn (1900 to
// 2900), Tam's farmhouse (2900 to 3600) and the open moor (3600 to 4200). The floor is at
// y = 520 everywhere.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter Six: The Reindeer Hill.
        public static ChapterScript reindeer_hill()
        {
            return new ChapterScript
            {
                id = "reindeer_hill",
                title = "The Reindeer Hill",
                eyebrow = "Chapter Six: 17 December, night",
                place = "Tam's reindeer farm, on the moor above the railway",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0.5,
                start_closeness = 0.7,
                start_hunger = 0.6,
                things = new ChapterThing[]
                {
                    // The hill (Bodhi).
                    new ChapterThing { event_name = "keepsake_antler", who = Character.dog, label = "Chew the antler", requires = new string[] { "climbing" }, hidden = true, keepsake = "gnawed_antler", memory_text = "An antler shed in the snow, gnawed smooth by fifty years of herding dogs. He added his own marks.", x = 2600, y = 520 },
                    new ChapterThing { event_name = "calf_penned", who = Character.dog, label = "Pull the gate rope", requires = new string[] { "round_the_calf" }, x = 2300, y = 520 },
                    // The climb (Em).
                    new ChapterThing { event_name = "field_gate_opened", who = Character.human, label = "Force the gate open", requires = new string[] { "saw_yard_light" }, x = 1740, y = 520 },
                    // Through the gate, before she whistles. (Nothing can be used once a reunion has
                    // begun, so the collar goes back on as the reunion ends.)
                    new ChapterThing { event_name = "keepsake_herd_book", who = Character.human, label = "Read the herd book", requires = new string[] { "field_gate_opened" }, keepsake = "herd_book", memory_text = "Tam's herd book: every reindeer named for fifty years, first in his father's hand, then his.", x = 2500, y = 520 },
                    new ChapterThing { event_name = "keepsake_moss_photo", who = Character.human, label = "Look at the photograph", requires = new string[] { "field_gate_opened" }, keepsake = "moss_photo", memory_text = "A photograph by Tam's door: a boy and a collie in deep snow. On the back: 'Moss. Winter '74.'", x = 3200, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "reached_pens", who = ZoneWho.dog, requires = new string[] { "climbing" }, x = 2050, y = 300, width = 150, height = 240 },
                    // Beyond the calf, on the moor side, so he can turn her back toward the pen.
                    new ChapterZone { event_name = "round_the_calf", who = ZoneWho.dog, requires = new string[] { "reached_pens" }, x = 3700, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "saw_yard_light", who = ZoneWho.human, requires = new string[] { "em_climbing" }, x = 1400, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "into_the_kitchen", who = ZoneWho.human, requires = new string[] { "puppy_book_line" }, x = 3300, y = 300, width = 120, height = 240 },
                },
                rules = new StoryRule[]
                {
                    // The hill (Bodhi).
                    new StoryRule { on = "level_started", control = Character.dog, beat = ChapterBeat.dog_section, follow = false, fetch = false, schedule_seconds = 3, schedule_event = "forest_1", cutscene = CutsceneChange.start, cutscene_skip_to = "climbing" },
                    new StoryRule { on = "forest_1", lines = new string[] { "Trees. Dark. Then big animals. Moss and breath. Not dogs.", "The old dog stumbles. Gets up. Stumbles. Up, old dog. Nearly." }, voice = StoryVoice.dog, schedule_seconds = 7, schedule_event = "climbing" },
                    new StoryRule { on = "climbing", objective = "Follow the smell of the big animals.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "reached_pens", lines = new string[] { "Wood. Bells. Thirty big animals, breathing, all of them watching.", "One small one, white face, outside the fence. Going the wrong way." }, voice = StoryVoice.dog, objective = "Turn the calf back into her pen." },
                    new StoryRule { on = "round_the_calf", lines = new string[] { "Something old wakes up in the legs. Low. Wide. Round her.", "Round. Round. In." }, voice = StoryVoice.dog, objective = "Pull the gate rope shut." },
                    new StoryRule { on = "calf_penned", lines = new string[] { "Rope in the teeth. Pull. The gate swings. Click.", "The small one inside. All the big ones, watching. Good." }, voice = StoryVoice.dog, objective = "Bark for help. Biscuit can't go on." },
                    // Tam (a film).
                    new StoryRule { when_all = new string[] { "calf_penned", "dog_called_during_dog_section" }, lines = new string[] { "Bark. Bark. A door. A light, swinging. A man who smells of animals and smoke." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 5, schedule_event = "tam_found_them", cutscene = CutsceneChange.start, cutscene_skip_to = "em_climbing" },
                    new StoryRule { on = "tam_found_them", lines = new string[] { "Tam told me later what he found: two dogs and a calf, all safe in his pen.", "'Well,' he'd said, with the torch on them. 'Who taught you that?'" }, schedule_seconds = 8, schedule_event = "tam_carried" },
                    new StoryRule { on = "tam_carried", lines = new string[] { "He carried Biscuit in to his stove like a lamb." }, schedule_seconds = 4, schedule_event = "em_climbing" },
                    // The climb (Em). Near contact: only a whistle from here on calls him.
                    new StoryRule { on = "em_climbing", lines = new string[] { "I followed the prints uphill until my lantern was the only light there was." }, objective = "Follow the prints uphill.", control = Character.human, beat = ChapterBeat.near_contact, cutscene = CutsceneChange.end },
                    new StoryRule { on = "saw_yard_light", lines = new string[] { "Then a yard light, above the trees. And above that, the sky caught fire. Green." }, objective = "Open the field gate." },
                    new StoryRule { on = "field_gate_opened", lines = new string[] { "The field gate was iced shut. I put my shoulder to it." }, objective = "Whistle for {dog}.", remove = new string[] { "field_gate" } },
                    new StoryRule { when_all = new string[] { "field_gate_opened", "human_called_during_near_contact" }, lines = new string[] { "I whistled. Not hope, by then. Habit." }, objective = "", schedule_seconds = 2.5, schedule_event = "he_heard" },
                    // The reunion: earned by both of them, his gate rope and her gate.
                    new StoryRule { when_all = new string[] { "calf_penned", "field_gate_opened", "he_heard" }, lines = new string[] { "Her whistle. Real. Close. Not wind.", "Run." }, voice = StoryVoice.dog, beat = ChapterBeat.reunion, reunion = true, fear_change = -0.5 },
                    new StoryRule { on = "reunion_embrace", lines = new string[] { "He hit me so hard we both went over in the snow." } },
                    new StoryRule { on = "reunion_complete", lines = new string[] { "I buckled his collar back on. The right notch, this time. The bell rang.", "I cried at a bell." }, beat = ChapterBeat.aftermath, schedule_seconds = 6, schedule_event = "puppy_book_line" },
                    new StoryRule { on = "puppy_book_line", lines = new string[] { "I remembered the line I'd underlined: 'Check the collar after a bath. Wet curls shrink.'" }, objective = "Go into Tam's kitchen." },
                    // Tam's kitchen (a film).
                    new StoryRule { on = "into_the_kitchen", lines = new string[] { "In Tam's kitchen Biscuit lay by the stove. Tam lifted his chin to the light.", "'I know you,' he said. 'You're Bess's. Born in that barn, thirteen year back.'" }, objective = "", schedule_seconds = 9, schedule_event = "kitchen_2", cutscene = CutsceneChange.start, cutscene_skip_to = "closing" },
                    new StoryRule { on = "kitchen_2", lines = new string[] { "'In a Long White an old herd dog goes out to bring them in. He can't help it.'", "I buckled Biscuit's blue collar on him. 'I said I'd bring him home in this.'" }, schedule_seconds = 9, schedule_event = "radio" },
                    new StoryRule { on = "radio", lines = new string[] { "'Roads closed above Kell Bridge,' said Valley Radio. 'The line is shut at Skelder.'", "'And a warning for Christmas Eve.' Tam turned it off. 'Long White,' he said." }, schedule_seconds = 9, schedule_event = "closing" },
                    new StoryRule { on = "closing", lines = new string[] { "Hollin was cut off. Christmas was a week away.", "And Biscuit couldn't walk." }, schedule_seconds = 8, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "Chapter Seven: Tam's Sled.", "The nineteenth of December." },
            };
        }
    }
}
