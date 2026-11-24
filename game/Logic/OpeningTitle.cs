// Purpose: the game's start-up animation, written as a timeline. The Unity scene only asks "what
// should be on screen at this moment?" and draws the answer: how strongly each layer shows, where
// Bodhi and Em stand, which drawing each shows, and how close the camera is. It never reads a
// clock; the scene passes in the seconds since the opening began.
//
// The beats (seconds):
//   0.0  black.
//   0.4  a warm dusk sky fades up over the house with the green door.
//   1.2  a trail of paw prints appears, one print at a time, along the bottom.
//   2.0  Bodhi trots in from the left, slows, and sits by the door (4.0).
//   4.5  the door opens a crack and warm light spills out; Em appears and kneels.
//   5.8  Bodhi leans in and they hug.
//   6.5  the name "Bodhi" fades in, then "Until We Find Home" under it.
//   8.5  New Game and Continue fade in.
//   9.5  settled: the hug breathes, and the light behind the door flickers softly, for ever.
// A tap before 9.5 skips straight to the settled screen, never to a blank one.
//
// Places are in the opening's own layout units (y down, like the chapters), on a stage
// `stage_width` wide with the ground at `ground_y`; the scene turns them into Unity units with
// LayoutSpace.
using System;

namespace Bodhi.Game
{
    // One character on the title screen this frame.
    public struct OpeningCharacter
    {
        public string frame_set;   // Which drawings: "bodhi" or "her" (FrameSheets.set).
        public string animation;   // Which animation in that set: "trot", "sit", "kneel", "embrace".
        public int frame;          // Which drawing of that animation, from 0.
        public bool loops;         // True if the animation goes round (false: plays once and holds).
        public double x;           // Where the feet are, in stage units from the left.
        public double y;           // The ground line the feet stand on (y down).
        public int facing;         // 1 faces right, -1 faces left.
        public double opacity;     // 0 hidden, 1 fully shown.
    }

    // Everything on screen at one moment of the opening.
    public struct OpeningFrame
    {
        public double sky_opacity;          // The dusk sky layer.
        public double house_opacity;        // The house with the green door.
        public double door_open;            // 0 shut, 1 open a crack (as far as it goes).
        public double door_light_opacity;   // The warm light spilling out of the door.
        public int paw_prints_shown;        // How many of the trail's prints show, from the left.
        public double title_opacity;        // The name "Bodhi".
        public double subtitle_opacity;     // "Until We Find Home".
        public double buttons_opacity;      // New Game and Continue.
        public bool buttons_ready;          // True once the buttons show enough to be pressed.
        public double camera_zoom;          // 1 is the normal view; above 1 is closer.
        public OpeningCharacter dog;        // Bodhi.
        public OpeningCharacter her;        // Em.
        public bool is_skippable;           // True while a tap would skip to the settled screen.
        public bool is_finished;            // True once the settled screen shows.
    }

    public class OpeningTitle
    {
        // The stage, in layout units.
        public const double stage_width = 720;
        public const double ground_y = 360;

        // When each beat starts and ends (seconds).
        public const double sky_from = 0.4, sky_to = 1.6;
        public const double house_from = 0.6, house_to = 1.8;
        public const double paw_prints_from = 1.2, seconds_per_paw_print = 0.2;
        public const int paw_print_total = 8;
        public const double trot_from = 2.0, trot_to = 4.0;
        public const double door_from = 4.5, door_to = 5.0, light_to = 5.3;
        public const double her_from = 4.6, her_to = 5.2, kneel_from = 4.8;
        public const double hug_from = 5.8, lean_to = 6.4;
        public const double title_from = 6.5, title_to = 7.5;
        public const double subtitle_from = 7.4, subtitle_to = 8.4;
        public const double buttons_from = 8.5, buttons_to = 9.3;
        public const double settled_at = 9.5;

        // The title's words.
        public const string title_text = "Bodhi";
        public const string subtitle_text = "Until We Find Home";

        // Where they stand.
        public const double dog_start_x = -60, dog_sit_x = 300, dog_lean_x = 325, her_x = 380;

        // The camera starts this close and eases back to 1 by the time the name shows.
        public const double first_zoom = 1.12;

        // The door light breathes between this share of full and full, once every few seconds.
        public const double light_lowest = 0.84, light_period_seconds = 5.0;

        // Times beyond this (nearly four months) are treated as this, so numbers stay small.
        public const double longest_seconds = 1e7;

        double last_good_seconds = 0;
        bool skipped = false;
        double skip_offset = 0;  // Added to the time after a skip, to land on the settled screen.

        // The easing used for every fade and move, "smoothstep": it starts slowly, speeds up
        // through the middle, and slows to a gentle stop (3p² - 2p³). Anything below 0 (or not a
        // number) gives 0 and anything above 1 gives 1.
        public static double ease(double progress)
        {
            if (!(progress > 0))
            {
                return 0;
            }
            if (progress >= 1)
            {
                return 1;
            }
            return progress * progress * (3 - 2 * progress);
        }

        // How far through an eased change from `from` to `to` the time is, 0 to 1. A change that
        // takes no time is a cut: 0 before it, 1 from it on.
        public static double fade_between(double seconds, double from, double to)
        {
            if (!(to > from))
            {
                return seconds >= from ? 1 : 0;
            }
            return ease((seconds - from) / (to - from));
        }

        // Where a paw print of the trail sits across the stage (an unknown print uses the nearest).
        public static double paw_print_x(int index)
        {
            int safe_index = Math.Max(0, Math.Min(paw_print_total - 1, index));
            return 20 + safe_index * (dog_sit_x - 40) / (paw_print_total - 1);
        }

