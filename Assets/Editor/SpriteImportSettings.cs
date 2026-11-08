using UnityEditor;
using UnityEngine;

namespace ProjectShift.Editor
{
    /// <summary>
    /// First-import settings for pixel-art sprites under Assets/01_Art/Sprites/: single sprite, 36 PPU,
    /// point filter, no mipmaps, uncompressed, centre pivot (Pixel.png: bottom-left).
    /// Only textures without import settings yet are touched, so existing .meta files never change.
    /// </summary>
    public class SpriteImportSettings : AssetPostprocessor
    {
        private const string SpriteFolder = "Assets/01_Art/Sprites/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpriteFolder) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 36;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool isPixel = assetPath == SpriteFolder + "Pixel.png";
            settings.spriteAlignment = (int)(isPixel ? SpriteAlignment.BottomLeft : SpriteAlignment.Center);
            settings.spritePivot = isPixel ? Vector2.zero : new Vector2(0.5f, 0.5f);
            importer.SetTextureSettings(settings);
        }
    }
}
