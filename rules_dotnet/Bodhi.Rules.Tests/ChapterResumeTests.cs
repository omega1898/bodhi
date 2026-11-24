// Purpose: proves saving in the middle of a chapter and carrying on later, the way the browser
// game's save_game.js does. A phone can close the game at any moment, so a player must come
// back to where they were. The save lists what has happened; resuming replays it quietly
// through the same rules. A save is treated as hostile: anything damaged is refused whole.
using System;
using System.Collections.Generic;
using System.Linq;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class ChapterResumeTests
    {
        static readonly SavedPlaces somewhere = new SavedPlaces(120.5, 300, 140, 300);

        // A small kitchen chapter: an opening film, feeding him, then switching to find his sock,
        // a reunion and a last walk into the garden.
        static ChapterScript kitchen_chapter()
        {
            return new ChapterScript
            {
                id = "test_kitchen",
                title = "Test Kitchen",
                things = new[]
                {
                    new ChapterThing { event_name = "bowl_used", who = Character.human, care = CareKind.meal, requires = new[] { "intro_done" }, x = 100, y = 300 },
                    new ChapterThing { event_name = "sock_found", who = Character.dog, hidden = true, keepsake = "sock", x = 500, y = 300 },
                    new ChapterThing { event_name = "door_opened", who = Character.human, requires = new[] { "bowl_used" }, x = 900, y = 300 },
                },
                zones = new[]
                {
                    new ChapterZone { event_name = "garden_reached", who = ZoneWho.both, requires = new[] { "reunion_complete" } },
                },
                rules = new[]
                {
                    new StoryRule { on = "level_started", cutscene = CutsceneChange.start, cutscene_skip_to = "intro_done", lines = new[] { "Morning." }, schedule_seconds = 2, schedule_event = "intro_done" },
                    new StoryRule { on = "intro_done", cutscene = CutsceneChange.end, beat = ChapterBeat.human_section, objective = "Feed {them}." },
                    new StoryRule { on = "bowl_used", switching = true, objective = "Find the sock." },
                    new StoryRule { when_all = new[] { "door_opened", "sock_found" }, reunion = true },
                    new StoryRule { on = "reunion_complete", objective = "Into the garden." },
                    new StoryRule { on = "garden_reached", complete = true },
                },
            };
        }

        // Plays the kitchen chapter past its opening film, to where she can feed him.
        static ChapterRun kitchen_after_film()
        {
            var run = new ChapterRun(kitchen_chapter());
            run.start();
            run.skip_cutscene();
            return run;
        }

        // Resumes a save of the kitchen chapter, or null if refused.
        static ChapterRun resume_kitchen(string text)
        {
            return ChapterRun.resume(kitchen_chapter(), text, out _);
        }

        // Fails unless two runs of the same chapter are in exactly the same story state. The save
        // leaves out events no rule reads, such as "ball_thrown" in a chapter that never asks.
        static void assert_same_story(ChapterRun expected, ChapterRun actual, string save_text)
        {
            string id = expected.chapter.id;
            var saved_events = expected.event_order.Where(name => save_text.Contains("\nevent " + name + "\n"));
            Assert.That(actual.event_order, Is.EqualTo(saved_events), id);
            Assert.That(actual.current_beat, Is.EqualTo(expected.current_beat), id);
            Assert.That(actual.controlled_character, Is.EqualTo(expected.controlled_character), id);
            Assert.That(actual.switching_allowed, Is.EqualTo(expected.switching_allowed), id);
            Assert.That(actual.dog_follows, Is.EqualTo(expected.dog_follows), id);
            Assert.That(actual.fetch_enabled, Is.EqualTo(expected.fetch_enabled), id);
            Assert.That(actual.objective, Is.EqualTo(expected.objective), id);
            Assert.That(actual.cutscene_playing, Is.EqualTo(expected.cutscene_playing), id);
            Assert.That(actual.reunion_stage, Is.EqualTo(expected.reunion_stage), id);
            Assert.That(actual.found_keepsakes, Is.EquivalentTo(expected.found_keepsakes), id);
            Assert.That(actual.bond.fear, Is.EqualTo(expected.bond.fear), id);
            Assert.That(actual.bond.closeness_need, Is.EqualTo(expected.bond.closeness_need), id);
            Assert.That(actual.care.hunger, Is.EqualTo(expected.care.hunger), id);
            Assert.That(actual.care.dirt, Is.EqualTo(expected.care.dirt), id);
            Assert.That(actual.care.food, Is.EqualTo(expected.care.food), id);
            var solids = expected.chapter.rules.SelectMany(rule => (rule.add ?? new string[0]).Concat(rule.remove ?? new string[0])).Distinct();
            foreach (var solid in solids)
            {
                Assert.That(actual.solid_is_removed(solid), Is.EqualTo(expected.solid_is_removed(solid)), id + " " + solid);
                Assert.That(actual.solid_is_added(solid), Is.EqualTo(expected.solid_is_added(solid)), id + " " + solid);
            }
            foreach (var thing in expected.chapter.things.Where(thing => thing.hidden))
            {
                Assert.That(actual.is_revealed(thing.reveal_name), Is.EqualTo(expected.is_revealed(thing.reveal_name)), id + " " + thing.reveal_name);
            }
        }

        // Every real chapter, saved halfway through and resumed, is in the same state as before,
        // and can still be finished.
        [TestCaseSource(typeof(AllChaptersTests), "chapter_ids")]
        public void every_chapter_resumes_halfway_through(string id)
        {
            int total_events = ChapterRobot.play(StoryChapters.all().Single(script => script.id == id), skip_films: false).event_order.Count;
            var run = new ChapterRun(StoryChapters.all().Single(script => script.id == id));
            ChapterRobot.finish(run, skip_films: false, stop_when: playing => playing.event_order.Count >= total_events / 2 && playing.can_save);
            Assert.That(run.is_complete, Is.False, id + " had no moment to save in its second half");
            string text = run.to_save_text(somewhere);
            var resumed = ChapterRun.resume(run.chapter, text, out SavedPlaces places);
            Assert.That(resumed, Is.Not.Null, id);
            assert_same_story(run, resumed, text);
            Assert.That(places.dog_x, Is.EqualTo(somewhere.dog_x));
            Assert.That(resumed.take_lines(), Is.Empty, "old lines aren't shown again");
            ChapterRobot.finish(resumed, skip_films: false);
            Assert.That(resumed.is_complete, Is.True, id);
        }

        // Saving the resumed run again gives the same text, so saves never drift.
        [Test]
        public void a_resumed_chapter_saves_the_same()
        {
            var run = kitchen_after_film();
            run.use("bowl_used", true);
            string text = run.to_save_text(somewhere);
            Assert.That(resume_kitchen(text).to_save_text(somewhere), Is.EqualTo(text));
        }

        // No saving during a film scene, the reunion or once the chapter is over: those moments
        // can't be rebuilt from a list of events.
        [Test]
        public void no_save_during_a_film_the_reunion_or_the_end()
        {
            var run = new ChapterRun(kitchen_chapter());
            run.start();
            Assert.That(run.can_save, Is.False, "film");
            Assert.That(run.to_save_text(somewhere), Is.Null);
            run.skip_cutscene();
            Assert.That(run.can_save, Is.True);
            run.use("bowl_used", true);
            run.use("door_opened", true);
            run.switch_character();
            run.sniff(500, 300);
            run.use("sock_found", true);
            Assert.That(run.reunion_stage, Is.EqualTo(ReunionStage.approach));
            Assert.That(run.can_save, Is.False, "approach");
            run.hold_dog(true);
            Assert.That(run.can_save, Is.False, "embrace");
            run.pass_time(ChapterRun.embrace_seconds + 0.1);
            Assert.That(run.reunion_stage, Is.EqualTo(ReunionStage.done));
            Assert.That(run.can_save, Is.True, "after the reunion");
            run.report_zone("garden_reached", true, true);
            Assert.That(run.is_complete, Is.True);
            Assert.That(run.can_save, Is.False, "complete");
        }

        // After the reunion, a resumed run knows it's over: he follows her and the goal is right.
        [Test]
        public void resuming_after_the_reunion_keeps_it_done()
        {
            var run = kitchen_after_film();
            run.use("bowl_used", true);
            run.use("door_opened", true);
            run.switch_character();
            run.sniff(500, 300);
            run.use("sock_found", true);
            run.hold_dog(true);
            run.pass_time(ChapterRun.embrace_seconds + 0.1);
            var resumed = resume_kitchen(run.to_save_text(somewhere));
            Assert.That(resumed.reunion_stage, Is.EqualTo(ReunionStage.done));
            Assert.That(resumed.dog_follows, Is.True);
            Assert.That(resumed.objective, Is.EqualTo("Into the garden."));
            Assert.That(resumed.found_keepsakes, Is.EquivalentTo(new[] { "sock" }));
            Assert.That(resumed.report_zone("garden_reached", true, true), Is.True);
            Assert.That(resumed.is_complete, Is.True);
        }

        // Something he sniffed out but nobody picked up yet is still there after resuming.
        [Test]
        public void a_sniffed_out_thing_stays_found()
        {
            var run = kitchen_after_film();
            run.use("bowl_used", true);
            run.switch_character();
            run.sniff(500, 300);
            var resumed = resume_kitchen(run.to_save_text(somewhere));
            Assert.That(resumed.can_use("sock_found", true), Is.True);
        }

        // An event no story rule reads (the scene reported it, but it changes nothing) is left
        // out of the save, so the save still loads.
        [Test]
        public void events_no_rule_reads_are_left_out()
        {
            var run = kitchen_after_film();
            Assert.That(run.report_event("ball_thrown"), Is.True);
            string text = run.to_save_text(somewhere);
            Assert.That(text, Does.Not.Contain("ball_thrown"));
            Assert.That(resume_kitchen(text), Is.Not.Null);
        }

        // The campaign carries on a saved chapter, and finishing it still counts.
        [Test]
        public void the_campaign_resumes_the_chapter_in_progress()
        {
            var campaign = new Campaign();
            var run = campaign.begin_chapter();
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.event_order.Count >= 6 && playing.can_save);
            string campaign_text = campaign.to_text();
            string chapter_text = run.to_save_text(somewhere);

            var later = new Campaign();
            Assert.That(later.load_text(campaign_text), Is.True);
            Assert.That(later.resume_chapter(StoryChapters.the_storm().id + chapter_text, out _), Is.Null, "damaged");
            var resumed = later.resume_chapter(chapter_text, out SavedPlaces places);
            Assert.That(resumed, Is.Not.Null);
            Assert.That(places.human_x, Is.EqualTo(somewhere.human_x));
            ChapterRobot.finish(resumed, skip_films: true);
            Assert.That(later.finish_chapter(resumed), Is.True);
            Assert.That(later.current_chapter.id, Is.EqualTo("the_storm"));
        }

        // Break it: a save of another chapter is refused by the campaign.
        [Test]
        public void the_campaign_refuses_another_chapters_save()
        {
            var storm = new ChapterRun(StoryChapters.the_storm());
            ChapterRobot.finish(storm, skip_films: true, stop_when: playing => playing.event_order.Count >= 6 && playing.can_save);
            var campaign = new Campaign();
            Assert.That(campaign.resume_chapter(storm.to_save_text(somewhere), out _), Is.Null);
            Assert.That(campaign.finish_chapter(storm), Is.False);
        }

        // Break it: damaged, edited or impossible saves are refused whole.
        [Test]
        public void a_damaged_save_is_refused()
        {
            var run = kitchen_after_film();
            run.use("bowl_used", true);
            string good = run.to_save_text(somewhere);
            var early = kitchen_after_film();
            string before_switching = early.to_save_text(somewhere);
            var lines = good.Split('\n').ToList();
            var reunited = kitchen_after_film();
            reunited.use("bowl_used", true);
            reunited.use("door_opened", true);
            reunited.switch_character();
            reunited.sniff(500, 300);
            reunited.use("sock_found", true);
            reunited.hold_dog(true);
            reunited.pass_time(ChapterRun.embrace_seconds + 0.1);
            string mid_reunion = reunited.to_save_text(somewhere).Replace("event reunion_embrace\n", "").Replace("event reunion_complete\n", "");
            string[] bad_texts =
            {
                null,
                "",
                "not a save\n",
                good.Replace("bodhi_chapter 1", "bodhi_chapter 2"),            // Another version.
                good.Replace("chapter test_kitchen", "chapter test_cellar"),   // Another chapter.
                good + "event made_up\n",                                     // Unknown event.
                good + "event bowl_used\n",                                   // Repeated event.
                good + "event garden_reached\n",                              // Saved after the end.
                good.Replace("event level_started\n", ""),                    // Doesn't start at the start.
                "bodhi_chapter 1\nchapter test_kitchen\ncontrolled human\nbond 0.05 0.3\ncare 0.3 0 2\nplace human 1 1\nplace dog 1 1\nevent level_started\n",  // Mid-film.
                before_switching.Replace("controlled human", "controlled dog"),  // Him, before the story lets you.
                good.Replace("controlled human", "controlled cat"),
                good.Replace("bond ", "bond 1.5 "),                           // Three feelings.
                swap_line(good, "bond ", "bond 1.5 0.3"),                     // Fear above 1.
                swap_line(good, "bond ", "bond NaN 0.3"),
                swap_line(good, "bond ", "bond 0.1 -0.2"),
                swap_line(good, "care ", "care 0.3 0 99"),
                swap_line(good, "care ", "care 2 0 2"),
                swap_line(good, "care ", "care 0.3 1.5 2"),                   // Muddier than possible.
                swap_line(good, "care ", "cure 0.3 0 2"),
                swap_line(good, "place human", "place human NaN 3"),
                swap_line(good, "place dog", "place dog 1 Infinity"),
                swap_line(good, "place dog", "place dog 1"),
                swap_line(good, "place human", "place human 2000000 3"),      // Far outside any chapter.
                mid_reunion,                                                  // Saved while he runs to her.
                good + "revealed bowl_used\n",                                // Not a hidden thing.
                good + "revealed sock\nrevealed sock\n",                      // Twice.
                good + "teleport 1\n",                                        // Unknown line.
                good.Replace("\n", "\r\n"),
                good.TrimEnd('\n'),                                           // Cut short.
                string.Join("\n", lines.Where(line => !line.StartsWith("care "))),  // A line missing.
                new string('x', ChapterRun.maximum_save_characters + 1),
            };
            foreach (var bad in bad_texts)
            {
                Assert.That(resume_kitchen(bad), Is.Null, bad == null ? "null" : bad.Substring(0, Math.Min(60, bad.Length)));
            }
            Assert.That(resume_kitchen(good), Is.Not.Null, "the undamaged save still loads");
            Assert.That(ChapterRun.resume(null, good, out _), Is.Null);
        }

        // Swaps the line starting with `start` for another one.
        static string swap_line(string text, string start, string replacement)
        {
            return string.Join("\n", text.Split('\n').Select(line => line.StartsWith(start, StringComparison.Ordinal) ? replacement : line));
        }
    }
}
