using UnityEngine;
using RHMG.Core;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Constructs enemy primitives (Beetle, Eye, Sentinel, CageWarden).</summary>
    internal static class EnemyBuilder
    {
        public static GameObject Build(EnemyKind kind, Transform parent)
        {
            GameObject root = new GameObject(kind.ToString());
            if (parent != null) root.transform.SetParent(parent, false);

            // Regular enemies grow 1.25x; the CageWarden keeps its 2.5x boss silhouette.
            bool isWarden = kind == EnemyKind.CageWarden;
            bool newBoss = kind == EnemyKind.SandQueen || kind == EnemyKind.BlueHairGuy;
            root.transform.localScale = Vector3.one * (isWarden ? 2.5f : (newBoss ? 3.25f : 1.25f));

            switch (kind)
            {
                case EnemyKind.SandBeetle:
                    Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.70f, 0.32f, 0.75f), Palette.Sand, "Body");
                    Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.26f, -0.05f), new Vector3(0.62f, 0.26f, 0.65f), Palette.EnemyPurple, "Carapace");
                    Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.20f, 0.38f), new Vector3(0.28f, 0.22f, 0.28f), Palette.EnemyPurple, "Head");
                    Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(-0.09f, 0.23f, 0.48f), new Vector3(0.06f, 0.06f, 0.06f), Palette.WarnRed, "EyeL");
                    Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0.09f, 0.23f, 0.48f), new Vector3(0.06f, 0.06f, 0.06f), Palette.WarnRed, "EyeR");
                    for (int i = 0; i < 3; i++)
                    {
                        float z = -0.20f + i * 0.20f;
                        Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(-0.40f, 0.10f, z), new Vector3(0.08f, 0.25f, 0.08f), Palette.EnemyPurple, $"LegL_{i}");
                        Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(0.40f, 0.10f, z), new Vector3(0.08f, 0.25f, 0.08f), Palette.EnemyPurple, $"LegR_{i}");
                    }
                    break;

                case EnemyKind.SandEye:
                    Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.10f, 0f), new Vector3(0.70f, 0.70f, 0.70f), Palette.Silver, "Sclera");
                    Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 1.10f, 0.26f), new Vector3(0.45f, 0.08f, 0.45f), Palette.EnemyPurple, "Iris");
                    Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.10f, 0.32f), new Vector3(0.24f, 0.24f, 0.10f), Palette.Ink, "Pupil");
                    Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(0f, 1.42f, 0.10f), new Vector3(0.68f, 0.12f, 0.25f), Palette.Sand, "LidTop");
                    Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(0f, 0.78f, 0.10f), new Vector3(0.68f, 0.12f, 0.25f), Palette.Sand, "LidBot");
                    Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(-0.16f, 0.55f, 0f), new Vector3(0.12f, 0.12f, 0.12f), Palette.EnemyPurple, "Pebble1");
                    Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0.16f, 0.55f, 0f), new Vector3(0.12f, 0.12f, 0.12f), Palette.EnemyPurple, "Pebble2");
                    break;

                case EnemyKind.Sentinel:
                    Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.65f, 0.75f, 0.45f), Palette.AshGrey, "ArmorTorso");
                    Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.42f, 0.42f, 0.42f), Palette.AshGrey, "Helm");
                    Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0f, 1.15f, 0.20f), new Vector3(0.30f, 0.08f, 0.06f), Palette.WarnRed, "VisorSlit");
                    GameObject hornL = Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(-0.25f, 1.38f, 0f), new Vector3(0.10f, 0.32f, 0.10f), Palette.Sand, "HornL");
                    hornL.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
                    GameObject hornR = Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(0.25f, 1.38f, 0f), new Vector3(0.10f, 0.32f, 0.10f), Palette.Sand, "HornR");
                    hornR.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
                    Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(-0.45f, 0.88f, 0f), new Vector3(0.25f, 0.25f, 0.30f), Palette.EnemyPurple, "PauldronL");
                    Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0.45f, 0.88f, 0f), new Vector3(0.25f, 0.25f, 0.30f), Palette.EnemyPurple, "PauldronR");
                    Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(-0.18f, 0.18f, 0f), new Vector3(0.16f, 0.36f, 0.16f), Palette.AshGrey, "LegL");
                    Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0.18f, 0.18f, 0f), new Vector3(0.16f, 0.36f, 0.16f), Palette.AshGrey, "LegR");
                    break;

                case EnemyKind.CageWarden:
                case EnemyKind.SandQueen:
                case EnemyKind.BlueHairGuy:
                    Color wardenBody = kind == EnemyKind.CageWarden ? Palette.AshGrey : Palette.EnemyPurple;
                    Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.90f, 0f), new Vector3(0.95f, 1.10f, 0.65f), wardenBody, "WardenTorso");
                    Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0f, 1.55f, 0f), new Vector3(0.55f, 0.50f, 0.55f), wardenBody, "WardenHelm");
                    Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0f, 1.55f, 0.26f), new Vector3(0.42f, 0.10f, 0.08f), Palette.WarnRed, "WardenVisor");
                    Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(-0.65f, 0.85f, 0.15f), new Vector3(0.26f, 0.80f, 0.26f), wardenBody, "ArmL");
                    Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(0.65f, 0.85f, -0.10f), new Vector3(0.26f, 0.80f, 0.26f), wardenBody, "ArmR");
                    Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(-0.28f, 0.22f, 0f), new Vector3(0.26f, 0.44f, 0.26f), wardenBody, "LegL");
                    Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0.28f, 0.22f, 0f), new Vector3(0.26f, 0.44f, 0.26f), wardenBody, "LegR");

                    Transform cage = new GameObject("HeldCage").transform;
                    cage.SetParent(root.transform, false);
                    cage.localPosition = new Vector3(-0.80f, 0.45f, 0.25f);
                    Prim.Make(PrimitiveType.Cube, cage, new Vector3(0f, -0.25f, 0f), new Vector3(0.50f, 0.06f, 0.50f), Palette.Sand, "CageBot");
                    Prim.Make(PrimitiveType.Cube, cage, new Vector3(0f, 0.25f, 0f), new Vector3(0.50f, 0.06f, 0.50f), Palette.Sand, "CageTop");
                    for (int b = 0; b < 4; b++)
                    {
                        float bx = (b % 2 == 0 ? -0.20f : 0.20f);
                        float bz = (b / 2 == 0 ? -0.20f : 0.20f);
                        Prim.Make(PrimitiveType.Cylinder, cage, new Vector3(bx, 0f, bz), new Vector3(0.04f, 0.50f, 0.04f), wardenBody, $"Bar_{b}");
                    }
                    break;
            }

            // 2D sprite replaces the primitive body (S1: missing sprite keeps the primitives). Hidden before the shadow exists.
            Sprite sprite = CharacterSprites.Enemy(kind);
            if (sprite != null)
            {
                CharacterSprites.SetPrimitivesVisible(root.transform, null, false);
                SpriteBillboard billboard = SpriteBillboard.Create(root.transform, sprite, CharacterSprites.EnemyHeight(kind));
                billboard.SetMotion(0f);
            }

            float shadowRadius = 0.50f;
            switch (kind)
            {
                case EnemyKind.SandEye:
                    shadowRadius = 0.45f;
                    break;
                case EnemyKind.Sentinel:
                    shadowRadius = 0.55f;
                    break;
                case EnemyKind.CageWarden:
                case EnemyKind.SandQueen:
                case EnemyKind.BlueHairGuy:
                    shadowRadius = 1.20f;
                    break;
            }
            BlobShadow.Create(root.transform, shadowRadius);

            return root;
        }
    }
}
