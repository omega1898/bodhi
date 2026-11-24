// Purpose: proves trust changes how Bodhi plays (spec R4.2): with more trust he answers the
// whistle from farther away, holds "wait" longer, and comes to her for contact more often.
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class BondEffectsTests
    {
        // The two ends and the middle of each mapping.
        [Test]
        public void trust_sets_whistle_range_wait_and_contact()
        {
            Assert.That(BondEffects.whistle_answer_range_metres(0.0), Is.EqualTo(8.0));
            Assert.That(BondEffects.whistle_answer_range_metres(1.0), Is.EqualTo(20.0));
            Assert.That(BondEffects.wait_hold_seconds(0.5), Is.EqualTo(8.0));
            Assert.That(BondEffects.contact_seek_interval_seconds(0.0), Is.EqualTo(40.0));
            Assert.That(BondEffects.contact_seek_interval_seconds(1.0), Is.EqualTo(15.0));
        }

        // More trust is never worse, at every step from 0 to 1.
        [Test]
        public void more_trust_is_never_worse()
        {
            for (int step = 1; step <= 100; step += 1)
            {
                double lower = (step - 1) / 100.0;
                double higher = step / 100.0;
                Assert.That(BondEffects.whistle_answer_range_metres(higher), Is.GreaterThan(BondEffects.whistle_answer_range_metres(lower)));
                Assert.That(BondEffects.wait_hold_seconds(higher), Is.GreaterThan(BondEffects.wait_hold_seconds(lower)));
                Assert.That(BondEffects.contact_seek_interval_seconds(higher), Is.LessThan(BondEffects.contact_seek_interval_seconds(lower)));
            }
        }

        // Break it: trust outside 0 to 1, or not a number, gives the end values, never wild ones.
        [Test]
        public void break_it_odd_trust_values_stay_in_range()
        {
            Assert.That(BondEffects.whistle_answer_range_metres(7.0), Is.EqualTo(20.0));
            Assert.That(BondEffects.whistle_answer_range_metres(-7.0), Is.EqualTo(8.0));
            Assert.That(BondEffects.wait_hold_seconds(double.NaN), Is.EqualTo(4.0));
            Assert.That(BondEffects.contact_seek_interval_seconds(double.PositiveInfinity), Is.EqualTo(15.0));
        }
    }
}
