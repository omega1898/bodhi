// Purpose: turns positions in a chapter's layout into positions in the Unity world, and back.
// The chapters were written for the browser game, in layout units with y pointing down (0 at
// the top, 600 at the bottom). Unity uses metres with y pointing up. Every conversion happens
// here, in one place, so the two can never drift apart.
//
// Scale: 50 layout units make a metre, so she stands 86 units, about 1.7 metres, tall.
using System;

namespace Bodhi.Game
{
    public readonly struct LayoutSpace
    {
        public const double units_per_metre = 50;
        public const double usual_layout_height = 600;  // The browser game's chapters.

        // The layout's bottom edge, in layout units. It becomes height 0 in the world.
        public readonly double layout_height;

        // Makes the conversion for a chapter this tall. A height that isn't a real number above
        // zero is treated as the usual 600.
        public LayoutSpace(double layout_height)
        {
            bool usable = layout_height > 0 && !double.IsInfinity(layout_height);  // NaN fails the first check.
            this.layout_height = usable ? layout_height : usual_layout_height;
        }

        // A layout x (units from the left) as a world x (metres from the left).
        public double to_world_x(double layout_x)
        {
            return layout_x / units_per_metre;
        }

        // A layout y (units down from the top) as a world y (metres up from the bottom).
        public double to_world_y(double layout_y)
        {
            return (layout_height - layout_y) / units_per_metre;
        }

        // A world x back as a layout x.
        public double to_layout_x(double world_x)
        {
            return world_x * units_per_metre;
        }

        // A world y back as a layout y.
        public double to_layout_y(double world_y)
        {
            return layout_height - world_y * units_per_metre;
        }

        // A sideways speed in units a second as metres a second.
        public double speed_to_world_x(double layout_speed)
        {
            return layout_speed / units_per_metre;
        }

        // An up-and-down speed: rising is negative in the layout and positive in the world.
        public double speed_to_world_y(double layout_speed)
        {
            return -layout_speed / units_per_metre;
        }

        // A sideways speed in metres a second back as units a second.
        public double speed_to_layout_x(double world_speed)
        {
            return world_speed * units_per_metre;
        }

        // An up-and-down speed back into the layout's direction.
        public double speed_to_layout_y(double world_speed)
        {
            return -world_speed * units_per_metre;
        }
    }
}
