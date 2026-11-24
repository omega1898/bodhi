// Purpose: how she and Bodhi move: speeding up, slowing down, turning, jumping and falling. It
// is the browser game's move_character (prototype/web/js/game.js) without the walls and floors,
// which Unity's physics handles. Each step takes what the player (or the companion steering)
// wants and gives back a velocity; the Unity script hands that velocity to the physics body.
//
// Positions and speeds are in layout units with y pointing down, like the chapters, so the
// numbers match the browser game exactly. LayoutSpace turns them into metres for Unity.
using System;
using Bodhi.Rules;

namespace Bodhi.Game
{
    // One character's movement numbers, the same as the browser game's character_specs. Speeds
    // are units a second; rates are units a second, per second.
    public class CharacterSpec
    {
        public double width, height;
        public double walk_speed, jog_speed, jump_speed;
        public double acceleration, braking, turning, air_control;

        public static readonly CharacterSpec her = new CharacterSpec
        {
            width = 26, height = 86, walk_speed = 175, jog_speed = 235, jump_speed = 620,
            acceleration = 1800, braking = 2600, turning = 3600, air_control = 1500,
        };

        public static readonly CharacterSpec his = new CharacterSpec
        {
            width = 46, height = 38, walk_speed = 235, jog_speed = 235, jump_speed = 690,
            acceleration = 2400, braking = 3000, turning = 4400, air_control = 2000,
        };

        // The numbers for her or for him.
        public static CharacterSpec of(Character who)
        {
            return who == Character.human ? her : his;
        }
    }

    public class CharacterMotion
    {
        public const double rising_gravity = 1800;     // Pull while rising: sets how high a jump goes.
        public const double falling_gravity = 2500;    // Stronger on the way down: weighty, not floaty.
        public const double maximum_fall_speed = 1100;
        public const double coyote_seconds = 0.1;      // A jump still works this long after an edge.
        public const double jump_buffer_seconds = 0.13;  // A press this long before landing still jumps.
        public const double jump_cut_speed = 200;      // Let go while rising faster than this: half the rise.
        public const double water_speed_scale = 0.62;  // Wading is slower.
        public const double longest_step_seconds = 0.25;  // A stalled phone doesn't fling anyone.

        readonly double jump_scale;
        double push_seconds = 0;      // How long the same direction has been held (for her jog).
        int last_direction = 0;
        double ground_grace = 0;      // Coyote time left.
        double jump_buffer = 0;       // Jump press still waiting to happen.
        bool jump_cut_allowed = false;

        // The numbers this character moves by.
        public CharacterSpec spec { get; }

        // Sideways speed, units a second: positive is right.
        public double velocity_x { get; private set; }

        // Up-and-down speed, units a second: negative is rising, positive is falling.
        public double velocity_y { get; private set; }

        // Which way they face: 1 right, -1 left.
        public int facing { get; private set; } = 1;

        // True if the last step was the one that left the ground in a jump (for the push-off
        // stretch and the sound).
        public bool jumped_this_step { get; private set; }

        // Makes the movement for her or for him. A puppy jumps lower: `jump_scale` is the
        // chapter's jump for his age (1 for a grown dog). A bad scale counts as 1.
        public CharacterMotion(Character who, double jump_scale = 1)
        {
            this.jump_scale = jump_scale > 0 && !double.IsInfinity(jump_scale) ? jump_scale : 1;
            spec = CharacterSpec.of(who);
        }

        // The player pressed jump: it happens at the next step that can jump, if that comes soon.
        public void press_jump()
        {
            jump_buffer = jump_buffer_seconds;
        }

        // Moves on by one step. `direction` is the push (-1 left, 0 none, 1 right); `jump_held`
        // whether jump is still held (letting go early makes a smaller jump); `on_ground` and
        // `in_water` come from the physics; `speed_scale` slows or speeds them (a hungry dog,
        // the companion trotting). Bad numbers are tamed rather than trusted.
        public void step(int direction, bool jump_held, bool on_ground, bool in_water, double speed_scale, double delta_seconds)
        {
            jumped_this_step = false;
            if (!(delta_seconds > 0) || double.IsInfinity(delta_seconds))  // Also refuses NaN.
            {
                return;
            }
            double seconds = Math.Min(delta_seconds, longest_step_seconds);
            direction = Math.Sign(direction);
            if (!(speed_scale > 0) || double.IsInfinity(speed_scale))
            {
                speed_scale = 1;
            }

            // Keep pushing one way and she breaks into a jog after a moment. (His jog speed is
            // his walk speed, so for him this changes nothing.)
            push_seconds = direction != 0 && direction == last_direction ? push_seconds + seconds : 0;
            last_direction = direction;
            double jog = ease_in_out((push_seconds - 0.7) / 0.6);
            double top_speed = (spec.walk_speed + (spec.jog_speed - spec.walk_speed) * jog) * (in_water ? water_speed_scale : 1) * speed_scale;

            // Speed changes at a steady rate: quick to start, quicker to stop, quickest to turn.
            bool turning = direction != 0 && Math.Sign(velocity_x) == -direction;
            double rate = on_ground ? (turning ? spec.turning : direction != 0 ? spec.acceleration : spec.braking) : spec.air_control;
            if (on_ground && Math.Abs(velocity_x) > top_speed + 1 && direction != 0)
            {
                rate = spec.braking * 0.5;  // Slowing from a jog to a walk is gradual.
            }
            velocity_x = move_toward(velocity_x, direction * top_speed, rate * seconds);
            if (direction != 0)
            {
                facing = direction;
            }

            // Standing on the ground holds them up: no falling speed builds while they stand.
            if (on_ground && velocity_y > 0)
            {
                velocity_y = 0;
            }
            ground_grace = on_ground ? coyote_seconds : Math.Max(0, ground_grace - seconds);
            if (jump_buffer > 0 && ground_grace > 0)
            {
                velocity_y = -spec.jump_speed * jump_scale;
                ground_grace = 0;
                jump_buffer = 0;
                jump_cut_allowed = true;
                jumped_this_step = true;
            }
            else
            {
                jump_buffer = Math.Max(0, jump_buffer - seconds);
            }

            double pull = velocity_y < 0 ? rising_gravity : falling_gravity;
            velocity_y = Math.Min(maximum_fall_speed, velocity_y + pull * seconds);

            // Letting go of jump while still rising fast cuts the rise in half, once.
            if (!jump_held && jump_cut_allowed && velocity_y < -jump_cut_speed)
            {
                velocity_y *= 0.5;
                jump_cut_allowed = false;
            }
            if (velocity_y >= 0)
            {
                jump_cut_allowed = false;
            }
        }

        // Moves `value` toward `target` by at most `largest_step`, landing exactly on it.
        static double move_toward(double value, double target, double largest_step)
        {
            if (Math.Abs(target - value) <= largest_step)
            {
                return target;
            }
            return value + Math.Sign(target - value) * largest_step;
        }

        // A smooth 0-to-1 curve: slow to start, slow to finish.
        static double ease_in_out(double progress)
        {
            double t = Math.Max(0, Math.Min(1, progress));
            return t * t * (3 - 2 * t);
        }
    }
}
