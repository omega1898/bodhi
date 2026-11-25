// Purpose: moves her or Bodhi through a chapter and pushes them out of anything solid: walls stop
// them, floors hold them up, ceilings bump their heads, a small step (a kerb, a plank's edge) is
// walked straight up, and someone who somehow falls out of the world is put back on the last
// safe ground. It is the browser game's move_character (prototype/web/js/game.js), kept out of
// Unity so it can be tested, and so the game feels exactly the same on every phone.
//
// The Unity scene doesn't use its own physics for them at all: it asks this where they are and
// draws them there. Positions are layout units with y pointing down.
using System;
using Bodhi.Rules;

namespace Bodhi.Game
{
    public class CharacterMover
    {
        public const double step_up_height = 14;         // A step this high is walked straight up.
        public const double fall_rescue_below = 200;     // This far below the chapter: put back.
        public const double safe_spot_seconds = 0.5;     // How often the last safe ground is noted.
        public const double longest_move_seconds = 1.0 / 120;  // Moves in small steps, so nothing thin is skipped.

        readonly double jump_scale;
        double safe_left, safe_top;
        double safe_timer = 0;

        public Character who { get; }
        public double width { get; }
        public double height { get; }
        public double size_scale { get; }   // His size against a grown dog (1 for her).
        public double left { get; private set; }
        public double top { get; private set; }
        public bool on_ground { get; private set; }

        // True if a wall stopped them while they were pushing into it (the companion then jumps).
        public bool blocked { get; private set; }

        // How far above their feet the top of that wall is (whether a jump could clear it).
        public double obstacle_height { get; private set; }

        // True if their last step left the ground in a jump (for the sound).
        public bool jumped { get; private set; }

        // How fast they actually moved sideways last step, units a second (walls included).
        public double moved_speed { get; private set; }

        // Their walking, running and jumping.
        public CharacterMotion motion { get; private set; }

        public double center_x => left + width / 2;
        public double feet_y => top + height;

        // Makes her (or him, at his size for this chapter) standing with feet at (center_x, feet_y).
        public CharacterMover(Character who, double center_x, double feet_y, double size_scale = 1, double jump_scale = 1)
        {
            this.who = who;
            this.size_scale = who == Character.dog && size_scale > 0 && !double.IsInfinity(size_scale) ? size_scale : 1;
            this.jump_scale = jump_scale;
            var spec = CharacterSpec.of(who);
            width = spec.width * this.size_scale;
            height = spec.height * this.size_scale;
            place(center_x, feet_y);
        }

        // Puts them somewhere at once (a cut in a film scene, or a Continue), standing still.
        public void place(double center_x, double feet_y)
        {
            left = usable(center_x) - width / 2;
            top = usable(feet_y) - height;
            safe_left = left;
            safe_top = top;
            motion = new CharacterMotion(who, jump_scale);
            on_ground = false;
            blocked = false;
            moved_speed = 0;
        }

        // Moves them one frame: `direction` -1, 0 or 1, `jump_held` for a smaller jump when let go
        // early, `in_water` from the scene (wading is slower), `speed_scale` for a hungry dog or a
        // trotting companion. Floors that block them and are there now (StageLook) stop them.
        public void step(int direction, bool jump_held, bool in_water, double speed_scale, double delta_seconds, ChapterStage stage, ChapterRun run)
        {
            jumped = false;
            if (!(delta_seconds > 0) || double.IsInfinity(delta_seconds))
            {
                return;
            }
            double start_left = left;
            double seconds_left = Math.Min(delta_seconds, CharacterMotion.longest_step_seconds);
            while (seconds_left > 1e-9)
            {
                double seconds = Math.Min(seconds_left, longest_move_seconds);
                seconds_left -= seconds;
                motion.step(direction, jump_held, on_ground, in_water, speed_scale, seconds);
                jumped |= motion.jumped_this_step;
                move_across(direction, seconds, stage, run);
                move_up_down(seconds, stage, run);
            }
            moved_speed = Math.Abs(left - start_left) / Math.Min(delta_seconds, CharacterMotion.longest_step_seconds);
            remember_or_rescue(Math.Min(delta_seconds, CharacterMotion.longest_step_seconds), stage);
        }

