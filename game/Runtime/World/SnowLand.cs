// Purpose: the snowy land of the title scene, all built in code: the snowfield where Em and Bodhi
// play (flat in the middle, rolling hills further out, rising toward the mountains), three
// mountain ranges one behind another (fog fades the farther ones, which is what makes them read
// as far away), snow-laden pine trees, a few boulders, and a little cabin on the hillside with a
// warm lit window, the scene's one warm light far off: home.
//
// The middle of the snowfield, where the snowman is built, is the world's origin (0, 0, 0).
// The camera looks from near (0, 1.6, -9) toward +z, so "far" means larger z.
using UnityEngine;

namespace Bodhi.Game
{
    public class SnowLand
    {
        public const float play_radius = 11f;   // Within this distance of the origin the snow is flat.

        static readonly Color snow = new Color(0.86f, 0.9f, 0.97f);
        static readonly Color snow_shade = new Color(0.72f, 0.79f, 0.9f);
        static readonly Color rock = new Color(0.17f, 0.2f, 0.26f);
        static readonly Color forest = new Color(0.05f, 0.1f, 0.1f);
        static readonly Color needles = new Color(0.04f, 0.11f, 0.1f);
        static readonly Color bark = new Color(0.16f, 0.11f, 0.08f);
        static readonly Color timber = new Color(0.22f, 0.14f, 0.1f);

        // Where the cabin stands, so the scene can put its window light there.
        public Vector3 cabin_window { get; private set; }

        // Builds everything under `parent`.
        public SnowLand(Transform parent)
        {
            var snow_material = WorldLook.solid(rim: 0.1f, sparkle: 1f, wrap: 0.5f);
            var plain_material = WorldLook.solid(rim: 0.15f, wrap: 0.4f);
            var mountain_material = WorldLook.solid(rim: 0f, wrap: 0.35f, sparkle: 0f);

            var ground = new MeshKit();
            ground.add_height_grid(-280, 280, -40, 310, 170, 130, ground_height, ground_colour);
            var ground_object = MeshKit.show("snowfield", ground.build("snowfield"), snow_material, parent, false);
            ground_object.GetComponent<MeshRenderer>().receiveShadows = true;

            add_mountains(parent, mountain_material);
            add_trees(parent, plain_material);
            add_boulders(parent, plain_material);
            add_cabin(parent, plain_material);
        }

        // How high the snow is at (x, z). Flat where they play, rolling hills beyond, and the land
        // rising gently toward the mountains. Small ripples, like wind-blown snow, everywhere.
        public static float ground_height(float x, float z)
        {
            float from_play = Mathf.Sqrt(x * x + z * z);
            float settle = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(play_radius, play_radius + 18, from_play));
            float hills = (Mathf.PerlinNoise(x * 0.025f + 3.1f, z * 0.025f + 7.7f) - 0.5f) * 7f
                        + (Mathf.PerlinNoise(x * 0.09f + 11f, z * 0.09f + 2f) - 0.5f) * 1.2f;
            float rise = Mathf.Max(0, z - 50) * 0.11f;
            float ripples = (Mathf.PerlinNoise(x * 0.7f, z * 0.7f) - 0.5f) * 0.06f;
            return (hills + rise) * settle + ripples;
        }

        // The snow's colour: brighter on slopes facing up, a little blue-grey in hollows.
        static Color ground_colour(Vector3 point, Vector3 facing)
        {
            float variation = Mathf.PerlinNoise(point.x * 0.15f + 40f, point.z * 0.15f);
            return Color.Lerp(snow_shade, snow, 0.55f + 0.45f * variation);
        }

