// Purpose: the screens outside a chapter, drawn with Unity's UI on their own canvas under the
// game's HUD: the start-up animation behind the title (OpeningTitle says what shows when), the
// back story's cards before a new game (BackStory), and the memory box of keepsakes (MemoryBox).
// Every choice of what to show is made by that tested Logic code; this file only draws it.
using System;
using Bodhi.Rules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bodhi.Game
{
    public class TitleScreens
    {
        // The title and memory box are drawn for a 1280 by 720 screen; the scaler fits any other.
        const float screen_width = 1280, screen_height = 720;
        const float stage_to_screen = screen_width / (float)OpeningTitle.stage_width;
        const float her_height_on_stage = 175;  // How tall Em stands in the opening, in stage units.

        static readonly Color dusk_top = new Color(0.27f, 0.24f, 0.38f);
        static readonly Color dusk_glow = new Color(0.93f, 0.62f, 0.46f);
        static readonly Color ground_color = new Color(0.16f, 0.15f, 0.17f);
        static readonly Color wall_color = new Color(0.55f, 0.47f, 0.42f);
        static readonly Color door_color = new Color(0.2f, 0.42f, 0.3f);
        static readonly Color light_color = new Color(1f, 0.84f, 0.52f);
        static readonly Color paw_color = new Color(0.1f, 0.08f, 0.08f, 0.55f);
        static readonly Color words_color = new Color(0.98f, 0.95f, 0.88f);

        readonly Font font;
        readonly RectTransform canvas;
        readonly RectTransform stage;          // The opening's scene; scaled for the camera zoom.
        readonly Image sky, glow, ground, wall, door_light, door;
        readonly Image[] paw_prints = new Image[OpeningTitle.paw_print_total];
        readonly Image dog_drawing, her_drawing;
        readonly Text title, subtitle;
        readonly CanvasGroup title_buttons;
        readonly Button continue_button, memory_box_button;
        readonly GameObject back_story_screen, memory_box_screen;
        readonly Image back_story_backdrop, hold_ring;
        readonly Text back_story_text, memory_box_text;

        OpeningTitle opening;
        BackStory story;
        double seconds_since_opening, seconds_held;
        float stage_units_per_sprite_unit = 0;     // Worked out from Em's first drawing.
        Action on_new_game, on_continue, on_memory_box, on_back_story_finished, on_memory_box_closed;

        // Builds every screen, hidden, on a canvas just under the HUD's.
        public TitleScreens(Transform parent)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var holder = new GameObject("Title screens", typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            var canvas_part = holder.AddComponent<Canvas>();
            canvas_part.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas_part.sortingOrder = 90;  // Under the HUD (100), so its fades still cover these.
            var scaler = holder.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(screen_width, screen_height);
            scaler.matchWidthOrHeight = 0.5f;
            holder.AddComponent<GraphicRaycaster>();
            canvas = holder.GetComponent<RectTransform>();

            var black = make_image(canvas, "black", Color.black, 0, 0, 1, 1);
            black.raycastTarget = false;
            stage = new GameObject("opening", typeof(RectTransform)).GetComponent<RectTransform>();
            stage.SetParent(canvas, false);
            stretch(stage, 0, 0, 1, 1);
            sky = make_image(stage, "sky", dusk_top, 0, 0, 1, 1);
            glow = make_image(stage, "glow", dusk_glow, 0, 0.1f, 1, 0.45f);
            ground = make_image(stage, "ground", ground_color, 0, 0, 1, 0.12f);
            wall = place_box(stage, "house", wall_color, 410, 120, 260, 230);
            door_light = place_box(stage, "door light", light_color, 440, 120, 60, 120);
            door = place_box(stage, "green door", door_color, 440, 120, 60, 120);
            door.rectTransform.pivot = new Vector2(1, 0);  // Opens toward its hinge on the right.
            var dot = round_sprite();
            for (int index = 0; index < paw_prints.Length; index++)
            {
                paw_prints[index] = place_box(stage, "paw print", paw_color, (float)OpeningTitle.paw_print_x(index), (float)(OpeningTitle.ground_y - OpeningTitle.paw_print_y(index)), 9, 6);
                paw_prints[index].sprite = dot;
            }
            her_drawing = make_image(stage, "Em", Color.white, 0, 0, 0, 0);
            dog_drawing = make_image(stage, "Bodhi", Color.white, 0, 0, 0, 0);

            title = make_text(canvas, "title", 96, 0.1f, 0.68f, 0.9f, 0.9f);
            title.text = OpeningTitle.title_text;
            subtitle = make_text(canvas, "subtitle", 32, 0.1f, 0.6f, 0.9f, 0.7f);
            subtitle.text = OpeningTitle.subtitle_text;
            var buttons_holder = new GameObject("buttons", typeof(RectTransform));
            buttons_holder.transform.SetParent(canvas, false);
            stretch(buttons_holder.GetComponent<RectTransform>(), 0, 0, 1, 1);
            title_buttons = buttons_holder.AddComponent<CanvasGroup>();
            make_button(buttons_holder.transform, "New Game", 0.2f, 0.12f, () => on_new_game?.Invoke());
            continue_button = make_button(buttons_holder.transform, "Continue", 0.5f, 0.12f, () => on_continue?.Invoke());
            memory_box_button = make_button(buttons_holder.transform, "Memory box", 0.8f, 0.12f, () => on_memory_box?.Invoke());

            back_story_screen = make_image(canvas, "back story", Color.black, 0, 0, 1, 1).gameObject;
            back_story_backdrop = make_image(back_story_screen.transform, "picture", Color.clear, 0, 0, 1, 1);
            back_story_text = make_text(back_story_screen.transform, "words", 38, 0.12f, 0.3f, 0.88f, 0.7f);
            hold_ring = make_image(back_story_screen.transform, "hold to skip", new Color(1, 1, 1, 0.5f), 0.93f, 0.08f, 0.93f, 0.08f);
            hold_ring.rectTransform.sizeDelta = new Vector2(44, 44);
            hold_ring.sprite = dot;
            hold_ring.type = Image.Type.Filled;
            hold_ring.fillMethod = Image.FillMethod.Radial360;

            memory_box_screen = make_image(canvas, "memory box", new Color(0.07f, 0.06f, 0.08f, 1f), 0, 0, 1, 1).gameObject;
            memory_box_text = make_text(memory_box_screen.transform, "keepsakes", 20, 0.06f, 0.16f, 0.94f, 0.95f);
            memory_box_text.alignment = TextAnchor.UpperLeft;
            make_button(memory_box_screen.transform, "Back", 0.5f, 0.08f, () => on_memory_box_closed?.Invoke());
            hide();
        }

        // Starts the opening, which settles on the title with its buttons.
        public void show_opening(bool has_save, Action new_game, Action continue_game, Action memory_box)
        {
            hide();
            SpriteLibrary.prepare(new[] { "her", "bodhi" });
            on_new_game = new_game;
            on_continue = continue_game;
            on_memory_box = memory_box;
            continue_button.gameObject.SetActive(has_save);
            memory_box_button.gameObject.SetActive(has_save);
            opening = new OpeningTitle();
            seconds_since_opening = 0;
            set_opening_shown(true);
            draw_opening(opening.frame_at(0));
        }

        // Tells the story so far, card by card, then calls `finished`.
        public void show_back_story(string dog_name, Action finished)
        {
            hide();
            story = new BackStory(dog_name);
            seconds_held = 0;
            on_back_story_finished = finished;
            back_story_screen.SetActive(true);
        }

        // The memory box: every chapter's keepsakes, found or still to find.
        public void show_memory_box(MemoryBox box, Action close)
        {
            on_memory_box_closed = close;
            memory_box_text.text = describe(box);
            memory_box_screen.SetActive(true);
        }

        // Hides every screen.
        public void hide()
        {
            opening = null;
            story = null;
            set_opening_shown(false);
            back_story_screen.SetActive(false);
            memory_box_screen.SetActive(false);
        }

        // One frame: moves on whichever screen is showing.
        public void update(float delta_seconds)
        {
            bool tapped = Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
            bool finger_down = Pointer.current != null && Pointer.current.press.isPressed;
            if (opening != null && !memory_box_screen.activeSelf)
            {
                seconds_since_opening += delta_seconds;
                var frame = opening.frame_at(seconds_since_opening);
                if (tapped && frame.is_skippable && opening.skip(seconds_since_opening))
                {
                    frame = opening.frame_at(seconds_since_opening);
                }
                draw_opening(frame);
            }
            if (story != null)
            {
                update_back_story(delta_seconds, tapped, finger_down);
            }
        }

        // Shows one moment of the opening.
        void draw_opening(OpeningFrame frame)
        {
            float zoom = (float)frame.camera_zoom;
            stage.localScale = new Vector3(zoom, zoom, 1);
            sky.color = with_alpha(dusk_top, frame.sky_opacity);
            glow.color = with_alpha(dusk_glow, frame.sky_opacity * 0.8);
            ground.color = with_alpha(ground_color, frame.house_opacity);
            wall.color = with_alpha(wall_color, frame.house_opacity);
            door.color = with_alpha(door_color, frame.house_opacity);
            door_light.color = with_alpha(light_color, frame.door_light_opacity);
            door.rectTransform.localScale = new Vector3(1 - 0.7f * (float)frame.door_open, 1, 1);  // Swings open: seen edge-on, it narrows.
            for (int index = 0; index < paw_prints.Length; index++)
            {
                paw_prints[index].enabled = index < frame.paw_prints_shown;
            }
            draw_character(her_drawing, frame.her);
            draw_character(dog_drawing, frame.dog);
            title.color = with_alpha(words_color, frame.title_opacity);
            subtitle.color = with_alpha(words_color, frame.subtitle_opacity);
            title_buttons.alpha = (float)frame.buttons_opacity;
            title_buttons.interactable = frame.buttons_ready;
            title_buttons.blocksRaycasts = frame.buttons_ready;
        }

        // Puts one character's drawing where the opening says, feet on its ground line.
        void draw_character(Image drawing, OpeningCharacter character)
        {
            var sprite = SpriteLibrary.sprite(character.frame_set, character.animation, character.frame);
            drawing.enabled = sprite != null && character.opacity > 0;
            if (!drawing.enabled)
            {
                return;  // Pictures missing: nothing to draw, and nothing breaks.
            }
            if (stage_units_per_sprite_unit <= 0)
            {
                var standing = SpriteLibrary.sprite("her", "idle", 0);
                float her_height = standing != null ? standing.bounds.size.y : sprite.bounds.size.y;
                stage_units_per_sprite_unit = her_height_on_stage / Mathf.Max(0.01f, her_height);
            }
            drawing.sprite = sprite;
            drawing.color = with_alpha(Color.white, character.opacity);
            var rect = drawing.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);  // The feet.
            rect.sizeDelta = (Vector2)sprite.bounds.size * stage_units_per_sprite_unit * stage_to_screen;
            rect.anchoredPosition = to_screen((float)character.x, (float)character.y);
            rect.localScale = new Vector3(character.facing < 0 ? -1 : 1, 1, 1);
        }

        // One frame of the back story: time, a tap for the next card, or a long hold to skip.
        void update_back_story(float delta_seconds, bool tapped, bool finger_down)
        {
            story.pass_time(delta_seconds);
            seconds_held = finger_down ? seconds_held + delta_seconds : 0;
            if (tapped)
            {
                story.tap();
            }
            if (finger_down)
            {
                story.hold(seconds_held);
            }
            if (story.is_finished)
            {
                var finished = on_back_story_finished;
                hide();
                finished?.Invoke();
                return;
            }
            back_story_text.text = story.text;
            back_story_text.color = with_alpha(words_color, story.card_opacity);
            back_story_backdrop.color = with_alpha(picture_color(story.picture), story.card_opacity * 0.6);
            hold_ring.fillAmount = (float)BackStory.hold_progress(seconds_held);
        }

        // A soft colour standing in for each card's picture until painted pictures exist.
        static Color picture_color(string picture)
        {
            switch (picture)
            {
                case "river": return new Color(0.17f, 0.27f, 0.36f);
                case "green_door": return new Color(0.13f, 0.27f, 0.2f);
                case "empty_bowl": return new Color(0.3f, 0.22f, 0.17f);
                case "photo": return new Color(0.36f, 0.3f, 0.22f);
                case "road": return new Color(0.28f, 0.3f, 0.24f);
                case "asleep": return new Color(0.2f, 0.16f, 0.24f);
                default: return Color.black;
            }
        }

        // The memory box as lines of text: each reached chapter, its keepsakes and their memories.
        static string describe(MemoryBox box)
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine("MEMORY BOX   " + box.keepsakes_found + " of " + box.keepsake_total + " keepsakes");
            foreach (var chapter in box.chapters)
            {
                if (!chapter.unlocked)
                {
                    continue;  // Chapters not reached yet stay a surprise.
                }
                lines.AppendLine();
                lines.AppendLine(chapter.number + ". " + chapter.title + "   " + chapter.keepsakes_found + "/" + chapter.keepsake_total);
                foreach (var keepsake in chapter.keepsakes)
                {
                    lines.AppendLine(keepsake.found ? "   * " + keepsake.name + ": " + keepsake.memory_text : "   ? " + keepsake.hint);
                }
            }
            return lines.ToString();
        }

        // Shows or hides everything that belongs to the opening and title.
        void set_opening_shown(bool shown)
        {
            stage.gameObject.SetActive(shown);
            title.gameObject.SetActive(shown);
            subtitle.gameObject.SetActive(shown);
            title_buttons.gameObject.SetActive(shown);
        }

        // A box placed in stage units: left edge, ground-up bottom (y up from the floor), size.
        Image place_box(RectTransform parent, string name, Color color, float left, float bottom_above_ground, float width, float height)
        {
            var image = make_image(parent, name, color, 0, 0, 0, 0);
            var rect = image.rectTransform;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = to_screen(left, (float)OpeningTitle.ground_y - bottom_above_ground);
            rect.sizeDelta = new Vector2(width, height) * stage_to_screen;
            image.raycastTarget = false;
            return image;
        }

        // From the opening's stage units (y down, ground at ground_y) to screen units (y up from
        // the bottom, the ground 12% up the screen).
        static Vector2 to_screen(float stage_x, float stage_y)
        {
            float ground_on_screen = screen_height * 0.12f;
            return new Vector2(stage_x * stage_to_screen, ground_on_screen + ((float)OpeningTitle.ground_y - stage_y) * stage_to_screen);
        }

        // A colour with its opacity set from a 0 to 1 number.
        static Color with_alpha(Color color, double opacity)
        {
            color.a *= Mathf.Clamp01((float)opacity);
            return color;
        }

        // A soft round dot, made once in code, for paw prints and the hold ring.
        static Sprite round_sprite()
        {
            const int size = 32;
            var picture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                    picture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(size / 2f - distance)));
                }
            }
            picture.Apply();
            return Sprite.Create(picture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        // A tappable button with words on it, centred on a share of the screen.
        Button make_button(Transform parent, string label, float x, float y, Action on_click)
        {
            var image = make_image(parent, label, new Color(1, 1, 1, 0.14f), x, y, x, y);
            image.rectTransform.sizeDelta = new Vector2(300, 64);
            var button = image.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => on_click());
            var words = make_text(image.transform, "label", 30, 0, 0, 1, 1);
            words.text = label;
            return button;
        }

        // A coloured rectangle stretched between two corners given as shares of its parent.
        static Image make_image(Transform parent, string name, Color color, float left, float bottom, float right, float top)
        {
            var holder = new GameObject(name, typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            var image = holder.AddComponent<Image>();
            image.color = color;
            stretch(image.rectTransform, left, bottom, right, top);
            return image;
        }

        // Centred words in the built-in font, stretched like make_image.
        Text make_text(Transform parent, string name, int size, float left, float bottom, float right, float top)
        {
            var holder = new GameObject(name, typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            var text = holder.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = words_color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            stretch(text.rectTransform, left, bottom, right, top);
            return text;
        }

        // Pins a rectangle's corners to shares of its parent, with no extra size.
        static void stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
