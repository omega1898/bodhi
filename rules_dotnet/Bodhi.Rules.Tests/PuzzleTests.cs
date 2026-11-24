// Purpose: proves the puzzle engine and its solver (spec 3.4, decision D5). A puzzle is data:
// elements with states, and actions tied to who can do them. The solver tries every order of
// actions, so it can prove a puzzle is solvable, find soft locks, and count wrong moves.
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class PuzzleTests
    {
        // A tiny puzzle: Bodhi fetches a key through a vent, she unlocks the door.
        // Dropping the key down the well is a trap when `well_is_recoverable` is false.
        static Puzzle key_and_door(bool well_is_recoverable)
        {
            var elements = new[]
            {
                new PuzzleElement("key", new[] { "behind_vent", "on_floor", "in_well" }, 0),
                new PuzzleElement("door", new[] { "locked", "open" }, 0),
            };
            var actions = new System.Collections.Generic.List<PuzzleAction>
            {
                new PuzzleAction("fetch_key_through_vent", Character.dog, new[] { new PuzzleCondition("key", "behind_vent") }, new[] { new PuzzleCondition("key", "on_floor") }),
                new PuzzleAction("unlock_door", Character.human, new[] { new PuzzleCondition("key", "on_floor") }, new[] { new PuzzleCondition("door", "open") }),
                new PuzzleAction("kick_key_into_well", null, new[] { new PuzzleCondition("key", "on_floor"), new PuzzleCondition("door", "locked") }, new[] { new PuzzleCondition("key", "in_well") }),
            };
            if (well_is_recoverable)
            {
                actions.Add(new PuzzleAction("dive_for_key", Character.dog, new[] { new PuzzleCondition("key", "in_well") }, new[] { new PuzzleCondition("key", "on_floor") }));
            }
            return new Puzzle("key_and_door", elements, actions.ToArray(), new[] { new PuzzleCondition("door", "open") });
        }

        // A well-formed puzzle has no problems.
        [Test]
        public void a_well_formed_puzzle_is_valid()
        {
            var puzzle = key_and_door(true);
            Assert.That(puzzle.problems, Is.Empty);
            Assert.That(puzzle.is_valid, Is.True);
        }

        // Playing it: only the right character can act, and only when the conditions hold.
        [Test]
        public void a_run_follows_the_rules()
        {
            var run = new PuzzleRun(key_and_door(true));
            Assert.That(run.report_action("unlock_door", Character.human), Is.False);  // No key yet.
            Assert.That(run.report_action("fetch_key_through_vent", Character.human), Is.False);  // She can't fit.
            Assert.That(run.report_action("fetch_key_through_vent", Character.dog), Is.True);
            Assert.That(run.is_solved, Is.False);
            Assert.That(run.report_action("unlock_door", Character.human), Is.True);
            Assert.That(run.is_solved, Is.True);
        }

        // "Has a move": what each character could do right now. Switching (spec R5.1) is built on this.
        [Test]
        public void each_character_knows_what_they_can_do_now()
        {
            var run = new PuzzleRun(key_and_door(true));
            Assert.That(run.available_actions(Character.human), Is.Empty);
            Assert.That(run.available_actions(Character.dog), Is.EqualTo(new[] { "fetch_key_through_vent" }));
        }

        // The solver finds the shortest solution, counts states, and sees the well as a wrong move.
        [Test]
        public void the_solver_finds_the_shortest_solution_and_wrong_moves()
        {
            var report = PuzzleSolver.solve(key_and_door(true));
            Assert.That(report.is_solvable, Is.True);
            Assert.That(report.shortest_solution, Is.EqualTo(new[] { "fetch_key_through_vent", "unlock_door" }));
            Assert.That(report.soft_lock_state_count, Is.EqualTo(0));
            Assert.That(report.wrong_move_count, Is.EqualTo(1));  // Kicking the key into the well.
            Assert.That(report.solvable_by_human_alone, Is.False);
            Assert.That(report.solvable_by_dog_alone, Is.False);
        }

        // A lever that can be pulled down (the goal) and pushed back up at any time.
        static Puzzle lever()
        {
            var elements = new[] { new PuzzleElement("lever", new[] { "up", "down" }, 0) };
            var actions = new[]
            {
                new PuzzleAction("pull_lever_down", Character.human, null, new[] { new PuzzleCondition("lever", "down") }),
                new PuzzleAction("push_lever_up", Character.human, null, new[] { new PuzzleCondition("lever", "up") }),
            };
            return new Puzzle("lever", elements, actions, new[] { new PuzzleCondition("lever", "down") });
        }

        // An action that would change nothing isn't offered: the lever is already up.
        [Test]
        public void actions_that_change_nothing_are_not_offered()
        {
            Assert.That(new PuzzleRun(lever()).available_actions(Character.human), Is.EqualTo(new[] { "pull_lever_down" }));
        }

        // Solving ends the puzzle, so moving away from the goal afterwards isn't a wrong move.
        [Test]
        public void the_goal_ends_the_search()
        {
            var report = PuzzleSolver.solve(lever());
            Assert.That(report.reachable_state_count, Is.EqualTo(2));
            Assert.That(report.wrong_move_count, Is.EqualTo(0));
        }

        // Break it: if the key can't come back out of the well, that state is a soft lock,
        // and the solver must say so (spec R6.1).
        [Test]
        public void break_it_the_solver_catches_a_soft_lock()
        {
            var report = PuzzleSolver.solve(key_and_door(false));
            Assert.That(report.is_solvable, Is.True);
            Assert.That(report.soft_lock_state_count, Is.EqualTo(1));
            Assert.That(report.soft_lock_example, Is.EqualTo("key=in_well, door=locked"));
        }

        // Break it: mistakes in the data are listed as problems, and the puzzle refuses to run.
        [Test]
        public void break_it_bad_data_is_reported_not_crashed()
        {
            var elements = new[]
            {
                new PuzzleElement("gate", new[] { "shut", "open" }, 5),           // Start state out of range.
                new PuzzleElement("gate", new[] { "shut", "open" }, 0),           // Duplicate name.
                new PuzzleElement("empty", new string[0], 0),                     // No states.
            };
            var actions = new[]
            {
                new PuzzleAction("open_gate", Character.human, new[] { new PuzzleCondition("gate", "ajar") }, new[] { new PuzzleCondition("lever", "down") }),
                new PuzzleAction("open_gate", Character.human, null, null),       // Duplicate, and changes nothing.
                null,
            };
            var puzzle = new Puzzle("broken", elements, actions, new[] { new PuzzleCondition("nowhere", "x") });
            Assert.That(puzzle.is_valid, Is.False);
            Assert.That(puzzle.problems.Count, Is.GreaterThanOrEqualTo(7));
            var run = new PuzzleRun(puzzle);
            Assert.That(run.report_action("open_gate", Character.human), Is.False);
            Assert.That(PuzzleSolver.solve(puzzle).is_solvable, Is.False);
        }

        // Break it: unknown or missing action names, and forced bad character values, are refused and counted.
        [Test]
        public void break_it_unknown_actions_are_refused_and_counted()
        {
            var run = new PuzzleRun(key_and_door(true));
            Assert.That(run.report_action("teleport_key", Character.dog), Is.False);
            Assert.That(run.report_action(null, Character.dog), Is.False);
            Assert.That(run.report_action("fetch_key_through_vent", (Character)7), Is.False);
            Assert.That(run.refused_action_count, Is.EqualTo(3));
            Assert.That(run.is_solved, Is.False);
        }

        // Break it: a puzzle too big to check every state is refused, not left to hang the tests.
        [Test]
        public void break_it_a_puzzle_too_big_to_check_is_refused()
        {
            var states = new string[10];
            for (int index = 0; index < states.Length; index += 1) { states[index] = "s" + index; }
            var elements = new PuzzleElement[7];
            for (int index = 0; index < elements.Length; index += 1) { elements[index] = new PuzzleElement("e" + index, states, 0); }
            var puzzle = new Puzzle("huge", elements, new PuzzleAction[0], new[] { new PuzzleCondition("e0", "s1") });
            Assert.That(puzzle.is_valid, Is.False);
        }
    }
}
