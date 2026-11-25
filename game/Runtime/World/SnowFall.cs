// Purpose: snow in the air. Two particle systems (Unity's built-in way to draw many small moving
// dots): gently falling flakes that drift on a soft breeze, and short puffs of kicked-up snow
// when a paw lands, a snowball is set down or someone kneels. The puffs come from one system
// that is reused for every puff ("pooled"), never one made per puff, which would stutter.
using UnityEngine;

namespace Bodhi.Game
{
    public class SnowFall
    {
        readonly ParticleSystem flakes;
        readonly ParticleSystem puffs;

        // Builds both systems under `parent`, with the flakes falling over a `width` by `width`
        // square of ground around `centre`.
        public SnowFall(Transform parent, Vector3 centre, float width)
        {
            flakes = make_system("falling snow", parent, WorldLook.particles(false), 1200);
            flakes.transform.localPosition = centre + Vector3.up * 9f;
            var main = flakes.main;
            main.startLifetime = 16f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new Color(0.9f, 0.94f, 1f, 0.42f);
            main.gravityModifier = 0.012f;
            var shape = flakes.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(width, 0.5f, width);
            var emission = flakes.emission;
            emission.rateOverTime = 14f;
            var wind = flakes.velocityOverLifetime;  // A slow, steady breeze across the scene.
            wind.enabled = true;
            wind.space = ParticleSystemSimulationSpace.World;
            wind.x = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            wind.y = new ParticleSystem.MinMaxCurve(-0.55f, -0.35f);
            wind.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            var flutter = flakes.noise;  // Each flake wanders a little as it falls.
            flutter.enabled = true;
            flutter.strength = 0.25f;
            flutter.frequency = 0.4f;
            flutter.scrollSpeed = 0.2f;
            flakes.Play();

            puffs = make_system("snow puffs", parent, WorldLook.particles(false), 300);
            var puff_main = puffs.main;
            puff_main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            puff_main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.7f);
            puff_main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.2f);
            puff_main.startColor = new Color(0.9f, 0.93f, 1f, 0.75f);
            puff_main.gravityModifier = 0.45f;
            var puff_shape = puffs.shape;
            puff_shape.enabled = true;
            puff_shape.shapeType = ParticleSystemShapeType.Hemisphere;
            puff_shape.radius = 0.12f;
            var puff_emission = puffs.emission;
            puff_emission.rateOverTime = 0f;
            var shrink = puffs.sizeOverLifetime;  // Each bit of snow shrinks away, never pops out.
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0));
            puffs.Play();
        }

        // A puff of `amount` bits of snow kicked up at `position` (in the scene's own space).
        public void puff(Vector3 position, int amount)
        {
            var settings = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
            };
            puffs.Emit(settings, Mathf.Clamp(amount, 1, 60));
        }

        // Runs the snow on as if `seconds` had passed, for still pictures made outside play
        // (the title snapshot tool). Normally Unity moves particles on by itself.
        public void run_for(float seconds)
        {
            flakes.Simulate(seconds, true, true);
        }

        // One particle system, ready to configure. Unity starts a new system playing at once
        // with its defaults, so it's stopped and cleared before the settings go in.
        static ParticleSystem make_system(string name, Transform parent, Material material, int most_alive)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            var system = holder.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.maxParticles = most_alive;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            var drawer = holder.GetComponent<ParticleSystemRenderer>();
            drawer.sharedMaterial = material;
            drawer.renderMode = ParticleSystemRenderMode.Billboard;
            drawer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            drawer.receiveShadows = false;
            return system;
        }
    }
}
