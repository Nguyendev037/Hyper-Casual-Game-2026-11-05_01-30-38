using UnityEngine;
using RHMG.Core;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Constructs low-poly chibi characters, companions, enemies, spirits, and environments.</summary>
    public static class CharacterFactory
    {
        public static GirlRig CreateGirl(Transform parent)
        {
            GameObject root = new GameObject("Girl");
            if (parent != null) root.transform.SetParent(parent, false);

            GirlRig rig = root.AddComponent<GirlRig>();

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 1.45f;
            rig.Visual = visual.transform;
            rig.BlobShadow = BlobShadow.Create(root.transform, 0.50f);

            // Torso & skirt base (~45% head ratio, total height ~1.8)
            Prim.Make(PrimitiveType.Capsule, visual.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.42f, 0.35f, 0.35f), Palette.Sand, "Torso");

            // Arms
            Prim.Make(PrimitiveType.Capsule, visual.transform, new Vector3(-0.28f, 0.42f, 0f), new Vector3(0.12f, 0.28f, 0.12f), Palette.Skin, "ArmL");
            Prim.Make(PrimitiveType.Capsule, visual.transform, new Vector3(0.28f, 0.42f, 0f), new Vector3(0.12f, 0.28f, 0.12f), Palette.Skin, "ArmR");

            // Legs & Shoes
            Prim.Make(PrimitiveType.Cylinder, visual.transform, new Vector3(-0.14f, 0.12f, 0f), new Vector3(0.14f, 0.22f, 0.14f), Palette.Wood, "ShoeL");
            Prim.Make(PrimitiveType.Cylinder, visual.transform, new Vector3(0.14f, 0.12f, 0f), new Vector3(0.14f, 0.22f, 0.14f), Palette.Wood, "ShoeR");

            // Head (Chibi big head ~0.8 scale at y=0.95)
            Transform head = Prim.Make(PrimitiveType.Sphere, visual.transform, new Vector3(0f, 0.95f, 0f), new Vector3(0.82f, 0.80f, 0.78f), Palette.Skin, "Head").transform;

            // Big anime eyes
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(-0.20f, 0.04f, 0.36f), new Vector3(0.14f, 0.20f, 0.08f), Palette.Ink, "EyeL");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0.20f, 0.04f, 0.36f), new Vector3(0.14f, 0.20f, 0.08f), Palette.Ink, "EyeR");
            // Eye sparkles
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(-0.18f, 0.09f, 0.39f), new Vector3(0.05f, 0.05f, 0.04f), Color.white, "SparkleL");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0.22f, 0.09f, 0.39f), new Vector3(0.05f, 0.05f, 0.04f), Color.white, "SparkleR");

            // Rosy cheeks (Blush)
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(-0.28f, -0.10f, 0.32f), new Vector3(0.12f, 0.06f, 0.08f), Palette.Rose, "BlushL");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0.28f, -0.10f, 0.32f), new Vector3(0.12f, 0.06f, 0.08f), Palette.Rose, "BlushR");

            // Signature Red Hair (bangs, sides, and twin ponytails)
            // Dark shell offset behind the hair gives the silhouette a rim against the sand floor.
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0f, 0.10f, -0.19f),
                new Vector3(0.933f, 0.890f, 0.869f), Palette.HairShadow, "HairOutline");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0f, 0.10f, -0.12f), new Vector3(0.88f, 0.84f, 0.82f), Palette.HairRed, "HairBack");
            // Front bangs framing face
            Prim.Make(PrimitiveType.Capsule, head, new Vector3(-0.22f, 0.24f, 0.30f), new Vector3(0.14f, 0.22f, 0.12f), Palette.HairRed, "BangsL");
            Prim.Make(PrimitiveType.Capsule, head, new Vector3(0.22f, 0.24f, 0.30f), new Vector3(0.14f, 0.22f, 0.12f), Palette.HairRed, "BangsR");
            Prim.Make(PrimitiveType.Capsule, head, new Vector3(0f, 0.30f, 0.34f), new Vector3(0.18f, 0.16f, 0.12f), Palette.HairRed, "BangsCenter");
            // Side twin ponytails
            Prim.Make(PrimitiveType.Capsule, head, new Vector3(-0.46f, 0.05f, -0.15f), new Vector3(0.18f, 0.45f, 0.18f), Palette.HairRed, "TailL");
            Prim.Make(PrimitiveType.Capsule, head, new Vector3(0.46f, 0.05f, -0.15f), new Vector3(0.18f, 0.45f, 0.18f), Palette.HairRed, "TailR");

            // Slots
            rig.HeadSlot = new GameObject("HeadSlot").transform;
            rig.HeadSlot.SetParent(head, false);
            rig.HeadSlot.localPosition = new Vector3(0f, 0.35f, 0f);

            rig.BodySlot = new GameObject("BodySlot").transform;
            rig.BodySlot.SetParent(visual.transform, false);
            rig.BodySlot.localPosition = new Vector3(0f, 0.40f, 0f);

            rig.StaffSlot = new GameObject("StaffSlot").transform;
            rig.StaffSlot.SetParent(visual.transform, false);
            rig.StaffSlot.localPosition = new Vector3(0.38f, 0.35f, 0.20f);

            // Default outfits
            rig.ApplyOutfit(OutfitSlot.Head, CostumeBuilder.AshHatId);
            rig.ApplyOutfit(OutfitSlot.Body, CostumeBuilder.JourneyCloakId);
            rig.ApplyOutfit(OutfitSlot.Staff, CostumeBuilder.SproutStaffId);

            return rig;
        }

        public static DogRig CreateDog(Transform parent)
        {
            GameObject root = new GameObject("Dog");
            if (parent != null) root.transform.SetParent(parent, false);

            DogRig rig = root.AddComponent<DogRig>();

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            rig.Visual = visual.transform;
            rig.BlobShadow = BlobShadow.Create(root.transform, 0.55f);

            // Round chubby dog body
            Prim.Make(PrimitiveType.Capsule, visual.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.55f, 0.45f, 0.70f), Palette.Sand, "Body");

            // Head
            Transform head = Prim.Make(PrimitiveType.Sphere, visual.transform, new Vector3(0f, 0.55f, 0.35f), new Vector3(0.46f, 0.44f, 0.46f), Palette.Sand, "Head").transform;
            Prim.Make(PrimitiveType.Cube, head, new Vector3(0f, -0.06f, 0.22f), new Vector3(0.20f, 0.16f, 0.20f), Palette.Skin, "Snout");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0f, -0.01f, 0.32f), new Vector3(0.08f, 0.06f, 0.08f), Palette.Ink, "Nose");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(-0.12f, 0.08f, 0.20f), new Vector3(0.07f, 0.07f, 0.07f), Palette.Ink, "EyeL");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0.12f, 0.08f, 0.20f), new Vector3(0.07f, 0.07f, 0.07f), Palette.Ink, "EyeR");
            Prim.Make(PrimitiveType.Capsule, head, new Vector3(-0.24f, 0.08f, -0.02f), new Vector3(0.12f, 0.26f, 0.12f), Palette.Wood, "EarL");
            Prim.Make(PrimitiveType.Capsule, head, new Vector3(0.24f, 0.08f, -0.02f), new Vector3(0.12f, 0.26f, 0.12f), Palette.Wood, "EarR");

            // Collar
            GameObject collar = Prim.Make(PrimitiveType.Cylinder, visual.transform, new Vector3(0f, 0.44f, 0.22f), new Vector3(0.42f, 0.06f, 0.42f), Palette.Wood, "Collar");
            rig.CollarRenderer = collar.transform;

            // Tail & 4 Legs
            rig.Tail = Prim.Make(PrimitiveType.Capsule, visual.transform, new Vector3(0f, 0.45f, -0.38f), new Vector3(0.12f, 0.26f, 0.12f), Palette.Sand, "Tail").transform;
            rig.LegFL = Prim.Make(PrimitiveType.Capsule, visual.transform, new Vector3(-0.20f, 0.14f, 0.22f), new Vector3(0.14f, 0.26f, 0.14f), Palette.Sand, "LegFL").transform;
            rig.LegFR = Prim.Make(PrimitiveType.Capsule, visual.transform, new Vector3(0.20f, 0.14f, 0.22f), new Vector3(0.14f, 0.26f, 0.14f), Palette.Sand, "LegFR").transform;
            rig.LegBL = Prim.Make(PrimitiveType.Capsule, visual.transform, new Vector3(-0.20f, 0.14f, -0.22f), new Vector3(0.14f, 0.26f, 0.14f), Palette.Sand, "LegBL").transform;
            rig.LegBR = Prim.Make(PrimitiveType.Capsule, visual.transform, new Vector3(0.20f, 0.14f, -0.22f), new Vector3(0.14f, 0.26f, 0.14f), Palette.Sand, "LegBR").transform;

            // Sand Ring under feet for SpiritHound
            GameObject sRing = new GameObject("SandRing");
            sRing.transform.SetParent(root.transform, false);
            sRing.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            var mfRing = sRing.AddComponent<MeshFilter>();
            mfRing.sharedMesh = MeshBuilder.Ring(0.55f, 0.75f, 24);
            var mrRing = sRing.AddComponent<MeshRenderer>();
            mrRing.sharedMaterial = MaterialLibrary.Lit(Palette.Sand);
            sRing.SetActive(false);
            rig.SandRing = sRing.transform;

            // Lion Mane Root
            Transform mane = new GameObject("Mane").transform;
            mane.SetParent(visual.transform, false);
            mane.localPosition = new Vector3(0f, 0.50f, 0.22f);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * (360f / 7f) * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Sin(angle) * 0.35f, Mathf.Cos(angle) * 0.35f, 0f);
                Prim.Make(PrimitiveType.Sphere, mane, pos, new Vector3(0.28f, 0.28f, 0.28f), Palette.LightGold, "ManeTuft_" + i);
            }
            rig.SunSymbol = Prim.Make(PrimitiveType.Sphere, head, new Vector3(0f, 0.28f, 0.18f), new Vector3(0.16f, 0.16f, 0.16f), Palette.LightGold, "SunSymbol").transform;
            rig.MoonSymbol = Prim.Make(PrimitiveType.Cylinder, head, new Vector3(0f, 0.28f, 0.18f), new Vector3(0.16f, 0.04f, 0.16f), Palette.Silver, "MoonSymbol").transform;
            mane.gameObject.SetActive(false);
            rig.ManeRoot = mane;

            // Seat & Mouth points
            rig.Seat = new GameObject("Seat").transform;
            rig.Seat.SetParent(root.transform, false);
            rig.Seat.localPosition = new Vector3(0f, 0.60f, -0.05f);

            rig.Mouth = new GameObject("Mouth").transform;
            rig.Mouth.SetParent(head, false);
            rig.Mouth.localPosition = new Vector3(0f, -0.05f, 0.32f);

            rig.SetTier(DogTier.Companion);
            return rig;
        }

        public static GameObject CreateSpirit(string spiritId, Transform parent)
        {
            GameObject root = new GameObject(string.IsNullOrEmpty(spiritId) ? "Spirit" : spiritId);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * 1.4f;
            Transform shadow = BlobShadow.Create(root.transform, 0.34f);

            Color auraColor = Palette.Sand;
            if (spiritId == "spirit.dew") auraColor = Palette.WaterBlue;
            else if (spiritId == "spirit.ember") auraColor = Palette.FireOrange;
            else if (spiritId == "spirit.steam") auraColor = Palette.WaterBlue;
            else if (spiritId == "spirit.bud" || spiritId == "spirit.growth") auraColor = Palette.LeafGreen;
            else if (spiritId == "spirit.wildfire") auraColor = Palette.FireOrange;

            Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(0f, 0.30f, 0f), new Vector3(0.36f, 0.40f, 0.36f), Palette.Sand, "JarBody");
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.52f, 0f), new Vector3(0.28f, 0.08f, 0.28f), Palette.Sand, "JarRim");
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.58f, 0f), new Vector3(0.22f, 0.08f, 0.22f), Palette.Wood, "Cork");

            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(-0.08f, 0.34f, 0.16f), new Vector3(0.06f, 0.06f, 0.06f), Palette.Ink, "EyeL");
            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0.08f, 0.34f, 0.16f), new Vector3(0.06f, 0.06f, 0.06f), Palette.Ink, "EyeR");

            GameObject aura = Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0f, 0.74f, 0f), new Vector3(0.24f, 0.28f, 0.24f), auraColor, "AuraTop");
            var mr = aura.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = MaterialLibrary.Unlit(auraColor);

            if (spiritId == "spirit.steam")
            {
                GameObject puff = Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0.10f, 0.82f, 0f), new Vector3(0.18f, 0.18f, 0.18f), Palette.Silver, "Puff");
                var mrPuff = puff.GetComponent<MeshRenderer>();
                if (mrPuff != null) mrPuff.sharedMaterial = MaterialLibrary.Unlit(Palette.Silver);
            }

            // 2D sprite replaces the jar body and the opaque aura spheres; the blob shadow stays (S1: missing sprite = jar).
            Sprite sprite = CharacterSprites.Spirit(spiritId);
            if (sprite != null)
            {
                CharacterSprites.SetPrimitivesVisible(root.transform, shadow, false);
                SpriteBillboard.Create(root.transform, sprite, CharacterSprites.SpiritHeight).SetMotion(0f);
            }

            return root;
        }

        public static GameObject CreateEnemy(EnemyKind kind, Transform parent)
        {
            return EnemyBuilder.Build(kind, parent);
        }

        /// <summary>Corrupted anchor (Blue Hair Guy phase 3): boss_anchor image, else a dark crystal on a base.</summary>
        public static GameObject CreateAnchor(Transform parent)
        {
            GameObject root = new GameObject("Anchor");
            if (parent != null) root.transform.SetParent(parent, false);
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.1f, 0f), new Vector3(1.2f, 0.2f, 1.2f), Palette.Ink, "AnchorBase");
            Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(0f, 1.0f, 0f), new Vector3(0.7f, 0.9f, 0.7f), Palette.EnemyPurple, "AnchorCrystal");
            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.1f, 0.3f), new Vector3(0.25f, 0.25f, 0.25f), Palette.WarnRed, "AnchorCore");
            Sprite sprite = CharacterSprites.Anchor();
            if (sprite != null)
            {
                CharacterSprites.SetPrimitivesVisible(root.transform, null, false);
                SpriteBillboard.Create(root.transform, sprite, CharacterSprites.AnchorHeight).SetMotion(0f);
            }

            BlobShadow.Create(root.transform, 0.9f);
            return root;
        }

        public static GameObject CreateDummy(Transform parent)
        {
            GameObject root = new GameObject("Dummy");
            if (parent != null) root.transform.SetParent(parent, false);

            Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.06f, 0f), new Vector3(1.10f, 0.12f, 0.22f), Palette.Wood, "BaseX");
            Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.22f, 0.12f, 1.10f), Palette.Wood, "BaseZ");
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.14f, 1.60f, 0.14f), Palette.Wood, "Pole");
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 1.10f, 0f), new Vector3(1.00f, 0.12f, 0.12f), Palette.Wood, "Crossbar");
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.55f, 0.70f, 0.55f), Palette.Sand, "StrawBody");
            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.35f, 0f), new Vector3(0.40f, 0.42f, 0.40f), Palette.Sand, "StrawHead");
            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.85f, 0.28f), new Vector3(0.24f, 0.04f, 0.24f), Palette.WarnRed, "Bullseye");

            BlobShadow.Create(root.transform, 0.60f);
            return root;
        }

        public static GameObject CreateCage(Transform parent, bool activate = true)
        {
            if (parent != null)
            {
                Transform existing = parent.Find("Cage");
                if (existing != null)
                {
                    existing.gameObject.SetActive(activate);
                    return existing.gameObject;
                }
            }
            GameObject root = new GameObject("Cage");
            if (parent != null) root.transform.SetParent(parent, false);

            Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.08f, 0f), new Vector3(1.40f, 0.16f, 1.40f), Palette.AshGrey, "Base", keepCollider: true);
            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.60f, 0f), new Vector3(1.35f, 0.45f, 1.35f), Palette.AshGrey, "Dome");

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Sin(angle) * 0.55f, 0.82f, Mathf.Cos(angle) * 0.55f);
                Prim.Make(PrimitiveType.Cylinder, root.transform, pos, new Vector3(0.06f, 1.40f, 0.06f), Palette.AshGrey, $"Bar_{i}", keepCollider: true);
            }

            Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0f, 0.85f, 0.58f), new Vector3(0.16f, 0.18f, 0.10f), Palette.WarnRed, "Padlock");
            root.SetActive(activate);
            return root;
        }

        public static GameObject CreateArena(Transform parent, float width, float depth, bool showcase)
        {
            return ArenaBuilder.Build(parent, width, depth, showcase);
        }
    }
}
