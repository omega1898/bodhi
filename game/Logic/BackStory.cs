// Purpose: the back story told on New Game, before the first chapter: caption cards, one story's
// set at a time. The flood story's are in Em's voice (docs/story.md, "Back story"); the Christmas
// story's start with a prologue told by a storyteller about old Tam on the moor, then Em's own
// opening (docs/christmas_script.md, section 2). Each card fades in, holds, and fades out, then
// the next comes. A tap moves on; holding a finger down skips them all. The scene passes time in
// and draws `text`, `picture` and `card_opacity`.
using System;
using Bodhi.Rules;

namespace Bodhi.Game
{
    // One card: its words, how long it stays, and the picture behind it ("" for none).
    public struct BackStoryCard
    {
        public string text;            // In her voice; {dog} becomes his name.
        public double hold_seconds;    // How long it stays, fades included.
        public string picture;         // A picture's key for the scene, like "green_door", or "".

        // Makes one card.
        public BackStoryCard(string text, double hold_seconds, string picture)
        {
            this.text = text;
            this.hold_seconds = hold_seconds;
            this.picture = picture;
        }
    }

    public class BackStory
    {
        public const double fade_seconds = 0.6;               // Each card fades in, and out, this long.
        public const double ignore_taps_for_seconds = 0.3;    // A tap this soon after a card starts is ignored.
        public const double hold_to_skip_seconds = 1.0;       // Holding a finger down this long skips it all.

        // The cards, in order. A fresh copy each time, so no caller can change them for another.
        public static BackStoryCard[] cards => new[]
        {
            new BackStoryCard("Our town sat low beside a wide, slow river.", 4.0, "river"),
            new BackStoryCard("My house was the one with the green door.", 3.5, "green_door"),
            new BackStoryCard("I had wanted a dog for as long as I could remember.", 4.0, "empty_bowl"),
            new BackStoryCard("Then a photograph came. A small, curly face.", 3.5, "photo"),
            new BackStoryCard("I already had his name: {dog}.", 3.5, "photo"),
            new BackStoryCard("On the first of May, I drove out to fetch him.", 4.0, "road"),
            new BackStoryCard("He fell asleep before we reached the end of the road.", 4.5, "asleep"),
        };

        // The Christmas story's cards: the prologue, "The Herder", then Em's opening.
        public static BackStoryCard[] christmas_cards => new[]
        {
            new BackStoryCard("The night before the first snow, the reindeer on Tam Rennie's hill would not settle.", 5.5, "moor_dusk"),
            new BackStoryCard("One by one they turned their tails to the north. Then all thirty, as one animal.", 5.5, "herd_turning"),
            new BackStoryCard("Tam had seen them do that once before. He had been twelve.", 4.5, "tam_at_gate"),
            new BackStoryCard("The winter of the Long White. The winter a dog called Moss brought him home.", 5.0, "old_photo"),
            new BackStoryCard("Down in Hollin, a few windows still kept a lamp lit. Out of habit, mostly.", 5.0, "valley_lights"),
            new BackStoryCard("Tam lit his own. 'A light for the hill,' he said, though there was no one to hear.", 6.0, "tam_window"),
            new BackStoryCard("Fifty years the hill had waited. It had been patient long enough.", 5.0, "black"),
            new BackStoryCard("The night of the blizzard, I whistled for him. He didn't come.", 5.0, "black"),
            new BackStoryCard("Hollin sits at the head of the valley, where the road gives up and the hill begins.", 5.5, "valley_dawn"),
            new BackStoryCard("Ours was the last cabin, under the pines. The porch creaked in two places.", 5.0, "cabin"),
            new BackStoryCard("{dog} was nine months old. Big paws. Bigger opinions.", 4.0, "asleep_by_stove"),
            new BackStoryCard("He had never seen snow.", 3.5, "window_flakes"),
            new BackStoryCard("On the fourteenth of December, he woke me up to show me.", 4.5, "window_morning"),
        };

        // A story's cards: the Christmas story's, or the flood story's.
        public static BackStoryCard[] cards_for(Story story)
        {
            return story == Story.christmas ? christmas_cards : cards;
        }

        readonly BackStoryCard[] story_cards;
        readonly string dog_name;
        double seconds_on_card = 0;

        // Which card shows, from 0; equal to the number of cards once finished.
        public int card_index { get; private set; } = 0;

        // True once every card has shown or the story was skipped: start chapter 1.
        public bool is_finished => card_index >= story_cards.Length;

        // The words on screen with his name filled in, or "" once finished.
        public string text => is_finished ? "" : StoryText.personalize(story_cards[card_index].text, dog_name);

        // The picture behind the words, or "" for none or once finished.
        public string picture => is_finished ? "" : story_cards[card_index].picture ?? "";

        // How strongly the card shows, 0 to 1: fading in, holding, fading out.
        public double card_opacity
        {
            get
            {
                if (is_finished)
                {
                    return 0;
                }
                double hold = story_cards[card_index].hold_seconds;
                double fading_in = seconds_on_card / fade_seconds;
                double fading_out = (hold - seconds_on_card) / fade_seconds;
                return Math.Max(0, Math.Min(1, Math.Min(fading_in, fading_out)));
            }
        }

        // Makes the flood story's back story, filling {dog} with this name (Bodhi if none is given).
        public BackStory(string dog_name) : this(dog_name, Story.flood)
        {
        }

        // Makes this story's back story, filling {dog} with this name (Bodhi if none is given).
        public BackStory(string dog_name, Story story)
        {
            this.dog_name = string.IsNullOrEmpty(dog_name) ? "Bodhi" : dog_name;
            story_cards = cards_for(story);
        }

        // Moves time on; a card whose time is up gives way to the next. Times that aren't above
        // zero are ignored; an endless time runs to the end.
        public void pass_time(double delta_seconds)
        {
            if (!(delta_seconds > 0))
            {
                return;
            }
            if (double.IsInfinity(delta_seconds))
            {
                skip_all();
                return;
            }
            seconds_on_card += delta_seconds;
            while (!is_finished && seconds_on_card >= story_cards[card_index].hold_seconds)
            {
                seconds_on_card -= story_cards[card_index].hold_seconds;
                card_index += 1;
            }
            if (is_finished)
            {
                seconds_on_card = 0;
            }
        }

        // A tap: moves on to the next card. False if it was too soon after the card began (so
        // the New Game tap can't skip a card) or the story is over.
        public bool tap()
        {
            if (is_finished || seconds_on_card < ignore_taps_for_seconds)
            {
                return false;
            }
            card_index += 1;
            seconds_on_card = 0;
            return true;
        }

        // A finger held down this long: once it reaches hold_to_skip_seconds, skips every card.
        // True if it skipped.
        public bool hold(double seconds_held)
        {
            if (is_finished || hold_progress(seconds_held) < 1)
            {
                return false;
            }
            skip_all();
            return true;
        }

        // How far a hold is toward skipping, 0 to 1, for a filling ring on screen.
        public static double hold_progress(double seconds_held)
        {
            if (!(seconds_held > 0))
            {
                return 0;
            }
            return Math.Min(1, seconds_held / hold_to_skip_seconds);
        }

        // Ends the story at once.
        public void skip_all()
        {
            card_index = story_cards.Length;
            seconds_on_card = 0;
        }
    }
}
