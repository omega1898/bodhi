// Purpose: sets the Unity project up the same way every time: the game's one scene, the app's
// name, ID and icon, and the oldest iOS it supports. Doing this in code instead of by clicking
// through Unity's settings windows means the choices are written down, can be reviewed in a
// pull request, and can be applied again in one step if anything gets changed by accident.
//
// Run it from Unity's menu (Bodhi, Set Up Project), or from Terminal without opening Unity:
//   Unity -batchmode -quit -projectPath unity -executeMethod Bodhi.Setup.ProjectSetup.set_up
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bodhi.Setup
{
    public static class ProjectSetup
    {
        public const string scene_path = "Assets/Scenes/Game.unity";
        public const string icon_path = "Assets/AppIcon/app_icon_1024.png";
        public const string product_name = "Until We Find Home";
        public const string company_name = "omega1898";
        // The app's permanent ID on the App Store. It can still be changed until the first upload
        // to App Store Connect; after that it can never change.
        public const string bundle_id = "com.omega1898.untilwefindhome";
        public const string oldest_ios = "16.0";

        // Unity's menu runs this, and so does the Terminal command at the top of the file.
        [MenuItem("Bodhi/Set Up Project")]
        public static void set_up()
        {
            make_game_scene();
            set_app_details();
            set_app_icon();
            AssetDatabase.SaveAssets();
            Debug.Log("Bodhi project set up: scene " + scene_path + ", app " + bundle_id);
        }

        // The whole game runs in one scene holding one object with ChapterDirector on it.
        // ChapterDirector makes the camera, the text and buttons and every chapter by itself.
        static void make_game_scene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var director = new GameObject("Game");
            director.AddComponent<Bodhi.Game.ChapterDirector>();
            Directory.CreateDirectory(Path.GetDirectoryName(scene_path));
            EditorSceneManager.SaveScene(scene, scene_path);

            // The scene the app opens when it starts. It's the only one.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene_path, true) };
        }

        // The name under the icon on the home screen, the App Store ID and the oldest iOS.
        static void set_app_details()
        {
            PlayerSettings.productName = product_name;
            PlayerSettings.companyName = company_name;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, bundle_id);
            PlayerSettings.iOS.targetOSVersionString = oldest_ios;
            PlayerSettings.bundleVersion = "1.0";      // The version players see on the App Store.
            PlayerSettings.iOS.buildNumber = "1";      // Raise by 1 for every upload to App Store Connect.
        }

        // The app icon: one 1024 x 1024 picture, copied from art/app_icon/. Unity makes every
        // smaller size Apple needs from it.
        static void set_app_icon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(icon_path);
            if (icon == null)
            {
                Debug.LogWarning("No app icon at " + icon_path + "; copy art/app_icon/app_icon_1024.png there.");
                return;
            }
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }
    }
}
