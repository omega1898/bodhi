// Purpose: proves what the "use" button offers at each moment (the words above it and what
// pressing it does), the browser game's find_current_prompt and use_action. Also proves the new
// care from the bag: feeding him and towelling the mud off when the game offers it.
using System;
using Bodhi.Game;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class PromptPickerTests
    {
        // A field: a gate she opens, a bone only he can use, a hidden toy, and a bowl that needs him close.
        static ChapterScript field()
        {
            return new ChapterScript
            {
                id = "test_field",
                start_hunger = 0.2,
                start_food = 2,
                things = new[]
                {
                    new ChapterThing { event_name = "gate_opened", who = Character.human, label = "Open the gate for {dog}", x = 400, y = 500 },
                    new ChapterThing { event_name = "far_gate_opened", who = Character.human, label = "Open the far gate", x = 440, y = 500 },
                    new ChapterThing { event_name = "bone_found", who = Character.dog, label = "Dig", x = 400, y = 500 },
                    new ChapterThing { event_name = "bowl_used", who = Character.human, label = "Fill the bowl", needs_dog = true, care = CareKind.meal, x = 800, y = 500 },
                    new ChapterThing { event_name = "high_shelf", who = Character.human, label = "Reach up", x = 1200, y = 380 },
                },
                rules = new[]
                {
                    new StoryRule { on = "level_started", switching = true },
                    new StoryRule { on = "gate_opened", objective = "Through." },
                },
            };
        }

        static ChapterRun started(ChapterScript script = null)
        {
            var run = new ChapterRun(script ?? field());
            run.start();
            return run;
        }

        // Her at x, him at dog_x, both standing on y 500.
        static PromptView standing(double human_x, double dog_x)
        {
            return new PromptView
            {
                human_x = human_x, human_feet_y = 500, human_height = 86,
                dog_x = dog_x, dog_feet_y = 500, dog_height = 38,
            };
        }

        // Nothing near: nothing offered.
        [Test]
        public void nothing_near_offers_nothing()
        {
            var prompt = PromptPicker.pick(started(), standing(100, 600), "Bodhi");
            Assert.That(prompt.action, Is.EqualTo(PromptAction.none));
            Assert.That(prompt.text, Is.EqualTo(""));
        }

        // Near a thing she can use: its label, with his name filled in, above the thing.
        [Test]
        public void a_thing_in_reach_is_offered_with_its_label()
        {
            var prompt = PromptPicker.pick(started(), standing(420, 1000), "Bodhi");
            Assert.That(prompt.action, Is.EqualTo(PromptAction.use_thing));
            Assert.That(prompt.thing_event, Is.EqualTo("gate_opened"), "the nearer of the two gates");
            Assert.That(prompt.text, Is.EqualTo("Open the gate for Bodhi"));
            Assert.That(prompt.x, Is.EqualTo(400));
            Assert.That(prompt.y, Is.EqualTo(500 - 84));
            Assert.That(PromptPicker.pick(started(), standing(435, 1000), "Bodhi").thing_event, Is.EqualTo("far_gate_opened"));
        }

        // Reach: 56 across and 70 up or down, as in the browser game.
        [Test]
        public void reach_has_edges()
        {
            Assert.That(PromptPicker.pick(started(), standing(455, 1000), "B").action, Is.EqualTo(PromptAction.use_thing), "15 from the far gate");
            Assert.That(PromptPicker.pick(started(), standing(497, 1000), "B").action, Is.EqualTo(PromptAction.none), "57 from the far gate");
            Assert.That(PromptPicker.pick(started(), standing(1200, 100), "B").action, Is.EqualTo(PromptAction.none), "the shelf is 120 up");
            var low = standing(1200, 100);
            low.human_feet_y = 449;
            Assert.That(PromptPicker.pick(started(), low, "B").thing_event, Is.EqualTo("high_shelf"), "69 up is in reach");
        }

        // Things are for whoever the player is: as him, the bone, not her gate.
        [Test]
        public void playing_him_offers_his_things()
        {
            var run = started();
            Assert.That(run.switch_character(), Is.True);
            var view = standing(100, 410);
            var prompt = PromptPicker.pick(run, view, "B");
            Assert.That(prompt.thing_event, Is.EqualTo("bone_found"));
            Assert.That(PromptPicker.act(run, prompt, view), Is.True);
            Assert.That(run.has_happened("bone_found"), Is.True);
        }

        // A thing that needs him close is only offered when he's within 150 and on her level.
        [Test]
        public void a_bowl_needs_him_close()
        {
            Assert.That(PromptPicker.pick(started(), standing(800, 1000), "B").action, Is.Not.EqualTo(PromptAction.use_thing));
            Assert.That(PromptPicker.pick(started(), standing(800, 900), "B").thing_event, Is.EqualTo("bowl_used"));
            var high = standing(800, 900);
            high.dog_feet_y = 450;
            Assert.That(PromptPicker.pick(started(), high, "B").action, Is.Not.EqualTo(PromptAction.use_thing), "he's on a ledge above her");
        }

        // Pressing use on a thing uses it, once.
        [Test]
        public void acting_on_a_thing_uses_it()
        {
            var run = started();
            var view = standing(420, 1000);
            var prompt = PromptPicker.pick(run, view, "B");
            Assert.That(PromptPicker.act(run, prompt, view), Is.True);
            Assert.That(run.has_happened("gate_opened"), Is.True);
            Assert.That(PromptPicker.act(run, prompt, view), Is.False, "used up");
        }

        // Side by side with nothing else to do: she pets him; as him, he leans in.
        [Test]
        public void together_she_pets_him_and_he_leans_in()
        {
            var run = started();
            var view = standing(100, 150);
            var prompt = PromptPicker.pick(run, view, "Bodhi");
            Assert.That(prompt.action, Is.EqualTo(PromptAction.pet));
            Assert.That(prompt.text, Is.EqualTo("Pet Bodhi"));
            Assert.That(prompt.x, Is.EqualTo(150));
            Assert.That(prompt.y, Is.EqualTo(500 - 38 - 26));
            Assert.That(PromptPicker.act(run, prompt, view), Is.True);
            Assert.That(run.has_happened("dog_petted"), Is.True);

            run.switch_character();
            var lean = PromptPicker.pick(run, view, "Bodhi");
            Assert.That(lean.action, Is.EqualTo(PromptAction.lean));
            Assert.That(lean.text, Is.EqualTo("Lean in"));
            Assert.That(lean.x, Is.EqualTo(100));
            Assert.That(lean.y, Is.EqualTo(500 - 86 - 18));
            Assert.That(PromptPicker.act(run, lean, view), Is.True);
            Assert.That(run.has_happened("dog_leaned_in"), Is.True);
        }

        // "Together" is within 70 across and 30 up or down.
        [Test]
        public void together_has_edges()
        {
            Assert.That(PromptPicker.pick(started(), standing(100, 169), "B").action, Is.EqualTo(PromptAction.pet));
            Assert.That(PromptPicker.pick(started(), standing(100, 171), "B").action, Is.EqualTo(PromptAction.none));
            var up = standing(100, 120);
            up.dog_feet_y = 469;
            Assert.That(PromptPicker.pick(started(), up, "B").action, Is.EqualTo(PromptAction.none));
            Assert.That(PromptPicker.together(standing(100, 120)), Is.True);
        }

        // Hungry with food in the bag: feeding comes before petting, and uses a meal.
        [Test]
        public void a_hungry_dog_is_fed_from_the_bag()
        {
            var script = field();
            script.start_hunger = 0.6;
            var run = started(script);
            var view = standing(100, 150);
            var prompt = PromptPicker.pick(run, view, "Bodhi");
            Assert.That(prompt.action, Is.EqualTo(PromptAction.feed));
            Assert.That(prompt.text, Is.EqualTo("Feed Bodhi"));
            double fear_before = run.bond.fear = 0.5;
            double closeness_before = run.bond.closeness_need;
            Assert.That(PromptPicker.act(run, prompt, view), Is.True);
            Assert.That(run.care.hunger, Is.EqualTo(0));
            Assert.That(run.care.food, Is.EqualTo(1));
            Assert.That(run.bond.fear, Is.EqualTo(fear_before - 0.1).Within(1e-9));
            Assert.That(run.bond.closeness_need, Is.EqualTo(closeness_before + 0.05).Within(1e-9));
            Assert.That(run.has_happened("dog_fed"), Is.True);
            Assert.That(PromptPicker.pick(run, view, "Bodhi").action, Is.EqualTo(PromptAction.pet), "fed now");
        }

        // Muddy: towelling is offered, takes most of the mud off, and brings him a little closer.
        [Test]
        public void a_muddy_dog_is_towelled()
        {
            var script = field();
            script.start_dirt = 0.8;
            var run = started(script);
            double closeness_before = run.bond.closeness_need;
            var view = standing(100, 150);
            var prompt = PromptPicker.pick(run, view, "Bodhi");
            Assert.That(prompt.action, Is.EqualTo(PromptAction.towel));
            Assert.That(prompt.text, Is.EqualTo("Towel Bodhi clean"));
            Assert.That(PromptPicker.act(run, prompt, view), Is.True);
            Assert.That(run.care.dirt, Is.EqualTo(0.05).Within(1e-9));
            Assert.That(run.bond.closeness_need, Is.EqualTo(closeness_before + 0.03).Within(1e-9));
            Assert.That(run.has_happened("dog_cleaned"), Is.True);
        }

        // Break it: care that isn't on offer changes nothing (apart, an empty bag, or in a film).
        [Test]
        public void care_not_on_offer_changes_nothing()
        {
            var script = field();
            script.start_hunger = 0.6;
            script.start_food = 0;
            var run = started(script);
            Assert.That(run.give_care(CareOffer.feed, together: true), Is.False, "empty bag");
            Assert.That(run.care.hunger, Is.EqualTo(0.6));

            var fed = started(field());
            Assert.That(fed.give_care(CareOffer.feed, together: true), Is.False, "not hungry yet");
            Assert.That(fed.give_care(CareOffer.towel, together: true), Is.False, "not muddy");
            Assert.That(fed.give_care(CareOffer.none, together: true), Is.False);

            var hungry = field();
            hungry.start_hunger = 0.9;
            var apart = started(hungry);
            Assert.That(apart.give_care(CareOffer.feed, together: false), Is.False);
            Assert.That(apart.care.food, Is.EqualTo(2));

            var as_him = started(hungry);
            as_him.switch_character();
            Assert.That(as_him.give_care(CareOffer.feed, together: true), Is.False, "only she feeds him");
        }

        // Feeding twice in a chapter still feeds him the second time (the event only counts once).
        [Test]
        public void feeding_again_still_feeds()
        {
            var script = field();
            script.start_hunger = 0.9;
            var run = started(script);
            Assert.That(run.give_care(CareOffer.feed, together: true), Is.True);
            run.care.update(600, Surface.dry_ground, 1, false);
            Assert.That(run.care.hunger, Is.GreaterThan(CareState.offer_food_from));
            Assert.That(run.give_care(CareOffer.feed, together: true), Is.True);
            Assert.That(run.care.food, Is.EqualTo(0));
            Assert.That(run.care.hunger, Is.EqualTo(0));
        }

        // The scene can't claim he was fed: that's the run's own event.
        [Test]
        public void the_scene_cannot_fake_care()
        {
            var run = started();
            Assert.That(run.report_event("dog_fed"), Is.False);
            Assert.That(run.report_event("dog_cleaned"), Is.False);
        }

        // The ball: holding it, she can throw; near it on the ground, she can pick it up.
        [Test]
        public void the_ball_comes_before_things()
        {
            var run = started();
            var holding = standing(420, 1000);
            holding.ball_exists = true;
            holding.ball_held_by_human = true;
            var prompt = PromptPicker.pick(run, holding, "B");
            Assert.That(prompt.action, Is.EqualTo(PromptAction.throw_ball));
            Assert.That(prompt.text, Is.EqualTo("Throw the ball"));
            Assert.That(PromptPicker.act(run, prompt, holding), Is.True);
            Assert.That(run.has_happened("ball_thrown"), Is.True);

            var near = standing(100, 1000);
            near.ball_exists = true;
            near.ball_x = 130;
            near.ball_y = 495;
            Assert.That(PromptPicker.pick(run, near, "B").action, Is.EqualTo(PromptAction.pick_up_ball));
            near.ball_x = 141;
            Assert.That(PromptPicker.pick(run, near, "B").action, Is.EqualTo(PromptAction.none));
            near.ball_x = 130;
            near.ball_held_by_dog = true;
            Assert.That(PromptPicker.pick(run, near, "B").action, Is.EqualTo(PromptAction.none), "he has it");
        }

        // The reunion: close to him she kneels (on the low button), then holds him.
        [Test]
        public void the_reunion_offers_kneel_then_hold()
        {
            var script = field();
            script.rules = new[] { new StoryRule { on = "level_started", reunion = true } };
            var run = started(script);
            var far = standing(100, 400);
            Assert.That(PromptPicker.pick(run, far, "B").action, Is.EqualTo(PromptAction.none));
            var close = standing(100, 240);
            var kneel = PromptPicker.pick(run, close, "B");
            Assert.That(kneel.action, Is.EqualTo(PromptAction.kneel));
            Assert.That(kneel.on_low_button, Is.True);
            Assert.That(kneel.text, Is.EqualTo("Kneel"));
            Assert.That(PromptPicker.act(run, kneel, close), Is.True);
            close.human_kneeling = true;
            var hold = PromptPicker.pick(run, close, "Bodhi");
            Assert.That(hold.action, Is.EqualTo(PromptAction.hold));
            Assert.That(hold.text, Is.EqualTo("Hold Bodhi"));
            Assert.That(hold.on_low_button, Is.False);
            Assert.That(hold.y, Is.EqualTo(500 - 86 - 4));
            Assert.That(PromptPicker.act(run, hold, far), Is.False, "he isn't there yet");
            Assert.That(PromptPicker.act(run, hold, close), Is.True);
            Assert.That(run.reunion_stage, Is.EqualTo(ReunionStage.embrace));
            Assert.That(PromptPicker.pick(run, close, "B").action, Is.EqualTo(PromptAction.none), "nothing during the embrace");
        }

        // Break it: a film, a finished chapter, or a prompt the moment has passed offers and does nothing.
        [Test]
        public void locked_moments_offer_nothing()
        {
            var script = field();
            script.rules = new[] { new StoryRule { on = "level_started", cutscene = CutsceneChange.start } };
            var run = started(script);
            Assert.That(PromptPicker.pick(run, standing(420, 450), "B").action, Is.EqualTo(PromptAction.none));
            var stale = new Prompt { action = PromptAction.use_thing, thing_event = "gate_opened" };
            Assert.That(PromptPicker.act(run, stale, standing(420, 450)), Is.False);
            Assert.That(PromptPicker.act(run, new Prompt(), standing(420, 450)), Is.False);
            Assert.That(PromptPicker.act(null, stale, standing(420, 450)), Is.False);
            Assert.That(PromptPicker.pick(null, standing(1, 1), "B").action, Is.EqualTo(PromptAction.none));
        }

        // Only she throws or picks up the ball, and never in a film; a second throw still counts.
        [Test]
        public void only_she_handles_the_ball()
        {
            var run = started();
            var view = standing(100, 1000);
            view.ball_exists = true;
            view.ball_held_by_human = true;
            var throw_it = new Prompt { action = PromptAction.throw_ball };
            Assert.That(PromptPicker.act(run, throw_it, view), Is.True);
            Assert.That(PromptPicker.act(run, throw_it, view), Is.True, "the event counts once, the throws don't");
            run.switch_character();
            Assert.That(PromptPicker.pick(run, view, "B").action, Is.Not.EqualTo(PromptAction.throw_ball));
            Assert.That(PromptPicker.act(run, throw_it, view), Is.False);
            Assert.That(PromptPicker.act(run, new Prompt { action = PromptAction.pick_up_ball }, view), Is.False);
            var loose = standing(1000, 100);
            loose.ball_exists = true;
            loose.ball_x = 100;
            loose.ball_y = 495;
            Assert.That(PromptPicker.pick(run, loose, "B").action, Is.Not.EqualTo(PromptAction.pick_up_ball), "as him, his nose finds it, not a button");
            run.switch_character();
            Assert.That(PromptPicker.act(run, new Prompt { action = PromptAction.pick_up_ball }, loose), Is.True);
            var high = standing(100, 1000);
            high.ball_exists = true;
            high.ball_x = 110;
            high.ball_y = 440;
            Assert.That(PromptPicker.pick(run, high, "B").action, Is.EqualTo(PromptAction.none), "the ball is on a shelf above her");

            var script = field();
            script.rules = new[] { new StoryRule { on = "level_started", cutscene = CutsceneChange.start } };
            var film = started(script);
            Assert.That(PromptPicker.act(film, throw_it, view), Is.False);
            Assert.That(PromptPicker.act(film, new Prompt { action = PromptAction.pick_up_ball }, view), Is.False);
        }

        // Break it: a prompt from a moment ago does nothing once they've moved apart.
        [Test]
        public void a_stale_prompt_needs_them_together()
        {
            var script = field();
            script.start_hunger = 0.9;
            script.start_dirt = 0.9;
            var run = started(script);
            var apart = standing(100, 500);
            Assert.That(PromptPicker.act(run, new Prompt { action = PromptAction.pet }, apart), Is.False);
            Assert.That(PromptPicker.act(run, new Prompt { action = PromptAction.feed }, apart), Is.False);
            Assert.That(PromptPicker.act(run, new Prompt { action = PromptAction.towel }, apart), Is.False);
            Assert.That(run.care.food, Is.EqualTo(2));
            var together = standing(100, 150);
            Assert.That(PromptPicker.act(run, new Prompt { action = PromptAction.towel }, together), Is.False, "feeding comes first");
            Assert.That(PromptPicker.act(run, new Prompt { action = PromptAction.feed }, together), Is.True);
            Assert.That(PromptPicker.act(run, new Prompt { action = PromptAction.towel }, apart), Is.False, "muddy, fed, but apart");
            Assert.That(PromptPicker.act(run, new Prompt { action = PromptAction.towel }, together), Is.True);
        }

        // The low button outside the reunion: she kneels, and if he's by her he leans in.
        [Test]
        public void kneeling_by_him_counts()
        {
            var run = started();
            Assert.That(PromptPicker.act(run, new Prompt { action = PromptAction.kneel }, standing(100, 600)), Is.True);
            Assert.That(run.has_happened("knelt_by_dog"), Is.False);
            Assert.That(PromptPicker.act(run, new Prompt { action = PromptAction.kneel }, standing(100, 200)), Is.True);
            Assert.That(run.has_happened("knelt_by_dog"), Is.True);
        }

        // Break it: no feeding in the reunion or once the chapter has ended.
        [Test]
        public void no_care_in_the_reunion_or_after_the_end()
        {
            var script = field();
            script.start_hunger = 0.9;
            script.rules = new[] { new StoryRule { on = "level_started", reunion = true } };
            var reunion = started(script);
            Assert.That(reunion.give_care(CareOffer.feed, together: true), Is.False);
            Assert.That(PromptPicker.pick(reunion, standing(100, 400), "B").action, Is.EqualTo(PromptAction.none), "he is still running to her");

            var ending = field();
            ending.start_hunger = 0.9;
            ending.rules = new[] { new StoryRule { on = "level_started", complete = true } };
            var finished = started(ending);
            Assert.That(finished.is_complete, Is.True);
            Assert.That(finished.give_care(CareOffer.feed, together: true), Is.False);
            Assert.That(PromptPicker.pick(finished, standing(100, 150), "B").action, Is.EqualTo(PromptAction.none));
        }

        // Break it: positions that aren't numbers never offer anything.
        [Test]
        public void nonsense_positions_offer_nothing()
        {
            var view = standing(double.NaN, 150);
            Assert.That(PromptPicker.pick(started(), view, "B").action, Is.EqualTo(PromptAction.none));
            view = standing(420, double.NaN);
            Assert.That(PromptPicker.pick(started(), view, "B").action, Is.EqualTo(PromptAction.use_thing), "her gate doesn't need him");
        }
    }
}
