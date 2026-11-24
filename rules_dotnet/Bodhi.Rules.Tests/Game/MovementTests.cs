// Purpose: proves the movement maths the Unity scene uses for her and Bodhi, ported from the
// browser game's move_character and update_companion: how they speed up, turn, jump and fall,
// how the one you aren't playing follows, and how chapter layout positions become Unity
// positions. Plain numbers in, plain numbers out, so it's all tested here without Unity.
using System;
using Bodhi.Game;
using Bodhi.Rules;
using NUnit.Framework;

namespace Bodhi.Rules.Tests
{
    public class LayoutSpaceTests
    {
        // Layout units (the browser game's, y pointing down) become metres with y pointing up,
        // and back again: 50 units to a metre, the floor of the layout at height 0.
        [Test]
        public void layout_and_world_positions_convert_both_ways()
        {
            var space = new LayoutSpace(600);
            Assert.That(space.to_world_x(500), Is.EqualTo(10));
            Assert.That(space.to_world_y(600), Is.EqualTo(0));
            Assert.That(space.to_world_y(520), Is.EqualTo(1.6).Within(1e-9));
            Assert.That(space.to_layout_x(space.to_world_x(1234)), Is.EqualTo(1234).Within(1e-9));
            Assert.That(space.to_layout_y(space.to_world_y(77)), Is.EqualTo(77).Within(1e-9));
            Assert.That(space.speed_to_world_y(-100), Is.EqualTo(2), "rising in the layout is up in the world");
            Assert.That(space.speed_to_layout_y(space.speed_to_world_y(321)), Is.EqualTo(321).Within(1e-9));
            Assert.That(space.speed_to_layout_x(space.speed_to_world_x(-55)), Is.EqualTo(-55).Within(1e-9));
        }

        // Break it: a nonsense layout height (zero, negative, not a number) falls back to the
        // browser game's 600, so nothing ends up at infinity.
        [Test]
        public void a_bad_height_falls_back_to_600()
        {
            foreach (var height in new[] { 0, -5, double.NaN, double.PositiveInfinity })
            {
                Assert.That(new LayoutSpace(height).layout_height, Is.EqualTo(600), height.ToString());
            }
            Assert.That(new LayoutSpace(900).layout_height, Is.EqualTo(900));
        }
    }

    public class CharacterMotionTests
    {
        const double frame = 1.0 / 60;

        // Runs a character for some seconds on the ground, pushing one way.
        static void walk(CharacterMotion motion, int direction, double seconds, bool in_water = false)
        {
            for (double time = 0; time < seconds; time += frame)
            {
                motion.step(direction, jump_held: false, on_ground: true, in_water: in_water, speed_scale: 1, delta_seconds: frame);
            }
        }

        // She reaches her walking speed quickly, then breaks into a jog after pushing the same
        // way for a moment; he has one speed. Stopping is quicker than starting.
        [Test]
        public void walking_jogging_and_stopping()
        {
            var her = new CharacterMotion(Character.human);
            walk(her, 1, 0.5);
            Assert.That(her.velocity_x, Is.EqualTo(CharacterSpec.her.walk_speed).Within(1e-6));
            walk(her, 1, 1.5);
            Assert.That(her.velocity_x, Is.EqualTo(CharacterSpec.her.jog_speed).Within(1e-6));
            int frames_to_stop = 0;
            while (her.velocity_x > 0 && frames_to_stop < 1000)
            {
                her.step(0, false, true, false, 1, frame);
                frames_to_stop++;
            }
            Assert.That(frames_to_stop, Is.EqualTo((int)Math.Ceiling(CharacterSpec.her.jog_speed / (CharacterSpec.her.braking * frame))).Within(1));
            var him = new CharacterMotion(Character.dog);
            walk(him, -1, 2);
            Assert.That(him.velocity_x, Is.EqualTo(-CharacterSpec.his.walk_speed).Within(1e-6));
            Assert.That(him.facing, Is.EqualTo(-1));
        }

