// Purpose: tells Unity how to bring in the 3D models made by the Blender scripts in art/blender/
// (game/Resources/Models). Unity's defaults would make materials the game doesn't use, squash
// the meshes, and could fold the bones away out of reach; the game needs every bone as a
// Transform it can turn, and paints the models with its own material (Bodhi/Lit). This runs
// inside the Unity Editor only, whenever one of those models is imported.
using UnityEditor;

namespace Bodhi.Game
{
    public class ModelImport : AssetPostprocessor
    {
        // Unity calls this before it imports a model.
        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Models/"))
            {
                return;
            }
            var importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;  // The game sets its own.
            importer.animationType = ModelImporterAnimationType.Generic;           // Bones as plain Transforms.
            importer.importAnimation = false;                                      // The game moves the bones itself.
            importer.optimizeGameObjects = false;                                  // Keep every bone reachable.
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;          // Keep the curls crisp.
            importer.importNormals = ModelImporterNormals.Import;
            importer.globalScale = 1f;
        }
    }
}
