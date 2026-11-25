// Purpose: decides where the character you aren't playing walks: beside you, to you in a
// reunion, to a spot the story sends him, after the ball and back with it in the fetch chapters,
// or (Bodhi, when calm) a little ahead to explore before sitting to check back. When a
// wall blocks the way it tries a jump, and at a wall too tall to clear it waits and looks back
// instead of leaping at it forever. Ported from update_companion in prototype/web/js/game.js.
//
// It only steers: it says which way to push, whether to jump, and how fast. CharacterMotion
// then moves the character, exactly as it moves the one you play. Positions are layout units
// (the centre of each character, y down), like the chapters.
using System;
using Bodhi.Rules;

namespace Bodhi.Game
{
    // What the steering needs to know about this frame. The Unity script fills it in.
    public struct CompanionView
    {
        public Character who;               // The companion: her, or Bodhi.
        public double companion_x;          // The companion's centre.
        public double companion_speed;      // The companion's sideways speed (to face you when still).
        public double leader_x;             // The centre of the one you're playing.
        public double leader_speed;         // How fast you're walking (he runs ahead only while you walk).
        public int leader_facing;           // Which way you face: 1 right, -1 left.
        public bool follow;                 // The story says the companion follows (StoryRule.follow).
        public bool reunion_running;        // A reunion is under way: he runs to her.
        public ChapterBeat beat;            // The story's beat: he explores only while they're together.
        public double closeness;            // How close he wants to be, 0 to 1 (the bond).
        public double fear;                 // How frightened he is, 0 to 1 (the bond).
        public double care_speed_scale;     // CareState.speed_scale(): a hungry dog drags. 0 means 1.
        public double jump_scale;           // His jump for his age (1 grown). 0 means 1.
        public bool on_ground;
        public bool blocked;                // The physics says a wall stops the companion.
        public double obstacle_height;      // How far above the companion's feet the wall's top is.
        public double companion_feet_y;     // Where the companion's feet are (to reach the ball).

        // His errands, which come before following (as in the browser game):
        public double? run_to_x;            // The story sends him running to this x (StageTracker).
        public bool fetching;               // Fetch is on, there's a ball, and it's his turn for it.
        public bool carrying_ball;          // He has it in his mouth: bring it back.
        public double ball_x, ball_y;       // The loose ball's centre.
        public double ball_speed_x;         // He waits for a rolling ball to slow down.
    }

    // What the companion should do this frame.
    public struct CompanionOrder
    {
        public int direction;               // -1 left, 0 stay, 1 right.
        public bool wants_jump;
        public double speed_scale;          // Passed to CharacterMotion.step.
        public double sit_seconds;          // Above 0: sit (or stand still, for her) this long.
        public int face;                    // Not 0: turn to face this way.
        public bool arrived;                // He got to run_to_x: tell StageTracker.arrived.
        public bool pick_up_ball;           // He's at the ball: pick it up (BallFlight.dog_picks_up).
        public bool drop_ball;              // He's back with it: drop it at her feet, "ball_fetched".
    }

    public class CompanionSteering
    {
        public const double her_follow_gap = 70;
        public const double extra_distance_to_set_off = 26;  // Sets off only when this much past the gap.
        public const double trot_within = 140;               // He trots when closer than this.
        public const double trot_scale = 0.7;
        public const double reunion_gap = 50;
        public const double reunion_speed_scale = 1.15;
        public const double first_run_ahead_seconds = 6;
        public const double run_ahead_distance = 230;
        public const double calm_enough_to_explore = 0.35;  // He runs ahead only below this fear.
        public const double wait_seconds = 2.5;              // At a wall he can't clear.
        public const double give_up_after_seconds = 1.4;     // Blocked this long: stop trying.
        public const double jump_rest_seconds = 0.6;         // Between two jumps at a wall.
        public const double run_to_gap = 10;                 // How close a story run gets him...
        public const double run_to_arrived = 14;             // ...and how close counts as there.
        public const double run_to_speed_scale = 1.1;
        public const double bring_back_gap = 34;             // He stops this near her with the ball...
        public const double drop_within = 40;                // ...and drops it this near.
        public const double ball_gap = 6;
        public const double ball_rolling_speed = 60;         // Faster than this, he waits for it.
        public const double pick_up_across = 20;
        public const double pick_up_up_down = 30;

        readonly Random random;
        bool following = false;
        double ahead_timer = first_run_ahead_seconds;
        double rest_seconds = 0;
        double blocked_seconds = 0;
        double jump_cooldown = 0;

        // Where he's running ahead to, or null when he isn't (for tests and the scene).
        public double? ahead_target { get; private set; }

