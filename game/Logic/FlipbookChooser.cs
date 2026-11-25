// Purpose: picks which drawing to show for her or a dog, every frame, the way the browser game's
// flipbook.js does (ported line for line, so both versions move alike). Walking and running
// drawings move on by distance walked, so a planted foot stays planted on the ground; a jump's
// drawing follows the vertical speed; a skid's drawing follows how fast it's still sliding;
// landings, turns, stops and getting up play once over whatever else is going on; everything
// else plays at a steady rate. Plain C#, so all of it is tested without Unity.
using System;

namespace Bodhi.Game
{
    // How a timed animation plays.
    public enum PlaybackMode
    {
        loop,   // Round and round.
        once,   // Through once, then holds the last drawing.
        intro,  // The first drawings once (getting down), then loops the rest (breathing while down).
    }

    // One timed animation's playback: drawings a second, and for an intro how many drawings it is
    // and how fast the rest loops.
    public class Playback
    {
        public PlaybackMode mode { get; }
        public double rate { get; }
        public int intro_frames { get; }
        public double loop_rate { get; }

        // Makes one playback entry.
        public Playback(PlaybackMode mode, double rate, int intro_frames = 0, double loop_rate = 0)
        {
            this.mode = mode;
            this.rate = rate;
            this.intro_frames = intro_frames;
            this.loop_rate = loop_rate;
        }
    }

    // What the chooser needs to know about a character this frame.
    public struct FlipbookInput
    {
        public double center_x;        // Game units.
        public double velocity_x;      // Units a second; right is positive.
        public double velocity_y;      // Units a second; down is positive (the layout's y).
        public bool on_ground;
        public int facing;             // 1 right, -1 left.
        public string pose;            // From pose_of: "stand", "walk", "jump", "sit", "kneel"...
        public double wag;             // A dog's happiness, 0 to 1: a happier dog wags faster.
        public double size_scale;      // A puppy's size against a grown dog (1 when grown).
        public double jump_scale;      // A puppy's jump against a grown dog's.
        public double time_seconds;    // The game's clock.
    }

    // What to draw: an animation, one of its drawings, and which way the drawing faces.
    public struct FlipbookChoice
    {
        public string animation;
        public int frame;
        public int facing;
    }

    // Remembers one character's last frame and picks its next drawing.
    public class FlipbookChooser
    {
        public const double getting_up_rate = 78;      // Drawings a second, getting-down played backwards.
        public const double walk_start_phase = 0.2;    // A first step: just before the back foot swings.
        public const double biggest_step = 40;         // A bigger move in one frame is a scene change.
        public const double brisk_speed = 100;         // Stopping from this fast plays a stop.
        public const double moving_speed = 18;         // Slower than this is standing still.
        public const double dog_sits_after_seconds = 2.5;

        readonly FrameSet drawings;
        readonly bool is_dog;
        readonly double jump_rise_speed, jump_fall_speed;

        // What it remembers from the last frame.
        bool started = false;
        double last_x, moved, phase, fast_until = -100, last_time = 0;
        bool was_on_ground;
        int last_facing;
        string last_pose;
        double pose_started = -100;
        bool intro_allowed = false, starting_stride = false;
        string gait = null;
        double idle_started = 0;
        string last_animation = "idle";
        double[] last_feet = null;
        OneShot one_shot = null;

        // A short move playing over the pose: its name, when it started, where it may show
        // ("stand", "ground" or "slow"), and whether it's getting-down played backwards.
        class OneShot
        {
            public string name;
            public double started;
            public string when;
            public bool backwards;
        }

        // What it showed last ("walk 12"), for tests.
        public string shown { get; private set; } = "";

        // Makes a chooser for one character's drawings.
        public FlipbookChooser(FrameSet drawings)
        {
            this.drawings = drawings;
            is_dog = drawings != null && drawings.name != "her";
            jump_rise_speed = (is_dog ? CharacterSpec.his : CharacterSpec.her).jump_speed;  // Take-off speed.
            jump_fall_speed = 700;                  // A fast fall.
        }

