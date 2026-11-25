// Purpose: tells Unity how to bring in the animation pictures (game/Resources/Frames) so they look
// sharp and stay small on an iPhone. Unity's defaults would spoil them: it would stretch each
// picture to a power-of-two size (moving every drawing off the box FrameSheets.Data.cs says it's
// in), make shrunken copies the game never uses, and squeeze them hard enough to smear the fur.
// This runs inside the Unity Editor only, whenever one of those pictures is imported.
using UnityEditor;
using UnityEngine;

namespace Bodhi.Game
{
    public class FrameImport : AssetPostprocessor
    {
        // The iPhone's picture squeeze: ASTC (the format every iPhone since 2014 decodes in its
        // graphics chip) in 4 by 4 blocks, the sharpest setting: a quarter of the memory of the
        // raw picture, with no visible change at the size the game shows them.
        public const TextureImporterFormat iphone_format = TextureImporterFormat.ASTC_4x4;

        // Unity calls this before it imports a picture.
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Frames/"))
            {
                return;
            }
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;    // Keep the exact size.
            importer.mipmapEnabled = false;                        // No shrunken copies.
            importer.alphaIsTransparency = true;                   // See-through edges stay clean.
            importer.wrapMode = TextureWrapMode.Clamp;             // Never borrow pixels from the far edge.
            importer.filterMode = FilterMode.Bilinear;             // Smooth between pixels as it moves.
            importer.isReadable = false;                           // Only the graphics chip needs them.
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            var iphone = importer.GetPlatformTextureSettings("iPhone");
            iphone.overridden = true;
            iphone.maxTextureSize = 2048;
            iphone.format = iphone_format;
            importer.SetPlatformTextureSettings(iphone);
        }
    }
}
