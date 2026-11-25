// Purpose: Chapter Five of the Christmas story, "The Lantern Train" (17 December), as data for
// ChapterRun. The dogs walk up the railway from Strand, Bodhi leading. Em rides the Lantern Train
// up the valley beside Pat the guard. On the viaduct the rails begin to hum; Bodhi gets Biscuit
// into a recess in the wall as the train passes a hand's width away, and through its window he
// and Em see each other. The dogs bolt into the forest; Em finds their prints in the recess, and
// her whistle is answered by one bark. Every word comes from docs/christmas_script.md, Chapter
// Five; when the script changes, change it here too.
//
// The x numbers are places along the chapter's side-on layout, left to right: Strand station (0
// to 800), the line through the birches and pines (800 to 2600), the viaduct over Skelder Gorge
// (2600 to 4200) and the halt beyond it (4200 to 5000). The floor is at y = 520 everywhere.
namespace Bodhi.Rules
{
    public static partial class ChristmasChapters
    {
        // Chapter Five: The Lantern Train.
        public static ChapterScript lantern_train()
        {
            return new ChapterScript
            {
                id = "lantern_train",
                title = "The Lantern Train",
                eyebrow = "Chapter Five: 17 December",
                place = "The valley railway, and the viaduct at Skelder Gorge",
                dog_age = "nine_months",
                dog_size = 0.9,
                dog_jump = 1.0,
                start_fear = 0.3,
                start_closeness = 0.6,
                start_hunger = 0.6,
                things = new ChapterThing[]
                {
                    // Up the line (Bodhi).
                    new ChapterThing { event_name = "keepsake_rail_crumbs", who = Character.dog, label = "Sniff the sleeper", requires = new string[] { "on_the_line" }, hidden = true, keepsake = "rail_crumbs", memory_text = "Crumbs scattered along a sleeper for the birds. Someone walks this line with bread in their pocket.", x = 1400, y = 520 },
                    new ChapterThing { event_name = "round_the_tree", who = Character.dog, label = "Nudge him round", requires = new string[] { "fallen_tree" }, x = 1850, y = 520 },
                    // The Lantern Train (Em).
                    new ChapterThing { event_name = "keepsake_lantern_tag", who = Character.human, label = "Read the lantern's tag", requires = new string[] { "kofis_mum_rang" }, keepsake = "lantern_tag", memory_text = "A lantern on the train with a child's tag: 'This one's for Mum, at the hospital.'", x = 700, y = 520 },
                    new ChapterThing { event_name = "boarded", who = Character.human, label = "Board the train", requires = new string[] { "kofis_mum_rang" }, x = 560, y = 520 },
                    // The viaduct.
                    new ChapterThing { event_name = "biscuit_in_recess", who = Character.dog, label = "Push him into the recess", requires = new string[] { "rails_hum" }, x = 3400, y = 520 },
                    new ChapterThing { event_name = "keepsake_tea_flask", who = Character.human, label = "Look in the halt hut", requires = new string[] { "ran_back" }, keepsake = "halt_flask", memory_text = "In the halt hut, a track worker's flask of tea, labelled: 'For whoever's on nights.'", x = 4650, y = 520 },
                },
                zones = new ChapterZone[]
                {
                    new ChapterZone { event_name = "fallen_tree", who = ZoneWho.dog, requires = new string[] { "on_the_line" }, x = 1650, y = 300, width = 120, height = 240 },
                    new ChapterZone { event_name = "line_climbing", who = ZoneWho.dog, requires = new string[] { "round_the_tree" }, x = 2400, y = 300, width = 150, height = 240 },
                    new ChapterZone { event_name = "rails_hum", who = ZoneWho.dog, requires = new string[] { "viaduct" }, x = 3200, y = 300, width = 120, height = 240 },
                    new ChapterZone { event_name = "recess_reached", who = ZoneWho.human, requires = new string[] { "ran_back" }, x = 3330, y = 300, width = 150, height = 240 },
                },
                rules = new StoryRule[]
                {
                    // Up the line (Bodhi).
                    new StoryRule { on = "level_started", control = Character.dog, beat = ChapterBeat.dog_section, follow = false, fetch = false, schedule_seconds = 3, schedule_event = "morning_1", cutscene = CutsceneChange.start, cutscene_skip_to = "on_the_line" },
                    new StoryRule { on = "morning_1", lines = new string[] { "Rails. Iron and frost. They smell of the valley. Up. Home is up.", "The old dog follows now. Slow. Slow is fine." }, voice = StoryVoice.dog, schedule_seconds = 6, schedule_event = "on_the_line" },
                    new StoryRule { on = "on_the_line", objective = "Follow the railway up the valley.", cutscene = CutsceneChange.end },
                    new StoryRule { on = "fallen_tree", lines = new string[] { "A tree, lying across. Over. The old dog can't. Round, then." }, voice = StoryVoice.dog, objective = "Help Biscuit round the fallen tree." },
                    new StoryRule { on = "round_the_tree", lines = new string[] { "Nose to his shoulder. Round. Round. There." }, voice = StoryVoice.dog, objective = "Follow the railway up the valley." },
                    new StoryRule { on = "line_climbing", lines = new string[] { "The light, going. The cold, coming. The rails go on, over nothing." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 4, schedule_event = "kofis_mum_rang" },
                    // The Lantern Train (Em).
                    new StoryRule { on = "kofis_mum_rang", lines = new string[] { "'Two dogs went up the railway line at first light,' Kofi's mum said.", "The Lantern Train left Strand at four, every window gold." }, objective = "Board the Lantern Train.", control = Character.human, beat = ChapterBeat.human_section },
                    new StoryRule { on = "boarded", lines = new string[] { "Pat the guard let me ride up front. 'I'll not run them down,' he said.", "'Not at Christmas. Not on my train.'", "We climbed out of Strand into the blue, the lanterns swinging." }, objective = "", schedule_seconds = 11, schedule_event = "viaduct", cutscene = CutsceneChange.start, cutscene_skip_to = "viaduct" },
                    // The viaduct (Bodhi).
                    new StoryRule { on = "viaduct", lines = new string[] { "Stone, and under the stone, nothing. Wind from the gorge. Ice, hanging.", "The old dog stops. Shakes. Goes on." }, voice = StoryVoice.dog, objective = "Lead Biscuit across the viaduct.", control = Character.dog, beat = ChapterBeat.dog_section, cutscene = CutsceneChange.end },
                    new StoryRule { on = "rails_hum", lines = new string[] { "The rails hum. Something big is coming. Big and loud and full of light." }, voice = StoryVoice.dog, objective = "Get Biscuit into the recess in the wall!", fear_change = 0.4 },
                    // The train passes (a film).
                    new StoryRule { on = "biscuit_in_recess", lines = new string[] { "Into the stone. Push the old dog in. Press. Press." }, voice = StoryVoice.dog, objective = "", schedule_seconds = 4, schedule_event = "train_passes", cutscene = CutsceneChange.start, cutscene_skip_to = "ran_back" },
                    new StoryRule { on = "train_passes", lines = new string[] { "Ginger curls, in the headlamp. One heartbeat, and gone under us." }, schedule_seconds = 3.5, schedule_event = "her_through_glass" },
                    new StoryRule { on = "her_through_glass", lines = new string[] { "Through the glass, going past: her. HER." }, voice = StoryVoice.dog, schedule_seconds = 3, schedule_event = "bolted" },
                    new StoryRule { on = "bolted", lines = new string[] { "Then the noise. The old dog, running blind. Run with him. Run." }, voice = StoryVoice.dog, schedule_seconds = 3.5, schedule_event = "brake" },
                    new StoryRule { on = "brake", lines = new string[] { "Pat pulled the brake before I'd finished shouting." }, schedule_seconds = 4, schedule_event = "ran_back" },
                    // Running back (Em). A beat of its own, so only a whistle from here on answers it.
                    new StoryRule { on = "ran_back", lines = new string[] { "The train stopped beyond the viaduct. I ran back along the line with the lantern." }, objective = "Run back to the recess.", control = Character.human, beat = ChapterBeat.near_contact, fear_change = -0.3, cutscene = CutsceneChange.end },
                    new StoryRule { on = "recess_reached", lines = new string[] { "In the recess, two sets of prints, pressed right back against the stone.", "A tuft of ginger curls on the stone. Nothing else." }, objective = "Whistle into the forest." },
                    new StoryRule { when_all = new string[] { "recess_reached", "human_called_during_near_contact" }, lines = new string[] { "I whistled into the dark." }, objective = "", schedule_seconds = 3, schedule_event = "one_bark", cutscene = CutsceneChange.start, cutscene_skip_to = "chapter_done" },
                    new StoryRule { on = "one_bark", lines = new string[] { "Then, far down in the trees, one bark. His." }, schedule_seconds = 6, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
                aftermath = new string[] { "Chapter Six: The Reindeer Hill.", "The same night." },
            };
        }
    }
}
