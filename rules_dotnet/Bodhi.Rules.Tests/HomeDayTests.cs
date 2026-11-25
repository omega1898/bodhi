// Purpose: proves the home days (HomeDay): three small tasks a day, the same three on every
// phone for the same day, bond points when a task is done, a gentle daily streak that resets
// without taking anything away, the fetch record, and clean handling of the day changing while
// the game is open, the clock going backwards, and damaged saves.
using System.Collections.Generic;
using System.Linq;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class HomeDayTests
    {
        const int first_day = 739000;  // About November 2024, counted from 1 January in year 1.

        // Finishes every task offered today, catching the ball five times for fetch.
        static void finish_today(HomeDay home, BondJourney journey)
        {
            foreach (var task in home.tasks_today.ToList())
            {
                if (task == HomeTask.fetch_five_in_a_row)
                {
                    for (int catches = 0; catches < HomeDay.fetch_catches_needed; catches++) home.fetch_caught(journey);
                }
                else
                {
                    Assert.That(home.do_task(task, journey).counted, Is.True, task.ToString());
                }
            }
            Assert.That(home.all_done_today, Is.True);
        }

        // Before any day is given there are no tasks; a day brings three different ones.
        [Test]
        public void a_day_offers_three_different_tasks()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            Assert.That(home.tasks_today, Is.Empty);
            Assert.That(home.change_day(first_day, journey), Is.EqualTo(DayChange.new_day_started));
            Assert.That(home.tasks_today.Count, Is.EqualTo(HomeDay.tasks_per_day));
            Assert.That(home.tasks_today.Distinct().Count(), Is.EqualTo(HomeDay.tasks_per_day));
            foreach (var task in home.tasks_today)
            {
                Assert.That(HomeDay.task_text(task), Is.Not.Empty);
            }
            Assert.That(home.change_day(first_day, journey), Is.EqualTo(DayChange.same_day));
        }

        // The same day and progress give the same three tasks every time, on every phone. This
        // exact answer is pinned (and was worked out separately in Python with the same mixer):
        // if it ever changes, players on different versions disagree.
        [Test]
        public void the_same_day_gives_the_same_tasks_everywhere()
        {
            var first = HomeDay.choose_tasks(first_day, true, true);
            var again = HomeDay.choose_tasks(first_day, true, true);
            Assert.That(again, Is.EqualTo(first));
            Assert.That(first, Is.EqualTo(new[] { HomeTask.teach_new_trick, HomeTask.walk_round_the_block, HomeTask.fetch_five_in_a_row }));
        }

        // Over two months every task comes up, so the days feel different.
        [Test]
        public void every_task_comes_up_over_the_weeks()
        {
            var seen = new HashSet<HomeTask>();
            for (int day = first_day; day < first_day + 60; day++)
            {
                seen.UnionWith(HomeDay.choose_tasks(day, true, true));
            }
            foreach (HomeTask task in System.Enum.GetValues(typeof(HomeTask)))
            {
                Assert.That(seen, Does.Contain(task), task.ToString());
            }
        }

        // The trick tasks are only offered when they can be done.
        [Test]
        public void trick_tasks_need_a_trick()
        {
            for (int day = first_day; day < first_day + 60; day++)
            {
                var tasks = HomeDay.choose_tasks(day, false, false);
                Assert.That(tasks, Does.Not.Contain(HomeTask.practise_known_trick));
                Assert.That(tasks, Does.Not.Contain(HomeTask.teach_new_trick));
                Assert.That(tasks.Length, Is.EqualTo(HomeDay.tasks_per_day));
            }
        }

        // Doing a task marks it done once and grows the bond; doing it again marks nothing new.
        [Test]
        public void a_done_task_grows_the_bond()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            home.change_day(first_day, journey);
            var task = home.tasks_today.First(t => t != HomeTask.fetch_five_in_a_row);
            Assert.That(home.progress(task), Is.EqualTo(0.0));
            var result = home.do_task(task, journey);
            Assert.That(result.counted, Is.True);
            Assert.That(result.task_finished, Is.True);
            Assert.That(result.bond.points_gained, Is.GreaterThan(0));
            Assert.That(home.is_done(task), Is.True);
            Assert.That(home.progress(task), Is.EqualTo(1.0));
            Assert.That(home.do_task(task, journey).task_finished, Is.False);
        }

        // Fetch counts catches in a row; a miss starts the count again; records are kept.
        [Test]
        public void fetch_keeps_its_records()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            home.change_day(first_day, journey);
            for (int catches = 0; catches < 3; catches++) home.fetch_caught(journey);
            home.fetch_missed();
            for (int catches = 0; catches < 2; catches++) home.fetch_caught(journey);
            Assert.That(home.fetch_catches_in_a_row, Is.EqualTo(2));
            Assert.That(home.fetch_best_today, Is.EqualTo(3));
            Assert.That(home.fetch_best_ever, Is.EqualTo(3));
            Assert.That(home.do_task(HomeTask.fetch_five_in_a_row, journey).counted, Is.False);  // Only catches count.
            home.change_day(first_day + 1, journey);
            Assert.That(home.fetch_best_today, Is.EqualTo(0));
            Assert.That(home.fetch_best_ever, Is.EqualTo(3));
        }

        // Teaching takes the next unlocked trick; the bond records it.
        [Test]
        public void teaching_at_home_teaches_the_next_trick()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            home.change_day(first_day, journey);
            var result = home.do_task(HomeTask.teach_new_trick, journey);
            Assert.That(result.bond.newly_taught, Is.EqualTo("sit"));
            Assert.That(journey.knows_trick("sit"), Is.True);
            Assert.That(home.do_task(HomeTask.teach_new_trick, journey).counted, Is.False);  // Nothing new to teach.
        }

        // All three done on days in a row builds a streak; a missed day resets it, but the bond
        // points stay exactly where they were.
        [Test]
        public void a_streak_grows_and_a_missed_day_resets_it_gently()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            for (int day = first_day; day < first_day + 3; day++)
            {
                home.change_day(day, journey);
                finish_today(home, journey);
            }
            Assert.That(home.streak, Is.EqualTo(3));
            Assert.That(home.best_streak, Is.EqualTo(3));
            int points = journey.points;
            home.change_day(first_day + 5, journey);  // Two days missed.
            Assert.That(home.streak, Is.EqualTo(0));
            Assert.That(home.best_streak, Is.EqualTo(3));
            Assert.That(journey.points, Is.EqualTo(points));
            finish_today(home, journey);
            Assert.That(home.streak, Is.EqualTo(1));
        }

        // The day changing while the game is open rolls over: new tasks, none done yet.
        [Test]
        public void midnight_rolls_over_cleanly()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            home.change_day(first_day, journey);
            finish_today(home, journey);
            Assert.That(home.change_day(first_day + 1, journey), Is.EqualTo(DayChange.new_day_started));
            Assert.That(home.all_done_today, Is.False);
            Assert.That(home.tasks_today.Any(task => home.is_done(task)), Is.False);
            Assert.That(home.streak, Is.EqualTo(1));  // Yesterday still counts until today is missed.
        }

        // Break it: the clock going back keeps today's tasks and grants nothing twice.
        [Test]
        public void the_clock_going_back_grants_nothing_twice()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            home.change_day(first_day, journey);
            finish_today(home, journey);
            string before = home.to_text();
            int points = journey.points;
            Assert.That(home.change_day(first_day - 1, journey), Is.EqualTo(DayChange.refused_went_backwards));
            Assert.That(home.to_text(), Is.EqualTo(before));
            foreach (var task in home.tasks_today.ToList())
            {
                Assert.That(home.do_task(task, journey).task_finished, Is.False);
            }
            Assert.That(home.streak, Is.EqualTo(1));
            Assert.That(home.change_day(first_day, journey), Is.EqualTo(DayChange.same_day));
            Assert.That(journey.points, Is.GreaterThanOrEqualTo(points));
        }

        // Break it: broken days, unknown tasks and a missing journey are refused and counted.
        [Test]
        public void bad_input_is_refused()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            home.change_day(first_day, journey);
            string before = home.to_text();
            Assert.That(home.change_day(0, journey), Is.EqualTo(DayChange.refused_bad_day));
            Assert.That(home.change_day(-3, journey), Is.EqualTo(DayChange.refused_bad_day));
            Assert.That(home.change_day(int.MaxValue, journey), Is.EqualTo(DayChange.refused_bad_day));
            Assert.That(home.change_day(first_day + 1, null), Is.EqualTo(DayChange.refused_bad_day));
            Assert.That(home.do_task((HomeTask)42, journey).counted, Is.False);
            Assert.That(home.do_task(HomeTask.brush, null).counted, Is.False);
            Assert.That(home.fetch_caught(null).counted, Is.False);
            Assert.That(home.refused_count, Is.EqualTo(7));
            Assert.That(home.to_text(), Is.EqualTo(before));
            Assert.That(HomeDay.task_text((HomeTask)42), Is.EqualTo(""));
            Assert.That(home.progress((HomeTask)42), Is.EqualTo(0.0));
        }

        // The home days save as text and load back the same, with the day's tasks and records.
        [Test]
        public void home_days_survive_a_save()
        {
            var home = new HomeDay();
            var journey = new BondJourney();
            home.change_day(first_day, journey);
            finish_today(home, journey);
            home.change_day(first_day + 1, journey);
            home.do_task(home.tasks_today.First(t => t != HomeTask.fetch_five_in_a_row), journey);
            var loaded = new HomeDay();
            Assert.That(loaded.load_text(home.to_text()), Is.True);
            Assert.That(loaded.to_text(), Is.EqualTo(home.to_text()));
            Assert.That(loaded.tasks_today, Is.EqualTo(home.tasks_today));
            Assert.That(loaded.streak, Is.EqualTo(1));
            Assert.That(loaded.change_day(first_day + 1, journey), Is.EqualTo(DayChange.same_day));
            var fresh = new HomeDay();
            Assert.That(loaded.load_text(fresh.to_text()), Is.True);  // A home that never had a day.
            Assert.That(loaded.tasks_today, Is.Empty);
        }

        // Break it: a damaged home save is refused whole and changes nothing.
        [Test]
        public void a_damaged_home_save_is_refused_whole()
        {
            string tasks = "home_task brush 0\nhome_task dinner 0\nhome_task cuddle 0\n";
            // The good save the bad ones are made from loads, so each refusal is for its own damage.
            Assert.That(new HomeDay().load_text("home 100 0 0 0 0 0\n" + tasks), Is.True);
            Assert.That(new HomeDay().load_text("home 100 2 3 99 6 5\n" + tasks), Is.True);
            string[] bad_texts =
            {
                null,
                "",
                "home 0 0 0 0 0 0",                                     // No line break.
                "home 0 0 0 0 0\n",                                     // A number missing.
                "home -5 0 0 0 0 0\n",
                "home 99999999999 0 0 0 0 0\n",
                "home 5000000 0 0 0 0 0\n" + tasks,                     // Beyond the last day.
                "home 0 0 0 0 0 0\n" + tasks,                           // Tasks without a day.
                "home 100 0 0 0 0 0\n",                                 // A day without tasks.
                "home 100 3 2 99 0 0\n" + tasks,                        // Streak above the best.
                "home 100 2 2 50 0 0\n" + tasks,                        // A streak, but days missed since.
                "home 100 0 0 101 0 0\n" + tasks,                       // Completed in the future.
                "home 100 0 0 0 3 9\n" + tasks,                         // Today beats the best ever.
                "home 100 0 0 0 0 0\nhome_task brush 0\nhome_task brush 0\nhome_task cuddle 0\n",
                "home 100 0 0 0 0 0\nhome_task fly 0\nhome_task dinner 0\nhome_task cuddle 0\n",
                "home 100 0 0 0 0 0\nhome_task 1 0\nhome_task dinner 0\nhome_task cuddle 0\n",
                "home 100 0 0 0 0 0\nhome_task brush 2\nhome_task dinner 0\nhome_task cuddle 0\n",
                "home 100 0 0 0 0 0\nhome_task brush 1\nhome_task dinner 1\nhome_task cuddle 1\n",  // All done, not recorded.
                "home 100 0 0 0 0 0\nhome_task fetch_five_in_a_row 1\nhome_task dinner 0\nhome_task cuddle 0\n",
                "home 100 0 0 0 0 0\n" + tasks + "home_task walk_round_the_block 0\n",
                new string('x', 2000) + "\n",
            };
            foreach (var bad in bad_texts)
            {
                var home = new HomeDay();
                home.change_day(first_day, new BondJourney());
                string before = home.to_text();
                Assert.That(home.load_text(bad), Is.False, bad ?? "null");
                Assert.That(home.to_text(), Is.EqualTo(before));
            }
        }
    }
}
