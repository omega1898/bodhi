// Purpose: proves story events move Bodhi's feelings as designed and never out of range.
// Ported from core/tests/test_bond_state.cpp.
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class BondStateTests
    {
        // A new bond starts where the C++ core starts it.
        [Test]
        public void a_new_bond_starts_calm_and_trusting()
        {
            var bond = new BondState();
            Assert.That(bond.closeness_need, Is.EqualTo(0.2));
            Assert.That(bond.fear, Is.EqualTo(0.0));
            Assert.That(bond.trust, Is.EqualTo(0.8));
        }

        // The collapse frightens him and makes him want to stay close.
        [Test]
        public void the_collapse_frightens_him_and_he_stays_close()
        {
            var bond = new BondState();
            bond.apply_event("walkway_collapsed");
            Assert.That(bond.fear, Is.GreaterThan(0.5));
            Assert.That(bond.closeness_need, Is.GreaterThan(0.4));
        }

        // After the reunion he stays close, whatever came before.
        [Test]
        public void after_the_reunion_he_stays_close()
        {
            var bond = new BondState { closeness_need = 0.0 };
            bond.apply_event("reunion_contact_made");
            Assert.That(bond.closeness_need, Is.GreaterThanOrEqualTo(0.9));
        }

        // Unknown events change nothing.
        [Test]
        public void an_unknown_event_changes_nothing()
        {
            var bond = new BondState();
            bond.apply_event("no_such_event");
            Assert.That(bond.fear, Is.EqualTo(0.0));
            Assert.That(bond.trust, Is.EqualTo(0.8));
        }

        // Break it: a frightening event a thousand times, then a calming one; feelings stay 0 to 1.
        [Test]
        public void break_it_repeated_events_stay_between_zero_and_one()
        {
            var bond = new BondState();
            for (int repeat = 0; repeat < 1000; repeat += 1)
            {
                bond.apply_event("walkway_collapsed");
            }
            Assert.That(bond.fear, Is.LessThanOrEqualTo(1.0));
            Assert.That(bond.closeness_need, Is.LessThanOrEqualTo(1.0));
            for (int repeat = 0; repeat < 1000; repeat += 1)
            {
                bond.apply_event("rested_together");
            }
            Assert.That(bond.fear, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(bond.closeness_need, Is.GreaterThanOrEqualTo(0.0));
        }

        // Break it: a broken number ("not a number") from a damaged save becomes 0, so it can't spread.
        [Test]
        public void break_it_a_broken_number_is_reset_to_zero()
        {
            var bond = new BondState { fear = double.NaN };
            bond.apply_event("whistle_heard");
            Assert.That(bond.fear, Is.EqualTo(0.0));
        }

        // Break it: no event name at all changes nothing and doesn't crash.
        [Test]
        public void break_it_a_missing_event_name_changes_nothing()
        {
            var bond = new BondState();
            bond.apply_event(null);
            Assert.That(bond.trust, Is.EqualTo(0.8));
        }
    }
}
