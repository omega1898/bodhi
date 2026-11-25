// Purpose: the camera. It follows whoever you're playing, a little ahead of the way they face so
// you can see where you're going, and eases there instead of snapping (that easing is most of
// what makes a 2D game feel smooth). It never shows past the ends of the chapter. In a film scene
// the story holds it on a fixed shot (StageTracker.camera), and a storm can shake it.
using Bodhi.Rules;
using UnityEngine;

namespace Bodhi.Game
{
    public class CameraRig
    {
        public const float metres_tall = 12f;        // A whole chapter's height (600 layout units) fits.
        public const float look_ahead_metres = 1.6f; // How far ahead of the way you face it looks.
        public const float follow_seconds = 0.35f;   // About how long it takes to catch up.
        public const float shot_seconds = 0.8f;      // A film shot eases in more slowly.
        public const float shake_metres = 0.12f;

        readonly Camera camera;
        Vector3 velocity;                            // SmoothDamp keeps its speed here between frames.
        float zoom_velocity;
        float look_ahead;
        float shake_seconds_left;

        public Camera view => camera;

        // Makes the scene's camera: orthographic (no perspective), looking at the layout's plane.
        public CameraRig(Transform parent)
        {
            var holder = new GameObject("Camera");
            holder.transform.SetParent(parent, false);
            holder.tag = "MainCamera";
            camera = holder.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = metres_tall / 2;  // Half the height that fits on screen.
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            holder.transform.position = new Vector3(0, 0, -10);
            holder.AddComponent<AudioListener>();
        }

        // Shakes it for a while (thunder, a falling beam).
        public void shake(double seconds)
        {
            if (seconds > 0 && !double.IsInfinity(seconds))
            {
                shake_seconds_left = Mathf.Max(shake_seconds_left, (float)seconds);
            }
        }

        // Puts it straight on the player (a new chapter, a Continue).
        public void jump_to(LayoutSpace space, ChapterStage stage, double player_x, double player_feet_y)
        {
            velocity = Vector3.zero;
            zoom_velocity = 0;
            look_ahead = 0;
            camera.orthographicSize = metres_tall / 2;
            var aim = clamp(follow_point(space, player_x, player_feet_y), space, stage, camera.orthographicSize);
            camera.transform.position = new Vector3(aim.x, aim.y, -10);
        }

        // One frame: follow the player, or ease toward the story's shot.
        public void follow(float delta_seconds, LayoutSpace space, ChapterStage stage, StageTracker tracker, double player_x, double player_feet_y, int facing)
        {
            if (delta_seconds <= 0)
            {
                return;
            }
            Vector2 aim;
            float size = metres_tall / 2;
            float seconds = follow_seconds;
            if (tracker != null && tracker.camera == CameraChange.hold)
            {
                aim = new Vector2((float)space.to_world_x(tracker.camera_x), (float)space.to_world_y(tracker.camera_y));
                size = (float)(metres_tall / 2 / tracker.camera_zoom);
                seconds = shot_seconds;
            }
            else
            {
                look_ahead = Mathf.MoveTowards(look_ahead, facing * look_ahead_metres, 3f * delta_seconds);
                aim = follow_point(space, player_x, player_feet_y) + new Vector2(look_ahead, 0);
            }
            camera.orthographicSize = Mathf.SmoothDamp(camera.orthographicSize, size, ref zoom_velocity, seconds);
            aim = clamp(aim, space, stage, camera.orthographicSize);
            var now = camera.transform.position;
            var eased = Vector3.SmoothDamp(new Vector3(now.x, now.y, -10), new Vector3(aim.x, aim.y, -10), ref velocity, seconds);
            if (shake_seconds_left > 0)
            {
                shake_seconds_left -= delta_seconds;
                eased += (Vector3)(Random.insideUnitCircle * shake_metres);
            }
            camera.transform.position = eased;
        }

        // A little above their feet, so you see more of what's ahead than of the floor.
        static Vector2 follow_point(LayoutSpace space, double player_x, double player_feet_y)
        {
            return new Vector2((float)space.to_world_x(player_x), (float)space.to_world_y(player_feet_y) + 2.2f);
        }

        // Keeps the view inside the chapter, as long as the chapter is wider than the view.
        Vector2 clamp(Vector2 aim, LayoutSpace space, ChapterStage stage, float half_height)
        {
            float half_width = half_height * camera.aspect;
            float left = (float)space.to_world_x(0);
            float right = (float)space.to_world_x(stage.width);
            float top = (float)space.to_world_y(0);
            float bottom = (float)space.to_world_y(stage.height);
            float x = right - left > half_width * 2 ? Mathf.Clamp(aim.x, left + half_width, right - half_width) : (left + right) / 2;
            float y = top - bottom > half_height * 2 ? Mathf.Clamp(aim.y, bottom + half_height, top - half_height) : (top + bottom) / 2;
            return new Vector2(x, y);
        }
    }
}