        // How far down a paw print sits: left and right paws take turns, a little apart.
        public static double paw_print_y(int index)
        {
            return ground_y + (index % 2 == 0 ? 6 : 14);
        }

        // A tap: skips to the settled screen. False if there was nothing left to skip.
        public bool skip(double seconds)
        {
            double now = effective_seconds(seconds);
            if (skipped || now >= settled_at)
            {
                return false;
            }
            skipped = true;
            skip_offset = settled_at - now;
            return true;
        }

        // What should be on screen this many seconds after the opening began.
        public OpeningFrame frame_at(double seconds)
        {
            double now = effective_seconds(seconds);
            var frame = new OpeningFrame
            {
                sky_opacity = fade_between(now, sky_from, sky_to),
                house_opacity = fade_between(now, house_from, house_to),
                door_open = fade_between(now, door_from, door_to),
                door_light_opacity = fade_between(now, door_from, light_to) * flicker(now),
                paw_prints_shown = paw_prints_at(now),
                title_opacity = fade_between(now, title_from, title_to),
                subtitle_opacity = fade_between(now, subtitle_from, subtitle_to),
                buttons_opacity = fade_between(now, buttons_from, buttons_to),
                camera_zoom = first_zoom + (1 - first_zoom) * fade_between(now, 0, title_from),
                dog = dog_at(now),
                her = her_at(now),
                is_finished = now >= settled_at,
            };
            frame.buttons_ready = frame.buttons_opacity >= 0.5;
            frame.is_skippable = !frame.is_finished;
            return frame;
        }

        // The time to show: a bad time keeps the last good one, before the start is the start,
        // a huge time is capped, and after a skip it's moved on to the settled screen.
        double effective_seconds(double seconds)
        {
            if (!double.IsNaN(seconds) && !double.IsInfinity(seconds))
            {
                last_good_seconds = Math.Max(0, Math.Min(longest_seconds, seconds));
            }
            if (!skipped)
            {
                return last_good_seconds;
            }
            return Math.Max(settled_at, Math.Min(longest_seconds, last_good_seconds + skip_offset));
        }

        // The door light's slow breathing: between light_lowest and 1.
        static double flicker(double seconds)
        {
            double wave = 0.5 + 0.5 * Math.Sin(2 * Math.PI * seconds / light_period_seconds);
            return light_lowest + (1 - light_lowest) * wave;
        }

        // How many paw prints show: the first at paw_prints_from, then one every step.
        static int paw_prints_at(double seconds)
        {
            if (seconds < paw_prints_from)
            {
                return 0;
            }
            double steps = Math.Floor((seconds - paw_prints_from) / seconds_per_paw_print) + 1;
            return (int)Math.Min(paw_print_total, steps);
        }

        // Bodhi: off to the left, trotting in and slowing, sitting by the door, leaning into the hug.
        static OpeningCharacter dog_at(double seconds)
        {
            if (seconds < trot_from)
            {
                return character("bodhi", true, "trot", dog_start_x, 1, 0, 0, 1);
            }
            if (seconds < trot_to)
            {
                // "Ease out": fast at first, slowing to a stop at the door (1 - (1 - p)²).
                double progress = (seconds - trot_from) / (trot_to - trot_from);
                double slowed = 1 - (1 - progress) * (1 - progress);
                double x = dog_start_x + (dog_sit_x - dog_start_x) * slowed;
                return character("bodhi", true, "trot", x, 1, 0, x - dog_start_x, 1);
            }
            if (seconds < hug_from)
            {
                return character("bodhi", true, "sit", dog_sit_x, 1, seconds - trot_to, 0, 1);
            }
            double lean_x = dog_sit_x + (dog_lean_x - dog_sit_x) * fade_between(seconds, hug_from, lean_to);
            return character("bodhi", true, "embrace", lean_x, 1, seconds - hug_from, 0, 1);
        }

        // Em: inside until the door opens, then in the doorway, kneeling, then hugging him.
        static OpeningCharacter her_at(double seconds)
        {
            double opacity = fade_between(seconds, her_from, her_to);
            if (seconds < kneel_from)
            {
                return character("her", false, "idle", her_x, -1, seconds, 0, opacity);
            }
            if (seconds < hug_from)
            {
                return character("her", false, "kneel", her_x, -1, seconds - kneel_from, 0, opacity);
            }
            return character("her", false, "embrace", her_x, -1, seconds - hug_from, 0, opacity);
        }

        // Makes one character's entry and picks its drawing: a walking cycle moves on by the
        // distance covered (so the feet don't slide), anything else by the time since it began.
        static OpeningCharacter character(string frame_set, bool is_dog, string animation, double x, int facing, double seconds_in, double distance, double opacity)
        {
            var playback = FlipbookChooser.playback_of(is_dog, animation);
            var drawings = FrameSheets.set(frame_set)?.animation(animation);
            int count = drawings == null ? 0 : drawings.count;
            int frame;
            if (drawings != null && drawings.cycle_length > 0)
            {
                double phase = (Math.Max(0, distance) / drawings.cycle_length) % 1;
                frame = FlipbookChooser.safe_frame(phase * count, count);
            }
            else
            {
                frame = FlipbookChooser.timed_frame(playback, count, seconds_in, false);
            }
            return new OpeningCharacter
            {
                frame_set = frame_set,
                animation = animation,
                frame = frame,
                loops = playback.mode != PlaybackMode.once,
                x = x,
                y = ground_y,
                facing = facing,
                opacity = opacity,
            };
        }
    }
}
