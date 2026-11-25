// Purpose: the snowy land of the title scene, all built in code in the storybook style (soft,
// rounded, gently painted, so it matches the characters): the snowfield where Em and Bodhi play
// (flat in the middle, rolling hills further out, rising toward the mountains), a frozen lake that
// mirrors the aurora, three mountain ranges one behind another (fog fades the farther ones, which
// is what makes them read as far away), snow-laden pines near and a forest line far off, bushes
// and drifts, boulders, a snowy fence leading to a little cabin with a porch, lit windows and a
// woodpile, and a wooden sled. The cabin's warm windows are the scene's far-off warm light: home.
//
// The middle of the snowfield, where the snowman is built, is the world's origin (0, 0, 0).
// The camera looks from near (0, 1.6, -13) toward +z, so "far" means larger z.
using System.Collections.Generic;
using UnityEngine;

namespace Bodhi.Game
{
    public class SnowLand
    {
        public const float play_radius = 11f;   // Within this distance of the origin the snow is flat.

        // The frozen lake: its middle and its half-widths across (x) and away (z).
        public static readonly Vector3 lake_middle = new Vector3(9f, 0f, 21f);
        public const float lake_across = 10f, lake_away = 4.5f;

        static readonly Color snow = new Color(0.86f, 0.9f, 0.97f);
        static readonly Color snow_shade = new Color(0.72f, 0.79f, 0.9f);
        static readonly Color rock = new Color(0.2f, 0.22f, 0.29f);
        static readonly Color forest = new Color(0.05f, 0.1f, 0.1f);
        static readonly Color needles = new Color(0.07f, 0.17f, 0.15f);
        static readonly Color bark = new Color(0.24f, 0.15f, 0.1f);
        static readonly Color timber = new Color(0.33f, 0.21f, 0.14f);
        static readonly Color timber_dark = new Color(0.2f, 0.13f, 0.09f);

        // Where the cabin's window and chimney are, so the scene can light it and add smoke.
        public Vector3 cabin_window { get; private set; }
        public Vector3 chimney_top { get; private set; }

        // A spot on the cabin, given in the cabin's own measurements (x along its front, y up, z
        // from front to back), turned into a spot in the scene. Used to hang its Christmas lights.
        public Vector3 on_cabin(float x, float y, float z)
        {
            return cabin_spot + cabin_turn * new Vector3(x, y, z);
        }
        Vector3 cabin_spot;
        Quaternion cabin_turn;

        readonly List<Vector3> taken = new List<Vector3>();  // Spots already used, so things don't overlap.

        // Builds everything under `parent`.
        public SnowLand(Transform parent)
        {
            var snow_material = WorldLook.solid(rim: 0.12f, sparkle: 1f, wrap: 0.55f, grain: 0.1f);
            var plain_material = WorldLook.solid(rim: 0.18f, wrap: 0.5f, grain: 0.22f);
            var mountain_material = WorldLook.solid(rim: 0f, wrap: 0.45f, grain: 0.06f);

            var ground = new MeshKit();
            ground.add_height_grid(-280, 280, -40, 310, 190, 150, ground_height, ground_colour);
            var ground_object = MeshKit.show("snowfield", ground.build("snowfield"), snow_material, parent, false);
            ground_object.GetComponent<MeshRenderer>().receiveShadows = true;

            add_lake(parent);
            add_mountains(parent, mountain_material);
            add_cabin(parent, plain_material, snow_material);
            add_fence(parent, plain_material);
            add_trees(parent, plain_material);
            add_far_forest(parent, plain_material);
            add_boulders(parent, plain_material);
            add_drifts_and_bushes(parent, snow_material, plain_material);
            add_sled(parent, plain_material);
        }

