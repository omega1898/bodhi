// Purpose: proves the hint rule (spec R6.4): after 3 minutes without progress the game nudges
// the player toward the next step on the shortest path, first gently ("look at the key"), then
// plainly ("fetch the key"). A dead end gets an offer to start over straight away.
using System.Collections.Generic;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class HintCoachTests
    {
        // A small two-character puzzle: Bodhi fetches the key through a vent, she unlocks the door.
        // Kicking the key into the well (with no way to dive for it) is a dead end.
        static Puzzle key_and_door()
        {
            var elements = new[]
            {
                new PuzzleElement("key", new[] { "behind_vent", "on_floor", "in_well" }, 0),
                new PuzzleElement("door", new[] { "locked", "open" }, 0),
                new PuzzleElement("bell", new[] { "quiet", "rung" }, 0),
            };
            var actions = new[]
            {
                new PuzzleAction("fetch_key_through_vent", Character.dog, new[] { new PuzzleCondition("key", "behind_vent") }, new[] { new PuzzleCondition("key", "on_floor") }),
                new PuzzleAction("push_key_back", Character.dog, new[] { new PuzzleCondition("key", "on_floor") }, new[] { new PuzzleCondition("key", "behind_vent") }),
                new PuzzleAction("unlock_door", Character.human, new[] { new PuzzleCondition("key", "on_floor") }, new[] { new PuzzleCondition("door", "open") }),
                new PuzzleAction("ring_bell", null, null, new[] { new PuzzleCondition("bell", "rung") }),  // Does nothing for the goal.
                new PuzzleAction("kick_key_into_well", null, new[] { new PuzzleCondition("key", "on_floor"), new PuzzleCondition("door", "locked") }, new[] { new PuzzleCondition("key", "in_well") }),
            };
            return new Puzzle("key_and_door", elements, actions, new[] { new PuzzleCondition("door", "open") });
        }

        // No hint before 3 minutes; then a gentle hint that names the thing to look at.
        [Test]
        public void a_gentle_hint_comes_after_three_minutes()
        {
            var coach = new HintCoach(new PuzzleRun(key_and_door()));
            coach.pass_time(179.0);
            Assert.That(coach.hint_due, Is.False);
            Assert.That(coach.take_hint().kind, Is.EqualTo(HintKind.none));
            coach.pass_time(1.0);
            Assert.That(coach.hint_due, Is.True);
            var hint = coach.take_hint();
            Assert.That(hint.kind, Is.EqualTo(HintKind.look_at));
            Assert.That(hint.element_name, Is.EqualTo("key"));
            Assert.That(hint.action_name, Is.EqualTo("fetch_key_through_vent"));
            Assert.That(hint.who, Is.EqualTo(Character.dog));
            Assert.That(coach.hint_due, Is.False);
            Assert.That(coach.hints_given, Is.EqualTo(1));
        }

        // Still stuck 90 seconds later: a plain hint that names the action.
        [Test]
        public void a_plain_hint_follows_if_still_stuck()
        {
            var coach = new HintCoach(new PuzzleRun(key_and_door()));
            coach.pass_time(180.0);
            coach.take_hint();
            coach.pass_time(89.0);
            Assert.That(coach.hint_due, Is.False);
            coach.pass_time(1.0);
            var hint = coach.take_hint();
            Assert.That(hint.kind, Is.EqualTo(HintKind.do_action));
            Assert.That(hint.action_name, Is.EqualTo("fetch_key_through_vent"));
        }

        // Progress resets the clock and the hints start gentle again, about the next step.
        [Test]
        public void progress_resets_the_clock()
        {
            var run = new PuzzleRun(key_and_door());
            var coach = new HintCoach(run);
            coach.pass_time(270.0);
            coach.take_hint();
            run.report_action("fetch_key_through_vent", Character.dog);
            coach.pass_time(1.0);
            Assert.That(coach.seconds_without_progress, Is.EqualTo(0.0));
            coach.pass_time(180.0);
            var hint = coach.take_hint();
            Assert.That(hint.kind, Is.EqualTo(HintKind.look_at));
            Assert.That(hint.action_name, Is.EqualTo("unlock_door"));
            Assert.That(hint.element_name, Is.EqualTo("door"));
            Assert.That(hint.who, Is.EqualTo(Character.human));
        }

        // Going forward then back again is not progress: the clock keeps running.
        [Test]
        public void undoing_a_step_is_not_progress()
        {
            var run = new PuzzleRun(key_and_door());
            var coach = new HintCoach(run);
            run.report_action("fetch_key_through_vent", Character.dog);
            coach.pass_time(1.0);  // Noticed: one step closer, so the clock goes back to 0.
            Assert.That(coach.seconds_without_progress, Is.EqualTo(0.0));
            run.report_action("push_key_back", Character.dog);
            coach.pass_time(100.0);
            coach.pass_time(80.0);
            Assert.That(coach.seconds_without_progress, Is.EqualTo(180.0));
            Assert.That(coach.hint_due, Is.True);
        }

        // A step that changes something but gets no closer (ringing the bell) is not progress.
        [Test]
        public void a_step_sideways_is_not_progress()
        {
            var run = new PuzzleRun(key_and_door());
            var coach = new HintCoach(run);
            coach.pass_time(100.0);
            run.report_action("ring_bell", Character.human);
            coach.pass_time(80.0);
            Assert.That(coach.seconds_without_progress, Is.EqualTo(180.0));
        }

        // A dead end is the game's fault, not the player's: offer to start over at once.
        [Test]
        public void a_dead_end_offers_to_start_over_at_once()
        {
            var run = new PuzzleRun(key_and_door());
            var coach = new HintCoach(run);
            run.report_action("fetch_key_through_vent", Character.dog);
            coach.pass_time(1.0);
            coach.pass_time(50.0);
            run.report_action("kick_key_into_well", Character.human);
            coach.pass_time(0.5);
            Assert.That(coach.seconds_without_progress, Is.EqualTo(50.5));  // A dead end is not progress.
            Assert.That(coach.hint_due, Is.True);
            Assert.That(coach.take_hint().kind, Is.EqualTo(HintKind.start_over));
        }

        // A solved puzzle gives no hints.
        [Test]
        public void a_solved_puzzle_gives_no_hints()
        {
            var run = new PuzzleRun(key_and_door());
            var coach = new HintCoach(run);
            run.report_action("fetch_key_through_vent", Character.dog);
            run.report_action("unlock_door", Character.human);
            coach.pass_time(1.0);
            coach.pass_time(10000.0);
            Assert.That(coach.hint_due, Is.False);
            Assert.That(coach.take_hint().kind, Is.EqualTo(HintKind.none));
        }

        // Following only the plain hints solves the station puzzle in its shortest number of steps.
        [Test]
        public void following_the_hints_solves_the_station_puzzle()
        {
            var puzzle = StationPuzzles.main_puzzle();
            int shortest = PuzzleSolver.solve(puzzle).shortest_solution.Length;
            var run = new PuzzleRun(puzzle);
            var coach = new HintCoach(run);
            var done = new List<string>();
            while (!run.is_solved && done.Count <= shortest)
            {
                coach.pass_time(180.0);
                var hint = coach.take_hint();
                Assert.That(hint.kind, Is.EqualTo(HintKind.look_at));
                coach.pass_time(90.0);
                hint = coach.take_hint();
                Assert.That(hint.kind, Is.EqualTo(HintKind.do_action));
                Assert.That(run.report_action(hint.action_name, hint.who ?? Character.human), Is.True, hint.action_name);
                done.Add(hint.action_name);
                coach.pass_time(0.1);
            }
            Assert.That(run.is_solved, Is.True);
            Assert.That(done.Count, Is.EqualTo(shortest), string.Join(", ", done));
        }

        // Break it: broken times change nothing, and a missing run never hints or crashes.
        [Test]
        public void break_it_broken_times_and_missing_runs()
        {
            var coach = new HintCoach(new PuzzleRun(key_and_door()));
            coach.pass_time(double.NaN);
            coach.pass_time(-500.0);
            coach.pass_time(-0.5);
            coach.pass_time(double.PositiveInfinity);
            Assert.That(coach.seconds_without_progress, Is.EqualTo(0.0));
            var nobody = new HintCoach(null);
            nobody.pass_time(1000.0);
            Assert.That(nobody.hint_due, Is.False);
            Assert.That(nobody.take_hint().kind, Is.EqualTo(HintKind.none));
        }

        // Break it: a broken puzzle (bad data) never hints.
        [Test]
        public void break_it_a_broken_puzzle_never_hints()
        {
            var broken = new Puzzle("broken", new[] { new PuzzleElement("lever", new[] { "up" }, 5) }, new PuzzleAction[0], new[] { new PuzzleCondition("lever", "down") });
            Assert.That(broken.is_valid, Is.False);
            var coach = new HintCoach(new PuzzleRun(broken));
            coach.pass_time(1000.0);
            Assert.That(coach.hint_due, Is.False);
        }
    }
}
