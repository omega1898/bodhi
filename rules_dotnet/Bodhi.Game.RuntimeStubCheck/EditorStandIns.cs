// Purpose: stand-ins for the few Unity Editor types the Editor scripts (game/Editor) use, so they
// can be compiled in the cloud like the scene scripts (see UnityStandIns.cs). Same names and
// shapes as Unity's real ones; they do nothing and are never shipped.
#pragma warning disable CS1591
using System;

namespace UnityEngine
{
    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }
}

namespace UnityEditor
{
    public enum BuildTarget { StandaloneOSX, iOS, Android }

    public enum TextureImporterType { Default, NormalMap, GUI, Sprite }

    public enum TextureImporterNPOTScale { None, ToNearest, ToLarger, ToSmaller }

    public enum TextureImporterCompression { Uncompressed, Compressed, CompressedHQ, CompressedLQ }

    public enum TextureImporterFormat { Automatic, RGBA32, ASTC_4x4, ASTC_5x5, ASTC_6x6, ASTC_8x8 }

    public class TextureImporterPlatformSettings
    {
        public string name { get; set; }
        public bool overridden { get; set; }
        public int maxTextureSize { get; set; }
        public TextureImporterFormat format { get; set; }
    }

    public class AssetImporter : UnityEngine.Object { }

    public class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType { get; set; }
        public TextureImporterNPOTScale npotScale { get; set; }
        public bool mipmapEnabled { get; set; }
        public bool alphaIsTransparency { get; set; }
        public UnityEngine.TextureWrapMode wrapMode { get; set; }
        public UnityEngine.FilterMode filterMode { get; set; }
        public bool isReadable { get; set; }
        public int maxTextureSize { get; set; }
        public TextureImporterCompression textureCompression { get; set; }
        public TextureImporterPlatformSettings GetPlatformTextureSettings(string platform) => new TextureImporterPlatformSettings();
        public void SetPlatformTextureSettings(TextureImporterPlatformSettings settings) { }
    }

    public enum ModelImporterMaterialImportMode { None, ImportStandard, ImportViaMaterialDescription }
    public enum ModelImporterAnimationType { None, Legacy, Generic, Human }
    public enum ModelImporterMeshCompression { Off, Low, Medium, High }
    public enum ModelImporterNormals { Import, Calculate, None }

    // Stand-in for the importer that brings in 3D models (game/Editor/ModelImport.cs).
    public class ModelImporter : AssetImporter
    {
        public ModelImporterMaterialImportMode materialImportMode { get; set; }
        public ModelImporterAnimationType animationType { get; set; }
        public bool importAnimation { get; set; }
        public bool optimizeGameObjects { get; set; }
        public bool importBlendShapes { get; set; }
        public bool importCameras { get; set; }
        public bool importLights { get; set; }
        public bool isReadable { get; set; }
        public ModelImporterMeshCompression meshCompression { get; set; }
        public ModelImporterNormals importNormals { get; set; }
        public float globalScale { get; set; }
    }

    public class AssetPostprocessor
    {
        public string assetPath => "";
        public AssetImporter assetImporter => null;
    }

    // For IosDeviceSettings: which devices and screen directions the iOS app allows.
    public enum iOSTargetDevice { iPhoneOnly, iPadOnly, iPhoneAndiPad }

    public enum UIOrientation { Portrait, PortraitUpsideDown, LandscapeRight, LandscapeLeft, AutoRotation }

    public static class PlayerSettings
    {
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static bool allowedAutorotateToPortrait { get; set; }
        public static bool allowedAutorotateToPortraitUpsideDown { get; set; }
        public static bool allowedAutorotateToLandscapeLeft { get; set; }
        public static bool allowedAutorotateToLandscapeRight { get; set; }

        public static class iOS
        {
            public static iOSTargetDevice targetDevice { get; set; }
            public static bool requiresFullScreen { get; set; }
        }
    }
}

namespace UnityEditor.Build
{
    public interface IPreprocessBuildWithReport
    {
        int callbackOrder { get; }
        void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report);
    }
}

namespace UnityEditor.Build.Reporting
{
    public struct BuildSummary
    {
        public BuildTarget platform { get; set; }
    }

    public class BuildReport
    {
        public BuildSummary summary { get; set; }
    }
}

namespace UnityEditor.Callbacks
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PostProcessBuildAttribute : Attribute { }
}

namespace UnityEditor.iOS.Xcode
{
    public class PlistElementDict
    {
        public void SetBoolean(string key, bool value) { }
    }

    public class PlistDocument
    {
        public PlistElementDict root { get; } = new PlistElementDict();
        public void ReadFromFile(string path) { }
        public void WriteToFile(string path) { }
    }
}
