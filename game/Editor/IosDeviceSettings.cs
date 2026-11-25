// Purpose: makes the iOS app run on both iPhone and iPad, always in landscape, filling the whole
// screen. Unity keeps these in its Player Settings window, which is easy to change by accident
// and hard to review; setting them here, in code, means every build gets the same answer and
// the choice is written down where anyone can read it.
// Runs inside the Unity Editor only, just before an iOS build starts.
#if UNITY_IOS
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Bodhi.Game
{
    public class IosDeviceSettings : IPreprocessBuildWithReport
    {
        // Unity runs build steps in order of this number, smallest first. Zero is fine: nothing
        // else in the project depends on running before or after this.
        public int callbackOrder => 0;

        // Unity calls this before it builds. It sets the devices, the screen direction and full
        // screen, so the Xcode project Unity writes already has the right settings.
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            // iPhone and iPad. The camera (CameraRig) works out the view's width from the
            // screen's shape, so the squarer iPad screen simply shows a little less to each side.
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;

            // Landscape only, either way up: the game is drawn for a wide screen.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Full screen on iPad too. Without this, Apple requires an iPad app to also work in
            // Split View (sharing the screen with another app) and in portrait, which this game
            // doesn't support.
            PlayerSettings.iOS.requiresFullScreen = true;
        }
    }
}
#endif
