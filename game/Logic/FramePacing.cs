// Purpose: how many frames a second the game asks for. A screen that refreshes 120 times a second
// (ProMotion iPhones and iPads) shows twice as many in-between positions as a 60 one, so movement
// glides instead of stepping; asking for fewer than the screen shows makes some pictures stay up
// twice as long as others, which reads as a stutter. So the game asks for exactly the screen's
// rate, kept within sensible limits.
using System;

namespace Bodhi.Game
{
    public static class FramePacing
    {
        public const int fallback_rate = 60;   // When the screen doesn't say (or says nonsense).
        public const int lowest_rate = 30;
        public const int highest_rate = 120;   // No phone screen shows more; more only costs battery.

        // The frame rate to ask for, from the screen's refresh rate (frames a second).
        public static int frames_per_second(double screen_refresh_rate)
        {
            if (double.IsNaN(screen_refresh_rate) || double.IsInfinity(screen_refresh_rate) || screen_refresh_rate < lowest_rate)
            {
                return fallback_rate;
            }
            return (int)Math.Min(highest_rate, Math.Round(screen_refresh_rate));
        }
    }
}
