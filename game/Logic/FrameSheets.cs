// Purpose: the list of every animation drawing the Unity app can show, read from
// FrameSheets.Data.cs (written by art/tools/export_sprites.py). For each drawing it knows which
// picture in game/Resources/Frames it's in, the box it fills there, where its feet are, and a
// few points from the drawing tool (the feet, and where a carried thing is held). Plain C#, so
// the numbers are checked by tests without Unity.
using System.Collections.Generic;

namespace Bodhi.Game
{
    // One drawing: its box in its picture and its points.
    public class SpriteFrame
    {
        // The box, in pixels. Unity counts rows up from the bottom of a picture, so y is the
        // bottom edge of the box.
        public int x { get; }
        public int y { get; }
        public int width { get; }
        public int height { get; }

        // Where the feet are, in pixels from the box's bottom-left corner. The drawing is placed
        // with this point on the character's feet.
        public double pivot_x { get; }
        public double pivot_y { get; }

        // The feet as drawn, in game units from the feet point, y down as in the drawing tool:
        // two (her) or four (a dog) pairs of x, y.
        public double[] feet { get; }

        // Where a carried thing goes, in game units from the feet point, y down: a dog's mouth,
        // her hand.
        public double hold_x { get; }
        public double hold_y { get; }

        // Makes a drawing's entry (FrameSheets.Data.cs calls this).
        public SpriteFrame(int x, int y, int width, int height, double pivot_x, double pivot_y, double[] feet, double[] hold)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.pivot_x = pivot_x;
            this.pivot_y = pivot_y;
            this.feet = feet ?? new double[0];
            hold_x = hold != null && hold.Length > 0 ? hold[0] : 0;
            hold_y = hold != null && hold.Length > 1 ? hold[1] : 0;
        }

        // How many feet this drawing has.
        public int foot_count => feet.Length / 2;
    }

    // One animation: its picture and its drawings in order.
    public class FrameAnimation
    {
        public string name { get; }

        // The picture's path for Resources.Load, like "Frames/her_walk".
        public string picture { get; }
        public int picture_width { get; }
        public int picture_height { get; }

        // For walking cycles: how far (game units) the body moves in one round of drawings. The
        // drawings move on by distance walked, so planted feet stay planted. 0 for the rest.
        public double cycle_length { get; }

        public SpriteFrame[] frames { get; }

        // Makes an animation's entry (FrameSheets.Data.cs calls this).
        public FrameAnimation(string name, string picture, int picture_width, int picture_height, double cycle_length, SpriteFrame[] frames)
        {
            this.name = name;
            this.picture = picture;
            this.picture_width = picture_width;
            this.picture_height = picture_height;
            this.cycle_length = cycle_length;
            this.frames = frames ?? new SpriteFrame[0];
        }

        // How many drawings it has.
        public int count => frames.Length;
    }

    // One character's drawings: her, Bodhi, Bodhi as a puppy, or Biscuit.
    public class FrameSet
    {
        public string name { get; }

        // How many picture pixels make one game unit in this set's pictures.
        public double pixels_per_unit { get; }

        readonly Dictionary<string, FrameAnimation> by_name = new Dictionary<string, FrameAnimation>();

        // Makes a set's entry (FrameSheets.Data.cs calls this).
        public FrameSet(string name, double pixels_per_unit, FrameAnimation[] animations)
        {
            this.name = name;
            this.pixels_per_unit = pixels_per_unit;
            foreach (var animation in animations ?? new FrameAnimation[0])
            {
                by_name[animation.name] = animation;
            }
        }

        // Every animation in the set.
        public IEnumerable<FrameAnimation> animations => by_name.Values;

        // The animation with this name, or null if the set has none.
        public FrameAnimation animation(string animation_name)
        {
            if (animation_name == null)
            {
                return null;
            }
            return by_name.TryGetValue(animation_name, out var found) ? found : null;
        }
    }

    // Every set of drawings, made once and shared.
    public static partial class FrameSheets
    {
        static FrameSet[] made;

        // Every set.
        public static FrameSet[] all => made ?? (made = make_all());

        // Ages at which Bodhi is drawn as a puppy (big head, short legs), as in the browser game's
        // dog_growth table (prototype/web/js/levels.js). From six months he's drawn grown.
        static readonly string[] puppy_ages = { "eight_weeks", "four_months", "five_months" };

        // Which set draws Bodhi at this age ("bodhi_puppy" or "bodhi"); an unknown age is grown.
        public static string dog_set_for_age(string dog_age)
        {
            return System.Array.IndexOf(puppy_ages, dog_age) >= 0 ? "bodhi_puppy" : "bodhi";
        }

        // The close-up sets end with this: sharper drawings ("her_close", "bodhi_close") of a few
        // animations, for when the camera is near (the opening's hug, the title screen).
        public const string close_up_ending = "_close";

        // The close-up set drawn from this set ("her" gives "her_close"), or null for no set.
        public static string close_up_name(string set_name)
        {
            return set_name == null ? null : set_name + close_up_ending;
        }

        // True if this is a close-up set rather than a normal one.
        public static bool is_close_up(string set_name)
        {
            return set_name != null && set_name.EndsWith(close_up_ending);
        }

        // The widest or tallest a set's pictures may be. Normal pictures stay within 2048 pixels,
        // which every iPhone handles easily. Close-ups may reach 4096 (every iPhone that runs
        // Metal can show that), because they're loaded only while a close-up is on screen.
        public static int largest_picture(string set_name)
        {
            return is_close_up(set_name) ? 4096 : 2048;
        }

        // The set with this name ("her", "bodhi", "bodhi_puppy", "biscuit"), or null.
        public static FrameSet set(string set_name)
        {
            foreach (var found in all)
            {
                if (found.name == set_name)
                {
                    return found;
                }
            }
            return null;
        }
    }
}
