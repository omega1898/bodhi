// Purpose: the 3D scene behind the title screen: a starry night with a green aurora over snowy
// mountains, where Em and Bodhi build a snowman and play in the snow. This file puts the scene
// together (sky, land, snow, lights, camera, the snowman and the two of them) and moves it on
// each frame. What happens when, the choreography, comes from SnowmanPlay (plain C#, tested
// outside Unity); the opening's fades and camera move come from OpeningTitle.
using Bodhi.Rules;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bodhi.Game
{
    public class TitleWorld
    {
        // Where the camera sits and looks once the opening has settled (metres, scene space).
        // A long lens from well back, as films do: the mountains and aurora loom large behind the
        // two of them instead of shrinking away.
        static readonly Vector3 camera_home = new Vector3(0.4f, 1.6f, -13f);
        static readonly Vector3 look_home = new Vector3(0f, 2.85f, 4f);
        static readonly Vector3 look_at_sky = new Vector3(0f, 9.5f, 4f);   // Where the opening starts.
        const float field_of_view = 28f;

        // The moon, behind the camera and to the left: it lights the snowy faces of the mountains
        // and the two of them, and throws their shadows long across the snow, away from us.
        static readonly Vector3 toward_moon = new Vector3(-0.55f, 0.42f, -0.72f);
        static readonly Vector3 lantern_spot = new Vector3(-1.5f, 0f, -1.1f);

        readonly GameObject root;
        readonly Camera camera;
        readonly NightSky sky;
        readonly SnowLand land;
        readonly SnowFall snow;
        readonly Volume post;
        readonly Light lantern_light;
        readonly Snowman snowman;
        readonly SnowmanPlay play = new SnowmanPlay();
        readonly HumanFigure em;
        readonly DogFigure bodhi;
        Camera game_camera;           // The chapters' camera, switched off while this one shows.
        double last_seconds = -1;
        float paw_puff_clock;

        // The camera this scene draws with.
        public Camera view => camera;

        // Em's and Bodhi's roots (for the snapshot tool's close-ups).
        public Transform em_body => em.body;
        public Transform bodhi_body => bodhi.body;

        // Builds the whole scene, hidden. `game_camera` is the chapters' camera, which is turned
        // off while the title shows so the phone doesn't draw the scene twice.
        public TitleWorld(Transform parent, Camera game_camera)
        {
            this.game_camera = game_camera;
            root = new GameObject("Title world");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(0, -500, 0);  // Well away from the chapters' 2D layout.

            camera = WorldLook.make_camera("Title camera", root.transform, field_of_view);
            post = WorldLook.make_post_processing("Title look", root.transform, 0.95f, 0.85f);
            sky = new NightSky(root.transform, toward_moon);
            land = new SnowLand(root.transform);
            snow = new SnowFall(root.transform, new Vector3(0, 0, -2), 30f);

            WorldLook.far_light("Moonlight", root.transform, new Color(0.7f, 0.82f, 1f), 0.62f, sky.moonlight_direction, true);
            WorldLook.far_light("Aurora light", root.transform, new Color(0.45f, 0.95f, 0.7f), 0.07f, new Vector3(-0.1f, -0.75f, -0.65f), false);
            lantern_light = WorldLook.lamp("Lantern light", root.transform, lantern_spot + Vector3.up * 0.5f, PaletteColours.warm_amber, 1.5f, 5f);
            WorldLook.lamp("Cabin window light", root.transform, land.cabin_window + new Vector3(0, 0, -1.2f), PaletteColours.warm_amber, 3f, 9f);
            Lantern.build(root.transform, lantern_spot);

            snowman = new Snowman(root.transform);
            em = HumanFigure.build_em(root.transform);
            bodhi = DogFigure.build_bodhi(root.transform);
            root.SetActive(false);
        }

        // Shows or hides the scene. While it shows, its sky, fog and light from the sky are the
        // scene's settings, and the chapters' camera is off.
        public void show(bool visible)
        {
            root.SetActive(visible);
            if (game_camera != null)
            {
                game_camera.enabled = !visible;
            }
            if (visible)
            {
                WorldLook.check_pipeline();
                sky.use();
                WorldLook.set_sky_light(new Color(0.08f, 0.13f, 0.25f), new Color(0.09f, 0.17f, 0.22f), new Color(0.18f, 0.23f, 0.33f));
                WorldLook.set_fog(new Color(0.1f, 0.16f, 0.26f), 0.0011f);
                last_seconds = -1;
            }
            else
            {
                WorldLook.clear_fog();
            }
        }

        // Moves the scene on to `seconds` since the opening began. `opening` gives the fades and
        // the camera's tilt from the sky down to the snowfield.
        public void update(double seconds, OpeningFrame opening)
        {
            float delta = last_seconds < 0 ? 0 : (float)(seconds - last_seconds);
            last_seconds = seconds;
            sky.set_fade((float)opening.sky_opacity);
            post.weight = 1f;

            // The camera: tilted up at the aurora at first, easing down onto the two of them, then
            // a slow sway so the settled screen is never quite still.
            float tilt = (float)opening.camera_tilt;
            float sway = Mathf.Sin((float)seconds * 0.11f) * 0.45f;
            var look = Vector3.Lerp(look_at_sky, look_home, tilt) + new Vector3(sway * 0.4f, 0, 0);
            var position = camera_home + new Vector3(sway, Mathf.Sin((float)seconds * 0.07f) * 0.06f, 0);
            camera.transform.localPosition = position;
            camera.transform.localRotation = Quaternion.LookRotation(look - position);

            // The lantern flickers very slightly, like a flame behind glass.
            float flicker = Mathf.PerlinNoise((float)seconds * 3.1f, 0.3f) * 0.35f + Mathf.PerlinNoise((float)seconds * 9.7f, 4.1f) * 0.12f;
            lantern_light.intensity = 1.35f + flicker * 0.6f * (float)opening.scene_opacity;

            var moment = play.frame_at(seconds);
            em.look_yaw = look_angle(em.body, moment.em.look, bodhi.body.localPosition + Vector3.up * 0.5f, 1.5f);
            bodhi.look_yaw = look_angle(bodhi.body, moment.bodhi.look, em.body.localPosition + Vector3.up * 1.2f, 0.9f);
            em.update(moment.em, delta, seconds);
            bodhi.update(moment.bodhi, delta, seconds);
            snowman.show(moment, em, bodhi);
            foreach (var puff in moment.puffs)
            {
                snow.puff(root.transform.TransformPoint(new Vector3((float)puff.x, (float)puff.y, (float)puff.z)), puff.amount);
            }
            kick_up_snow(delta);
        }

        // How far a character should turn their head, in degrees from the way they face, to look
        // at what the choreography says: their partner, the snowman, or ahead (no turn).
        static float look_angle(Transform who, LookAt look, Vector3 partner, float snowman_height)
        {
            Vector3 target;
            switch (look)
            {
                case LookAt.partner: target = partner; break;
                case LookAt.snowman: target = new Vector3(0, snowman_height, 0); break;
                default: return 0;
            }
            var toward = target - who.localPosition;
            float bearing = Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(who.localEulerAngles.y, bearing);
        }

        // Little puffs of snow thrown up behind Bodhi's paws while he gallops.
        void kick_up_snow(float delta)
        {
            paw_puff_clock -= delta;
            if (bodhi.speed > 3.4f && paw_puff_clock <= 0)
            {
                paw_puff_clock = 0.16f;
                var behind = bodhi.body.localPosition - bodhi.body.forward * 0.3f;
                snow.puff(root.transform.TransformPoint(new Vector3(behind.x, 0.04f, behind.z)), 3);
            }
        }

        // For still pictures made outside play (the snapshot tool): falling snow only moves by
        // itself while the game plays, so here it's run on by hand.
        public void settle_snow(float seconds)
        {
            snow.run_for(seconds);
        }
    }
}
