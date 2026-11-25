// Purpose: proves the start-up animation (OpeningTitle) shows the right thing at each moment:
// black first, then the starry sky and aurora, the camera tilting down to the snowfield, the
// name, the buttons, and a settled screen after. A tap skips straight to the settled title
// screen, never to a blank one. Broken times never crash or give an empty screen.
using Bodhi.Game;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class OpeningTitleTests
    {
        // At the very start the screen is black: nothing has faded up yet.
        [Test]
        public void it_starts_black()
        {
            var frame = new OpeningTitle().frame_at(0);
            Assert.That(frame.scene_opacity, Is.EqualTo(0));
            Assert.That(frame.sky_opacity, Is.EqualTo(0));
            Assert.That(frame.camera_tilt, Is.EqualTo(0), "the camera starts looking up at the sky");
            Assert.That(frame.title_opacity, Is.EqualTo(0));
            Assert.That(frame.buttons_opacity, Is.EqualTo(0));
            Assert.That(frame.buttons_ready, Is.False);
            Assert.That(frame.is_skippable, Is.True);
            Assert.That(frame.is_finished, Is.False);
        }

        // The beats come in order: the picture and sky, the camera tilting down, the name, the
        // subtitle, the buttons.
        [Test]
        public void the_beats_come_in_order()
        {
            var opening = new OpeningTitle();
            Assert.That(opening.frame_at(1.0).sky_opacity, Is.GreaterThan(0).And.LessThan(1), "the sky is fading up");
            Assert.That(opening.frame_at(2.5).sky_opacity, Is.EqualTo(1));
            Assert.That(opening.frame_at(2.5).scene_opacity, Is.EqualTo(1));
            Assert.That(opening.frame_at(2.5).camera_tilt, Is.EqualTo(0), "it lingers on the aurora first");
            Assert.That(opening.frame_at(4.4).camera_tilt, Is.GreaterThan(0).And.LessThan(1), "tilting down");
            Assert.That(opening.frame_at(6.3).camera_tilt, Is.EqualTo(1));
            Assert.That(opening.frame_at(5.5).title_opacity, Is.EqualTo(0));
            Assert.That(opening.frame_at(6.7).title_opacity, Is.EqualTo(1));
            Assert.That(opening.frame_at(6.7).subtitle_opacity, Is.LessThan(1), "the subtitle comes after the name");
            Assert.That(opening.frame_at(7.5).buttons_opacity, Is.EqualTo(0));
            Assert.That(opening.frame_at(8.5).buttons_opacity, Is.EqualTo(1));
            Assert.That(opening.frame_at(8.5).buttons_ready, Is.True);
        }

        // Once settled it stays settled: everything shows, for as long as the title is up.
        [Test]
        public void it_stays_settled()
        {
            var opening = new OpeningTitle();
            foreach (double seconds in new[] { 8.6, 30.0, 600.0 })
            {
                var frame = opening.frame_at(seconds);
                Assert.That(frame.is_finished, Is.True);
                Assert.That(frame.camera_tilt, Is.EqualTo(1));
                Assert.That(frame.title_opacity, Is.EqualTo(1));
                Assert.That(frame.buttons_ready, Is.True);
                Assert.That(frame.is_skippable, Is.False);
            }
        }

        // A tap skips to the settled title screen: everything showing, nothing blank, buttons ready.
        [Test]
        public void a_tap_skips_to_the_settled_title()
        {
            var opening = new OpeningTitle();
            Assert.That(opening.skip(0.2), Is.True);
            var frame = opening.frame_at(0.25);
            Assert.That(frame.scene_opacity, Is.EqualTo(1));
            Assert.That(frame.sky_opacity, Is.EqualTo(1));
            Assert.That(frame.camera_tilt, Is.EqualTo(1));
            Assert.That(frame.title_opacity, Is.EqualTo(1));
            Assert.That(frame.subtitle_opacity, Is.EqualTo(1));
            Assert.That(frame.buttons_opacity, Is.EqualTo(1));
            Assert.That(frame.buttons_ready, Is.True);
            Assert.That(frame.is_finished, Is.True);
            Assert.That(opening.skip(0.3), Is.False, "already skipped");
        }

        // Tapping once it has settled is not a skip, so the scene can treat it as a button tap.
        [Test]
        public void a_tap_after_it_settled_is_not_a_skip()
        {
            var opening = new OpeningTitle();
            opening.frame_at(11);
            Assert.That(opening.skip(11), Is.False);
        }

        // Break it: not-a-number, infinite, negative and huge times all give a sensible frame.
        [Test]
        public void broken_times_give_a_sensible_frame()
        {
            var opening = new OpeningTitle();
            var negative = opening.frame_at(-5);
            Assert.That(negative.sky_opacity, Is.EqualTo(0), "before the start is the start");
            Assert.That(negative.is_finished, Is.False);
            opening.frame_at(4.4);
            var nan = opening.frame_at(double.NaN);
            Assert.That(nan.camera_tilt, Is.EqualTo(opening.frame_at(4.4).camera_tilt), "a bad time keeps the last good one");
            var infinite = opening.frame_at(double.PositiveInfinity);
            Assert.That(infinite.camera_tilt, Is.EqualTo(opening.frame_at(4.4).camera_tilt));
            foreach (double huge in new[] { 1e12, double.MaxValue, 123456789.0 })
            {
                var frame = new OpeningTitle().frame_at(huge);
                Assert.That(frame.is_finished, Is.True);
                Assert.That(frame.title_opacity, Is.EqualTo(1));
                Assert.That(double.IsNaN(frame.camera_tilt), Is.False);
            }
            var skipped = new OpeningTitle();
            Assert.That(skipped.skip(double.NaN), Is.True, "a skip with a bad time still skips");
            Assert.That(skipped.frame_at(double.NaN).title_opacity, Is.EqualTo(1));
        }

        // The easing starts and ends gently and never leaves 0 to 1, whatever it's given.
        [Test]
        public void the_easing_is_gentle_and_safe()
        {
            Assert.That(OpeningTitle.ease(0), Is.EqualTo(0));
            Assert.That(OpeningTitle.ease(1), Is.EqualTo(1));
            Assert.That(OpeningTitle.ease(0.5), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(OpeningTitle.ease(0.05), Is.LessThan(0.05), "slow at the start");
            Assert.That(OpeningTitle.ease(-3), Is.EqualTo(0));
            Assert.That(OpeningTitle.ease(40), Is.EqualTo(1));
            Assert.That(OpeningTitle.ease(double.NaN), Is.EqualTo(0));
            Assert.That(OpeningTitle.fade_between(5, 3, 3), Is.EqualTo(1), "a zero-length fade is a cut");
        }
    }
}