        // The drawing pose from what the character is doing (the browser game's update_pose).
        // story_pose is a pose the story asked for ("sit", "lean"...), or "" for none.
        public static string pose_of(bool is_dog, string story_pose, bool on_ground, double velocity_x, double idle_seconds)
        {
            if (!string.IsNullOrEmpty(story_pose) && story_pose != "stand")
            {
                return story_pose;
            }
            if (!on_ground)
            {
                return "jump";
            }
            if (Math.Abs(velocity_x) > moving_speed)
            {
                return "walk";
            }
            if (is_dog && idle_seconds > dog_sits_after_seconds)
            {
                return "sit";
            }
            return "stand";
        }

        // How each timed animation plays (flipbook.js's flipbook_playback). Rates are drawings a
        // second: the drawing tool makes many in-betweens, so they're high, and each move still
        // takes the time it always did.
        public static Playback playback_of(bool is_dog, string animation)
        {
            switch (animation)
            {
                case "idle": return new Playback(PlaybackMode.loop, is_dog ? 12 : 10);
                case "land": return new Playback(PlaybackMode.once, 65);
                case "turn": return new Playback(PlaybackMode.once, 40);
                case "stop": return new Playback(PlaybackMode.once, 32);
                case "kneel": return new Playback(PlaybackMode.intro, 36, 13, 6);
                case "sit": return is_dog ? new Playback(PlaybackMode.intro, 36, 13, 6) : new Playback(PlaybackMode.loop, 8);
                case "embrace": return is_dog ? new Playback(PlaybackMode.intro, 31, 13, 8) : new Playback(PlaybackMode.loop, 8);
                case "sniff": return new Playback(PlaybackMode.loop, 20);
                case "bark": return new Playback(PlaybackMode.once, 25);
                case "sleep": return new Playback(PlaybackMode.loop, 6);
                case "lean": return new Playback(PlaybackMode.loop, 8);
                default: return new Playback(PlaybackMode.loop, 10);
            }
        }

        // How low a pose is ("upright" unless it's down), and which getting-down drawings bring
        // it back up when played backwards (null: it just pops up). Moving to another level gets
        // up first; moving within a level (her kneeling into the hug) needs neither.
        public static (string level, string up_from) level_of(bool is_dog, string pose)
        {
            if (is_dog)
            {
                switch (pose)
                {
                    case "sit": return ("sitting", "sit");
                    case "sleep": return ("lying", null);
                    case "embrace": return ("hugging", "embrace");
                }
            }
            else
            {
                switch (pose)
                {
                    case "kneel": return ("kneeling", "kneel");
                    case "embrace": return ("kneeling", "kneel");
                    case "sit": return ("sitting", null);
                }
            }
            return ("upright", null);
        }

        // Picks what to draw this frame.
        public FlipbookChoice choose(FlipbookInput input)
        {
            if (drawings == null)
            {
                return new FlipbookChoice { animation = "idle", frame = 0, facing = 1 };
            }
            input = cleaned(input);
            remember(input);
            var choice = pick(input);
            var animation = drawings.animation(choice.animation);
            if (animation == null || animation.count == 0)
            {
                choice.animation = "idle";
                animation = drawings.animation("idle");
            }
            choice.frame = animation == null ? 0 : safe_frame(choice.frame, animation.count);
            if (choice.facing == 0)
            {
                choice.facing = facing_of(input);
            }
            last_animation = choice.animation;
            shown = choice.animation + " " + choice.frame;
            if (animation != null && animation.count > 0)
            {
                // The feet as drawn on screen (turned the way the drawing faces), for the next stride.
                var feet = animation.frames[choice.frame].feet;
                last_feet = new double[feet.Length];
                for (int index = 0; index < feet.Length; index++)
                {
                    last_feet[index] = index % 2 == 0 ? feet[index] * choice.facing : feet[index];
                }
            }
            return choice;
        }

