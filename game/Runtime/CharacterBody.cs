// Purpose: her, Bodhi or the visiting dog in the Unity scene. Where they are and how they move
// comes from CharacterMover (tested outside Unity, the browser game's movement exactly); this
// script only draws them there, as grey boxes for now, with a nose to show which way they face
// and a squat for sitting, kneeling or sleeping. A human artist's rig replaces the boxes later.
using Bodhi.Rules;
using UnityEngine;

namespace Bodhi.Game
{
    public class CharacterBody : MonoBehaviour
    {
        LayoutSpace space;
        SpriteRenderer body_drawing;
        SpriteRenderer nose_drawing;
        SpriteRenderer carried_drawing;
        int facing_override = 0;     // A story turn ("she faces right"), until they next move.
        double pose_seconds_left = 0;

        // Where they are and how they move.
        public CharacterMover mover { get; private set; }

        // The pose they're holding ("sit", "kneel", "sleep"...), or "" for none.
        public string pose { get; private set; } = "";

        // True while hidden (she's in bed and the bed shows her asleep; the guest has gone home).
        public bool hidden { get; private set; }

        // Which way they face now: 1 right, -1 left.
        public int facing => facing_override != 0 ? facing_override : mover.motion.facing;

        // True while a pose keeps them still (sitting, kneeling, sleeping, leaning).
        public bool holding_still => pose_seconds_left > 0 && pose != "" && pose != "stand" && pose != "bark" && pose != "sniff" && pose != "jump";

        // Makes one of them, standing with feet at (center_x, feet_y) in the layout.
        public static CharacterBody create(string name, Character who, Transform parent, LayoutSpace space, double center_x, double feet_y, double size_scale, double jump_scale, Color color)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            var body = holder.AddComponent<CharacterBody>();
            body.space = space;
            body.mover = new CharacterMover(who, center_x, feet_y, size_scale, jump_scale);
            float width = (float)(body.mover.width / LayoutSpace.units_per_metre);
            float height = (float)(body.mover.height / LayoutSpace.units_per_metre);
            body.body_drawing = GreyBox.make_box(holder.transform, "body", new Vector2(-width / 2, 0), new Vector2(width, height), color, GreyBox.character_order);
            var nose_color = Color.Lerp(color, Color.black, 0.35f);
            float nose = Mathf.Min(width, height) * 0.35f;
            body.nose_drawing = GreyBox.make_box(holder.transform, "nose", new Vector2(width / 2, height * 0.6f), new Vector2(nose, nose), nose_color, GreyBox.character_order + 1);
            body.carried_drawing = GreyBox.make_box(holder.transform, "carried", new Vector2(width / 2, height * 0.35f), new Vector2(nose * 1.2f, nose * 0.8f), new Color(0.85f, 0.75f, 0.55f), GreyBox.character_order + 2);
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
            carried_drawing.enabled = !string.IsNullOrEmpty(item);
        }

        // Puts them somewhere at once, standing still.
        public void place(double center_x, double feet_y)
        {
            mover.place(center_x, feet_y);
            place_drawing();
        }

        // Draws them where the mover says, after everything has moved this frame.
        void LateUpdate()
        {
            place_drawing();
        }

        // Moves the drawing to their feet, turns it the way they face, and squats it for a pose.
        void place_drawing()
        {
            transform.localPosition = new Vector3((float)space.to_world_x(mover.center_x), (float)space.to_world_y(mover.feet_y), 0);
            float squat = pose == "sleep" ? 0.45f : pose == "sit" || pose == "kneel" ? 0.7f : pose == "lean" ? 0.9f : 1f;
            transform.localScale = new Vector3(facing, squat, 1);  // A negative x mirrors it.
            body_drawing.enabled = !hidden;
            nose_drawing.enabled = !hidden;
            if (hidden)
            {
                carried_drawing.enabled = false;
            }
        }
    }
}
