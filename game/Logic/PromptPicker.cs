// Purpose: decides what the "use" button offers right now, the words shown above it, and what
// pressing it does. It is the browser game's find_current_prompt and use_action
// (prototype/web/js/game.js), kept out of Unity so it can be tested. In order of importance:
// the reunion (kneel, then hold him), the ball, the nearest thing you can use, looking after
// him (feed, towel), and last, side by side, petting him (or, as him, leaning in).
//
// Positions are layout units with y pointing down, like the chapters. The scene fills in a
// PromptView each frame from where the two of them stand.
using System;
using Bodhi.Rules;

namespace Bodhi.Game
{
    // What pressing the button would do.
    public enum PromptAction
    {
        none,
        kneel,         // The reunion: she kneels as he runs to her (on the low button).
        hold,          // The reunion: she holds him, and the embrace begins.
        throw_ball,    // She throws the ball she's holding (the scene launches it).
        use_thing,     // Uses the thing named in thing_event.
        pick_up_ball,  // She picks up the ball (the scene puts it in her hand).
        feed,          // A meal from the bag.
        towel,         // Towels the mud off.
        pet,           // She pets him.
        lean,          // He leans into her.
    }

    // What the button offers: its action, its words and where to show them.
    public struct Prompt
    {
        public PromptAction action;
        public string text;          // Already personalised ("Pet Bodhi"). Empty when none.
        public double x, y;          // Where the words go, in the layout.
        public string thing_event;   // For use_thing: which thing.
        public bool on_low_button;   // Shown on the low button (kneel/sniff) instead of "use".
    }

    // Where the two of them (and the ball) are this frame.
    public struct PromptView
    {
        public double human_x, human_feet_y, human_height;
        public double dog_x, dog_feet_y, dog_height;
        public bool human_kneeling;
        public bool ball_exists;         // This chapter has the ball.
        public bool ball_held_by_human;
        public bool ball_held_by_dog;
        public double ball_x, ball_y;    // The ball's centre, when nobody holds it.
    }

    public static class PromptPicker
    {
        public const double reach_across = 56;          // How close she (or he) must be to use a thing...
        public const double reach_up_down = 70;         // ...measured from the feet.
        public const double prompt_above_thing = 84;    // The words float this far above it.
        public const double dog_near_across = 150;      // "He's right there", for a bowl or a bath...
        public const double dog_near_up_down = 40;      // ...on her level.
        public const double together_across = 70;       // Side by side, for petting and care...
        public const double together_up_down = 30;
        public const double reunion_close = 150;        // Close enough in the reunion to kneel.
        public const double ball_reach = 40;