        // The input with every number that isn't a real one (not a number, infinite) made safe:
        // speeds and places become 0, a bad clock keeps the last good time.
        FlipbookInput cleaned(FlipbookInput input)
        {
            input.center_x = real_or(input.center_x, 0);
            input.velocity_x = real_or(input.velocity_x, 0);
            input.velocity_y = real_or(input.velocity_y, 0);
            input.wag = real_or(input.wag, 0);
            input.size_scale = real_or(input.size_scale, 1);
            input.jump_scale = real_or(input.jump_scale, 1);
            input.time_seconds = real_or(input.time_seconds, last_time);
            last_time = input.time_seconds;
            return input;
        }

        // The value if it's a real number, otherwise the fallback.
        static double real_or(double value, double fallback)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? fallback : value;
        }

        // A facing that is always 1 or -1.
        static int facing_of(FlipbookInput input) => input.facing < 0 ? -1 : 1;

        // Notices landings, turns, stops and pose changes, and how far it moved since last frame.
        void remember(FlipbookInput input)
        {
            string pose = string.IsNullOrEmpty(input.pose) ? "stand" : input.pose;
            int facing = facing_of(input);
            double x = input.center_x;
            double time = input.time_seconds;
            if (!started)
            {
                started = true;
                last_x = x;
                was_on_ground = input.on_ground;
                last_facing = facing;
                last_pose = pose;
            }
            double step = Math.Abs(x - last_x);
            moved = step < biggest_step ? step : 0;
            last_x = x;
            if (Math.Abs(input.velocity_x) > brisk_speed)
            {
                fast_until = time + 0.2;
            }
            if (input.on_ground && !was_on_ground)
            {
                start_one_shot("land", time, "ground", false);
            }
            if (facing != last_facing && input.on_ground)
            {
                start_one_shot("turn", time, "ground", false);
            }
            if (pose != last_pose)
            {
                pose_started = time;
                starting_stride = pose == "walk";
                bool turning = one_shot != null && one_shot.name == "turn";
                if (last_pose == "walk" && pose == "stand" && time < fast_until && !turning)
                {
                    start_one_shot("stop", time, "stand", false);
                }
                var old_level = level_of(is_dog, last_pose);
                var new_level = level_of(is_dog, pose);
                if (old_level.level != new_level.level && old_level.up_from != null)
                {
                    start_one_shot(old_level.up_from, time, "slow", true);
                }
                // Getting-down drawings play only when coming from upright (or once back up).
                intro_allowed = old_level.level == "upright" || (old_level.level != new_level.level && old_level.up_from != null);
            }
            was_on_ground = input.on_ground;
            last_facing = facing;
            last_pose = pose;
        }

        // Starts a short move that plays over whatever the pose would show.
        void start_one_shot(string name, double time, string when, bool backwards)
        {
            one_shot = new OneShot { name = name, started = time, when = when, backwards = backwards };
        }

