// Purpose: everything drawn on top of the game: the story lines (subtitles), the current goal,
// the words over the "use" button, a film's big caption, the night's darkness, fades and
// lightning, the touch buttons on a phone, the heart pop when care grows the bond, and the
// pause, after-chapter and end-of-year screens. It is built in code with Unity's UI (uGUI) and its built-in font, so it needs no
// imported assets; an artist's fonts and panels replace the plain ones later. The title lives in TitleScreens.
//
// It shows what it's told and reports button presses; ChapterDirector decides everything else.
using System;
using Bodhi.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Bodhi.Game
{
    public class GameHud
    {
        public const float fade_seconds = 1.2f;
        public const float flash_seconds = 0.25f;
        public const float bond_note_seconds = 3f;  // How long a heart pop stays; it fades in its last second.

        static readonly Color her_color = new Color(0.98f, 0.95f, 0.88f);
        static readonly Color dog_color = new Color(1f, 0.85f, 0.55f);
        static readonly Color note_color = new Color(0.8f, 0.88f, 0.95f);

        readonly PlayerControls controls;
        readonly Font font;
        readonly RectTransform canvas;
        readonly Text subtitle, objective, prompt, caption, bond_note;
        readonly Image darkness, fade, flash;
        readonly GameObject touch_buttons;
        readonly GameObject pause_screen, aftermath_screen, year_end_screen;
        readonly Text aftermath_text, year_end_text;
        readonly Button aftermath_home_button, year_end_home_button;
        Action on_resume, on_next, on_go_home;
        float fade_left, flash_left, bond_note_left;

        // Builds the whole overlay and the EventSystem that lets taps and clicks reach it.
        public GameHud(Transform parent, PlayerControls controls)
        {
            this.controls = controls;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var holder = new GameObject("HUD", typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            var canvas_part = holder.AddComponent<Canvas>();
            canvas_part.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas_part.sortingOrder = 100;
            var scaler = holder.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);  // Sizes below are for this screen; it scales.
            scaler.matchWidthOrHeight = 0.5f;
            holder.AddComponent<GraphicRaycaster>();
            canvas = holder.GetComponent<RectTransform>();
            make_event_system(parent);

            darkness = make_image(canvas, "darkness", Color.clear, 0, 0, 1, 1);
            darkness.raycastTarget = false;
            caption = make_text(canvas, "caption", 46, TextAnchor.MiddleCenter, 0.1f, 0.6f, 0.9f, 0.8f);
            objective = make_text(canvas, "objective", 20, TextAnchor.UpperLeft, 0.02f, 0.86f, 0.6f, 0.97f);
            subtitle = make_text(canvas, "subtitle", 28, TextAnchor.LowerCenter, 0.12f, 0.14f, 0.88f, 0.3f);
            prompt = make_text(canvas, "prompt", 22, TextAnchor.MiddleCenter, 0.5f, 0.5f, 0.5f, 0.5f);
            prompt.rectTransform.sizeDelta = new Vector2(360, 40);
            bond_note = make_text(canvas, "bond note", 26, TextAnchor.UpperRight, 0.55f, 0.6f, 0.98f, 0.86f);
            add_shadow(bond_note);
            add_shadow(subtitle);
            add_shadow(prompt);
            add_shadow(caption);

            touch_buttons = build_touch_buttons();
            touch_buttons.SetActive(Touchscreen.current != null || Application.isMobilePlatform);


            pause_screen = make_screen("pause", new Color(0, 0, 0, 0.7f));
            make_text(pause_screen.transform, "paused", 48, TextAnchor.MiddleCenter, 0.1f, 0.55f, 0.9f, 0.75f).text = "Paused";
            make_button(pause_screen.transform, "Carry on", 0.5f, 0.4f, () => on_resume?.Invoke());

            aftermath_screen = make_screen("aftermath", new Color(0.05f, 0.05f, 0.07f, 0.94f));
            aftermath_text = make_text(aftermath_screen.transform, "lines", 26, TextAnchor.MiddleCenter, 0.12f, 0.3f, 0.88f, 0.85f);
            make_button(aftermath_screen.transform, "Next", 0.38f, 0.18f, () => on_next?.Invoke());
            aftermath_home_button = make_button(aftermath_screen.transform, "Go home", 0.62f, 0.18f, () => on_go_home?.Invoke());

            year_end_screen = make_screen("year end", new Color(0.05f, 0.05f, 0.07f, 1f));
            year_end_text = make_text(year_end_screen.transform, "lines", 30, TextAnchor.MiddleCenter, 0.12f, 0.25f, 0.88f, 0.8f);
            year_end_home_button = make_button(year_end_screen.transform, "Go home", 0.5f, 0.15f, () => on_go_home?.Invoke());

            fade = make_image(canvas, "fade", Color.clear, 0, 0, 1, 1);
            fade.raycastTarget = false;
            flash = make_image(canvas, "flash", Color.clear, 0, 0, 1, 1);
            flash.raycastTarget = false;
            hide_screens();
        }

        // The pause panel, or none.
        public void show_pause(bool paused, Action resume)
        {
            on_resume = resume;
            pause_screen.SetActive(paused);
        }

        // The quiet screen after a chapter: its lines, then Next, or Go home (left out when
        // `go_home` is null).
        public void show_aftermath(string text, Action next, Action go_home)
        {
            hide_screens();
            on_next = next;
            on_go_home = go_home;
            aftermath_text.text = text ?? "";
            aftermath_home_button.gameObject.SetActive(go_home != null);
            aftermath_screen.SetActive(true);
        }

        // The end of the year, with Go home (left out when `go_home` is null).
        public void show_year_end(string text, Action go_home)
        {
            hide_screens();
            on_go_home = go_home;
            year_end_text.text = text ?? "";
            year_end_home_button.gameObject.SetActive(go_home != null);
            year_end_screen.SetActive(true);
        }

        // The heart pop when care grows the bond ("♥ +2"), with any lines worth celebrating
        // under it ("Bodhi learned Sit!"). Top right, gone after a few seconds. Nothing to show
        // leaves the last one alone.
        public void show_bond_note(string heart_pop, string lines)
        {
            string text = heart_pop ?? "";
            if (!string.IsNullOrEmpty(lines))
            {
                text = text == "" ? lines : text + "\n" + lines;
            }
            if (text == "")
            {
                return;
            }
            bond_note.text = text;
            bond_note_left = bond_note_seconds;
        }

        // Hides the pause, after-chapter and end-of-year screens.
        public void hide_screens()
        {
            pause_screen.SetActive(false);
            aftermath_screen.SetActive(false);
            year_end_screen.SetActive(false);
        }

        // The story line on screen, coloured by who says it.
        public void set_subtitle(bool showing, string text, StoryVoice voice)
        {
            subtitle.text = showing ? text ?? "" : "";
            subtitle.color = voice == StoryVoice.dog ? dog_color : voice == StoryVoice.note ? note_color : her_color;
            subtitle.fontStyle = voice == StoryVoice.dog ? FontStyle.Italic : FontStyle.Normal;
        }

        // The current goal, top left ("" for none).
        public void set_objective(string text)
        {
            objective.text = text ?? "";
        }

        // A film's big caption ("" clears it).
        public void set_caption(string text)
        {
            caption.text = text ?? "";
        }

        // What the "use" button offers, floating over the spot in the world it's about.
        public void set_prompt(string text, Vector3 world_spot, Camera camera)
        {
            prompt.text = text ?? "";
            if (string.IsNullOrEmpty(text) || camera == null)
            {
                return;
            }
            var screen = camera.WorldToScreenPoint(world_spot);
            var scale = canvas.lossyScale.x > 0 ? canvas.lossyScale.x : 1;
            prompt.rectTransform.anchorMin = prompt.rectTransform.anchorMax = Vector2.zero;
            prompt.rectTransform.anchoredPosition = new Vector2(screen.x / scale, screen.y / scale);
        }

        // How dark the night is (0 none to just under 1), in the chapter's own darkness colour.
        public void set_darkness(double amount, Color tint)
        {
            tint.a = Mathf.Clamp01((float)amount);
            darkness.color = tint;
        }

        // A soft cut from black.
        public void fade_from_black()
        {
            fade_left = fade_seconds;
        }

        // A lightning flash.
        public void lightning()
        {
            flash_left = flash_seconds;
        }

        // Each frame: the fade, the flash and the heart pop wear off.
        public void update(float delta_seconds)
        {
            fade_left = Mathf.Max(0, fade_left - delta_seconds);
            flash_left = Mathf.Max(0, flash_left - delta_seconds);
            bond_note_left = Mathf.Max(0, bond_note_left - delta_seconds);
            bond_note.color = new Color(her_color.r, her_color.g, her_color.b, Mathf.Clamp01(bond_note_left));
            fade.color = new Color(0, 0, 0, fade_left / fade_seconds);
            flash.color = new Color(1, 1, 1, 0.8f * flash_left / flash_seconds);
        }

        // The on-screen buttons for a phone: walk left and right on the left, the rest on the right.
        GameObject build_touch_buttons()
        {
            var holder = new GameObject("touch buttons", typeof(RectTransform));
            holder.transform.SetParent(canvas, false);
            stretch(holder.GetComponent<RectTransform>(), 0, 0, 1, 1);
            make_touch(holder.transform, "<", GameButton.left, 0.07f, 0.12f);
            make_touch(holder.transform, ">", GameButton.right, 0.19f, 0.12f);
            make_touch(holder.transform, "Jump", GameButton.jump, 0.9f, 0.12f);
            make_touch(holder.transform, "Use", GameButton.use, 0.79f, 0.2f);
            make_touch(holder.transform, "Low", GameButton.low, 0.9f, 0.3f);
            make_touch(holder.transform, "Call", GameButton.call, 0.68f, 0.12f);
            make_touch(holder.transform, "Swap", GameButton.switch_character, 0.9f, 0.88f);
            make_touch(holder.transform, "Skip", GameButton.skip, 0.79f, 0.88f);
            make_touch(holder.transform, "II", GameButton.pause, 0.04f, 0.92f);
            return holder;
        }

        // One touch button: presses the game button while a finger is on it.
        void make_touch(Transform parent, string label, GameButton which, float x, float y)
        {
            var image = make_image(parent, "touch " + label, new Color(1, 1, 1, 0.18f), x, y, x, y);
            image.rectTransform.sizeDelta = new Vector2(96, 96);
            var words = make_text(image.transform, "label", 24, TextAnchor.MiddleCenter, 0, 0, 1, 1);
            words.text = label;
            words.raycastTarget = false;
            var touch = image.gameObject.AddComponent<TouchButton>();
            touch.controls = controls;
            touch.button = which;
        }

        // A full-screen panel for a menu.
        GameObject make_screen(string name, Color color)
        {
            return make_image(canvas, name, color, 0, 0, 1, 1).gameObject;
        }

        // A tappable button with words on it.
        Button make_button(Transform parent, string label, float x, float y, Action on_click)
        {
            var image = make_image(parent, label, new Color(1, 1, 1, 0.12f), x, y, x, y);
            image.rectTransform.sizeDelta = new Vector2(300, 64);
            var button = image.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => on_click());
            var words = make_text(image.transform, "label", 30, TextAnchor.MiddleCenter, 0, 0, 1, 1);
            words.text = label;
            words.raycastTarget = false;
            return button;
        }

        // A coloured rectangle stretched between two corners given as shares of its parent (0 to 1).
        static Image make_image(Transform parent, string name, Color color, float left, float bottom, float right, float top)
        {
            var holder = new GameObject(name, typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            var image = holder.AddComponent<Image>();
            image.color = color;
            stretch(image.rectTransform, left, bottom, right, top);
            return image;
        }

        // Words in the built-in font, stretched between two corners like make_image.
        Text make_text(Transform parent, string name, int size, TextAnchor anchor, float left, float bottom, float right, float top)
        {
            var holder = new GameObject(name, typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            var text = holder.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = her_color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            stretch(text.rectTransform, left, bottom, right, top);
            return text;
        }

        // A soft dark outline so words read over a bright sky.
        static void add_shadow(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.75f);
            shadow.effectDistance = new Vector2(2, -2);
        }

        // Pins a rectangle's corners to shares of its parent, with no extra size.
        static void stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Unity's UI needs one EventSystem to pass taps and clicks on; this one reads the Input System.
        static void make_event_system(Transform parent)
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }
            var holder = new GameObject("EventSystem");
            holder.transform.SetParent(parent, false);
            holder.AddComponent<EventSystem>();
            holder.AddComponent<InputSystemUIInputModule>();
        }
    }
}
