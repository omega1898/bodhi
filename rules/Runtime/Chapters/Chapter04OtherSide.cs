// Purpose: the chapter "The Other Side" as data for ChapterRun.
// First generated from the old browser game; since it was removed, this file is the original: edit it here.
namespace Bodhi.Rules
{
    public static partial class StoryChapters
    {
        // Chapter 4: The Other Side.
        public static ChapterScript other_side()
        {
            return new ChapterScript
            {
                id = "other_side",
                title = "The Other Side",
                eyebrow = "Chapter Three",
                place = "A flooded railway station, nightfall",
                dog_age = "six_months",
                dog_size = 0.74,
                dog_jump = 1,
                start_fear = 0.05,
                start_closeness = 0.3,
                things = new ChapterThing[]
                {
                    new ChapterThing { event_name = "power_restored", who = Character.human, label = "Restore the power", requires = new string[] { "separation_scene_finished" }, x = 220, y = 182 },
                    new ChapterThing { event_name = "glove_found", who = Character.dog, label = "Take the glove", x = 2700, y = 520 },
                    new ChapterThing { event_name = "dog_barked", who = Character.dog, label = "Bark at the window", requires = new string[] { "glove_found" }, x = 3010, y = 520 },
                    new ChapterThing { event_name = "fence_touched", who = Character.human, label = "Reach through the fence", requires = new string[] { "pair_met_at_fence" }, x = 3222, y = 520 },
                    new ChapterThing { event_name = "lever_pulled", who = Character.human, label = "Pull the gate lever", requires = new string[] { "fence_touched" }, x = 3460, y = 520 },
                    new ChapterThing { event_name = "debris_cleared", who = Character.dog, label = "Dig through the debris", requires = new string[] { "fence_touched" }, x = 3130, y = 520 },
                    new ChapterThing { event_name = "keepsake_ticket", who = Character.human, label = "Pick up the old ticket", keepsake = "ticket", memory_text = "An old ticket: 'two adults, one dog'. Someone else's holiday.", x = 600, y = 300 },
                    new ChapterThing { event_name = "keepsake_luggage_tag", who = Character.dog, label = "Nose the luggage tag", hidden = true, keepsake = "luggage_tag", memory_text = "A luggage tag, still smelling faintly of someone's perfume. Somebody loved this bag.", x = 1960, y = 520 },
                    new ChapterThing { event_name = "keepsake_bench_scratches", who = Character.dog, label = "Sniff the scratches", hidden = true, keepsake = "bench_scratches", memory_text = "Scratches on the waiting-room bench, at paw height, and grey hairs caught in the wood. Biscuit waited here.", x = 2880, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "walkway_collapsed", who = ZoneWho.human, x = 1150, y = 0, width = 110, height = 320 },
                    new ChapterZone { event_name = "scent_lost", who = ZoneWho.dog, x = 2200, y = 330, width = 200, height = 200 },
                    new ChapterZone { event_name = "pair_met_at_fence", who = ZoneWho.both, requires = new string[] { "dog_barked" }, x = 3030, y = 300, width = 400, height = 260 },
                    new ChapterZone { event_name = "reached_cabin", who = ZoneWho.human, requires = new string[] { "reunion_complete" }, x = 3700, y = 300, width = 300, height = 300 },
                },
                rules = new StoryRule[]
                {
                    new StoryRule { on = "level_started", lines = new string[] { "The station roof still holds. Mostly.", "Past the platforms there's a maintenance cabin. Dry, if we're lucky." }, objective = "Find a way through the station." },
                    new StoryRule { on = "walkway_collapsed", lines = new string[] { "No. No, no, no." }, objective = "", beat = ChapterBeat.separation, schedule_seconds = 3.5, schedule_event = "separation_scene_finished", fear_change = 0.6, closeness_change = 0.4, remove = new string[] { "walkway" }, add = new string[] { "edge_rail" } },
                    new StoryRule { on = "separation_scene_finished", lines = new string[] { "{dog} is down there, on the lower platform. I can hear {them}.", "The signal room. If the power comes back, the gates down there might open." }, objective = "Restore the power in the signal room.", beat = ChapterBeat.human_section },
                    new StoryRule { on = "human_called_during_human_section", lines = new string[] { "A bark answers, somewhere below. Hold on. Hold on." } },
                    new StoryRule { on = "power_restored", lines = new string[] { "Lights. A door, opening.", "Her smell. Rain and soap. That way." }, voice = StoryVoice.dog, objective = "Follow her scent. Sniff to find it.", beat = ChapterBeat.dog_section, control = Character.dog, remove = new string[] { "luggage_gate" } },
                    new StoryRule { on = "dog_sniffed_during_dog_section", lines = new string[] { "Old dog. The fence-dog, from home.", "Here. Days ago. Going west." }, voice = StoryVoice.dog },
                    new StoryRule { on = "scent_lost", lines = new string[] { "Water. Loud. Cold.", "Her smell is gone." }, voice = StoryVoice.dog, fear_change = 0.15 },
                    new StoryRule { on = "glove_found", lines = new string[] { "Her glove.", "Her smell again, strong. The window." }, voice = StoryVoice.dog, objective = "Let her know where you are.", fear_change = -0.2 },
                    new StoryRule { on = "dog_barked", lines = new string[] { "That bark. From the old waiting room.", "And there, a maintenance gantry I walked straight past." }, objective = "Cross the gantry. Get down to the tracks.", beat = ChapterBeat.human_section, control = Character.human, remove = new string[] { "edge_rail" }, add = new string[] { "gantry" } },
                    new StoryRule { on = "pair_met_at_fence", lines = new string[] { "{dog}.", "Soaked. Shaking. The glove in {their} mouth. Alive." }, objective = "Reach through the fence.", beat = ChapterBeat.near_contact },
                    new StoryRule { on = "fence_touched", lines = new string[] { "A warm nose pushes into my palm.", "The gate's jammed. A lever on my side. Debris on {their} side." }, objective = "Pull the lever. Then switch to {dog} and dig.", beat = ChapterBeat.final_obstacle, switching = true, fear_change = -0.3 },
                    new StoryRule { on = "lever_pulled", lines = new string[] { "It grinds down and locks. Come on, {dog}." } },
                    new StoryRule { on = "debris_cleared", lines = new string[] { "Dig. Dig. Through." }, voice = StoryVoice.dog },
                    new StoryRule { when_all = new string[] { "lever_pulled", "debris_cleared" }, lines = new string[] { "The gate gives." }, reunion = true, remove = new string[] { "fence_gate" } },
                    new StoryRule { on = "reunion_embrace", lines = new string[] { "I've got you.", "I've got you. I'm here." } },
                    new StoryRule { on = "reunion_complete", objective = "The cabin is just ahead." },
                    new StoryRule { on = "reached_cabin", lines = new string[] { "The cabin door opens on the first try. Dry floor. A stove." }, schedule_seconds = 4, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "I lit the stove. The glove dried on the hearth.", "{dog} slept against my leg all night.", "Every time I moved, {dog} lifted {their} head to check I was still there." },
            };
        }
    }
}