        // Makes the steering. `random` decides how long he waits before running ahead again; a
        // test passes one with a fixed seed so the result is always the same.
        public CompanionSteering(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        // Decides this frame's order. A frame time that isn't a real number steers nowhere.
        public CompanionOrder steer(CompanionView view, double delta_seconds)
        {
            var order = new CompanionOrder { speed_scale = 1 };
            if (!(delta_seconds > 0) || double.IsInfinity(delta_seconds))
            {
                return order;
            }
            jump_cooldown = Math.Max(0, jump_cooldown - delta_seconds);
            bool is_dog = view.who == Character.dog;
            double? target_x = null;
            double stop_distance = is_dog ? 118 - clamp_unit(view.closeness) * 80 : her_follow_gap;

            if (is_dog && view.reunion_running)
            {
                target_x = view.leader_x;
                stop_distance = reunion_gap;
                order.speed_scale = reunion_speed_scale;
            }
            else if (is_dog && view.run_to_x != null && !double.IsNaN(view.run_to_x.Value) && !double.IsInfinity(view.run_to_x.Value))
            {
                target_x = view.run_to_x;
                stop_distance = run_to_gap;
                order.speed_scale = run_to_speed_scale;
                if (Math.Abs(view.companion_x - view.run_to_x.Value) < run_to_arrived)
                {
                    order.arrived = true;
                    order.sit_seconds = 2;
                    order.face = face_toward(view.companion_x, view.leader_x);
                }
            }
            else if (is_dog && view.fetching && view.carrying_ball)
            {
                target_x = view.leader_x;
                stop_distance = bring_back_gap;
                if (Math.Abs(view.leader_x - view.companion_x) < drop_within)
                {
                    order.drop_ball = true;
                    order.sit_seconds = 1.6;
                }
            }
            else if (is_dog && view.fetching && Math.Abs(view.ball_speed_x) < ball_rolling_speed)
            {
                target_x = view.ball_x;
                stop_distance = ball_gap;
                if (Math.Abs(view.ball_x - view.companion_x) < pick_up_across && Math.Abs(view.ball_y - view.companion_feet_y) < pick_up_up_down)
                {
                    order.pick_up_ball = true;
                }
            }
            else if (is_dog && view.fetching)
            {
                // The ball is still rolling fast: he stays put and watches it.
            }
            else if (is_dog && ahead_target != null)
            {
                target_x = ahead_target;
                stop_distance = 12;
                if (Math.Abs(view.companion_x - ahead_target.Value) < 16 || blocked_seconds > 0.8)
                {
                    ahead_target = null;
                    order.sit_seconds = 1.5;  // Stop, sit, and check back.
                    order.face = face_toward(view.companion_x, view.leader_x);
                }
            }
            else if (view.follow)
            {
                target_x = view.leader_x;
            }

            // His habit, from the design: runs a little ahead, then stops and checks back.
            if (is_dog && view.beat == ChapterBeat.together && view.fear < calm_enough_to_explore && !view.reunion_running && ahead_target == null)
            {
                ahead_timer -= delta_seconds;
                if (ahead_timer <= 0 && Math.Abs(view.leader_speed) > 60)
                {
                    ahead_target = view.leader_x + (view.leader_facing < 0 ? -1 : 1) * run_ahead_distance;
                    ahead_timer = 9 + random.NextDouble() * 6;
                }
            }

            if (target_x != null)
            {
                double distance = target_x.Value - view.companion_x;
                // Two thresholds: set off only when clearly too far, stop once close. With one
                // threshold the companion would twitch back and forth at the boundary.
                if (Math.Abs(distance) > stop_distance + extra_distance_to_set_off)
                {
                    following = true;
                }
                else if (Math.Abs(distance) < stop_distance)
                {
                    following = false;
                }
                if (following)
                {
                    order.direction = Math.Sign(distance);
                    if (is_dog && Math.Abs(distance) < trot_within && view.run_to_x == null)
                    {
                        order.speed_scale *= trot_scale;  // Trot when close, run when far.
                    }
                }
            }

            if (rest_seconds > 0)
            {
                rest_seconds -= delta_seconds;
                order.direction = 0;
            }
            else if (order.direction != 0 && view.blocked && view.on_ground)
            {
                blocked_seconds += delta_seconds;
                // Only try a jump if the wall could be cleared: a sensible dog doesn't leap at a
                // tall fence over and over.
                double jump_speed = CharacterSpec.of(view.who).jump_speed * usable_scale(view.jump_scale);
                double jump_reach = jump_speed * jump_speed / (2 * CharacterMotion.rising_gravity);
                if (view.obstacle_height > jump_reach)
                {
                    blocked_seconds = 2;
                }
                if (jump_cooldown <= 0 && blocked_seconds < give_up_after_seconds)
                {
                    order.wants_jump = true;
                    jump_cooldown = jump_rest_seconds;
                }
                if (blocked_seconds > give_up_after_seconds)
                {
                    // Can't get through. Wait, facing the other one.
                    blocked_seconds = 0;
                    rest_seconds = wait_seconds;
                    ahead_target = null;
                    order.sit_seconds = wait_seconds;
                    order.face = face_toward(view.companion_x, view.leader_x);
                    order.direction = 0;
                }
            }
            else if (!view.blocked)
            {
                blocked_seconds = Math.Max(0, blocked_seconds - delta_seconds);
            }

            // Standing still, the companion turns to look at you.
            if (order.direction == 0 && target_x != null && order.sit_seconds <= 0 && order.face == 0
                && Math.Abs(view.companion_speed) < 10 && Math.Abs(view.leader_x - view.companion_x) > 24)
            {
                order.face = face_toward(view.companion_x, view.leader_x);
            }
            if (is_dog)
            {
                order.speed_scale *= usable_scale(view.care_speed_scale);  // A hungry dog drags a little.
            }
            return order;
        }

        // -1 or 1: the way from one x to another (1 if they're level).
        static int face_toward(double from_x, double to_x)
        {
            return to_x < from_x ? -1 : 1;
        }

        // A scale that isn't a real number above zero counts as 1.
        static double usable_scale(double scale)
        {
            return scale > 0 && !double.IsInfinity(scale) ? scale : 1;
        }

        // Keeps a bond value between 0 and 1 (NaN becomes 0.5, the middle).
        static double clamp_unit(double value)
        {
            return double.IsNaN(value) ? 0.5 : Math.Max(0, Math.Min(1, value));
        }
    }
}
