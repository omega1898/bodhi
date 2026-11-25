// Purpose: hands out the animation drawings as Unity sprites. The pictures (game/Resources/Frames)
// are loaded and every drawing's sprite made when a chapter loads (prepare), never mid-chapter,
// because loading a picture while playing would make the game stutter. Sets not needed by the
// chapter are let go, so the phone holds only the drawings in use (her, and the grown dog's or the
// puppy's, and Biscuit's when he visits).
using System.Collections.Generic;
using UnityEngine;

namespace Bodhi.Game
{
    public static class SpriteLibrary
    {
        // Per set: its pictures, and per animation the sprite of each drawing.
        static readonly Dictionary<string, List<Texture2D>> pictures = new Dictionary<string, List<Texture2D>>();
        static readonly Dictionary<string, Dictionary<string, Sprite[]>> sprites = new Dictionary<string, Dictionary<string, Sprite[]>>();

        // Loads every set named and lets go of the rest. A set whose pictures are missing is left
        // out, and its character is drawn as a grey box instead.
        public static void prepare(IEnumerable<string> set_names)
        {
            var wanted = new HashSet<string>(set_names);
            foreach (string loaded in new List<string>(sprites.Keys))
            {
                if (!wanted.Contains(loaded))
                {
                    let_go(loaded);
                }
            }
            foreach (string set_name in wanted)
            {
                if (set_name != null && !sprites.ContainsKey(set_name))
                {
                    load(set_name);
                }
            }
        }

        // The sprite for one drawing, or null if it isn't loaded.
        public static Sprite sprite(string set_name, string animation_name, int frame)
        {
            if (set_name == null || animation_name == null || !sprites.TryGetValue(set_name, out var animations))
            {
                return null;
            }
            if (!animations.TryGetValue(animation_name, out var drawings) || frame < 0 || frame >= drawings.Length)
            {
                return null;
            }
            return drawings[frame];
        }

        // The sharper close-up drawing when its set is loaded ("her_close" for "her"), otherwise
        // the normal one, so a close-up still shows something if its sharper set is missing.
        public static Sprite close_up(string set_name, string animation_name, int frame)
        {
            // Unity objects are checked with == null, never ??, because Unity's own null check
            // also catches a sprite that has been let go.
            Sprite sharper = sprite(FrameSheets.close_up_name(set_name), animation_name, frame);
            if (sharper != null)
            {
                return sharper;
            }
            return sprite(set_name, animation_name, frame);
        }

        // True if a set's sprites are ready.
        public static bool has(string set_name) => set_name != null && sprites.ContainsKey(set_name);

        // Loads one set's pictures and makes a sprite for each drawing, its pivot on the feet.
        static void load(string set_name)
        {
            var set = FrameSheets.set(set_name);
            if (set == null)
            {
                return;
            }
            // Pixels per metre: the picture's pixels per game unit times game units per metre.
            float pixels_per_metre = (float)(set.pixels_per_unit * LayoutSpace.units_per_metre);
            var loaded_pictures = new List<Texture2D>();
            var animations = new Dictionary<string, Sprite[]>();
            foreach (var animation in set.animations)
            {
                var picture = Resources.Load<Texture2D>(animation.picture);
                if (picture == null)  // Unity objects are checked with == null, never ??.
                {
                    Debug.LogWarning("Missing drawings: Resources/" + animation.picture + ".png. Run art/tools/export_sprites.py.");
                    foreach (var made in loaded_pictures)
                    {
                        Resources.UnloadAsset(made);
                    }
                    return;
                }
                loaded_pictures.Add(picture);
                var drawings = new Sprite[animation.count];
                for (int index = 0; index < animation.count; index++)
                {
                    var frame = animation.frames[index];
                    var box = new Rect(frame.x, frame.y, frame.width, frame.height);
                    var pivot = new Vector2((float)(frame.pivot_x / frame.width), (float)(frame.pivot_y / frame.height));
                    // FullRect: each sprite is a plain rectangle, two triangles, the cheapest shape to draw.
                    drawings[index] = Sprite.Create(picture, box, pivot, pixels_per_metre, 0, SpriteMeshType.FullRect);
                    drawings[index].name = set_name + "_" + animation.name + "_" + index;
                }
                animations[animation.name] = drawings;
            }
            pictures[set_name] = loaded_pictures;
            sprites[set_name] = animations;
        }

        // Lets go of one set's sprites and pictures.
        static void let_go(string set_name)
        {
            foreach (var drawings in sprites[set_name].Values)
            {
                foreach (var drawing in drawings)
                {
                    Object.Destroy(drawing);
                }
            }
            sprites.Remove(set_name);
            if (pictures.TryGetValue(set_name, out var set_pictures))
            {
                foreach (var picture in set_pictures)
                {
                    Resources.UnloadAsset(picture);
                }
                pictures.Remove(set_name);
            }
        }
    }
}
