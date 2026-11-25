// Purpose: the chapter "The Orchard" as data for ChapterRun.
// First generated from the old browser game; since it was removed, this file is the original: edit it here.
namespace Bodhi.Rules
{
    public static partial class StoryChapters
    {
        // Chapter 7: The Orchard.
        public static ChapterScript orchard()
        {
            return new ChapterScript
            {
                id = "orchard",
                title = "The Orchard",
                eyebrow = "Chapter Six",
                place = "High Acre Farm, an orchard gone wild",
                dog_age = "eight_months",
                dog_size = 0.84,
                dog_jump = 1,
                start_fear = 0.05,
                start_closeness = 0.5,
                things = new ChapterThing[]
                {
                    new ChapterThing { event_name = "pantry_food", who = Character.human, label = "Take the dog biscuits from the pantry", food = 3, x = 420, y = 520 },
                    new ChapterThing { event_name = "key_found", who = Character.human, label = "Search the farmhouse for a key", requires = new string[] { "separation_scene_finished" }, x = 560, y = 520 },
                    new ChapterThing { event_name = "apple_eaten", who = Character.dog, label = "Eat a windfall apple", care = CareKind.treat, x = 1620, y = 520 },
                    new ChapterThing { event_name = "dog_barked", who = Character.dog, label = "Bark from the hayloft door", requires = new string[] { "biscuit_bed" }, x = 3050, y = 380 },
                    new ChapterThing { event_name = "key_turned", who = Character.human, label = "Turn the key", requires = new string[] { "pair_met_at_gate" }, x = 1380, y = 520 },
                    new ChapterThing { event_name = "bolt_pulled", who = Character.dog, label = "Pull the bolt with your teeth", requires = new string[] { "key_turned" }, x = 1440, y = 520 },
                    new ChapterThing { event_name = "keepsake_farm_diary", who = Character.human, label = "Read the farmer's diary", keepsake = "farm_diary", memory_text = "The farmer's diary, kept every day for thirty years. The last page: 'Leaving for my sister's. Somebody eat the apples.'", x = 700, y = 520 },
                    new ChapterThing { event_name = "keepsake_school_scarf", who = Character.dog, label = "Nose the scarecrow's scarf", hidden = true, keepsake = "school_scarf", memory_text = "The scarecrow wears a child's scarf, green and grey, tied on carefully. Left where an old dog would smell it.", x = 1950, y = 520 },
                    new ChapterThing { event_name = "keepsake_barn_beam", who = Character.human, label = "Read the names on the beam", keepsake = "barn_beam", memory_text = "A hundred years of names carved in the barn beam. The newest, in chalk: 'MINA + BISCUIT', and a heart.", x = 3300, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "orchard_gate_found", who = ZoneWho.human, x = 1250, y = 0, width = 100, height = 600 },
                    new ChapterZone { event_name = "biscuit_bed", who = ZoneWho.dog, requires = new string[] { "dog_turn" }, x = 2900, y = 290, width = 220, height = 100 },
                    new ChapterZone { event_name = "pair_met_at_gate", who = ZoneWho.both, requires = new string[] { "dog_barked" }, x = 1300, y = 300, width = 220, height = 260 },
                    new ChapterZone { event_name = "barn_rest", who = ZoneWho.human, requires = new string[] { "reunion_complete" }, x = 2900, y = 280, width = 300, height = 320 },
                },
                rules = new StoryRule[]
                {
                    new StoryRule { on = "level_started", lines = new string[] { "An orchard, gone wild. Apples everywhere, rotting sweet.", "A farmhouse with its door open, and no smoke from the chimney." }, objective = "Find somewhere to sleep." },
                    new StoryRule { on = "orchard_gate_found", lines = new string[] { "That smell. The fence-dog. HERE!" }, voice = StoryVoice.dog, beat = ChapterBeat.separation, follow = false, schedule_seconds = 3, schedule_event = "separation_scene_finished" },
                    new StoryRule { on = "separation_scene_finished", lines = new string[] { "Under the orchard gate and gone, before I could grab him.", "The gate is locked. The farmhouse, then." }, objective = "Find the key to the orchard gate. Try the farmhouse.", beat = ChapterBeat.human_section },
                    new StoryRule { on = "key_found", lines = new string[] { "A key on a nail, and a note pinned under it in the farmer's hand:", "'The old grey dog came back tonight. Gave him milk. He slept in the barn. He won't stay. He's looking for someone.'" }, voice = StoryVoice.note, schedule_seconds = 7, schedule_event = "dog_turn" },
                    new StoryRule { on = "dog_turn", lines = new string[] { "Apples. Grass. And him, everywhere.", "His smell. To the big wooden house." }, voice = StoryVoice.dog, objective = "Follow the old dog's trail to the barn.", beat = ChapterBeat.dog_section, control = Character.dog, follow = true },
                    new StoryRule { on = "apple_eaten", lines = new string[] { "Sweet. Crunch. Another." }, voice = StoryVoice.dog },
                    new StoryRule { on = "biscuit_bed", lines = new string[] { "Here. He slept here. Still warm. This morning." }, voice = StoryVoice.dog, objective = "Bark from the hayloft door. Tell her." },
                    new StoryRule { on = "dog_barked", lines = new string[] { "A bark from high in the barn.", "Then he came racing back through the trees to the gate, ears flying." }, objective = "Unlock the orchard gate.", beat = ChapterBeat.human_section, control = Character.human, follow = false },
                    new StoryRule { on = "pair_met_at_gate", lines = new string[] { "Nose through the bars. Tail going round in circles." }, objective = "Turn the key.", beat = ChapterBeat.near_contact },
                    new StoryRule { on = "key_turned", lines = new string[] { "The key turns. But the bolt on his side has rusted shut." }, objective = "Switch to {dog}. Pull the bolt with your teeth.", beat = ChapterBeat.final_obstacle, switching = true },
                    new StoryRule { on = "bolt_pulled", lines = new string[] { "Pull. Pull. Click!" }, voice = StoryVoice.dog },
                    new StoryRule { when_all = new string[] { "key_turned", "bolt_pulled" }, reunion = true, remove = new string[] { "orchard_gate" } },
                    new StoryRule { on = "reunion_embrace", lines = new string[] { "There you are.", "He was here, wasn't he? I know. I know." } },
                    new StoryRule { on = "reunion_complete", lines = new string[] { "Apples for supper. For both of us." }, objective = "Bed down in the barn, where he slept." },
                    new StoryRule { on = "barn_rest", lines = new string[] { "The hay still had the shape of him in it." }, schedule_seconds = 5, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "We slept in the hay where Biscuit had slept the night before.", "{dog} couldn't settle until he had sniffed every inch of it.", "One day behind him now. Maybe less." },
            };
        }
    }
}
