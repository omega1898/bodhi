// Purpose: the chapter "The Motorway" as data for ChapterRun.
// First generated from the old browser game; since it was removed, this file is the original: edit it here.
namespace Bodhi.Rules
{
    public static partial class StoryChapters
    {
        // Chapter 5: The Motorway.
        public static ChapterScript motorway()
        {
            return new ChapterScript
            {
                id = "motorway",
                title = "The Motorway",
                eyebrow = "Chapter Four",
                place = "The motorway west, where the water stopped the traffic",
                dog_age = "six_and_a_half_months",
                dog_size = 0.77,
                dog_jump = 1,
                start_fear = 0.15,
                start_closeness = 0.6,
                things = new ChapterThing[]
                {
                    new ChapterThing { event_name = "ladder_taken", who = Character.human, label = "Take the ladder off the van", requires = new string[] { "separation_scene_finished" }, x = 1040, y = 300 },
                    new ChapterThing { event_name = "ladder_laid", who = Character.human, label = "Lay the ladder across the gap", requires = new string[] { "ladder_taken" }, x = 1280, y = 300 },
                    new ChapterThing { event_name = "mina_note", who = Character.human, label = "Read the note under the wiper", requires = new string[] { "ladder_laid" }, x = 2400, y = 300 },
                    new ChapterThing { event_name = "car_food", who = Character.dog, label = "Pull the bag out of the car", food = 2, x = 2300, y = 520 },
                    new ChapterThing { event_name = "dog_barked", who = Character.dog, label = "Bark up at the bridge", requires = new string[] { "biscuit_scent" }, x = 2960, y = 520 },
                    new ChapterThing { event_name = "barrier_touched", who = Character.human, label = "Reach through the barrier", requires = new string[] { "pair_met_at_gate" }, x = 3040, y = 520 },
                    new ChapterThing { event_name = "crank_turned", who = Character.human, label = "Turn the barrier crank", requires = new string[] { "barrier_touched" }, x = 3150, y = 520 },
                    new ChapterThing { event_name = "gravel_dug", who = Character.dog, label = "Dig away the gravel", requires = new string[] { "barrier_touched" }, x = 2965, y = 520 },
                    new ChapterThing { event_name = "keepsake_toy_car", who = Character.human, label = "Pick up the toy car", keepsake = "toy_car", memory_text = "A toy car on the dashboard of a real one. Some child chose it, out of everything, for the journey.", x = 830, y = 300 },
                    new ChapterThing { event_name = "keepsake_dog_bed", who = Character.dog, label = "Nose the dog bed", hidden = true, keepsake = "dog_bed", memory_text = "A dog bed in the boot of an estate car, still smelling of its dog. They took the dog. They left the bed.", x = 1950, y = 520 },
                    new ChapterThing { event_name = "keepsake_windscreen", who = Character.human, label = "Read the windscreen", keepsake = "windscreen", memory_text = "Written with a finger in the dust on a windscreen: 'Gone on foot. Love you. Dad.'", x = 2750, y = 300 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "deck_cracked", who = ZoneWho.human, x = 1380, y = 0, width = 100, height = 320 },
                    new ChapterZone { event_name = "biscuit_scent", who = ZoneWho.dog, requires = new string[] { "ladder_laid" }, x = 2560, y = 330, width = 200, height = 230 },
                    new ChapterZone { event_name = "pair_met_at_gate", who = ZoneWho.both, requires = new string[] { "dog_barked" }, x = 2940, y = 380, width = 420, height = 180 },
                    new ChapterZone { event_name = "left_motorway", who = ZoneWho.human, requires = new string[] { "reunion_complete" }, x = 3850, y = 0, width = 350, height = 600 },
                },
                rules = new StoryRule[]
                {
                    new StoryRule { on = "level_started", lines = new string[] { "The motorway west. A mile of cars, nose to tail, where the water stopped them.", "On the first car door: a chalk heart, and an arrow." }, objective = "Follow the hearts west." },
                    new StoryRule { on = "deck_cracked", lines = new string[] { "The bridge deck just dropped away.", "{dog} went down with it, into the water." }, objective = "", beat = ChapterBeat.separation, schedule_seconds = 3.5, schedule_event = "separation_scene_finished", fear_change = 0.5, closeness_change = 0.3, remove = new string[] { "deck_gap" }, add = new string[] { "edge_rail" } },
                    new StoryRule { on = "separation_scene_finished", lines = new string[] { "He's swimming. He's all right. He's swimming.", "That breakdown van. Breakdown vans carry ladders." }, objective = "Find something to bridge the gap.", beat = ChapterBeat.human_section },
                    new StoryRule { on = "ladder_taken", lines = new string[] { "An aluminium ladder. Long enough. Just." }, objective = "Lay the ladder across the gap." },
                    new StoryRule { on = "ladder_laid", lines = new string[] { "Water. Cars. Her, up high.", "Find the way to her." }, voice = StoryVoice.dog, objective = "Follow the road under the bridge.", beat = ChapterBeat.dog_section, control = Character.dog, remove = new string[] { "edge_rail" }, add = new string[] { "ladder_bridge" } },
                    new StoryRule { on = "car_food", lines = new string[] { "Food. A whole bag. Ours." }, voice = StoryVoice.dog },
                    new StoryRule { on = "biscuit_scent", lines = new string[] { "The fence-dog. He sheltered under this car.", "Days ago. Going west." }, voice = StoryVoice.dog, objective = "Bark. Let her know where you are.", fear_change = -0.1 },
                    new StoryRule { on = "mina_note", lines = new string[] { "'BISCUIT: if anyone finds my dog, he is old and grey and deaf in one ear. Mum is at St Agnes. I'm going there.", "I drew hearts so he can follow me. Mina.'" }, voice = StoryVoice.note },
                    new StoryRule { on = "dog_barked", lines = new string[] { "A bark from below the end of the bridge.", "He was waiting for me at the bottom of the embankment." }, objective = "Get down the embankment at the end of the bridge.", beat = ChapterBeat.human_section, control = Character.human },
                    new StoryRule { on = "pair_met_at_gate", lines = new string[] { "{dog}, soaked to the ears, behind the barrier." }, objective = "Reach through the barrier.", beat = ChapterBeat.near_contact },
                    new StoryRule { on = "barrier_touched", lines = new string[] { "A cold nose. A warm tongue.", "The barrier's chained down. A crank on my side. Gravel piled against it on his." }, objective = "Turn the crank. Then switch to {dog} and dig.", beat = ChapterBeat.final_obstacle, switching = true, fear_change = -0.2 },
                    new StoryRule { on = "crank_turned", lines = new string[] { "It ratchets up. Come on, {dog}." } },
                    new StoryRule { on = "gravel_dug", lines = new string[] { "Dig. Dig. Through." }, voice = StoryVoice.dog },
                    new StoryRule { when_all = new string[] { "crank_turned", "gravel_dug" }, reunion = true, remove = new string[] { "slip_gate" } },
                    new StoryRule { on = "reunion_embrace", lines = new string[] { "You swam. You actually swam.", "Good boy. Good, good boy." } },
                    new StoryRule { on = "reunion_complete", objective = "Keep west." },
                    new StoryRule { on = "left_motorway", lines = new string[] { "By dark we were past the last car." }, schedule_seconds = 4, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "We slept in a camper van with a cracked windscreen.", "On its door, in chalk: a heart, and an arrow, west.", "Somewhere ahead of us, an old deaf dog was following a little girl's hearts." },
            };
        }
    }
}
