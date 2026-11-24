// Purpose: proves Bodhi's needs (hunger, dirt, food in the bag) behave like the web prototype's
// care.js, never punish the player, and survive damaged saved values.
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class CareStateTests
    {
        // A new game: a little hungry, clean, two meals in the bag.
        [Test]
        public void a_new_game_starts_a_little_hungry_and_clean()
        {
            var care = new CareState();
            Assert.That(care.hunger, Is.EqualTo(0.3));
            Assert.That(care.dirt, Is.EqualTo(0.0));
            Assert.That(care.food, Is.EqualTo(2));
        }

        // A grown dog goes from fed to fully hungry in 8 minutes; a puppy sooner.
        [Test]
        public void hunger_grows_with_time_and_faster_for_a_puppy()
        {
            var grown = new CareState(0.0, 0.0, 2);
            grown.update(4 * 60, Surface.dry_ground, 1.0, false);
            Assert.That(grown.hunger, Is.EqualTo(0.5).Within(1e-9));

            var puppy = new CareState(0.0, 0.0, 2);
            puppy.update(4 * 60, Surface.dry_ground, 0.5, false);
            Assert.That(puppy.hunger, Is.EqualTo(0.625).Within(1e-9));
        }

        // Scripted scenes pause his needs, so a film doesn't make him hungry.
        [Test]
        public void scripted_scenes_pause_his_needs()
        {
            var care = new CareState();
            care.update(600, Surface.mud, 1.0, true);
            Assert.That(care.hunger, Is.EqualTo(0.3));
            Assert.That(care.dirt, Is.EqualTo(0.0));
        }

        // Water makes him dirty slowly, mud quickly, dry ground not at all.
        [Test]
        public void water_and_mud_make_him_dirty()
        {
            var in_water = new CareState();
            in_water.update(5, Surface.water, 1.0, false);
            Assert.That(in_water.dirt, Is.EqualTo(0.4).Within(1e-9));

            var in_mud = new CareState();
            in_mud.update(2, Surface.mud, 1.0, false);
            Assert.That(in_mud.dirt, Is.EqualTo(0.4).Within(1e-9));

            var on_ground = new CareState();
            on_ground.update(60, Surface.dry_ground, 1.0, false);
            Assert.That(on_ground.dirt, Is.EqualTo(0.0));
        }

        // Hungry means a little slower, never stopped.
        [Test]
        public void a_hungry_dog_is_a_little_slower_never_stopped()
        {
            Assert.That(new CareState(0.69, 0, 2).speed_scale(), Is.EqualTo(1.0));
            Assert.That(new CareState(0.7, 0, 2).speed_scale(), Is.EqualTo(0.88));
        }

        // Feeding from the bag uses a meal and calms him; a meal at home uses none.
        [Test]
        public void feeding_uses_a_meal_and_calms_him()
        {
            var care = new CareState(0.8, 0, 2);
            var bond = new BondState { fear = 0.5 };
            Assert.That(care.feed(true, bond), Is.True);
            Assert.That(care.hunger, Is.EqualTo(0.0));
            Assert.That(care.food, Is.EqualTo(1));
            Assert.That(bond.fear, Is.EqualTo(0.4).Within(1e-9));

            var at_home = new CareState(0.8, 0, 0);
            Assert.That(at_home.feed(false, new BondState()), Is.True);
            Assert.That(at_home.food, Is.EqualTo(0));
        }

        // A towel takes off most of the mud; a bath takes off all of it.
        [Test]
        public void a_towel_cleans_most_a_bath_cleans_all()
        {
            var towelled = new CareState(0, 0.9, 2);
            towelled.clean(false);
            Assert.That(towelled.dirt, Is.EqualTo(0.15).Within(1e-9));

            var bathed = new CareState(0, 0.9, 2);
            bathed.clean(true);
            Assert.That(bathed.dirt, Is.EqualTo(0.0));
        }

        // A found treat takes half his hunger away, and never below zero.
        [Test]
        public void a_treat_takes_half_the_hunger()
        {
            var hungry = new CareState(0.8, 0, 2);
            hungry.eat_treat();
            Assert.That(hungry.hunger, Is.EqualTo(0.3).Within(1e-9));
            hungry.eat_treat();
            Assert.That(hungry.hunger, Is.EqualTo(0.0));
            Assert.That(hungry.food, Is.EqualTo(2));  // A treat doesn't come from the bag.
        }

        // Feed is offered only when he's peckish and there's food; towelling once mud shows;
        // never when they're apart, during a scene, or when the player is Bodhi.
        [Test]
        public void care_is_offered_only_when_it_makes_sense()
        {
            Assert.That(new CareState(0.5, 0, 1).offered_care(true, Character.human, false), Is.EqualTo(CareOffer.feed));
            Assert.That(new CareState(0.5, 0, 0).offered_care(true, Character.human, false), Is.EqualTo(CareOffer.none));
            Assert.That(new CareState(0.1, 0.6, 0).offered_care(true, Character.human, false), Is.EqualTo(CareOffer.towel));
            Assert.That(new CareState(0.5, 0.6, 1).offered_care(false, Character.human, false), Is.EqualTo(CareOffer.none));
            Assert.That(new CareState(0.5, 0.6, 1).offered_care(true, Character.dog, false), Is.EqualTo(CareOffer.none));
            Assert.That(new CareState(0.5, 0.6, 1).offered_care(true, Character.human, true), Is.EqualTo(CareOffer.none));
        }

        // Found food fills the bag up to 9 meals, and the rest is left behind.
        [Test]
        public void the_bag_holds_nine_meals()
        {
            var care = new CareState(0, 0, 7);
            Assert.That(care.pick_up_food(5), Is.EqualTo(2));
            Assert.That(care.food, Is.EqualTo(9));
        }

        // Break it: feeding from an empty bag does nothing, instead of going below zero.
        [Test]
        public void break_it_an_empty_bag_cannot_feed()
        {
            var care = new CareState(0.9, 0, 0);
            Assert.That(care.feed(true, new BondState()), Is.False);
            Assert.That(care.hunger, Is.EqualTo(0.9));
            Assert.That(care.food, Is.EqualTo(0));
        }

        // Break it: damaged saved values (not numbers, out of range) are pulled back to safe ones.
        [Test]
        public void break_it_damaged_saved_values_are_made_safe()
        {
            var care = new CareState(double.NaN, 5.0, -3);
            Assert.That(care.hunger, Is.EqualTo(0.3));
            Assert.That(care.dirt, Is.EqualTo(1.0));
            Assert.That(care.food, Is.EqualTo(0));
            Assert.That(new CareState(0, 0, 1000).food, Is.EqualTo(9));
        }

        // Break it: broken times (not a number, infinite, negative, huge) change nothing.
        [Test]
        public void break_it_broken_times_change_nothing()
        {
            var care = new CareState();
            care.update(double.NaN, Surface.mud, 1.0, false);
            care.update(double.PositiveInfinity, Surface.mud, 1.0, false);
            care.update(-10, Surface.mud, 1.0, false);
            care.update(1.0e9, Surface.mud, 1.0, false);
            Assert.That(care.hunger, Is.EqualTo(0.3));
            Assert.That(care.dirt, Is.EqualTo(0.0));
        }

        // Break it: odd found-food amounts (negative, not a number, huge) never break the bag.
        [Test]
        public void break_it_odd_food_amounts_are_safe()
        {
            var care = new CareState(0, 0, 2);
            Assert.That(care.pick_up_food(-4), Is.EqualTo(0));
            Assert.That(care.pick_up_food(double.NaN), Is.EqualTo(0));
            Assert.That(care.pick_up_food(1.0e12), Is.EqualTo(7));
            Assert.That(care.food, Is.EqualTo(9));
        }
    }
}
