// Purpose: her, Bodhi or the visiting dog in the Unity scene. Where they are and how they move
// comes from CharacterMover (tested outside Unity, the browser game's movement exactly); this
// script only draws them there, with the hand-drawn animation frames: FlipbookChooser picks the
// drawing each frame and SpriteLibrary holds the pictures. If a set's pictures are missing it
// falls back to a grey box with a nose, so the game still plays.
using Bodhi.Rules;
using UnityEngine;

namespace Bodhi.Game
{
    public class CharacterBody : MonoBehaviour
    {
        LayoutSpace space;
        string drawing_set;              // "her", "bodhi", "bodhi_puppy" or "biscuit".
        FlipbookChooser chooser;
        SpriteRenderer drawing;          // The hand-drawn frame.
        SpriteRenderer body_drawing;     // The grey box, if the drawings are missing.
        SpriteRenderer nose_drawing;
        SpriteRenderer carried_drawing;
        double clock_seconds = 0;        // The animation clock.
        double idle_seconds = 0;         // How long they've stood still (an idle dog sits).
        double jump_scale = 1;           // A puppy jumps lower; his jump drawings start lower too.
        Vector2 carried_size;            // The carried thing's box, in metres.
        int facing_override = 0;     // A story turn ("she faces right"), until they next move.
        double pose_seconds_left = 0;

        // Where they are and how they move.
        public CharacterMover mover { get; private set; }

        // The pose they're holding ("sit", "kneel", "sleep"...), or "" for none.
        public string pose { get; private set; } = "";

        // True while hidden (she's in bed and the bed shows her asleep; the guest has gone home).
        public bool hidden { get; private set; }

        // How happy a dog is, 0 to 1: a happier dog wags faster.
        public double wag { get; set; } = 0.4;

        // Which way they face now: 1 right, -1 left.
        public int facing => facing_override != 0 ? facing_override : mover.motion.facing;

        // True while a pose keeps them still (sitting, kneeling, sleeping, leaning).
        public bool holding_still => pose_seconds_left > 0 && pose != "" && pose != "stand" && pose != "bark" && pose != "sniff" && pose != "jump";

        // Makes one of them, standing with feet at (center_x, feet_y) in the layout, drawn with
        // a set of drawings (SpriteLibrary.prepare must have loaded it, or a grey box shows).
        public static CharacterBody create(string name, Character who, string drawing_set, Transform parent, LayoutSpace space, double center_x, double feet_y, double size_scale, double jump_scale, Color color)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            var body = holder.AddComponent<CharacterBody>();
            body.space = space;
            body.mover = new CharacterMover(who, center_x, feet_y, size_scale, jump_scale);
            body.drawing_set = drawing_set;
            body.jump_scale = jump_scale;
            body.chooser = new FlipbookChooser(FrameSheets.set(drawing_set));
            var drawing_holder = new GameObject("drawing");
            drawing_holder.transform.SetParent(holder.transform, false);
            body.drawing = drawing_holder.AddComponent<SpriteRenderer>();
            body.drawing.sortingOrder = GreyBox.character_order;
            float width = (float)(body.mover.width / LayoutSpace.units_per_metre);
            float height = (float)(body.mover.height / LayoutSpace.units_per_metre);
            body.body_drawing = GreyBox.make_box(holder.transform, "body", new Vector2(-width / 2, 0), new Vector2(width, height), color, GreyBox.character_order);
            var nose_color = Color.Lerp(color, Color.black, 0.35f);
            float nose = Mathf.Min(width, height) * 0.35f;
            body.nose_drawing = GreyBox.make_box(holder.transform, "nose", new Vector2(width / 2, height * 0.6f), new Vector2(nose, nose), nose_color, GreyBox.character_order + 1);
            body.carried_size = new Vector2(nose * 1.2f, nose * 0.8f);
            body.carried_drawing = GreyBox.make_box(holder.transform, "carried", new Vector2(width / 2, height * 0.35f), body.carried_size, new Color(0.85f, 0.75f, 0.55f), GreyBox.character_order + 2);
            body.carried_drawing.enabled = false;
            body.place_drawing();
            return body;
        }