        // What the button offers now. Never null: "nothing" is an action of its own.
        public static Prompt pick(ChapterRun run, PromptView view, string dog_name)
        {
            var nothing = new Prompt { action = PromptAction.none, text = "" };
            if (run == null)
            {
                return nothing;
            }
            bool as_her = run.controlled_character == Character.human;
            double me_x = as_her ? view.human_x : view.dog_x;
            double me_feet = as_her ? view.human_feet_y : view.dog_feet_y;
            double me_top = me_feet - (as_her ? view.human_height : view.dog_height);
            double dog_top = view.dog_feet_y - view.dog_height;

            // A film, the embrace or the chapter's end: nothing to press.
            if (run.controls_locked)
            {
                return nothing;
            }
            // The reunion: he runs to her (the run puts the player in her shoes).
            if (run.reunion_stage == ReunionStage.approach)
            {
                if (!(Math.Abs(view.dog_x - view.human_x) < reunion_close))
                {
                    return nothing;
                }
                return view.human_kneeling
                    ? new Prompt { action = PromptAction.hold, text = StoryText.personalize("Hold {dog}", dog_name), x = view.human_x, y = me_top - 4 }
                    : new Prompt { action = PromptAction.kneel, text = "Kneel", x = view.human_x, y = me_top - 20, on_low_button = true };
            }
            if (as_her && view.ball_exists && view.ball_held_by_human)
            {
                return new Prompt { action = PromptAction.throw_ball, text = "Throw the ball", x = me_x, y = me_top - 20 };
            }

            ChapterThing nearest = null;
            foreach (var thing in run.chapter.things ?? Array.Empty<ChapterThing>())
            {
                if (thing == null || !(Math.Abs(thing.x - me_x) < reach_across) || !(Math.Abs(thing.y - me_feet) < reach_up_down))
                {
                    continue;
                }
                if (!run.can_use(thing.event_name, dog_is_near(view, thing.x)))
                {
                    continue;
                }
                if (nearest == null || Math.Abs(thing.x - me_x) < Math.Abs(nearest.x - me_x))
                {
                    nearest = thing;
                }
            }
            if (nearest != null)
            {
                return new Prompt
                {
                    action = PromptAction.use_thing, thing_event = nearest.event_name,
                    text = StoryText.personalize(nearest.label, dog_name), x = nearest.x, y = nearest.y - prompt_above_thing,
                };
            }

            bool ball_loose = view.ball_exists && !view.ball_held_by_human && !view.ball_held_by_dog;
            if (as_her && ball_loose && Math.Abs(view.ball_x - me_x) < ball_reach && Math.Abs(view.ball_y - me_feet) < ball_reach)
            {
                return new Prompt { action = PromptAction.pick_up_ball, text = "Pick up the ball", x = view.ball_x, y = view.ball_y - 40 };
            }

            bool side_by_side = together(view);
            var offer = run.care.offered_care(side_by_side, run.controlled_character, run.cutscene_playing);
            if (offer == CareOffer.feed)
            {
                return new Prompt { action = PromptAction.feed, text = StoryText.personalize("Feed {dog}", dog_name), x = view.dog_x, y = dog_top - 26 };
            }
            if (offer == CareOffer.towel)
            {
                return new Prompt { action = PromptAction.towel, text = StoryText.personalize("Towel {dog} clean", dog_name), x = view.dog_x, y = dog_top - 26 };
            }
            if (side_by_side && as_her)
            {
                return new Prompt { action = PromptAction.pet, text = StoryText.personalize("Pet {dog}", dog_name), x = view.dog_x, y = dog_top - 26 };
            }
            if (side_by_side)
            {
                double human_top = view.human_feet_y - view.human_height;
                return new Prompt { action = PromptAction.lean, text = "Lean in", x = view.human_x, y = human_top - 18 };
            }
            return nothing;
        }

        // Does what the prompt offers. True if something happened. For the ball, the run hears
        // about a throw and the scene moves the ball itself.
        public static bool act(ChapterRun run, Prompt prompt, PromptView view)
        {
            if (run == null)
            {
                return false;
            }
            switch (prompt.action)
            {
                case PromptAction.kneel:
                    return run.kneel(dog_is_near(view, view.human_x));
                case PromptAction.hold:
                    return run.hold_dog(Math.Abs(view.dog_x - view.human_x) < reunion_close);
                case PromptAction.throw_ball:
                    return !run.controls_locked && run.controlled_character == Character.human && (run.report_event("ball_thrown") || run.has_happened("ball_thrown"));
                case PromptAction.pick_up_ball:
                    return !run.controls_locked && run.controlled_character == Character.human;
                case PromptAction.use_thing:
                    var thing = find_thing(run, prompt.thing_event);
                    return thing != null && run.use(thing.event_name, dog_is_near(view, thing.x));
                case PromptAction.feed:
                    return run.give_care(CareOffer.feed, together(view));
                case PromptAction.towel:
                    return run.give_care(CareOffer.towel, together(view));
                case PromptAction.pet:
                case PromptAction.lean:
                    return together(view) && run.pet();
                default:
                    return false;
            }
        }

        // True if he's within 150 of this x and on her level (for a bowl, a bath, his bed).
        public static bool dog_is_near(PromptView view, double x)
        {
            return Math.Abs(view.dog_x - x) < dog_near_across && Math.Abs(view.dog_feet_y - view.human_feet_y) < dog_near_up_down;
        }

        // True if they're side by side.
        public static bool together(PromptView view)
        {
            return Math.Abs(view.dog_x - view.human_x) < together_across && Math.Abs(view.dog_feet_y - view.human_feet_y) < together_up_down;
        }

        // The chapter's thing with this event, or null.
        static ChapterThing find_thing(ChapterRun run, string event_name)
        {
            if (string.IsNullOrEmpty(event_name))
            {
                return null;
            }
            foreach (var thing in run.chapter.things ?? Array.Empty<ChapterThing>())
            {
                if (thing != null && thing.event_name == event_name)
                {
                    return thing;
                }
            }
            return null;
        }
    }
}
