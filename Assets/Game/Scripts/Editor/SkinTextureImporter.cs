using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RHMG.EditorTools
{
    public sealed class SkinTextureImporter : AssetPostprocessor
    {
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "panel", new Vector4(64, 64, 64, 64) },
            { "card", new Vector4(56, 56, 56, 56) },
            { "button_primary", new Vector4(48, 48, 48, 48) },
            { "button_secondary", new Vector4(48, 48, 48, 48) },
            { "bar_frame", new Vector4(24, 24, 24, 24) },
            { "bar_fill", new Vector4(24, 24, 24, 24) },
            { "toast", new Vector4(40, 40, 40, 40) }
        };

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            TextureImporter importer = (TextureImporter)assetImporter;
            if (path.Contains("/Resources/RHMG/UI/") || path.Contains("/Resources/RHMG/Icons/"))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                string id = System.IO.Path.GetFileNameWithoutExtension(path);
                if (Borders.TryGetValue(id, out Vector4 border)) importer.spriteBorder = border;
                return;
            }
            if (path.Contains("/Resources/RHMG/Sprites/"))
            {
                // Character billboards: spirits pivot at the centre (they float), everything else at the feet.
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 1024;
                importer.filterMode = FilterMode.Bilinear;
                bool spirit = System.IO.Path.GetFileNameWithoutExtension(path).StartsWith("spirit_");
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)(spirit ? SpriteAlignment.Center : SpriteAlignment.BottomCenter);
                importer.SetTextureSettings(settings);
                return;
            }
            if (path.Contains("/Resources/RHMG/World/"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 1024;
            }
        }
    }
}
