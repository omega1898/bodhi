// Purpose: lets the iPhone app run at 120 frames a second on ProMotion screens. iOS holds every
// app on an iPhone to 60 unless the app's Info.plist sets CADisableMinimumFrameDurationOnPhone,
// so this adds that setting to the Xcode project Unity writes, if it isn't there already.
// Runs inside the Unity Editor only, after an iOS build.
#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Bodhi.Game
{
    public static class HighFrameRateBuild
    {
        public const string setting_name = "CADisableMinimumFrameDurationOnPhone";

        // Unity calls this after it writes the Xcode project.
        [PostProcessBuild]
        public static void after_build(BuildTarget target, string project_folder)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }
            string plist_path = Path.Combine(project_folder, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plist_path);
            plist.root.SetBoolean(setting_name, true);
            plist.WriteToFile(plist_path);
        }
    }
}
#endif
