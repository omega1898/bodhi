// Purpose: proves the station chapter's main puzzle (water level, floating platform,
// counterweight) meets spec R6.1 to R6.3, using the solver to try every order of actions.
using System.Linq;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class StationPuzzleTests
    {
        // R6.1: solvable, and no reachable state can trap the player.
        [Test]
        public void the_main_puzzle_is_solvable_with_no_soft_lock()
        {
            var report = PuzzleSolver.solve(StationPuzzles.main_puzzle());
            Assert.That(StationPuzzles.main_puzzle().problems, Is.Empty);
            Assert.That(report.is_solvable, Is.True);
            Assert.That(report.soft_lock_state_count, Is.EqualTo(0), report.soft_lock_example);
        }

        // R6.2: neither of them can do it alone.
        [Test]
        public void the_main_puzzle_needs_both_of_them()
        {
            var report = PuzzleSolver.solve(StationPuzzles.main_puzzle());
            Assert.That(report.solvable_by_human_alone, Is.False);
            Assert.That(report.solvable_by_dog_alone, Is.False);
        }

        // R6.3: at least 6 actions, and at least one wrong turn to explore.
        [Test]
        public void the_main_puzzle_is_deep_enough()
        {
            var report = PuzzleSolver.solve(StationPuzzles.main_puzzle());
            Assert.That(report.shortest_solution.Length, Is.GreaterThanOrEqualTo(6), string.Join(", ", report.shortest_solution));
            Assert.That(report.wrong_move_count, Is.GreaterThanOrEqualTo(1));
            TestContext.WriteLine("states " + report.reachable_state_count + ", wrong moves " + report.wrong_move_count
                + ", shortest: " + string.Join(", ", report.shortest_solution));
        }

        // A playthrough with a mistake: pumping the water too low strands the platform, and
        // opening the sluice puts it right. Then both finish it together.
        [Test]
        public void a_mistake_can_be_put_right()
        {
            var run = new PuzzleRun(StationPuzzles.main_puzzle());
            Assert.That(run.report_action("restore_power", Character.human), Is.True);
            Assert.That(run.report_action("nose_open_valve", Character.dog), Is.True);
            Assert.That(run.report_action("run_pump", Character.human), Is.True);
            Assert.That(run.report_action("run_pump_again", Character.human), Is.True);       // Too low now.
            Assert.That(run.report_action("jump_onto_platform", Character.dog), Is.False);  // Stranded.
            Assert.That(run.report_action("open_sluice_from_low", Character.human), Is.True);
            Assert.That(run.report_action("jump_onto_platform", Character.dog), Is.True);
            Assert.That(run.report_action("haul_platform_across", Character.human), Is.True);
            Assert.That(run.report_action("release_counterweight", Character.human), Is.True);
            Assert.That(run.report_action("climb_the_ramp", Character.dog), Is.True);
            Assert.That(run.is_solved, Is.True);
        }

        // Break it: without the sluice, pumping too low would trap the player. The solver must
        // catch that, which shows the sluice is what keeps this puzzle fair.
        [Test]
        public void break_it_without_the_sluice_the_solver_finds_a_soft_lock()
        {
            var actions = StationPuzzles.actions().Where(action => !action.name.StartsWith("open_sluice")).ToArray();
            var puzzle = new Puzzle("station_without_sluice", StationPuzzles.elements(), actions, StationPuzzles.goal());
            var report = PuzzleSolver.solve(puzzle);
            Assert.That(report.soft_lock_state_count, Is.GreaterThan(0));
        }

        // Break it: each call gives a fresh copy, so changing one can't change the real puzzle.
        [Test]
        public void break_it_changing_a_copy_leaves_the_puzzle_alone()
        {
            var elements = StationPuzzles.elements();
            elements[0] = new PuzzleElement("power", new[] { "on" }, 0);
            Assert.That(PuzzleSolver.solve(StationPuzzles.main_puzzle()).solvable_by_human_alone, Is.False);
            Assert.That(StationPuzzles.elements()[0].states.Length, Is.EqualTo(2));
        }
    }
}
