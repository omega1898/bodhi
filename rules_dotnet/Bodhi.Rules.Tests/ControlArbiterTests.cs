// Purpose: proves who-the-player-controls works as spec R5.1 and R5.2 ask: a switch is offered
// only when the active character is stuck and the other one isn't, never because time passed;
// a switch blends in under a second; and no button press is lost or doubled during it.
using System.Collections.Generic;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class ControlArbiterTests
    {
        // Offered exactly when the active character has no move and the other has one.
        [Test]
        public void a_switch_is_offered_only_when_stuck()
        {
            var arbiter = new ControlArbiter(Character.human, false);
            Assert.That(arbiter.switch_offered(false, true), Is.True);
            Assert.That(arbiter.switch_offered(true, true), Is.False);
            Assert.That(arbiter.switch_offered(false, false), Is.False);
            Assert.That(arbiter.switch_offered(true, false), Is.False);
        }

        // R5.1 over the real puzzle: in every reachable state, for whoever is active, the offer
        // matches "stuck and the other has a move". And the puzzle really needs switching both ways.
        [Test]
        public void in_every_station_puzzle_state_the_offer_matches_who_is_stuck()
        {
            var puzzle = StationPuzzles.main_puzzle();
            int offers_to_dog = 0;
            int offers_to_human = 0;
            foreach (var state in PuzzleSolver.reachable_states(puzzle))
            {
                var run = new PuzzleRun(puzzle, state);
                if (run.is_solved)
                {
                    continue;
                }
                bool human_has_move = run.available_actions(Character.human).Length > 0;
                bool dog_has_move = run.available_actions(Character.dog).Length > 0;
                foreach (var active in new[] { Character.human, Character.dog })
                {
                    var arbiter = new ControlArbiter(active, false);
                    bool active_stuck = active == Character.human ? !human_has_move : !dog_has_move;
                    bool other_can_move = active == Character.human ? dog_has_move : human_has_move;
                    bool offered = arbiter.switch_offered(run);
                    Assert.That(offered, Is.EqualTo(active_stuck && other_can_move), run.describe());
                    if (offered && active == Character.human) { offers_to_dog += 1; }
                    if (offered && active == Character.dog) { offers_to_human += 1; }
                }
            }
            Assert.That(offers_to_dog, Is.GreaterThan(0));
            Assert.That(offers_to_human, Is.GreaterThan(0));
        }

        // Taking an offered switch blends to the other character in under a second.
        [Test]
        public void a_switch_blends_in_under_a_second()
        {
            var arbiter = new ControlArbiter(Character.human, false);
            Assert.That(arbiter.request_switch(false, true), Is.True);
            Assert.That(arbiter.is_blending, Is.True);
            arbiter.update(ControlArbiter.blend_seconds - 0.01);
            Assert.That(arbiter.active_character, Is.EqualTo(Character.human));
            arbiter.update(0.02);
            Assert.That(arbiter.is_blending, Is.False);
            Assert.That(arbiter.active_character, Is.EqualTo(Character.dog));
            Assert.That(ControlArbiter.blend_seconds, Is.LessThan(1.0));
        }

        // Free switching (both routes open) allows a switch any time; without it, only when stuck.
        [Test]
        public void free_switching_allows_a_switch_any_time()
        {
            Assert.That(new ControlArbiter(Character.human, true).request_switch(true, true), Is.True);
            Assert.That(new ControlArbiter(Character.human, false).request_switch(true, true), Is.False);
        }

        // A story beat can force a switch even when nobody is stuck.
        [Test]
        public void a_story_beat_can_force_a_switch()
        {
            var arbiter = new ControlArbiter(Character.human, false);
            Assert.That(arbiter.force_switch(Character.dog), Is.True);
            arbiter.update(1.0);
            Assert.That(arbiter.active_character, Is.EqualTo(Character.dog));
            Assert.That(arbiter.force_switch(Character.dog), Is.False);  // Already him.
        }

        // R5.2: presses during the blend are held, then delivered once each, in order, to the new character.
        [Test]
        public void presses_during_a_switch_are_neither_lost_nor_doubled()
        {
            var arbiter = new ControlArbiter(Character.human, false);
            var delivered = new List<string>();
            arbiter.on_input = (who, press) => delivered.Add(who + ":" + press);

            arbiter.submit_input(PlayerInput.jump);
            arbiter.request_switch(false, true);
            arbiter.submit_input(PlayerInput.move_left);
            arbiter.submit_input(PlayerInput.act);
            Assert.That(delivered, Is.EqualTo(new[] { "human:jump" }));
            arbiter.update(ControlArbiter.blend_seconds);
            arbiter.update(0.1);
            Assert.That(delivered, Is.EqualTo(new[] { "human:jump", "dog:move_left", "dog:act" }));
        }

        // Break it: time alone never switches, however long nothing happens (R5.1).
        [Test]
        public void break_it_time_alone_never_switches()
        {
            var arbiter = new ControlArbiter(Character.human, true);
            for (int step = 0; step < 100000; step += 1)
            {
                arbiter.update(0.1);
            }
            Assert.That(arbiter.active_character, Is.EqualTo(Character.human));
            Assert.That(arbiter.is_blending, Is.False);
        }

        // Break it: a flood of presses during a blend is capped, and the extra ones are counted.
        [Test]
        public void break_it_a_flood_of_presses_is_capped()
        {
            var arbiter = new ControlArbiter(Character.human, false);
            int delivered = 0;
            arbiter.on_input = (who, press) => delivered += 1;
            arbiter.request_switch(false, true);
            for (int index = 0; index < 1000; index += 1) { arbiter.submit_input(PlayerInput.jump); }
            arbiter.update(1.0);
            Assert.That(delivered, Is.EqualTo(ControlArbiter.held_input_limit));
            Assert.That(arbiter.dropped_input_count, Is.EqualTo(1000 - ControlArbiter.held_input_limit));
        }

        // Break it: asking again mid-blend, forced bad values and broken times are refused safely.
        [Test]
        public void break_it_bad_requests_are_refused()
        {
            var arbiter = new ControlArbiter(Character.human, true);
            arbiter.request_switch(true, true);
            Assert.That(arbiter.request_switch(true, true), Is.False);  // Already switching.
            Assert.That(arbiter.force_switch((Character)9), Is.False);
            arbiter.submit_input((PlayerInput)77);
            arbiter.update(double.NaN);
            arbiter.update(-5);
            Assert.That(arbiter.is_blending, Is.True);
            arbiter.update(double.PositiveInfinity);
            Assert.That(arbiter.is_blending, Is.True);
            Assert.That(arbiter.refused_input_count, Is.EqualTo(2));
            Assert.That(new ControlArbiter((Character)5, false).active_character, Is.EqualTo(Character.human));
        }

        // Break it: no listener attached doesn't crash.
        [Test]
        public void break_it_no_listener_does_not_crash()
        {
            var arbiter = new ControlArbiter(Character.dog, false);
            arbiter.submit_input(PlayerInput.act);
            Assert.That(arbiter.active_character, Is.EqualTo(Character.dog));
        }
    }
}
