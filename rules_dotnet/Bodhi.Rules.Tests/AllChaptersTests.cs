// Purpose: proves all twelve chapters, as exported from the browser game, work on the one C#
// story engine. A robot player finishes every chapter, reaches every event the browser robot
// waits for, and finds every keepsake. Checks on the data itself catch story rules that could
// never run and reunions that don't need both of them.
//
// What the robot proves, and what it doesn't: it plays the story logic, assuming either
// character can walk anywhere. So it proves no chapter's story can get stuck, but not that every
// ledge can be jumped. That part is the Unity scene's job, checked by its own play tests.
using System;
using System.Collections.Generic;
using System.Linq;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class AllChaptersTests
    {
        // Events the engine announces or the ball game reports, which no rule needs to make.
        static readonly string[] made_by_play =
        {
            "level_started", "cutscene_skipped", "reunion_embrace", "reunion_complete", "dog_petted",
            "dog_leaned_in", "knelt_by_dog", "ball_thrown", "ball_fetched", "dog_sniffed",
        };

        public static IEnumerable<string> chapter_ids() => StoryChapters.all().Select(chapter => chapter.id);

        // Finds a chapter by id.
        static ChapterScript chapter(string id) => StoryChapters.all().Single(script => script.id == id);

        // All twelve, in the story's order.
        [Test]
        public void twelve_chapters_in_story_order()
        {
            Assert.That(chapter_ids(), Is.EqualTo(new[]
            {
                "coming_home", "the_storm", "last_morning", "other_side", "motorway", "visiting_hours",
                "orchard", "lessons", "first_snow", "crossing", "high_farm", "home",
            }));
        }

        // Bodhi grows through the year and never shrinks: a puppy at 8 weeks, grown at one year.
        [Test]
        public void bodhi_only_grows()
        {
            var chapters = StoryChapters.all();
            Assert.That(chapters.First().dog_size, Is.EqualTo(0.46));
            Assert.That(chapters.Last().dog_size, Is.EqualTo(1.0));
            for (int index = 1; index < chapters.Length; index++)
            {
                Assert.That(chapters[index].dog_size, Is.GreaterThanOrEqualTo(chapters[index - 1].dog_size), chapters[index].id);
                Assert.That(chapters[index].dog_jump, Is.GreaterThanOrEqualTo(chapters[index - 1].dog_jump), chapters[index].id);
            }
        }

        // Every chapter can be finished, watching every film scene through.
        [TestCaseSource(nameof(chapter_ids))]
        public void every_chapter_can_be_finished(string id)
        {
            var run = ChapterRobot.play(chapter(id), skip_films: false);
            Assert.That(run.is_complete, Is.True, id + " got stuck after: " + string.Join(", ", run.event_order.TakeLast(8)));
        }

        // Every chapter can be finished skipping every film scene, so Skip never breaks a chapter.
        [TestCaseSource(nameof(chapter_ids))]
        public void every_chapter_can_be_finished_skipping_films(string id)
        {
            var run = ChapterRobot.play(chapter(id), skip_films: true);
            Assert.That(run.is_complete, Is.True, id + " got stuck after: " + string.Join(", ", run.event_order.TakeLast(8)));
        }

        // The robot reaches every event the browser playtest waits for, so both tell the same story.
        [Test]
        public void every_browser_checkpoint_is_reached()
        {
            foreach (var (chapter_id, events) in ChapterCheckpoints.all)
            {
                var run = ChapterRobot.play(chapter(chapter_id), skip_films: false);
                foreach (var event_name in events)
                {
                    Assert.That(run.has_happened(event_name), Is.True, chapter_id + " never reached " + event_name);
                }
            }
        }

        // Every keepsake can be found: 4 in the first chapter, 3 in each of the others. (This
        // test found three of Bodhi's that couldn't be, in The Last Morning and Home, because
        // nothing there let the player be him. Both chapters now let you switch to him.)
        [TestCaseSource(nameof(chapter_ids))]
        public void every_keepsake_can_be_found(string id)
        {
            var script = chapter(id);
            var keepsakes = script.things.Where(thing => !string.IsNullOrEmpty(thing.keepsake)).Select(thing => thing.keepsake).ToList();
            Assert.That(keepsakes.Count, Is.EqualTo(id == "coming_home" ? 4 : 3));
            var run = ChapterRobot.play(script, skip_films: false);
            Assert.That(run.found_keepsakes, Is.EquivalentTo(keepsakes), id);
        }

        // Break it: a chapter with one of Bodhi's hidden things must let the player be him at
        // some point, by handing him control or by allowing a switch.
        [TestCaseSource(nameof(chapter_ids))]
        public void a_chapter_with_his_hidden_things_lets_you_be_him(string id)
        {
            var script = chapter(id);
            if (script.things.Any(thing => thing.who == Character.dog && thing.hidden))
            {
                Assert.That(script.rules.Any(rule => rule.control == Character.dog || rule.switching == true), Is.True, id);
            }
        }

        // In The Last Morning and Home you can switch to Bodhi from the start, and switching
        // stops when she sets out (Last Morning) or tucks him in (Home), so each ending plays
        // as her, as written.
        [TestCase("last_morning", "set_out")]
        [TestCase("home", "tucked_in")]
        public void the_quiet_chapters_let_you_switch_until_the_ending(string id, string ending_event)
        {
            var script = chapter(id);
            var run = new ChapterRun(script);
            run.start();
            Assert.That(run.switching_allowed, Is.True, id + ": from the start");
            Assert.That(run.switch_character(), Is.True);
            Assert.That(run.controlled_character, Is.EqualTo(Character.dog));
            ChapterRobot.finish(run, skip_films: false, stop_when: playing => playing.has_happened(ending_event));
            Assert.That(run.switching_allowed, Is.False, id + ": after " + ending_event);
            Assert.That(run.controlled_character, Is.EqualTo(Character.human), id + ": the ending plays as her");
        }

        // Break it: while you're playing Bodhi, she can't set out (or tuck him in): those are
        // hers to do, so the ending always plays as her. Switch back, and she can.
        [Test]
        public void the_endings_wait_until_you_are_her()
        {
            var run = new ChapterRun(chapter("last_morning"));
            run.start();
            Assert.That(run.use("photo_taken", true), Is.True);
            Assert.That(run.switch_character(), Is.True);
            Assert.That(run.report_zone("set_out", true, true), Is.False, "as him");
            Assert.That(run.switch_character(), Is.True);
            Assert.That(run.report_zone("set_out", true, true), Is.True, "as her");
            Assert.That(run.switching_allowed, Is.False);
            Assert.That(chapter("home").things.Single(thing => thing.event_name == "tucked_in").who, Is.EqualTo(Character.human));
        }

        // Every event a rule waits for can be made by something: a thing, a zone, a timer, a
        // film's Skip, a call or the engine. Otherwise that rule could never run.
        [TestCaseSource(nameof(chapter_ids))]
        public void no_story_rule_waits_for_an_event_nothing_makes(string id)
        {
            var script = chapter(id);
            var made = new HashSet<string>(made_by_play);
            made.UnionWith(script.things.Select(thing => thing.event_name));
            made.UnionWith(script.zones.Select(zone => zone.event_name));
            made.UnionWith(script.rules.Select(rule => rule.schedule_event));
            made.UnionWith(script.rules.Select(rule => rule.cutscene_skip_to));
            foreach (var rule in script.rules)
            {
                foreach (var event_name in rule.when_all.Append(rule.on).Where(name => !string.IsNullOrEmpty(name)))
                {
                    bool from_a_call = event_name.Contains("_called") || event_name.EndsWith("_answered") || event_name.StartsWith("dog_sniffed_during_");
                    Assert.That(from_a_call || made.Contains(event_name), Is.True, id + ": nothing makes " + event_name);
                }
            }
        }

        // Spec R6.2: each reunion is earned by both of them, one act by her and one by Bodhi.
        [TestCaseSource(nameof(chapter_ids))]
        public void every_reunion_needs_both_of_them(string id)
        {
            var script = chapter(id);
            foreach (var rule in script.rules.Where(rule => rule.reunion))
            {
                var doers = rule.when_all
                    .Select(event_name => script.things.FirstOrDefault(thing => thing.event_name == event_name))
                    .Where(thing => thing != null)
                    .Select(thing => thing.who)
                    .Distinct();
                Assert.That(doers, Is.EquivalentTo(new[] { Character.human, Character.dog }), id);
            }
        }

        // Break it: running the exporter's output twice gives fresh copies, so one playthrough
        // can't change the chapter another one plays.
        [Test]
        public void each_copy_of_a_chapter_is_separate()
        {
            var first = StoryChapters.the_storm();
            first.rules[0].lines = new[] { "changed" };
            Assert.That(StoryChapters.the_storm().rules[0].lines, Is.Not.EqualTo(new[] { "changed" }));
        }
    }

    // A robot player for the story logic. Each step it does the first thing that moves the story:
    // hold Bodhi in a reunion, sniff around, use something, walk into a zone, call, pet, play fetch, or
    // switch characters. When nothing helps, it waits a quarter of a second.
    static class ChapterRobot
    {
        const double step_seconds = 0.25;
        const double longest_game_seconds = 3600;  // An hour of game time means it's stuck.

        // Plays a chapter to its end (or until it's clearly stuck) and returns the run.
        public static ChapterRun play(ChapterScript script, bool skip_films)
        {
            var run = new ChapterRun(script);
            finish(run, skip_films);
            return run;
        }

        // Plays a run to its end, starting it first if nothing has happened yet. With stop_when,
        // it stops early, as soon as that question about the run answers true. after_each_step,
        // if given, is called after every step, so a test can look at each moment.
        public static void finish(ChapterRun run, bool skip_films, Func<ChapterRun, bool> stop_when = null, Action<ChapterRun> after_each_step = null)
        {
            var script = run.chapter;
            if (run.event_order.Count == 0)
            {
                run.start();
            }
            var sniffed_at = new HashSet<ChapterThing>();
            int switches_without_progress = 0;
            for (double seconds = 0; seconds < longest_game_seconds && !run.is_complete; seconds += step_seconds)
            {
                if (stop_when != null && stop_when(run))
                {
                    return;
                }
                int events_before = run.event_order.Count;
                if (run.cutscene_playing && skip_films)
                {
                    run.skip_cutscene();
                }
                else if (!run.controls_locked)
                {
                    act_once(run, script, sniffed_at, ref switches_without_progress);
                }
                if (run.event_order.Count == events_before)
                {
                    run.pass_time(step_seconds);
                }
                else
                {
                    switches_without_progress = 0;
                }
                after_each_step?.Invoke(run);
            }
        }

        // Does the first useful thing. Only one per step, like a player pressing one button.
        static void act_once(ChapterRun run, ChapterScript script, HashSet<ChapterThing> sniffed_at, ref int switches_without_progress)
        {
            if (run.hold_dog(true))
            {
                return;
            }
            // Like a player exploring: while he's Bodhi, sniff around before moving the story on.
            // While she's playing and a switch is allowed, switch to him first if he hasn't
            // sniffed out everything yet.
            var hidden = script.things.FirstOrDefault(thing => thing.hidden && !sniffed_at.Contains(thing) && !run.has_happened(thing.event_name));
            if (hidden != null && run.controlled_character == Character.dog)
            {
                sniffed_at.Add(hidden);
                run.sniff(hidden.x, hidden.y);
                return;
            }
            if (hidden != null && run.switching_allowed && run.switch_character())
            {
                return;
            }
            // Keepsakes first: a player picks up what they find before moving on.
            foreach (var thing in script.things.OrderBy(thing => string.IsNullOrEmpty(thing.keepsake)))
            {
                if (run.use(thing.event_name, true))
                {
                    return;
                }
            }
            foreach (var zone in script.zones)
            {
                if (run.report_zone(zone.event_name, true, true))
                {
                    return;
                }
            }
            if (story_waits_for(script, run, run.controlled_character + "_called") && run.call())
            {
                return;
            }
            int events_before_petting = run.event_order.Count;
            if ((story_waits_for(script, run, "dog_petted") || story_waits_for(script, run, "dog_leaned_in")) && run.pet()
                && run.event_order.Count > events_before_petting)  // Only she can pet: as him, it does nothing new.
            {
                return;
            }
            if (story_waits_for(script, run, "knelt_by_dog") && run.kneel(true) && run.has_happened("knelt_by_dog"))
            {
                return;
            }
            if (run.fetch_enabled && (run.report_event("ball_thrown") || run.report_event("ball_fetched")))
            {
                return;
            }
            if (run.switching_allowed && switches_without_progress < 2)
            {
                switches_without_progress++;
                run.switch_character();
            }
        }

        // True if some rule not yet run waits for an event starting with this name (for calls,
        // that covers "human_called_during_human_section" too) that hasn't happened.
        static bool story_waits_for(ChapterScript script, ChapterRun run, string name_start)
        {
            return script.rules.Any(rule => rule.when_all.Append(rule.on).Any(event_name =>
                event_name != null && event_name.StartsWith(name_start, StringComparison.Ordinal) && !run.has_happened(event_name)));
        }
    }
}
