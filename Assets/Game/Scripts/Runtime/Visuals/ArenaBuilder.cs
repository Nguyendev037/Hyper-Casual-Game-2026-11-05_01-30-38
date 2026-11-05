using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Constructs normal dungeon arenas and the Sun/Moon showcase arena.</summary>
    internal static class ArenaBuilder
    {
        public static GameObject Build(Transform parent, float width, float depth, bool showcase)
        {
            width = Mathf.Max(6f, width);
            depth = Mathf.Max(6f, depth);

            GameObject root = new GameObject(showcase ? "ShowcaseArena" : "TowerArena");
            if (parent != null) root.transform.SetParent(parent, false);

            float halfW = width * 0.5f;
            float halfD = depth * 0.5f;

            Color floorColor = showcase ? Lighten(Palette.Night, 1.15f) : Palette.SandMortar;
            Color wallColor = showcase ? Palette.AshGrey : Palette.Wood;
            Color pillarColor = showcase ? Palette.Silver : Palette.AshGrey;

            // Base floor slab
            GameObject floorBase = Prim.Make(PrimitiveType.Cube, root.transform,
                new Vector3(0f, -0.1f, 0f),
                new Vector3(width, 0.2f, depth), floorColor, "FloorBase", keepCollider: true);
            SetTexture(floorBase, "tex_floor_sand", floorColor, new Vector2(width / 2f, depth / 2f));

            // Floor decorative pattern
            if (showcase)
            {
                // Sun & Moon concentric rings in center
                GameObject sunRing = new GameObject("SunRing");
                sunRing.transform.SetParent(root.transform, false);
                sunRing.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                var mfSun = sunRing.AddComponent<MeshFilter>();
                mfSun.sharedMesh = MeshBuilder.Ring(1.8f, 2.2f, 36);
                var mrSun = sunRing.AddComponent<MeshRenderer>();
                mrSun.sharedMaterial = MaterialLibrary.Unlit(Palette.LightGold);

                GameObject moonRing = new GameObject("MoonRing");
                moonRing.transform.SetParent(root.transform, false);
                moonRing.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                var mfMoon = moonRing.AddComponent<MeshFilter>();
                mfMoon.sharedMesh = MeshBuilder.Ring(2.4f, 2.7f, 36);
                var mrMoon = moonRing.AddComponent<MeshRenderer>();
                mrMoon.sharedMaterial = MaterialLibrary.Unlit(Palette.Silver);
            }
            else
            {
                // Sand checkerboard: 2x2-unit tiles over a thin mortar base (#C9A66E).
                const float tileSize = 2f;
                const float gap = 0.06f;
                int tilesX = Mathf.Max(1, Mathf.RoundToInt(width / tileSize));
                int tilesZ = Mathf.Max(1, Mathf.RoundToInt(depth / tileSize));
                float stepX = width / tilesX;
                float stepZ = depth / tilesZ;
                for (int x = 0; x < tilesX; x++)
                {
                    for (int z = 0; z < tilesZ; z++)
                    {
                        Color tone = ((x + z) % 2 == 0) ? Palette.Sand : Palette.SandShade;
                        bool runeTile = x == tilesX / 2 && z == tilesZ / 2;
                        Vector3 tilePos = new Vector3(-halfW + (x + 0.5f) * stepX, 0.01f,
                            -halfD + (z + 0.5f) * stepZ);
                        GameObject tile = Prim.Make(PrimitiveType.Cube, root.transform, tilePos,
                            new Vector3(stepX - gap, 0.02f, stepZ - gap), tone, $"Tile_{x}_{z}");
                        var tileRenderer = tile.GetComponent<MeshRenderer>();
                        if (tileRenderer != null)
                        {
                            string textureId = runeTile ? "tex_floor_rune" : "tex_floor_sand";
                            SetTexture(tile, textureId, tone, new Vector2(stepX / 2f, stepZ / 2f));
                            tileRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        }
                    }
                }
            }

            // Low border walls (south, west, east, and north with gate gap)
            float wallH = 0.6f;
            float wallT = 0.4f;
            // South
            GameObject southWall = Prim.Make(PrimitiveType.Cube, root.transform,
                new Vector3(0f, wallH * 0.5f, -halfD),
                new Vector3(width, wallH, wallT), wallColor, "WallSouth", keepCollider: true);
            SetTexture(southWall, "tex_wall_stone", wallColor, new Vector2(width / 2f, wallH / 2f));
            // West
            GameObject westWall = Prim.Make(PrimitiveType.Cube, root.transform,
                new Vector3(-halfW, wallH * 0.5f, 0f),
                new Vector3(wallT, wallH, depth), wallColor, "WallWest", keepCollider: true);
            SetTexture(westWall, "tex_wall_stone", wallColor, new Vector2(depth / 2f, wallH / 2f));
            // East
            GameObject eastWall = Prim.Make(PrimitiveType.Cube, root.transform,
                new Vector3(halfW, wallH * 0.5f, 0f),
                new Vector3(wallT, wallH, depth), wallColor, "WallEast", keepCollider: true);
            SetTexture(eastWall, "tex_wall_stone", wallColor, new Vector2(depth / 2f, wallH / 2f));

            // North wall sections with gate opening
            float gateW = 3.2f;
            float sideWallW = (width - gateW) * 0.5f;
            GameObject northWallL = Prim.Make(PrimitiveType.Cube, root.transform,
                new Vector3(-halfW + sideWallW * 0.5f, wallH * 0.5f, halfD),
                new Vector3(sideWallW, wallH, wallT), wallColor, "WallNorthL", keepCollider: true);
            SetTexture(northWallL, "tex_wall_stone", wallColor, new Vector2(sideWallW / 2f, wallH / 2f));
            GameObject northWallR = Prim.Make(PrimitiveType.Cube, root.transform,
                new Vector3(halfW - sideWallW * 0.5f, wallH * 0.5f, halfD),
                new Vector3(sideWallW, wallH, wallT), wallColor, "WallNorthR", keepCollider: true);
            SetTexture(northWallR, "tex_wall_stone", wallColor, new Vector2(sideWallW / 2f, wallH / 2f));

            // 4 Corner pillars
            Vector2[] corners = { new Vector2(-halfW, -halfD), new Vector2(halfW, -halfD),
                                  new Vector2(-halfW, halfD), new Vector2(halfW, halfD) };
            for (int i = 0; i < 4; i++)
            {
                Vector3 pPos = new Vector3(corners[i].x, 1.5f, corners[i].y);
                GameObject pillar = Prim.Make(PrimitiveType.Cylinder, root.transform, pPos,
                    new Vector3(0.7f, 1.5f, 0.7f), pillarColor, $"Pillar_{i}", keepCollider: true);
                SetTexture(pillar, "tex_wall_stone", pillarColor, Vector2.one);
                GameObject pillarCap = Prim.Make(PrimitiveType.Cube, root.transform,
                    pPos + Vector3.up * 1.5f, new Vector3(0.9f, 0.2f, 0.9f), pillarColor, $"PillarCap_{i}");
                SetTexture(pillarCap, "tex_wall_stone", pillarColor, Vector2.one);
            }

            // Top Gate portal (cửa lên tầng)
            Vector3 gateCenter = new Vector3(0f, 0f, halfD);
            GameObject gatePillarL = Prim.Make(PrimitiveType.Cylinder, root.transform,
                gateCenter + new Vector3(-gateW * 0.5f, 1.8f, 0f),
                new Vector3(0.55f, 1.8f, 0.55f), pillarColor, "GatePillarL", keepCollider: true);
            SetTexture(gatePillarL, "tex_wall_stone", pillarColor, Vector2.one);
            GameObject gatePillarR = Prim.Make(PrimitiveType.Cylinder, root.transform,
                gateCenter + new Vector3(gateW * 0.5f, 1.8f, 0f),
                new Vector3(0.55f, 1.8f, 0.55f), pillarColor, "GatePillarR", keepCollider: true);
            SetTexture(gatePillarR, "tex_wall_stone", pillarColor, Vector2.one);
            GameObject gateLintel = Prim.Make(PrimitiveType.Cube, root.transform,
                gateCenter + new Vector3(0f, 3.6f, 0f),
                new Vector3(gateW + 0.8f, 0.5f, 0.7f), pillarColor, "GateLintel");
            SetTexture(gateLintel, "tex_wall_stone", pillarColor, new Vector2(gateW / 2f, 1f));

            // Gate energy veil
            GameObject portal = Prim.Make(PrimitiveType.Cube, root.transform, gateCenter + new Vector3(0f, 1.8f, 0f),
                new Vector3(gateW * 0.85f, 3.2f, 0.1f), showcase ? Palette.LightGold : Palette.WaterBlue, "GatePortal");
            var mrPortal = portal.GetComponent<MeshRenderer>();
            if (mrPortal != null)
            {
                mrPortal.sharedMaterial = MaterialLibrary.UnlitTransparent(
                    new Color(Palette.WaterBlue.r, Palette.WaterBlue.g, Palette.WaterBlue.b, 0.45f));
            }

            return root;
        }

        private static void SetTexture(GameObject target, string textureId, Color tint, Vector2 tiling)
        {
            Texture2D texture = WorldSkin.Tex(textureId);
            if (texture == null)
            {
                return;
            }

            MeshRenderer renderer = target == null ? null : target.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = MaterialLibrary.LitTextured(tint, texture, tiling);
            }
        }

        private static Color Lighten(Color c, float factor)
        {
            return new Color(Mathf.Clamp01(c.r * factor), Mathf.Clamp01(c.g * factor),
                Mathf.Clamp01(c.b * factor), c.a);
        }
    }
}
