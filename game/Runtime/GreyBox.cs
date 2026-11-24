// Purpose: draws the stand-in pictures: plain coloured boxes made in code, so the game can be
// played before a human artist paints the real characters and scenes. Everything is built from
// one white square, tinted. Nothing here is saved as a Unity asset file.
using Bodhi.Game;
using UnityEngine;

namespace Bodhi.Game
{
    public static class GreyBox
    {
        static Sprite white_square;  // Made once, shared by every box.

        // Draw orders, back to front.
        public const int sky_order = -100;
        public const int wall_order = -50;
        public const int prop_order = -20;
        public const int water_order = 15;
        public const int solid_order = 0;
        public const int thing_order = 5;
        public const int character_order = 10;
        public const int front_order = 30;

        // The 1x1 white square every box is cut from. Its pivot is its bottom-left corner.
        public static Sprite square()
        {
            if (white_square == null)
            {
                var texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.white);
                texture.filterMode = FilterMode.Point;
                texture.Apply();
                white_square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0, 0), 1);
                white_square.name = "grey box";
            }
            return white_square;
        }

        // A box with its bottom-left corner at `corner` (world metres), this big, in this colour.
        public static SpriteRenderer make_box(Transform parent, string name, Vector2 corner, Vector2 size, Color color, int order)
        {
            var box = new GameObject(name);
            box.transform.SetParent(parent, false);
            box.transform.localPosition = new Vector3(corner.x, corner.y, 0);
            box.transform.localScale = new Vector3(Mathf.Max(0.001f, size.x), Mathf.Max(0.001f, size.y), 1);
            var drawing = box.AddComponent<SpriteRenderer>();
            drawing.sprite = square();
            drawing.color = color;
            drawing.sortingOrder = order;
            return drawing;
        }

        // A layout box (top-left corner and size in layout units) as a world box.
        public static SpriteRenderer make_layout_box(Transform parent, string name, LayoutSpace space, double left, double top, double width, double height, Color color, int order)
        {
            var corner = new Vector2((float)space.to_world_x(left), (float)space.to_world_y(top + height));
            var size = new Vector2((float)(width / LayoutSpace.units_per_metre), (float)(height / LayoutSpace.units_per_metre));
            return make_box(parent, name, corner, size, color, order);
        }

        // A colour written the browser's way, or the fallback if it can't be read.
        public static Color color_of(string text, Color fallback)
        {
            if (!StageLook.parse_color(text, out StageColor color))
            {
                return fallback;
            }
            return new Color((float)color.red, (float)color.green, (float)color.blue, (float)color.alpha);
        }
    }
}
