// Purpose: proves the start-up animation (OpeningTitle) shows the right thing at each moment:
// black first, then the sky, the paw prints, Bodhi trotting in and sitting, the door opening,
// Em kneeling, the hug, the name, the buttons, and a calm loop after. A tap skips straight to the
// settled title screen, never to a blank one. Broken times never crash or give an empty screen.
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
            Assert.That(frame.sky_opacity, Is.EqualTo(0));
            Assert.That(frame.house_opacity, Is.EqualTo(0));
            Assert.That(frame.paw_prints_shown, Is.EqualTo(0));
            Assert.That(frame.title_opacity, Is.EqualTo(0));
            Assert.That(frame.buttons_opacity, Is.EqualTo(0));
            Assert.That(frame.buttons_ready, Is.False);
            Assert.That(frame.is_skippable, Is.True);
            Assert.That(frame.is_finished, Is.False);
        }

        // The beats come in order: sky, paw prints, Bodhi, the door, Em, the name, the buttons.
        [Test]
        public void the_beats_come_in_order()
        {
            var opening = new OpeningTitle();
            Assert.That(opening.frame_at(1.0).sky_opacity, Is.GreaterThan(0).And.LessThan(1), "the sky is fading up");
            Assert.That(opening.frame_at(2.0).sky_opacity, Is.EqualTo(1));
            Assert.That(opening.frame_at(1.1).paw_prints_shown, Is.EqualTo(0));
            Assert.That(opening.frame_at(1.9).paw_prints_shown, Is.GreaterThan(0).And.LessThan(OpeningTitle.paw_print_total), "step by step");
            Assert.That(opening.frame_at(3.0).paw_prints_shown, Is.EqualTo(OpeningTitle.paw_print_total));
            Assert.That(opening.frame_at(4.4).door_light_opacity, Is.EqualTo(0));
            Assert.That(opening.frame_at(4.4).her.opacity, Is.EqualTo(0), "she's inside until the door opens");
            Assert.That(opening.frame_at(5.6).door_light_opacity, Is.GreaterThan(0.5));
            Assert.That(opening.frame_at(5.6).her.opacity, Is.EqualTo(1));
            Assert.That(opening.frame_at(6.4).title_opacity, Is.EqualTo(0));
            Assert.That(opening.frame_at(7.6).title_opacity, Is.EqualTo(1));
            Assert.That(opening.frame_at(7.6).subtitle_opacity, Is.LessThan(1), "the subtitle comes after the name");
            Assert.That(opening.frame_at(8.4).buttons_opacity, Is.EqualTo(0));
            Assert.That(opening.frame_at(9.4).buttons_opacity, Is.EqualTo(1));
            Assert.That(opening.frame_at(9.4).buttons_ready, Is.True);
        }

        // Bodhi trots in from off the left edge, slows, sits by the door, then leans into the hug;
        // Em kneels, then hugs him. Every animation name is one the drawings really have.
        [Test]
        public void bodhi_trots_in_sits_and_hugs_her()
        {
            var opening = new OpeningTitle();
            var before = opening.frame_at(1.0).dog;
            Assert.That(before.x, Is.LessThan(0), "off screen to the left");
            var early = opening.frame_at(2.4).dog;
            var late = opening.frame_at(3.4).dog;
            Assert.That(early.animation, Is.EqualTo("trot"));
            Assert.That(early.facing, Is.EqualTo(1));
            Assert.That(late.x, Is.GreaterThan(early.x));
            double early_speed = early.x - opening.frame_at(2.3).dog.x;
            double late_speed = late.x - opening.frame_at(3.3).dog.x;
            Assert.That(late_speed, Is.LessThan(early_speed), "he slows down");
            Assert.That(opening.frame_at(4.2).dog.animation, Is.EqualTo("sit"));
            Assert.That(opening.frame_at(5.2).her.animation, Is.EqualTo("kneel"));
            Assert.That(opening.frame_at(6.5).dog.animation, Is.EqualTo("embrace"));
            Assert.That(opening.frame_at(6.5).her.animation, Is.EqualTo("embrace"));
            Assert.That(opening.frame_at(6.5).her.facing, Is.EqualTo(-1), "she faces him");
            for (double time = 0; time < 12; time += 0.05)
            {
                var frame = opening.frame_at(time);
                foreach (var character in new[] { frame.dog, frame.her })
                {
                    var animation = FrameSheets.set(character.frame_set).animation(character.animation);
                    Assert.That(animation, Is.Not.Null, character.frame_set + " " + character.animation);
                    Assert.That(character.frame, Is.InRange(0, animation.count - 1));
                }
            }
        }

        // The trot's drawings move on with the distance he covers, so his feet don't slide.
        [Test]
        public void the_trot_moves_on_by_distance()
        {
            var opening = new OpeningTitle();
            int changes = 0;
            int last = opening.frame_at(2.0).dog.frame;
            for (double time = 2.0; time < 3.6; time += 1.0 / 60)
            {
                int frame = opening.frame_at(time).dog.frame;
                if (frame != last) changes += 1;
                last = frame;
            }
            Assert.That(changes, Is.GreaterThan(10), "the legs move");
        }

        // The camera starts a little close and eases back to its normal view.
        [Test]
        public void the_camera_eases_back()
        {
            var opening = new OpeningTitle();
            Assert.That(opening.frame_at(0).camera_zoom, Is.EqualTo(OpeningTitle.first_zoom));
            Assert.That(opening.frame_at(3).camera_zoom, Is.LessThan(OpeningTitle.first_zoom).And.GreaterThan(1));
            Assert.That(opening.frame_at(OpeningTitle.settled_at).camera_zoom, Is.EqualTo(1));
        }

        // Once settled it can't be skipped any more, and the light keeps flickering softly.
        [Test]
        public void it_settles_into_a_calm_loop()
        {
            var opening = new OpeningTitle();
            var settled = opening.frame_at(OpeningTitle.settled_at + 0.01);
            Assert.That(settled.is_finished, Is.True);
            Assert.That(settled.is_skippable, Is.False);
            double lowest = 1, highest = 0;
            for (double time = 20; time < 30; time += 0.1)
            {
                double light = opening.frame_at(time).door_light_opacity;
                lowest = System.Math.Min(lowest, light);
                highest = System.Math.Max(highest, light);
            }
            Assert.That(highest - lowest, Is.GreaterThan(0.05), "the light moves");
            Assert.That(lowest, Is.GreaterThan(0.6), "but stays warm");
        }

        // A tap skips to the settled title screen: everything showing, nothing blank, buttons ready.
        [Test]
        public void a_tap_skips_to_the_settled_title()
        {
            var opening = new OpeningTitle();
            Assert.That(opening.skip(0.2), Is.True);
            var frame = opening.frame_at(0.25);
            Assert.That(frame.sky_opacity, Is.EqualTo(1));
            Assert.That(frame.house_opacity, Is.EqualTo(1));
            Assert.That(frame.title_opacity, Is.EqualTo(1));
            Assert.That(frame.subtitle_opacity, Is.EqualTo(1));
            Assert.That(frame.buttons_opacity, Is.EqualTo(1));
            Assert.That(frame.buttons_ready, Is.True);
            Assert.That(frame.her.opacity, Is.EqualTo(1));
            Assert.That(frame.dog.animation, Is.EqualTo("embrace"));
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
            opening.frame_at(3);
            var nan = opening.frame_at(double.NaN);
            Assert.That(nan.dog.x, Is.EqualTo(opening.frame_at(3).dog.x), "a bad time keeps the last good one");
            var infinite = opening.frame_at(double.PositiveInfinity);
            Assert.That(infinite.dog.x, Is.EqualTo(opening.frame_at(3).dog.x));
            foreach (double huge in new[] { 1e12, double.MaxValue, 123456789.0 })
            {
                var frame = new OpeningTitle().frame_at(huge);
                Assert.That(frame.is_finished, Is.True);
                Assert.That(frame.title_opacity, Is.EqualTo(1));
                Assert.That(double.IsNaN(frame.door_light_opacity), Is.False);
                Assert.That(frame.door_light_opacity, Is.InRange(0, 1));
                Assert.That(frame.dog.frame, Is.GreaterThanOrEqualTo(0));
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

        // Paw prints sit along the bottom, left to right, and an unknown print is safe.
        [Test]
        public void paw_prints_run_left_to_right()
        {
            Assert.That(OpeningTitle.paw_print_x(1), Is.GreaterThan(OpeningTitle.paw_print_x(0)));
            Assert.That(OpeningTitle.paw_print_x(-4), Is.EqualTo(OpeningTitle.paw_print_x(0)));
            Assert.That(OpeningTitle.paw_print_x(999), Is.EqualTo(OpeningTitle.paw_print_x(OpeningTitle.paw_print_total - 1)));
        }
    }
}
