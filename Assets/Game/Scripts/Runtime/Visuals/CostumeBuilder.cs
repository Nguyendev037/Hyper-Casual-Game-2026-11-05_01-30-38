using UnityEngine;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Constructs visual primitives for the 6 signature outfits.</summary>
    public static class CostumeBuilder
    {
        public const string AshHatId = "outfit.head.ash_hat";
        public const string DawnClipId = "outfit.head.dawn_clip";
        public const string JourneyCloakId = "outfit.body.journey_cloak";
        public const string RoseDressId = "outfit.body.rose_dress";
        public const string SproutStaffId = "outfit.staff.sprout";
        public const string SunrayStaffId = "outfit.staff.sunray";

        public static bool BuildHead(Transform slot, string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            if (id == AshHatId)
            {
                // Wide brim
                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, 0.05f, -0.02f), new Vector3(1.15f, 0.03f, 1.15f), Palette.AshGrey, "HatBrim");
                // Tapered cone body
                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, 0.32f, -0.05f), new Vector3(0.65f, 0.25f, 0.65f), Palette.AshGrey, "HatCone");
                // Bent upper cone & tip
                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, 0.52f, -0.15f), new Vector3(0.38f, 0.18f, 0.38f), Palette.AshGrey, "HatMid");
                Prim.Make(PrimitiveType.Sphere, slot, new Vector3(0f, 0.64f, -0.28f), new Vector3(0.24f, 0.24f, 0.24f), Palette.AshGrey, "HatTip");
                // Ribbon band
                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, 0.12f, -0.03f), new Vector3(0.72f, 0.06f, 0.72f), Palette.HairRed, "RibbonBand");
                return true;
            }

            if (id == DawnClipId)
            {
                Transform clipRoot = new GameObject("DawnClip").transform;
                clipRoot.SetParent(slot, false);
                clipRoot.localPosition = new Vector3(-0.42f, 0.15f, 0.18f);

                // Central sun orb
                Prim.Make(PrimitiveType.Sphere, clipRoot, Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), Palette.LightGold, "SunCenter");

                // 6 radial rays
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * 60f * Mathf.Deg2Rad;
                    Vector3 rayPos = new Vector3(Mathf.Sin(angle) * 0.16f, Mathf.Cos(angle) * 0.16f, 0f);
                    GameObject ray = Prim.Make(PrimitiveType.Cylinder, clipRoot, rayPos, new Vector3(0.03f, 0.08f, 0.03f), Palette.LightGold, "Ray_" + i);
                    ray.transform.localRotation = Quaternion.Euler(0f, 0f, -i * 60f);
                }

                // Small ribbon bow beneath
                Prim.Make(PrimitiveType.Cube, clipRoot, new Vector3(-0.07f, -0.12f, 0.02f), new Vector3(0.12f, 0.06f, 0.04f), Palette.FireOrange, "BowL");
                Prim.Make(PrimitiveType.Cube, clipRoot, new Vector3(0.07f, -0.12f, 0.02f), new Vector3(0.12f, 0.06f, 0.04f), Palette.FireOrange, "BowR");
                return true;
            }

            return false;
        }

        public static bool BuildBody(Transform slot, string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            if (id == JourneyCloakId)
            {
                // Flowing cape behind back
                GameObject cape = Prim.Make(PrimitiveType.Cube, slot, new Vector3(0f, -0.22f, -0.22f), new Vector3(0.68f, 0.72f, 0.12f), Palette.CloakTeal, "Cape");
                cape.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

                // Lowered hood resting on back of shoulders
                Prim.Make(PrimitiveType.Sphere, slot, new Vector3(0f, 0.12f, -0.20f), new Vector3(0.55f, 0.22f, 0.30f), Palette.CloakTeal, "LoweredHood");

                // Wooden clasp / button on chest
                Prim.Make(PrimitiveType.Sphere, slot, new Vector3(0f, 0.06f, 0.22f), new Vector3(0.09f, 0.09f, 0.09f), Palette.Wood, "Clasp");
                return true;
            }

            if (id == RoseDressId)
            {
                // Flared 2-tier bell skirt
                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, -0.12f, 0f), new Vector3(0.66f, 0.22f, 0.66f), Palette.Rose, "SkirtTier1");
                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, -0.23f, 0f), new Vector3(0.74f, 0.04f, 0.74f), Palette.Sand, "LaceTrim1");

                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, -0.32f, 0f), new Vector3(0.88f, 0.24f, 0.88f), Palette.Rose, "SkirtTier2");
                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, -0.44f, 0f), new Vector3(0.96f, 0.05f, 0.96f), Palette.Sand, "LaceTrim2");

                // Sand belt / ribbon
                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, 0.02f, 0f), new Vector3(0.52f, 0.06f, 0.52f), Palette.Sand, "DressBelt");
                return true;
            }

            return false;
        }

        public static bool BuildStaff(Transform slot, string id, out Transform tip)
        {
            tip = null;
            if (string.IsNullOrEmpty(id)) return false;

            if (id == SproutStaffId)
            {
                // Curved wooden staff
                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, 0.35f, 0f), new Vector3(0.06f, 0.95f, 0.06f), Palette.Wood, "Shaft");

                // Wooden crook curve
                GameObject crook = Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0.08f, 0.88f, 0f), new Vector3(0.06f, 0.22f, 0.06f), Palette.Wood, "Crook");
                crook.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);

                // Sprout central bud
                Prim.Make(PrimitiveType.Sphere, slot, new Vector3(0.15f, 1.02f, 0f), new Vector3(0.18f, 0.22f, 0.18f), Palette.LeafGreen, "SproutBud");

                // 3 small sprout leaves
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * 120f * Mathf.Deg2Rad;
                    Vector3 leafPos = new Vector3(0.15f + Mathf.Cos(angle) * 0.12f, 1.06f, Mathf.Sin(angle) * 0.12f);
                    Prim.Make(PrimitiveType.Sphere, slot, leafPos, new Vector3(0.08f, 0.14f, 0.08f), Palette.LeafGreen, "Leaf_" + i);
                }

                GameObject tipGo = new GameObject("StaffTip");
                tipGo.transform.SetParent(slot, false);
                tipGo.transform.localPosition = new Vector3(0.15f, 1.18f, 0f);
                tip = tipGo.transform;
                return true;
            }

            if (id == SunrayStaffId)
            {
                // Slender silver metallic shaft
                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, 0.40f, 0f), new Vector3(0.045f, 1.05f, 0.045f), Palette.Silver, "Shaft");

                // Glowing sun sphere at top
                GameObject orb = Prim.Make(PrimitiveType.Sphere, slot, new Vector3(0f, 0.98f, 0f), new Vector3(0.24f, 0.24f, 0.24f), Palette.LightGold, "SunOrb");
                var mr = orb.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = MaterialLibrary.Unlit(Palette.LightGold);

                // Halo ray ring around orb
                Prim.Make(PrimitiveType.Cylinder, slot, new Vector3(0f, 0.98f, 0f), new Vector3(0.42f, 0.03f, 0.42f), Palette.LightGold, "HaloRing");

                // 4 ray spikes
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * 90f * Mathf.Deg2Rad;
                    Vector3 spikePos = new Vector3(Mathf.Sin(angle) * 0.28f, 0.98f, Mathf.Cos(angle) * 0.28f);
                    GameObject spike = Prim.Make(PrimitiveType.Cylinder, slot, spikePos, new Vector3(0.035f, 0.10f, 0.035f), Palette.LightGold, "Spike_" + i);
                    spike.transform.localRotation = Quaternion.Euler(0f, 0f, -i * 90f);
                }

                GameObject tipGo = new GameObject("StaffTip");
                tipGo.transform.SetParent(slot, false);
                tipGo.transform.localPosition = new Vector3(0f, 1.18f, 0f);
                tip = tipGo.transform;
                return true;
            }

            return false;
        }
    }
}
