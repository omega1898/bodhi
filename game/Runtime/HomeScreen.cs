// Purpose: the home screen, between and after chapters: Em and Bodhi at home, their bond's level
// with a bar toward the next one, the streak, and today's three small tasks with a button each.
// Fetch opens a timing game: the ball arcs from her to him and a tap while it's in his reach is
// a catch. Drawn with Unity's UI on its own canvas over everything else. Every choice is made by
// tested Logic code (GameSession saves each task, FetchGame times the ball, BondFeedback words
// it all); this file only draws the answer and passes taps on.
using System;
using Bodhi.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace Bodhi.Game
{
    public class HomeScreen
    {
        // Drawn for a 1280 by 720 screen; the scaler fits any other.
        const float screen_width = 1280, screen_height = 720;
        const float floor_height = 180;                 // The floor's top edge, from the bottom.
        const float her_height_on_screen = 300;         // How tall Em stands.
        const float her_x = 170, dog_x = 450;           // Where each stands, centre of the feet.
        const float ball_size = 26, ball_arc_height = 230;
        const float frames_per_second = 8;              // How fast the drawings move on.
        const float pose_seconds = 1.6f;                // How long a task's pose lasts.
        const float message_seconds = 4;                // How long a line stays before it fades.

        static readonly Color room_color = new Color(0.33f, 0.27f, 0.25f);
        static readonly Color window_color = new Color(0.55f, 0.66f, 0.74f);
        static readonly Color floor_color = new Color(0.42f, 0.31f, 0.22f);
        static readonly Color panel_color = new Color(0.08f, 0.07f, 0.09f, 0.9f);
        static readonly Color bar_back_color = new Color(1, 1, 1, 0.15f);
        static readonly Color bar_color = new Color(0.94f, 0.5f, 0.52f);
        static readonly Color words_color = new Color(0.98f, 0.95f, 0.88f);
        static readonly Color ball_color = new Color(0.85f, 0.25f, 0.2f);
        static readonly Color ball_catchable_color = new Color(1f, 0.85f, 0.3f);

        readonly GameSession session;
        readonly string dog_name;
        readonly Font font;
        readonly GameObject screen, fetch_panel;
        readonly Image her_drawing, dog_drawing, ball, progress_fill;
        readonly Text level_text, next_text, tricks_text, streak_text, heading_text, message_text, fetch_text, leave_label, throw_label;
        readonly Text[] task_texts = new Text[HomeDay.tasks_per_day];
        readonly Button[] task_buttons = new Button[HomeDay.tasks_per_day];
        readonly Text[] task_button_labels = new Text[HomeDay.tasks_per_day];

        readonly FetchGame fetch = new FetchGame();
        Action on_leave;
        string dog_set = "bodhi";
        float dog_size = 1;
        float pixels_per_sprite_unit = 0;  // Worked out from Em's standing drawing.
        float seconds_shown, message_left, pose_left;
        string her_pose = "idle", dog_pose = "idle";

        // True while home is on screen; the director pauses the chapter's input meanwhile.
        public bool showing => screen.activeSelf;

        // Builds the screen, hidden, on its own canvas above the HUD (so the touch buttons don't show through).
        public HomeScreen(Transform parent, GameSession session, string dog_name)
        {
            this.session = session;
            this.dog_name = dog_name;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var holder = new GameObject("Home", typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            var canvas_part = holder.AddComponent<Canvas>();
            canvas_part.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas_part.sortingOrder = 110;  // Above the HUD (100) and the title (90).
            var scaler = holder.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(screen_width, screen_height);
            scaler.matchWidthOrHeight = 0.5f;
            holder.AddComponent<GraphicRaycaster>();

            screen = make_image(holder.transform, "home", room_color, 0, 0, 1, 1).gameObject;
            make_image(screen.transform, "window", window_color, 0.06f, 0.45f, 0.3f, 0.7f).raycastTarget = false;
            make_image(screen.transform, "floor", floor_color, 0, 0, 1, floor_height / screen_height).raycastTarget = false;
            her_drawing = make_image(screen.transform, "Em", Color.white, 0, 0, 0, 0);
            dog_drawing = make_image(screen.transform, "Bodhi", Color.white, 0, 0, 0, 0);
            her_drawing.raycastTarget = false;
            dog_drawing.raycastTarget = false;

            level_text = make_text(screen.transform, "level", 40, TextAnchor.MiddleLeft, 0.04f, 0.88f, 0.6f, 0.98f);
            make_image(screen.transform, "bar", bar_back_color, 0.04f, 0.83f, 0.4f, 0.86f).raycastTarget = false;
            progress_fill = make_image(screen.transform, "bar filled", bar_color, 0.04f, 0.83f, 0.4f, 0.86f);
            progress_fill.raycastTarget = false;
            progress_fill.sprite = TitleScreens.round_sprite();
            progress_fill.type = Image.Type.Filled;
            progress_fill.fillMethod = Image.FillMethod.Horizontal;
            next_text = make_text(screen.transform, "next level", 20, TextAnchor.MiddleLeft, 0.42f, 0.81f, 0.7f, 0.88f);
            tricks_text = make_text(screen.transform, "tricks", 20, TextAnchor.MiddleLeft, 0.04f, 0.75f, 0.6f, 0.81f);
            streak_text = make_text(screen.transform, "streak", 24, TextAnchor.UpperRight, 0.6f, 0.88f, 0.97f, 0.97f);

            heading_text = make_text(screen.transform, "today", 28, TextAnchor.MiddleLeft, 0.52f, 0.7f, 0.97f, 0.78f);
            for (int row = 0; row < HomeDay.tasks_per_day; row++)
            {
                int this_row = row;  // A copy for the button's action, so each button keeps its own row.
                float row_y = 0.62f - row * 0.13f;
                task_texts[row] = make_text(screen.transform, "task " + row, 22, TextAnchor.MiddleLeft, 0.52f, row_y - 0.05f, 0.8f, row_y + 0.05f);
                task_buttons[row] = make_button(screen.transform, "", 0.89f, row_y, 190, 56, () => press_task(this_row), out task_button_labels[row]);
            }
            make_button(screen.transform, "", 0.89f, 0.07f, 220, 56, () => on_leave?.Invoke(), out leave_label);

            fetch_panel = make_image(screen.transform, "fetch", panel_color, 0.52f, 0.14f, 0.98f, 0.8f).gameObject;
            make_text(fetch_panel.transform, "how", 22, TextAnchor.MiddleCenter, 0.05f, 0.78f, 0.95f, 0.95f).text = "Tap Catch! when the ball turns gold.";
            fetch_text = make_text(fetch_panel.transform, "record", 20, TextAnchor.MiddleCenter, 0.05f, 0.64f, 0.95f, 0.76f);
            make_button(fetch_panel.transform, "", 0.5f, 0.42f, 320, 96, press_throw_or_catch, out throw_label);
            make_button(fetch_panel.transform, "Done", 0.5f, 0.14f, 200, 56, close_fetch, out _);

            ball = make_image(screen.transform, "ball", ball_color, 0, 0, 0, 0);
            ball.sprite = TitleScreens.round_sprite();
            ball.raycastTarget = false;
            ball.rectTransform.sizeDelta = new Vector2(ball_size, ball_size);
            message_text = make_text(screen.transform, "message", 24, TextAnchor.MiddleLeft, 0.03f, 0.02f, 0.75f, 0.23f);
            hide();
        }

        // Shows home: Bodhi drawn from `dog_set` at `dog_size` (a puppy early in the year), and a
        // button labelled `leave_text` that calls `leave` (back to the title, or on to the next chapter).
        public void show(string dog_set, double dog_size, string leave_text, Action leave)
        {
            this.dog_set = dog_set ?? "bodhi";
            this.dog_size = (float)dog_size;
            on_leave = leave;
            leave_label.text = leave_text;
            seconds_shown = 0;
            message_left = 0;
            pose_left = 0;
            fetch_panel.SetActive(false);
            screen.SetActive(true);
            refresh();
        }

        // Hides home.
        public void hide()
        {
            screen.SetActive(false);
            fetch_panel.SetActive(false);
        }

        // Redraws everything from the year: after a task, a catch, or a new day starting.
        public void refresh()
        {
            var journey = session.campaign.journey;
            var home = session.campaign.home;
            level_text.text = journey.level_name;
            progress_fill.fillAmount = (float)journey.progress_to_next_level;
            next_text.text = BondFeedback.next_level_text(journey);
            tricks_text.text = BondFeedback.tricks_text(journey);
            streak_text.text = BondFeedback.streak_text(home);
            heading_text.text = BondFeedback.today_heading(home, dog_name);
            for (int row = 0; row < HomeDay.tasks_per_day; row++)
            {
                bool has_task = row < home.tasks_today.Count;
                task_texts[row].gameObject.SetActive(has_task);
                task_buttons[row].gameObject.SetActive(has_task);
                if (!has_task)
                {
                    continue;
                }
                var task = home.tasks_today[row];
                task_texts[row].text = BondFeedback.task_label(home, task);
                task_button_labels[row].text = BondFeedback.task_button(home, task);
                task_buttons[row].interactable = !home.is_done(task) || task == HomeTask.fetch_five_in_a_row;
            }
            fetch_text.text = BondFeedback.fetch_text(home);
            throw_label.text = fetch.ball_in_air ? "Catch!" : "Throw";
        }

        // One frame: the drawings move, the ball flies, a line fades.
        public void update(float delta_seconds)
        {
            if (!showing)
            {
                return;
            }
            seconds_shown += delta_seconds;
            pose_left = Mathf.Max(0, pose_left - delta_seconds);
            if (pose_left <= 0)
            {
                her_pose = "idle";
                dog_pose = "idle";
            }
            message_left = Mathf.Max(0, message_left - delta_seconds);
            message_text.color = new Color(words_color.r, words_color.g, words_color.b, Mathf.Clamp01(message_left));
            if (fetch_panel.activeSelf)
            {
                var landed = fetch.pass_time(delta_seconds);
                if (landed != FetchResult.nothing)
                {
                    finish_throw(landed);
                }
            }
            draw("her", her_pose, her_drawing, her_x, 1, 1);
            draw(dog_set, dog_pose, dog_drawing, dog_x, -1, dog_size);  // Facing her.
            draw_ball();
        }

        // A task's button: fetch opens the game; anything else is done at once and saved.
        void press_task(int row)
        {
            var home = session.campaign.home;
            if (row >= home.tasks_today.Count)
            {
                return;
            }
            var task = home.tasks_today[row];
            if (task == HomeTask.fetch_five_in_a_row)
            {
                fetch_panel.SetActive(true);
                refresh();
                return;
            }
            var result = session.do_home_task(task);
            if (result.counted)
            {
                strike_poses(task);
                say(BondFeedback.heart_pop(result.bond), string.Join("\n", BondFeedback.lines_for_home(result, home, dog_name)));
            }
            refresh();
        }

        // The big fetch button: she throws, or (with the ball in the air) he jumps for it.
        void press_throw_or_catch()
        {
            if (!fetch.ball_in_air)
            {
                fetch.throw_ball(session.campaign.home.fetch_catches_in_a_row);
                message_left = 0;
                refresh();
                return;
            }
            finish_throw(fetch.tap());
        }

        // A throw ended: a catch is counted and saved; a miss starts the run again.
        void finish_throw(FetchResult result)
        {
            if (result == FetchResult.caught)
            {
                var caught = session.fetch_caught();
                set_poses("idle", "jump");
                var lines = BondFeedback.lines_for_home(caught, session.campaign.home, dog_name);
                lines.Insert(0, BondFeedback.fetch_line(result));
                say(BondFeedback.heart_pop(caught.bond), string.Join("\n", lines));
            }
            else if (result == FetchResult.missed)
            {
                session.fetch_missed();
                set_poses("idle", "sit");
                say("", BondFeedback.fetch_line(result));
            }
            refresh();
        }

        // Leaves the fetch game; the record stays.
        void close_fetch()
        {
            fetch.put_away();
            session.fetch_missed();
            fetch_panel.SetActive(false);
            refresh();
        }

        // A short pose for each of them while a task is done, so it looks like something happened.
        void strike_poses(HomeTask task)
        {
            switch (task)
            {
                case HomeTask.cuddle: set_poses("kneel", "lean"); break;
                case HomeTask.walk_round_the_block: set_poses("walk", "trot"); break;
                case HomeTask.find_hidden_ball: set_poses("idle", "sniff"); break;
                case HomeTask.dinner: set_poses("kneel", "sniff"); break;
                default: set_poses("kneel", "sit"); break;  // Brushing, drying, tricks.
            }
        }

        // Puts both into a pose for a moment.
        void set_poses(string her_animation, string dog_animation)
        {
            her_pose = her_animation;
            dog_pose = dog_animation;
            pose_left = pose_seconds;
        }

        // Shows the heart pop and lines on the floor, fading after a few seconds.
        void say(string heart_pop, string lines)
        {
            string text = heart_pop;
            if (!string.IsNullOrEmpty(lines))
            {
                text = string.IsNullOrEmpty(text) ? lines : text + "\n" + lines;
            }
            message_text.text = text ?? "";
            message_left = string.IsNullOrEmpty(text) ? 0 : message_seconds;
        }

        // Draws one of them standing on the floor at `x`, looping the animation's drawings.
        void draw(string set_name, string animation, Image drawing, float x, int facing, float size)
        {
            var frames = FrameSheets.set(set_name)?.animation(animation);
            int count = frames != null ? frames.count : 0;
            int frame = count > 0 ? (int)(seconds_shown * frames_per_second) % count : 0;
            var sprite = SpriteLibrary.sprite(set_name, animation, frame);
            if (sprite == null)  // Unity objects are checked with == null, never ??.
            {
                sprite = SpriteLibrary.sprite(set_name, "idle", 0);
            }
            drawing.enabled = sprite != null;
            if (sprite == null)
            {
                return;  // Pictures missing: nothing to draw, and nothing breaks.
            }
            if (pixels_per_sprite_unit <= 0)
            {
                var standing = SpriteLibrary.sprite("her", "idle", 0);
                float her_height = standing != null ? standing.bounds.size.y : sprite.bounds.size.y;
                pixels_per_sprite_unit = her_height_on_screen / Mathf.Max(0.01f, her_height);
            }
            drawing.sprite = sprite;
            var rect = drawing.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);  // The feet.
            rect.sizeDelta = (Vector2)sprite.bounds.size * (pixels_per_sprite_unit * size);
            rect.anchoredPosition = new Vector2(x, floor_height);
            rect.localScale = new Vector3(facing, 1, 1);
        }

        // The ball on its arc from her hand to his mouth, gold while it can be caught.
        void draw_ball()
        {
            ball.enabled = fetch_panel.activeSelf && fetch.ball_in_air;
            if (!ball.enabled)
            {
                return;
            }
            float start_y = floor_height + her_height_on_screen * 0.6f;
            float end_y = floor_height + her_height_on_screen * 0.3f * dog_size;
            float across = (float)fetch.across;
            float x = her_x + (dog_x - her_x) * across;
            float y = start_y + (end_y - start_y) * across + ball_arc_height * (float)fetch.height;
            ball.rectTransform.anchorMin = ball.rectTransform.anchorMax = Vector2.zero;
            ball.rectTransform.anchoredPosition = new Vector2(x, y);
            ball.color = fetch.in_catch_window ? ball_catchable_color : ball_color;
        }

        // A tappable button with words on it, centred on a share of its parent, `width` by `height`.
        Button make_button(Transform parent, string label, float x, float y, float width, float height, Action on_click, out Text words)
        {
            var image = make_image(parent, "button " + label, new Color(1, 1, 1, 0.16f), x, y, x, y);
            image.rectTransform.sizeDelta = new Vector2(width, height);
            var button = image.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => on_click());
            words = make_text(image.transform, "label", 26, TextAnchor.MiddleCenter, 0, 0, 1, 1);
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

        // Words in the built-in font, stretched like make_image.
        Text make_text(Transform parent, string name, int size, TextAnchor anchor, float left, float bottom, float right, float top)
        {
            var holder = new GameObject(name, typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            var text = holder.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
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
