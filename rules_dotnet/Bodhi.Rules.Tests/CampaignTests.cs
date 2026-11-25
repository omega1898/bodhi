// Purpose: proves the campaign: the twelve chapters played in order, with Bodhi's care, his
// keepsakes and his story memory carried from one chapter to the next, and saved between
// chapters. Also checks spec R3.1: every chapter leaves a memory that a later chapter recalls.
using System.Linq;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class CampaignTests
    {
        // Plays the campaign's current chapter to the end with the robot and finishes it.
        static ChapterRun play_current(Campaign campaign)
        {
            var run = campaign.begin_chapter();
            ChapterRobot.finish(run, skip_films: true);
            Assert.That(campaign.finish_chapter(run), Is.True, run.chapter.id);
            return run;
        }

        // A new campaign starts at Coming Home with nothing found and nothing remembered.
        [Test]
        public void a_new_campaign_starts_at_the_beginning()
        {
            var campaign = new Campaign();
            Assert.That(campaign.current_chapter.id, Is.EqualTo("coming_home"));
            Assert.That(campaign.chapter_number, Is.EqualTo(1));
            Assert.That(campaign.is_finished, Is.False);
            Assert.That(campaign.memory.count, Is.EqualTo(0));
            Assert.That(campaign.keepsakes_found, Is.EqualTo(0));
            Assert.That(campaign.keepsake_total, Is.EqualTo(37));
        }

        // Playing all twelve chapters in order finishes the campaign, remembering every moment.
        [Test]
        public void the_whole_year_can_be_played()
        {
            var campaign = new Campaign();
            for (int chapter = 0; chapter < 12; chapter++)
            {
                play_current(campaign);
            }
            Assert.That(campaign.is_finished, Is.True);
            Assert.That(campaign.current_chapter, Is.Null);
            Assert.That(campaign.begin_chapter(), Is.Null);
            foreach (var moment in ChapterMemories.all)
            {
                Assert.That(campaign.memory.knows(moment.fact), Is.True, moment.fact);
            }
            Assert.That(campaign.keepsakes_found, Is.EqualTo(37));  // Every keepsake: 4 in the first chapter, 3 in the other 11.
        }

        // Care carries over: the mud and the meals in the bag at the end of one chapter are there
        // at the start of the next (unless the next chapter sets its own).
        [Test]
        public void care_carries_to_the_next_chapter()
        {
            var campaign = new Campaign();
            var coming_home = play_current(campaign);
            var storm = campaign.begin_chapter();
            Assert.That(storm.chapter.id, Is.EqualTo("the_storm"));
            Assert.That(storm.care.food, Is.EqualTo(coming_home.care.food));
            Assert.That(storm.care.dirt, Is.EqualTo(coming_home.care.dirt));
            Assert.That(storm.care.hunger, Is.EqualTo(coming_home.care.hunger));
        }

        // A chapter only counts once it's complete, and only the run that was begun.
        [Test]
        public void only_a_finished_run_of_this_chapter_counts()
        {
            var campaign = new Campaign();
            var run = campaign.begin_chapter();
            Assert.That(campaign.finish_chapter(run), Is.False);  // Not complete yet.
            var stranger = new ChapterRun(StoryChapters.the_storm());
            ChapterRobot.finish(stranger, skip_films: true);
            Assert.That(campaign.finish_chapter(stranger), Is.False);  // Not the chapter being played.
            Assert.That(campaign.finish_chapter(null), Is.False);
            Assert.That(campaign.current_chapter.id, Is.EqualTo("coming_home"));
            ChapterRobot.finish(run, skip_films: true);
            Assert.That(campaign.finish_chapter(run), Is.True);
            Assert.That(campaign.finish_chapter(run), Is.False);  // Counted once.
            Assert.That(campaign.current_chapter.id, Is.EqualTo("the_storm"));
        }

        // Spec R3.1: every chapter but the last leaves a moment a later chapter recalls, and each
        // moment's event really happens when the chapter is played.
        [Test]
        public void every_chapter_leaves_a_memory_a_later_chapter_recalls()
        {
            var ids = StoryChapters.all().Select(chapter => chapter.id).ToList();
            for (int index = 0; index < ids.Count - 1; index++)
            {
                Assert.That(ChapterMemories.all.Any(moment => moment.chapter_id == ids[index]), Is.True, ids[index] + " leaves no memory");
            }
            foreach (var moment in ChapterMemories.all)
            {
                int made = ids.IndexOf(moment.chapter_id);
                int recalled = ids.IndexOf(moment.recalled_in);
                Assert.That(made, Is.GreaterThanOrEqualTo(0), moment.fact);
                Assert.That(recalled, Is.GreaterThan(made), moment.fact + " must be recalled in a later chapter");
                var run = new ChapterRun(StoryChapters.all()[made]);
                ChapterRobot.finish(run, skip_films: false);
                Assert.That(run.has_happened(moment.event_name), Is.True, moment.fact + ": " + moment.event_name + " never happens");
            }
        }

        // After the river takes him in The Crossing, Bodhi is wary of water for the rest of the year.
        [Test]
        public void after_the_river_he_is_wary_of_water()
        {
            var campaign = new Campaign();
            while (campaign.current_chapter.id != "crossing")
            {
                Assert.That(campaign.companion(new BondState()).approaches_water_warily, Is.False, campaign.current_chapter.id);
                play_current(campaign);
            }
            play_current(campaign);
            Assert.That(campaign.companion(new BondState()).approaches_water_warily, Is.True);
        }

        // Between chapters the campaign saves as text and loads back the same.
        [Test]
        public void the_campaign_survives_a_save()
        {
            var campaign = new Campaign();
            play_current(campaign);
            play_current(campaign);
            string text = campaign.to_text();
            var loaded = new Campaign();
            Assert.That(loaded.load_text(text), Is.True);
            Assert.That(loaded.to_text(), Is.EqualTo(text));
            Assert.That(loaded.current_chapter.id, Is.EqualTo("last_morning"));
            Assert.That(loaded.keepsakes_found, Is.EqualTo(campaign.keepsakes_found));
            Assert.That(loaded.memory.knows("met_biscuit"), Is.True);
            Assert.That(loaded.care.food, Is.EqualTo(campaign.care.food));
            Assert.That(loaded.care.dirt, Is.EqualTo(campaign.care.dirt));
        }

        // Break it: damaged or hostile save text is refused whole and changes nothing.
        [Test]
        public void a_damaged_save_is_refused_whole()
        {
            var good = new Campaign();
            play_current(good);
            string text = good.to_text();
            string[] bad_texts =
            {
                null,
                "",
                "not a save",
                text.Replace("bodhi_campaign 1", "bodhi_campaign 9"),   // A newer format.
                text.Replace("chapter 2", "chapter 99"),                // No such chapter.
                text.Replace("chapter 2", "chapter -1"),
                text.Replace("chapter 2", "chapter two"),
                text + "fact Bad Name!\n",                              // Breaks the fact name rule.
                text + "keepsake nowhere:thing\n",                      // No such keepsake.
                text + "teleport 1\n",                                  // Unknown line.
                text + text,                                            // Everything twice.
                text.Replace("\n", "\r\n"),
                text.TrimEnd('\n'),                                     // Cut short.
                new string('x', Campaign.maximum_text_length + 1),
                text.Replace("care ", "care NaN "),
                replace_care(text, "care 1.5 0 2"),                     // Hunger above 1.
                replace_care(text, "care NaN 0 2"),
                replace_care(text, "care 0.3 -0.1 2"),                  // Dirt below 0.
                replace_care(text, "care 0.3 0 99"),                    // More meals than the bag holds.
            };
            foreach (var bad in bad_texts)
            {
                var campaign = new Campaign();
                play_current(campaign);
                string before = campaign.to_text();
                Assert.That(campaign.load_text(bad), Is.False, bad == null ? "null" : bad.Substring(0, System.Math.Min(40, bad.Length)));
                Assert.That(campaign.to_text(), Is.EqualTo(before));
            }
        }

        // Swaps the care line of a save for another one.
        static string replace_care(string text, string care_line)
        {
            return string.Join("\n", text.Split('\n').Select(line => line.StartsWith("care ") ? care_line : line));
        }

        // Break it: a finished campaign saves and loads too, and has nothing left to begin.
        [Test]
        public void a_finished_campaign_saves()
        {
            var campaign = new Campaign();
            while (!campaign.is_finished)
            {
                play_current(campaign);
            }
            var loaded = new Campaign();
            Assert.That(loaded.load_text(campaign.to_text()), Is.True);
            Assert.That(loaded.is_finished, Is.True);
            Assert.That(loaded.begin_chapter(), Is.Null);
        }
    }
}
