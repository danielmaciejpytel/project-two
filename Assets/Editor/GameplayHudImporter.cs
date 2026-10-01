using UnityEditor;
using UnityEngine;

// Import settings for the gameplay HUD graphics (Assets/Sprites/GameplayHud): PNGs exported from the SVG sources in Source~
// at 2x the 1920x1080 layout, so one sprite pixel is one pixel on a 4K screen. With a pixels-per-unit of 200 and the canvas
// reference of 100, a sprite's native size is its size in the layout.
public class GameplayHudImporter : AssetPostprocessor
{
    public const string Folder = "Assets/Sprites/GameplayHud/";
    public const float PixelsPerUnit = 200.0f;

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
        // The full-screen dimming is a flat colour; everything else stays uncompressed so the edges stay sharp.
        importer.textureCompression = assetPath.EndsWith("overlay-dim.png") ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteExtrude = 0;
        importer.SetTextureSettings(settings);
    }
}
