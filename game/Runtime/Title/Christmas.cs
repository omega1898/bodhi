// Purpose: the Christmas touches on the title screen (the owner's choice for the season): a
// Christmas tree in the clearing with twinkling fairy lights, baubles, a glowing star and
// presents underneath; fairy lights along the cabin's eaves and porch; and a holly wreath on the
// cabin door. The snowman's Santa hat lives with the snowman (Snowman.cs) and Bodhi's red bow and
// bell with Bodhi (DogFigure.cs). The tree's soft glow on the snow is a small warm light.
using UnityEngine;

namespace Bodhi.Game
{
    public static class Christmas
    {
        // Where the Christmas tree stands: in the clearing, to the left of where they play.
        public static readonly Vector3 tree_spot = new Vector3(-6.4f, 0f, 7.6f);
        const float tree_tall = 3.3f;

        // The fairy lights' colours: warm white most, with red, green, gold and blue.
        static readonly Color[] bulb_colours =
        {
            new Color(1f, 0.85f, 0.6f), new Color(1f, 0.25f, 0.2f), new Color(1f, 0.85f, 0.6f),
            new Color(0.3f, 1f, 0.4f), new Color(1f, 0.7f, 0.2f), new Color(1f, 0.85f, 0.6f), new Color(0.35f, 0.55f, 1f),
        };
        static readonly Color[] bauble_colours = { new Color(0.75f, 0.08f, 0.1f), new Color(0.85f, 0.65f, 0.2f), new Color(0.75f, 0.08f, 0.1f), new Color(0.65f, 0.72f, 0.82f) };
        static readonly Color[] paper_colours = { new Color(0.7f, 0.1f, 0.12f), new Color(0.12f, 0.4f, 0.25f), new Color(0.15f, 0.25f, 0.55f), new Color(0.85f, 0.75f, 0.55f) };
        static readonly Color ribbon_gold = new Color(0.9f, 0.72f, 0.3f);
        static readonly Color ribbon_red = new Color(0.8f, 0.1f, 0.12f);
        static readonly Color holly = new Color(0.06f, 0.22f, 0.12f);
        static readonly Color snow = new Color(0.88f, 0.92f, 0.98f);

        // Builds every decoration under `parent`, hanging the cabin's lights from `land`'s cabin.
        public static void build(Transform parent, SnowLand land)
        {
            var plain = WorldLook.solid(rim: 0.2f, wrap: 0.5f, grain: 0.18f);
            var shiny = WorldLook.solid(rim: 0.3f, gloss: 0.85f, grain: 0.05f);
            var lights = WorldLook.fairy_lights(5f);

            build_tree(parent, plain, shiny, lights);
            build_cabin_lights(parent, land, lights, plain);
            WorldLook.lamp("Christmas tree glow", parent, tree_spot + Vector3.up * 1.4f, new Color(1f, 0.78f, 0.5f), 0.9f, 4.5f);
        }

        // The tree: a storybook pine with a light dusting of snow, a spiral of fairy lights wound
        // round it, baubles hung among the branches, a star on top, and presents at its foot.
        static void build_tree(Transform parent, Material plain, Material shiny, Material lights)
        {
            var random = new System.Random(1225);
            var branches = new MeshKit();
            SnowLand.add_pine(branches, tree_spot, tree_tall, random, 16);
            MeshKit.show("christmas tree", branches.build("christmas tree"), plain, parent, true);

            // The lights, wound round the tree in a spiral from the bottom tier to near the top.
            var bulbs = new MeshKit();
            int bulb_count = 70;
            for (int bulb = 0; bulb < bulb_count; bulb++)
            {
                float share = (float)bulb / bulb_count;
                float height = tree_tall * (0.22f + share * 0.7f);
                float width = tree_tall * 0.3f * (1 - (height / tree_tall - 0.15f) / 0.8f * 0.82f) * 0.86f;
                float angle = share * Mathf.PI * 2 * 5.5f;
                var place = tree_spot + new Vector3(Mathf.Cos(angle) * width, height, Mathf.Sin(angle) * width);
                bulbs.add_ball(place, Vector3.one * 0.03f, bulb_colours[bulb % bulb_colours.Length], 5);
            }
            // The star on top: five glowing points round a middle.
            var star_middle = tree_spot + Vector3.up * (tree_tall * 1.04f);
            bulbs.add_ball(star_middle, Vector3.one * 0.07f, new Color(1f, 0.85f, 0.4f), 8);
            for (int point = 0; point < 5; point++)
            {
                float angle = point * Mathf.PI * 2 / 5;
                var outward = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0);
                var turn = Quaternion.FromToRotation(Vector3.up, outward);
                bulbs.add_round_shape(star_middle, turn, new[]
                {
                    new MeshKit.Ring(0, 0.05f, new Color(1f, 0.85f, 0.4f)),
                    new MeshKit.Ring(0.2f, 0, new Color(1f, 0.9f, 0.5f)),
                }, 5, new Vector3(1, 1, 0.45f));
            }
            MeshKit.show("christmas lights", bulbs.build("christmas lights"), lights, parent, false);

            // Baubles hung among the branches, and a few on the outer edge of each tier.
            var baubles = new MeshKit();
            for (int bauble = 0; bauble < 26; bauble++)
            {
                float height = tree_tall * (0.25f + (float)random.NextDouble() * 0.62f);
                float width = tree_tall * 0.3f * (1 - (height / tree_tall - 0.15f) / 0.8f * 0.82f) * 0.92f;
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                var place = tree_spot + new Vector3(Mathf.Cos(angle) * width, height - 0.04f, Mathf.Sin(angle) * width);
                baubles.add_ball(place, Vector3.one * (0.045f + (float)random.NextDouble() * 0.025f), bauble_colours[bauble % bauble_colours.Length], 8);
            }
            MeshKit.show("baubles", baubles.build("baubles"), shiny, parent, true);