        // Three ranges, near to far. Nearer ones are lower and darker at their feet (forest);
        // the farthest are the tallest and palest, mostly lost in the fog.
        static void add_mountains(Transform parent, Material material)
        {
            var ranges = new[]
            {
                (middle_z: 330f, depth: 140f, peak: 62f, seed: 1.7f, forested: true),
                (middle_z: 560f, depth: 180f, peak: 120f, seed: 5.3f, forested: false),
                (middle_z: 900f, depth: 260f, peak: 205f, seed: 9.1f, forested: false),
            };
            var holder = new GameObject("Mountains");
            holder.transform.SetParent(parent, false);
            foreach (var range in ranges)
            {
                var kit = new MeshKit();
                float half = range.depth / 2;
                System.Func<float, float, float> height = (x, z) => mountain_height(x, z, range.middle_z, half, range.peak, range.seed);
                System.Func<Vector3, Vector3, Color> colour = (point, facing) =>
                {
                    float streaks = MeshKit.noise(point * 0.02f) - 0.5f;
                    float snowy = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.5f, 0.78f, facing.y + streaks * 0.45f));
                    var surface = Color.Lerp(rock, snow, snowy);
                    if (range.forested)
                    {
                        float low = Mathf.InverseLerp(range.peak * 0.32f, range.peak * 0.12f, point.y);
                        surface = Color.Lerp(surface, forest, Mathf.Clamp01(low) * 0.9f);
                    }
                    return surface;
                };
                kit.add_height_grid(-2400, 2400, range.middle_z - half, range.middle_z + half, 300, 40, height, colour);
                MeshKit.show("range", kit.build("mountain range"), material, holder.transform, false);
            }
        }

        // The height of a mountain range at (x, z): sharp ridged peaks along its length (noise
        // folded so its creases point up, the usual trick for mountain ridges), falling away to
        // nothing at its front and back edges.
        static float mountain_height(float x, float z, float middle_z, float half_depth, float peak, float seed)
        {
            float across = 1 - Mathf.Pow((z - middle_z) / half_depth, 2);
            if (across <= 0)
            {
                return -2;  // Tucked just under the snow.
            }
            float ridge = 0, size = 1, scale = 0.0035f;
            for (int octave = 0; octave < 4; octave++)
            {
                float wave = Mathf.PerlinNoise(x * scale + seed * 13, z * scale * 0.8f + seed * 7);
                float folded = 1 - Mathf.Abs(wave * 2 - 1);
                ridge += folded * folded * size;
                size *= 0.5f;
                scale *= 2.1f;
            }
            float peaks = 0.45f + 0.55f * Mathf.PerlinNoise(x * 0.0012f + seed, seed);
            return peak * ridge * peaks * Mathf.Pow(across, 0.75f) - 2;
        }

        // Pines around the clearing: more toward the back and sides, none between the camera and
        // the snowman, and a few close by at the sides to frame the picture.
        static void add_trees(Transform parent, Material material)
        {
            var random = new System.Random(77);
            var kit = new MeshKit();
            int planted = 0;
            for (int attempt = 0; attempt < 600 && planted < 75; attempt++)
            {
                float x = (float)(random.NextDouble() * 2 - 1) * 70;
                float z = (float)(random.NextDouble() * 110) - 12;
                bool in_clearing = Mathf.Sqrt(x * x + z * z) < play_radius + 4;
                bool in_view_corridor = z < 18 && Mathf.Abs(x) < 9 + z * 0.5f;
                if (in_clearing || in_view_corridor)
                {
                    continue;
                }
                float tall = 4.5f + (float)random.NextDouble() * 5f;
                add_pine(kit, new Vector3(x, ground_height(x, z) - 0.2f, z), tall, random);
                planted += 1;
            }
            MeshKit.show("pines", kit.build("pines"), material, parent, true);
        }

        // One snow-laden pine: a short trunk under four to six tiers of branches, each tier a
        // cone whose upper side is white with snow and whose underside stays dark.
        static void add_pine(MeshKit kit, Vector3 foot, float tall, System.Random random)
        {
            kit.add_tube(foot, foot + Vector3.up * tall * 0.3f, tall * 0.035f, tall * 0.025f, bark, bark, 6);
            int tiers = 4 + random.Next(3);
            for (int tier = 0; tier < tiers; tier++)
            {
                float share = (float)tier / tiers;
                float width = tall * 0.3f * (1 - share * 0.78f);
                float tier_tall = tall * (0.34f - share * 0.12f);
                var tier_foot = foot + Vector3.up * tall * (0.16f + share * 0.7f);
                var snowy = Color.Lerp(needles, snow, 0.8f);
                var rings = new[]
                {
                    new MeshKit.Ring(-tier_tall * 0.08f, width * 0.82f, needles * 0.6f),
                    new MeshKit.Ring(0, width, needles),
                    new MeshKit.Ring(tier_tall * 0.1f, width * 0.84f, Color.Lerp(needles, snow, 0.25f)),
                    new MeshKit.Ring(tier_tall * 0.3f, width * 0.62f, snowy),
                    new MeshKit.Ring(tier_tall * 0.6f, width * 0.32f, Color.Lerp(snowy, needles, 0.45f)),
                    new MeshKit.Ring(tier_tall, 0, snowy),
                };
                var lean = Quaternion.Euler((float)random.NextDouble() * 4 - 2, (float)random.NextDouble() * 360, (float)random.NextDouble() * 4 - 2);
                kit.add_round_shape(tier_foot, lean, rings, 9, Vector3.one, width * 0.12f, 0.6f);
            }
        }

        // A few boulders poking out of the snow near the clearing, dark below, capped with snow.
        static void add_boulders(Transform parent, Material material)
        {
            var kit = new MeshKit();
            var spots = new[] { new Vector3(-7.5f, 0, 6f), new Vector3(8.5f, 0, 9f), new Vector3(-12f, 0, 15f), new Vector3(5.5f, 0, -3.5f) };
            var sizes = new[] { 0.9f, 1.3f, 1.8f, 0.55f };
            for (int index = 0; index < spots.Length; index++)
            {
                float size = sizes[index];
                var foot = spots[index] + Vector3.up * (ground_height(spots[index].x, spots[index].z) - size * 0.35f);
                var rings = new[]
                {
                    new MeshKit.Ring(0, size * 0.9f, rock),
                    new MeshKit.Ring(size * 0.35f, size, rock),
                    new MeshKit.Ring(size * 0.7f, size * 0.85f, Color.Lerp(rock, snow, 0.5f)),
                    new MeshKit.Ring(size * 0.95f, size * 0.5f, snow),
                    new MeshKit.Ring(size * 1.08f, 0, snow),
                };
                kit.add_round_shape(foot, Quaternion.Euler(0, index * 70, 0), rings, 12, new Vector3(1.2f, 1, 0.9f), size * 0.18f, size * 0.7f);
            }
            MeshKit.show("boulders", kit.build("boulders"), material, parent, true);
        }

        // A small timber cabin up the slope, roof deep in snow, one window glowing warm.
        void add_cabin(Transform parent, Material material)
        {
            var spot = new Vector3(-26, 0, 58);
            spot.y = ground_height(spot.x, spot.z);
            var turn = Quaternion.Euler(0, 24, 0);
            var kit = new MeshKit();
            kit.add_box(spot + turn * new Vector3(0, 1.4f, 0), new Vector3(6, 2.8f, 4.4f), timber, turn);
            // The roof: two snowy slabs leaning together, with a ridge of snow on top.
            for (int side = -1; side <= 1; side += 2)
            {
                var slab_turn = turn * Quaternion.Euler(0, 0, side * -34);
                kit.add_box(spot + turn * new Vector3(side * 1.55f, 3.75f, 0), new Vector3(3.9f, 0.35f, 5.2f), snow, slab_turn);
            }
            kit.add_box(spot + turn * new Vector3(1.6f, 4.6f, 0.8f), new Vector3(0.6f, 1.6f, 0.6f), rock, turn);
            kit.add_box(spot + turn * new Vector3(1.6f, 5.45f, 0.8f), new Vector3(0.75f, 0.2f, 0.75f), snow, turn);
            kit.add_box(spot + turn * new Vector3(1.6f, 1.05f, -2.22f), new Vector3(1.0f, 2.1f, 0.08f), bark, turn);
            MeshKit.show("cabin", kit.build("cabin"), material, parent, true);

            var window = new MeshKit();
            cabin_window = spot + turn * new Vector3(-1.4f, 1.55f, -2.25f);
            window.add_box(cabin_window, new Vector3(1.2f, 0.9f, 0.06f), Color.white, turn);
            MeshKit.show("cabin window", window.build("window"), WorldLook.glowing(PaletteColours.warm_amber, 3.2f), parent, false);
        }
    }
}
