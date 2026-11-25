// Purpose: proves the Christmas story's chapters (rules/Runtime/Christmas/) play to the end on
// the one story engine, keep the script's rules for words (docs/christmas_script.md), and that
// Chapter One ends the way the story needs: on the player's own whistle, which nobody answers.
using System;
using System.Collections.Generic;
using System.Linq;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class ChristmasChaptersTests
    {
        const int longest_caption = 90;  // One line on a phone (the script's first rule).

        // Events the engine announces or the ball game reports, which no rule needs to make.
        static readonly string[] made_by_play =
        {
            "level_started", "cutscene_skipped", "reunion_embrace", "reunion_complete", "dog_petted",
            "dog_leaned_in", "knelt_by_dog", "ball_thrown", "ball_fetched", "dog_sniffed",
        };

        public static IEnumerable<string> chapter_ids() => ChristmasChapters.all().Select(chapter => chapter.id);

        // Finds a chapter by id.
        static ChapterScript chapter(string id) => ChristmasChapters.all().Single(script => script.id == id);

        // Every caption and prompt a player reads in a chapter, with the names filled in. Keepsake
        // texts are left out: they are read in the memory box, where a longer note fits.
        static List<string> captions(ChapterScript script)
        {
            var texts = new List<string> { script.title, script.eyebrow, script.place };
            texts.AddRange(script.aftermath);
            texts.AddRange(script.things.Select(thing => thing.label));
            foreach (var rule in script.rules)
            {
                texts.AddRange(rule.lines);
                if (rule.objective != null)
                {
                    texts.Add(rule.objective);
                }
            }
            return texts.Select(text => StoryText.personalize(text, "Bodhi")).ToList();
        }

        // The story begins with First Snow, and no two chapters share an id.
        [Test]
        public void the_story_begins_with_first_snow()
        {
            Assert.That(chapter_ids().First(), Is.EqualTo("first_snow"));
            Assert.That(chapter_ids().Distinct().Count(), Is.EqualTo(chapter_ids().Count()));
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

        // Each chapter has three keepsakes (the bible, section 4), and all of them can be found.
        [TestCaseSource(nameof(chapter_ids))]
        public void every_keepsake_can_be_found(string id)
        {
            var script = chapter(id);
            var keepsakes = script.things.Where(thing => !string.IsNullOrEmpty(thing.keepsake)).Select(thing => thing.keepsake).ToList();
            Assert.That(keepsakes.Count, Is.EqualTo(3), id);
            var run = ChapterRobot.play(script, skip_films: false);
            Assert.That(run.found_keepsakes, Is.EquivalentTo(keepsakes), id);
        }

        // Every event a rule waits for can be made by something, or that rule could never run.
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

        // The script's first rule: every caption fits on one line of a phone.
        [TestCaseSource(nameof(chapter_ids))]
        public void every_caption_fits_on_one_line(string id)
        {
            foreach (var text in captions(chapter(id)))
            {
                Assert.That(text.Length, Is.LessThanOrEqualTo(longest_caption), id + ": " + text);
            }
        }

        // Break it: a misspelt name slot ({Dog}, a stray brace) would show on screen as code.
        [TestCaseSource(nameof(chapter_ids))]
        public void no_caption_shows_an_unfilled_name_slot(string id)
        {
            var script = chapter(id);
            var texts = captions(script).Concat(script.things.Select(thing => StoryText.personalize(thing.memory_text, "Bodhi")));
            foreach (var text in texts)
            {
                Assert.That(text.Contains("{") || text.Contains("}"), Is.False, id + ": " + text);
            }
        }

        // Bodhi's voice never says "I", "me" or "my" (the voice guide): he thinks in smells and
        // fragments, not about himself.
        [TestCaseSource(nameof(chapter_ids))]
        public void bodhi_never_talks_about_himself(string id)
        {
            var first_person = new[] { "I", "me", "my", "myself", "I'm", "I'd", "I've", "I'll" };
            var his_lines = chapter(id).rules.Where(rule => rule.voice == StoryVoice.dog).SelectMany(rule => rule.lines);
            foreach (var line in his_lines)
            {
                var words = line.Split(new[] { ' ', '.', ',', '!', '?', ':' }, StringSplitOptions.RemoveEmptyEntries);
                Assert.That(words.Intersect(first_person), Is.Empty, id + ": " + line);
            }
        }

        // In this story Bodhi is nine months old in every chapter, the same size throughout.
        [TestCaseSource(nameof(chapter_ids))]
        public void bodhi_is_nine_months_old_throughout(string id)
        {
            var script = chapter(id);
            Assert.That(script.dog_age, Is.EqualTo("nine_months"), id);
            Assert.That(script.dog_size, Is.EqualTo(0.9), id);
        }

        // The hook: once his collar comes back on the lead, the chapter waits for the player to
        // whistle. Time alone never ends it; the whistle does, and he doesn't come.
        [Test]
        public void chapter_one_ends_on_her_whistle_into_the_storm()
        {
            var run = new ChapterRun(ChristmasChapters.first_snow());
            ChapterRobot.finish(run, skip_films: false, stop_when: playing => playing.has_happened("lead_came_back"));
            Assert.That(run.objective, Is.EqualTo("Whistle for {dog}."));
            Assert.That(run.current_beat, Is.EqualTo(ChapterBeat.human_section));
            for (int step = 0; step < 2400; step++)
            {
                run.pass_time(0.25);  // Ten minutes of waiting.
            }
            Assert.That(run.is_complete, Is.False, "waiting never ends it");
            Assert.That(run.call(), Is.True);
            var lines = run.take_lines().Select(line => line.text).ToList();
            Assert.That(lines, Does.Contain("For the first time, he didn't come."));
            for (int step = 0; step < 120 && !run.is_complete; step++)
            {
                run.pass_time(0.25);
            }
            Assert.That(run.is_complete, Is.True);
        }

        // Break it: a player who whistles all chapter long, at every chance, still plays the
        // whole day. Only the whistle after the lunge ends it.
        [Test]
        public void whistling_early_never_ends_chapter_one()
        {
            var run = new ChapterRun(ChristmasChapters.first_snow());
            ChapterRobot.finish(run, skip_films: false, after_each_step: playing => playing.call());
            Assert.That(run.is_complete, Is.True);
            var order = run.event_order.ToList();
            Assert.That(order.IndexOf("human_called_during_human_section"), Is.GreaterThan(order.IndexOf("lead_came_back")));
            Assert.That(order.IndexOf("lead_came_back"), Is.GreaterThan(order.IndexOf("lamp_lit")));
        }

        // Skipping the lunge's film still leaves the player holding the empty lead, asked to
        // whistle: Skip never skips the hook.
        [Test]
        public void skipping_the_lunge_still_asks_for_the_whistle()
        {
            var run = new ChapterRun(ChristmasChapters.first_snow());
            ChapterRobot.finish(run, skip_films: false, stop_when: playing => playing.has_happened("the_lunge"));
            Assert.That(run.cutscene_playing, Is.True);
            Assert.That(run.skip_cutscene(), Is.True);
            Assert.That(run.is_complete, Is.False);
            Assert.That(run.objective, Is.EqualTo("Whistle for {dog}."));
        }

        // Break it: each call gives a fresh copy, so one playthrough can't change another's chapter.
        [Test]
        public void each_copy_of_a_chapter_is_separate()
        {
            var first = ChristmasChapters.first_snow();
            first.rules[1].lines = new[] { "changed" };
            Assert.That(ChristmasChapters.first_snow().rules[1].lines, Is.Not.EqualTo(new[] { "changed" }));
        }
    }
}
