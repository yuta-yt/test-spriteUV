using UnityEngine;
using UnityEditor;
using System.Collections;

namespace SpriteUVTest.Editor
{

public class UVTextureImportManager : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        TextureImporter importer = assetImporter as TextureImporter;

        // Force the import settings of some textures 
        // to remove the need to modify manual import settings
        // when converting a sequence.
        if (importer.assetPath.Contains("Assets/Resources/UV"))
        {
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            
            importer.SetPlatformTextureSettings (new TextureImporterPlatformSettings
            {
                overridden = true,
                name = "Standalone",
                maxTextureSize = 4096,
                format = TextureImporterFormat.RGBA64,
                textureCompression = TextureImporterCompression.Uncompressed,
                resizeAlgorithm = TextureResizeAlgorithm.Mitchell
            });
        }

        // Sprites used in Texture2DArray need to be readable
        if (importer.assetPath.Contains("Assets/Texture/Sprite"))
        {
            importer.isReadable = true;
        }
    }
}

}