// Purpose: proves the touch-driven care moments (towel-drying, feeding, bathing, grooming)
// follow their steps, build trust only up to a cap per chapter, and can never fail or block
// the story (spec R4.1 to R4.3, decision D6).
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class CareMomentTests
    {
        // Makes a moment with a fresh dog, bond and chapter trust budget.
        static CareMoment make(CareMomentKind kind, out CareState care, out BondState bond, out CareTrustBudget budget)
        {
            care = new CareState(0.8, 0.9, 2);
            bond = new BondState { trust = 0.5, fear = 0.5 };
            budget = new CareTrustBudget();
            return new CareMoment(kind, care, bond, budget, true);
        }

        // Rubs every wet area dry, three rubs each.
        static void dry_every_area(CareMoment moment)
        {
            for (int area = 0; area < CareMoment.wet_area_count; area += 1)
            {
                for (int rub = 0; rub < CareMoment.rubs_per_wet_area; rub += 1)
                {
                    moment.report_gesture(CareGesture.towel_rub, area);
                }
            }
        }

        // Drying: each wet area needs its rubs; then he's mostly clean and it's finished.
        [Test]
        public void drying_finishes_when_every_area_is_dry()
        {
            var moment = make(CareMomentKind.drying, out var care, out _, out _);
            for (int area = 0; area < CareMoment.wet_area_count - 1; area += 1)
            {
                for (int rub = 0; rub < CareMoment.rubs_per_wet_area; rub += 1)
                {
                    moment.report_gesture(CareGesture.towel_rub, area);
                }
            }
            Assert.That(moment.state, Is.EqualTo(CareMomentState.in_progress));
            Assert.That(moment.progress(), Is.EqualTo(0.75).Within(1e-9));

            dry_every_area(moment);
            Assert.That(moment.state, Is.EqualTo(CareMomentState.finished));
            Assert.That(care.dirt, Is.EqualTo(0.15).Within(1e-9));
            Assert.That(moment.finished_event_name, Is.EqualTo("dog_dried"));
        }

        // Feeding: pour the food, then wait while he eats. Pouring alone isn't enough.
        [Test]
        public void feeding_needs_the_food_poured_and_a_wait()
        {
            var moment = make(CareMomentKind.feeding, out var care, out _, out _);
            moment.pass_time(10);  // Waiting before pouring does nothing.
            moment.report_gesture(CareGesture.food_poured);
            moment.pass_time(CareMoment.eating_seconds - 1);
            Assert.That(moment.state, Is.EqualTo(CareMomentState.in_progress));
            moment.pass_time(1);
            Assert.That(moment.state, Is.EqualTo(CareMomentState.finished));
            Assert.That(care.hunger, Is.EqualTo(0.0));
            Assert.That(care.food, Is.EqualTo(1));
        }

        // Bathing: scrub, rinse, then step back for the shake-off, in that order.
        [Test]
        public void bathing_goes_scrub_rinse_step_back()
        {
            var moment = make(CareMomentKind.bathing, out var care, out _, out _);
            Assert.That(moment.report_gesture(CareGesture.rinse), Is.False);  // Rinse before scrubbing: not yet.
            for (int index = 0; index < CareMoment.scrubs_needed; index += 1) { moment.report_gesture(CareGesture.scrub); }
            for (int index = 0; index < CareMoment.rinses_needed; index += 1) { moment.report_gesture(CareGesture.rinse); }
            Assert.That(moment.state, Is.EqualTo(CareMomentState.in_progress));
            moment.report_gesture(CareGesture.step_back);
            Assert.That(moment.state, Is.EqualTo(CareMomentState.finished));
            Assert.That(care.dirt, Is.EqualTo(0.0));
        }

        // Grooming: only strokes along the coat count. Against the coat is gently ignored.
        [Test]
        public void grooming_counts_strokes_along_the_coat()
        {
            var moment = make(CareMomentKind.grooming, out _, out var bond, out _);
            Assert.That(moment.report_gesture(CareGesture.brush_against_coat), Is.False);
            for (int index = 0; index < CareMoment.strokes_needed; index += 1) { moment.report_gesture(CareGesture.brush_with_coat); }
            Assert.That(moment.state, Is.EqualTo(CareMomentState.finished));
            Assert.That(bond.fear, Is.LessThan(0.5));
        }

        // Every counted gesture adds a little trust.
        [Test]
        public void counted_gestures_add_trust()
        {
            var moment = make(CareMomentKind.grooming, out _, out var bond, out _);
            moment.report_gesture(CareGesture.brush_with_coat);
            Assert.That(bond.trust, Is.EqualTo(0.5 + CareMoment.trust_per_gesture).Within(1e-9));
        }

        // Break it: repeating care can't farm trust. The chapter's budget caps it across moments.
        [Test]
        public void break_it_repeating_care_cannot_farm_trust()
        {
            var bond = new BondState { trust = 0.5 };
            var budget = new CareTrustBudget();
            var care = new CareState();
            for (int repeat = 0; repeat < 20; repeat += 1)
            {
                var moment = new CareMoment(CareMomentKind.drying, care, bond, budget, true);
                dry_every_area(moment);
            }
            Assert.That(bond.trust, Is.EqualTo(0.5 + CareTrustBudget.trust_per_chapter).Within(1e-9));
        }

        // Break it: gestures after the moment is finished change nothing more.
        [Test]
        public void break_it_gestures_after_finishing_do_nothing()
        {
            var moment = make(CareMomentKind.grooming, out _, out var bond, out _);
            for (int index = 0; index < CareMoment.strokes_needed + 50; index += 1) { moment.report_gesture(CareGesture.brush_with_coat); }
            Assert.That(bond.trust, Is.EqualTo(0.5 + CareMoment.strokes_needed * CareMoment.trust_per_gesture).Within(1e-9));

            // Bathing's last step has no count of its own, so this is where the guard matters.
            var bath = make(CareMomentKind.bathing, out _, out var bath_bond, out _);
            for (int index = 0; index < CareMoment.scrubs_needed; index += 1) { bath.report_gesture(CareGesture.scrub); }
            for (int index = 0; index < CareMoment.rinses_needed; index += 1) { bath.report_gesture(CareGesture.rinse); }
            bath.report_gesture(CareGesture.step_back);
            double trust_when_finished = bath_bond.trust;
            Assert.That(bath.report_gesture(CareGesture.step_back), Is.False);
            Assert.That(bath_bond.trust, Is.EqualTo(trust_when_finished));
        }

        // Break it: rubbing an area that's already dry, or an area that doesn't exist, doesn't count.
        [Test]
        public void break_it_dry_or_missing_areas_do_not_count()
        {
            var moment = make(CareMomentKind.drying, out _, out _, out _);
            for (int rub = 0; rub < 20; rub += 1) { moment.report_gesture(CareGesture.towel_rub, 0); }
            Assert.That(moment.report_gesture(CareGesture.towel_rub, -1), Is.False);
            Assert.That(moment.report_gesture(CareGesture.towel_rub, 99), Is.False);
            Assert.That(moment.progress(), Is.EqualTo(0.25).Within(1e-9));
            Assert.That(moment.refused_gesture_count, Is.EqualTo(2));
        }

        // Break it: a forced gesture value that isn't real, and broken times, are refused safely.
        [Test]
        public void break_it_bad_gestures_and_times_are_refused()
        {
            var moment = make(CareMomentKind.feeding, out _, out _, out _);
            Assert.That(moment.report_gesture((CareGesture)99), Is.False);
            moment.report_gesture(CareGesture.food_poured);
            moment.pass_time(double.NaN);
            moment.pass_time(double.PositiveInfinity);
            moment.pass_time(-100);
            Assert.That(moment.state, Is.EqualTo(CareMomentState.in_progress));
            Assert.That(moment.refused_gesture_count, Is.EqualTo(1));
        }

        // Care never fails (R4.3): any moment can be skipped at any point, with no penalty.
        [Test]
        public void skipping_care_never_punishes()
        {
            foreach (CareMomentKind kind in System.Enum.GetValues(typeof(CareMomentKind)))
            {
                var moment = make(kind, out var care, out var bond, out _);
                moment.skip();
                Assert.That(moment.state, Is.EqualTo(CareMomentState.skipped));
                Assert.That(bond.trust, Is.EqualTo(0.5));
                Assert.That(bond.fear, Is.EqualTo(0.5));
                Assert.That(care.hunger, Is.EqualTo(0.8));
                Assert.That(moment.report_gesture(CareGesture.towel_rub, 0), Is.False);
            }
        }

        // Care never blocks the story (R4.3): the station chapter finishes whether care
        // happens in the middle of it or never happens at all.
        [Test]
        public void care_never_blocks_the_chapter()
        {
            var with_care = new RailwayChapter();
            with_care.report_world_event("walkway_collapsed", 60.0);
            with_care.report_world_event("dog_dried", 61.0);
            with_care.report_world_event("dog_fed", 62.0);
            with_care.report_world_event("separation_scene_finished", 75.0);
            with_care.report_world_event("maintenance_power_restored", 140.0);
            with_care.report_world_event("dog_barked_from_window", 230.0);
            with_care.report_world_event("pair_met_at_fence", 300.0);
            with_care.report_world_event("fence_touch_finished", 345.0);
            with_care.report_world_event("jammed_gate_lever_pulled", 380.0);
            with_care.report_world_event("debris_cleared", 420.0);
            with_care.report_world_event("dog_groomed", 425.0);
            with_care.report_world_event("reunion_scene_finished", 470.0);
            Assert.That(with_care.current_beat, Is.EqualTo(ChapterBeat.aftermath));
            Assert.That(with_care.refused_input_count, Is.EqualTo(0));
        }

        // Break it: a missing dog, bond or budget (null) doesn't crash; the moment still runs.
        [Test]
        public void break_it_missing_parts_do_not_crash()
        {
            var moment = new CareMoment(CareMomentKind.bathing, null, null, null, true);
            for (int index = 0; index < 20; index += 1) { moment.report_gesture(CareGesture.scrub); moment.report_gesture(CareGesture.rinse); }
            moment.report_gesture(CareGesture.step_back);
            Assert.That(moment.state, Is.EqualTo(CareMomentState.finished));
        }
    }
}
