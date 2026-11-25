// Purpose: Chapter Four of the Christmas story, "Strand" (16 December), as data for ChapterRun.
// Biscuit leads them to the red door where he was young; it never opens, and from then on Bodhi
// leads. Kofi shares his chestnuts, Bodhi shoves Biscuit back from the bus, and they live on what
// is dropped under the market huts. Em works the market with posters until six, when the whole
// square counts down for the tree and her whistle is lost under the band, one row of huts away.
// The dogs sleep in Santa's grotto, facing her last poster. Every word comes from
// docs/christmas_script.md, Chapter Four; when the script changes, change it here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: Kirk Wynd and its
// red door (0 to 1200), the road and the bus stop (1200 to 1600), the market square with the
// chestnut stall, the carousel, the nativity stable and the town tree (1600 to 3900), the town
// hall and its steps (3900 to 4350), and Santa's grotto and the bus shelter facing it (4350 to
// 5400). The floor is at y = 520 everywhere.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter Four: Strand.
        public static ChapterScript strand()
        {
            return new ChapterScript
            {
                id = "strand",
                title = "Strand",
                eyebrow = "Chapter Four: 16 December",
                place = "Strand, the market town at the foot of the valley",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0.5,
                start_closeness = 0.6,
                start_hunger = 0.8,
                things = new ChapterThing[]
                {
                    // Dusk (Bodhi).
                    new ChapterThing { event_name = "biscuit_shoved", who = Character.dog, label = "Shove him back", requires = new string[] { "kerb" }, x = 1420, y = 520 },
                    new ChapterThing { event_name = "pretzel_found", who = Character.dog, label = "Eat it", requires = new string[] { "market" }, hidden = true, x = 2300, y = 520 },
                    new ChapterThing { event_name = "keepsake_railing_mitten", who = Character.dog, label = "Sniff the railing", requires = new string[] { "market" }, hidden = true, keepsake = "railing_mitten", memory_text = "A lost mitten, clipped to a market railing at a child's height, so its owner could reach it.", x = 2700, y = 520 },
                    // Six o'clock (Em).
                    new ChapterThing { event_name = "asked_at_stall", who = Character.human, label = "Ask at the chestnut stall", requires = new string[] { "em_bus" }, x = 2000, y = 520 },
                    new ChapterThing { event_name = "poster_carousel", who = Character.human, label = "Put up a poster", requires = new string[] { "asked_at_stall" }, x = 2850, y = 520 },
                    new ChapterThing { event_name = "poster_stall", who = Character.human, label = "Put up a poster", requires = new string[] { "asked_at_stall" }, x = 2080, y = 520 },
                    new ChapterThing { event_name = "keepsake_porch_photo", who = Character.human, label = "Look at the photograph", requires = new string[] { "em_bus" }, keepsake = "first_tree_photo", memory_text = "In the town-hall porch, a photograph sixty years old: the first Strand tree, and everyone under it.", x = 4050, y = 520 },
                    new ChapterThing { event_name = "keepsake_letter_board", who = Character.human, label = "Read the letter board", requires = new string[] { "em_bus" }, keepsake = "grotto_letter", memory_text = "Pinned to the grotto's letter board: 'Dear Santa, please can Grandad's dog come home from the vet.'", x = 4650, y = 520 },
                    // Night.
                    new ChapterThing { event_name = "crate_pushed", who = Character.dog, label = "Push the box", requires = new string[] { "near_miss_done" }, x = 4400, y = 520 },
                    new ChapterThing { event_name = "last_poster", who = Character.human, label = "Put up your last poster", requires = new string[] { "em_last_poster" }, x = 5000, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "red_door", who = ZoneWho.dog, requires = new string[] { "arrived" }, x = 520, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "kerb", who = ZoneWho.dog, requires = new string[] { "kofi_done" }, x = 1300, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "market", who = ZoneWho.dog, requires = new string[] { "biscuit_shoved" }, x = 1700, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "steps_climbed", who = ZoneWho.human, requires = new string[] { "poster_carousel", "poster_stall" }, x = 3950, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "into_the_grotto", who = ZoneWho.dog, requires = new string[] { "crate_pushed" }, x = 4600, y = 300, width = 120, height = 240 },
                },
                rules = new StoryRule[]
                {
                    // Dusk (Bodhi).
                    new StoryRule { on = "level_started", control = Character.dog, beat = ChapterBeat.dog_section, follow = false, fetch = false, schedule_seconds = 3, schedule_event = "dusk_1", cutscene = CutsceneChange.start, cutscene_skip_to = "arrived" },
                    new StoryRule { on = "dusk_1", lines = new string[] { "Boots. Wheels. Sugar, burning. A hundred people, all at once.", "Too many smells. Too many feet. The old dog, pressed against a wall." }, voice = StoryVoice.dog, schedule_seconds = 7, schedule_event = "arrived" },
                    new StoryRule { on = "arrived", objective = "Follow Biscuit.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "red_door", lines = new string[] { "The old dog knows these stones. His tail goes up, for the first time in days.", "A red door. The old dog sits. Waits. Like it will open." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 7, schedule_event = "door_stays_shut" },
                    new StoryRule { on = "door_stays_shut", lines = new string[] { "It doesn't open." }, voice = StoryVoice.dog, schedule_seconds = 3, schedule_event = "kofi_came" },
                    // Kofi (a film).
                    new StoryRule { on = "kofi_came", lines = new string[] { "A different door. A boy. Chestnuts, hot, in paper. Hands that smell of smoke.", "He crouches. He doesn't grab. He holds out the paper." }, voice = StoryVoice.dog, schedule_seconds = 7, schedule_event = "kofi_2", cutscene = CutsceneChange.start, cutscene_skip_to = "kofi_done" },
                    new StoryRule { on = "kofi_2", lines = new string[] { "Hot. Sweet. Half for the old dog.", "The boy runs in, shouting. The old dog still looks at the red door." }, voice = StoryVoice.dog, care_hunger = 0.6, schedule_seconds = 7, schedule_event = "kofi_done" },
                    new StoryRule { on = "kofi_done", lines = new string[] { "Nobody is coming to the red door. Come, old dog." }, voice = StoryVoice.dog, objective = "Lead Biscuit away. Find somewhere warm.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "kerb", lines = new string[] { "Wheels. Big. Loud. Coming. The old dog, stepping off the stones." }, voice = StoryVoice.dog, objective = "Push Biscuit back onto the kerb!", fear_change = 0.3 },
                    new StoryRule { on = "biscuit_shoved", lines = new string[] { "Bark. Shove. Shoulder into him. Back. BACK.", "The wheels, a nose away. Hot wind. Gone." }, voice = StoryVoice.dog, objective = "Lead Biscuit away. Find somewhere warm.", fear_change = -0.2 },
                    new StoryRule { on = "market", lines = new string[] { "Under the huts: dark, and dropped things. Boots go past like weather." }, voice = StoryVoice.dog, objective = "Find something dropped. Stay out of sight." },
                    new StoryRule { on = "pretzel_found", lines = new string[] { "Dropped. Salt and bread, still warm. Dropped things are allowed." }, voice = StoryVoice.dog, objective = "", care_hunger = 0.4, schedule_seconds = 4, schedule_event = "em_bus" },
                    // Six o'clock (Em).
                    new StoryRule { on = "em_bus", lines = new string[] { "Strand had its Christmas market up. Every stall had a string of lights.", "Gingerbread. Chestnuts. Hot apple. The brass band's breath, hanging in the cold." }, objective = "Ask at the stalls.", control = Character.human, beat = ChapterBeat.human_section },
                    new StoryRule { on = "asked_at_stall", lines = new string[] { "'A ginger one and a grandpa one,' the boy said. 'On our step. The grandpa wanted in.'", "'Old dogs go back to where they were young,' his mum said. 'My gran did too.'", "I rang Ama. 'Kirk Wynd,' she said. 'The red door.' Then, very quietly: 'That was ours.'" }, objective = "Put up posters." },
                    new StoryRule { on = "poster_carousel", lines = new string[] { "One poster on the carousel, where every child in Strand would ride past it." } },
                    new StoryRule { on = "poster_stall", lines = new string[] { "One on the chestnut stall, where Kofi could see it." } },
                    new StoryRule { when_all = new string[] { "poster_carousel", "poster_stall" }, objective = "Climb the town-hall steps. Look for him." },
                    new StoryRule { on = "steps_climbed", lines = new string[] { "From the town-hall steps I could see the whole square, and none of it was him." }, objective = "", schedule_seconds = 5, schedule_event = "countdown" },
                    // The tree and the whistle (films).
                    new StoryRule { on = "countdown", lines = new string[] { "At six the whole square counted down from ten, and the tree came on." }, schedule_seconds = 6, schedule_event = "whistle_time", cutscene = CutsceneChange.start, cutscene_skip_to = "whistle_time" },
                    new StoryRule { on = "whistle_time", objective = "Whistle for {dog}.", cutscene = CutsceneChange.end },
                    new StoryRule { when_all = new string[] { "whistle_time", "human_called_during_human_section" }, lines = new string[] { "I whistled across the square at the very moment the band began to play." }, objective = "", schedule_seconds = 4, schedule_event = "under_the_trumpets", cutscene = CutsceneChange.start, cutscene_skip_to = "near_miss_done" },
                    new StoryRule { on = "under_the_trumpets", lines = new string[] { "A sliver of something. Her? Under the trumpets. Gone.", "The big animal in the straw shouts. Ears flat. Gone, gone." }, voice = StoryVoice.dog, schedule_seconds = 7, schedule_event = "one_row" },
                    new StoryRule { on = "one_row", lines = new string[] { "One row of huts. I found that out later. One row of huts." }, schedule_seconds = 5, schedule_event = "near_miss_done" },
                    // Night (Bodhi).
                    new StoryRule { on = "near_miss_done", lines = new string[] { "The old dog, shaking again. Cold in his bones, not his fur." }, voice = StoryVoice.dog, objective = "Biscuit is fading. Find somewhere warm.", control = Character.dog, beat = ChapterBeat.dog_section, cutscene = CutsceneChange.end },
                    new StoryRule { on = "crate_pushed", lines = new string[] { "A box. Push. Push. Up against the little house." }, voice = StoryVoice.dog, objective = "Climb in the back window." },
                    new StoryRule { on = "into_the_grotto", lines = new string[] { "Inside: a big chair. Beard and peppermints. Snow that isn't cold.", "The old dog lies down in it and sighs." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 7, schedule_event = "grotto_sleep", cutscene = CutsceneChange.start, cutscene_skip_to = "em_last_poster" },
                    new StoryRule { on = "grotto_sleep", lines = new string[] { "A big chair. Beard and peppermints. The old dog asleep in cotton snow." }, voice = StoryVoice.dog, schedule_seconds = 5, schedule_event = "em_last_poster" },
                    // Em's last poster.
                    new StoryRule { on = "em_last_poster", objective = "Put up your last poster.", control = Character.human, beat = ChapterBeat.human_section, cutscene = CutsceneChange.end },
                    new StoryRule { on = "last_poster", lines = new string[] { "Across the square I put up my last poster. It faced the grotto." }, objective = "", schedule_seconds = 7, schedule_event = "chapter_done", cutscene = CutsceneChange.start, cutscene_skip_to = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "Chapter Five: The Lantern Train.", "The seventeenth of December." },
            };
        }
    }
}
