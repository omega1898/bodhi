// Purpose: the ball in the fetch chapters: carried, thrown, bouncing and rolling to a stop. It is
// the browser game's update_ball, throw and drop_ball_for (prototype/web/js/game.js), kept out of
// Unity so it can be tested. The ball is small and simple, so it moves by these numbers against
// the chapter's floors and walls rather than by Unity's physics: that way it bounces the same on
// every phone, and the same as in the browser game.
//
// Positions are layout units with y pointing down; (x, y) is the ball's centre.
using System;
using Bodhi.Rules;

namespace Bodhi.Game
{
    // Who has the ball.
    public enum BallHolder
    {
        nobody,  // On the ground or in the air.
        human,   // In her hand.
        dog,     // In his mouth.
    }

    public class BallFlight
    {
        public const double gravity = 1440;          // 80% of a jump's pull: the ball floats a little.
        public const double maximum_fall_speed = 900;
        public const double throw_speed_across = 520;
        public const double throw_speed_up = 380;
        public const double radius = 5;
        public const double bounce_above_speed = 120;  // Lands faster than this: it bounces.
        public const double bounce_keep = 0.45;        // Each bounce keeps this much of the speed...
        public const double roll_keep = 0.86;          // ...and each landing this much of the roll.
        public const double wall_keep = 0.5;           // Off a wall it comes back at half speed.
        public const double edge_margin = 6;           // Kept this far inside the chapter.
        public const double longest_step_seconds = 1.0 / 60;  // Moves in small steps so it never jumps through a thin plank.

        public double x { get; private set; }
        public double y { get; private set; }
        public double velocity_x { get; private set; }
        public double velocity_y { get; private set; }
        public BallHolder held_by { get; private set; } = BallHolder.nobody;

        // True after he brought it back: he waits for her to throw it again, instead of picking
        // it straight back up (a small dog stood close enough to do that forever).
        public bool waiting_for_throw { get; private set; }

        // A ball lying at (x, y).
        public BallFlight(double x, double y)
        {
            this.x = usable(x);
            this.y = usable(y);
        }

        // She picks it up.
        public void pick_up()
        {
            held_by = BallHolder.human;
            velocity_x = 0;
            velocity_y = 0;
        }

        // She throws it the way she faces (1 right, -1 left). Only from her hand.
        public bool throw_ball(int facing)
        {
            if (held_by != BallHolder.human)
            {
                return false;
            }
            held_by = BallHolder.nobody;
            waiting_for_throw = false;
            velocity_x = (facing < 0 ? -1 : 1) * throw_speed_across;
            velocity_y = -throw_speed_up;
            return true;
        }

        // He picks it up in his mouth. Only a loose ball he isn't waiting to see thrown.
        public bool dog_picks_up()
        {
            if (held_by != BallHolder.nobody || waiting_for_throw)
            {
                return false;
            }
            held_by = BallHolder.dog;
            velocity_x = 0;
            velocity_y = 0;
            return true;
        }

        // He drops it at her feet: just in front of her, resting on the ground. The round is over
        // until she throws it again. Only from his mouth.
        public bool drop_at_her_feet(double human_x, double human_feet_y, int human_facing)
        {
            if (held_by != BallHolder.dog)
            {
                return false;
            }
            held_by = BallHolder.nobody;
            x = usable(human_x) + (human_facing < 0 ? -18 : 18);
            y = usable(human_feet_y) - 6;
            velocity_x = 0;
            velocity_y = 0;
            waiting_for_throw = true;
            return true;
        }

        // Moves on by one frame. A held ball goes where its holder is: in his mouth (nearer for a
        // puppy) or in her hand. A loose one falls, bounces and rolls against what blocks him.
        public void step(double delta_seconds, ChapterStage stage, ChapterRun run, BallHolderPlace holder)
        {
            if (held_by != BallHolder.nobody)
            {
                bool dog = held_by == BallHolder.dog;
                double size = holder.size_scale > 0 && !double.IsInfinity(holder.size_scale) ? holder.size_scale : 1;
                int facing = holder.facing < 0 ? -1 : 1;
                x = usable(holder.x) + facing * (dog ? 30 * size : 12);
                y = usable(holder.top_y) + (dog ? 12 * size : 42);
                return;
            }
            if (!(delta_seconds > 0) || double.IsInfinity(delta_seconds))
            {
                return;
            }
            double left = Math.Min(delta_seconds, 0.25);
            while (left > 0)
            {
                double seconds = Math.Min(left, longest_step_seconds);
                left -= seconds;
                move(seconds, stage, run);
            }
        }

        // One small step of falling and rolling.
        void move(double seconds, ChapterStage stage, ChapterRun run)
        {
            var solids = stage?.solids ?? Array.Empty<StageSolid>();
            velocity_y = Math.Min(maximum_fall_speed, velocity_y + gravity * seconds);
            x += velocity_x * seconds;
            foreach (var solid in solids)
            {
                if (blocks_ball(solid, run) && touches(solid))
                {
                    x = velocity_x > 0 ? solid.x - radius : solid.x + solid.width + radius;
                    velocity_x *= -wall_keep;
                }
            }
            y += velocity_y * seconds;
            foreach (var solid in solids)
            {
                if (blocks_ball(solid, run) && touches(solid))
                {
                    if (velocity_y > 0)
                    {
                        y = solid.y - radius;
                        velocity_y = Math.Abs(velocity_y) > bounce_above_speed ? -velocity_y * bounce_keep : 0;
                        velocity_x *= roll_keep;
                    }
                    else
                    {
                        y = solid.y + solid.height + radius;
                        velocity_y = 0;
                    }
                }
            }
            double width = stage != null && stage.width > 0 && !double.IsInfinity(stage.width) ? stage.width : double.MaxValue;
            x = Math.Max(edge_margin, Math.Min(width - edge_margin, x));
        }

        // The ball stops where he would: floors and walls that block him and are there now.
        static bool blocks_ball(StageSolid solid, ChapterRun run)
        {
            return StageLook.solid_is_present(solid, run) && StageLook.solid_blocks(solid, Character.dog);
        }

        // True if the ball overlaps this box.
        bool touches(StageSolid solid)
        {
            return x + radius > solid.x && x - radius < solid.x + solid.width
                && y + radius > solid.y && y - radius < solid.y + solid.height;
        }

        // A number, or 0 for nonsense.
        static double usable(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;
        }
    }

    // Where whoever holds the ball is this frame: their centre, the top of their head, which way
    // they face and (for him) how big he is against a grown dog.
    public struct BallHolderPlace
    {
        public double x, top_y;
        public int facing;
        public double size_scale;
    }
}
