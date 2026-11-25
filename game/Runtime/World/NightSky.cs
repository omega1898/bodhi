// Purpose: the sky over the 3D title scene: the starry night (drawn by the Bodhi/Night Sky shader
// as Unity's "skybox", the backdrop behind everything) and the green aurora, which hangs in front
// of it as long curved ribbons of mesh far beyond the mountains (drawn by Bodhi/Aurora). The whole
// sky fades up from black at the start of the opening.
using UnityEngine;

namespace Bodhi.Game
{
    public class NightSky
    {
        // Each curtain of the aurora: how far away its middle hangs, how high its lower edge is,
        // how tall it is, how wide it sweeps, its brightness, and a seed so each moves differently.
        struct Curtain
        {
            public float distance, base_height, height, width, brightness, seed, bend;
        }

        static readonly Curtain[] curtains =
        {
            new Curtain { distance = 1100, base_height = 150, height = 330, width = 3000, brightness = 1.5f, seed = 1, bend = 260 },
            new Curtain { distance = 1600, base_height = 230, height = 420, width = 3800, brightness = 1.0f, seed = 2, bend = -340 },
            new Curtain { distance = 850, base_height = 190, height = 260, width = 2000, brightness = 0.7f, seed = 3, bend = 180 },
        };

        readonly Material sky_material;
        readonly Material[] curtain_materials = new Material[curtains.Length];

        // The way the moon's light shines (from the moon toward the scene).
        public readonly Vector3 moonlight_direction;

        // Builds the sky and the aurora's curtains under `parent`. `moon_direction` points from
        // the scene toward the moon in the sky.
        public NightSky(Transform parent, Vector3 moon_direction)
        {
            moonlight_direction = -moon_direction.normalized;
            sky_material = new Material(Shader.Find("Bodhi/Night Sky"));
            sky_material.SetVector("_MoonDirection", moon_direction.normalized);
            var holder = new GameObject("Aurora");
            holder.transform.SetParent(parent, false);
            var aurora_shader = Shader.Find("Bodhi/Aurora");
            for (int index = 0; index < curtains.Length; index++)
            {
                var curtain = curtains[index];
                var material = new Material(aurora_shader);
                material.SetFloat("_Seed", curtain.seed);
                material.SetFloat("_Brightness", curtain.brightness);
                curtain_materials[index] = material;
                var mesh = make_curtain(curtain);
                var drawn = MeshKit.show("curtain " + (index + 1), mesh, material, holder.transform, false);
                drawn.GetComponent<MeshRenderer>().receiveShadows = false;
            }
        }

        // Puts this sky behind the scene (skyboxes are a setting of the whole scene in Unity).
        public void use()
        {
            RenderSettings.skybox = sky_material;
        }

        // How much of the sky shows: 0 black, 1 full. The aurora fades with it.
        public void set_fade(float fade)
        {
            sky_material.SetFloat("_Fade", fade);
            foreach (var material in curtain_materials)
            {
                material.SetFloat("_Fade", fade);
            }
        }

        // One curtain as a ribbon: a strip of quads along a gentle curve across the sky, every
        // quad standing upright. Each corner remembers how far along the ribbon it is and whether
        // it's at the bottom or top, which the shader uses to paint rays and fade the top. The
        // corners' "normal" is set sideways across the ribbon: the way the shader sways it.
        static Mesh make_curtain(Curtain curtain)
        {
            const int steps = 220;
            var corners = new Vector3[(steps + 1) * 2];
            var sideways = new Vector3[corners.Length];
            var along_and_up = new Vector2[corners.Length];
            var extra = new Vector4[corners.Length];
            var triangles = new int[steps * 6];
            var path = new Vector3[steps + 1];
            for (int step = 0; step <= steps; step++)
            {
                float share = (float)step / steps - 0.5f;  // -0.5 at one end, 0.5 at the other.
                float x = share * curtain.width;
                // A bow toward or away from the camera, plus deep S-bends, so the ribbon snakes
                // across the sky and folds back on itself instead of standing like a flat wall.
                float z = curtain.distance + curtain.bend * (1 - 4 * share * share)
                        + Mathf.Sin(share * 9f + curtain.seed) * 340 + Mathf.Sin(share * 17f + curtain.seed * 3) * 110;
                float lower_edge = curtain.base_height + Mathf.Sin(share * 6.2f + curtain.seed * 2) * 70 + Mathf.Sin(share * 15f) * 25;
                path[step] = new Vector3(x, lower_edge, z);
            }
            float length = 0;
            for (int step = 0; step <= steps; step++)
            {
                if (step > 0)
                {
                    length += Vector3.Distance(path[step - 1], path[step]);
                }
                var ahead = path[Mathf.Min(steps, step + 1)] - path[Mathf.Max(0, step - 1)];
                var across = Vector3.Cross(Vector3.up, ahead).normalized;
                int bottom = step * 2, top = bottom + 1;
                corners[bottom] = path[step];
                corners[top] = path[step] + Vector3.up * curtain.height;
                sideways[bottom] = sideways[top] = across;
                along_and_up[bottom] = new Vector2(length, 0);
                along_and_up[top] = new Vector2(length, 1);
            }
            for (int index = 0; index < extra.Length; index++)
            {
                extra[index] = new Vector4(length, 0, 0, 0);
            }
            for (int step = 0; step < steps; step++)
            {
                int bottom = step * 2;
                int at = step * 6;
                triangles[at] = bottom;
                triangles[at + 1] = bottom + 1;
                triangles[at + 2] = bottom + 2;
                triangles[at + 3] = bottom + 2;
                triangles[at + 4] = bottom + 1;
                triangles[at + 5] = bottom + 3;
            }
            var mesh = new Mesh { name = "aurora curtain" };
            mesh.vertices = corners;
            mesh.normals = sideways;
            mesh.SetUVs(0, along_and_up);
            mesh.SetUVs(1, extra);
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            bounds.Expand(80);  // The shader sways it sideways; keep it from being culled early.
            mesh.bounds = bounds;
            return mesh;
        }
    }
}
