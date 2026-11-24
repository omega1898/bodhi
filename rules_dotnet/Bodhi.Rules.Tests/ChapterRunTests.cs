// Purpose: proves the one story engine that runs any chapter from data (ChapterRun). Each test
// builds a tiny chapter by hand, plays it the way the scene would report things, and checks the
// story moves exactly as the browser game's rules did (prototype/web/js/game.js).
using System;
using System.Linq;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class ChapterRunTests
    {
        // A small flood chapter in the shape of "The Storm": a film opening, a split, a call that
        // hands control to Bodhi, a meeting zone, a shared final act and a reunion.
        static ChapterScript flood_chapter()
        {
            return new ChapterScript
            {
                id = "test_flood",
                title = "Test Flood",
                start_fear = 0.05,
                start_closeness = 0.5,
                things = new[]
                {
                    new ChapterThing { event_name = "sheets_taken", who = Character.human, requires = new[] { "storm_done" }, x = 250, y = 300 },
                    new ChapterThing { event_name = "sheets_lowered", who = Character.human, requires = new[] { "pair_met" }, x = 1385, y = 300 },
                    new ChapterThing { event_name = "sheet_grabbed", who = Character.dog, requires = new[] { "sheets_lowered" }, x = 1640, y = 410 },
                    new ChapterThing { event_name = "keepsake_lead", who = Character.dog, hidden = true, keepsake = "lead", memory_text = "His lead, by the door.", x = 2900, y = 520 },
                    new ChapterThing { event_name = "bowl_filled", who = Character.human, needs_dog = true, care = CareKind.meal, x = 100, y = 300 },
                },
                zones = new[]
                {
                    new ChapterZone { event_name = "pair_met", who = ZoneWho.both, requires = new[] { "dog_climbing" } },
                    new ChapterZone { event_name = "reached_window", who = ZoneWho.human, requires = new[] { "reunion_complete" } },
                    new ChapterZone { event_name = "porch_step", who = ZoneWho.human },
                },
                rules = new[]
                {
                    new StoryRule { on = "level_started", cutscene = CutsceneChange.start, cutscene_skip_to = "storm_done", lines = new[] { "Rain all week." }, schedule_seconds = 4, schedule_event = "storm_1" },
                    new StoryRule { on = "storm_1", beat = ChapterBeat.separation, fear_change = 0.6, closeness_change = 0.3, remove = new[] { "stairs" }, schedule_seconds = 3, schedule_event = "storm_done" },
                    new StoryRule { on = "storm_done", cutscene = CutsceneChange.end, beat = ChapterBeat.human_section, remove = new[] { "stairs" }, objective = "Find something to pull {them} up with." },
                    new StoryRule { when_all = new[] { "sheets_taken", "human_called_during_human_section" }, control = Character.dog, follow = true, beat = ChapterBeat.dog_section, voice = StoryVoice.dog, lines = new[] { "Her whistle. Up." }, schedule_seconds = 0.1, schedule_event = "dog_climbing" },
                    new StoryRule { on = "pair_met", control = Character.human, beat = ChapterBeat.near_contact, follow = false },
                    new StoryRule { on = "sheets_lowered", beat = ChapterBeat.final_obstacle, switching = true },
                    new StoryRule { when_all = new[] { "sheets_lowered", "sheet_grabbed" }, reunion = true },
                    new StoryRule { on = "reunion_complete", objective = "Out of the window." },
                    new StoryRule { on = "reached_window", schedule_seconds = 7, schedule_event = "chapter_done" },
                    new StoryRule { on = "chapter_done", complete = true },
                },
            };
        }

        // Runs time forward in small steps, the way the game's fixed update does.
        static void wait(ChapterRun run, double seconds)
        {
            for (double passed = 0; passed < seconds - 1e-9; passed += 0.05)
            {
                run.pass_time(0.05);
            }
        }

        // Starting fires level_started: the opening film runs and its first line is queued.
        [Test]
        public void starting_runs_the_opening_scene()
        {
            var run = new ChapterRun(flood_chapter());
            run.start();
            Assert.That(run.has_happened("level_started"), Is.True);
            Assert.That(run.cutscene_playing, Is.True);
            Assert.That(run.controls_locked, Is.True);
            var lines = run.take_lines();
            Assert.That(lines.Select(line => line.text), Is.EqualTo(new[] { "Rain all week." }));
            Assert.That(lines[0].voice, Is.EqualTo(StoryVoice.her));
            Assert.That(run.take_lines(), Is.Empty);  // Taken once.
        }

        // Timers fire their events after the scheduled time; the film then ends by itself.
        [Test]
        public void scheduled_events_fire_on_time()
        {
            var run = new ChapterRun(flood_chapter());
            run.start();
            wait(run, 3.9);
            Assert.That(run.has_happened("storm_1"), Is.False);
            wait(run, 0.2);
            Assert.That(run.has_happened("storm_1"), Is.True);
            Assert.That(run.current_beat, Is.EqualTo(ChapterBeat.separation));
            Assert.That(run.solid_is_removed("stairs"), Is.True);
            Assert.That(run.bond.fear, Is.EqualTo(0.65).Within(1e-9));
            Assert.That(run.bond.closeness_need, Is.EqualTo(0.8).Within(1e-9));
            wait(run, 3.1);
            Assert.That(run.current_beat, Is.EqualTo(ChapterBeat.human_section));
            Assert.That(run.cutscene_playing, Is.False);
            Assert.That(run.objective, Is.EqualTo("Find something to pull {them} up with."));
        }

        // Skip drops the film's timers and jumps to its end event, exactly once.
        [Test]
        public void skipping_the_film_jumps_to_its_end()
        {
            var run = new ChapterRun(flood_chapter());
            run.start();
            Assert.That(run.skip_cutscene(), Is.True);
            Assert.That(run.has_happened("cutscene_skipped"), Is.True);
            Assert.That(run.has_happened("storm_done"), Is.True);
            Assert.That(run.cutscene_playing, Is.False);
            wait(run, 10);
            Assert.That(run.has_happened("storm_1"), Is.False);  // Its timer was dropped.
            Assert.That(run.skip_cutscene(), Is.False);           // Nothing left to skip.
        }

        // A thing can't be used before its requirements, by the wrong character, or twice.
        [Test]
        public void things_follow_their_requirements()
        {
            var run = new ChapterRun(flood_chapter());
            run.start();
            Assert.That(run.use("sheets_taken", false), Is.False);  // The film is still playing.
            run.skip_cutscene();
            Assert.That(run.use("sheet_grabbed", false), Is.False);  // Bodhi's job, and not yet.
            Assert.That(run.use("no_such_thing", false), Is.False);
            Assert.That(run.can_use("sheets_taken", false), Is.True);
            Assert.That(run.use("sheets_taken", false), Is.True);
            Assert.That(run.use("sheets_taken", false), Is.False);  // Each thing counts once.
        }

        // Whistling during a beat fires both call events; together with the sheets that hands
        // control to Bodhi and starts him following.
        [Test]
        public void a_call_and_a_thing_together_hand_over_control()
        {
            var run = new ChapterRun(flood_chapter());
            run.start();
            run.skip_cutscene();
            run.use("sheets_taken", false);
            Assert.That(run.controlled_character, Is.EqualTo(Character.human));
            double fear_before = run.bond.fear;
            Assert.That(run.call(), Is.True);
            Assert.That(run.has_happened("human_called"), Is.True);
            Assert.That(run.has_happened("human_called_during_human_section"), Is.True);
            Assert.That(run.bond.fear, Is.EqualTo(Math.Max(0, fear_before - 0.03)).Within(1e-9));
            Assert.That(run.controlled_character, Is.EqualTo(Character.dog));
            Assert.That(run.call(), Is.False);  // Still getting his breath back: 1.4 seconds between calls.
            Assert.That(run.dog_follows, Is.True);
            Assert.That(run.take_lines().Last().voice, Is.EqualTo(StoryVoice.dog));
            wait(run, 0.8);
            Assert.That(run.has_happened("dog_climbing"), Is.True);
            Assert.That(run.has_happened("dog_answered"), Is.True);  // He answers her whistle.
        }

        // A "both" zone needs both inside; a one-character zone needs that character in control.
        [Test]
        public void zones_check_who_is_inside()
        {
            var run = new ChapterRun(flood_chapter());
            run.start();
            run.skip_cutscene();
            Assert.That(run.report_zone("pair_met", true, true), Is.False);  // Not open yet.
            run.use("sheets_taken", false);
            run.call();
            wait(run, 0.2);
            Assert.That(run.report_zone("pair_met", false, true), Is.False);  // Only Bodhi is there.
            Assert.That(run.report_zone("porch_step", true, false), Is.False);  // She's there, but the player is Bodhi.
            Assert.That(run.report_zone("pair_met", true, true), Is.True);
            Assert.That(run.current_beat, Is.EqualTo(ChapterBeat.near_contact));
            Assert.That(run.report_zone("pair_met", true, true), Is.False);  // Once only.
        }

        // Switching is only allowed when the story says so.
        [Test]
        public void switching_needs_permission()
        {
            var run = play_to_final_obstacle();
            Assert.That(run.switching_allowed, Is.True);
            Assert.That(run.can_use("sheet_grabbed", false), Is.False);  // Open, but it's Bodhi's job.
            Assert.That(run.switch_character(), Is.True);
            Assert.That(run.controlled_character, Is.EqualTo(Character.dog));
            Assert.That(run.can_use("sheet_grabbed", false), Is.True);
            var fresh = new ChapterRun(flood_chapter());
            fresh.start();
            fresh.skip_cutscene();
            Assert.That(fresh.hold_dog(true), Is.False);  // No reunion to hold him in.
            Assert.That(fresh.switch_character(), Is.False);
            Assert.That(fresh.controlled_character, Is.EqualTo(Character.human));
        }

        // Both final acts start the reunion: control goes to her, switching stops, and holding
        // Bodhi close starts the embrace, which ends 6.5 seconds later.
        [Test]
        public void the_reunion_plays_out()
        {
            var run = play_to_final_obstacle();
            run.switch_character();
            Assert.That(run.use("sheet_grabbed", false), Is.True);
            Assert.That(run.reunion_stage, Is.EqualTo(ReunionStage.approach));
            Assert.That(run.controlled_character, Is.EqualTo(Character.human));
            Assert.That(run.switching_allowed, Is.False);
            Assert.That(run.objective, Is.EqualTo(""));
            Assert.That(run.hold_dog(false), Is.False);  // He has to be right there.
            Assert.That(run.hold_dog(true), Is.True);
            Assert.That(run.reunion_stage, Is.EqualTo(ReunionStage.embrace));
            Assert.That(run.bond.fear, Is.EqualTo(0.0));
            Assert.That(run.bond.closeness_need, Is.EqualTo(0.95).Within(1e-9));
            Assert.That(run.has_happened("reunion_embrace"), Is.True);
            Assert.That(run.controls_locked, Is.True);
            wait(run, 6.4);
            Assert.That(run.has_happened("reunion_complete"), Is.False);
            wait(run, 0.2);
            Assert.That(run.reunion_stage, Is.EqualTo(ReunionStage.done));
            Assert.That(run.dog_follows, Is.True);
            Assert.That(run.objective, Is.EqualTo("Out of the window."));
        }

        // A call just before the embrace gets no answer: he doesn't bark mid-hug.
        [Test]
        public void no_answer_during_the_embrace()
        {
            var run = new ChapterRun(new ChapterScript
            {
                id = "test_hug",
                rules = new[] { new StoryRule { on = "level_started", reunion = true } },
            });
            run.start();
            Assert.That(run.call(), Is.True);
            Assert.That(run.hold_dog(true), Is.True);
            wait(run, 2);
            Assert.That(run.has_happened("dog_answered"), Is.False);
        }

        // After the reunion, reaching the window finishes the chapter; nothing moves after that.
        [Test]
        public void the_chapter_completes()
        {
            var run = play_to_final_obstacle();
            run.switch_character();
            run.use("sheet_grabbed", false);
            run.hold_dog(true);
            wait(run, 7);
            Assert.That(run.report_zone("reached_window", true, false), Is.True);
            wait(run, 7.1);
            Assert.That(run.is_complete, Is.True);
            Assert.That(run.call(), Is.False);
            int count = run.event_order.Count;
            run.pass_time(100);
            Assert.That(run.event_order.Count, Is.EqualTo(count));
        }

        // Sniffing reveals hidden things close to his nose; a keepsake is remembered with its line.
        [Test]
        public void sniffing_reveals_keepsakes()
        {
            var run = play_to_dog_section();
            Assert.That(run.can_use("keepsake_lead", false), Is.False);  // Still hidden.
            Assert.That(run.sniff(2000, 520), Is.True);                   // Too far away.
            Assert.That(run.can_use("keepsake_lead", false), Is.False);
            Assert.That(run.is_revealed("lead"), Is.False);
            Assert.That(run.is_revealed(null), Is.False, "break it: no name, nothing found");
            Assert.That(run.has_happened("dog_sniffed_during_dog_section"), Is.True);
            run.sniff(2800, 520);
            Assert.That(run.is_revealed("lead"), Is.True, "its keepsake name, not its event");
            Assert.That(run.is_revealed("keepsake_lead"), Is.False);
            Assert.That(run.can_use("keepsake_lead", false), Is.True);
            run.take_lines();
            Assert.That(run.use("keepsake_lead", false), Is.True);
            Assert.That(run.found_keepsakes, Is.EquivalentTo(new[] { "lead" }));
            Assert.That(run.take_lines().Single().text, Is.EqualTo("His lead, by the door."));
        }

        // Only Bodhi sniffs; she kneels instead.
        [Test]
        public void only_bodhi_sniffs()
        {
            var run = new ChapterRun(flood_chapter());
            run.start();
            run.skip_cutscene();
            Assert.That(run.sniff(2900, 520), Is.False);
        }

        // A meal needs him close and empties his hunger; petting calms him.
        [Test]
        public void care_and_petting()
        {
            var script = flood_chapter();
            script.start_hunger = 0.6;
            script.start_dirt = 0.4;
            script.start_food = 1;
            script.start_fear = 0.5;
            var run = new ChapterRun(script);
            run.start();
            run.skip_cutscene();
            Assert.That(run.care.hunger, Is.EqualTo(0.6));
            Assert.That(run.care.food, Is.EqualTo(1));
            Assert.That(run.use("bowl_filled", false), Is.False);
            Assert.That(run.use("bowl_filled", true), Is.True);
            Assert.That(run.care.hunger, Is.EqualTo(0.0));
            Assert.That(run.care.food, Is.EqualTo(1));  // A meal at home doesn't use the bag.
            double fear_before = run.bond.fear;
            Assert.That(run.pet(), Is.True);
            Assert.That(run.has_happened("dog_petted"), Is.True);
            Assert.That(run.bond.fear, Is.EqualTo(Math.Max(0, fear_before - 0.08)).Within(1e-9));
        }

        // Found food goes in the bag, an apple is a treat, and a bath takes all the mud off.
        [Test]
        public void things_that_look_after_him()
        {
            var run = new ChapterRun(new ChapterScript
            {
                id = "test_orchard",
                start_hunger = 0.8,
                start_dirt = 0.9,
                start_food = 0,
                things = new[]
                {
                    new ChapterThing { event_name = "pantry_food", food = 3 },
                    new ChapterThing { event_name = "apple_eaten", care = CareKind.treat },
                    new ChapterThing { event_name = "towelled_dry", care = CareKind.bath, needs_dog = true },
                },
            });
            run.start();
            run.use("pantry_food", false);
            Assert.That(run.care.food, Is.EqualTo(3));
            run.use("apple_eaten", false);
            Assert.That(run.care.hunger, Is.EqualTo(0.3).Within(1e-9));
            run.use("towelled_dry", true);
            Assert.That(run.care.dirt, Is.EqualTo(0.0));
        }

        // Care carries on from the last chapter unless the chapter sets its own.
        [Test]
        public void care_carries_over()
        {
            var carried = new CareState(0.6, 0.2, 5);
            var run = new ChapterRun(new ChapterScript { id = "test_carry", start_dirt = 0.7 }, carried);
            Assert.That(run.care.hunger, Is.EqualTo(0.6));
            Assert.That(run.care.dirt, Is.EqualTo(0.7));
            Assert.That(run.care.food, Is.EqualTo(5));
        }

        // A rule's care values replace what's there (a roll in the mud, a packed bag).
        [Test]
        public void rules_can_set_care()
        {
            var script = new ChapterScript
            {
                id = "test_care",
                rules = new[] { new StoryRule { on = "level_started", care_dirt = 0.8, care_food = 3 } },
            };
            var run = new ChapterRun(script);
            run.start();
            Assert.That(run.care.dirt, Is.EqualTo(0.8));
            Assert.That(run.care.food, Is.EqualTo(3));
            Assert.That(run.care.hunger, Is.EqualTo(new CareState().hunger));
        }

        // "unless" stops a rule's effects but still uses it up; "until" closes a thing.
        [Test]
        public void unless_and_until()
        {
            var script = new ChapterScript
            {
                id = "test_unless",
                things = new[]
                {
                    new ChapterThing { event_name = "reached_for_photo", who = Character.human, until = new[] { "photo_gone" } },
                },
                rules = new[]
                {
                    new StoryRule { on = "photo_gone", unless = new[] { "reached_for_photo" }, lines = new[] { "Gone." } },
                    new StoryRule { on = "photo_gone", lines = new[] { "The river took it." } },
                },
            };
            var reached = new ChapterRun(script);
            reached.start();
            reached.use("reached_for_photo", false);
            reached.report_event("photo_gone");
            Assert.That(reached.take_lines().Select(line => line.text), Is.EqualTo(new[] { "The river took it." }));

            var too_late = new ChapterRun(script);
            too_late.start();
            too_late.report_event("photo_gone");
            Assert.That(too_late.can_use("reached_for_photo", false), Is.False);
            Assert.That(too_late.take_lines().Select(line => line.text), Is.EqualTo(new[] { "Gone.", "The river took it." }));
        }

        // Personalising fills in his name and he/him/his, never she.
        [Test]
        public void text_is_personalised_with_he()
        {
            Assert.That(StoryText.personalize("{dog} wagged. I held {them} and {their} lead.", "Bodhi"),
                Is.EqualTo("Bodhi wagged. I held him and his lead."));
        }

        // Break it: a script with nulls, empty names, a rule that loops on itself, NaN times and
        // huge timers must not crash or hang.
        [Test]
        public void hostile_scripts_do_not_crash()
        {
            var script = new ChapterScript
            {
                id = null,
                things = new ChapterThing[] { null, new ChapterThing { event_name = null, requires = null, until = null } },
                zones = new ChapterZone[] { null, new ChapterZone { event_name = "", requires = null, unless = null } },
                rules = new StoryRule[]
                {
                    null,
                    new StoryRule { on = "level_started", schedule_seconds = 0, schedule_event = "loop" },
                    new StoryRule { on = "loop", schedule_seconds = 0, schedule_event = "loop", when_all = null, unless = null, lines = null, remove = null, add = null },
                    new StoryRule { on = "loop", schedule_seconds = double.NaN, schedule_event = "never" },
                    new StoryRule { on = "loop", schedule_seconds = 1e300, schedule_event = "far" },
                },
            };
            var run = new ChapterRun(script);
            run.start();
            run.pass_time(double.NaN);
            run.pass_time(-5);
            run.pass_time(0.1);
            run.pass_time(double.PositiveInfinity);
            Assert.That(run.has_happened("loop"), Is.True);
            Assert.That(run.has_happened("never"), Is.False);
            Assert.That(run.use(null, true), Is.False);
            Assert.That(run.report_zone(null, true, true), Is.False);
            Assert.That(run.report_event(null), Is.False);
            Assert.That(run.report_event(""), Is.False);
            Assert.Throws<ArgumentNullException>(() => new ChapterRun(null));
        }

        // Break it: the scene can't report a thing's or a zone's event directly to skip its checks.
        [Test]
        public void reports_cannot_skip_checks()
        {
            var run = new ChapterRun(flood_chapter());
            run.start();
            run.skip_cutscene();
            Assert.That(run.report_event("sheet_grabbed"), Is.False);
            Assert.That(run.report_event("pair_met"), Is.False);
            Assert.That(run.report_event("reunion_complete"), Is.False);  // Only the reunion itself says that.
            Assert.That(run.has_happened("sheet_grabbed"), Is.False);
        }

        // Break it: an event name past the length limit is refused, so a scene can't flood memory.
        [Test]
        public void overlong_event_names_are_refused()
        {
            var run = new ChapterRun(new ChapterScript { id = "test_long" });
            run.start();
            Assert.That(run.report_event(new string('a', ChapterRun.maximum_event_name_length + 1)), Is.False);
            Assert.That(run.report_event(new string('a', ChapterRun.maximum_event_name_length)), Is.True);
        }

        // Plays the flood chapter up to the final obstacle, where switching opens.
        static ChapterRun play_to_final_obstacle()
        {
            var run = play_to_dog_section();
            run.report_zone("pair_met", true, true);
            run.use("sheets_lowered", false);
            return run;
        }

        // Plays the flood chapter until the player controls Bodhi.
        static ChapterRun play_to_dog_section()
        {
            var run = new ChapterRun(flood_chapter());
            run.start();
            run.skip_cutscene();
            run.use("sheets_taken", false);
            run.call();
            wait(run, 0.2);
            return run;
        }
    }
}
