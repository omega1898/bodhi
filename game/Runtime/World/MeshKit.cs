// Purpose: builds 3D shapes in code, so the scenes need no model files. A shape is made of
// triangles between "vertices" (corner points), and each vertex carries its own colour, which the
// Bodhi/Lit shader reads. Many simple pieces (balls, tapered tubes, cones, boxes) go into one
// MeshKit and come out as a single mesh, which a phone draws in one go.
//
// The main tool is the "round shape": a stack of circles (rings) of different sizes, joined
// into a surface, like a shape turned on a lathe. A ball, a limb, a tree's tier of branches, a
// carrot and a coat's skirt are all round shapes with different rings.
using System.Collections.Generic;
using UnityEngine;

namespace Bodhi.Game
{
    public class MeshKit
    {
        readonly List<Vector3> corners = new List<Vector3>();
        readonly List<Color> colours = new List<Color>();
        readonly List<int> triangles = new List<int>();

        // One ring of a round shape: how far along the shape's axis it sits, how wide it is, and
        // its colour.
        public struct Ring
        {
            public float height;
            public float radius;
            public Color colour;

            public Ring(float height, float radius, Color colour)
            {
                this.height = height;
                this.radius = radius;
                this.colour = colour;
            }
        }

        // How many corners the kit holds so far (a phone is happy with tens of thousands).
        public int corner_count => corners.Count;

        // A ball, or a squashed or stretched one ("ellipsoid"), centred at `centre`. `lumps` above
        // 0 makes the surface knobbly (snow, curls), with `lump_size` metres between knobs.
        public void add_ball(Vector3 centre, Vector3 radii, Color colour, int detail, float lumps = 0, float lump_size = 0.1f, Quaternion? turn = null)
        {
            var rings = new List<Ring>();
            int ring_count = Mathf.Max(4, detail);
            for (int index = 0; index <= ring_count; index++)
            {
                float angle = Mathf.PI * index / ring_count;  // 0 at the bottom pole, pi at the top.
                rings.Add(new Ring(-Mathf.Cos(angle), Mathf.Sin(angle), colour));
            }
            add_round_shape(centre, turn ?? Quaternion.identity, rings, detail * 2, radii, lumps, lump_size);
        }

        // A tube that tapers from `radius_from` at `from` to `radius_to` at `to`, with rounded
        // ends: an arm, a leg, a branch, a stick. Two tubes that meet at a joint with the same
        // radius there join seamlessly at any angle, because both ends are half-balls.
        public void add_tube(Vector3 from, Vector3 to, float radius_from, float radius_to, Color colour_from, Color colour_to, int sides = 10, float lumps = 0, float lump_size = 0.1f)
        {
            Vector3 axis = to - from;
            float length = axis.magnitude;
            if (length < 0.0001f)
            {
                add_ball(from, Vector3.one * radius_from, colour_from, sides / 2, lumps, lump_size);
                return;
            }
            var turn = Quaternion.FromToRotation(Vector3.up, axis / length);
            var rings = new List<Ring>();
            const int cap_steps = 4;
            for (int step = 0; step <= cap_steps; step++)  // The rounded bottom end.
            {
                float angle = Mathf.PI / 2 * step / cap_steps;
                rings.Add(new Ring(-Mathf.Cos(angle) * radius_from, Mathf.Sin(angle) * radius_from, colour_from));
            }
            int middle_steps = Mathf.Max(1, Mathf.CeilToInt(length / Mathf.Max(0.02f, (radius_from + radius_to))));
            for (int step = 1; step < middle_steps; step++)  // Rings along the length.
            {
                float share = (float)step / middle_steps;
                rings.Add(new Ring(length * share, Mathf.Lerp(radius_from, radius_to, share), Color.Lerp(colour_from, colour_to, share)));
            }
            for (int step = 0; step <= cap_steps; step++)  // The rounded top end.
            {
                float angle = Mathf.PI / 2 * step / cap_steps;
                rings.Add(new Ring(length + Mathf.Sin(angle) * radius_to, Mathf.Cos(angle) * radius_to, colour_to));
            }
            add_round_shape(from, turn, rings, sides, Vector3.one, lumps, lump_size);
        }