        // Turning round uses the turning rate, the quickest of all; slowing from a jog to a walk
        // is gradual; water slows them; in the air there's less control.
        [Test]
        public void turning_jog_to_walk_water_and_air()
        {
            var turning = new CharacterMotion(Character.dog);
            walk(turning, 1, 1);
            turning.step(-1, false, true, false, 1, frame);
            Assert.That(turning.velocity_x, Is.EqualTo(CharacterSpec.his.walk_speed - CharacterSpec.his.turning * frame).Within(1e-6));
            var slowing = new CharacterMotion(Character.human);
            walk(slowing, 1, 2);
            slowing.step(1, false, true, false, 0.5, frame);  // Suddenly half speed: still pushing.
            Assert.That(slowing.velocity_x, Is.EqualTo(CharacterSpec.her.jog_speed - CharacterSpec.her.braking * 0.5 * frame).Within(1e-6));
            var wading = new CharacterMotion(Character.human);
            walk(wading, 1, 0.5, in_water: true);
            Assert.That(wading.velocity_x, Is.EqualTo(CharacterSpec.her.walk_speed * CharacterMotion.water_speed_scale).Within(1e-6));
            var flying = new CharacterMotion(Character.human);
            flying.step(1, false, false, false, 1, frame);
            Assert.That(flying.velocity_x, Is.EqualTo(CharacterSpec.her.air_control * frame).Within(1e-6));
        }

        // A jump leaves at the jump speed, rises with lighter gravity and falls with heavier
        // gravity, never faster than the top falling speed. A puppy jumps lower.
        [Test]
        public void jumping_rising_and_falling()
        {
            var her = new CharacterMotion(Character.human);
            her.press_jump();
            her.step(0, true, true, false, 1, frame);
            Assert.That(her.jumped_this_step, Is.True);
            Assert.That(her.velocity_y, Is.EqualTo(-CharacterSpec.her.jump_speed + CharacterMotion.rising_gravity * frame).Within(1e-6));
            double before = her.velocity_y;
            her.step(0, true, false, false, 1, frame);
            Assert.That(her.jumped_this_step, Is.False);
            Assert.That(her.velocity_y - before, Is.EqualTo(CharacterMotion.rising_gravity * frame).Within(1e-6));
            for (int index = 0; index < 600; index++)
            {
                her.step(0, true, false, false, 1, frame);
            }
            Assert.That(her.velocity_y, Is.EqualTo(CharacterMotion.maximum_fall_speed));
            var puppy = new CharacterMotion(Character.dog, jump_scale: 0.5);
            puppy.press_jump();
            puppy.step(0, true, true, false, 1, frame);
            Assert.That(puppy.velocity_y, Is.EqualTo(-CharacterSpec.his.jump_speed * 0.5 + CharacterMotion.rising_gravity * frame).Within(1e-6));
        }

        // Letting go of jump early cuts the rise in half once, so a tap is a hop and a hold is
        // a full jump.
        [Test]
        public void letting_go_early_makes_a_smaller_jump()
        {
            var tap = new CharacterMotion(Character.human);
            tap.press_jump();
            tap.step(0, true, true, false, 1, frame);
            double rising = tap.velocity_y;
            tap.step(0, false, false, false, 1, frame);
            Assert.That(tap.velocity_y, Is.EqualTo((rising + CharacterMotion.rising_gravity * frame) * 0.5).Within(1e-6));
            double after_cut = tap.velocity_y;
            tap.step(0, false, false, false, 1, frame);
            Assert.That(tap.velocity_y, Is.EqualTo(after_cut + CharacterMotion.rising_gravity * frame).Within(1e-6), "only once");
        }

        // "Coyote time": a jump still works a tenth of a second after walking off an edge, and
        // a press a little before landing still jumps on landing ("jump buffer").
        [Test]
        public void coyote_time_and_jump_buffer()
        {
            var late = new CharacterMotion(Character.dog);
            late.step(1, false, true, false, 1, frame);
            for (int index = 0; index < 4; index++)
            {
                late.step(1, false, false, false, 1, frame);
            }
            late.press_jump();
            late.step(1, true, false, false, 1, frame);
            Assert.That(late.jumped_this_step, Is.True, "0.08 s after the edge");
            var too_late = new CharacterMotion(Character.dog);
            too_late.step(1, false, true, false, 1, frame);
            for (int index = 0; index < 8; index++)
            {
                too_late.step(1, false, false, false, 1, frame);
            }
            too_late.press_jump();
            too_late.step(1, true, false, false, 1, frame);
            Assert.That(too_late.jumped_this_step, Is.False, "0.13 s after the edge");
            var early = new CharacterMotion(Character.human);
            early.press_jump();
            for (int index = 0; index < 6; index++)
            {
                early.step(0, true, false, false, 1, frame);
            }
            early.step(0, true, true, false, 1, frame);
            Assert.That(early.jumped_this_step, Is.True, "pressed 0.1 s before landing");
            var too_early = new CharacterMotion(Character.human);
            too_early.press_jump();
            for (int index = 0; index < 9; index++)
            {
                too_early.step(0, true, false, false, 1, frame);
            }
            too_early.step(0, true, true, false, 1, frame);
            Assert.That(too_early.jumped_this_step, Is.False, "pressed 0.15 s before landing");
        }

