// Purpose: the look of a 3D scene: its materials, its lights, the light from the whole sky, the
// fog that fades distant mountains, and the "post-processing" (bloom, colour grading, vignette)
// laid over the finished picture. Values follow docs/unity_art_bible.md: cold blue night, one
// warm light, Neutral tonemapping, and bloom that only the brightest things cross. Every scene
// calls these helpers instead of setting numbers of its own, so the game looks like one world.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Bodhi.Game
{
    public static class WorldLook
    {
        // Finds one of the game's shaders. They live in game/Resources/Shaders, which Unity always
        // puts in the app, so Shader.Find works on the phone too.
        static Shader shader(string name)
        {
            var found = Shader.Find(name);
            if (found == null)
            {
                Debug.LogWarning("Shader " + name + " is missing; using Unity's default instead.");
                found = Shader.Find("Universal Render Pipeline/Unlit");
            }
            return found;
        }

        // The solid material: colours come from the mesh. `rim` is the cold edge light (stronger on
        // the characters, so they read against the night), `sparkle` is for snow, `gloss` for shine.
        public static Material solid(float rim = 0.25f, float sparkle = 0, float gloss = 0, float wrap = 0.35f, float grain = 0.16f)
        {
            prepare_grain();
            var material = new Material(shader("Bodhi/Lit"));
            material.SetFloat("_Grain", grain);
            material.SetColor("_RimColor", PaletteColours.moon_rim);
            material.SetFloat("_RimStrength", rim);
            material.SetFloat("_Sparkle", sparkle);
            material.SetFloat("_Gloss", gloss);
            material.SetFloat("_Wrap", wrap);
            return material;
        }

        // The painted-grain picture every solid material shares: soft blotchy noise, 128 pixels
        // square, that repeats seamlessly (each layer's grid wraps round at the edges). Made once.
        static Texture2D grain_picture;
        static void prepare_grain()
        {
            if (grain_picture != null)
            {
                return;
            }
            const int size = 128;
            var random = new System.Random(1898);
            var pixels = new Color32[size * size];
            var total = new float[size * size];
            float weight = 1f, weights = 0f;
            for (int cells = 4; cells <= 32; cells *= 2)  // Big soft blotches, then finer ones.
            {
                var corners = new float[cells * cells];
                for (int index = 0; index < corners.Length; index++)
                {
                    corners[index] = (float)random.NextDouble();
                }
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float across = (float)x / size * cells, down = (float)y / size * cells;
                        int left = (int)across, top = (int)down;
                        float sx = Mathf.SmoothStep(0, 1, across - left), sy = Mathf.SmoothStep(0, 1, down - top);
                        float a = corners[(top % cells) * cells + left % cells], b = corners[(top % cells) * cells + (left + 1) % cells];
                        float c = corners[((top + 1) % cells) * cells + left % cells], d = corners[((top + 1) % cells) * cells + (left + 1) % cells];
                        total[y * size + x] += Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy) * weight;
                    }
                }
                weights += weight;
                weight *= 0.55f;
            }
            for (int index = 0; index < pixels.Length; index++)
            {
                byte value = (byte)Mathf.Clamp(total[index] / weights * 255f, 0, 255);
                pixels[index] = new Color32(value, value, value, 255);
            }
            grain_picture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "painted grain", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            grain_picture.SetPixels32(pixels);
            grain_picture.Apply(true, true);
            Shader.SetGlobalTexture("_BodhiGrain", grain_picture);
        }

        // A solid material that also glows with its own light (a lantern's glass, a lit window).
        // `strength` above 1 crosses the bloom threshold, so it softly glows on screen.
        public static Material glowing(Color colour, float strength)
        {
            var material = solid(0);
            material.SetColor("_EmissionColor", colour * strength);
            return material;
        }

        // Fairy lights: bulbs glowing in their own colours (from the mesh), each twinkling.
        public static Material fairy_lights(float strength)
        {
            var material = glowing(Color.white, strength);
            material.SetFloat("_Twinkle", 1f);
            return material;
        }

        // Soft round particles. `adds_light` true makes them glow (halos); false makes them cover
        // what's behind like snowflakes do.
        public static Material particles(bool adds_light)
        {
            var material = new Material(shader("Bodhi/Glow"));
            material.SetFloat("_SourceBlend", adds_light ? (float)BlendMode.One : (float)BlendMode.SrcAlpha);
            material.SetFloat("_DestinationBlend", adds_light ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        // The light from the whole sky, as three colours: overhead, at the horizon and from the
        // ground. This keeps shadowed sides blue and alive instead of dead grey. The shader reads
        // it as "spherical harmonics" (a compact way to store light from every direction), so
        // that is filled in too.
        public static void set_sky_light(Color overhead, Color horizon, Color ground)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = overhead;
            RenderSettings.ambientEquatorColor = horizon;
            RenderSettings.ambientGroundColor = ground;
            var light = new SphericalHarmonicsL2();
            light.AddAmbientLight(horizon);
            light.AddDirectionalLight(Vector3.up, (overhead - horizon) * 0.9f, 1f);
            light.AddDirectionalLight(Vector3.down, (ground - horizon) * 0.9f, 1f);
            RenderSettings.ambientProbe = light;
        }

        // Fog that thickens with distance, so far mountains fade toward the sky's horizon colour,
        // the way air does. `density` is how quickly (bigger is foggier).
        public static void set_fog(Color colour, float density)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = colour;
            RenderSettings.fogDensity = density;
        }

        // Turns the fog off again (the chapters don't use it yet).
        public static void clear_fog()
        {
            RenderSettings.fog = false;
        }

        // A light shining one way from far off, like the moon. Only one light casts shadows, the
        // main one, because shadows cost the most on a phone.
        public static Light far_light(string name, Transform parent, Color colour, float intensity, Vector3 shine_toward, bool casts_shadows)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.rotation = Quaternion.LookRotation(shine_toward.normalized);
            var light = holder.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = colour;
            light.intensity = intensity;
            light.shadows = casts_shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = 0.8f;
            light.shadowBias = 0.04f;
            light.shadowNormalBias = 0.3f;
            return light;
        }

        // A light glowing out from one point, like a lantern or a window, reaching `range` metres.
        public static Light lamp(string name, Transform parent, Vector3 position, Color colour, float intensity, float range)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = position;
            var light = holder.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        // A perspective camera set up for the 3D scenes: post-processing on (a camera made in code
        // has it off, and every effect silently does nothing until it's on), multisampled edges,
        // and a far view so the mountains and aurora show.
        public static Camera make_camera(string name, Transform parent, float field_of_view)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            var camera = holder.AddComponent<Camera>();
            camera.fieldOfView = field_of_view;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 3000f;
            camera.clearFlags = CameraClearFlags.Skybox;
            var urp = camera.GetUniversalAdditionalCameraData();
            urp.renderPostProcessing = true;
            urp.antialiasing = AntialiasingMode.None;  // The pipeline's 4x multisampling smooths edges.
            camera.allowMSAA = true;
            urp.volumeLayerMask = 1;  // Layer 0, "Default", where the scene's Volume lives.
            return camera;
        }

        // The post-processing laid over the picture, with the art bible's values for a phone:
        //   bloom: only things brighter than `bloom_threshold` glow (the aurora, the lantern, stars);
        //   Neutral tonemapping: keeps colours true (ACES pushes them orange);
        //   colour adjustments: a little contrast;
        //   split toning: shadows toward night blue, highlights toward warm amber;
        //   vignette: a soft darkening at the corners that frames the picture.
        // Film grain, depth of field and motion blur are left off: costly on phones, and they
        // blur the motion. Returns the Volume, which a scene can fade in and out.
        public static Volume make_post_processing(string name, Transform parent, float bloom_threshold, float bloom_intensity)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.layer = 0;
            var volume = holder.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = bloom_threshold;
            bloom.intensity.value = bloom_intensity;
            bloom.scatter.value = 0.72f;
            bloom.tint.value = new Color(0.9f, 1f, 0.95f);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.value = TonemappingMode.Neutral;

            var colours = profile.Add<ColorAdjustments>(true);
            colours.contrast.value = 10f;
            colours.saturation.value = 4f;
            colours.postExposure.value = -0.05f;

            var toning = profile.Add<SplitToning>(true);
            toning.shadows.value = PaletteColours.night;
            toning.highlights.value = PaletteColours.warm_amber;
            toning.balance.value = -20f;

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.26f;
            vignette.smoothness.value = 0.45f;
            vignette.color.value = PaletteColours.shadow;
            return volume;
        }

        // Checks the render pipeline is the one the 3D scenes are made for. Its settings (soft
        // shadows, how far shadows reach, HDR for bloom) are set once by the project setup
        // script (unity/Assets/Editor/ProjectSetup.cs), never changed while the game runs, because
        // changing them here would quietly rewrite Unity's settings files.
        public static void check_pipeline()
        {
            if (UniversalRenderPipeline.asset == null)
            {
                Debug.LogWarning("URP isn't the active render pipeline; the 3D scenes will look flat.");
            }
        }
    }

    // The art bible's palette as Unity colours, read from look/Runtime/Palette.cs so there's one
    // source for every colour.
    public static class PaletteColours
    {
        public static Color night => from(Bodhi.Look.Palette.night);
        public static Color rain => from(Bodhi.Look.Palette.rain);
        public static Color far_mist => from(Bodhi.Look.Palette.far_mist);
        public static Color shadow => from(Bodhi.Look.Palette.shadow);
        public static Color her_coat => from(Bodhi.Look.Palette.her_coat);
        public static Color her_scarf => from(Bodhi.Look.Palette.her_scarf);
        public static Color her_hair => from(Bodhi.Look.Palette.her_hair);
        public static Color bodhi_coat => from(Bodhi.Look.Palette.bodhi_coat);
        public static Color bodhi_shade => from(Bodhi.Look.Palette.bodhi_shade);
        public static Color moon_rim => from(Bodhi.Look.Palette.moon_rim);
        public static Color warm_amber => from(Bodhi.Look.Palette.warm_amber);

        // Turns one palette entry's hex code into a Unity colour (white if the code were broken).
        static Color from(Bodhi.Look.PaletteEntry entry)
        {
            return Bodhi.Look.PaletteColour.try_parse(entry.hex, out var colour)
                ? new Color((float)colour.red, (float)colour.green, (float)colour.blue)
                : Color.white;
        }
    }
}