            // Presents round the foot of the tree, wrapped and tied with ribbon, snow on their lids.
            var presents = new MeshKit();
            var sizes = new[] { new Vector3(0.42f, 0.3f, 0.35f), new Vector3(0.3f, 0.42f, 0.3f), new Vector3(0.5f, 0.22f, 0.32f), new Vector3(0.26f, 0.26f, 0.26f), new Vector3(0.36f, 0.34f, 0.28f) };
            for (int present = 0; present < sizes.Length; present++)
            {
                float angle = -Mathf.PI * 0.5f + (present - 2) * 0.62f;  // Fanned round the front of the tree.
                var size = sizes[present];
                var foot = tree_spot + new Vector3(Mathf.Cos(angle) * 0.95f, 0, Mathf.Sin(angle) * 0.95f);
                var turn = Quaternion.Euler(0, angle * Mathf.Rad2Deg + 20 * present, 0);
                var middle = foot + Vector3.up * (size.y / 2);
                var ribbon = present % 2 == 0 ? ribbon_gold : ribbon_red;
                presents.add_box(middle, size, paper_colours[present % paper_colours.Length], turn);
                presents.add_box(middle, new Vector3(size.x + 0.01f, size.y + 0.01f, size.z * 0.16f), ribbon, turn);
                presents.add_box(middle, new Vector3(size.x * 0.16f, size.y + 0.012f, size.z + 0.01f), ribbon, turn);
                var top = middle + Vector3.up * (size.y / 2 + 0.02f);
                presents.add_ball(top + turn * new Vector3(-0.04f, 0, 0), new Vector3(0.05f, 0.03f, 0.03f), ribbon, 6);
                presents.add_ball(top + turn * new Vector3(0.04f, 0, 0), new Vector3(0.05f, 0.03f, 0.03f), ribbon, 6);
                presents.add_ball(top + Vector3.up * 0.005f, new Vector3(size.x * 0.4f, 0.015f, size.z * 0.4f), snow, 6, 0.004f, 0.03f, turn);
            }
            MeshKit.show("presents", presents.build("presents"), plain, parent, true);
        }

        // Fairy lights along the cabin's front eave and its porch roof, hanging in gentle loops
        // between hooks, and a holly wreath with a red bow on the door.
        static void build_cabin_lights(Transform parent, SnowLand land, Material lights, Material plain)
        {
            var bulbs = new MeshKit();
            string_of_lights(bulbs, land.on_cabin(-3.1f, 2.75f, -2.4f), land.on_cabin(3.1f, 2.75f, -2.4f), 5, 0.12f);
            string_of_lights(bulbs, land.on_cabin(-1.3f, 2.25f, -3.75f), land.on_cabin(2.5f, 2.25f, -3.75f), 3, 0.08f);
            string_of_lights(bulbs, land.on_cabin(-1.3f, 2.25f, -3.75f), land.on_cabin(-1.3f, 0.3f, -3.55f), 1, 0.02f);
            string_of_lights(bulbs, land.on_cabin(2.5f, 2.25f, -3.75f), land.on_cabin(2.5f, 0.3f, -3.55f), 1, 0.02f);
            MeshKit.show("cabin lights", bulbs.build("cabin lights"), lights, parent, false);

            var wreath = new MeshKit();
            var door = land.on_cabin(1.6f, 1.72f, -2.3f);
            var face = land.on_cabin(0, 0, -1) - land.on_cabin(0, 0, 0);
            var across = Vector3.Cross(Vector3.up, face).normalized;
            for (int leaf = 0; leaf < 16; leaf++)
            {
                float angle = leaf * Mathf.PI * 2 / 16;
                var place = door + across * Mathf.Cos(angle) * 0.24f + Vector3.up * Mathf.Sin(angle) * 0.24f;
                wreath.add_ball(place, Vector3.one * 0.075f, holly, 6, 0.02f, 0.04f);
                if (leaf % 4 == 1)
                {
                    wreath.add_ball(place + face.normalized * 0.06f, Vector3.one * 0.025f, ribbon_red, 4);  // Holly berries.
                }
            }
            wreath.add_ball(door + Vector3.down * 0.24f + face.normalized * 0.06f + across * 0.06f, new Vector3(0.07f, 0.05f, 0.03f), ribbon_red, 6);
            wreath.add_ball(door + Vector3.down * 0.24f + face.normalized * 0.06f - across * 0.06f, new Vector3(0.07f, 0.05f, 0.03f), ribbon_red, 6);
            MeshKit.show("wreath", wreath.build("wreath"), plain, parent, true);
        }

        // A string of bulbs from `from` to `to`, sagging into `loops` gentle curves `sag` metres deep.
        static void string_of_lights(MeshKit bulbs, Vector3 from, Vector3 to, int loops, float sag)
        {
            int count = Mathf.Max(2, Mathf.RoundToInt(Vector3.Distance(from, to) / 0.28f));
            for (int bulb = 0; bulb <= count; bulb++)
            {
                float share = (float)bulb / count;
                float dip = Mathf.Abs(Mathf.Sin(share * Mathf.PI * loops)) * sag;
                var place = Vector3.Lerp(from, to, share) + Vector3.down * dip;
                bulbs.add_ball(place, Vector3.one * 0.035f, bulb_colours[bulb % bulb_colours.Length], 5);
            }
        }
    }
}