        // Her jog builds up gradually from 0.7 s of pushing, and turning round starts the count
        // again.
        [Test]
        public void the_jog_builds_up_and_restarts_on_a_turn()
        {
            var her = new CharacterMotion(Character.human);
            walk(her, 1, 1.0);
            double halfway = CharacterSpec.her.walk_speed + (CharacterSpec.her.jog_speed - CharacterSpec.her.walk_speed) * 0.5;
            // She speeds up over a few frames, so her speed trails the target a little; the gap
            // from walk to jog is now twice as wide, so the allowance is too.
            Assert.That(her.velocity_x, Is.EqualTo(halfway).Within(8), "half a jog at 1 s");
            var turner = new CharacterMotion(Character.human);
            walk(turner, 1, 0.6);
            walk(turner, -1, 0.6);
            Assert.That(turner.velocity_x, Is.EqualTo(-CharacterSpec.her.walk_speed).Within(1e-6), "no jog yet after turning");
        }

        // One jump at a time: no second jump straight after the first, even with a fresh press,
        // and a press used by a jump isn't used again if they touch down at once (a low ceiling).
        [Test]
        public void one_press_one_jump()
        {
            var her = new CharacterMotion(Character.human);
            her.press_jump();
            her.step(0, true, true, false, 1, frame);
            her.press_jump();
            her.step(0, true, false, false, 1, frame);
            Assert.That(her.jumped_this_step, Is.False, "no jump in mid-air");
            var bumped = new CharacterMotion(Character.human);
            bumped.press_jump();
            bumped.step(0, true, true, false, 1, frame);
            bumped.step(0, true, true, false, 1, frame);
            Assert.That(bumped.jumped_this_step, Is.False, "the press was already used");
        }

        // Letting go near the top of a jump (rising slower than the cut speed) changes nothing.
        [Test]
        public void letting_go_near_the_top_changes_nothing()
        {
            var her = new CharacterMotion(Character.human);
            her.press_jump();
            her.step(0, true, true, false, 1, frame);
            while (her.velocity_y < -CharacterMotion.jump_cut_speed)
            {
                her.step(0, true, false, false, 1, frame);
            }
            double near_top = her.velocity_y;
            her.step(0, false, false, false, 1, frame);
            Assert.That(her.velocity_y, Is.EqualTo(near_top + CharacterMotion.rising_gravity * frame).Within(1e-6));
        }

        // Standing on the ground, nothing builds up a falling speed (which would slam them
        // down the moment they walk off an edge).
        [Test]
        public void standing_never_builds_up_a_fall()
        {
            var her = new CharacterMotion(Character.human);
            for (int index = 0; index < 300; index++)
            {
                her.step(0, false, true, false, 1, frame);
            }
            Assert.That(her.velocity_y, Is.EqualTo(CharacterMotion.falling_gravity * frame).Within(1e-6));
        }

        // Break it: bad frame times and pushes change nothing or are tamed; a very long frame
        // (the phone stalled) counts as a quarter of a second.
        [Test]
        public void bad_input_is_tamed()
        {
            var her = new CharacterMotion(Character.human);
            foreach (var bad in new[] { 0, -1, double.NaN, double.PositiveInfinity })
            {
                her.step(1, false, true, false, 1, bad);
            }
            Assert.That(her.velocity_x, Is.EqualTo(0));
            Assert.That(her.velocity_y, Is.EqualTo(0));
            for (int index = 0; index < 30; index++)
            {
                her.step(5, false, true, false, double.NaN, frame);  // A push of 5 is a push of 1; a bad speed scale is 1.
            }
            Assert.That(her.velocity_x, Is.EqualTo(CharacterSpec.her.walk_speed).Within(1e-6));
            foreach (var bad_scale in new[] { 0, -1, double.NaN })
            {
                var puppy = new CharacterMotion(Character.dog, jump_scale: bad_scale);
                puppy.press_jump();
                puppy.step(0, true, true, false, 1, frame);
                Assert.That(puppy.velocity_y, Is.EqualTo(-CharacterSpec.his.jump_speed + CharacterMotion.rising_gravity * frame).Within(1e-6), "a bad jump scale is 1");
            }
            var stalled = new CharacterMotion(Character.dog);
            stalled.step(0, false, false, false, 1, 10);
            Assert.That(stalled.velocity_y, Is.EqualTo(CharacterMotion.falling_gravity * CharacterMotion.longest_step_seconds).Within(1e-6));
        }
    }

