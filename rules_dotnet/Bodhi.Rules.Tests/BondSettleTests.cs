// Purpose: proves Bodhi's feelings settle back over time after a scare (spec R3.2): after the
// separation he stays closer for a while, then slowly relaxes. Fear fades faster than the need
// to be near her.
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class BondSettleTests
    {
        // After the walkway collapse, closeness need is high and fades halfway back in 3 minutes.
        [Test]
        public void after_the_separation_he_stays_close_for_a_while()
        {
            var bond = new BondState();
            bond.apply_event("walkway_collapsed");
            double raised = bond.closeness_need;
            Assert.That(raised, Is.GreaterThan(0.4));
            for (int second = 0; second < 180; second += 1) { bond.settle(1.0); }
            double halfway = BondState.resting_closeness_need + (raised - BondState.resting_closeness_need) / 2;
            Assert.That(bond.closeness_need, Is.EqualTo(halfway).Within(0.001));
        }

        // Fear fades faster: halfway in one minute.
        [Test]
        public void fear_fades_faster_than_closeness()
        {
            var bond = new BondState { fear = 0.8 };
            bond.settle(60.0);
            Assert.That(bond.fear, Is.EqualTo(0.4).Within(1e-9));
            Assert.That(BondState.fear_half_life_seconds, Is.LessThan(BondState.closeness_need_half_life_seconds));
        }

        // Settling never pushes feelings past their resting values, and trust doesn't fade.
        [Test]
        public void settling_stops_at_rest_and_leaves_trust_alone()
        {
            var bond = new BondState { closeness_need = 0.1, fear = 0.0, trust = 0.9 };
            bond.settle(10000);
            Assert.That(bond.closeness_need, Is.EqualTo(0.1));  // Already calmer than rest: left alone.
            Assert.That(bond.fear, Is.EqualTo(0.0));
            Assert.That(bond.trust, Is.EqualTo(0.9));
        }

        // Settling in many small steps gives the same answer as one big step, so the frame rate
        // can't change how fast he calms down.
        [Test]
        public void settling_is_the_same_at_any_frame_rate()
        {
            var one_step = new BondState { closeness_need = 0.9, fear = 0.9 };
            var many_steps = new BondState { closeness_need = 0.9, fear = 0.9 };
            one_step.settle(10.0);
            for (int frame = 0; frame < 600; frame += 1) { many_steps.settle(10.0 / 600); }
            Assert.That(many_steps.closeness_need, Is.EqualTo(one_step.closeness_need).Within(1e-9));
            Assert.That(many_steps.fear, Is.EqualTo(one_step.fear).Within(1e-9));
        }

        // Break it: broken times change nothing.
        [Test]
        public void break_it_broken_times_change_nothing()
        {
            var bond = new BondState { closeness_need = 0.9, fear = 0.9 };
            bond.settle(double.NaN);
            bond.settle(-60);
            bond.settle(-0.001);
            bond.settle(double.PositiveInfinity);
            Assert.That(bond.closeness_need, Is.EqualTo(0.9));
            Assert.That(bond.fear, Is.EqualTo(0.9));
        }
    }
}