        // How high the snow is at (x, z). Flat where they play, rolling hills beyond, a hollow for
        // the lake, and the land rising gently toward the mountains. Small ripples everywhere.
        public static float ground_height(float x, float z)
        {
            float from_play = Mathf.Sqrt(x * x + z * z);
            float settle = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(play_radius, play_radius + 18, from_play));
            float hills = (Mathf.PerlinNoise(x * 0.025f + 3.1f, z * 0.025f + 7.7f) - 0.5f) * 7f
                        + (Mathf.PerlinNoise(x * 0.09f + 11f, z * 0.09f + 2f) - 0.5f) * 1.2f;
            float rise = Mathf.Max(0, z - 50) * 0.11f;
            float ripples = (Mathf.PerlinNoise(x * 0.7f, z * 0.7f) - 0.5f) * 0.06f;
            float height = (hills + rise) * settle + ripples;
            // The lake: a flat hollow just below the ice, with a soft bank round it.
            float bank = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.0f, 1.6f, lake_distance(x, z)));
            return Mathf.Lerp(-0.08f, height, bank);
        }

        // How far (x, z) is from the lake's middle, in lake-sizes: under 1 is on the ice.
        public static float lake_distance(float x, float z)
        {
            float across = (x - lake_middle.x) / lake_across, away = (z - lake_middle.z) / lake_away;
            return Mathf.Sqrt(across * across + away * away);
        }

        // The snow's colour: brighter on slopes facing up, a little blue-grey in hollows.
        static Color ground_colour(Vector3 point, Vector3 facing)
        {
            float variation = Mathf.PerlinNoise(point.x * 0.15f + 40f, point.z * 0.15f);
            float fine = Mathf.PerlinNoise(point.x * 1.3f, point.z * 1.3f + 9f);
            return Color.Lerp(snow_shade, snow, 0.5f + 0.38f * variation + 0.12f * fine);
        }

        // The frozen lake: an oval of ice drawn by the Bodhi/Ice shader, which mirrors the night
        // sky and the aurora's glow.
        static void add_lake(Transform parent)
        {
            var kit = new MeshKit();
            var rings = new List<MeshKit.Ring>();
            for (int step = 12; step >= 0; step--)  // From the rim in to the middle, so it faces up.
            {
                rings.Add(new MeshKit.Ring(0, step / 12f, Color.white));
            }
            kit.add_round_shape(lake_middle, Quaternion.identity, rings, 48, new Vector3(lake_across, 1, lake_away));
            var lake = MeshKit.show("frozen lake", kit.build("lake"), new Material(Shader.Find("Bodhi/Ice")), parent, false);
            lake.GetComponent<MeshRenderer>().receiveShadows = false;
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
                    float snowy = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.48f, 0.74f, facing.y + streaks * 0.45f));
                    var surface = Color.Lerp(rock, snow, snowy);
                    if (range.forested)
                    {
                        float low = Mathf.InverseLerp(range.peak * 0.32f, range.peak * 0.12f, point.y);
                        surface = Color.Lerp(surface, forest, Mathf.Clamp01(low) * 0.9f);
                    }
                    return surface;
                };
                kit.add_height_grid(-2400, 2400, range.middle_z - half, range.middle_z + half, 420, 52, height, colour);
                MeshKit.show("range", kit.build("mountain range"), material, holder.transform, false);
            }
        }

        // The height of a mountain range at (x, z): ridged peaks along its length (noise folded so
        // its creases point up, the usual trick for mountain ridges), falling away to nothing at
        // its front and back edges.
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
                float folded = Mathf.Max(0, 1 - Mathf.Abs(wave * 2 - 1));  // Perlin noise can stray a hair past 0 to 1.
                ridge += Mathf.Pow(folded, 1.6f) * size;
                size *= 0.48f;
                scale *= 2.1f;
            }
            float peaks = 0.45f + 0.55f * Mathf.PerlinNoise(x * 0.0012f + seed, seed);
            return peak * ridge * peaks * Mathf.Pow(across, 0.75f) - 2;
        }

        // True if a spot is free: not in the play area, the view to the snowman, the lake, or too
        // near something already placed.
        bool free_spot(Vector3 spot, float room)
        {
            bool in_clearing = Mathf.Sqrt(spot.x * spot.x + spot.z * spot.z) < play_radius + 3;
            bool in_view_corridor = spot.z < 18 && Mathf.Abs(spot.x) < 9 + spot.z * 0.5f;
            bool on_lake = lake_distance(spot.x, spot.z) < 1.5f;
            if (in_clearing || in_view_corridor || on_lake)
            {
                return false;
            }
            foreach (var other in taken)
            {
                if ((other - spot).sqrMagnitude < room * room)
                {
                    return false;
                }
            }
            return true;
        }

        // Pines round the clearing, framing the picture at the sides.
        void add_trees(Transform parent, Material material)
        {
            var random = new System.Random(77);
            var kit = new MeshKit();
            int planted = 0;
            for (int attempt = 0; attempt < 1500 && planted < 95; attempt++)
            {
                var spot = new Vector3((float)(random.NextDouble() * 2 - 1) * 70, 0, (float)(random.NextDouble() * 110) - 12);
                if (!free_spot(spot, 2.4f))
                {
                    continue;
                }
                taken.Add(spot);
                float tall = 4.5f + (float)random.NextDouble() * 5.5f;
                spot.y = ground_height(spot.x, spot.z) - 0.2f;
                add_pine(kit, spot, tall, random, 14);
                planted += 1;
            }
            MeshKit.show("pines", kit.build("pines"), material, parent, true);
        }

        // One snow-laden storybook pine: a short trunk under four to six puffy tiers, each one
        // rounded like a bell, its top heaped with lumpy snow and its underside dark green.
        public static void add_pine(MeshKit kit, Vector3 foot, float tall, System.Random random, int sides)
        {
            kit.add_tube(foot, foot + Vector3.up * tall * 0.3f, tall * 0.04f, tall * 0.028f, bark, bark, 8);
            int tiers = 4 + random.Next(3);
            for (int tier = 0; tier < tiers; tier++)
            {
                float share = (float)tier / tiers;
                float width = tall * 0.31f * (1 - share * 0.76f);
                float tier_tall = tall * (0.36f - share * 0.13f);
                var tier_foot = foot + Vector3.up * tall * (0.15f + share * 0.7f);
                var snowy = Color.Lerp(needles, snow, 0.86f);
                var rings = new List<MeshKit.Ring>
                {
                    new MeshKit.Ring(-tier_tall * 0.1f, width * 0.78f, needles * 0.55f),
                    new MeshKit.Ring(-tier_tall * 0.02f, width * 0.98f, needles * 0.8f),
                    new MeshKit.Ring(tier_tall * 0.06f, width, Color.Lerp(needles, snow, 0.45f)),
                };
                for (int step = 1; step <= 5; step++)  // The rounded, bell-shaped top, heaped with snow.
                {
                    float up = step / 5f;
                    float bell = width * Mathf.Pow(1 - up, 0.75f) * 0.92f;
                    rings.Add(new MeshKit.Ring(tier_tall * (0.06f + up * 0.94f), bell, Color.Lerp(snowy, needles, up < 0.3f ? 0.25f : 0.05f)));
                }
                var lean = Quaternion.Euler((float)random.NextDouble() * 4 - 2, (float)random.NextDouble() * 360, (float)random.NextDouble() * 4 - 2);
                kit.add_round_shape(tier_foot, lean, rings, sides, Vector3.one, width * 0.1f, 0.5f);
            }
        }

        // A forest line on the far hills: hundreds of small, simple pines, softened by the fog.
        void add_far_forest(Transform parent, Material material)
        {
            var random = new System.Random(310);
            var kit = new MeshKit();
            for (int planted = 0; planted < 420; planted++)
            {
                float z = 75 + (float)random.NextDouble() * 160;
                float x = (float)(random.NextDouble() * 2 - 1) * (120 + z * 0.9f);
                if (lake_distance(x, z) < 1.6f)
                {
                    continue;
                }
                float tall = 4f + (float)random.NextDouble() * 6f;
                add_pine(kit, new Vector3(x, ground_height(x, z) - 0.3f, z), tall, random, 7);
            }
            MeshKit.show("far forest", kit.build("far forest"), material, parent, false);
        }

        // A few boulders poking out of the snow near the clearing, dark below, capped with snow.
        void add_boulders(Transform parent, Material material)
        {
            var kit = new MeshKit();
            var spots = new[] { new Vector3(-7.5f, 0, 6f), new Vector3(8.5f, 0, 9f), new Vector3(-12f, 0, 15f), new Vector3(5.5f, 0, -3.5f), new Vector3(-16f, 0, 4f) };
            var sizes = new[] { 0.9f, 1.3f, 1.8f, 0.55f, 1.1f };
            for (int index = 0; index < spots.Length; index++)
            {
                float size = sizes[index];
                taken.Add(spots[index]);
                var foot = spots[index] + Vector3.up * (ground_height(spots[index].x, spots[index].z) - size * 0.35f);
                var rings = new[]
                {
                    new MeshKit.Ring(0, size * 0.9f, rock),
                    new MeshKit.Ring(size * 0.35f, size, rock),
                    new MeshKit.Ring(size * 0.62f, size * 0.9f, Color.Lerp(rock, snow, 0.4f)),
                    new MeshKit.Ring(size * 0.85f, size * 0.66f, snow),
                    new MeshKit.Ring(size * 1.0f, size * 0.36f, snow),
                    new MeshKit.Ring(size * 1.08f, 0, snow),
                };
                kit.add_round_shape(foot, Quaternion.Euler(0, index * 70, 0), rings, 18, new Vector3(1.2f, 1, 0.9f), size * 0.16f, size * 0.6f);
            }
            MeshKit.show("boulders", kit.build("boulders"), material, parent, true);
        }

        // Soft snowdrifts heaped round the clearing's edge, and round snow-covered bushes with dark
        // green peeping out underneath.
        void add_drifts_and_bushes(Transform parent, Material snow_material, Material plain_material)
        {
            var random = new System.Random(55);
            var drifts = new MeshKit();
            var bushes = new MeshKit();
            for (int attempt = 0; attempt < 400; attempt++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float distance = play_radius + 1 + (float)random.NextDouble() * 14;
                var spot = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);
                if (spot.z < -6 || (spot.z < 16 && Mathf.Abs(spot.x) < 5 + spot.z * 0.4f) || lake_distance(spot.x, spot.z) < 1.3f)
                {
                    continue;
                }
                spot.y = ground_height(spot.x, spot.z);
                if (random.NextDouble() < 0.55)
                {
                    float long_way = 1.2f + (float)random.NextDouble() * 2.5f;
                    var radii = new Vector3(long_way, 0.35f + (float)random.NextDouble() * 0.35f, long_way * 0.55f);
                    drifts.add_ball(spot + Vector3.down * 0.15f, radii, snow, 12, 0.05f, 0.6f, Quaternion.Euler(0, angle * Mathf.Rad2Deg + 90, 0));
                }
                else if (random.NextDouble() < 0.5)
                {
                    float size = 0.45f + (float)random.NextDouble() * 0.5f;
                    var rings = new[]
                    {
                        new MeshKit.Ring(0, size * 0.85f, needles * 0.7f),
                        new MeshKit.Ring(size * 0.3f, size, needles),
                        new MeshKit.Ring(size * 0.55f, size * 0.95f, Color.Lerp(needles, snow, 0.7f)),
                        new MeshKit.Ring(size * 0.85f, size * 0.6f, snow),
                        new MeshKit.Ring(size * 1.0f, 0, snow),
                    };
                    bushes.add_round_shape(spot + Vector3.down * 0.1f, Quaternion.Euler(0, (float)random.NextDouble() * 360, 0), rings, 14, new Vector3(1.2f, 0.9f, 1f), size * 0.18f, size * 0.5f);
                }
            }
            MeshKit.show("snowdrifts", drifts.build("drifts"), snow_material, parent, false).GetComponent<MeshRenderer>().receiveShadows = true;
            MeshKit.show("bushes", bushes.build("bushes"), plain_material, parent, true);
        }

        // A snowy wooden fence, post by post, along the path up to the cabin.
        void add_fence(Transform parent, Material material)
        {
            var kit = new MeshKit();
            var start = new Vector3(-10, 0, 14);
            var end = new Vector3(-23, 0, 52);
            Vector3 last_top = Vector3.zero;
            const int posts = 14;
            for (int post = 0; post < posts; post++)
            {
                float share = (float)post / (posts - 1);
                var spot = Vector3.Lerp(start, end, share) + new Vector3(Mathf.Sin(share * 4) * 2.5f, 0, 0);
                spot.y = ground_height(spot.x, spot.z);
                taken.Add(spot);
                var lean = new Vector3(Mathf.Sin(post * 1.7f) * 0.06f, 0, Mathf.Cos(post * 2.3f) * 0.04f);
                var top = spot + new Vector3(0, 1.05f, 0) + lean;
                kit.add_tube(spot + Vector3.down * 0.2f, top, 0.065f, 0.055f, timber_dark, timber, 7);
                kit.add_ball(top + Vector3.up * 0.04f, new Vector3(0.09f, 0.06f, 0.09f), snow, 6, 0.01f, 0.05f);
                if (post > 0)
                {
                    var middle_now = top + Vector3.down * 0.35f;
                    var middle_then = last_top + Vector3.down * 0.35f;
                    kit.add_tube(middle_then, middle_now, 0.035f, 0.035f, timber, timber, 6);
                    kit.add_tube(middle_then + Vector3.up * 0.04f, middle_now + Vector3.up * 0.04f, 0.03f, 0.03f, snow, snow, 6, 0.006f, 0.05f);
                }
                last_top = top;
            }
            MeshKit.show("fence", kit.build("fence"), material, parent, true);
        }

        // A wooden sled left in the snow by the trees on the right, its rope trailing.
        void add_sled(Transform parent, Material material)
        {
            var spot = new Vector3(5.2f, 0, 7.2f);
            spot.y = ground_height(spot.x, spot.z);
            taken.Add(spot);
            var turn = Quaternion.Euler(0, -35, 0);
            var kit = new MeshKit();
            for (int side = -1; side <= 1; side += 2)
            {
                var runner_back = spot + turn * new Vector3(side * 0.22f, 0.04f, -0.5f);
                var runner_front = spot + turn * new Vector3(side * 0.22f, 0.04f, 0.45f);
                var runner_curl = spot + turn * new Vector3(side * 0.22f, 0.22f, 0.62f);
                kit.add_tube(runner_back, runner_front, 0.022f, 0.022f, timber_dark, timber_dark, 6);
                kit.add_tube(runner_front, runner_curl, 0.022f, 0.02f, timber_dark, timber_dark, 6);
                kit.add_box(spot + turn * new Vector3(side * 0.22f, 0.1f, -0.1f), new Vector3(0.035f, 0.11f, 0.06f), timber_dark, turn);
            }
            for (int slat = 0; slat < 5; slat++)
            {
                kit.add_box(spot + turn * new Vector3(0, 0.17f, -0.42f + slat * 0.2f), new Vector3(0.56f, 0.025f, 0.14f), timber, turn);
            }
            kit.add_ball(spot + turn * new Vector3(0.05f, 0.2f, -0.2f), new Vector3(0.2f, 0.04f, 0.18f), snow, 8, 0.01f, 0.05f);
            var rope = new Color(0.6f, 0.5f, 0.35f);
            kit.add_tube(spot + turn * new Vector3(-0.2f, 0.2f, 0.6f), spot + turn * new Vector3(0.4f, 0.02f, 1.2f), 0.012f, 0.012f, rope, rope, 5);
            MeshKit.show("sled", kit.build("sled"), material, parent, true);
        }

        // A small timber cabin up the slope: snow piled soft on the roof, a porch with a little
        // roof of its own, two windows glowing warm, smoke from the chimney, and a woodpile.
        void add_cabin(Transform parent, Material material, Material snow_material)
        {
            var spot = new Vector3(-26, 0, 58);
            spot.y = ground_height(spot.x, spot.z);
            taken.Add(spot);
            var turn = Quaternion.Euler(0, 24, 0);
            cabin_spot = spot;
            cabin_turn = turn;
            var kit = new MeshKit();
            System.Func<float, float, float, Vector3> at = (x, y, z) => spot + turn * new Vector3(x, y, z);
            kit.add_box(at(0, 1.4f, 0), new Vector3(6, 2.8f, 4.4f), timber, turn);
            for (int log = 0; log < 7; log++)  // Log ends at the corners, for a timber look.
            {
                for (int corner = 0; corner < 4; corner++)
                {
                    float x = corner % 2 == 0 ? -3.02f : 3.02f, z = corner < 2 ? -2.22f : 2.22f;
                    kit.add_ball(at(x, 0.2f + log * 0.4f, z), Vector3.one * 0.17f, timber_dark, 6);
                }
            }
            for (int side = -1; side <= 1; side += 2)  // The roof boards under the snow.
            {
                kit.add_box(at(side * 1.55f, 3.62f, 0), new Vector3(3.8f, 0.18f, 5.0f), timber_dark, turn * Quaternion.Euler(0, 0, side * -34));
            }
            kit.add_box(at(1.6f, 4.5f, 0.8f), new Vector3(0.6f, 1.6f, 0.6f), rock, turn);
            kit.add_box(at(1.6f, 1.05f, -2.22f), new Vector3(1.0f, 2.1f, 0.08f), timber_dark, turn);
            // The porch: a floor, two posts and a little roof over the door.
            kit.add_box(at(0.6f, 0.15f, -2.9f), new Vector3(3.6f, 0.15f, 1.4f), timber, turn);
            for (int side = -1; side <= 1; side += 2)
            {
                kit.add_tube(at(0.6f + side * 1.6f, 0.2f, -3.5f), at(0.6f + side * 1.6f, 2.3f, -3.5f), 0.07f, 0.07f, timber_dark, timber_dark, 6);
            }
            kit.add_box(at(0.6f, 2.4f, -3.0f), new Vector3(3.8f, 0.12f, 1.6f), timber_dark, turn * Quaternion.Euler(-12, 0, 0));
            // The woodpile against the side wall: logs stacked three high.
            for (int row = 0; row < 3; row++)
            {
                for (int log = 0; log < 5 - row; log++)
                {
                    var log_middle = at(-3.4f, 0.16f + row * 0.27f, -1.2f + log * 0.28f + row * 0.14f);
                    kit.add_tube(log_middle + turn * new Vector3(-0.45f, 0, 0), log_middle + turn * new Vector3(0.45f, 0, 0), 0.13f, 0.13f, bark, timber, 7);
                }
            }
            MeshKit.show("cabin", kit.build("cabin"), material, parent, true);

            // Snow lying soft and thick on the roof, the porch roof, the chimney and the woodpile.
            var roof_snow = new MeshKit();
            for (int side = -1; side <= 1; side += 2)
            {
                roof_snow.add_ball(at(side * 1.5f, 3.8f, 0), new Vector3(1.95f, 0.28f, 2.6f), snow, 12, 0.05f, 0.4f, turn * Quaternion.Euler(0, 0, side * -34));
            }
            roof_snow.add_ball(at(0, 4.68f, 0), new Vector3(0.45f, 0.2f, 2.65f), snow, 10, 0.04f, 0.3f, turn);
            roof_snow.add_ball(at(0.6f, 2.52f, -3.0f), new Vector3(1.95f, 0.13f, 0.85f), snow, 10, 0.03f, 0.3f, turn * Quaternion.Euler(-12, 0, 0));
            roof_snow.add_ball(at(1.6f, 5.32f, 0.8f), new Vector3(0.36f, 0.14f, 0.36f), snow, 8, 0.02f, 0.2f, turn);
            roof_snow.add_ball(at(-3.4f, 0.98f, -0.6f), new Vector3(0.5f, 0.12f, 0.9f), snow, 8, 0.03f, 0.3f, turn);
            MeshKit.show("roof snow", roof_snow.build("roof snow"), snow_material, parent, true);

            chimney_top = at(1.6f, 5.5f, 0.8f);
            var windows = new MeshKit();
            cabin_window = at(-1.4f, 1.55f, -2.25f);
            windows.add_box(cabin_window, new Vector3(1.2f, 0.9f, 0.06f), Color.white, turn);
            windows.add_box(at(-3.03f, 1.55f, 0.9f), new Vector3(0.06f, 0.8f, 1.0f), Color.white, turn);
            MeshKit.show("cabin windows", windows.build("windows"), WorldLook.glowing(PaletteColours.warm_amber, 3.4f), parent, false);
            var frames = new MeshKit();
            frames.add_box(cabin_window + turn * new Vector3(0, 0, -0.02f), new Vector3(0.06f, 0.9f, 0.04f), timber_dark, turn);
            frames.add_box(cabin_window + turn * new Vector3(0, 0, -0.02f), new Vector3(1.2f, 0.06f, 0.04f), timber_dark, turn);
            MeshKit.show("window frames", frames.build("frames"), material, parent, false);
        }
    }
}
