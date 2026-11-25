// Purpose: takes still pictures of the 3D title scene without pressing Play, so its look can be
// checked from Terminal (and by Claude Code sessions, which can read pictures but can't watch the
// Unity window). It builds the title world in an empty scene, plays it forward to each moment
// asked for, and saves what the camera sees as a PNG. Nothing in the project is changed.
//
// From Terminal (Unity must not already have the project open):
//   Unity -batchmode -projectPath unity -executeMethod Bodhi.Setup.TitleSnapshot.capture
//         -snapshotFolder /some/folder -snapshotTimes 4,12,40 [-snapshotWidth 1600 -snapshotHeight 740]
//         [-snapshotCamera x,y,z,look_x,look_y,look_z,field_of_view]   (a closer camera, for detail)
//         [-snapshotFollow em|bodhi -snapshotAngle 60]   (a close-up that follows one of them)
// Leave out -nographics: the pictures need the graphics chip.
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Bodhi.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Bodhi.Setup
{
    public static class TitleSnapshot
    {
        // Unity's menu runs this (Bodhi, Title snapshots), and so does the Terminal command above.
        [MenuItem("Bodhi/Title snapshots")]
        public static void capture()
        {
            string folder = argument("-snapshotFolder", Path.Combine(Directory.GetCurrentDirectory(), "Snapshots"));
            var times = argument("-snapshotTimes", "3,7,12,20,40").Split(',').Select(text => double.Parse(text, CultureInfo.InvariantCulture)).OrderBy(time => time).ToArray();
            int width = int.Parse(argument("-snapshotWidth", "1600"));
            int height = int.Parse(argument("-snapshotHeight", "740"));
            string camera_text = argument("-snapshotCamera", "");
            string follow = argument("-snapshotFollow", "");
            float angle = float.Parse(argument("-snapshotAngle", "70"), CultureInfo.InvariantCulture);
            Directory.CreateDirectory(folder);

            int quality_before = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(0, true);  // "Mobile": the settings the iPhone uses.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var holder = new GameObject("snapshot");
            var world = new TitleWorld(holder.transform, null);
            var opening = new OpeningTitle();
            world.show(true);

            const double step = 1.0 / 30.0;
            double time = 0;
            foreach (double wanted in times)
            {
                while (time < wanted)
                {
                    time = Math.Min(wanted, time + step);
                    world.update(time, opening.frame_at(time));
                }
                world.settle_snow(12f);
                if (camera_text != "")
                {
                    aim(world.view, camera_text);
                }
                if (follow != "")
                {
                    close_up(world.view, follow == "em" ? world.em_body : world.bodhi_body, follow == "em" ? 0.9f : 0.4f, angle);
                }
                string path = Path.Combine(folder, (follow == "" ? "title_" : follow + "_") + wanted.ToString("00.000", CultureInfo.InvariantCulture) + "s.png");
                render(world.view, width, height, path);
                Debug.Log("Saved title snapshot " + path);
            }
            UnityEngine.Object.DestroyImmediate(holder);
            QualitySettings.SetQualityLevel(quality_before, true);  // Leave the project as it was.
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        // Points the camera from a list of seven numbers: where it is, where it looks, its zoom.
        static void aim(Camera camera, string numbers)
        {
            var values = numbers.Split(',').Select(text => float.Parse(text, CultureInfo.InvariantCulture)).ToArray();
            var parent = camera.transform.parent;
            var position = new Vector3(values[0], values[1], values[2]);
            var look = new Vector3(values[3], values[4], values[5]);
            camera.transform.position = parent.TransformPoint(position);
            camera.transform.rotation = Quaternion.LookRotation(parent.TransformPoint(look) - camera.transform.position);
            camera.fieldOfView = values[6];
        }

        // Points the camera at one character from a few metres away, `angle` degrees round from
        // straight in front of them, aiming at `height` metres up their body.
        static void close_up(Camera camera, Transform who, float height, float angle)
        {
            var target = who.position + Vector3.up * height;
            var from = who.rotation * Quaternion.Euler(0, angle, 0) * Vector3.forward;
            camera.transform.position = target + from * 3.2f + Vector3.up * 0.25f;
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position);
            camera.fieldOfView = 32;
        }

        // Renders what `camera` sees into a picture `width` by `height` and saves it as a PNG.
        static void render(Camera camera, int width, int height, string path)
        {
            var target = new RenderTexture(width, height, 24, GraphicsFormat.R8G8B8A8_SRGB);
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            else
            {
                camera.targetTexture = target;
                camera.Render();
                camera.targetTexture = null;
            }
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var picture = new Texture2D(width, height, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            picture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, picture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(picture);
            target.Release();
        }

        // The value after `name` on the command line, or `otherwise` if it isn't there.
        static string argument(string name, string otherwise)
        {
            var arguments = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(arguments, name);
            return at >= 0 && at + 1 < arguments.Length ? arguments[at + 1] : otherwise;
        }
    }
}