        // Sideways first, so a wall stops them without also stopping a fall.
        void move_across(int direction, double seconds, ChapterStage stage, ChapterRun run)
        {
            left += motion.velocity_x * seconds;
            blocked = false;
            foreach (var solid in stage?.solids ?? Array.Empty<StageSolid>())
            {
                if (!stops_me(solid, run) || !overlaps(left, top, solid))
                {
                    continue;
                }
                double step_needed = feet_y - solid.y;
                if (on_ground && step_needed > 0 && step_needed <= step_up_height && !is_blocked_at(left, solid.y - height, stage, run))
                {
                    top = solid.y - height;  // A small step: walk up it.
                    continue;
                }
                left = left + width / 2 < solid.x + solid.width / 2 ? solid.x - width : solid.x + solid.width;
                motion.stop_across();
                blocked = direction != 0;
                obstacle_height = feet_y - solid.y;
            }
            double chapter_width = stage != null && stage.width > 0 && !double.IsInfinity(stage.width) ? stage.width : double.MaxValue;
            if (left < 0 || left + width > chapter_width)
            {
                left = Math.Max(0, Math.Min(chapter_width - width, left));
                motion.stop_across();
                blocked = direction != 0;
                obstacle_height = double.MaxValue;  // The chapter's end can't be jumped.
            }
        }

        // Then up and down: land on floors, bump heads on ceilings.
        void move_up_down(double seconds, ChapterStage stage, ChapterRun run)
        {
            top += motion.velocity_y * seconds;
            on_ground = false;
            foreach (var solid in stage?.solids ?? Array.Empty<StageSolid>())
            {
                if (!stops_me(solid, run) || !overlaps(left, top, solid))
                {
                    continue;
                }
                if (motion.velocity_y >= 0)
                {
                    top = solid.y - height;
                    on_ground = true;
                }
                else
                {
                    top = solid.y + solid.height;
                }
                motion.stop_up_down();
            }
        }

        // Notes the last safe ground now and then; puts them back there if they fell out of the world.
        void remember_or_rescue(double seconds, ChapterStage stage)
        {
            safe_timer -= seconds;
            if (on_ground && safe_timer <= 0)
            {
                safe_left = left;
                safe_top = top;
                safe_timer = safe_spot_seconds;
            }
            double chapter_height = stage != null && stage.height > 0 && !double.IsInfinity(stage.height) ? stage.height : LayoutSpace.usual_layout_height;
            if (top > chapter_height + fall_rescue_below)
            {
                left = safe_left;
                top = safe_top;
                motion.stop_up_down();
                motion.stop_across();
            }
        }

        // True if a floor or wall stops this one and is there now.
        bool stops_me(StageSolid solid, ChapterRun run)
        {
            return StageLook.solid_blocks(solid, who) && StageLook.solid_is_present(solid, run);
        }

        // True if standing at (at_left, at_top) would put them inside something solid.
        bool is_blocked_at(double at_left, double at_top, ChapterStage stage, ChapterRun run)
        {
            foreach (var solid in stage?.solids ?? Array.Empty<StageSolid>())
            {
                if (stops_me(solid, run) && overlaps(at_left, at_top, solid))
                {
                    return true;
                }
            }
            return false;
        }

        // True if their box at (at_left, at_top) overlaps this solid.
        bool overlaps(double at_left, double at_top, StageSolid solid)
        {
            return at_left < solid.x + solid.width && at_left + width > solid.x
                && at_top < solid.y + solid.height && at_top + height > solid.y;
        }

        // A number, or 0 for nonsense.
        static double usable(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;
        }
    }
}
