using UnityEditor;
using UnityEngine;

// Import settings for the gameplay background layers (Assets/Sprites/Background/Layers): PNGs exported from the SVG sources in Source~,
// 1 pixel of the 1920x1080 artwork = 1 pixel of the sprite, so 100 pixels per unit like the old frame animation.
public class BackgroundImporter : AssetPostprocessor
{
    public const string Folder = "Assets/Sprites/Background/Layers/";
    public const float PixelsPerUnit = 100.0f;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith(".png")) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.isReadable = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteExtrude = 0;
        importer.SetTextureSettings(settings);
    }
}