        // The choice before it's checked against the drawings that exist.
        FlipbookChoice pick(FlipbookInput input)
        {
            string pose = last_pose;
            double speed = Math.Abs(input.velocity_x);
            double time = input.time_seconds;
            int facing = last_facing;

            // In the air: one arc of drawings, by vertical speed. Take-off shows the first, the
            // top of the jump the middle one, a fast fall the last.
            if (pose == "jump")
            {
                int count = frame_count("jump");
                double rise = jump_rise_speed * (input.jump_scale > 0 ? input.jump_scale : 1);
                double through_arc = input.velocity_y < 0 ? 1 + input.velocity_y / rise : 1 + input.velocity_y / jump_fall_speed;
                starting_stride = true;  // Landing into a run starts the stride from the feet.
                return choice("jump", (int)Math.Round(clamp(through_arc, 0, 2) * (count - 1) / 2), 0);
            }
            // A skid: reversed at speed, still sliding the old way, drawn facing the slide.
            if (pose == "walk" && Math.Sign(input.velocity_x) == -facing && speed > 50)
            {
                int count = frame_count("skid");
                double slowing = 1 - clamp((speed - 50) / 150, 0, 1);
                starting_stride = true;
                return choice("skid", (int)Math.Round(slowing * (count - 1)), -facing);
            }
            // A landing, turn, stop or getting up, while slow enough to show.
            if (one_shot != null)
            {
                var shot = one_shot;
                bool where_allowed = shot.when == "stand" ? pose == "stand" : shot.when == "ground" ? pose == "stand" || pose == "walk" : true;
                var source = playback_of(is_dog, shot.name);
                int count = shot.backwards ? source.intro_frames : frame_count(shot.name);
                double rate = shot.backwards ? getting_up_rate : source.rate;
                double speed_limit = shot.backwards ? 150 : 110;
                double seconds = time - shot.started;
                if (count > 0 && speed < speed_limit && where_allowed && seconds < count / rate)
                {
                    int index = safe_frame(seconds * rate, count);
                    return choice(shot.name, shot.backwards ? count - 1 - index : index, 0);
                }
                one_shot = null;
                pose_started = Math.Max(pose_started, time);
            }
            if (pose == "walk")
            {
                return stride(input, speed);
            }
            // Everything else: the pose's own animation, or standing still. Idle starts from the
            // home stance each time, exactly where landings, turns and stops end.
            string animation_name = pose == "stand" || drawings.animation(pose) == null ? "idle" : pose;
            var playback = playback_of(is_dog, animation_name);
            if (is_dog && animation_name == "idle")
            {
                playback = new Playback(PlaybackMode.loop, 12 + clamp(input.wag, 0, 1) * 28);  // Happier: faster wag.
            }
            double began = pose_started;
            if (animation_name == "idle")
            {
                if (last_animation != "idle")
                {
                    idle_started = time;
                }
                began = idle_started;
            }
            return choice(animation_name, timed_frame(playback, frame_count(animation_name), time - began, !intro_allowed), 0);
        }

        // Walk or run for her; walk, trot or gallop for a dog: by speed, moved on by distance covered.
        FlipbookChoice stride(FlipbookInput input, double speed)
        {
            string chosen = gait_for(is_dog, gait, speed);
            var entry = drawings.animation(chosen);
            if ((entry == null || entry.count == 0) && chosen == "walk" && is_dog)
            {
                chosen = "trot";  // Drawings made before dogs had a walk: trot instead.
                entry = drawings.animation(chosen);
            }
            if (entry == null || entry.count == 0)
            {
                return choice("idle", 0, 0);
            }
            // A new gait carries on from the drawing whose feet are closest to the ones just
            // shown: a walk and a trot put their feet down in a different order, so the same
            // point in both cycles can have the feet in quite different places.
            bool changed_gait = gait != null && chosen != gait;
            gait = chosen;
            double size = input.size_scale > 0 ? input.size_scale : 1;
            if (starting_stride || (changed_gait && last_feet != null))
            {
                phase = last_feet != null ? closest_phase(entry, last_feet, last_facing) : walk_start_phase;
                starting_stride = false;
            }
            else if (entry.cycle_length > 0)
            {
                phase = (phase + moved / (entry.cycle_length * size)) % 1;
            }
            return choice(chosen, (int)Math.Floor(phase * entry.count), 0);
        }

        // Her gaits and a dog's, slowest first, and the speeds between them. A real person
        // breaks into a run somewhere above a brisk walk; a dog walks, trots at the pace of a
        // person hurrying, and gallops only when it really runs.
        static readonly string[] her_gaits = { "walk", "run" };
        static readonly double[] her_gait_speeds = { 160 };
        static readonly string[] dog_gaits = { "walk", "trot", "gallop" };
        static readonly double[] dog_gait_speeds = { 85, 200 };
        public const double gait_margin = 10;  // Each switch has two lines this far either side.

