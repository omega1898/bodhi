// Purpose: sets the Unity project up the same way every time: the game's one scene, the app's
// name, ID and icon, the oldest iOS it supports, and the graphics settings the 3D scenes need. Doing this in code instead of by clicking
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
            set_graphics();
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

        // The render pipeline's settings for the 3D scenes, on every quality level (the iPhone uses
        // "Mobile"; the Mac test copy uses "PC"): soft shadow edges; a sharper shadow picture,
        // reaching 30 metres (the play area, so the shadows stay crisp); HDR, which bloom needs so
        // only truly bright things glow; and 4x multisampling, which smooths jagged edges and is
        // cheap on iPhone and iPad graphics chips. Set through Unity's own serialized fields,
        // because some have no public setter.
        static void set_graphics()
        {
            for (int level = 0; level < QualitySettings.count; level++)
            {
                var pipeline = QualitySettings.GetRenderPipelineAssetAt(level);
                if (pipeline == null)
                {
                    continue;
                }
                var settings = new SerializedObject(pipeline);
                set_number(settings, "m_SoftShadowsSupported", 1);
                set_number(settings, "m_MainLightShadowmapResolution", 2048);
                set_number(settings, "m_ShadowDistance", 30);
                set_number(settings, "m_SupportsHDR", 1);
                set_number(settings, "m_MSAA", 4);
                settings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pipeline);
            }

            // Keep every post-processing effect in the app. URP normally leaves out any effect it
            // can't find in a saved Volume profile, but the 3D scenes make their profiles in code
            // (WorldLook), so without this the built app silently loses its bloom (seen in the
            // Mac test copy's log: "Shader 'Hidden/Universal Render Pipeline/Bloom' ... stripped").
            if (UnityEngine.Rendering.GraphicsSettings.TryGetRenderPipelineSettings<UnityEngine.Rendering.Universal.URPShaderStrippingSetting>(out var stripping))
            {
                stripping.stripUnusedPostProcessingVariants = false;
            }
            else
            {
                Debug.LogWarning("Couldn't find URP's shader stripping settings; check bloom still works in a build.");
            }
        }

        // Sets one serialized number (whole or decimal), or warns if this Unity version names it differently.
        static void set_number(SerializedObject settings, string name, float value)
        {
            var field = settings.FindProperty(name);
            if (field == null)
            {
                Debug.LogWarning("Render pipeline setting " + name + " not found; set it by hand in the URP asset.");
                return;
            }
            if (field.propertyType == SerializedPropertyType.Float)
            {
                field.floatValue = value;
            }
            else if (field.propertyType == SerializedPropertyType.Boolean)
            {
                field.boolValue = value != 0;
            }
            else
            {
                field.intValue = (int)value;
            }
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
