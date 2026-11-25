// Purpose: proves the words the game shows about the bond and the home days (BondFeedback): the
// heart pop, "Bodhi learned Sit!", the level-up line, today's tasks with their progress, the
// streak and the fetch record. Also which buttons in a chapter count as care
// (PromptPicker.bond_care_for), and breaks both with missing and impossible input.
using System.Linq;
using Bodhi.Game;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class BondFeedbackTests
    {
        const int first_day = 739000;

        // A heart pop shows the points gained, and nothing when none were.
        [Test]
        public void the_heart_pop_shows_points_gained()
        {
            var journey = new BondJourney();
            Assert.That(BondFeedback.heart_pop(journey.record_care(BondCareKind.feed, first_day)), Is.EqualTo("♥ +4"));
            journey.record_care(BondCareKind.feed, first_day);
            journey.record_care(BondCareKind.feed, first_day);
            Assert.That(BondFeedback.heart_pop(journey.record_care(BondCareKind.feed, first_day)), Is.EqualTo(""), "today's meals are used up");
        }

        // Going up a level names the new level, and says which trick he's ready to learn.
        [Test]
        public void a_level_up_names_the_level_and_the_trick()
        {
            var journey = new BondJourney();
            journey.record_care(BondCareKind.chapter_finished, first_day);
            var change = journey.record_care(BondCareKind.chapter_finished, first_day);
            Assert.That(change.levelled_up, Is.True);
            var lines = BondFeedback.lines_for(change, "Bodhi");
            Assert.That(lines, Does.Contain("Em and Bodhi are friends now."));
            Assert.That(lines, Does.Contain("Bodhi is ready to learn High five. Teach him at home."));
        }

        // Teaching a trick says what he learned, in his name.
        [Test]
        public void a_trick_taught_is_named()
        {
            var journey = new BondJourney();
            var change = journey.teach_next_trick(first_day);
            Assert.That(BondFeedback.lines_for(change, "Bodhi"), Is.EqualTo(new[] { "Bodhi learned Sit!" }));
            Assert.That(BondFeedback.lines_for(change, "Rex"), Is.EqualTo(new[] { "Rex learned Sit!" }));
        }

        // The level, what comes next, and his tricks, for the home screen.
        [Test]
        public void the_bond_is_described()
        {
            var journey = new BondJourney();
            Assert.That(BondFeedback.next_level_text(journey), Is.EqualTo("Next: Friends"));
            Assert.That(BondFeedback.tricks_text(journey), Is.EqualTo("Ready to learn: Sit"));
            journey.teach_next_trick(first_day);
            Assert.That(BondFeedback.tricks_text(journey), Is.EqualTo("Knows: Sit"));
            for (int day = first_day; journey.level_number < BondJourney.highest_level; day++)
            {
                journey.record_care(BondCareKind.chapter_finished, day);
            }
            Assert.That(BondFeedback.next_level_text(journey), Is.EqualTo("The closest there is"));
            Assert.That(BondFeedback.tricks_text(journey), Is.EqualTo("Knows: Sit. Ready to learn: High five"));
        }

        // Each task reads with its progress, and as done once done; its button says what to do.
        [Test]
        public void tasks_read_with_their_progress()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            int day = first_day;
            while (!HomeDay.choose_tasks(day, false, true).Contains(HomeTask.fetch_five_in_a_row)) day++;
            home.change_day(day, journey);
            home.fetch_caught(journey);
            home.fetch_caught(journey);
            Assert.That(BondFeedback.task_label(home, HomeTask.fetch_five_in_a_row), Is.EqualTo("Play fetch: 5 catches in a row (2/5)"));
            Assert.That(BondFeedback.task_button(home, HomeTask.fetch_five_in_a_row), Is.EqualTo("Play"));
            var other = home.tasks_today.First(each => each != HomeTask.fetch_five_in_a_row);
            home.do_task(other, journey);
            Assert.That(BondFeedback.task_label(home, other), Is.EqualTo(HomeDay.task_text(other) + " - done"));
            Assert.That(BondFeedback.task_button(home, other), Is.EqualTo("Done"));
            for (int catches = 0; catches < HomeDay.fetch_catches_needed; catches++) home.fetch_caught(journey);
            Assert.That(BondFeedback.task_button(home, HomeTask.fetch_five_in_a_row), Is.EqualTo("Play again"), "fetch stays playable");
        }

        // Finishing the last task gives a happy line, and the streak once there is one.
        [Test]
        public void all_three_done_gives_a_happy_line()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            Assert.That(BondFeedback.streak_text(home), Is.EqualTo("Do all three today to start a streak"));
            Assert.That(BondFeedback.today_heading(home, "Bodhi"), Is.EqualTo(""), "no day yet");
            HomeResult last = null;
            for (int day = first_day; day < first_day + 2; day++)
            {
                home.change_day(day, journey);
                foreach (var task in home.tasks_today.ToList())
                {
                    if (task == HomeTask.fetch_five_in_a_row)
                    {
                        for (int catches = 0; catches < HomeDay.fetch_catches_needed; catches++) last = home.fetch_caught(journey);
                    }
                    else
                    {
                        last = home.do_task(task, journey);
                    }
                }
            }
            Assert.That(last.all_done_today, Is.True);
            var lines = BondFeedback.lines_for_home(last, home, "Bodhi");
            Assert.That(lines, Does.Contain("All three done. Bodhi's tail hasn't stopped."));
            Assert.That(lines, Does.Contain("That's 2 days in a row."));
            Assert.That(BondFeedback.streak_text(home), Is.EqualTo("2 days in a row"));
            Assert.That(BondFeedback.today_heading(home, "Bodhi"), Is.EqualTo("All done today. Bodhi will see you tomorrow."));
            home.change_day(first_day + 2, journey);
            Assert.That(BondFeedback.today_heading(home, "Bodhi"), Is.EqualTo("Today with Bodhi"));
        }

        // The fetch record and what a throw did, in a few words.
        [Test]
        public void fetch_is_described()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            home.change_day(first_day, journey);
            home.fetch_caught(journey);
            Assert.That(BondFeedback.fetch_text(home), Is.EqualTo("In a row: 1   Best today: 1   Best ever: 1"));
            Assert.That(BondFeedback.fetch_line(FetchResult.caught), Is.EqualTo("Caught it!"));
            Assert.That(BondFeedback.fetch_line(FetchResult.missed), Is.EqualTo("Missed. Throw again."));
            Assert.That(BondFeedback.fetch_line(FetchResult.nothing), Is.EqualTo(""));
        }

        // Feeding, towelling, petting and leaning in count as care; the rest of the buttons don't.
        [Test]
        public void care_buttons_count_as_care()
        {
            Assert.That(PromptPicker.bond_care_for(PromptAction.feed, out var kind) && kind == BondCareKind.feed, Is.True);
            Assert.That(PromptPicker.bond_care_for(PromptAction.towel, out kind) && kind == BondCareKind.towel_or_bath, Is.True);
            Assert.That(PromptPicker.bond_care_for(PromptAction.pet, out kind) && kind == BondCareKind.pet, Is.True);
            Assert.That(PromptPicker.bond_care_for(PromptAction.lean, out kind) && kind == BondCareKind.pet, Is.True);
            foreach (var action in new[] { PromptAction.none, PromptAction.kneel, PromptAction.hold, PromptAction.throw_ball, PromptAction.use_thing, PromptAction.pick_up_ball, (PromptAction)99 })
            {
                Assert.That(PromptPicker.bond_care_for(action, out _), Is.False, action.ToString());
            }
        }

        // Break it: missing or impossible input gives empty words, never a crash.
        [Test]
        public void missing_input_gives_no_words()
        {
            Assert.That(BondFeedback.heart_pop(null), Is.EqualTo(""));
            Assert.That(BondFeedback.heart_pop(new BondChange()), Is.EqualTo(""));
            Assert.That(BondFeedback.lines_for(null, "Bodhi"), Is.Empty);
            Assert.That(BondFeedback.lines_for(new BondChange { counted = true, newly_taught = "fly" }, "Bodhi"), Is.Empty, "not a trick");
            Assert.That(BondFeedback.lines_for_home(null, new HomeDay(), "Bodhi"), Is.Empty);
            Assert.That(BondFeedback.lines_for_home(new HomeResult(), null, "Bodhi"), Is.Empty);
            Assert.That(BondFeedback.task_label(null, HomeTask.brush), Is.EqualTo(""));
            Assert.That(BondFeedback.task_label(new HomeDay(), (HomeTask)99), Is.EqualTo(""));
            Assert.That(BondFeedback.task_button(new HomeDay(), (HomeTask)99), Is.EqualTo(""));
            Assert.That(BondFeedback.streak_text(null), Is.EqualTo(""));
            Assert.That(BondFeedback.today_heading(null, "Bodhi"), Is.EqualTo(""));
            Assert.That(BondFeedback.fetch_text(null), Is.EqualTo(""));
            Assert.That(BondFeedback.next_level_text(null), Is.EqualTo(""));
            Assert.That(BondFeedback.tricks_text(null), Is.EqualTo(""));
            Assert.That(BondJourney.level_name_for(0), Is.Null);
            Assert.That(BondJourney.level_name_for(BondJourney.highest_level + 1), Is.Null);
            Assert.That(BondJourney.level_name_for(1), Is.EqualTo("Getting to know you"));
        }
    }
}
