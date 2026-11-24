// Purpose: proves the back story told on New Game (BackStory): its cards come in order in Em's
// voice with his name filled in, each holds for its time and fades in and out, a tap moves on,
// holding skips them all, and broken input never crashes or gets stuck.
using Bodhi.Game;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class BackStoryTests
    {
        // Moves time on in small steps, like frames.
        static void wait(BackStory story, double seconds)
        {
            for (double passed = 0; passed < seconds - 1e-9; passed += 0.05)
            {
                story.pass_time(0.05);
            }
        }

        // Five to seven cards, each with words and a time to hold, ending on the drive home.
        [Test]
        public void the_cards_tell_the_back_story()
        {
            var cards = BackStory.cards;
            Assert.That(cards.Length, Is.InRange(5, 7));
            foreach (var card in cards)
            {
                Assert.That(card.text, Is.Not.Empty);
                Assert.That(card.hold_seconds, Is.GreaterThan(BackStory.fade_seconds * 2));
                Assert.That(card.picture, Is.Not.Null);
            }
            Assert.That(cards[cards.Length - 1].text, Is.EqualTo("He fell asleep before we reached the end of the road."));
        }

        // The first card shows straight away, fading in; his name is filled in.
        [Test]
        public void the_first_card_fades_in()
        {
            var story = new BackStory("Bodhi");
            Assert.That(story.card_index, Is.EqualTo(0));
            Assert.That(story.text, Is.EqualTo(BackStory.cards[0].text));
            Assert.That(story.picture, Is.EqualTo(BackStory.cards[0].picture));
            Assert.That(story.card_opacity, Is.EqualTo(0));
            wait(story, BackStory.fade_seconds);
            Assert.That(story.card_opacity, Is.EqualTo(1).Within(1e-9));
            Assert.That(story.is_finished, Is.False);
            var named = new BackStory("Bodhi");
            while (!named.is_finished)
            {
                Assert.That(named.text, Does.Not.Contain("{"), "every placeholder is filled");
                wait(named, 0.5);
            }
        }

        // Each card holds for its time, fades out at the end, then the next comes; after the last
        // the story is finished.
        [Test]
        public void cards_move_on_by_themselves()
        {
            var story = new BackStory("Bodhi");
            wait(story, BackStory.cards[0].hold_seconds - 0.1);
            Assert.That(story.card_index, Is.EqualTo(0));
            Assert.That(story.card_opacity, Is.LessThan(1), "fading out");
            wait(story, 0.2);
            Assert.That(story.card_index, Is.EqualTo(1));
            double total = 0;
            foreach (var card in BackStory.cards) total += card.hold_seconds;
            wait(story, total);
            Assert.That(story.is_finished, Is.True);
            Assert.That(story.text, Is.EqualTo(""));
            Assert.That(story.card_opacity, Is.EqualTo(0));
        }

        // A tap moves to the next card, but not in the first moment of a card, so the tap on New
        // Game can't skip the first card by accident.
        [Test]
        public void a_tap_moves_on()
        {
            var story = new BackStory("Bodhi");
            Assert.That(story.tap(), Is.False, "too soon");
            wait(story, BackStory.ignore_taps_for_seconds + 0.05);
            Assert.That(story.tap(), Is.True);
            Assert.That(story.card_index, Is.EqualTo(1));
            for (int index = 0; index < 20; index++)
            {
                wait(story, BackStory.ignore_taps_for_seconds + 0.05);
                story.tap();
            }
            Assert.That(story.is_finished, Is.True);
            Assert.That(story.tap(), Is.False, "nothing left to tap");
        }

        // Holding a finger down for a second skips the whole story.
        [Test]
        public void holding_skips_it_all()
        {
            var story = new BackStory("Bodhi");
            Assert.That(story.hold(0.5), Is.False);
            Assert.That(BackStory.hold_progress(0.5), Is.EqualTo(0.5 / BackStory.hold_to_skip_seconds).Within(1e-9));
            Assert.That(story.hold(BackStory.hold_to_skip_seconds), Is.True);
            Assert.That(story.is_finished, Is.True);
            Assert.That(story.hold(5), Is.False, "already finished");
        }

        // Break it: bad times, a missing name and absurd holds never crash or stick.
        [Test]
        public void broken_input_is_safe()
        {
            var story = new BackStory(null);
            Assert.That(story.text, Is.EqualTo(BackStory.cards[0].text.Replace("{dog}", "Bodhi")));
            story.pass_time(double.NaN);
            story.pass_time(-10);
            story.pass_time(double.NegativeInfinity);
            Assert.That(story.card_index, Is.EqualTo(0));
            Assert.That(story.hold(double.NaN), Is.False);
            Assert.That(story.hold(-1), Is.False);
            Assert.That(BackStory.hold_progress(double.NaN), Is.EqualTo(0));
            Assert.That(BackStory.hold_progress(1e300), Is.EqualTo(1));
            story.pass_time(double.PositiveInfinity);
            Assert.That(story.is_finished, Is.True, "a huge pause runs to the end");
            var paused = new BackStory("Bodhi");
            paused.pass_time(3600);
            Assert.That(paused.is_finished, Is.True);
            Assert.That(BackStory.cards, Is.Not.SameAs(BackStory.cards), "each caller gets its own copy");
        }
    }
}