        // A stack of rings joined into a surface, standing on `base_point` and turned by `turn`.
        // `stretch` scales the result (x and z across, y along the axis). Rings of radius 0 close
        // the shape to a point. This is the lathe every other round piece is made with.
        public void add_round_shape(Vector3 base_point, Quaternion turn, IList<Ring> rings, int sides, Vector3 stretch, float lumps = 0, float lump_size = 0.1f)
        {
            sides = Mathf.Max(3, sides);
            int first = corners.Count;
            for (int ring_index = 0; ring_index < rings.Count; ring_index++)
            {
                var ring = rings[ring_index];
                for (int side = 0; side <= sides; side++)  // The last column repeats the first, closing the seam.
                {
                    float angle = 2 * Mathf.PI * (side % sides) / sides;
                    var local = new Vector3(Mathf.Cos(angle) * ring.radius, ring.height, Mathf.Sin(angle) * ring.radius);
                    local = Vector3.Scale(local, stretch);
                    var point = base_point + turn * local;
                    if (lumps > 0 && ring.radius > 0)
                    {
                        var outward = turn * Vector3.Scale(new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)), stretch).normalized;
                        point += outward * knobs(point, lump_size) * lumps;
                    }
                    corners.Add(point);
                    colours.Add(ring.colour);
                }
            }
            int columns = sides + 1;
            for (int ring_index = 0; ring_index < rings.Count - 1; ring_index++)
            {
                for (int side = 0; side < sides; side++)
                {
                    int here = first + ring_index * columns + side;
                    int above = here + columns;
                    triangles.Add(here);
                    triangles.Add(above);
                    triangles.Add(here + 1);
                    triangles.Add(here + 1);
                    triangles.Add(above);
                    triangles.Add(above + 1);
                }
            }
        }

        // A box, turned by `turn` about its centre. Each face gets its own corners, so its edges
        // stay sharp (a cabin wall, a lantern's frame).
        public void add_box(Vector3 centre, Vector3 size, Color colour, Quaternion? turn = null)
        {
            var rotation = turn ?? Quaternion.identity;
            Vector3 half = size / 2;
            // Each face: its outward direction and two directions along it.
            var faces = new[]
            {
                (Vector3.up, Vector3.right, Vector3.forward),
                (Vector3.down, Vector3.forward, Vector3.right),
                (Vector3.right, Vector3.forward, Vector3.up),
                (Vector3.left, Vector3.up, Vector3.forward),
                (Vector3.forward, Vector3.up, Vector3.right),
                (Vector3.back, Vector3.right, Vector3.up),
            };
            foreach (var (outward, across, along) in faces)
            {
                int first = corners.Count;
                Vector3 middle = Vector3.Scale(outward, half);
                Vector3 a = Vector3.Scale(across, half);
                Vector3 b = Vector3.Scale(along, half);
                corners.Add(centre + rotation * (middle - a - b));
                corners.Add(centre + rotation * (middle + a - b));
                corners.Add(centre + rotation * (middle + a + b));
                corners.Add(centre + rotation * (middle - a + b));
                for (int count = 0; count < 4; count++)
                {
                    colours.Add(colour);
                }
                triangles.Add(first);
                triangles.Add(first + 2);
                triangles.Add(first + 1);
                triangles.Add(first);
                triangles.Add(first + 3);
                triangles.Add(first + 2);
            }
        }

        // A flat grid of points whose heights come from `height_at`, coloured by `colour_at`
        // (which gets the point and its surface direction). Ground and mountains are grids.
        public void add_height_grid(float left, float right, float near, float far, int columns, int rows, System.Func<float, float, float> height_at, System.Func<Vector3, Vector3, Color> colour_at)
        {
            int first = corners.Count;
            var points = new Vector3[(columns + 1) * (rows + 1)];
            for (int row = 0; row <= rows; row++)
            {
                for (int column = 0; column <= columns; column++)
                {
                    float x = Mathf.Lerp(left, right, (float)column / columns);
                    float z = Mathf.Lerp(near, far, (float)row / rows);
                    points[row * (columns + 1) + column] = new Vector3(x, height_at(x, z), z);
                }
            }
            for (int row = 0; row <= rows; row++)
            {
                for (int column = 0; column <= columns; column++)
                {
                    var point = points[row * (columns + 1) + column];
                    // The surface direction, from the neighbours on each side.
                    var left_point = points[row * (columns + 1) + Mathf.Max(0, column - 1)];
                    var right_point = points[row * (columns + 1) + Mathf.Min(columns, column + 1)];
                    var near_point = points[Mathf.Max(0, row - 1) * (columns + 1) + column];
                    var far_point = points[Mathf.Min(rows, row + 1) * (columns + 1) + column];
                    var facing = Vector3.Cross(far_point - near_point, right_point - left_point).normalized;
                    corners.Add(point);
                    colours.Add(colour_at(point, facing));
                }
            }
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int here = first + row * (columns + 1) + column;
                    int above = here + columns + 1;
                    triangles.Add(here);
                    triangles.Add(above);
                    triangles.Add(here + 1);
                    triangles.Add(here + 1);
                    triangles.Add(above);
                    triangles.Add(above + 1);
                }
            }
        }

        // Turns everything added so far into one mesh, with smooth surface directions worked out
        // from the triangles (so light falls smoothly across them).
        public Mesh build(string name)
        {
            var mesh = new Mesh { name = name };
            if (corners.Count > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;  // More than 65,535 corners.
            }
            mesh.SetVertices(corners);
            mesh.SetColors(colours);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Makes a GameObject that draws `mesh` with `material`, under `parent`.
        public static GameObject show(string name, Mesh mesh, Material material, Transform parent, bool casts_shadow = true)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.AddComponent<MeshFilter>().sharedMesh = mesh;
            var drawer = holder.AddComponent<MeshRenderer>();
            drawer.sharedMaterial = material;
            drawer.shadowCastingMode = casts_shadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return holder;
        }

        // How far a knobbly surface bulges at a point, from -1 to 1: big knobs `size` metres apart
        // with smaller ones on top, like curls or packed snow.
        static float knobs(Vector3 point, float size)
        {
            size = Mathf.Max(0.005f, size);
            float big = noise(point / size) - 0.5f;
            float small = noise(point / (size * 0.45f) + Vector3.one * 7.3f) - 0.5f;
            return Mathf.Clamp((big * 2f + small * 1.1f) * 1.3f, -1f, 1f);
        }

        // Smooth noise from 0 to 1 at a point in space, from Unity's 2D Perlin noise sampled on
        // three crossing planes and averaged. Good enough for knobbly snow and curls.
        public static float noise(Vector3 point)
        {
            float xy = Mathf.PerlinNoise(point.x + 31.7f, point.y + 11.3f);
            float yz = Mathf.PerlinNoise(point.y + 57.1f, point.z + 3.9f);
            float zx = Mathf.PerlinNoise(point.z + 19.4f, point.x + 71.2f);
            return (xy + yz + zx) / 3f;
        }
    }
}
