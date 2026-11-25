// Purpose: Chapter Twelve of the Christmas story, "Christmas Day" (25 December), as data for
// ChapterRun. Home, and every setup paid off: the cold nose at six, the stocking with the new
// ball, the stolen bauble and the name tag, Christmas dinner, Mina, Ama and Biscuit for lunch,
// Dougie's sandwich for the stowaways, Tam on the phone, the sprout, and at night the snowman of
// the title screen, the whistle he answers, and the lamp lit for nobody with the old saying said
// whole. The epilogue, back on Tam's hill, is the screen after the chapter. Every word comes from
// docs/christmas_script.md, Chapter Twelve and the epilogue; when the script changes, change it
// here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: the cabin, with its
// bed, stove and stocking, the lopsided tree, the table and the window lamp (0 to 1500), and the
// clearing where the snowman goes (1500 to 2600). The floor is at y = 520 everywhere.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter Twelve: Christmas Day.
        public static ChapterScript christmas_day()
        {
            return new ChapterScript
            {
                id = "christmas_day",
                title = "Christmas Day",
                eyebrow = "Chapter Twelve: 25 December",
                place = "The cabin above Hollin",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0,
                start_closeness = 0.6,
                start_hunger = 0.5,
                start_dirt = 0.3,
                has_ball = true,
                things = new ChapterThing[]
                {
                    // Morning.
                    new ChapterThing { event_name = "stocking_opened", who = Character.human, label = "Open his stocking", requires = new string[] { "christmas_morning" }, x = 500, y = 520 },
                    new ChapterThing { event_name = "tag_on", who = Character.human, label = "Put his name tag on", requires = new string[] { "stocking_opened" }, needs_dog = true, x = 520, y = 520 },
                    new ChapterThing { event_name = "towelled", who = Character.human, label = "Towel {dog} dry", requires = new string[] { "tag_on" }, needs_dog = true, care = CareKind.bath, x = 300, y = 520 },
                    new ChapterThing { event_name = "christmas_dinner", who = Character.human, label = "Give him his Christmas dinner", requires = new string[] { "tag_on" }, needs_dog = true, care = CareKind.meal, x = 620, y = 520 },
                    new ChapterThing { event_name = "keepsake_bothy_stick", who = Character.dog, label = "Sniff under the tree", requires = new string[] { "christmas_morning" }, hidden = true, keepsake = "wrapped_stick", memory_text = "The bothy stick, under the tree, wrapped by {dog} in his own way: in my best scarf.", x = 820, y = 520 },
                    // Lunch.
                    new ChapterThing { event_name = "presents_opened", who = Character.human, label = "Open the presents", requires = new string[] { "lunch" }, x = 800, y = 520 },
                    new ChapterThing { event_name = "keepsake_minas_drawing", who = Character.human, label = "Look at Mina's drawing", requires = new string[] { "lunch" }, keepsake = "minas_drawing", memory_text = "Mina's drawing: two dogs and a lamp in a window. 'BODHI AND BISCUIT. HOME.' x x", x = 860, y = 520 },
                    new ChapterThing { event_name = "keepsake_tams_card", who = Character.human, label = "Read Tam's card", requires = new string[] { "tam_rang" }, keepsake = "tams_card", memory_text = "A card from Tam, with a reindeer hair taped inside: 'From Clementine.'", x = 1000, y = 520 },
                    new ChapterThing { event_name = "sprout_stolen", who = Character.dog, label = "Investigate the table", requires = new string[] { "sprout_time" }, x = 1100, y = 520 },
                    // Night.
                    new ChapterThing { event_name = "snowman_base", who = Character.human, label = "Roll the bottom ball", requires = new string[] { "sprout_stolen" }, x = 2000, y = 520 },
                    new ChapterThing { event_name = "arms_fetched", who = Character.dog, label = "Fetch a stick", requires = new string[] { "snowman_base" }, x = 2300, y = 520 },
                    new ChapterThing { event_name = "last_lamp", who = Character.human, label = "Light the lamp in the window", requires = new string[] { "came_running" }, x = 1400, y = 520 },
                },
                rules = new StoryRule[]
                {
                    // Morning.
                    new StoryRule { on = "level_started", follow = true, switching = false, fetch = false, schedule_seconds = 3, schedule_event = "wake_1", cutscene = CutsceneChange.start, cutscene_skip_to = "christmas_morning" },
                    new StoryRule { on = "wake_1", lines = new string[] { "Morning. Her feet. Her bed. Snow at the window. Out?" }, voice = StoryVoice.dog, schedule_seconds = 4.5, schedule_event = "wake_2" },
                    new StoryRule { on = "wake_2", lines = new string[] { "Christmas morning, and a cold nose in my ear, exactly on time." }, schedule_seconds = 4.5, schedule_event = "christmas_morning" },
                    new StoryRule { on = "christmas_morning", objective = "Open his stocking.", switching = true, cutscene = CutsceneChange.end },
                    new StoryRule { on = "stocking_opened", lines = new string[] { "In his stocking: a new ball, and the red bauble I'd found under the sofa.", "And his name tag. It had come in the post on Christmas Eve, while we were on the hill." }, objective = "Put his name tag on.", fetch = true },
                    new StoryRule { on = "tag_on", lines = new string[] { "I buckled it on beside the bell. The right notch." }, objective = "Look after him: towel, Christmas dinner, a cuddle." },
                    new StoryRule { on = "tag_on", lines = new string[] { "'BODHI. If found, call Em.'" }, voice = StoryVoice.note },
                    new StoryRule { on = "ball_fetched", lines = new string[] { "He brought the new ball back. Then he lay on it, in case anyone else wanted it." } },
                    new StoryRule { on = "towelled", lines = new string[] { "The last of the fell came out of his curls: one burr, from Windy Gap. I kept it." } },
                    new StoryRule { on = "christmas_dinner", lines = new string[] { "His own Christmas dinner: turkey, carrot, no gravy. He ate it in nine seconds." } },
                    new StoryRule { on = "dog_petted", lines = new string[] { "He leaned on me and sighed, like an old dog. Like Biscuit." } },
                    // Lunch (a film to start).
                    new StoryRule { when_all = new string[] { "tag_on", "towelled", "christmas_dinner", "dog_petted" }, lines = new string[] { "Mina, Ama and Biscuit came up the lane for lunch, Biscuit in his blue collar.", "Biscuit barked once at the door. Hello." }, objective = "", fetch = false, schedule_seconds = 8, schedule_event = "lunch", cutscene = CutsceneChange.start, cutscene_skip_to = "lunch" },
                    new StoryRule { on = "lunch", objective = "Open the presents.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "presents_opened", lines = new string[] { "Mina gave {dog} a drawing. He sniffed it very seriously, and approved." }, objective = "", schedule_seconds = 5, schedule_event = "dougie_came" },
                    new StoryRule { on = "dougie_came", lines = new string[] { "Dougie came up the lane with a box. Inside: a ham sandwich. 'For the stowaways.'", "He'd had them in his lorry for a whole day and never known." }, schedule_seconds = 9, schedule_event = "tam_rang" },
                    new StoryRule { on = "tam_rang", lines = new string[] { "Tam rang. Clementine was fine. Did the sled run true? It did." }, schedule_seconds = 5, schedule_event = "sprout_time" },
                    new StoryRule { on = "sprout_time", objective = "Something smells interesting on the table.", control = Character.dog },
                    new StoryRule { on = "sprout_stolen", lines = new string[] { "Green. Round. Smells of... no. No." }, voice = StoryVoice.dog },
                    new StoryRule { on = "sprout_stolen", lines = new string[] { "He stole a sprout off Mina's plate, tasted it, and gave it back." }, objective = "Go out and build a snowman.", control = Character.human, beat = ChapterBeat.aftermath },
                    // Night: the snowman, the whistle, the lamp.
                    new StoryRule { on = "snowman_base", lines = new string[] { "That night, under the aurora, we built a snowman in the clearing." }, objective = "Switch to {dog}. Fetch sticks for its arms." },
                    new StoryRule { on = "arms_fetched", lines = new string[] { "Sticks. Good sticks. Not for chewing. For the cold white one." }, voice = StoryVoice.dog },
                    new StoryRule { on = "arms_fetched", lines = new string[] { "He brought the arms. He did not eat the nose. He has grown up.", "Mina put the Santa hat on. 'Now he's finished,' she said." }, objective = "Whistle for {dog}.", control = Character.human },
                    // The whistle: a beat of its own (the aftermath), so only a whistle from here answers it.
                    new StoryRule { when_all = new string[] { "arms_fetched", "human_called_during_aftermath" }, lines = new string[] { "I whistled, just to hear it. Of course he came.", "I heard him before I saw him: the little brass bell, and the new tag beside it." }, objective = "Light the lamp in the window.", schedule_seconds = 0.5, schedule_event = "came_running" },
                    new StoryRule { on = "last_lamp", lines = new string[] { "Mina handed me the matches. 'A light for the hill,' I said.", "'And the hill brings them home,' we said, all of us." }, objective = "", schedule_seconds = 8, schedule_event = "last_lines", cutscene = CutsceneChange.start, cutscene_skip_to = "chapter_done" },
                    new StoryRule { on = "last_lines", lines = new string[] { "That night I put the lamp in the window anyway.", "Not for anyone on the hill. Everyone I'd been waiting for was home." }, schedule_seconds = 8, schedule_event = "here_is_the_place" },
                    new StoryRule { on = "here_is_the_place", lines = new string[] { "Her. The stove. The old dog, snoring. Here is the place." }, voice = StoryVoice.dog, schedule_seconds = 7, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                // The epilogue: back on Tam's hill, the bookend to the prologue.
                aftermath = new string[] { "Up on the moor, Tam's reindeer lay down in the snow, facing south.", "He left his lamp lit anyway. Habit, mostly." },
            };
        }
    }
}
