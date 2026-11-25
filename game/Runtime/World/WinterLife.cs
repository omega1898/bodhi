// Purpose: the small living details of a winter night, which make the scene feel real rather
// than a still model: footprints and paw prints pressed into the snow wherever Em and Bodhi walk
// (fading slowly), their breath misting in the cold air, smoke curling from the cabin's chimney,
// warm motes drifting up from the lantern, and now and then a shooting star. Each is one
// particle system (Unity's built-in way to draw many small things) that is reused, never one
// object made per footprint or breath, which would stutter on a phone.
using UnityEngine;

namespace Bodhi.Game
{
    public class WinterLife
    {
        static readonly Color print_colour = new Color(0.45f, 0.55f, 0.72f, 0.55f);  // Shadowed snow, blue.

        readonly Transform scene;
        readonly ParticleSystem prints, breath, smoke, motes, shooting_stars;
        float breath_clock_em = 1.2f, breath_clock_bodhi = 0.4f, star_clock = 6f;
        readonly System.Random random = new System.Random(24);

        // Where each foot last left a print, so a new print is made only after it has moved on.
        readonly Vector3[] last_prints = new Vector3[6];

        // Builds every effect under `scene`: smoke rising from `chimney`, motes round `lantern`.
        public WinterLife(Transform scene, Vector3 chimney, Vector3 lantern)
        {
            this.scene = scene;
            var soft = WorldLook.particles(false);
            var glowing = WorldLook.particles(true);

            // Footprints: flat soft ovals lying on the snow, slowly fading over half a minute.
            prints = make("footprints", soft, 260);
            var print_main = prints.main;
            print_main.startLifetime = 32f;
            print_main.startSpeed = 0f;
            print_main.startSize3D = true;
            print_main.startRotation3D = false;
            var print_fade = prints.colorOverLifetime;
            print_fade.enabled = true;
            print_fade.color = fade_out(print_colour, 0.7f);
            prints.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            prints.GetComponent<ParticleSystemRenderer>().sortingFudge = 10;  // Drawn before the snow puffs.

            // Breath: a soft white cloud that swells, drifts up and forward, and thins away.
            breath = make("breath", soft, 40);
            var breath_main = breath.main;
            breath_main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
            breath_main.startSpeed = 0f;
            breath_main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
            breath_main.gravityModifier = -0.02f;
            var breath_grow = breath.sizeOverLifetime;
            breath_grow.enabled = true;
            breath_grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 4.5f));
            var breath_fade = breath.colorOverLifetime;
            breath_fade.enabled = true;
            breath_fade.color = fade_out(new Color(0.9f, 0.94f, 1f, 0.32f), 0.15f);

