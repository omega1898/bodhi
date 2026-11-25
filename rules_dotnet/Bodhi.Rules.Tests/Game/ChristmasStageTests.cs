// Purpose: proves the game plays the Christmas story: each of its chapters has a stage that
// lines up with the story, the stage is found by story and chapter together (both stories have a
// "first_snow"), and a session, its saves and the memory box all stay inside the Christmas story.
using System;
using System.IO;
using System.Linq;
using Bodhi.Game;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class ChristmasStageTests
    {
        string folder;

        // Each test gets its own empty save folder.
        [SetUp]
        public void make_folder()
        {
            folder = Path.Combine(Path.GetTempPath(), "bodhi_christmas_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        // And it is removed afterwards.
        [TearDown]
        public void remove_folder()
        {
            Directory.Delete(folder, true);
        }

        // Every Christmas chapter has a stage, in the same order, and rule 4's staging belongs to rule 4.
        [Test]
        public void every_chapter_has_a_stage_that_lines_up()
        {
            var scripts = ChristmasChapters.all();
            var stages = ChristmasStages.all();
            Assert.That(stages.Select(stage => stage.id), Is.EqualTo(scripts.Select(script => script.id)));
            for (int index = 0; index < scripts.Length; index++)
            {
                var script = scripts[index];
                var stage = stages[index];
                Assert.That(stage.rules.Length, Is.EqualTo(script.rules.Length), script.id + " rules");
                Assert.That(stage.things.Select(look => look.event_name), Is.EqualTo(script.things.Select(thing => thing.event_name)), script.id + " things");
                Assert.That(stage.rules, Has.None.Null, script.id);
            }
        }

        // The same chapter id finds a different stage in each story; an unknown one finds none.
        [Test]
        public void a_stage_is_found_by_story_and_chapter()
        {
            Assert.That(StoryStages.for_chapter(Story.christmas, "first_snow").width, Is.EqualTo(7600));
            Assert.That(StoryStages.for_chapter(Story.flood, "first_snow").width, Is.EqualTo(4200));
            Assert.That(StoryStages.for_chapter(Story.christmas, "the_storm"), Is.Null);
            Assert.That(StoryStages.for_chapter(Story.christmas, null), Is.Null);
        }

        // Break it: a rule that takes away a door the stage doesn't have would do nothing, and the
        // players would be shut in. Every door the story opens is on the stage.
        [Test]
        public void every_door_the_story_opens_is_on_the_stage()
        {
            foreach (var script in ChristmasChapters.all())
            {
                var stage = ChristmasStages.for_chapter(script.id);
                foreach (var solid_id in script.rules.SelectMany(rule => rule.remove.Concat(rule.add)))
                {
                    Assert.That(stage.find_solid(solid_id), Is.Not.Null, script.id + ": " + solid_id);
                }
            }
        }

        // Everything you can use or walk into is inside the stage, so it can be reached.
        [Test]
        public void every_thing_and_zone_is_inside_the_stage()
        {
            foreach (var script in ChristmasChapters.all())
            {
                var stage = ChristmasStages.for_chapter(script.id);
                foreach (var thing in script.things)
                {
                    Assert.That(thing.x, Is.InRange(0, stage.width), script.id + ": " + thing.event_name);
                }
                foreach (var zone in script.zones)
                {
                    Assert.That(zone.x + zone.width, Is.InRange(0, stage.width), script.id + ": " + zone.event_name);
                }
            }
        }

        // Break it: a misspelt sound or mood would play silence. Every one named is one the
        // game can make.
        [Test]
        public void every_sound_and_mood_is_one_the_game_can_play()
        {
            foreach (var stage in ChristmasStages.all())
            {
                var sounds = stage.rules.Select(rule => rule.sound).Concat(stage.things.Select(look => look.sound)).Where(sound => sound != "").Distinct();
                Assert.That(sounds, Is.SubsetOf(SoundSynth.effect_names), stage.id);
                Assert.That(stage.all_moods(), Is.SubsetOf(MusicSynth.moods.ToArray()), stage.id);
            }
        }

        // Chapter Two's yard fence stops Em but not the dogs: they get under the wire, she
        // can only stand at it at dawn and look at the collar.
        [Test]
        public void only_the_dogs_get_under_the_yard_fence()
        {
            var fence = ChristmasStages.lamp_in_window().find_solid("yard_fence");
            Assert.That(StageLook.solid_blocks(fence, Character.human), Is.True);
            Assert.That(StageLook.solid_blocks(fence, Character.dog), Is.False);
        }

        // The cruellest near miss: when Em pulls Biscuit's collar off the wire, the lorry with
        // both dogs in it is still standing in the yard below her. Then it goes.
        [Test]
        public void the_lorry_is_still_there_when_she_finds_the_collar()
        {
            var stage = ChristmasStages.lamp_in_window();
            var lorry = stage.props.Single(prop => prop.kind == "lorry");
            var run = new ChapterRun(ChristmasChapters.lamp_in_window());
            ChapterRobot.finish(run, skip_films: false, stop_when: playing => playing.has_happened("collar_found"));
            Assert.That(StageLook.prop_is_showing(lorry, run), Is.True, "still in the yard");
            ChapterRobot.finish(run, skip_films: false);
            Assert.That(run.is_complete, Is.True);
            Assert.That(StageLook.prop_is_showing(lorry, run), Is.False, "gone down the valley");
        }

        // A Christmas session's new game starts First Snow, and its memory box holds only the
        // Christmas chapters.
        [Test]
        public void a_christmas_session_plays_the_christmas_story()
        {
            var session = new GameSession(new SaveFiles(folder), "Bodhi", Story.christmas);
            session.new_game();
            Assert.That(session.run.chapter.id, Is.EqualTo("first_snow"));
            Assert.That(session.run.chapter.title, Is.EqualTo("First Snow"));
            var box = MemoryBox.build(session.campaign, "Bodhi");
            Assert.That(box.chapters.Select(chapter => chapter.id), Is.EqualTo(ChristmasChapters.all().Select(chapter => chapter.id)));
            Assert.That(box.keepsake_total, Is.EqualTo(3 * ChristmasChapters.all().Length), "three a chapter");
        }

        // Break it: a save from the flood story, part way through its year with its keepsakes,
        // is refused by a Christmas year, which is left as it was.
        [Test]
        public void a_flood_save_does_not_load_into_the_christmas_story()
        {
            var flood = new Campaign(Story.flood);
            var run = flood.begin_chapter();
            ChapterRobot.finish(run, skip_films: true);
            Assert.That(flood.finish_chapter(run), Is.True);
            var christmas = new Campaign(Story.christmas);
            Assert.That(christmas.load_text(flood.to_text()), Is.False);
            Assert.That(christmas.current_chapter.id, Is.EqualTo("first_snow"));
        }
    }
}
