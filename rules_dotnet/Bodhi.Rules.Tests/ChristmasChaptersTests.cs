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

        // Chapter Two's choice: he can walk away toward her light, but the old dog can't follow,
        // and he comes back. Only lying down beside Biscuit moves the night on to dawn.
        [Test]
        public void chapter_two_lets_him_try_to_leave_but_only_staying_brings_dawn()
        {
            var run = new ChapterRun(ChristmasChapters.lamp_in_window());
            ChapterRobot.finish(run, skip_films: false, stop_when: playing => playing.has_happened("her_light_seen"));
            Assert.That(run.controlled_character, Is.EqualTo(Character.dog));
            Assert.That(run.objective, Is.EqualTo("Go to her light, or stay with Biscuit."));
            Assert.That(run.report_zone("tried_to_leave", false, true), Is.True);
            for (int step = 0; step < 240; step++)
            {
                run.pass_time(0.25);
            }
            Assert.That(run.has_happened("dawn"), Is.False, "trying to leave changes nothing");
            Assert.That(run.use("stayed", true), Is.True);
            for (int step = 0; step < 40 && !run.has_happened("dawn"); step++)
            {
                run.pass_time(0.25);
            }
            Assert.That(run.has_happened("dawn"), Is.True);
            Assert.That(run.controlled_character, Is.EqualTo(Character.human), "dawn is hers");
        }

        // Break it: once he has stayed, walking away again does nothing; the zone is switched off.
        [Test]
        public void after_staying_he_cannot_try_to_leave()
        {
            var run = new ChapterRun(ChristmasChapters.lamp_in_window());
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("her_light_seen"));
            Assert.That(run.use("stayed", true), Is.True);
            Assert.That(run.report_zone("tried_to_leave", false, true), Is.False);
        }

        // Chapter Three: one place, an hour apart. Everything Em reads at Kell Bridge is a trace
        // the player left as Bodhi, so she can only read it after he has made it, and the
        // chapter plays as him first, then as her.
        [Test]
        public void chapter_three_is_his_morning_then_her_hour_later()
        {
            var run = ChapterRobot.play(ChristmasChapters.kell_bridge(), skip_films: false);
            var order = run.event_order.ToList();
            Assert.That(order.IndexOf("sausage_found"), Is.LessThan(order.IndexOf("bowl_read")), "the bowl is licked before she sees it");
            Assert.That(order.IndexOf("hid_in_woodshed"), Is.LessThan(order.IndexOf("curl_found")), "the curl is on the nail before she finds it");
            Assert.That(order.IndexOf("followed_biscuit"), Is.LessThan(order.IndexOf("em_arrived")));
            Assert.That(order.IndexOf("em_arrived"), Is.LessThan(order.IndexOf("prints_read")));
        }

        // Break it: Em can't ask Nell before she has read all three traces herself.
        [Test]
        public void em_reads_the_traces_before_asking_nell()
        {
            var run = new ChapterRun(ChristmasChapters.kell_bridge());
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("em_arrived"));
            Assert.That(run.use("talked_to_nell", true), Is.False);
            Assert.That(run.use("prints_read", true), Is.True);
            Assert.That(run.use("bowl_read", true), Is.True);
            Assert.That(run.use("talked_to_nell", true), Is.False, "two of three");
            Assert.That(run.use("curl_found", true), Is.True);
            Assert.That(run.use("talked_to_nell", true), Is.True);
        }

        // Chapter Four's near miss: her whistle reaches the square while he is one row of huts
        // away, so the line he half hears comes only after she has whistled, and the dogs are
        // already settled in the grotto when she puts up the poster that faces it.
        [Test]
        public void chapter_four_misses_by_one_row_of_huts()
        {
            var run = ChapterRobot.play(ChristmasChapters.strand(), skip_films: false);
            var order = run.event_order.ToList();
            Assert.That(order.IndexOf("human_called_during_human_section"), Is.LessThan(order.IndexOf("under_the_trumpets")));
            Assert.That(order.IndexOf("into_the_grotto"), Is.LessThan(order.IndexOf("last_poster")));
            Assert.That(order.IndexOf("red_door"), Is.LessThan(order.IndexOf("kofi_done")), "the red door first: then he leads");
        }

        // Break it: a whistle on the way into the market, before the tree is lit, doesn't start
        // the near miss early; it waits for six o'clock.
        [Test]
        public void chapter_four_near_miss_waits_for_six_oclock()
        {
            var run = new ChapterRun(ChristmasChapters.strand());
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("em_bus"));
            Assert.That(run.call(), Is.True);
            Assert.That(run.has_happened("under_the_trumpets"), Is.False);
            ChapterRobot.finish(run, skip_films: false, stop_when: playing => playing.has_happened("whistle_time"));
            Assert.That(run.cutscene_playing, Is.True, "the earlier whistle counts once six o'clock comes");
        }

        // Chapter Five's ending: back at the recess, her whistle into the forest is answered by
        // one bark, and that ends the chapter.
        [Test]
        public void chapter_five_ends_on_one_bark()
        {
            var run = new ChapterRun(ChristmasChapters.lantern_train());
            ChapterRobot.finish(run, skip_films: false, stop_when: playing => playing.has_happened("recess_reached"));
            Assert.That(run.objective, Is.EqualTo("Whistle into the forest."));
            Assert.That(run.call(), Is.True);
            ChapterRobot.finish(run, skip_films: false);
            Assert.That(run.is_complete, Is.True);
            Assert.That(run.event_order.ToList().IndexOf("one_bark"), Is.GreaterThan(run.event_order.ToList().IndexOf("recess_reached")));
        }

        // Break it: a whistle on the platform at Strand doesn't count as the whistle into the
        // forest. Reaching the recess still waits for her to whistle again.
        [Test]
        public void a_whistle_at_the_station_does_not_answer_the_forest()
        {
            var run = new ChapterRun(ChristmasChapters.lantern_train());
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("kofis_mum_rang"));
            Assert.That(run.call(), Is.True);
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("ran_back"));
            Assert.That(run.report_zone("recess_reached", true, false), Is.True);
            for (int step = 0; step < 120; step++)
            {
                run.pass_time(0.25);
            }
            Assert.That(run.has_happened("one_bark"), Is.False, "no bark without a whistle at the recess");
        }

        // Chapter Six is the midpoint: the first reunion of the story, after which he follows
        // her again, and the collar goes back on only once they are together.
        [Test]
        public void chapter_six_reunites_them_and_the_collar_goes_back_on()
        {
            var run = ChapterRobot.play(ChristmasChapters.reindeer_hill(), skip_films: false);
            var order = run.event_order.ToList();
            Assert.That(ChristmasChapters.reindeer_hill().rules.Count(rule => rule.reunion), Is.EqualTo(1));
            Assert.That(order.IndexOf("reunion_complete"), Is.GreaterThan(order.IndexOf("he_heard")));
            Assert.That(order.IndexOf("puppy_book_line"), Is.GreaterThan(order.IndexOf("reunion_complete")), "the collar goes back on as the reunion ends");
            Assert.That(run.dog_follows, Is.True, "together again");
        }

        // Break it: her whistle before the gate is open doesn't bring him; the reunion waits for
        // both acts, his gate rope and her gate.
        [Test]
        public void chapter_six_reunion_waits_for_the_gate()
        {
            var run = new ChapterRun(ChristmasChapters.reindeer_hill());
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("saw_yard_light"));
            run.pass_time(2);  // Past the rest between two whistles, in case the robot has just whistled.
            run.call();
            Assert.That(run.has_happened("human_called_during_near_contact"), Is.True, "she has whistled");
            for (int step = 0; step < 40; step++)
            {
                run.pass_time(0.25);
            }
            Assert.That(run.reunion_stage, Is.EqualTo(ReunionStage.none), "the gate is still shut");
            Assert.That(run.use("field_gate_opened", true), Is.True);
            for (int step = 0; step < 20; step++)
            {
                run.pass_time(0.25);
            }
            Assert.That(run.reunion_stage, Is.EqualTo(ReunionStage.approach), "the earlier whistle counts once the gate is open");
        }

        // Chapter Seven's morning is care before plot: the phone can't be answered until she
        // has towelled him, picked the ice from his paws, fed him and cuddled him.
        [Test]
        public void chapter_seven_looks_after_him_before_the_phone()
        {
            var run = new ChapterRun(ChristmasChapters.tams_sled());
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("morning"));
            Assert.That(run.use("phone_answered", true), Is.False);
            Assert.That(run.use("towelled", true), Is.True);
            Assert.That(run.use("paws_brushed", true), Is.True);
            Assert.That(run.use("breakfast", true), Is.True);
            Assert.That(run.use("phone_answered", true), Is.False, "not without a cuddle");
            Assert.That(run.pet(), Is.True);
            Assert.That(run.use("phone_answered", true), Is.True);
            Assert.That(run.care.dirt, Is.EqualTo(0), "the towel took five days of hill out of his curls");
        }

        // Chapter Eight: the black ice holds him and not her. Its edge stops Em, never Bodhi,
        // and they come back together only when his rope and her crank have both been done.
        [Test]
        public void chapter_eight_black_ice_holds_him_not_her()
        {
            var edge = Bodhi.Game.ChristmasStages.blackwater().find_solid("black_ice_edge");
            Assert.That(Bodhi.Game.StageLook.solid_blocks(edge, Character.human), Is.True);
            Assert.That(Bodhi.Game.StageLook.solid_blocks(edge, Character.dog), Is.False);
            var run = new ChapterRun(ChristmasChapters.blackwater());
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("rope_held"));
            Assert.That(run.reunion_stage, Is.EqualTo(ReunionStage.none), "the rope alone doesn't bring them in");
            ChapterRobot.finish(run, skip_films: false);
            Assert.That(run.is_complete, Is.True);
            var order = run.event_order.ToList();
            Assert.That(order.IndexOf("reunion_embrace"), Is.GreaterThan(order.IndexOf("crank_turned")));
        }

        // Chapter Nine: Biscuit's recovery is the morning after both of them kept him alive.
        // Bodhi lay along him and dug out the logs; Em kept the fire; then the polite bark.
        [Test]
        public void chapter_nine_biscuit_recovers_only_after_the_long_night()
        {
            var run = ChapterRobot.play(ChristmasChapters.bothy(), skip_films: false);
            var order = run.event_order.ToList();
            Assert.That(order.IndexOf("morning"), Is.GreaterThan(order.IndexOf("lay_along_biscuit")));
            Assert.That(order.IndexOf("morning"), Is.GreaterThan(order.IndexOf("logs_carried")));
            Assert.That(order.IndexOf("morning"), Is.GreaterThan(order.IndexOf("stick_brought")));
            Assert.That(order.IndexOf("tams_bothy"), Is.GreaterThan(order.IndexOf("book_1974")), "the book tells her whose bothy it is");
        }

        // Break it: the woodpile is outside, so Bodhi can't dig the logs before Em has
        // shouldered the snow-jammed door open.
        [Test]
        public void chapter_nine_logs_wait_for_the_door()
        {
            var run = new ChapterRun(ChristmasChapters.bothy());
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("fire_low"));
            Assert.That(run.switch_character(), Is.True);
            run.sniff(2000, 520);
            Assert.That(run.use("logs_dug", true), Is.False, "the door is still shut");
            Assert.That(run.switch_character(), Is.True);
            Assert.That(run.use("door_shouldered", true), Is.True);
            Assert.That(run.switch_character(), Is.True);
            Assert.That(run.use("logs_dug", true), Is.True);
        }

        // Chapter Ten: he asks before he goes. The gully edge holds Bodhi until she says go, and
        // it only opens after he has looked at her; in Chapter One he lunged without a look.
        [Test]
        public void chapter_ten_he_looks_at_her_before_he_goes()
        {
            var edge = Bodhi.Game.ChristmasStages.windy_gap().find_solid("gully_edge");
            Assert.That(Bodhi.Game.StageLook.solid_blocks(edge, Character.dog), Is.True);
            Assert.That(Bodhi.Game.StageLook.solid_blocks(edge, Character.human), Is.False);
            var run = new ChapterRun(ChristmasChapters.windy_gap());
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("the_look"));
            Assert.That(run.controlled_character, Is.EqualTo(Character.dog));
            Assert.That(run.solid_is_removed("gully_edge"), Is.False, "not until she says go");
            Assert.That(run.report_zone("reached_sled", false, true), Is.False, "he can't reach the sled yet");
            Assert.That(run.use("looked_at_her", true), Is.True);
            for (int step = 0; step < 20; step++)
            {
                run.pass_time(0.25);
            }
            Assert.That(run.has_happened("she_said_go"), Is.True);
            Assert.That(run.solid_is_removed("gully_edge"), Is.True);
            Assert.That(run.report_zone("reached_sled", false, true), Is.True);
        }

        // Chapter Eleven reverses the roles: once the lantern is out the player is Bodhi, and Em
        // follows him on the harness, from the old path to the footbridge.
        [Test]
        public void chapter_eleven_he_leads_her_home()
        {
            var run = new ChapterRun(ChristmasChapters.christmas_eve());
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("he_leads"));
            Assert.That(run.controlled_character, Is.EqualTo(Character.dog), "the player is Bodhi");
            Assert.That(run.dog_follows, Is.True, "and Em follows on the harness");
            Assert.That(run.switching_allowed, Is.False, "all the way to the footbridge");
            ChapterRobot.finish(run, skip_films: false);
            Assert.That(run.is_complete, Is.True);
            var order = run.event_order.ToList();
            Assert.That(order.IndexOf("mercy_heard"), Is.LessThan(order.IndexOf("village_lights")), "the bell before the lights");
            Assert.That(order.IndexOf("bell_first"), Is.GreaterThan(order.IndexOf("crossed")));
        }

        // Break it: neither of them crosses the footbridge alone; the ice stays until his rope
        // and her handrail are both done.
        [Test]
        public void chapter_eleven_footbridge_needs_both()
        {
            var run = new ChapterRun(ChristmasChapters.christmas_eve());
            ChapterRobot.finish(run, skip_films: true, stop_when: playing => playing.has_happened("footbridge_reached"));
            Assert.That(run.controlled_character, Is.EqualTo(Character.dog), "he is still leading");
            Assert.That(run.use("rope_across", true), Is.True);
            Assert.That(run.solid_is_removed("footbridge_ice"), Is.False, "his rope alone");
            Assert.That(run.switch_character(), Is.True);
            Assert.That(run.use("handrail_cleared", true), Is.True);
            Assert.That(run.solid_is_removed("footbridge_ice"), Is.True);
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
