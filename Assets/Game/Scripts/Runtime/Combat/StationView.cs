using System;
using System.Collections.Generic;
using UnityEngine;
using RHMG.Core;
using RHMG.Runtime.UI;
using RHMG.Runtime.Visuals;

namespace RHMG.Runtime.Combat
{
    /// <summary>Spirit Station (SPEC-P02 §4): three function tiles, standing on one for 1 s activates it; the exit gate leaves.</summary>
    public sealed class StationView
    {
        public static readonly Vector3[] TileSpots =
        {
            new Vector3(-4f, 0f, 3f), new Vector3(0f, 0f, 4.5f), new Vector3(4f, 0f, 3f)
        };

        public static readonly Vector3 ExitSpot = new Vector3(0f, 0f, -5f);
        public const float ExitSeconds = 0.6f;

        private sealed class Tile
        {
            public StationTile Kind;
            public GameObject Root;
            public GameObject Disc;
            public GameObject Progress;
            public TextMesh Label;
            public SpriteRenderer Art;   // tile_* rune art (lot E); null = coloured disc only
            public float Hold;
            public bool Spent;
        }

        private static string ArtOf(StationTile kind)
        {
            switch (kind)
            {
                case StationTile.Fusion: return "tile_fusion";
                case StationTile.SpiritUp: return "tile_spirit_up";
                case StationTile.Heal: return "tile_heal";
                case StationTile.XpChest: return "tile_xp_chest";
                default: return "tile_card_chest";
            }
        }

        private readonly CombatWorld world;
        private readonly Transform root;
        private readonly List<Tile> tiles = new List<Tile>(3);
        private GameObject exitDisc;
        private GameObject exitProgress;
        private TextMesh exitLabel;
        private float exitHold;
        private bool used;
        private bool paused;
        private Camera viewCamera;

        public event Action<int, StationTile> TileActivated;
        public event Action ExitReached;

        public bool Used => used;
        public int TileCount => tiles.Count;

        public StationView(CombatWorld owner, Transform parent)
        {
            world = owner;
            root = new GameObject("Station").transform;
            root.SetParent(parent, false);
        }

        public void Build(IList<StationTile> kinds, Camera camera)
        {
            viewCamera = camera;
            for (int i = 0; i < kinds.Count && i < TileSpots.Length; i++)
            {
                tiles.Add(MakeTile(kinds[i], TileSpots[i]));
            }

            exitDisc = VfxFactory.UnitDisc(new Color(Palette.LightGold.r, Palette.LightGold.g, Palette.LightGold.b, 0.35f));
            exitDisc.transform.SetParent(root, false);
            exitDisc.transform.position = ExitSpot + Vector3.up * 0.03f;
            exitDisc.transform.localScale = new Vector3(CombatTuning.StationTileRadius, 1f, CombatTuning.StationTileRadius);
            exitProgress = VfxFactory.UnitDisc(new Color(Palette.LightGold.r, Palette.LightGold.g, Palette.LightGold.b, 0.9f));
            exitProgress.transform.SetParent(root, false);
            exitProgress.transform.position = ExitSpot + Vector3.up * 0.05f;
            exitProgress.transform.localScale = Vector3.zero;
            exitLabel = MakeLabel("Cổng ra", ExitSpot + new Vector3(0f, 1.6f, 0f));
            SetUsed(false);
        }

        public Vector3 TilePosition(int index)
        {
            return TileSpots[Mathf.Clamp(index, 0, TileSpots.Length - 1)];
        }

        public StationTile TileKind(int index)
        {
            return tiles[index].Kind;
        }

        /// <summary>Frozen while a selection panel is open.</summary>
        public void SetPaused(bool value)
        {
            paused = value;
        }

        /// <summary>One tile was used: the other two fade, the gate lights up.</summary>
        public void MarkUsed(int index)
        {
            used = true;
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].Spent = true;
                tiles[i].Hold = 0f;
                SetProgress(tiles[i].Progress, 0f);
            }

