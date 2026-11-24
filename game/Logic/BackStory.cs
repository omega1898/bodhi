// Purpose: the back story told on New Game, before the first chapter: a few caption cards in
// Em's voice (first person, past tense, short sentences; docs/story.md, "Back story"). Each card
// fades in, holds, and fades out, then the next comes. A tap moves on; holding a finger down
// skips them all. The scene passes time in and draws `text`, `picture` and `card_opacity`.
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

        readonly BackStoryCard[] story_cards = cards;
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

        // Makes the story, filling {dog} with this name (Bodhi if none is given).
        public BackStory(string dog_name)
        {
            this.dog_name = string.IsNullOrEmpty(dog_name) ? "Bodhi" : dog_name;
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
