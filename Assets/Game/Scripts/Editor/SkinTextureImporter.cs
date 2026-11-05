using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RHMG.EditorTools
{
    public sealed class SkinTextureImporter : AssetPostprocessor
    {
        // 9-slice borders (Unity order L, B, R, T) from the Gemini layout, staging/E/ui_layout.json "sprites" (plan 04 lot E).
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "panel", new Vector4(64, 64, 64, 64) },
            { "panel_station", new Vector4(64, 64, 64, 64) },
            { "card", new Vector4(48, 48, 48, 48) },
            { "card_common", new Vector4(48, 48, 48, 48) },
            { "card_rare", new Vector4(48, 48, 48, 48) },
            { "card_spirit", new Vector4(48, 48, 48, 48) },
            { "button_primary", new Vector4(64, 48, 64, 48) },
            { "button_secondary", new Vector4(64, 48, 64, 48) },
            { "circle_button", new Vector4(64, 64, 64, 64) },
            { "tab_on", new Vector4(48, 48, 48, 48) },
            { "tab_off", new Vector4(48, 48, 48, 48) },
            { "toast", new Vector4(64, 48, 64, 48) },
            { "bar_frame", new Vector4(48, 48, 48, 48) },
            { "bar_fill", new Vector4(48, 48, 48, 48) },
            { "xp_bar_frame", new Vector4(48, 48, 48, 48) },
            { "xp_bar_fill", new Vector4(48, 48, 48, 48) },
            { "boss_bar_frame", new Vector4(48, 48, 48, 48) },
            { "boss_bar_fill", new Vector4(48, 48, 48, 48) },
            { "badge_equipped", new Vector4(48, 48, 48, 48) }
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
                string spriteId = System.IO.Path.GetFileNameWithoutExtension(path);
                // Floating / flat things pivot at the centre: spirits, spirit stones, station rune tiles.
                bool spirit = spriteId.StartsWith("spirit_") || spriteId.StartsWith("gem_") || spriteId.StartsWith("tile_");
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)(spirit ? SpriteAlignment.Center : SpriteAlignment.BottomCenter);
                importer.SetTextureSettings(settings);
                return;
            }
            if (path.Contains("/Resources/RHMG/VFX/"))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 512;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                TextureImporterSettings vfx = new TextureImporterSettings();
                importer.ReadTextureSettings(vfx);
                vfx.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(vfx);
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
