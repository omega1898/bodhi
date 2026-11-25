// Purpose: Chapter One of the Christmas story, "First Snow" (14 December, the free chapter), as
// data for ChapterRun. One perfect day in the snow, then the blizzard, and a whistle nobody
// answers. Every word comes from docs/christmas_script.md, section 3; when the script changes,
// change it here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: the pine woods
// behind the cabin (0 to 1500), the cabin (1500 to 3000), the clearing where the snowman goes
// (3000 to 4200), the lane down through the pines (4200 to 6000) and the timber yard (6000 to
// 7600). The floor is at y = 520 everywhere.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter One: First Snow.
        public static ChapterScript first_snow()
        {
            return new ChapterScript
            {
                id = "first_snow",
                title = "First Snow",
                eyebrow = "Chapter One: 14 December",
                place = "The cabin above Hollin",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0.05,
                start_closeness = 0.35,
                start_hunger = 0.4,
                start_dirt = 0,
                start_food = 0,
                things = new ChapterThing[]
                {
                    // Morning, in the clearing.
                    new ChapterThing { event_name = "front_door_opened", who = Character.human, label = "Open the door", requires = new string[] { "woke_up" }, needs_dog = true, x = 2950, y = 520 },
                    new ChapterThing { event_name = "snowball_thrown", who = Character.human, label = "Throw a snowball for {dog}", requires = new string[] { "first_bites" }, needs_dog = true, x = 3350, y = 520 },
                    new ChapterThing { event_name = "snowball_dug_out", who = Character.dog, label = "Dig it out", requires = new string[] { "snowball_thrown" }, hidden = true, x = 3750, y = 520 },
                    new ChapterThing { event_name = "snowman_base_rolled", who = Character.human, label = "Roll the bottom ball", requires = new string[] { "snowball_returned" }, x = 3550, y = 520 },
                    new ChapterThing { event_name = "stick_fetched", who = Character.dog, label = "Fetch a stick", requires = new string[] { "snowman_base_rolled" }, x = 4100, y = 520 },
                    new ChapterThing { event_name = "keepsake_minas_card", who = Character.human, label = "Read Mina's card", requires = new string[] { "mina_done" }, keepsake = "minas_card", memory_text = "'To {dog}. Happy first Christmas! Love, Biscuit (he can't write) and Mina (10).'", x = 3450, y = 520 },
                    // Afternoon, at the timber yard.
                    new ChapterThing { event_name = "sandwich_sniffed", who = Character.dog, label = "Sniff under the cab", requires = new string[] { "reached_timber_yard" }, hidden = true, x = 6450, y = 520 },
                    new ChapterThing { event_name = "tree_chosen", who = Character.dog, label = "Choose this tree", requires = new string[] { "reached_timber_yard" }, x = 7300, y = 520 },
                    new ChapterThing { event_name = "tree_tied_on", who = Character.human, label = "Tie the tree onto the sled", requires = new string[] { "dougie_agreed" }, needs_dog = true, x = 7250, y = 520 },
                    // Evening, in the cabin.
                    new ChapterThing { event_name = "towelled_dry", who = Character.human, label = "Towel {dog} dry", requires = new string[] { "home_with_tree" }, needs_dog = true, care = CareKind.bath, x = 1900, y = 520 },
                    new ChapterThing { event_name = "dinner_given", who = Character.human, label = "Fill his bowl", requires = new string[] { "towelled_dry" }, needs_dog = true, care = CareKind.meal, x = 2150, y = 520 },
                    new ChapterThing { event_name = "tree_decorated", who = Character.human, label = "Hang the baubles", requires = new string[] { "dinner_given" }, x = 2450, y = 520 },
                    new ChapterThing { event_name = "keepsake_stolen_bauble", who = Character.dog, label = "Dig out the bauble", requires = new string[] { "bauble_missing" }, hidden = true, keepsake = "stolen_bauble", memory_text = "The oldest bauble in the cabin, red glass. He had hidden it with great care.", x = 2600, y = 520 },
                    new ChapterThing { event_name = "stocking_hung", who = Character.human, label = "Hang up his stocking", requires = new string[] { "keepsake_stolen_bauble" }, x = 2000, y = 520 },
                    new ChapterThing { event_name = "keepsake_puppy_book", who = Character.human, label = "Read the puppy book", requires = new string[] { "home_with_tree" }, keepsake = "puppy_book", memory_text = "My puppy book, folded at 'First winter': 'Most puppies love snow. Some eat it.' I'd underlined the next line twice, and then forgotten it.", x = 2300, y = 520 },
                    new ChapterThing { event_name = "lamp_lit", who = Character.human, label = "Light the lamp in the window", requires = new string[] { "stocking_hung" }, x = 2750, y = 520 },
                    new ChapterThing { event_name = "went_to_bed", who = Character.human, label = "Go to bed", requires = new string[] { "lamp_lit" }, x = 1650, y = 520 },
                    // Night.
                    new ChapterThing { event_name = "night_door_opened", who = Character.human, label = "Open the door", requires = new string[] { "hammering" }, x = 2950, y = 520 },
                    new ChapterThing { event_name = "lantern_taken", who = Character.human, label = "Take the lantern and his lead", requires = new string[] { "set_out_ready" }, needs_dog = true, x = 1560, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "reached_timber_yard", who = ZoneWho.human, requires = new string[] { "mina_done" }, x = 6000, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "home_with_tree", who = ZoneWho.human, requires = new string[] { "tree_tied_on" }, x = 3000, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "into_the_woods", who = ZoneWho.human, requires = new string[] { "lantern_taken" }, x = 1200, y = 300, width = 200, height = 240 },
                    new ChapterZone { event_name = "scent_caught", who = ZoneWho.human, requires = new string[] { "into_the_woods" }, x = 800, y = 300, width = 200, height = 240 },
                    // The lunge waits until his last scent line has shown, so the lines stay in order.
                    new ChapterZone { event_name = "the_lunge", who = ZoneWho.human, requires = new string[] { "under_it_all" }, x = 350, y = 300, width = 200, height = 240 },
                },
                rules = new StoryRule[]
                {
                    // Waking (a film).
                    new StoryRule { on = "level_started", follow = false, fetch = false, schedule_seconds = 3.5, schedule_event = "wake_1", cutscene = CutsceneChange.start, cutscene_skip_to = "woke_up" },
                    new StoryRule { on = "wake_1", lines = new string[] { "White. Out there. Everything white.", "It smells of nothing. How does it smell of nothing?" }, voice = StoryVoice.dog, schedule_seconds = 5, schedule_event = "wake_2" },
                    new StoryRule { on = "wake_2", lines = new string[] { "Six in the morning: a cold nose in my ear, and a whine at the window.", "Snow had come in the night, the way it does in Hollin: all at once, and meaning it." }, schedule_seconds = 6, schedule_event = "woke_up" },
                    new StoryRule { on = "woke_up", objective = "Go out into the snow with {dog}.", follow = true, cutscene = CutsceneChange.end },
                    // The first snow.
                    new StoryRule { on = "front_door_opened", lines = new string[] { "I opened the door. He looked at the snow. The snow looked at him.", "Then he went in face first." }, schedule_seconds = 4, schedule_event = "first_steps" },
                    new StoryRule { on = "first_steps", lines = new string[] { "Cold! Cold on the paws! Again!", "Bite it. It goes. Bite more." }, voice = StoryVoice.dog, schedule_seconds = 3.5, schedule_event = "first_bites" },
                    new StoryRule { on = "first_bites", lines = new string[] { "He bit the snow. He bit all of it. He sneezed, and bit it again." }, objective = "Throw a snowball for {dog}." },
                    // The snowball that vanishes.
                    new StoryRule { on = "snowball_thrown", lines = new string[] { "The snowball landed, and stopped being a snowball." }, follow = false, schedule_seconds = 2.5, schedule_event = "snowball_gone" },
                    new StoryRule { on = "snowball_gone", lines = new string[] { "Gone. Into the white. Where? Where?" }, voice = StoryVoice.dog, objective = "Switch to {dog}. Sniff to find it.", switching = true },
                    new StoryRule { on = "snowball_dug_out", lines = new string[] { "Snow. Snow. Snow. Then: her mittens. Under here." }, voice = StoryVoice.dog, schedule_seconds = 3, schedule_event = "snowball_returned" },
                    new StoryRule { on = "snowball_returned", lines = new string[] { "He dug it out and brought me back a handful of water.", "He was very proud of the water." }, objective = "Whistle for {dog}.", control = Character.human },
                    // The whistle. Its lines come with her first whistle, whenever that is; the
                    // snowman waits until she has whistled and he has brought the snowball back.
                    new StoryRule { on = "human_called", lines = new string[] { "One whistle, and he came. He always came.", "I heard him before I saw him: the little brass bell on his collar.", "I'd bought the bell so I would always know where he was. That was the idea." } },
                    new StoryRule { when_all = new string[] { "snowball_returned", "human_called" }, objective = "Build a snowman: roll the bottom ball.", follow = true },
                    // The snowman.
                    new StoryRule { on = "snowman_base_rolled", lines = new string[] { "The bottom ball took me ten minutes. He helped by lying on it.", "Then the middle, and a head packed hard between my mittens." }, objective = "Find sticks for its arms." },
                    new StoryRule { on = "stick_fetched", lines = new string[] { "He brought a stick for the arms. Then a branch. Then most of a tree." }, objective = "", schedule_seconds = 3.5, schedule_event = "carrot_stolen" },
                    new StoryRule { on = "carrot_stolen", lines = new string[] { "Orange. Crunchy. Mine." }, voice = StoryVoice.dog, schedule_seconds = 2.5, schedule_event = "nose_gone" },
                    new StoryRule { on = "nose_gone", lines = new string[] { "The snowman had a nose for almost four seconds." }, schedule_seconds = 3, schedule_event = "mina_came" },
                    // Mina and Biscuit come up the lane (a film).
                    new StoryRule { on = "mina_came", lines = new string[] { "Mina came up the lane from the cottage below, Biscuit plodding behind her.", "Biscuit was thirteen, grey to the eyebrows, and deaf in one ear.", "He barked once, very politely. That is how Biscuit says hello." }, control = Character.human, switching = false, schedule_seconds = 8, schedule_event = "mina_2", cutscene = CutsceneChange.start, cutscene_skip_to = "mina_done" },
                    new StoryRule { on = "mina_2", lines = new string[] { "The old dog. Warm blanket. Toast. And sheep, a long time ago.", "And the girl, all over him. The best smell on the whole lane." }, voice = StoryVoice.dog, schedule_seconds = 6, schedule_event = "mina_3" },
                    new StoryRule { on = "mina_3", lines = new string[] { "Mina put a Santa hat on the snowman. 'Now he's finished,' she said." }, schedule_seconds = 4, schedule_event = "mina_4" },
                    new StoryRule { on = "mina_4", lines = new string[] { "Biscuit stood at the top of the lane and stared up at the hill. Listening.", "'He does that when weather's coming,' Mina said. 'Mum says he can't hear it.'" }, schedule_seconds = 6.5, schedule_event = "mina_5" },
                    new StoryRule { on = "mina_5", lines = new string[] { "Down in the village the church bell rang twelve. Hollin's bell is called Mercy.", "It was rung for weddings, for funerals, and once, long ago, for the hill." }, schedule_seconds = 6.5, schedule_event = "mina_done" },
                    new StoryRule { on = "mina_done", objective = "Take the sled down to the timber yard.", follow = true, cutscene = CutsceneChange.end },
                    // The timber yard.
                    new StoryRule { on = "reached_timber_yard", lines = new string[] { "At the foot of the lane, Dougie was loading his lorry with trees for Strand.", "'Last load goes down at first light,' he said. 'Pick quick.'" }, objective = "Let {dog} choose a tree.", switching = true },
                    new StoryRule { on = "sandwich_sniffed", lines = new string[] { "Pine. Oil. Under the seat, a sandwich. Ham." }, voice = StoryVoice.dog, schedule_seconds = 3, schedule_event = "dougie_caught_him" },
                    new StoryRule { on = "dougie_caught_him", lines = new string[] { "Dougie caught him with both paws on the cab step. 'That's my piece,' he said." } },
                    new StoryRule { on = "tree_chosen", lines = new string[] { "He chose it. He sat down beside it and would not move.", "It was the most lopsided tree in the yard. It leaned like it was listening." }, objective = "", control = Character.human, switching = false, schedule_seconds = 5, schedule_event = "dougie_agreed" },
                    new StoryRule { on = "dougie_agreed", lines = new string[] { "'Three Christmases that one's stood there,' Dougie said. 'Nobody wanted it.'", "{dog} wanted it." }, objective = "Tie the tree onto the sled." },
                    new StoryRule { on = "tree_tied_on", lines = new string[] { "I pulled the sled. He took the rope in his teeth and pulled the other way." }, objective = "Pull the tree home." },
                    new StoryRule { on = "home_with_tree", lines = new string[] { "We got home eventually.", "By half past three the sun was gone, and all the snow went blue.", "The radio said snow showers, heavier later. The radio doesn't live in Hollin." }, objective = "Towel him dry." },
                    // Evening: looking after him, the tree, the stocking, the lamp.
                    new StoryRule { on = "towelled_dry", lines = new string[] { "Snow in his ears, snow in his toes, snow in places snow shouldn't go.", "He came out of the towel twice his size and very pleased about it.", "His curls had doubled. I let his collar out a notch, so it wouldn't pinch." }, objective = "Give him his dinner." },
                    new StoryRule { on = "dinner_given", lines = new string[] { "Dinner, gone. Then he chased the empty bowl round the kitchen." }, objective = "Decorate the tree." },
                    new StoryRule { on = "tree_decorated", lines = new string[] { "Fairy lights, three tins of baubles, and the tinsel he hadn't eaten." }, objective = "", schedule_seconds = 4, schedule_event = "bauble_missing" },
                    new StoryRule { on = "bauble_missing", lines = new string[] { "One red bauble went missing. So did {dog}, briefly." }, objective = "Switch to {dog}. Find the bauble.", switching = true },
                    new StoryRule { on = "keepsake_stolen_bauble", objective = "Hang up his stocking.", control = Character.human },
                    new StoryRule { on = "stocking_hung", lines = new string[] { "His first stocking. I'd sewn his name on in felt. The D was backwards.", "He sniffed it, found it empty, and looked at me as if I'd been unfair." }, objective = "Light the lamp in the window." },
                    new StoryRule { on = "lamp_lit", lines = new string[] { "In Hollin, on winter nights, you leave a light in the window.", "For anyone still out on the hill. The custom is older than the church.", "'A light for the hill,' you say as you light it. Everyone does. Nobody knows why.", "Not for us, {dog}. We're in." }, objective = "Time for bed." },
                    // Bed, then midnight (a film).
                    new StoryRule { on = "went_to_bed", lines = new string[] { "He was not allowed on the bed. He got on the bed." }, objective = "", control = Character.human, switching = false, follow = false, schedule_seconds = 4, schedule_event = "bed_2", cutscene = CutsceneChange.start, cutscene_skip_to = "hammering" },
                    new StoryRule { on = "bed_2", lines = new string[] { "Her feet. Her breathing. The stove, ticking.", "Here. Here is the place." }, voice = StoryVoice.dog, schedule_seconds = 6, schedule_event = "midnight" },
                    new StoryRule { on = "midnight", lines = new string[] { "Wind. Big wind. Something out there is wrong." }, voice = StoryVoice.dog, fear_change = 0.3, schedule_seconds = 3.5, schedule_event = "midnight_2" },
                    new StoryRule { on = "midnight_2", lines = new string[] { "Just after midnight, the wind found the chimney and began to roar." }, schedule_seconds = 4, schedule_event = "hammering" },
                    new StoryRule { on = "hammering", lines = new string[] { "Then someone was hammering on the door." }, objective = "Open the door.", follow = true, cutscene = CutsceneChange.end },
                    // Mina and Ama at the door (a film).
                    new StoryRule { on = "night_door_opened", lines = new string[] { "Mina and Ama, white with snow, a torch shaking between them.", "'Biscuit got out with the logs,' Ama said. 'He can't hear us call.'", "'I shut the log store,' Mina said. 'I did. I'm sure I did.'" }, schedule_seconds = 9, schedule_event = "door_2", cutscene = CutsceneChange.start, cutscene_skip_to = "set_out_ready" },
                    new StoryRule { on = "door_2", lines = new string[] { "Then she stopped talking and just looked at {dog}.", "'He can smell him,' she said. 'Please.'" }, schedule_seconds = 6, schedule_event = "set_out_ready" },
                    new StoryRule { on = "set_out_ready", objective = "Take the lantern. Keep {dog} on his lead.", cutscene = CutsceneChange.end },
                    // The blizzard.
                    new StoryRule { on = "lantern_taken", lines = new string[] { "I clipped on his lead and took the lantern. I left the lamp in the window." }, objective = "Follow {dog}. Find Biscuit." },
                    new StoryRule { on = "into_the_woods", lines = new string[] { "Past the cabin, the world was white and roaring and had no edges.", "The lantern lit a small circle of snow, and nothing else." } },
                    new StoryRule { on = "scent_caught", lines = new string[] { "The old dog! Faint. Frightened. That way!" }, voice = StoryVoice.dog, fear_change = 0.2, schedule_seconds = 2.5, schedule_event = "lead_sang" },
                    new StoryRule { on = "lead_sang", lines = new string[] { "{dog} pulled so hard the lead sang." }, schedule_seconds = 3, schedule_event = "under_it_all" },
                    new StoryRule { on = "under_it_all", lines = new string[] { "Snow in the nose. Snow in the eyes. Under it all: the old dog. Close." }, voice = StoryVoice.dog },
                    // The lunge (a film): the lead comes back with only his collar on it.
                    new StoryRule { on = "the_lunge", lines = new string[] { "THE OLD DOG. Close. Go. GO." }, voice = StoryVoice.dog, beat = ChapterBeat.separation, follow = false, schedule_seconds = 2, schedule_event = "lead_went_light", cutscene = CutsceneChange.start, cutscene_skip_to = "lead_came_back" },
                    new StoryRule { on = "lead_went_light", lines = new string[] { "Then the lead went light in my hand." }, schedule_seconds = 3, schedule_event = "lead_came_back" },
                    new StoryRule { on = "lead_came_back", lines = new string[] { "I pulled it in. His collar came back on the end of it. Bell and all.", "Wet curls lie flat. I had let it out a notch. One notch." }, objective = "Whistle for {dog}.", beat = ChapterBeat.human_section, cutscene = CutsceneChange.end },
                    // The hook: the player whistles, and for the first time he doesn't come.
                    new StoryRule { on = "human_called_during_human_section", lines = new string[] { "I whistled.", "The only bell was the one in my hand.", "For the first time, he didn't come." }, objective = "", schedule_seconds = 8, schedule_event = "into_the_dark", cutscene = CutsceneChange.start, cutscene_skip_to = "chapter_done" },
                    new StoryRule { on = "into_the_dark", lines = new string[] { "Her whistle, torn to pieces by the wind.", "No collar. Cold neck. Doesn't matter.", "The old dog. Thin as a thread. That way.", "Keep going." }, voice = StoryVoice.dog, schedule_seconds = 11, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "Chapter Two: The Lamp in the Window.", "The same night. Two in the morning." },
            };
        }
    }
}
