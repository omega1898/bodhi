// Purpose: proves the year-long bond between Em and Bodhi (BondJourney): ordinary care adds
// points, the same care on the same day gives less and then nothing (so it can't be farmed),
// points climb through five warmly named levels, each level unlocks a trick, and bad input
// (unknown kinds, broken days, huge numbers) is refused without changing anything.
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class BondJourneyTests
    {
        // A new journey starts at level 1, "Getting to know you", with sit ready to teach.
        [Test]
        public void a_new_journey_starts_at_getting_to_know_you()
        {
            var journey = new BondJourney();
            Assert.That(journey.points, Is.EqualTo(0));
            Assert.That(journey.level_number, Is.EqualTo(1));
            Assert.That(journey.level_name, Is.EqualTo("Getting to know you"));
            Assert.That(journey.progress_to_next_level, Is.EqualTo(0.0));
            Assert.That(journey.unlocks_earned(), Is.EqualTo(new[] { "sit" }));
            Assert.That(journey.trick_waiting_to_be_taught(), Is.EqualTo("sit"));
            Assert.That(journey.knows_any_trick, Is.False);
        }

        // Every kind of care has exactly one row in the tuning table.
        [Test]
        public void every_kind_of_care_has_a_rule()
        {
            foreach (BondCareKind kind in System.Enum.GetValues(typeof(BondCareKind)))
            {
                Assert.That(BondJourney.full_points_for(kind), Is.GreaterThan(0), kind.ToString());
            }
        }

        // Feeding him gives full points the first times in a day, then fewer, then none.
        [Test]
        public void the_same_care_gives_less_then_nothing_on_one_day()
        {
            var journey = new BondJourney();
            int full = BondJourney.full_points_for(BondCareKind.feed);
            var gains = new System.Collections.Generic.List<int>();
            for (int time = 0; time < 10; time++)
            {
                gains.Add(journey.record_care(BondCareKind.feed, 100).points_gained);
            }
            Assert.That(gains[0], Is.EqualTo(full));
            Assert.That(gains[1], Is.EqualTo(full));
            Assert.That(gains[2], Is.LessThan(full).And.GreaterThan(0));
            Assert.That(gains[9], Is.EqualTo(0));
            Assert.That(journey.times_recorded_today(BondCareKind.feed), Is.GreaterThan(0));
        }

        // The next day the full points come back; other kinds of care have their own allowance.
        [Test]
        public void a_new_day_gives_full_points_again()
        {
            var journey = new BondJourney();
            for (int time = 0; time < 10; time++)
            {
                journey.record_care(BondCareKind.feed, 100);
            }
            Assert.That(journey.record_care(BondCareKind.brush, 100).points_gained, Is.EqualTo(BondJourney.full_points_for(BondCareKind.brush)));
            Assert.That(journey.record_care(BondCareKind.feed, 101).points_gained, Is.EqualTo(BondJourney.full_points_for(BondCareKind.feed)));
        }

        // Break it: turning the phone's clock back doesn't open a fresh allowance. A day earlier
        // than the newest one seen counts against the newest day's allowance.
        [Test]
        public void turning_the_clock_back_grants_nothing_twice()
        {
            var journey = new BondJourney();
            for (int time = 0; time < 10; time++)
            {
                journey.record_care(BondCareKind.walk, 200);
            }
            int points = journey.points;
            for (int time = 0; time < 10; time++)
            {
                Assert.That(journey.record_care(BondCareKind.walk, 199).points_gained, Is.EqualTo(0));
                Assert.That(journey.record_care(BondCareKind.walk, 200).points_gained, Is.EqualTo(0));
            }
            Assert.That(journey.points, Is.EqualTo(points));
            Assert.That(journey.counting_day, Is.EqualTo(200));
        }

        // Points climb through the named levels; a level up says so and names the new trick.
        [Test]
        public void levels_rise_and_unlock_tricks()
        {
            var journey = new BondJourney();
            BondChange level_up = null;
            for (int chapter = 0; chapter < 3 && level_up == null; chapter++)
            {
                var change = journey.record_care(BondCareKind.chapter_finished, 1);
                if (change.levelled_up) level_up = change;
            }
            Assert.That(level_up, Is.Not.Null);
            Assert.That(level_up.level_after, Is.EqualTo(2));
            Assert.That(journey.level_name, Is.EqualTo("Friends"));
            Assert.That(level_up.new_unlocks, Is.EqualTo(new[] { "high_five" }));
            Assert.That(BondJourney.trick_display_name("high_five"), Is.EqualTo("High five"));
            Assert.That(journey.unlocks_earned(), Is.EqualTo(new[] { "sit", "high_five" }));
        }

        // Progress to the next level runs from 0 to 1 and the top level reads as full.
        [Test]
        public void progress_to_the_next_level_stays_between_zero_and_one()
        {
            var journey = new BondJourney();
            int day = 1;
            while (journey.level_number < BondJourney.highest_level)
            {
                double progress = journey.progress_to_next_level;
                Assert.That(progress, Is.InRange(0.0, 1.0));
                journey.record_care(BondCareKind.keepsake_found, day);
                day += 1;
            }
            Assert.That(journey.level_name, Is.EqualTo("Inseparable"));
            Assert.That(journey.progress_to_next_level, Is.EqualTo(1.0));
            Assert.That(journey.unlocks_earned(), Is.EqualTo(new[] { "sit", "high_five", "spin", "bow", "hug" }));
        }

        // Teaching goes through the unlocked tricks in order; once taught, he knows it.
        [Test]
        public void tricks_are_taught_in_order_once_unlocked()
        {
            var journey = new BondJourney();
            var first = journey.teach_next_trick(5);
            Assert.That(first.counted, Is.True);
            Assert.That(first.newly_taught, Is.EqualTo("sit"));
            Assert.That(first.points_gained, Is.EqualTo(BondJourney.full_points_for(BondCareKind.trick)));
            Assert.That(journey.knows_trick("sit"), Is.True);
            Assert.That(journey.knows_any_trick, Is.True);
            Assert.That(journey.trick_waiting_to_be_taught(), Is.Null);
            var nothing_new = journey.teach_next_trick(5);
            Assert.That(nothing_new.counted, Is.False);
            Assert.That(nothing_new.newly_taught, Is.Null);
        }

        // A finished care moment maps onto the kind of care it is.
        [Test]
        public void care_moments_map_to_kinds_of_care()
        {
            Assert.That(BondJourney.kind_for_care_moment(CareMomentKind.feeding), Is.EqualTo(BondCareKind.feed));
            Assert.That(BondJourney.kind_for_care_moment(CareMomentKind.drying), Is.EqualTo(BondCareKind.towel_or_bath));
            Assert.That(BondJourney.kind_for_care_moment(CareMomentKind.bathing), Is.EqualTo(BondCareKind.towel_or_bath));
            Assert.That(BondJourney.kind_for_care_moment(CareMomentKind.grooming), Is.EqualTo(BondCareKind.brush));
        }

        // Break it: unknown kinds and broken days are refused, counted, and change nothing.
        [Test]
        public void bad_input_is_refused_and_changes_nothing()
        {
            var journey = new BondJourney();
            journey.record_care(BondCareKind.pet, 50);
            string before = journey.to_text();
            var bad_calls = new System.Func<BondChange>[]
            {
                () => journey.record_care((BondCareKind)99, 50),
                () => journey.record_care((BondCareKind)(-1), 50),
                () => journey.record_care(BondCareKind.pet, -1),
                () => journey.record_care(BondCareKind.pet, int.MinValue),
                () => journey.record_care(BondCareKind.pet, int.MaxValue),
                () => journey.record_care(BondCareKind.pet, BondJourney.largest_day + 1),
                () => journey.teach_next_trick(-5),
            };
            foreach (var call in bad_calls)
            {
                var change = call();
                Assert.That(change.counted, Is.False);
                Assert.That(change.points_gained, Is.EqualTo(0));
            }
            Assert.That(journey.refused_count, Is.EqualTo(bad_calls.Length));
            Assert.That(journey.to_text(), Is.EqualTo(before));
            Assert.That(BondJourney.trick_display_name(null), Is.Null);
            Assert.That(BondJourney.trick_display_name("fly"), Is.Null);
            Assert.That(journey.knows_trick(null), Is.False);
        }

        // Break it: years of play can't push the points past their ceiling or wrap them negative.
        [Test]
        public void points_stop_at_their_ceiling()
        {
            var journey = new BondJourney();
            Assert.That(journey.load_text("bond " + (BondJourney.largest_points - 1) + " 1 0 0 0 0 0 0 0 0 0 0\n"), Is.True);
            var change = journey.record_care(BondCareKind.chapter_finished, 2);
            Assert.That(journey.points, Is.EqualTo(BondJourney.largest_points));
            Assert.That(change.points_gained, Is.EqualTo(1));
            Assert.That(journey.record_care(BondCareKind.chapter_finished, 3).points_gained, Is.EqualTo(0));
        }

        // The journey saves as text and loads back the same, allowances and tricks included.
        [Test]
        public void the_journey_survives_a_save()
        {
            var journey = new BondJourney();
            journey.teach_next_trick(7);
            journey.record_care(BondCareKind.feed, 7);
            journey.record_care(BondCareKind.feed, 7);
            var loaded = new BondJourney();
            Assert.That(loaded.load_text(journey.to_text()), Is.True);
            Assert.That(loaded.to_text(), Is.EqualTo(journey.to_text()));
            Assert.That(loaded.knows_trick("sit"), Is.True);
            Assert.That(loaded.times_recorded_today(BondCareKind.feed), Is.EqualTo(2));
            Assert.That(loaded.record_care(BondCareKind.feed, 7).points_gained, Is.LessThan(BondJourney.full_points_for(BondCareKind.feed)));
        }

        // Break it: a damaged journey save is refused whole and changes nothing.
        [Test]
        public void a_damaged_journey_save_is_refused_whole()
        {
            string[] bad_texts =
            {
                null,
                "",
                "bond 10 5 0 0 0 0 0 0 0 0 0 0",                      // Cut short: no line break.
                "bond 10 5 0 0 0 0 0 0 0 0 0\n",                     // A count missing.
                "bond 10 5 0 0 0 0 0 0 0 0 0 0 0\n",                 // A count too many.
                "bond -10 5 0 0 0 0 0 0 0 0 0 0\n",                  // Negative points.
                "bond NaN 5 0 0 0 0 0 0 0 0 0 0\n",
                "bond 99999999999 5 0 0 0 0 0 0 0 0 0 0\n",          // Too big for a number.
                "bond 2000000 5 0 0 0 0 0 0 0 0 0 0\n",              // Above the ceiling.
                "bond 10 -5 0 0 0 0 0 0 0 0 0 0\n",                  // Negative day.
                "bond 10 5 99 0 0 0 0 0 0 0 0 0\n",                  // More than the day's allowance.
                "bond 10 5 0 0 0 0 0 0 0 0 0 0\ntrick fly\n",        // No such trick.
                "bond 10 5 0 0 0 0 0 0 0 0 0 0\ntrick spin\n",       // Not unlocked at 10 points.
                "bond 10 5 0 0 0 0 0 0 0 0 0 0\ntrick sit\ntrick sit\n",
                "bond 10 5 0 0 0 0 0 0 0 0 0 0\nteleport\n",
                "trick sit\nbond 10 5 0 0 0 0 0 0 0 0 0 0\n",
                new string('x', 5000) + "\n",
            };
            foreach (var bad in bad_texts)
            {
                var journey = new BondJourney();
                journey.record_care(BondCareKind.walk, 3);
                string before = journey.to_text();
                Assert.That(journey.load_text(bad), Is.False, bad ?? "null");
                Assert.That(journey.to_text(), Is.EqualTo(before));
            }
        }
    }
}