        // Moves them one frame (see CharacterMover.step). Walking off ends kneeling, sitting or
        // sleeping, as in the browser game; any other held pose (leaning in) keeps them still.
        public void move(int direction, bool jump_held, bool in_water, double speed_scale, double delta_seconds, ChapterStage stage, ChapterRun run)
        {
            if (direction != 0 && (pose == "kneel" || pose == "sit" || pose == "sleep"))
            {
                end_pose();
            }
            if (holding_still)
            {
                direction = 0;
            }
            if (direction != 0)
            {
                facing_override = 0;
            }
            mover.step(direction, jump_held, in_water, speed_scale, delta_seconds, stage, run);
            bool moving = System.Math.Abs(mover.motion.velocity_x) > FlipbookChooser.moving_speed;
            idle_seconds = moving ? 0 : idle_seconds + delta_seconds;
            pose_seconds_left = System.Math.Max(0, pose_seconds_left - delta_seconds);
            if (pose_seconds_left <= 0)
            {
                pose = "";
            }
        }

        // Strikes a pose for a while. Seconds that aren't a real number above zero hold it until they move.
        public void strike_pose(string new_pose, double seconds)
        {
            pose = new_pose ?? "";
            pose_seconds_left = seconds > 0 && !double.IsNaN(seconds) ? seconds : double.MaxValue;
        }

        // Lets go of any pose.
        public void end_pose()
        {
            pose = "";
            pose_seconds_left = 0;
        }

        // Turns them to face left (-1) or right (1) until they next move.
        public void face(int direction)
        {
            facing_override = direction < 0 ? -1 : direction > 0 ? 1 : 0;
        }

        // Shows or hides them.
        public void hide(bool now_hidden)
        {
            hidden = now_hidden;
        }

        // Shows something in his mouth (a glove, a collar), or nothing for "".
        public void carry(string item)
        {
            carrying = !string.IsNullOrEmpty(item);
        }

        bool carrying = false;

        // Puts them somewhere at once, standing still.
        public void place(double center_x, double feet_y)
        {
            mover.place(center_x, feet_y);
            place_drawing();
        }

        // Draws them where the mover says, after everything has moved this frame. The clock runs
        // on Unity's frame time, so a character the story isn't moving (him, in the hug) still
        // breathes.
        void LateUpdate()
        {
            clock_seconds += Mathf.Min(Time.deltaTime, 0.1f);
            place_drawing();
        }

        // Moves the drawing to their feet, picks the drawing for what they're doing, and turns it
        // the way it faces. Without drawings, the grey box squats for a pose instead.
        void place_drawing()
        {
            transform.localPosition = new Vector3((float)space.to_world_x(mover.center_x), (float)space.to_world_y(mover.feet_y), 0);
            bool drawn = SpriteLibrary.has(drawing_set);
            int drawn_facing = facing;
            float size = (float)mover.size_scale;
            float hold_x = (float)(mover.width / 2), hold_y = (float)(mover.height * 0.35);
            if (drawn)
            {
                var input = new FlipbookInput
                {
                    center_x = mover.center_x,
                    velocity_x = mover.motion.velocity_x,
                    velocity_y = mover.motion.velocity_y,
                    on_ground = mover.on_ground,
                    facing = facing,
                    pose = FlipbookChooser.pose_of(mover.who == Character.dog, pose, mover.on_ground, mover.motion.velocity_x, idle_seconds),
                    wag = wag,
                    size_scale = size,
                    jump_scale = jump_scale,
                    time_seconds = clock_seconds,
                };
                var choice = chooser.choose(input);
                drawing.sprite = SpriteLibrary.sprite(drawing_set, choice.animation, choice.frame);
                drawn_facing = choice.facing;
                var shown = FrameSheets.set(drawing_set).animation(choice.animation).frames[choice.frame];
                hold_x = (float)shown.hold_x;
                hold_y = (float)-shown.hold_y;  // The drawing's y runs down; Unity's runs up.
                transform.localScale = new Vector3(drawn_facing * size, size, 1);  // A negative x mirrors it.
            }
            else
            {
                float squat = pose == "sleep" ? 0.45f : pose == "sit" || pose == "kneel" ? 0.7f : pose == "lean" ? 0.9f : 1f;
                transform.localScale = new Vector3(facing, squat, 1);
            }
            drawing.enabled = drawn && !hidden;
            body_drawing.enabled = !drawn && !hidden;
            nose_drawing.enabled = !drawn && !hidden;
            // The carried thing's middle on the hold point (its box is placed by its corner).
            float metres = (float)(1 / LayoutSpace.units_per_metre);
            carried_drawing.transform.localPosition = new Vector3(hold_x * metres - carried_size.x / 2, hold_y * metres - carried_size.y / 2, 0);
            carried_drawing.enabled = carrying && !hidden;
        }
    }
}
