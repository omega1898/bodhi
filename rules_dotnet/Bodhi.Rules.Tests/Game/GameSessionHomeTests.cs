// Purpose: proves the game session's side of the bond and the home days: telling it the date,
// opening home from the title without starting a chapter, doing the day's tasks, playing fetch,
// and care given in a chapter, each saved at once so nothing is lost. Also tries to break it: a
// task done twice, a task not offered today, the clock turned back, broken day numbers, care
// before any year is open, and damaged saves.
using System;
using System.IO;
using System.Linq;
using Bodhi.Game;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class GameSessionHomeTests
    {
        const int first_day = 739000;  // About November 2024, counted from 1 January in year 1.
        string folder;

        // Each test gets its own empty save folder.
        [SetUp]
        public void make_folder()
        {
            folder = Path.Combine(Path.GetTempPath(), "bodhi_home_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        // And it is removed afterwards.
        [TearDown]
        public void remove_folder()
        {
            Directory.Delete(folder, true);
        }

        GameSession new_session()
        {
            return new GameSession(new SaveFiles(folder), "Bodhi");
        }

        // The year's save file as it is on the phone now.
        string year_file()
        {
            string path = Path.Combine(folder, "campaign.save");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        // The first day from first_day on whose tasks for a brand new year include fetch.
        static int a_day_with_fetch()
        {
            int day = first_day;
            while (!HomeDay.choose_tasks(day, false, true).Contains(HomeTask.fetch_five_in_a_row)) day++;
            return day;
        }

        // Does every task offered today through the session, catching five balls for fetch.
        static void finish_today(GameSession session)
        {
            foreach (var task in session.campaign.home.tasks_today.ToList())
            {
                if (task == HomeTask.fetch_five_in_a_row)
                {
                    for (int catches = 0; catches < HomeDay.fetch_catches_needed; catches++) session.fetch_caught();
                }
                else
                {
                    Assert.That(session.do_home_task(task).counted, Is.True, task.ToString());
                }
            }
            Assert.That(session.campaign.home.all_done_today, Is.True);
        }

        // The phone's date becomes a day number: the same all day, one more the next day.
        [Test]
        public void a_date_becomes_a_day_number()
        {
            int morning = GameSession.day_number(new DateTime(2026, 11, 24, 0, 0, 1));
            Assert.That(GameSession.day_number(new DateTime(2026, 11, 24, 23, 59, 59)), Is.EqualTo(morning));
            Assert.That(GameSession.day_number(new DateTime(2026, 11, 25, 0, 0, 0)), Is.EqualTo(morning + 1));
            Assert.That(morning, Is.GreaterThan(0).And.LessThan(BondJourney.largest_day));
        }

        // The date given at start-up is used by New Game: today's three tasks are ready and saved.
        [Test]
        public void a_new_game_starts_on_today()
        {
            var session = new_session();
            session.set_today(first_day);
            Assert.That(session.has_save, Is.False, "telling the date alone writes nothing");
            session.new_game();
            Assert.That(session.campaign.home.day, Is.EqualTo(first_day));
            Assert.That(session.campaign.home.tasks_today.Count, Is.EqualTo(HomeDay.tasks_per_day));
            Assert.That(new_session().saved_year().home.day, Is.EqualTo(first_day));
        }

        // Home from the title opens the saved year without starting a chapter, on today's date.
        [Test]
        public void home_opens_the_saved_year_without_a_chapter()
        {
            var first = new_session();
            first.set_today(first_day);
            first.new_game();
            var later = new_session();
            later.set_today(first_day + 1);
            later.open_home();
            Assert.That(later.run, Is.Null);
            Assert.That(later.campaign.current_chapter.id, Is.EqualTo("coming_home"));
            Assert.That(later.campaign.home.day, Is.EqualTo(first_day + 1));
            Assert.That(new_session().saved_year().home.day, Is.EqualTo(first_day + 1), "the new day was saved");
            later.continue_game();
            Assert.That(later.run.chapter.id, Is.EqualTo("coming_home"), "Continue still plays the chapter");
        }

        // A task done at home is saved at once, with its bond points.
        [Test]
        public void a_home_task_is_saved_straight_away()
        {
            var session = new_session();
            session.set_today(first_day);
            session.open_home();
            var task = session.campaign.home.tasks_today.First(each => each != HomeTask.fetch_five_in_a_row);
            var result = session.do_home_task(task);
            Assert.That(result.counted && result.task_finished, Is.True);
            Assert.That(result.bond.points_gained, Is.GreaterThan(0));
            var saved = new_session().saved_year();
            Assert.That(saved.home.is_done(task), Is.True);
            Assert.That(saved.journey.points, Is.EqualTo(session.campaign.journey.points));
        }

        // Five catches in a row finish the fetch task; a miss starts the run again; the best is kept.
        [Test]
        public void fetch_catches_finish_the_fetch_task()
        {
            var session = new_session();
            session.set_today(a_day_with_fetch());
            session.open_home();
            session.fetch_caught();
            session.fetch_caught();
            session.fetch_missed();
            Assert.That(session.campaign.home.fetch_catches_in_a_row, Is.EqualTo(0));
            HomeResult last = null;
            for (int catches = 0; catches < HomeDay.fetch_catches_needed; catches++) last = session.fetch_caught();
            Assert.That(last.task_finished, Is.True);
            var saved = new_session().saved_year();
            Assert.That(saved.home.is_done(HomeTask.fetch_five_in_a_row), Is.True);
            Assert.That(saved.home.fetch_best_ever, Is.EqualTo(HomeDay.fetch_catches_needed));
        }

        // All three done on two days in a row makes a streak of two, kept in the save.
        [Test]
        public void days_in_a_row_build_a_streak()
        {
            var session = new_session();
            session.set_today(first_day);
            session.open_home();
            finish_today(session);
            Assert.That(session.set_today(first_day + 1), Is.EqualTo(DayChange.new_day_started));
            Assert.That(session.campaign.home.all_done_today, Is.False, "new tasks");
            finish_today(session);
            Assert.That(new_session().saved_year().home.streak, Is.EqualTo(2));
        }

        // Care given in a chapter grows the bond and is saved at once; the chapter carries on.
        [Test]
        public void care_in_a_chapter_is_saved()
        {
            var session = new_session();
            session.set_today(first_day);
            session.new_game();
            var change = session.record_care(BondCareKind.feed);
            Assert.That(change.points_gained, Is.EqualTo(BondJourney.full_points_for(BondCareKind.feed)));
            Assert.That(new_session().saved_year().journey.points, Is.EqualTo(change.points_gained));
            var later = new_session();
            later.continue_game();
            Assert.That(later.run.chapter.id, Is.EqualTo("coming_home"));
            Assert.That(later.campaign.journey.points, Is.EqualTo(change.points_gained));
        }

        // Continue rolls the home days on to today too.
        [Test]
        public void continue_moves_home_on_to_today()
        {
            var first = new_session();
            first.set_today(first_day);
            first.new_game();
            var later = new_session();
            later.set_today(first_day + 3);
            later.continue_game();
            Assert.That(later.campaign.home.day, Is.EqualTo(first_day + 3));
        }

        // Break it: tapping a task twice in a day counts once: no more points, no double streak.
        [Test]
        public void a_task_tapped_twice_counts_once()
        {
            var session = new_session();
            session.set_today(first_day);
            session.open_home();
            var task = session.campaign.home.tasks_today.First(each => each != HomeTask.fetch_five_in_a_row);
            session.do_home_task(task);
            int points = session.campaign.journey.points;
            string saved = year_file();
            var again = session.do_home_task(task);
            Assert.That(again.counted, Is.False);
            Assert.That(session.campaign.journey.points, Is.EqualTo(points));
            Assert.That(year_file(), Is.EqualTo(saved), "nothing new to save");
        }

        // Break it: a task that isn't one of today's three, or isn't a real task, is refused.
        [Test]
        public void a_task_not_offered_today_is_refused()
        {
            var session = new_session();
            session.set_today(first_day);
            session.open_home();
            var not_today = Enum.GetValues(typeof(HomeTask)).Cast<HomeTask>().First(each => !session.campaign.home.tasks_today.Contains(each));
            Assert.That(session.do_home_task(not_today).counted, Is.False);
            Assert.That(session.do_home_task((HomeTask)99).counted, Is.False);
            Assert.That(session.campaign.journey.points, Is.EqualTo(0));
        }

        // Break it: the clock turned back changes nothing: the same tasks, still done, nothing rewritten.
        [Test]
        public void the_clock_going_backwards_changes_nothing()
        {
            var session = new_session();
            session.set_today(first_day);
            session.open_home();
            var task = session.campaign.home.tasks_today.First(each => each != HomeTask.fetch_five_in_a_row);
            session.do_home_task(task);
            var tasks = session.campaign.home.tasks_today.ToList();
            string saved = year_file();
            Assert.That(session.set_today(first_day - 1), Is.EqualTo(DayChange.refused_went_backwards));
            Assert.That(session.campaign.home.tasks_today, Is.EqualTo(tasks));
            Assert.That(session.campaign.home.is_done(task), Is.True);
            Assert.That(session.do_home_task(task).counted, Is.False, "still can't be done twice");
            Assert.That(year_file(), Is.EqualTo(saved));
            Assert.That(session.set_today(first_day), Is.EqualTo(DayChange.same_day));
        }

        // Break it: broken day numbers are refused and not remembered for the next game.
        [Test]
        public void broken_days_are_refused()
        {
            var session = new_session();
            Assert.That(session.set_today(0), Is.EqualTo(DayChange.refused_bad_day));
            Assert.That(session.set_today(-5), Is.EqualTo(DayChange.refused_bad_day));
            Assert.That(session.set_today(BondJourney.largest_day + 1), Is.EqualTo(DayChange.refused_bad_day));
            Assert.That(session.set_today(GameSession.day_number(DateTime.MinValue)), Is.EqualTo(DayChange.refused_bad_day));
            session.new_game();
            Assert.That(session.campaign.home.day, Is.EqualTo(0), "no day yet");
            Assert.That(session.campaign.home.refused_count, Is.EqualTo(0), "no broken day reached the rules");
        }

        // Break it: care and tasks before any year is open count for nothing and write nothing.
        [Test]
        public void nothing_counts_before_a_year_is_open()
        {
            var session = new_session();
            session.set_today(first_day);
            Assert.That(session.record_care(BondCareKind.pet).counted, Is.False);
            Assert.That(session.do_home_task(HomeTask.brush).counted, Is.False);
            Assert.That(session.fetch_caught().counted, Is.False);
            session.fetch_missed();
            Assert.That(session.has_save, Is.False);
        }

        // Break it: home with a damaged year save opens the backup's year; with both damaged, a fresh one.
        [Test]
        public void home_with_a_damaged_save_uses_the_backup()
        {
            var first = new_session();
            first.set_today(first_day);
            first.new_game();
            var task = first.campaign.home.tasks_today.First(each => each != HomeTask.fetch_five_in_a_row);
            first.do_home_task(task);
            File.WriteAllText(Path.Combine(folder, "campaign.save"), "garbage\n");

            var later = new_session();
            later.set_today(first_day);
            later.open_home();
            Assert.That(later.refused_saves, Is.EqualTo(1));
            Assert.That(later.campaign.home.day, Is.EqualTo(first_day), "the backup: the new game's save");
            Assert.That(later.campaign.home.is_done(task), Is.False, "the task came after the backup");

            File.WriteAllText(Path.Combine(folder, "campaign.save"), "garbage\n");
            File.WriteAllText(Path.Combine(folder, "campaign.save.backup"), "garbage\n");
            var last = new_session();
            last.open_home();
            Assert.That(last.refused_saves, Is.EqualTo(2));
            Assert.That(last.campaign.journey.points, Is.EqualTo(0));
            Assert.That(last.run, Is.Null);
        }
    }
}