            SetUsed(true);
        }

        public void Step(float deltaTime)
        {
            if (paused || world.Player == null)
            {
                return;
            }

            Vector3 player = world.Player.transform.position;
            if (viewCamera != null)
            {
                for (int i = 0; i < tiles.Count; i++)
                {
                    tiles[i].Label.transform.rotation = viewCamera.transform.rotation;
                }

                exitLabel.transform.rotation = viewCamera.transform.rotation;
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile tile = tiles[i];
                if (tile.Spent)
                {
                    continue;
                }

                bool inside = CombatMath.Overlap(player, 0f, TileSpots[i], CombatTuning.StationTileRadius);
                tile.Hold = inside ? tile.Hold + deltaTime : Mathf.Max(0f, tile.Hold - deltaTime * 2f);
                SetProgress(tile.Progress, tile.Hold / CombatTuning.StationActivateSeconds);
                if (tile.Hold >= CombatTuning.StationActivateSeconds)
                {
                    tile.Hold = 0f;
                    SetProgress(tile.Progress, 0f);
                    TileActivated?.Invoke(i, tile.Kind);
                    return;
                }
            }

            bool atExit = CombatMath.Overlap(player, 0f, ExitSpot, CombatTuning.StationTileRadius);
            exitHold = atExit ? exitHold + deltaTime : Mathf.Max(0f, exitHold - deltaTime * 2f);
            SetProgress(exitProgress, exitHold / ExitSeconds);
            if (exitHold >= ExitSeconds)
            {
                exitHold = 0f;
                SetProgress(exitProgress, 0f);
                ExitReached?.Invoke();
            }
        }

        public static string NameOf(StationTile kind)
        {
            switch (kind)
            {
                case StationTile.Fusion: return "Dung hợp";
                case StationTile.SpiritUp: return "Thăng cấp";
                case StationTile.Heal: return "Hồi máu";
                case StationTile.XpChest: return "Rương linh thạch";
                default: return "Rương thẻ";
            }
        }

        public static Color ColorOf(StationTile kind)
        {
            switch (kind)
            {
                case StationTile.Fusion: return new Color(0.95f, 0.55f, 0.25f, 1f);
                case StationTile.SpiritUp: return Palette.LightGold;
                case StationTile.Heal: return Palette.LeafGreen;
                case StationTile.XpChest: return new Color(0.45f, 0.88f, 1f, 1f);
                default: return new Color(0.75f, 0.5f, 1f, 1f);
            }
        }

        private Tile MakeTile(StationTile kind, Vector3 spot)
        {
            Tile tile = new Tile();
            tile.Kind = kind;
            Color color = ColorOf(kind);
            tile.Root = new GameObject("Tile_" + kind);
            tile.Root.transform.SetParent(root, false);
            tile.Disc = VfxFactory.UnitDisc(new Color(color.r, color.g, color.b, 0.4f));
            tile.Disc.transform.SetParent(tile.Root.transform, false);
            tile.Disc.transform.position = spot + Vector3.up * 0.03f;
            tile.Disc.transform.localScale = new Vector3(CombatTuning.StationTileRadius, 1f, CombatTuning.StationTileRadius);
            Sprite art = CharacterSprites.Prop(ArtOf(kind));
            if (art != null)
            {
                GameObject flat = new GameObject("Art");
                flat.transform.SetParent(tile.Root.transform, false);
                flat.transform.position = spot + Vector3.up * 0.04f;
                flat.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                tile.Art = flat.AddComponent<SpriteRenderer>();
                tile.Art.sprite = art;
                Material material = CharacterSprites.SpriteMaterial();
                if (material != null)
                {
                    tile.Art.sharedMaterial = material;
                }

                float scale = CombatTuning.StationTileRadius * 2.3f / Mathf.Max(0.01f, art.bounds.size.x);
                flat.transform.localScale = new Vector3(scale, scale, scale);
            }
            tile.Progress = VfxFactory.UnitDisc(new Color(color.r, color.g, color.b, 0.95f));
            tile.Progress.transform.SetParent(tile.Root.transform, false);
            tile.Progress.transform.position = spot + Vector3.up * 0.05f;
            tile.Progress.transform.localScale = Vector3.zero;
            tile.Label = MakeLabel(NameOf(kind), spot + new Vector3(0f, 1.6f, 0f));
            tile.Label.transform.SetParent(tile.Root.transform, true);
            return tile;
        }

        private TextMesh MakeLabel(string text, Vector3 position)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(root, false);
            go.transform.position = position;
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.text = Loc.T(text);
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 48;
            mesh.characterSize = 0.07f;
            mesh.color = Color.white;
            Font font = UiSkin.Body;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (font != null && renderer != null)
            {
                mesh.font = font;
                renderer.sharedMaterial = font.material;
            }

            return mesh;
        }

        private void SetUsed(bool value)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                Color c = ColorOf(tiles[i].Kind);
                float alpha = tiles[i].Spent ? 0.1f : 0.4f;
                SetColor(tiles[i].Disc, new Color(c.r, c.g, c.b, alpha));
                tiles[i].Label.color = tiles[i].Spent ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
                if (tiles[i].Art != null)
                {
                    tiles[i].Art.color = tiles[i].Spent ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
                }
            }

            Color g = Palette.LightGold;
            SetColor(exitDisc, new Color(g.r, g.g, g.b, value ? 0.7f : 0.3f));
            exitLabel.color = value ? Palette.LightGold : new Color(1f, 1f, 1f, 0.8f);
        }

        private static void SetColor(GameObject disc, Color color)
        {
            MeshRenderer renderer = disc.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = MaterialLibrary.UnlitTransparent(color);
            }
        }

        private static void SetProgress(GameObject disc, float t01)
        {
            float scale = Mathf.Clamp01(t01) * CombatTuning.StationTileRadius;
            disc.transform.localScale = new Vector3(scale, 1f, scale);
        }
    }
}