        // Which gait to use at a speed, given the one in use now (null when just setting off).
        // It moves up a gait only clearly above a line and down only clearly below it, so a
        // speed close to a line doesn't flicker between two gaits.
        public static string gait_for(bool is_dog, string current, double speed)
        {
            var gaits = is_dog ? dog_gaits : her_gaits;
            var lines = is_dog ? dog_gait_speeds : her_gait_speeds;
            int index = Math.Max(0, Array.IndexOf(gaits, current));
            while (index < lines.Length && speed > lines[index] + gait_margin)
            {
                index += 1;
            }
            while (index > 0 && speed < lines[index - 1] - gait_margin)
            {
                index -= 1;
            }
            return gaits[index];
        }

        // The point in a cycle whose feet are closest to the feet just shown, so setting off,
        // landing into a run or coming out of a skid carries on from where the feet already are.
        public static double closest_phase(FrameAnimation entry, double[] feet_on_screen, int facing)
        {
            int best_index = 0;
            double best_gap = double.MaxValue;
            for (int index = 0; index < entry.count; index++)
            {
                var feet = entry.frames[index].feet;
                double gap = 0;
                for (int value = 0; value + 1 < feet.Length && value + 1 < feet_on_screen.Length; value += 2)
                {
                    double across = feet[value] * facing - feet_on_screen[value];
                    double down = feet[value + 1] - feet_on_screen[value + 1];
                    gap += Math.Sqrt(across * across + down * down);
                }
                if (gap < best_gap)
                {
                    best_gap = gap;
                    best_index = index;
                }
            }
            return (best_index + 0.5) / Math.Max(1, entry.count);
        }

        // Which drawing of a timed animation shows `seconds` after it started. skip_intro goes
        // straight to the loop of an intro animation (already down, so no getting down again).
        public static int timed_frame(Playback playback, int frame_count, double seconds, bool skip_intro)
        {
            if (frame_count <= 0)
            {
                return 0;
            }
            double rate = Math.Max(0, playback.rate);
            double played = Math.Max(0, seconds) * rate;
            if (playback.mode == PlaybackMode.once)
            {
                return safe_frame(played, frame_count);
            }
            if (playback.mode == PlaybackMode.intro)
            {
                int intro_frames = Math.Min(frame_count, Math.Max(1, playback.intro_frames));
                int loop_count = frame_count - intro_frames;
                if (loop_count <= 0 || (!skip_intro && played < intro_frames))
                {
                    return safe_frame(played, intro_frames);
                }
                double looped = (skip_intro ? Math.Max(0, seconds) : seconds - intro_frames / rate) * playback.loop_rate;
                return intro_frames + safe_frame(Math.Floor(looped) % loop_count, loop_count);
            }
            return safe_frame(Math.Floor(played) % frame_count, frame_count);
        }

        // Turns any number into a drawing that exists: whole, from 0 to frame_count - 1. Odd
        // input (not a number, negative, huge) becomes a real drawing instead of nothing.
        public static int safe_frame(double index, int frame_count)
        {
            if (frame_count <= 0 || double.IsNaN(index) || index < 0)
            {
                return 0;
            }
            return (int)Math.Min(frame_count - 1, Math.Floor(Math.Min(index, int.MaxValue)));
        }

        // How many drawings an animation has (0 if it's missing).
        int frame_count(string animation_name)
        {
            var animation = drawings.animation(animation_name);
            return animation == null ? 0 : animation.count;
        }

        // Makes a choice.
        static FlipbookChoice choice(string animation, int frame, int facing)
        {
            return new FlipbookChoice { animation = animation, frame = frame, facing = facing };
        }

        // Keeps a value between two limits (a value that isn't a number becomes the lower one).
        static double clamp(double value, double lowest, double highest)
        {
            if (double.IsNaN(value))
            {
                return lowest;
            }
            return Math.Max(lowest, Math.Min(highest, value));
        }
    }
}
