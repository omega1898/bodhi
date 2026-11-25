// Purpose: builds the game from one command, so a build never depends on remembering which
// boxes to tick in Unity's Build Profiles window. Two builds:
//   - iOS: writes an Xcode project into unity/ios_build/. Open it in Xcode, pick your Team,
//     and press Run to put the game on an iPhone or iPad.
//   - Mac: a quick test copy of the game that runs on this Mac, to check it starts.
//
// From Unity's menu (Bodhi, Build ...), or from Terminal without opening Unity:
//   Unity -batchmode -quit -projectPath unity -executeMethod Bodhi.Setup.BuildApp.build_ios
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Bodhi.Setup
{
    public static class BuildApp
    {
        public const string ios_folder = "ios_build";
        public const string mac_app = "Builds/UntilWeFindHome.app";

        // Writes the Xcode project for iPhone and iPad (IosDeviceSettings in the game package
        // sets both devices, landscape and full screen just before this runs).
        [MenuItem("Bodhi/Build iOS (Xcode project)")]
        public static void build_ios()
        {
            build(BuildTarget.iOS, ios_folder);
        }

        // A test copy that runs on this Mac.
        [MenuItem("Bodhi/Build Mac test copy")]
        public static void build_mac()
        {
            build(BuildTarget.StandaloneOSX, mac_app);
        }

        // Builds the game's one scene for `target` into `output`, and fails loudly if it can't,
        // so a Terminal build stops with an error instead of quietly writing nothing.
        static void build(BuildTarget target, string output)
        {
            ProjectSetup.set_up();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.scene_path },
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Build failed for " + target + ": " + report.summary.result);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }
            Debug.Log("Built " + target + " into " + output);
        }
    }
}
