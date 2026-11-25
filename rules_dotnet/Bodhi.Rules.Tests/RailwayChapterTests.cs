// Purpose: plays the station chapter ("The Other Side") from start to finish with no graphics,
// then tries to break it. Ported from core/tests/test_railway_chapter.cpp: the same events, the
// same times and the same expected results, so the C# rules match the tested C++ ones.
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class RailwayChapterTests
    {
        // Plays the chapter up to the start of the final obstacle, as the level design intends.
        static void play_until_final_obstacle(RailwayChapter chapter)
        {
            chapter.report_world_event("walkway_collapsed", 60.0);
            chapter.report_world_event("separation_scene_finished", 75.0);
            chapter.report_world_event("maintenance_power_restored", 140.0);
            chapter.report_world_event("glove_found", 200.0);
            chapter.report_world_event("dog_barked_from_window", 230.0);
            chapter.report_world_event("pair_met_at_fence", 300.0);
            chapter.report_world_event("fence_touch_finished", 345.0);
        }

        // The full designed path, checking story, control and Bodhi's feelings at each stage.
        [Test]
        public void the_designed_path_plays_from_start_to_finish()
        {
            var chapter = new RailwayChapter();
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.together));
            Assert.That(chapter.controlled_character, Is.EqualTo(Character.human));

            chapter.report_world_event("walkway_collapsed", 60.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.separation));
            Assert.That(chapter.bond.fear, Is.GreaterThan(0.5));

            chapter.report_world_event("separation_scene_finished", 75.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.human_section));

            // Her action on one route opens Bodhi's route and hands control to him.
            chapter.report_world_event("maintenance_power_restored", 140.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.dog_section));
            Assert.That(chapter.controlled_character, Is.EqualTo(Character.dog));

            // His bark shows her a way through and hands control back.
            chapter.report_world_event("dog_barked_from_window", 230.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.human_section));
            Assert.That(chapter.controlled_character, Is.EqualTo(Character.human));

            chapter.report_world_event("pair_met_at_fence", 300.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.near_contact));
            chapter.report_world_event("fence_touch_finished", 345.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.final_obstacle));

            // Playtest measure: how long the player lingered at the fence.
            Assert.That(chapter.seconds_spent_in(ChapterBeat.near_contact, 345.0), Is.EqualTo(45.0));

            // Both routes must act: the lever as her, switch, clear the debris as him.
            chapter.report_world_event("jammed_gate_lever_pulled", 380.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.final_obstacle));
            Assert.That(chapter.request_character_switch(381.0), Is.True);
            Assert.That(chapter.controlled_character, Is.EqualTo(Character.dog));
            chapter.report_world_event("debris_cleared", 420.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.reunion));
            Assert.That(chapter.controlled_character, Is.EqualTo(Character.human));

            chapter.report_world_event("reunion_contact_made", 430.0);
            Assert.That(chapter.bond.closeness_need, Is.GreaterThanOrEqualTo(0.9));
            chapter.report_world_event("reunion_scene_finished", 470.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.aftermath));
            Assert.That(chapter.refused_input_count, Is.EqualTo(0));
        }

        // Break it: an event that belongs later in the story does nothing early on.
        [Test]
        public void break_it_a_late_event_does_nothing_early()
        {
            var chapter = new RailwayChapter();
            chapter.report_world_event("reunion_scene_finished", 5.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.together));
        }

        // Break it: announcing the final obstacle's result directly is refused.
        [Test]
        public void break_it_the_final_result_cannot_be_announced_from_outside()
        {
            var chapter = new RailwayChapter();
            play_until_final_obstacle(chapter);
            Assert.That(chapter.report_world_event("shared_route_opened", 350.0), Is.False);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.final_obstacle));
        }

        // Break it: one route's action alone is not enough, even repeated.
        [Test]
        public void break_it_one_route_alone_is_not_enough()
        {
            var chapter = new RailwayChapter();
            play_until_final_obstacle(chapter);
            chapter.report_world_event("jammed_gate_lever_pulled", 360.0);
            chapter.report_world_event("jammed_gate_lever_pulled", 361.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.final_obstacle));
        }

        // Break it: the final action done before the final obstacle doesn't count later.
        [Test]
        public void break_it_an_early_final_action_does_not_count()
        {
            var chapter = new RailwayChapter();
            chapter.report_world_event("debris_cleared", 10.0);
            play_until_final_obstacle(chapter);
            chapter.report_world_event("jammed_gate_lever_pulled", 360.0);
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.final_obstacle));
        }

        // Break it: switching characters outside the final obstacle is refused.
        [Test]
        public void break_it_switching_is_refused_outside_the_final_obstacle()
        {
            var chapter = new RailwayChapter();
            Assert.That(chapter.request_character_switch(1.0), Is.False);
            Assert.That(chapter.controlled_character, Is.EqualTo(Character.human));
        }

        // Break it: bad names and bad times are refused and counted, not acted on, and a huge
        // name is cut before it reaches the log.
        [Test]
        public void break_it_bad_names_and_times_are_refused_and_counted()
        {
            var chapter = new RailwayChapter();
            chapter.report_world_event("walkway_collapsed", 60.0);
            Assert.That(chapter.report_world_event("", 61.0), Is.False);
            Assert.That(chapter.report_world_event("Walkway_Collapsed", 61.0), Is.False);
            Assert.That(chapter.report_world_event("x\",\"kind\":\"admin", 61.0), Is.False);
            Assert.That(chapter.report_world_event(new string('a', 100000), 61.0), Is.False);
            Assert.That(chapter.report_world_event("separation_scene_finished", 30.0), Is.False);  // Time went backwards.
            Assert.That(chapter.current_beat, Is.EqualTo(ChapterBeat.separation));
            Assert.That(chapter.refused_input_count, Is.EqualTo(5));
            foreach (var entry in chapter.telemetry.entries)
            {
                Assert.That(entry.detail.Length, Is.LessThanOrEqualTo(100));
            }
        }

        // Break it: a missing name (null) is refused and counted, never crashes.
        [Test]
        public void break_it_a_missing_name_is_refused()
        {
            var chapter = new RailwayChapter();
            Assert.That(chapter.report_world_event(null, 1.0), Is.False);
            Assert.That(chapter.refused_input_count, Is.EqualTo(1));
        }

        // Break it: a beat value that isn't a real beat can't read outside the list.
        [Test]
        public void break_it_a_forced_bad_beat_reads_zero()
        {
            var chapter = new RailwayChapter();
            Assert.That(chapter.seconds_spent_in((ChapterBeat)99, 10.0), Is.EqualTo(0.0));
            Assert.That(chapter.seconds_spent_in((ChapterBeat)(-1), 10.0), Is.EqualTo(0.0));
        }
    }
}
