// Purpose: Chapter Seven of the Christmas story, "Tam's Sled" (19 December), as data for
// ChapterRun. A day together after five days apart: Em looks after Bodhi, Mina confesses on the
// phone and Em answers with her own guilt, Tam tells the story of Moss and the Long White, and
// they mend his father's sled and set out over the fell before the storm. Every word comes from
// docs/christmas_script.md, Chapter Seven; when the script changes, change it here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: Tam's kitchen (0
// to 900), the yard, workshop and barn (900 to 1800), the practice field (1800 to 2600), the farm
// gate (2600 to 2800) and the drovers' path climbing to the first cairn (2800 to 4000). The floor
// is at y = 520 everywhere.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter Seven: Tam's Sled.
        public static ChapterScript tams_sled()
        {
            return new ChapterScript
            {
                id = "tams_sled",
                title = "Tam's Sled",
                eyebrow = "Chapter Seven: 19 December",
                place = "Tam's farm, and the drovers' path",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0.1,
                start_closeness = 0.8,
                start_hunger = 0.6,
                start_dirt = 0.6,
                things = new ChapterThing[]
                {
                    // Morning: looking after him.
                    new ChapterThing { event_name = "towelled", who = Character.human, label = "Towel {dog} dry", requires = new string[] { "morning" }, needs_dog = true, care = CareKind.bath, x = 300, y = 520 },
                    new ChapterThing { event_name = "paws_brushed", who = Character.human, label = "Pick the ice from his paws", requires = new string[] { "morning" }, needs_dog = true, x = 360, y = 520 },
                    new ChapterThing { event_name = "breakfast", who = Character.human, label = "Give him breakfast", requires = new string[] { "morning" }, needs_dog = true, care = CareKind.meal, x = 430, y = 520 },
                    new ChapterThing { event_name = "keepsake_kitchen_list", who = Character.human, label = "Read the list on the wall", requires = new string[] { "morning" }, keepsake = "kitchen_list", memory_text = "A list on Tam's kitchen wall: 'Hay. Lichen. Ring Jean on Sunday.'", x = 650, y = 520 },
                    new ChapterThing { event_name = "keepsake_calf_bell", who = Character.dog, label = "Sniff the little bell", requires = new string[] { "morning" }, hidden = true, keepsake = "calf_bell", memory_text = "In the barn, a tiny bell on a nail: Clementine's first, too small for her now.", x = 1650, y = 520 },
                    // The phone and Tam.
                    new ChapterThing { event_name = "phone_answered", who = Character.human, label = "Answer the phone", requires = new string[] { "towelled", "paws_brushed", "breakfast", "dog_petted" }, x = 520, y = 520 },
                    new ChapterThing { event_name = "asked_tam", who = Character.human, label = "Ask Tam the way", requires = new string[] { "phone_done" }, x = 300, y = 520 },
                    // The sled.
                    new ChapterThing { event_name = "pin_found", who = Character.dog, label = "Dig in the straw", requires = new string[] { "sled_down" }, hidden = true, x = 1400, y = 520 },
                    new ChapterThing { event_name = "keepsake_sled_initials", who = Character.human, label = "Look under the seat", requires = new string[] { "sled_down" }, keepsake = "sled_initials", memory_text = "Carved under the sled seat: 'A. R. 1951'. Tam's father, who made it.", x = 1100, y = 520 },
                    new ChapterThing { event_name = "pin_fitted", who = Character.human, label = "Fit the pin", requires = new string[] { "pin_found" }, x = 1150, y = 520 },
                    new ChapterThing { event_name = "harness_on", who = Character.human, label = "Put him in the harness", requires = new string[] { "pin_fitted" }, needs_dog = true, x = 1150, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "practice_pull", who = ZoneWho.dog, requires = new string[] { "harness_on" }, x = 2300, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "farm_gate", who = ZoneWho.human, requires = new string[] { "tam_pretended" }, x = 2650, y = 300, width = 120, height = 240 },
                    new ChapterZone { event_name = "first_slope", who = ZoneWho.both, requires = new string[] { "farm_gate" }, x = 3150, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "first_cairn", who = ZoneWho.human, requires = new string[] { "first_slope" }, x = 3750, y = 300, width = 150, height = 240 },
                },
                rules = new StoryRule[]
                {
                    // Morning (a film), then looking after him.
                    new StoryRule { on = "level_started", follow = true, switching = true, fetch = false, schedule_seconds = 3, schedule_event = "sleep_1", cutscene = CutsceneChange.start, cutscene_skip_to = "morning" },
                    new StoryRule { on = "sleep_1", lines = new string[] { "He slept on my feet and woke every hour to check they were still there." }, schedule_seconds = 5, schedule_event = "sleep_2" },
                    new StoryRule { on = "sleep_2", lines = new string[] { "Her feet. Still here. Sleep. Check. Still here." }, voice = StoryVoice.dog, schedule_seconds = 4, schedule_event = "morning" },
                    new StoryRule { on = "morning", objective = "Look after him: towel, paws, breakfast, a cuddle.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "towelled", lines = new string[] { "Five days of hill came out of his curls: ice, burrs, half a pine cone." } },
                    new StoryRule { on = "paws_brushed", lines = new string[] { "Ice balls between his toes, hard as marbles. He let me pick out every one.", "He has never let me touch his feet. He let me then." } },
                    new StoryRule { on = "breakfast", lines = new string[] { "Tam fed us both. Porridge for me. For him, something Tam wouldn't name." } },
                    new StoryRule { on = "dog_petted", lines = new string[] { "He leaned on me so hard I had to brace my feet." } },
                    new StoryRule { when_all = new string[] { "towelled", "paws_brushed", "breakfast", "dog_petted" }, objective = "Answer Tam's phone." },
                    // The phone (a film).
                    new StoryRule { on = "phone_answered", lines = new string[] { "Ama rang on Tam's wall phone. Mina had kept the lamp lit every night.", "She wouldn't go to bed until both of them were home." }, objective = "", schedule_seconds = 7, schedule_event = "mina_on_phone", cutscene = CutsceneChange.start, cutscene_skip_to = "phone_done" },
                    new StoryRule { on = "mina_on_phone", lines = new string[] { "Then Mina, very small on the line: 'It was me. The log store. I didn't latch it.'", "'I let his collar out a notch,' I told her. 'Latches slip. So do collars.'" }, schedule_seconds = 9, schedule_event = "mina_lamp" },
                    new StoryRule { on = "mina_lamp", lines = new string[] { "'Do I keep the lamp on?' 'Every night,' I said. 'Until we're home.'" }, schedule_seconds = 5, schedule_event = "phone_done" },
                    new StoryRule { on = "phone_done", objective = "Ask Tam the way home.", cutscene = CutsceneChange.end },
                    // Tam, and the story of Moss (a film).
                    new StoryRule { on = "asked_tam", lines = new string[] { "'Drovers' path goes over the top to Hollin,' said Tam. 'Three days, with him.'", "'Go now, or you're here till the thaw.'" }, objective = "", schedule_seconds = 7, schedule_event = "moss_1", cutscene = CutsceneChange.start, cutscene_skip_to = "sled_down" },
                    new StoryRule { on = "moss_1", lines = new string[] { "Then, looking at the stove and not at me, Tam told me about Moss.", "'Christmas 'seventy-four. I was twelve, and daft, and I went back for a calf.'" }, schedule_seconds = 8, schedule_event = "moss_2" },
                    new StoryRule { on = "moss_2", lines = new string[] { "'The Long White came down. Three nights in the bothy.'", "'Moss lay on me so I'd not freeze.'" }, schedule_seconds = 7, schedule_event = "moss_3" },
                    new StoryRule { on = "moss_3", lines = new string[] { "'Then she brought me down off the hill in the dark, nose to the old path.'", "'Every window in Hollin was lit. That's what we walked to.'" }, schedule_seconds = 8, schedule_event = "moss_4" },
                    new StoryRule { on = "moss_4", lines = new string[] { "'Snow forgets, lass. A nose remembers.'" }, schedule_seconds = 5, schedule_event = "sled_down" },
                    // The sled.
                    new StoryRule { on = "sled_down", lines = new string[] { "He took his father's sled down off the wall. 'She's been wanting a job,' he said." }, objective = "Switch to {dog}. Find the runner pin in the straw.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "pin_found", lines = new string[] { "Straw. Mice. Old iron, small and cold. Here." }, voice = StoryVoice.dog, objective = "Fit the pin to the runner.", control = Character.human },
                    new StoryRule { on = "pin_fitted", lines = new string[] { "One pin, and the runner sat true." }, objective = "Put {dog} in the harness." },
                    new StoryRule { on = "harness_on", lines = new string[] { "A small harness, soft with fifty years of saddle soap." }, objective = "Switch to {dog}. Pull the empty sled." },
                    new StoryRule { on = "practice_pull", lines = new string[] { "Straps. Weight behind. Heavy. Then not heavy. Then running!" }, voice = StoryVoice.dog, objective = "", schedule_seconds = 3, schedule_event = "tam_pretended" },
                    new StoryRule { on = "tam_pretended", lines = new string[] { "He took to it like he'd been born in a harness. Tam pretended not to be impressed." }, objective = "Say goodbye, and set out.", control = Character.human },
                    // Setting out.
                    new StoryRule { on = "farm_gate", lines = new string[] { "Clementine tried to come too. Tam had to sit on the gate.", "At noon we set out up the drovers' path, Biscuit riding on a sheepskin." }, objective = "Pull together up the first slope.", switching = false },
                    new StoryRule { on = "first_slope", lines = new string[] { "I pushed and he pulled, and the sled went up the hill like it remembered how." }, objective = "" },
                    new StoryRule { on = "first_cairn", lines = new string[] { "At the first cairn I looked back. Tam waved with his whole arm." }, schedule_seconds = 6, schedule_event = "snow_came", cutscene = CutsceneChange.start, cutscene_skip_to = "chapter_done" },
                    new StoryRule { on = "snow_came", lines = new string[] { "Then the snow came down, and the farm was gone." }, schedule_seconds = 6, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "Chapter Eight: Blackwater.", "The twentieth of December." },
            };
        }
    }
}
