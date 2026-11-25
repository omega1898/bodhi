// Purpose: proves how she and Bodhi move through a chapter: standing on floors, stopped by walls,
// walking up small steps but not big ones, bumping their heads, falling where a floor was taken
// away, a gap that stops only her, the chapter's ends, and being put back on safe ground after
// falling out of the world.
using System;
using System.Linq;
using Bodhi.Game;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class CharacterMoverTests
    {
        const double frame = 1.0 / 60;

        // A floor at 520, a kerb 10 high at 300, a crate 30 high at 500, a low beam at 800, a gap
        // at 1000 that stops only her, and a hole from 1300 to 1400.
        static ChapterStage street()
        {
            return new ChapterStage
            {
                id = "test_street",
                width = 2000,
                height = 600,
                solids = new[]
                {
                    new StageSolid { kind = "floor", x = 0, y = 520, width = 1300, height = 80 },
                    new StageSolid { kind = "floor", x = 1400, y = 520, width = 600, height = 80 },
                    new StageSolid { kind = "plank", x = 300, y = 510, width = 100, height = 10 },
                    new StageSolid { kind = "plank", x = 500, y = 490, width = 60, height = 30 },
                    new StageSolid { kind = "plank", x = 760, y = 420, width = 120, height = 10 },
                    new StageSolid { kind = "invisible", x = 1000, y = 400, width = 10, height = 120, blocks = SolidBlocks.human },
                },
            };
        }

        static void walk(CharacterMover mover, int direction, double seconds, ChapterStage stage, ChapterRun run = null, bool jump_held = false)
        {
            for (double time = 0; time < seconds; time += frame)
            {
                mover.step(direction, jump_held, false, 1, frame, stage, run);
            }
        }

        // Dropped just above the floor, she lands and stands; walking right, she walks up the kerb.
        [Test]
        public void she_lands_stands_and_walks_up_a_kerb()
        {
            var stage = street();
            var her = new CharacterMover(Character.human, 200, 500);
            walk(her, 0, 0.5, stage);
            Assert.That(her.on_ground, Is.True);
            Assert.That(her.feet_y, Is.EqualTo(520).Within(1e-9));
            Assert.That(her.center_x, Is.EqualTo(200).Within(1e-9));
            walk(her, 1, 0.7, stage);
            Assert.That(her.center_x, Is.GreaterThan(300 + her.width / 2));
            Assert.That(her.feet_y, Is.EqualTo(510).Within(1e-9), "up on the kerb");
            Assert.That(her.blocked, Is.False);
        }

        // A crate 30 high is too big a step: she stops against it, pushing, and her speed is gone.
        [Test]
        public void a_crate_stops_her_until_she_jumps()
        {
            var stage = street();
            var her = new CharacterMover(Character.human, 460, 520);
            walk(her, 0, 0.1, stage);
            walk(her, 1, 1, stage);
            Assert.That(her.left + her.width, Is.EqualTo(500).Within(1e-9), "against the crate");
            Assert.That(her.blocked, Is.True);
            Assert.That(her.obstacle_height, Is.EqualTo(30).Within(1e-9));
            Assert.That(her.motion.velocity_x, Is.EqualTo(0), "the wall took her speed");
            Assert.That(her.moved_speed, Is.EqualTo(0).Within(1e-6));
            her.step(0, false, false, 1, frame, stage, null);
            Assert.That(her.blocked, Is.False, "not pushing: not blocked");
            her.motion.press_jump();
            her.step(1, true, false, 1, frame, stage, null);
            Assert.That(her.jumped, Is.True, "this frame she left the ground");
            her.step(1, true, false, 1, frame, stage, null);
            Assert.That(her.jumped, Is.False, "one jump, one frame");
            walk(her, 1, 1, stage, jump_held: true);
            Assert.That(her.left, Is.GreaterThan(500), "over it");
        }

        // A kerb under a low beam: stepping up would put her head in the beam, so the kerb stops
        // her like a wall. And in the air, a step's side is a wall too: she isn't lifted onto it.
        [Test]
        public void a_step_she_cant_stand_on_is_a_wall()
        {
            var stage = street();
            stage.solids = stage.solids.Append(new StageSolid { kind = "plank", x = 1500, y = 510, width = 100, height = 10 })
                .Append(new StageSolid { kind = "plank", x = 1480, y = 420, width = 200, height = 10 }).ToArray();
            var her = new CharacterMover(Character.human, 1450, 520);
            walk(her, 0, 0.1, stage);
            walk(her, 1, 1, stage);
            Assert.That(her.left + her.width, Is.EqualTo(1500).Within(1e-9), "stopped at the kerb");
            Assert.That(her.feet_y, Is.EqualTo(520).Within(1e-9));
            var falling = new CharacterMover(Character.human, 486, 494);
            for (int step = 0; step < 4; step++)
            {
                falling.step(1, false, false, 1, frame, stage, null);
            }
            Assert.That(falling.blocked, Is.True, "she reached the crate's side in the air");
            Assert.That(falling.left + falling.width, Is.LessThanOrEqualTo(500 + 1e-9), "pushed back from the crate's side");
            Assert.That(falling.feet_y, Is.GreaterThan(490), "not lifted onto it");
        }

        // A low beam: a jump under it bumps her head and she comes straight down, not stuck to it.
        [Test]
        public void a_low_beam_bumps_her_head()
        {
            var stage = street();
            var her = new CharacterMover(Character.human, 820, 520);
            walk(her, 0, 0.1, stage);
            Assert.That(her.feet_y, Is.EqualTo(520).Within(1e-9));
            her.motion.press_jump();
            double highest_top = her.top;
            for (int step = 0; step < 20; step++)
            {
                her.step(0, true, false, 1, frame, stage, null);
                highest_top = Math.Min(highest_top, her.top);
            }
            Assert.That(highest_top, Is.GreaterThanOrEqualTo(430 - 1e-9).And.LessThan(431), "her head stops at the beam's underside");
            Assert.That(her.on_ground, Is.True, "a third of a second later she's back down, not stuck to the beam");
            Assert.That(her.feet_y, Is.EqualTo(520).Within(1e-9));
        }

        // The gap stops her, but he walks through it.
        [Test]
        public void a_gap_stops_her_but_not_him()
        {
            var stage = street();
            var her = new CharacterMover(Character.human, 950, 520);
            var him = new CharacterMover(Character.dog, 950, 520);
            walk(her, 1, 1, stage);
            walk(him, 1, 1, stage);
            Assert.That(her.left + her.width, Is.EqualTo(1000).Within(1e-9));
            Assert.That(him.center_x, Is.GreaterThan(1010));
        }

        // A hole: he falls in and, out of the world, is put back on the last safe ground.
        [Test]
        public void falling_out_of_the_world_puts_him_back()
        {
            var stage = street();
            var him = new CharacterMover(Character.dog, 1200, 520);
            walk(him, 0, 1, stage);
            double safe_left = him.left;
            walk(him, 1, 0.6, stage);
            Assert.That(him.on_ground, Is.False, "in the hole");
            walk(him, 0, 3, stage);
            Assert.That(him.feet_y, Is.LessThanOrEqualTo(520 + 1e-9), "back up");
            Assert.That(him.left, Is.GreaterThanOrEqualTo(safe_left).And.LessThan(1300));
        }

        // The chapter's ends stop them, and can't be jumped.
        [Test]
        public void the_chapter_ends_stop_them()
        {
            var stage = street();
            var her = new CharacterMover(Character.human, 30, 520);
            walk(her, -1, 1, stage);
            Assert.That(her.left, Is.EqualTo(0));
            Assert.That(her.blocked, Is.True);
            Assert.That(her.obstacle_height, Is.EqualTo(double.MaxValue));
            var far = new CharacterMover(Character.human, 1980, 520);
            walk(far, 1, 1, stage);
            Assert.That(far.left + far.width, Is.EqualTo(2000).Within(1e-9));
        }

        // A floor the story hasn't put in place yet isn't there: she falls past it.
        [Test]
        public void a_hidden_floor_isnt_there_until_the_story_adds_it()
        {
            var stage = street();
            stage.solids = stage.solids.Append(new StageSolid { kind = "plank", id = "bridge", x = 1290, y = 520, width = 120, height = 12, hidden = true }).ToArray();
            var her = new CharacterMover(Character.human, 1350, 500);
            walk(her, 0, 1, stage);
            Assert.That(her.feet_y, Is.GreaterThan(520), "she fell into the hole");
            stage.solids.Last().hidden = false;
            var again = new CharacterMover(Character.human, 1350, 500);
            walk(again, 0, 1, stage);
            Assert.That(again.feet_y, Is.EqualTo(520).Within(1e-9));
            Assert.That(again.center_x, Is.EqualTo(1350).Within(1e-9), "standing on the bridge");
        }

        // A puppy is smaller and jumps lower; a size that isn't a real number counts as grown.
        [Test]
        public void a_puppy_is_smaller_and_jumps_lower()
        {
            var puppy = new CharacterMover(Character.dog, 100, 520, size_scale: 0.5, jump_scale: 0.6);
            Assert.That((puppy.width, puppy.height), Is.EqualTo((23.0, 19.0)));
            Assert.That(new CharacterMover(Character.dog, 100, 520, size_scale: double.NaN).height, Is.EqualTo(38));
            Assert.That(new CharacterMover(Character.human, 100, 520, size_scale: 0.5).height, Is.EqualTo(86), "she's always full size");
            var stage = street();
            var grown = new CharacterMover(Character.dog, 100, 520);
            walk(puppy, 0, 0.2, stage);
            walk(grown, 0, 0.2, stage);
            puppy.motion.press_jump();
            grown.motion.press_jump();
            double puppy_best = puppy.feet_y, grown_best = grown.feet_y;
            for (int step = 0; step < 60; step++)
            {
                puppy.step(0, true, false, 1, frame, stage, null);
                grown.step(0, true, false, 1, frame, stage, null);
                puppy_best = Math.Min(puppy_best, puppy.feet_y);
                grown_best = Math.Min(grown_best, grown.feet_y);
            }
            Assert.That(520 - puppy_best, Is.LessThan(520 - grown_best));
            Assert.That(puppy.jumped, Is.False, "the jump was long ago");
        }

        // Break it: a stalled frame doesn't send anyone through a thin plank or a wall, a place
        // that isn't a number is tamed, and bad frame times move nothing.
        [Test]
        public void stalls_and_nonsense_dont_break_them()
        {
            var stage = street();
            stage.solids = stage.solids.Append(new StageSolid { kind = "plank", x = 1500, y = 200, width = 100, height = 4 }).ToArray();
            var falling = new CharacterMover(Character.dog, 1550, -1000, size_scale: 0.5);
            for (int step = 0; step < 30; step++)
            {
                falling.step(0, false, false, 1, 0.25, stage, null);
            }
            Assert.That(falling.feet_y, Is.EqualTo(200).Within(1e-9), "a small puppy falling fast still lands on a thin plank");
            var odd = new CharacterMover(Character.human, double.NaN, double.PositiveInfinity);
            Assert.That((odd.center_x, odd.feet_y), Is.EqualTo((0.0, 0.0)));
            var still = new CharacterMover(Character.human, 200, 520);
            foreach (var bad in new[] { double.NaN, 0, -1, double.PositiveInfinity })
            {
                still.step(1, false, false, 1, bad, stage, null);
            }
            Assert.That(still.center_x, Is.EqualTo(200));
            var no_stage = new CharacterMover(Character.human, 200, 100);
            no_stage.step(0, false, false, 1, 0.1, null, null);
            Assert.That(no_stage.feet_y, Is.GreaterThan(100), "with no stage she simply falls");
        }

        // A cut puts them somewhere at once, standing still, and wading slows them.
        [Test]
        public void placing_and_wading()
        {
            var stage = street();
            var her = new CharacterMover(Character.human, 200, 520);
            walk(her, 1, 0.5, stage);
            her.place(100, 520);
            Assert.That((her.center_x, her.feet_y), Is.EqualTo((100.0, 520.0)));
            Assert.That(her.motion.velocity_x, Is.EqualTo(0));
            var dry = new CharacterMover(Character.human, 100, 520);
            var wet = new CharacterMover(Character.human, 100, 520);
            for (int step = 0; step < 30; step++)
            {
                dry.step(1, false, false, 1, frame, stage, null);
                wet.step(1, false, true, 1, frame, stage, null);
            }
            Assert.That(wet.center_x - 100, Is.LessThan((dry.center_x - 100) * 0.8));
            Assert.That(wet.moved_speed, Is.EqualTo(CharacterSpec.her.walk_speed * CharacterMotion.water_speed_scale).Within(1));
        }
    }
}
