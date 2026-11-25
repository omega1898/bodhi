// Purpose: Chapter Nine of the Christmas story, "The Bothy" (21 to 22 December), as data for
// ChapterRun. The darkest, quietest chapter: two nights snowed into a stone shelter on the fell.
// Biscuit stops eating; Bodhi lies along his back all night; when the fire runs low Bodhi goes out
// into the storm to dig out the woodpile. Em reads the visitors' book and finds Tam's entry from
// 1974: this is the bothy where Moss kept him alive. At dawn Biscuit eats from her hand and barks
// once. Every word comes from docs/christmas_script.md, Chapter Nine; when the script changes,
// change it here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: the bothy's one
// room, with its hearth, sleeping platform and shelf (300 to 1260), its snow-jammed door (1260),
// and outside in the storm, the woodpile under the drift (1300 to 2400). The floor is at y = 520.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter Nine: The Bothy.
        public static ChapterScript bothy()
        {
            return new ChapterScript
            {
                id = "bothy",
                title = "The Bothy",
                eyebrow = "Chapter Nine: 21 December",
                place = "The bothy on the open fell",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0.3,
                start_closeness = 0.8,
                start_hunger = 0.5,
                start_dirt = 0.4,
                things = new ChapterThing[]
                {
                    // The first night.
                    new ChapterThing { event_name = "fire_lit", who = Character.human, label = "Light the fire", requires = new string[] { "inside" }, x = 500, y = 520 },
                    new ChapterThing { event_name = "biscuit_wont_eat", who = Character.human, label = "Offer Biscuit some food", requires = new string[] { "fire_lit" }, x = 700, y = 520 },
                    new ChapterThing { event_name = "lay_along_biscuit", who = Character.dog, label = "Lie along his back", requires = new string[] { "biscuit_wont_eat" }, x = 720, y = 520 },
                    new ChapterThing { event_name = "door_shouldered", who = Character.human, label = "Shoulder the door open", requires = new string[] { "fire_low" }, x = 1220, y = 520 },
                    new ChapterThing { event_name = "logs_dug", who = Character.dog, label = "Dig out the logs", requires = new string[] { "door_shouldered" }, hidden = true, x = 2000, y = 520 },
                    new ChapterThing { event_name = "logs_carried", who = Character.human, label = "Carry the logs in", requires = new string[] { "logs_dug" }, x = 1200, y = 520 },
                    new ChapterThing { event_name = "towelled", who = Character.human, label = "Towel {dog} dry", requires = new string[] { "logs_carried" }, needs_dog = true, care = CareKind.bath, x = 600, y = 520 },
                    // The book.
                    new ChapterThing { event_name = "book_read", who = Character.human, label = "Read the visitors' book", requires = new string[] { "towelled" }, x = 1000, y = 520 },
                    new ChapterThing { event_name = "keepsake_book_peaches", who = Character.human, label = "Read the 1987 page", requires = new string[] { "inside" }, keepsake = "book_peaches", memory_text = "In the visitors' book: 'Christmas 1987. Snowed in three days with my brother and a tin of peaches. Best ever.'", x = 970, y = 520 },
                    new ChapterThing { event_name = "keepsake_book_shortbread", who = Character.human, label = "Read the 1994 page", requires = new string[] { "inside" }, keepsake = "book_shortbread", memory_text = "In the visitors' book: '1994. Left the shortbread. You're welcome.' The tin was still on the shelf.", x = 1040, y = 520 },
                    new ChapterThing { event_name = "keepsake_moss_doorframe", who = Character.dog, label = "Sniff the doorframe", requires = new string[] { "inside" }, hidden = true, keepsake = "moss_doorframe", memory_text = "Low on the doorframe, at a dog's height, someone had cut a name: MOSS.", x = 1240, y = 520 },
                    // The second night.
                    new ChapterThing { event_name = "stick_brought", who = Character.dog, label = "Pick up the stick", requires = new string[] { "second_night" }, x = 1900, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "out_in_the_storm", who = ZoneWho.dog, requires = new string[] { "door_shouldered" }, x = 1450, y = 300, width = 150, height = 240 },
                },
                rules = new StoryRule[]
                {
                    // The first night.
                    new StoryRule { on = "level_started", follow = true, switching = false, fetch = false, schedule_seconds = 3, schedule_event = "open_1", cutscene = CutsceneChange.start, cutscene_skip_to = "inside" },
                    new StoryRule { on = "open_1", lines = new string[] { "The storm caught us at the bothy door, and held us there two days." }, schedule_seconds = 4.5, schedule_event = "open_2" },
                    new StoryRule { on = "open_2", lines = new string[] { "Stone. Smoke. Old boots. A hundred strangers, all of them kind." }, voice = StoryVoice.dog, schedule_seconds = 4.5, schedule_event = "inside" },
                    new StoryRule { on = "inside", objective = "Light the fire.", switching = true, cutscene = CutsceneChange.end },
                    new StoryRule { on = "fire_lit", lines = new string[] { "There was kindling in the grate, laid by someone for whoever came next." }, objective = "Offer Biscuit some food." },
                    new StoryRule { on = "biscuit_wont_eat", lines = new string[] { "On the first night Biscuit stopped eating.", "I stopped sleeping." }, objective = "Switch to {dog}. Keep Biscuit warm.", fear_change = 0.3 },
                    // Lying close (a film).
                    new StoryRule { on = "lay_along_biscuit", lines = new string[] { "The old dog, too still. Lie close. Closer. Like under the fallen tree.", "His heart, slow. Slower than before." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 9, schedule_event = "fire_low", cutscene = CutsceneChange.start, cutscene_skip_to = "fire_low" },
                    new StoryRule { on = "fire_low", lines = new string[] { "Some time after midnight the fire burned down to red.", "The woodpile was outside, under the drift." }, objective = "Open the door. It's jammed with snow.", control = Character.human, cutscene = CutsceneChange.end },
                    new StoryRule { on = "door_shouldered", lines = new string[] { "The door was jammed with snow. I put my shoulder to it, and the storm came in." }, objective = "Switch to {dog}. Find the logs under the drift.", remove = new string[] { "bothy_door" } },
                    new StoryRule { on = "out_in_the_storm", lines = new string[] { "Outside: the howl. Dark. Snow in the ears, the eyes, the mouth." }, voice = StoryVoice.dog, fear_change = 0.2 },
                    new StoryRule { on = "logs_dug", lines = new string[] { "Wood under the snow. Dig. Dig. Back to her." }, voice = StoryVoice.dog, objective = "Carry the logs in.", control = Character.human },
                    new StoryRule { on = "logs_carried", lines = new string[] { "He'd dug out half the woodpile. I carried it in, armful by armful.", "The fire caught. Biscuit didn't lift his head." }, objective = "Towel {dog} dry." },
                    new StoryRule { on = "towelled", lines = new string[] { "He came in white and came out of the towel steaming." }, objective = "Read the visitors' book." },
                    // The book (a film).
                    new StoryRule { on = "book_read", lines = new string[] { "To pass the hours I read the visitors' book aloud. Forty years of strangers.", "Most of them had been stuck in this room at Christmas, like us." }, objective = "", schedule_seconds = 8, schedule_event = "book_1974", cutscene = CutsceneChange.start, cutscene_skip_to = "second_night" },
                    new StoryRule { on = "book_1974", lines = new string[] { "'Christmas 1974. Snowed in three nights. Moss kept me warm. T. R., aged 12.'" }, voice = StoryVoice.note, schedule_seconds = 6, schedule_event = "tams_bothy" },
                    new StoryRule { on = "tams_bothy", lines = new string[] { "T. R. Tam Rennie. This was Tam's bothy. This was where Moss had kept him alive." }, schedule_seconds = 6, schedule_event = "second_night" },
                    // The second night, and morning.
                    new StoryRule { on = "second_night", lines = new string[] { "The second night was the longest. I kept the fire. He kept Biscuit." }, objective = "Switch to {dog}. Bring her something.", fear_change = -0.1, cutscene = CutsceneChange.end },
                    new StoryRule { on = "stick_brought", lines = new string[] { "A stick. A good stick. For her." }, voice = StoryVoice.dog, objective = "", control = Character.human, schedule_seconds = 3, schedule_event = "present" },
                    new StoryRule { on = "present", lines = new string[] { "Near dawn he brought me a present: a stick, from the woodpile.", "I said thank you. I meant it." }, schedule_seconds = 6, schedule_event = "morning", cutscene = CutsceneChange.start, cutscene_skip_to = "closing" },
                    new StoryRule { on = "morning", lines = new string[] { "In the morning Biscuit lifted his head, ate from my hand, and barked once.", "Hello." }, fear_change = -0.4, schedule_seconds = 7, schedule_event = "morning_2" },
                    new StoryRule { on = "morning_2", lines = new string[] { "The old dog. Warm. Hungry. Here." }, voice = StoryVoice.dog, schedule_seconds = 4, schedule_event = "closing" },
                    new StoryRule { on = "closing", lines = new string[] { "I wrote our names in the book, under forty years of strangers.", "Then we went to climb the pass." }, schedule_seconds = 8, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "Chapter Ten: Windy Gap.", "The twenty-third of December." },
            };
        }
    }
}
