// Purpose: the chapter "Visiting Hours" as data for ChapterRun.
// First generated from the old browser game; since it was removed, this file is the original: edit it here.
namespace Bodhi.Rules
{
    public static partial class StoryChapters
    {
        // Chapter 6: Visiting Hours.
        public static ChapterScript visiting_hours()
        {
            return new ChapterScript
            {
                id = "visiting_hours",
                title = "Visiting Hours",
                eyebrow = "Chapter Five",
                place = "St Agnes Hospital, the children's wing",
                dog_age = "seven_months",
                dog_size = 0.8,
                dog_jump = 1,
                start_fear = 0.2,
                start_closeness = 0.85,
                things = new ChapterThing[]
                {
                    new ChapterThing { event_name = "note_read", who = Character.human, label = "Read the note", x = 900, y = 300 },
                    new ChapterThing { event_name = "basement_unlocked", who = Character.human, label = "Release the basement locks", requires = new string[] { "separation_scene_finished" }, x = 1900, y = 300 },
                    new ChapterThing { event_name = "lift_called", who = Character.dog, label = "Paw the lift button", x = 2500, y = 520 },
                    new ChapterThing { event_name = "glass_touched", who = Character.human, label = "Press your hand to the glass", requires = new string[] { "pair_met_at_lift" }, x = 2776, y = 520 },
                    new ChapterThing { event_name = "crank_turned", who = Character.human, label = "Crank the lift doors", requires = new string[] { "glass_touched" }, x = 3010, y = 520 },
                    new ChapterThing { event_name = "cord_pulled", who = Character.dog, label = "Pull the emergency cord", requires = new string[] { "glass_touched" }, x = 2650, y = 520 },
                    new ChapterThing { event_name = "keepsake_get_well_card", who = Character.human, label = "Read the card", keepsake = "get_well_card", memory_text = "A get-well card in a child's writing: 'Dear Nurse Ama, thank you for staying.'", x = 700, y = 300 },
                    new ChapterThing { event_name = "keepsake_pet_bracelet", who = Character.dog, label = "Nose the tiny bracelet", hidden = true, keepsake = "pet_bracelet", memory_text = "A tiny hospital bracelet, too small for a person: 'VISITOR: BISCUIT (DOG)'. Ama brought him to the children's ward on Thursdays.", x = 1300, y = 520 },
                    new ChapterThing { event_name = "keepsake_crayon_dog", who = Character.dog, label = "Look at the drawing", keepsake = "crayon_dog", memory_text = "A crayon drawing of a grey dog with a blue collar, signed 'Mina'. Taped low on the wall, at a dog's eye level. On purpose.", x = 2200, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "doors_closed", who = ZoneWho.human, x = 1360, y = 0, width = 80, height = 300 },
                    new ChapterZone { event_name = "rabbit_found", who = ZoneWho.dog, x = 1860, y = 330, width = 80, height = 200 },
                    new ChapterZone { event_name = "pair_met_at_lift", who = ZoneWho.both, requires = new string[] { "lift_called" }, x = 2560, y = 300, width = 420, height = 260 },
                    new ChapterZone { event_name = "reached_ward", who = ZoneWho.human, requires = new string[] { "reunion_complete" }, x = 3550, y = 300, width = 250, height = 300 },
                },
                rules = new StoryRule[]
                {
                    new StoryRule { on = "level_started", lines = new string[] { "Since the station, {dog} won't leave my side.", "Not even a step ahead. I don't mind." }, objective = "Find somewhere safe to sleep." },
                    new StoryRule { on = "note_read", lines = new string[] { "'Power failing. Most evacuated Tuesday.", "I've stayed with the four who couldn't be moved. I won't leave them alone in the dark.' Ama, night nurse." }, voice = StoryVoice.note },
                    new StoryRule { on = "doors_closed", lines = new string[] { "The fire doors slammed shut behind me.", "{dog}. Claws on the stairwell. Going down." }, beat = ChapterBeat.separation, schedule_seconds = 3, schedule_event = "separation_scene_finished", fear_change = 0.5, add = new string[] { "fire_door" } },
                    new StoryRule { on = "separation_scene_finished", lines = new string[] { "The security desk is at the end of the ward. Hold on, {dog}." }, objective = "Find the security desk. Unlock the basement.", beat = ChapterBeat.human_section },
                    new StoryRule { on = "basement_unlocked", lines = new string[] { "Click. A door. Dark below.", "Her voice, through the walls. Far. Above." }, voice = StoryVoice.dog, objective = "Find a way up to her. Small spaces are your spaces.", beat = ChapterBeat.dog_section, control = Character.dog, remove = new string[] { "basement_door" } },
                    new StoryRule { on = "rabbit_found", lines = new string[] { "Soft. Small. Smells of a child, a long time ago.", "Carry it." }, voice = StoryVoice.dog, fear_change = -0.25 },
                    new StoryRule { on = "lift_called", lines = new string[] { "Somewhere below, a lift chimed.", "The doors at the end of the corridor shuddered open. An empty shaft, and a way down." }, objective = "Go down the lift shaft.", beat = ChapterBeat.human_section, control = Character.human, remove = new string[] { "lift_door_upper" } },
                    new StoryRule { on = "pair_met_at_lift", lines = new string[] { "Through the glass: {dog}. With a stuffed rabbit, of all things.", "Tail going so hard {their} whole body shakes." }, objective = "Press your hand to the glass.", beat = ChapterBeat.near_contact },
                    new StoryRule { on = "glass_touched", lines = new string[] { "A nose against the other side. Fog on the glass.", "The crank's jammed on my side. There's an emergency cord on {their} side." }, objective = "Turn the crank. Switch to {dog} and pull the cord.", beat = ChapterBeat.final_obstacle, switching = true },
                    new StoryRule { when_all = new string[] { "crank_turned", "cord_pulled" }, reunion = true, remove = new string[] { "lift_glass" } },
                    new StoryRule { on = "reunion_embrace", lines = new string[] { "You found me.", "You keep finding me." } },
                    new StoryRule { on = "reunion_complete", objective = "The children's ward is just ahead." },
                    new StoryRule { on = "reached_ward", lines = new string[] { "The children's ward. Someone painted stars on the ceiling.", "Taped to the door, a letter in the same hand as the note upstairs:" }, schedule_seconds = 5, schedule_event = "ama_letter" },
                    new StoryRule { on = "ama_letter", lines = new string[] { "'Mina. If you get here: they came for us on Friday. We have gone north with the patients, to Northgate.", "Follow the river. I love you more than anything. Mum.'" }, voice = StoryVoice.note, schedule_seconds = 11, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "We slept in the children's ward, under painted stars.", "{dog} wouldn't let go of the rabbit.", "I thought about Ama, who stayed. I understood her now." },
            };
        }
    }
}
