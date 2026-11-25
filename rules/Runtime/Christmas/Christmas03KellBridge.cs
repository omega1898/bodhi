// Purpose: Chapter Three of the Christmas story, "Kell Bridge" (15 December), as data for
// ChapterRun. The dogs' morning first: off the lorry, Nell's sausage ends, hiding from the plough
// crew, the smell of home up the lake, and Bodhi following Biscuit the other way. Then Em, an hour
// later, reading every trace the player has just left. Every word comes from
// docs/christmas_script.md, Chapter Three; when the script changes, change it here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: the road down to
// Strand (0 to 800), Nell's cafe and its woodshed (800 to 2000), the snowplough depot (2000 to
// 3200), the bridge where the lorry stopped (3200 to 4200) and the frozen lake shore (4200 to
// 5000). The floor is at y = 520 everywhere.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter Three: Kell Bridge.
        public static ChapterScript kell_bridge()
        {
            return new ChapterScript
            {
                id = "kell_bridge",
                title = "Kell Bridge",
                eyebrow = "Chapter Three: 15 December",
                place = "Kell Bridge, halfway down the valley",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0.4,
                start_closeness = 0.6,
                start_hunger = 0.6,
                things = new ChapterThing[]
                {
                    // The morning (Bodhi).
                    new ChapterThing { event_name = "off_the_lorry", who = Character.dog, label = "Climb down", requires = new string[] { "woke" }, x = 3500, y = 520 },
                    new ChapterThing { event_name = "sausage_found", who = Character.dog, label = "Eat", requires = new string[] { "nell_bowl" }, hidden = true, x = 1750, y = 520 },
                    new ChapterThing { event_name = "keepsake_dog_bed", who = Character.dog, label = "Sniff under the bench", requires = new string[] { "off_the_lorry" }, hidden = true, keepsake = "depot_dog_bed", memory_text = "Under the depot bench, a dog bed with a blanket folded in it. Its dog was out on the plough.", x = 2500, y = 520 },
                    // An hour later (Em).
                    new ChapterThing { event_name = "prints_read", who = Character.human, label = "Look at the prints", requires = new string[] { "em_arrived" }, x = 3500, y = 520 },
                    new ChapterThing { event_name = "bowl_read", who = Character.human, label = "Look at the bowl", requires = new string[] { "em_arrived" }, x = 1750, y = 520 },
                    new ChapterThing { event_name = "curl_found", who = Character.human, label = "Look at the woodshed door", requires = new string[] { "em_arrived" }, x = 1850, y = 520 },
                    new ChapterThing { event_name = "keepsake_noticeboard", who = Character.human, label = "Read the noticeboard", requires = new string[] { "em_arrived" }, keepsake = "nells_noticeboard", memory_text = "Nell's noticeboard: 'FOUND: one cat, very rude. Ask inside.' The cat was asleep on the counter.", x = 1050, y = 520 },
                    new ChapterThing { event_name = "keepsake_class_card", who = Character.human, label = "Read the card on the counter", requires = new string[] { "em_arrived" }, keepsake = "class_three_card", memory_text = "A card on Nell's counter: 'To the plough crew, for keeping the road open. Love, Class 3.'", x = 1300, y = 520 },
                    new ChapterThing { event_name = "talked_to_nell", who = Character.human, label = "Talk to Nell", requires = new string[] { "prints_read", "bowl_read", "curl_found" }, x = 1200, y = 520 },
                    new ChapterThing { event_name = "poster_put_up", who = Character.human, label = "Put up the poster", requires = new string[] { "nell_done" }, x = 1150, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "hid_in_woodshed", who = ZoneWho.dog, requires = new string[] { "crew_out" }, x = 1800, y = 300, width = 100, height = 240 },
                    new ChapterZone { event_name = "home_smelled", who = ZoneWho.dog, requires = new string[] { "crew_gone" }, x = 4500, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "followed_biscuit", who = ZoneWho.dog, requires = new string[] { "biscuit_went_down" }, x = 200, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "end_of_prints", who = ZoneWho.human, requires = new string[] { "poster_up" }, x = 200, y = 300, width = 200, height = 240 },
                },
                rules = new StoryRule[]
                {
                    // The morning (Bodhi).
                    new StoryRule { on = "level_started", control = Character.dog, beat = ChapterBeat.dog_section, follow = false, fetch = false, schedule_seconds = 3, schedule_event = "wake_1", cutscene = CutsceneChange.start, cutscene_skip_to = "woke" },
                    new StoryRule { on = "wake_1", lines = new string[] { "The floor stops. Bacon. Lake. Men's voices. Down. Now.", "The old dog, asleep. Nose in his ear. Up. Down. Now." }, voice = StoryVoice.dog, schedule_seconds = 6, schedule_event = "woke" },
                    new StoryRule { on = "woke", objective = "Get Biscuit down off the lorry.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "off_the_lorry", lines = new string[] { "Down the logs. Snow. Cold. Bacon, so close it hurts." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 4, schedule_event = "nell_bowl" },
                    new StoryRule { on = "nell_bowl", lines = new string[] { "A door. A woman, all wool and frying. A bowl, set down. She steps back.", "Wait. Wait. She goes in." }, voice = StoryVoice.dog, objective = "Find what she left. Sniff for it." },
                    new StoryRule { on = "sausage_found", lines = new string[] { "Sausage ends. Something black and crumbly that tastes of iron. Water.", "The old dog first, this time." }, voice = StoryVoice.dog, objective = "", care_hunger = 0.2, schedule_seconds = 4, schedule_event = "crew_out" },
                    new StoryRule { on = "crew_out", lines = new string[] { "Boots. Big boots. Engines waking. Hide." }, voice = StoryVoice.dog, objective = "Hide in the woodshed.", fear_change = 0.2 },
                    new StoryRule { on = "hid_in_woodshed", lines = new string[] { "Under the wood. Dark. Pine and dust, and mice, long ago." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 6, schedule_event = "crew_gone" },
                    new StoryRule { on = "crew_gone", lines = new string[] { "Engines, going. Boots, gone. Quiet." }, voice = StoryVoice.dog, objective = "Find which way is home.", fear_change = -0.2 },
                    new StoryRule { on = "home_smelled", lines = new string[] { "On the wind, up the lake: pine smoke, the lane, her. Home is up." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 4, schedule_event = "biscuit_went_down" },
                    new StoryRule { on = "biscuit_went_down", lines = new string[] { "Behind: the old dog, going down. Down the road. Doesn't look back." }, voice = StoryVoice.dog, objective = "Follow Biscuit." },
                    new StoryRule { on = "followed_biscuit", lines = new string[] { "Up is home. Down is the old dog.", "Down, then." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 5, schedule_event = "em_arrived", cutscene = CutsceneChange.start, cutscene_skip_to = "em_arrived" },
                    // An hour later (Em).
                    new StoryRule { on = "em_arrived", lines = new string[] { "An hour later, Ama's car came round the last bend above Kell Bridge.", "On a tree net in the yard that morning: three ginger curls. The order book said Strand." }, control = Character.human, beat = ChapterBeat.human_section, schedule_seconds = 7, schedule_event = "the_drive", cutscene = CutsceneChange.end },
                    new StoryRule { on = "the_drive", lines = new string[] { "I'd given Mina my spare key. 'Keep the lamp lit. In case he finds his own way.'", "'I'd do it again,' Ama said, halfway down. 'I know,' I said. That was all of it." }, objective = "Look for signs of them." },
                    new StoryRule { on = "prints_read", lines = new string[] { "Two sets of prints by the road. One big and slow. One small, and bouncing." } },
                    new StoryRule { on = "bowl_read", lines = new string[] { "By the woodshed, a bowl licked so clean it shone." } },
                    new StoryRule { on = "curl_found", lines = new string[] { "On a nail in the woodshed door: one ginger curl.", "I'd know it anywhere." } },
                    new StoryRule { when_all = new string[] { "prints_read", "bowl_read", "curl_found" }, objective = "Ask in the café." },
                    // Nell (a film).
                    new StoryRule { on = "talked_to_nell", lines = new string[] { "'Two of them,' said Nell. 'A ginger one, no collar, and an old soldier.'", "She'd left them sausage ends and the heel of a black pudding. Both gone." }, objective = "", schedule_seconds = 8, schedule_event = "nell_2", cutscene = CutsceneChange.start, cutscene_skip_to = "nell_done" },
                    new StoryRule { on = "nell_2", lines = new string[] { "'Wouldn't come to me,' she said. 'The young one stood between me and the old one.'", "His name tag was still in the post. That was why nobody had rung." }, schedule_seconds = 8, schedule_event = "nell_done" },
                    new StoryRule { on = "nell_done", objective = "Put up a poster in Nell's window.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "poster_put_up", lines = new string[] { "'LOST: {dog}, 9 months, ginger curls, very friendly. With an old grey dog, Biscuit.'" }, voice = StoryVoice.note, objective = "", schedule_seconds = 4, schedule_event = "poster_up" },
                    new StoryRule { on = "poster_up", lines = new string[] { "Nell taped it in her window, next to the cat." }, objective = "Follow the prints." },
                    // The end of the prints (a film).
                    new StoryRule { on = "end_of_prints", lines = new string[] { "Ama looked down the road a long time. 'We lived in Strand,' she said,", "'when Biscuit was young. He's not lost, Em. He's going home.'" }, objective = "", schedule_seconds = 9, schedule_event = "closing", cutscene = CutsceneChange.start, cutscene_skip_to = "chapter_done" },
                    new StoryRule { on = "closing", lines = new string[] { "Two sets of prints going down the road, away from home.", "Every few steps, the small ones turned to face the valley. Then went on." }, schedule_seconds = 9, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "Chapter Four: Strand.", "The sixteenth of December." },
            };
        }
    }
}
