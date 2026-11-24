// Purpose: proves FlipbookChooser picks drawings the way the browser game does: planted feet
// stay planted while walking, jumps follow the arc, landings, turns, stops and skids play, a dog
// sits down once and breathes, getting up plays backwards, gaits don't flicker, and nonsense
// input never crashes or picks a drawing that doesn't exist.
using System;
using System.Linq;
using Bodhi.Game;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class FlipbookTests
    {
        const double frame_seconds = 1.0 / 120;

        // A character driven through the chooser: where it is, how it moves, and its clock.
        class Driven
        {
            public FlipbookChooser chooser;
            public FlipbookInput input;

            public Driven(string set_name, string pose = "stand")
            {
                chooser = new FlipbookChooser(FrameSheets.set(set_name));
                input = new FlipbookInput { center_x = 500, on_ground = true, facing = 1, pose = pose, size_scale = 1, jump_scale = 1, wag = 0.4 };
            }

            // One frame: moves by its speed, then asks for a drawing.
            public FlipbookChoice step()
            {
                input.time_seconds += frame_seconds;
                input.center_x += input.velocity_x * frame_seconds;
                return chooser.choose(input);
            }

            // Several frames; returns the last choice.
            public FlipbookChoice run(double seconds)
            {
                var last = step();
                for (double time = frame_seconds; time < seconds; time += frame_seconds)
                {
                    last = step();
                }
                return last;
            }
        }

        [Test]
        public void walking_keeps_a_planted_foot_on_the_ground()
        {
            foreach (var (set_name, speed) in new[] { ("her", 100.0), ("her", 200.0), ("bodhi", 120.0), ("bodhi", 230.0) })
            {
                var walker = new Driven(set_name, "walk");
                walker.input.velocity_x = speed;
                walker.run(1.0);  // Settle into the stride.
                // For each foot: where on the ground it was put down, while it stays down.
                var put_down_at = new double?[4];
                double worst_slide = 0;
                FlipbookChoice choice = default;
                for (int frame = 0; frame < 240; frame++)
                {
                    choice = walker.step();
                    Assert.That(choice.animation, Is.EqualTo(set_name == "her" ? (speed > 135 ? "run" : "walk") : (speed > 160 ? "gallop" : "trot")));
                    var feet = FrameSheets.set(set_name).animation(choice.animation).frames[choice.frame].feet;
                    for (int foot = 0; foot < feet.Length / 2; foot++)
                    {
                        if (feet[foot * 2 + 1] != 0)
                        {
                            put_down_at[foot] = null;  // In the air.
                            continue;
                        }
                        double on_ground_x = walker.input.center_x + feet[foot * 2];
                        put_down_at[foot] = put_down_at[foot] ?? on_ground_x;
                        worst_slide = Math.Max(worst_slide, Math.Abs(on_ground_x - put_down_at[foot].Value));
                    }
                }
                // A foot on the ground stays where it was put down, give or take one drawing's
                // worth of ground (the body moves on a little before the next drawing shows).
                var gait = FrameSheets.set(set_name).animation(choice.animation);
                Assert.That(worst_slide, Is.LessThanOrEqualTo(gait.cycle_length / gait.count + 0.6), set_name + " at " + speed);
            }
        }

        [Test]
        public void a_jump_follows_the_arc_and_lands()
        {
            var jumper = new Driven("her");
            jumper.run(0.5);
            int count = FrameSheets.set("her").animation("jump").count;
            jumper.input.on_ground = false;
            jumper.input.pose = "jump";
            jumper.input.velocity_y = -620;
            Assert.That(jumper.step(), Is.EqualTo(new FlipbookChoice { animation = "jump", frame = 0, facing = 1 }), "take-off");
            jumper.input.velocity_y = 0;
            Assert.That(jumper.step().frame, Is.EqualTo((count - 1) / 2), "top of the arc");
            jumper.input.velocity_y = 900;
            Assert.That(jumper.step().frame, Is.EqualTo(count - 1), "falling fast");
            jumper.input.on_ground = true;
            jumper.input.pose = "stand";
            jumper.input.velocity_y = 0;
            Assert.That(jumper.step().animation, Is.EqualTo("land"));
            Assert.That(jumper.run(0.5).animation, Is.EqualTo("idle"), "the landing ends in standing");
        }

        [Test]
        public void turning_stopping_and_skidding_play()
        {
            var runner = new Driven("her");
            runner.run(0.3);
            runner.input.facing = -1;
            Assert.That(runner.step().animation, Is.EqualTo("turn"));

            runner = new Driven("her", "walk");
            runner.input.velocity_x = 200;
            runner.run(0.5);
            runner.input.velocity_x = 0;
            runner.input.pose = "stand";
            Assert.That(runner.step().animation, Is.EqualTo("stop"), "a brisk stop");

            runner = new Driven("bodhi", "walk");
            runner.input.velocity_x = 230;
            runner.run(0.5);
            runner.input.facing = -1;  // Reversed while still sliding right.
            var skid = runner.step();
            Assert.That(skid.animation, Is.EqualTo("skid"));
            Assert.That(skid.facing, Is.EqualTo(1), "a skid is drawn facing the slide");
        }

        [Test]
        public void a_dog_sits_down_once_then_breathes_and_gets_up_backwards()
        {
            var dog = new Driven("bodhi");
            dog.run(0.2);
            dog.input.pose = "sit";
            var sit = FrameSheets.set("bodhi").animation("sit");
            var playback = FlipbookChooser.playback_of(true, "sit");
            Assert.That(dog.step(), Is.EqualTo(new FlipbookChoice { animation = "sit", frame = 0, facing = 1 }), "sitting starts at the top");
            var later = dog.run(playback.intro_frames / playback.rate + 0.5);
            Assert.That(later.frame, Is.GreaterThanOrEqualTo(playback.intro_frames), "then breathes in the loop");
            for (int frame = 0; frame < 300; frame++)
            {
                Assert.That(dog.step().frame, Is.InRange(playback.intro_frames, sit.count - 1), "never sits down again");
            }
            dog.input.pose = "stand";
            var up = dog.step();
            Assert.That(up.animation, Is.EqualTo("sit"));
            Assert.That(up.frame, Is.EqualTo(playback.intro_frames - 1), "getting up plays sitting down backwards");
            Assert.That(dog.run(0.4).animation, Is.EqualTo("idle"));

            var already = new Driven("bodhi", "sit");
            Assert.That(already.step().frame, Is.GreaterThanOrEqualTo(playback.intro_frames), "a dog that starts sitting is already down");
        }

        [Test]
        public void gaits_do_not_flicker_near_the_switch_speed()
        {
            var her = new Driven("her", "walk");
            her.input.velocity_x = 140;
            Assert.That(her.run(0.3).animation, Is.EqualTo("run"));
            her.input.velocity_x = 120;
            Assert.That(her.run(0.3).animation, Is.EqualTo("run"), "just under the line: still running");
            her.input.velocity_x = 110;
            Assert.That(her.run(0.3).animation, Is.EqualTo("walk"));
            her.input.velocity_x = 130;
            Assert.That(her.run(0.3).animation, Is.EqualTo("walk"), "just over the line: still walking");
        }

        [Test]
        public void the_drawing_pose_comes_from_what_they_do()
        {
            Assert.That(FlipbookChooser.pose_of(false, "", true, 0, 9), Is.EqualTo("stand"));
            Assert.That(FlipbookChooser.pose_of(true, "", true, 0, 3), Is.EqualTo("sit"), "an idle dog sits");
            Assert.That(FlipbookChooser.pose_of(true, "", true, 0, 1), Is.EqualTo("stand"));
            Assert.That(FlipbookChooser.pose_of(false, "", true, -40, 0), Is.EqualTo("walk"));
            Assert.That(FlipbookChooser.pose_of(false, "", false, 0, 0), Is.EqualTo("jump"));
            Assert.That(FlipbookChooser.pose_of(false, "kneel", false, 200, 0), Is.EqualTo("kneel"), "the story's pose wins");
        }

        [Test]
        public void nonsense_never_breaks_the_chooser()
        {
            foreach (var set_name in new[] { "her", "bodhi", "bodhi_puppy", "biscuit" })
            {
                var set = FrameSheets.set(set_name);
                var driven = new Driven(set_name);
                double[] odd = { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1e300, 1e300, 0 };
                string[] poses = { null, "", "fly", "jump", "walk", "sit", "kneel", "sleep", "embrace", "stand" };
                var random = new Random(7);  // The same "random" mix every run.
                double any() => random.Next(3) == 0 ? odd[random.Next(odd.Length)] : random.NextDouble() * 600 - 300;
                for (int index = 0; index < 2000; index++)
                {
                    driven.input.center_x = any();
                    driven.input.velocity_x = any();
                    driven.input.velocity_y = any();
                    driven.input.wag = any();
                    driven.input.size_scale = any();
                    driven.input.jump_scale = any();
                    driven.input.facing = random.Next(5) - 2;
                    driven.input.on_ground = random.Next(3) != 0;
                    driven.input.pose = poses[random.Next(poses.Length)];
                    driven.input.time_seconds = random.Next(9) == 0 ? double.NaN : index * frame_seconds;
                    var choice = driven.chooser.choose(driven.input);
                    var animation = set.animation(choice.animation);
                    Assert.That(animation, Is.Not.Null, set_name + " chose a missing animation " + choice.animation);
                    Assert.That(choice.frame, Is.InRange(0, animation.count - 1), set_name + " " + choice.animation);
                    Assert.That(choice.facing == 1 || choice.facing == -1, "facing is always left or right");
                }
            }
            var nothing = new FlipbookChooser(null).choose(new FlipbookInput());
            Assert.That(nothing.animation, Is.EqualTo("idle"));
            var empty = new FlipbookChooser(new FrameSet("empty", 3, null)).choose(new FlipbookInput { pose = "walk", velocity_x = 200 });
            Assert.That(empty.frame, Is.EqualTo(0), "a set with no drawings still answers");
        }

        [Test]
        public void the_game_runs_at_the_screens_own_rate()
        {
            Assert.That(FramePacing.frames_per_second(120), Is.EqualTo(120), "ProMotion");
            Assert.That(FramePacing.frames_per_second(59.94), Is.EqualTo(60));
            Assert.That(FramePacing.frames_per_second(90), Is.EqualTo(90));
            Assert.That(FramePacing.frames_per_second(240), Is.EqualTo(120), "never more than a phone shows");
            foreach (double nonsense in new[] { 0, -60, 5, double.NaN, double.PositiveInfinity })
            {
                Assert.That(FramePacing.frames_per_second(nonsense), Is.EqualTo(60), nonsense + " falls back to 60");
            }
        }

        [Test]
        public void frame_arithmetic_survives_odd_numbers()
        {
            Assert.That(FlipbookChooser.safe_frame(double.NaN, 5), Is.EqualTo(0));
            Assert.That(FlipbookChooser.safe_frame(-3, 5), Is.EqualTo(0));
            Assert.That(FlipbookChooser.safe_frame(1e300, 5), Is.EqualTo(4));
            Assert.That(FlipbookChooser.safe_frame(2.9, 5), Is.EqualTo(2));
            Assert.That(FlipbookChooser.safe_frame(3, 0), Is.EqualTo(0));
            var intro = new Playback(PlaybackMode.intro, 36, 13, 6);
            Assert.That(FlipbookChooser.timed_frame(intro, 21, 0, false), Is.EqualTo(0));
            Assert.That(FlipbookChooser.timed_frame(intro, 21, 0, true), Is.EqualTo(13), "skipping the intro starts in the loop");
            Assert.That(FlipbookChooser.timed_frame(intro, 21, 100, false), Is.InRange(13, 20));
            Assert.That(FlipbookChooser.timed_frame(intro, 21, double.NaN, false), Is.InRange(0, 20));
            Assert.That(FlipbookChooser.timed_frame(new Playback(PlaybackMode.once, -5), 9, 3, false), Is.EqualTo(0), "a negative rate holds still");
            Assert.That(FlipbookChooser.timed_frame(new Playback(PlaybackMode.loop, 10), 0, 3, false), Is.EqualTo(0));
            Assert.That(FlipbookChooser.closest_phase(new FrameAnimation("none", "", 1, 1, 0, null), new double[] { 1, 2 }, 1), Is.EqualTo(0.5));
        }
    }
}
