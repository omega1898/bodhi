// Purpose: proves how Bodhi follows her turns his feelings into distance and glances back,
// stays inside safe ranges whatever numbers arrive, and remembers being swept away.
// Ported from core/tests/test_companion_behavior.cpp, plus the story-memory part.
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class CompanionBehaviorTests
    {
        // Makes a bond with the given feelings, so each test reads in one line.
        static BondState bond(double closeness_need, double fear, double trust)
        {
            return new BondState { closeness_need = closeness_need, fear = fear, trust = trust };
        }

        // True when every number is real and inside the documented ranges.
        static bool is_inside_safe_ranges(CompanionBehavior behavior)
        {
            return double.IsFinite(behavior.follow_distance_metres)
                && behavior.follow_distance_metres >= CompanionBehavior.closest_follow_distance_metres
                && behavior.follow_distance_metres <= CompanionBehavior.farthest_follow_distance_metres
                && double.IsFinite(behavior.check_back_interval_seconds)
                && behavior.check_back_interval_seconds >= CompanionBehavior.shortest_check_back_interval_seconds
                && behavior.check_back_interval_seconds <= CompanionBehavior.longest_check_back_interval_seconds;
        }

        // Calm: trails far, explores, rarely looks back. Terrified: stays close, never runs ahead.
        [Test]
        public void calm_and_terrified_are_the_two_ends()
        {
            var calm = CompanionBehavior.choose(bond(0.0, 0.0, 0.8), null);
            Assert.That(calm.follow_distance_metres, Is.EqualTo(6.0));
            Assert.That(calm.check_back_interval_seconds, Is.EqualTo(12.0));
            Assert.That(calm.may_run_ahead, Is.True);

            var terrified = CompanionBehavior.choose(bond(0.0, 1.0, 0.8), null);
            Assert.That(terrified.follow_distance_metres, Is.EqualTo(1.0));
            Assert.That(terrified.check_back_interval_seconds, Is.EqualTo(2.0));
            Assert.That(terrified.may_run_ahead, Is.False);
        }

        // High closeness need alone keeps him close; more fear is never farther away.
        [Test]
        public void more_feeling_is_never_farther_away()
        {
            var clingy = CompanionBehavior.choose(bond(0.9, 0.0, 0.8), null);
            Assert.That(clingy.follow_distance_metres, Is.LessThan(2.0));
            Assert.That(clingy.may_run_ahead, Is.False);
            double previous_distance = 6.0;
            for (int step = 0; step <= 100; step += 1)
            {
                var behavior = CompanionBehavior.choose(bond(0.2, step / 100.0, 0.8), null);
                Assert.That(behavior.follow_distance_metres, Is.LessThanOrEqualTo(previous_distance));
                previous_distance = behavior.follow_distance_metres;
            }
        }

        // Exactly halfway anxious is already too anxious to run ahead (the C++ rule is "below 0.5").
        [Test]
        public void halfway_anxious_does_not_run_ahead()
        {
            Assert.That(CompanionBehavior.choose(bond(0.0, 0.5, 0.8), null).may_run_ahead, Is.False);
            Assert.That(CompanionBehavior.choose(bond(0.0, 0.49, 0.8), null).may_run_ahead, Is.True);
        }

        // After the walkway collapses he stays close; the default bond lets him explore.
        [Test]
        public void the_walkway_collapse_keeps_him_close()
        {
            var feelings = new BondState();
            Assert.That(CompanionBehavior.choose(feelings, null).may_run_ahead, Is.True);
            feelings.apply_event("walkway_collapsed");
            Assert.That(CompanionBehavior.choose(feelings, null).may_run_ahead, Is.False);
        }

        // R3.2: once swept away by the river, he approaches water warily in later chapters.
        [Test]
        public void being_swept_away_makes_him_wary_of_water()
        {
            var memory = new StoryMemory();
            Assert.That(CompanionBehavior.choose(new BondState(), memory).approaches_water_warily, Is.False);
            memory.remember("swept_away_by_river");
            Assert.That(CompanionBehavior.choose(new BondState(), memory).approaches_water_warily, Is.True);
        }

        // Break it: a broken feeling (not a number) makes him cautious, never lets him roam.
        [Test]
        public void break_it_a_broken_feeling_makes_him_cautious()
        {
            var from_broken_fear = CompanionBehavior.choose(bond(0.0, double.NaN, 0.8), null);
            Assert.That(from_broken_fear.may_run_ahead, Is.False);
            Assert.That(from_broken_fear.follow_distance_metres, Is.EqualTo(1.0));
            Assert.That(CompanionBehavior.choose(bond(double.NaN, 0.0, 0.8), null).may_run_ahead, Is.False);
            Assert.That(CompanionBehavior.choose(null, null).may_run_ahead, Is.False);
        }

        // Break it: every combination of hostile numbers stays inside the safe ranges, and huge
        // negative feelings count as calm, not "beyond calm".
        [Test]
        public void break_it_hostile_numbers_stay_in_safe_ranges()
        {
            double[] hostile_values = { double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.MaxValue, double.MinValue, double.Epsilon, -0.0, -1.0, 0.5, 2.0, 1e308 };
            foreach (double closeness_need in hostile_values)
            {
                foreach (double fear in hostile_values)
                {
                    foreach (double trust in hostile_values)
                    {
                        Assert.That(is_inside_safe_ranges(CompanionBehavior.choose(bond(closeness_need, fear, trust), null)), Is.True);
                    }
                }
            }
            var beyond_calm = CompanionBehavior.choose(bond(double.NegativeInfinity, double.NegativeInfinity, 0.8), null);
            Assert.That(beyond_calm.follow_distance_metres, Is.EqualTo(6.0));
        }
    }
}
