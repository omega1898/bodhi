// Purpose: proves the memory box (MemoryBox), the collectibles screen: every chapter in order
// with its keepsakes, found ones with their memory and missing ones with a hint, totals for the
// year, and replaying a finished chapter without moving the year's progress.
using System.Linq;
using Bodhi.Game;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class MemoryBoxTests
    {
        // Plays the campaign's current chapter to its end with the robot (which finds every
        // keepsake) and records it.
        static void play_current(Campaign campaign)
        {
            var run = campaign.begin_chapter();
            ChapterRobot.finish(run, skip_films: true);
            Assert.That(campaign.finish_chapter(run), Is.True);
        }

        // A new year lists all twelve chapters in order; only the first is open, none finished.
        [Test]
        public void a_new_year_lists_every_chapter()
        {
            var box = MemoryBox.build(new Campaign(), "Bodhi");
            var chapters = StoryChapters.all();
            Assert.That(box.chapters.Length, Is.EqualTo(chapters.Length));
            for (int index = 0; index < chapters.Length; index++)
            {
                Assert.That(box.chapters[index].number, Is.EqualTo(index + 1));
                Assert.That(box.chapters[index].id, Is.EqualTo(chapters[index].id));
                Assert.That(box.chapters[index].title, Is.EqualTo(chapters[index].title));
                Assert.That(box.chapters[index].finished, Is.False);
                Assert.That(box.chapters[index].unlocked, Is.EqualTo(index == 0));
            }
            Assert.That(box.keepsakes_found, Is.EqualTo(0));
            Assert.That(box.keepsake_total, Is.EqualTo(new Campaign().keepsake_total));
            Assert.That(box.chapters.Sum(chapter => chapter.keepsake_total), Is.EqualTo(box.keepsake_total));
        }

        // A missing keepsake shows a hint, never its memory: Bodhi's nose for the ones only he can
        // find, "something left behind" for the rest.
        [Test]
        public void missing_keepsakes_show_a_hint()
        {
            var coming_home = MemoryBox.build(new Campaign(), "Bodhi").chapters[0];
            var slipper = coming_home.keepsakes.Single(keepsake => keepsake.id == "first_slipper");
            Assert.That(slipper.found, Is.False);
            Assert.That(slipper.memory_text, Is.EqualTo(""));
            Assert.That(slipper.hint, Is.EqualTo("Somewhere Bodhi's nose could find it"));
            Assert.That(slipper.name, Is.EqualTo("First slipper"));
            var card = coming_home.keepsakes.Single(keepsake => keepsake.id == "welcome_card");
            Assert.That(card.hint, Is.EqualTo("Something left behind"));
        }

        // After a chapter, its keepsakes show their memories, it counts as finished, and the next
        // chapter opens. Only finished chapters can be replayed.
        [Test]
        public void a_finished_chapter_shows_its_memories()
        {
            var campaign = new Campaign();
            play_current(campaign);
            var box = MemoryBox.build(campaign, "Bodhi");
            var coming_home = box.chapters[0];
            Assert.That(coming_home.finished, Is.True);
            Assert.That(coming_home.can_replay, Is.True);
            Assert.That(coming_home.keepsakes_found, Is.EqualTo(coming_home.keepsake_total));
            var slipper = coming_home.keepsakes.Single(keepsake => keepsake.id == "first_slipper");
            Assert.That(slipper.found, Is.True);
            Assert.That(slipper.memory_text, Does.StartWith("His first crime"));
            Assert.That(slipper.hint, Is.EqualTo(""));
            Assert.That(box.chapters[1].unlocked, Is.True);
            Assert.That(box.chapters[1].finished, Is.False);
            Assert.That(box.chapters[1].can_replay, Is.False);
            Assert.That(box.chapters[2].unlocked, Is.False);
            Assert.That(box.keepsakes_found, Is.EqualTo(campaign.keepsakes_found));
            Assert.That(box.can_replay("coming_home"), Is.True);
            Assert.That(box.can_replay("the_storm"), Is.False);
        }

        // Replaying gives a fresh copy of the chapter and a run of it, and finishing that run
        // doesn't move the year on or change the save.
        [Test]
        public void replaying_leaves_the_year_alone()
        {
            var campaign = new Campaign();
            play_current(campaign);
            string save_before = campaign.to_text();
            var script = MemoryBox.replay_script(campaign, "coming_home");
            Assert.That(script, Is.Not.Null);
            Assert.That(script.id, Is.EqualTo("coming_home"));
            var replay = MemoryBox.begin_replay(campaign, "coming_home");
            Assert.That(replay, Is.Not.Null);
            ChapterRobot.finish(replay, skip_films: true);
            Assert.That(replay.is_complete, Is.True);
            Assert.That(campaign.finish_chapter(replay), Is.False, "a replay isn't the year's chapter");
            Assert.That(campaign.to_text(), Is.EqualTo(save_before));
            Assert.That(campaign.current_chapter.id, Is.EqualTo("the_storm"));
            Assert.That(MemoryBox.keepsakes_new_in_replay(campaign, replay), Is.Empty, "all already found");
        }

        // Keepsakes found in a replay that the year hasn't got yet are listed, for saving.
        [Test]
        public void new_keepsakes_from_a_replay_are_listed()
        {
            var run = new ChapterRun(StoryChapters.coming_home());
            ChapterRobot.finish(run, skip_films: true);
            var found = MemoryBox.keepsakes_new_in_replay(new Campaign(), run);
            Assert.That(found, Is.EquivalentTo(new[] { "first_slipper", "welcome_card", "adoption_receipt", "puppy_list" }));
        }

        // Break it: no campaign, unknown or empty ids, unfinished chapters and a missing run.
        [Test]
        public void broken_requests_are_refused_safely()
        {
            var box = MemoryBox.build(null, null);
            Assert.That(box.chapters.Length, Is.EqualTo(StoryChapters.all().Length));
            Assert.That(box.chapters[0].unlocked, Is.True, "no campaign is a new year");
            Assert.That(box.chapters[0].keepsakes.Single(k => k.id == "first_slipper").hint, Does.Contain("Bodhi"));
            var campaign = new Campaign();
            foreach (string id in new[] { null, "", "no_such_chapter", "coming_home" })
            {
                Assert.That(box.can_replay(id), Is.False, id ?? "null");
                Assert.That(MemoryBox.replay_script(campaign, id), Is.Null, id ?? "null");
                Assert.That(MemoryBox.begin_replay(campaign, id), Is.Null, id ?? "null");
            }
            Assert.That(MemoryBox.replay_script(null, "coming_home"), Is.Null);
            Assert.That(MemoryBox.keepsakes_new_in_replay(campaign, null), Is.Empty);
            Assert.That(MemoryBox.keepsakes_new_in_replay(null, null), Is.Empty);
        }

        // The whole year finished: every chapter can be replayed and every keepsake shows.
        [Test]
        public void a_finished_year_opens_everything()
        {
            var campaign = new Campaign();
            while (!campaign.is_finished)
            {
                play_current(campaign);
            }
            var box = MemoryBox.build(campaign, "Bodhi");
            Assert.That(box.chapters.All(chapter => chapter.finished && chapter.unlocked && chapter.can_replay), Is.True);
            Assert.That(box.keepsakes_found, Is.EqualTo(box.keepsake_total));
            Assert.That(box.chapters.SelectMany(chapter => chapter.keepsakes).All(keepsake => keepsake.memory_text.Length > 0 && !keepsake.memory_text.Contains("{")), Is.True);
        }
    }
}