            // Chimney smoke: slow grey-blue puffs rising, swelling and leaning with the breeze.
            smoke = make("chimney smoke", soft, 60);
            smoke.transform.localPosition = chimney;
            var smoke_main = smoke.main;
            smoke_main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
            smoke_main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            smoke_main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            var smoke_shape = smoke.shape;
            smoke_shape.enabled = true;
            smoke_shape.shapeType = ParticleSystemShapeType.Cone;
            smoke_shape.angle = 8f;
            smoke_shape.radius = 0.15f;
            smoke_shape.rotation = new Vector3(-90, 0, 0);  // Pointing up.
            var smoke_rate = smoke.emission;
            smoke_rate.rateOverTime = 4f;
            var smoke_drift = smoke.velocityOverLifetime;
            smoke_drift.enabled = true;
            smoke_drift.space = ParticleSystemSimulationSpace.World;
            smoke_drift.x = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            smoke_drift.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            smoke_drift.z = new ParticleSystem.MinMaxCurve(0f, 0.1f);
            var smoke_grow = smoke.sizeOverLifetime;
            smoke_grow.enabled = true;
            smoke_grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 4f));
            var smoke_fade = smoke.colorOverLifetime;
            smoke_fade.enabled = true;
            smoke_fade.color = fade_out(new Color(0.55f, 0.62f, 0.75f, 0.35f), 0.1f);
            smoke.Play();

            // Lantern motes: a few warm specks drifting lazily up out of the lantern's glow.
            motes = make("lantern motes", glowing, 30);
            motes.transform.localPosition = lantern + Vector3.up * 0.3f;
            var motes_main = motes.main;
            motes_main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            motes_main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            motes_main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.03f);
            motes_main.startColor = new Color(1f, 0.75f, 0.4f, 0.9f);
            motes_main.gravityModifier = -0.01f;
            var motes_shape = motes.shape;
            motes_shape.enabled = true;
            motes_shape.shapeType = ParticleSystemShapeType.Sphere;
            motes_shape.radius = 0.25f;
            var motes_rate = motes.emission;
            motes_rate.rateOverTime = 5f;
            var motes_wander = motes.noise;
            motes_wander.enabled = true;
            motes_wander.strength = 0.15f;
            motes_wander.frequency = 0.6f;
            var motes_fade = motes.colorOverLifetime;
            motes_fade.enabled = true;
            motes_fade.color = fade_out(new Color(1f, 0.78f, 0.45f, 0.9f), 0.2f);
            motes.Play();

            // Shooting stars: a single bright streak across the far sky every so often.
            shooting_stars = make("shooting stars", glowing, 4);
            var star_main = shooting_stars.main;
            star_main.startLifetime = 0.7f;
            star_main.startSize = 3f;
            star_main.startColor = new Color(0.85f, 0.92f, 1f, 1f);
            var star_fade = shooting_stars.colorOverLifetime;
            star_fade.enabled = true;
            star_fade.color = fade_out(Color.white, 0.4f);
            var star_drawer = shooting_stars.GetComponent<ParticleSystemRenderer>();
            star_drawer.renderMode = ParticleSystemRenderMode.Stretch;
            star_drawer.velocityScale = 0.18f;
            star_drawer.lengthScale = 2f;
        }

        // Moves the effects on one frame: prints where feet have landed, breath on a slow rhythm
        // (quicker for a panting dog), and the odd shooting star.
        public void update(float delta, Vector3[] em_feet, Vector3[] bodhi_paws, Vector3 em_mouth, Vector3 bodhi_mouth, float em_facing, float bodhi_facing)
        {
            for (int foot = 0; foot < em_feet.Length; foot++)
            {
                leave_print(foot, em_feet[foot], em_facing, new Vector3(0.11f, 0.26f, 1f), 0.24f);
            }
            for (int paw = 0; paw < bodhi_paws.Length; paw++)
            {
                leave_print(2 + paw, bodhi_paws[paw], bodhi_facing, new Vector3(0.08f, 0.09f, 1f), 0.3f);
            }

            breath_clock_em -= delta;
            if (breath_clock_em <= 0)
            {
                breath_clock_em = 2.4f + (float)random.NextDouble() * 1.2f;
                puff_breath(em_mouth, em_facing, 3);
            }
            breath_clock_bodhi -= delta;
            if (breath_clock_bodhi <= 0)
            {
                breath_clock_bodhi = 0.7f + (float)random.NextDouble() * 0.4f;
                puff_breath(bodhi_mouth, bodhi_facing, 2);
            }

            star_clock -= delta;
            if (star_clock <= 0)
            {
                star_clock = 7f + (float)random.NextDouble() * 9f;
                var start = new Vector3((float)(random.NextDouble() * 2 - 1) * 500, 260 + (float)random.NextDouble() * 160, 900);
                var heading = new Vector3((float)(random.NextDouble() < 0.5 ? -1 : 1) * 700, -260, 0);
                shooting_stars.Emit(new ParticleSystem.EmitParams { position = scene.TransformPoint(start), velocity = heading }, 1);
            }
        }

        // Presses a print into the snow where a foot is, if the foot is down on the snow and has
        // moved at least `stride` metres since its last print.
        void leave_print(int index, Vector3 foot_world, float facing, Vector3 size, float stride)
        {
            var foot = scene.InverseTransformPoint(foot_world);
            if (foot.y > 0.12f)
            {
                return;  // In the air.
            }
            var flat = new Vector3(foot.x, 0, foot.z);
            if ((flat - last_prints[index]).sqrMagnitude < stride * stride)
            {
                return;
            }
            last_prints[index] = flat;
            prints.Emit(new ParticleSystem.EmitParams
            {
                position = scene.TransformPoint(flat + Vector3.up * 0.015f),
                startSize3D = size,
                rotation = facing,
                startColor = print_colour,
            }, 1);
        }

        // A breath: a few soft wisps at the mouth, drifting the way the face points.
        void puff_breath(Vector3 mouth_world, float facing, int wisps)
        {
            var forward = Quaternion.Euler(0, facing, 0) * Vector3.forward;
            for (int wisp = 0; wisp < wisps; wisp++)
            {
                var drift = scene.TransformDirection(forward * (0.18f + 0.1f * (float)random.NextDouble()) + Vector3.up * 0.05f);
                breath.Emit(new ParticleSystem.EmitParams { position = mouth_world, velocity = drift }, 1);
            }
        }

        // Runs the chimney smoke and lantern motes on by hand, for still pictures (the snapshot
        // tool), since particles only move by themselves while the game plays.
        public void run_for(float seconds)
        {
            smoke.Simulate(seconds, true, false);
            motes.Simulate(seconds, true, false);
        }

        // A gradient from `colour` to see-through: holds its strength, then fades from `fade_from`.
        static ParticleSystem.MinMaxGradient fade_out(Color colour, float fade_from)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(colour, 0), new GradientColorKey(colour, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(colour.a, 0.05f), new GradientAlphaKey(colour.a, fade_from), new GradientAlphaKey(0, 1) });
            return new ParticleSystem.MinMaxGradient(gradient);
        }

        // One particle system, stopped and empty, ready to be set up.
        ParticleSystem make(string name, Material material, int most_alive)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(scene, false);
            var system = holder.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.maxParticles = most_alive;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.startColor = Color.white;
            var emission = system.emission;
            emission.rateOverTime = 0;
            var shape = system.shape;
            shape.enabled = false;
            var drawer = holder.GetComponent<ParticleSystemRenderer>();
            drawer.sharedMaterial = material;
            drawer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            drawer.receiveShadows = false;
            system.Play();
            return system;
        }
    }
}
