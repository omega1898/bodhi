// Purpose: proves the subtitle pacing, ported from update_subtitles in the browser game: each
// line stays long enough to read, a short gap between lines, and lines that waited too long are
// dropped so the words on screen match what's happening now.
using System.Collections.Generic;
using Bodhi.Game;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class SubtitlePacerTests
    {
        // Moves time on in small steps, like frames.
        static void wait(SubtitlePacer pacer, double seconds)
        {
            for (double passed = 0; passed < seconds - 1e-9; passed += 0.05)
            {
                pacer.pass_time(0.05);
            }
        }

        static List<StoryLine> lines(params string[] texts)
        {
            var list = new List<StoryLine>();
            foreach (var text in texts) list.Add(new StoryLine(text, StoryVoice.her));
            return list;
        }

        // A line shows on the next frame, with his name filled in, for 2.2 s plus 0.055 s a letter.
        [Test]
        public void a_line_stays_long_enough_to_read()
        {
            var pacer = new SubtitlePacer("Bodhi");
            pacer.add(new List<StoryLine> { new StoryLine("Where is {dog}?", StoryVoice.dog) });
            Assert.That(pacer.showing, Is.False);
            pacer.pass_time(0.05);
            Assert.That(pacer.showing, Is.True);
            Assert.That(pacer.text, Is.EqualTo("Where is Bodhi?"));
            Assert.That(pacer.voice, Is.EqualTo(StoryVoice.dog));
            double expected = SubtitlePacer.base_seconds + "Where is Bodhi?".Length * SubtitlePacer.seconds_per_character;
            wait(pacer, expected - 0.1);
            Assert.That(pacer.showing, Is.True);
            wait(pacer, 0.2);
            Assert.That(pacer.showing, Is.False);
            Assert.That(pacer.text, Is.EqualTo(""));
        }

        // Lines come one at a time, with a short gap between them.
        [Test]
        public void lines_come_in_order_with_a_gap()
        {
            var pacer = new SubtitlePacer("Bodhi");
            pacer.add(lines("One.", "Two."));
            pacer.pass_time(0.05);
            Assert.That(pacer.text, Is.EqualTo("One."));
            wait(pacer, SubtitlePacer.base_seconds + 4 * SubtitlePacer.seconds_per_character + 0.05);
            Assert.That(pacer.showing, Is.False, "the gap");
            wait(pacer, SubtitlePacer.gap_seconds);
            Assert.That(pacer.text, Is.EqualTo("Two."));
        }

        // While a long line shows (8.8 s), two more wait. When it ends, more than 9 s later,
        // a newer line is waiting too, so both old ones are dropped.
        [Test]
        public void stale_lines_are_dropped_for_a_newer_one()
        {
            var pacer = new SubtitlePacer("Bodhi");
            pacer.add(lines(new string('a', 120), "Old.", "Also old."));
            pacer.pass_time(0.05);
            wait(pacer, 5);
            pacer.add(lines("New."));
            wait(pacer, 5);
            Assert.That(pacer.text, Is.EqualTo("New."));
        }

        // But the last line waiting is never dropped, however old: something always shows.
        [Test]
        public void the_last_waiting_line_is_kept()
        {
            var pacer = new SubtitlePacer("Bodhi");
            pacer.add(lines(new string('a', 120), "Old.", "Also old."));
            pacer.pass_time(0.05);
            wait(pacer, 10);
            Assert.That(pacer.text, Is.EqualTo("Also old."));
        }

        // Tapping moves on to the next line at once.
        [Test]
        public void tapping_skips_the_line()
        {
            var pacer = new SubtitlePacer("Bodhi");
            pacer.add(lines("One.", "Two."));
            pacer.pass_time(0.05);
            pacer.skip_line();
            pacer.pass_time(0.05);
            Assert.That(pacer.showing, Is.False);
            wait(pacer, SubtitlePacer.gap_seconds + 0.05);
            Assert.That(pacer.text, Is.EqualTo("Two."));
        }

        // Skipping a film scene clears every waiting line and the one on screen.
        [Test]
        public void clearing_empties_everything()
        {
            var pacer = new SubtitlePacer("Bodhi");
            pacer.add(lines("One.", "Two."));
            pacer.pass_time(0.05);
            pacer.clear();
            Assert.That(pacer.showing, Is.False);
            pacer.pass_time(0.05);
            Assert.That(pacer.showing, Is.False, "the second line was dropped too");
            wait(pacer, 5);
            Assert.That(pacer.showing, Is.False);
        }

        // Break it: bad times and missing lines change nothing.
        [Test]
        public void bad_input_changes_nothing()
        {
            var pacer = new SubtitlePacer(null);
            pacer.add(null);
            pacer.add(lines("{dog} sleeps."));
            pacer.pass_time(double.NaN);
            pacer.pass_time(-1);
            pacer.pass_time(double.PositiveInfinity);
            Assert.That(pacer.showing, Is.False);
            pacer.pass_time(0.05);
            Assert.That(pacer.text, Is.EqualTo("Bodhi sleeps."), "no name given: his own name");
        }
    }
}