    public class CompanionSteeringTests
    {
        const double frame = 1.0 / 60;

        // The view of the world one frame gives the steering: a leader standing still at 500.
        static CompanionView view(Character who, double companion_x, double leader_x = 500)
        {
            return new CompanionView
            {
                who = who, companion_x = companion_x, leader_x = leader_x, leader_facing = 1,
                follow = true, beat = ChapterBeat.human_section, closeness = 0.5, on_ground = true,
            };
        }

        // He follows her at a gap that shrinks as he wants to be close; she follows him at 70.
        // He sets off only when clearly too far and stops once close, so he never twitches.
        [Test]
        public void follows_at_a_gap_without_twitching()
        {
            var steering = new CompanionSteering(new Random(1));
            double gap = 118 - 0.5 * 80;
            Assert.That(steering.steer(view(Character.dog, 500 - gap - 20), frame).direction, Is.EqualTo(0), "close enough");
            var setting_off = steering.steer(view(Character.dog, 500 - gap - 30), frame);
            Assert.That(setting_off.direction, Is.EqualTo(1));
            Assert.That(steering.steer(view(Character.dog, 500 - gap - 10), frame).direction, Is.EqualTo(1), "keeps going");
            Assert.That(steering.steer(view(Character.dog, 500 - gap + 1), frame).direction, Is.EqualTo(0), "stops inside the gap");
            var her = new CompanionSteering(new Random(1));
            Assert.That(her.steer(view(Character.human, 500 - 70 - 30), frame).direction, Is.EqualTo(1));
            Assert.That(her.steer(view(Character.human, 500 - 70 + 1), frame).direction, Is.EqualTo(0));
        }

        // He trots when close and runs when far; a hungry dog drags a little.
        [Test]
        public void trots_close_runs_far_and_drags_when_hungry()
        {
            var steering = new CompanionSteering(new Random(1));
            Assert.That(steering.steer(view(Character.dog, 380), frame).speed_scale, Is.EqualTo(0.7).Within(1e-9));
            Assert.That(steering.steer(view(Character.dog, 100), frame).speed_scale, Is.EqualTo(1).Within(1e-9));
            var hungry = view(Character.dog, 100);
            hungry.care_speed_scale = CareState.hungry_speed_scale;
            Assert.That(steering.steer(hungry, frame).speed_scale, Is.EqualTo(CareState.hungry_speed_scale).Within(1e-9));
        }

        // With following off, he stays put; in a reunion he runs to her, faster, up close.
        [Test]
        public void stays_when_told_and_runs_to_her_in_a_reunion()
        {
            var steering = new CompanionSteering(new Random(1));
            var stay = view(Character.dog, 100);
            stay.follow = false;
            Assert.That(steering.steer(stay, frame).direction, Is.EqualTo(0));
            var reunion = view(Character.dog, 380);
            reunion.follow = false;
            reunion.reunion_running = true;
            var running = steering.steer(reunion, frame);
            Assert.That(running.direction, Is.EqualTo(1));
            Assert.That(running.speed_scale, Is.EqualTo(1.15 * 0.7).Within(1e-9));
            reunion.companion_x = 440;
            Assert.That(steering.steer(reunion, frame).direction, Is.EqualTo(1), "in a reunion: right up to her");
            Assert.That(new CompanionSteering(new Random(1)).steer(view(Character.dog, 440), frame).direction, Is.EqualTo(0), "not in a reunion: close enough");
        }

