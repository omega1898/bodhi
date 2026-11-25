// Purpose: the game's start-up animation, written as a timeline. The Unity scene only asks "what
// should be on screen at this moment?" and draws the answer: how strongly the sky and the scene
// show, how far the camera has tilted down from the aurora, and when the title and buttons fade
// in. It never reads a clock; the scene passes in the seconds since the opening began.
//
// The beats (seconds):
//   0.0  black.
//   0.3  the picture fades up: the camera is looking up at a starry sky and the green aurora.
//   2.6  the camera tilts slowly down, past snowy mountains, to the snowfield where Em and Bodhi
//        are building a snowman (what they do is SnowmanPlay's job, not this file's).
//   5.6  the name "Bodhi" fades in, then "A Light in the Window" under it.
//   7.6  New Game and Continue fade in.
//   8.6  settled: the two of them keep playing in the snow for as long as the title shows.
// A tap before 8.6 skips straight to the settled screen, never to a blank one.
using System;

namespace Bodhi.Game
{
    // Everything the opening decides at one moment.
    public struct OpeningFrame
    {
        public double scene_opacity;        // 0 all black, 1 the picture fully up.
        public double sky_opacity;          // The starry sky and the aurora.
        public double camera_tilt;          // 0 looking up at the aurora, 1 settled on the snowfield.
        public double title_opacity;        // The name "Bodhi".
        public double subtitle_opacity;     // "A Light in the Window".
        public double buttons_opacity;      // New Game and Continue.
        public bool buttons_ready;          // True once the buttons show enough to be pressed.
        public bool is_skippable;           // True while a tap would skip to the settled screen.
        public bool is_finished;            // True once the settled screen shows.
    }

    public class OpeningTitle
    {
        // When each beat starts and ends (seconds).
        public const double scene_from = 0.3, scene_to = 2.4;
        public const double sky_from = 0.3, sky_to = 2.0;
        public const double tilt_from = 2.6, tilt_to = 6.2;
        public const double title_from = 5.6, title_to = 6.6;
        public const double subtitle_from = 6.5, subtitle_to = 7.5;
        public const double buttons_from = 7.6, buttons_to = 8.4;
        public const double settled_at = 8.6;

        // The title's words.
        public const string title_text = "Bodhi";
        public const string subtitle_text = "A Light in the Window";  // The Christmas story's name.

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
                scene_opacity = fade_between(now, scene_from, scene_to),
                sky_opacity = fade_between(now, sky_from, sky_to),
                camera_tilt = fade_between(now, tilt_from, tilt_to),
                title_opacity = fade_between(now, title_from, title_to),
                subtitle_opacity = fade_between(now, subtitle_from, subtitle_to),
                buttons_opacity = fade_between(now, buttons_from, buttons_to),
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
    }
}