        // His habit when they're together and calm: he runs a little ahead of her, sits, and
        // checks back. Never when he's frightened, and never while she's standing still.
        [Test]
        public void runs_ahead_sits_and_checks_back_when_calm()
        {
            var steering = new CompanionSteering(new Random(1));
            var walking_together = view(Character.dog, 450);
            walking_together.beat = ChapterBeat.together;
            walking_together.leader_speed = 150;
            double waited = 0;
            while (steering.ahead_target == null && waited < 10)
            {
                steering.steer(walking_together, frame);
                waited += frame;
            }
            Assert.That(waited, Is.EqualTo(CompanionSteering.first_run_ahead_seconds).Within(2 * frame));
            Assert.That(steering.ahead_target, Is.EqualTo(500 + CompanionSteering.run_ahead_distance));
            var order = steering.steer(walking_together, frame);
            Assert.That(order.direction, Is.EqualTo(1));
            walking_together.companion_x = 500 + CompanionSteering.run_ahead_distance - 20;
            Assert.That(steering.steer(walking_together, frame).direction, Is.EqualTo(1), "keeps going until right there");
            walking_together.companion_x = 500 + CompanionSteering.run_ahead_distance - 5;
            order = steering.steer(walking_together, frame);
            Assert.That(order.sit_seconds, Is.EqualTo(1.5));
            Assert.That(order.face, Is.EqualTo(-1), "looks back at her");
            Assert.That(steering.ahead_target, Is.Null);

            var frightened = new CompanionSteering(new Random(1));
            var scared = view(Character.dog, 450);
            scared.beat = ChapterBeat.together;
            scared.leader_speed = 150;
            scared.fear = 0.6;
            var standing = new CompanionSteering(new Random(1));
            var still = view(Character.dog, 450);
            still.beat = ChapterBeat.together;
            for (double time = 0; time < 30; time += frame)
            {
                frightened.steer(scared, frame);
                standing.steer(still, frame);
            }
            Assert.That(frightened.ahead_target, Is.Null);
            Assert.That(standing.ahead_target, Is.Null);

            var apart = new CompanionSteering(new Random(1));
            var in_a_dog_section = view(Character.dog, 450);
            in_a_dog_section.leader_speed = 150;
            var facing_left = new CompanionSteering(new Random(1));
            var leftward = view(Character.dog, 550);
            leftward.beat = ChapterBeat.together;
            leftward.leader_speed = 150;
            leftward.leader_facing = -1;
            for (double time = 0; time < 10 && facing_left.ahead_target == null; time += frame)
            {
                apart.steer(in_a_dog_section, frame);
                facing_left.steer(leftward, frame);
            }
            Assert.That(apart.ahead_target, Is.Null, "only while they're together");
            Assert.That(facing_left.ahead_target, Is.EqualTo(500 - CompanionSteering.run_ahead_distance), "ahead is the way she faces");
        }

        // Blocked by a wall he can clear, he jumps (not every frame); blocked by one he can't,
        // he sits and waits facing her instead of leaping at it again and again.
        [Test]
        public void jumps_what_he_can_and_waits_at_what_he_cant()
        {
            var steering = new CompanionSteering(new Random(1));
            var low_wall = view(Character.dog, 100);
            low_wall.blocked = true;
            low_wall.obstacle_height = 60;
            Assert.That(steering.steer(low_wall, frame).wants_jump, Is.True);
            Assert.That(steering.steer(low_wall, frame).wants_jump, Is.False, "not again straight away");
            int frames_until_next_jump = 1;
            while (!steering.steer(low_wall, frame).wants_jump && frames_until_next_jump < 120)
            {
                frames_until_next_jump++;
            }
            Assert.That(frames_until_next_jump * frame, Is.EqualTo(CompanionSteering.jump_rest_seconds).Within(2 * frame), "tries again after a rest");
            var waiting = new CompanionSteering(new Random(1));
            var tall_fence = view(Character.dog, 100);
            tall_fence.blocked = true;
            tall_fence.obstacle_height = 400;
            var order = waiting.steer(tall_fence, frame);
            Assert.That(order.wants_jump, Is.False);
            Assert.That(order.direction, Is.EqualTo(0));
            Assert.That(order.sit_seconds, Is.EqualTo(CompanionSteering.wait_seconds));
            Assert.That(order.face, Is.EqualTo(1));
            Assert.That(waiting.steer(tall_fence, frame).direction, Is.EqualTo(0), "still waiting");
            tall_fence.blocked = false;  // The way clears (a gate opens): he still finishes his wait.
            Assert.That(waiting.steer(tall_fence, frame).direction, Is.EqualTo(0), "waits out the rest");
        }

        // Break it: a frame time that isn't a real number changes nothing and steers nowhere.
        [Test]
        public void bad_frame_times_steer_nowhere()
        {
            var steering = new CompanionSteering(new Random(1));
            foreach (var bad in new[] { 0, -1, double.NaN, double.PositiveInfinity })
            {
                Assert.That(steering.steer(view(Character.dog, 100), bad).direction, Is.EqualTo(0), bad.ToString());
            }
            Assert.Throws<ArgumentNullException>(() => new CompanionSteering(null));
        }
    }
}
