using UnityEngine;
using RHMG.Core;

namespace RHMG.Runtime.Visuals
{
    /// <summary>Visual showcase gallery displaying all characters, 8 costume combinations, companions, enemies, and environments.</summary>
    public sealed class VisualGallery : MonoBehaviour
    {
        [SerializeField] private bool m_BuildOnStart = true;

        private void Start()
        {
            if (m_BuildOnStart)
            {
                BuildGallery();
            }
        }

        [ContextMenu("Build Gallery")]
        public void BuildGallery()
        {
            SetupLightingAndCamera();
            BuildOutfitShowcase();
            BuildDogShowcase();
            BuildCastAndEnemiesShowcase();
            BuildVfxAndArenasShowcase();
        }

        private void SetupLightingAndCamera()
        {
            // Directional Light
            var lights = FindObjectsByType<Light>();
            bool hasDir = false;
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional) { hasDir = true; break; }
            }
            if (!hasDir)
            {
                GameObject lightGo = new GameObject("GalleryLight");
                lightGo.transform.SetParent(transform, false);
                Light light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.96f, 0.90f);
                light.intensity = 1.15f;
                lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            }

            // Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("GalleryCamera");
                camGo.transform.SetParent(transform, false);
                cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = 12f;
            cam.transform.position = new Vector3(0f, 18f, -16f);
            cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        }

        private void BuildOutfitShowcase()
        {
            Transform rowRoot = new GameObject("Row1_Outfits_8_Combinations").transform;
            rowRoot.SetParent(transform, false);

            string[] hats = { CostumeBuilder.AshHatId, CostumeBuilder.DawnClipId };
            string[] bodies = { CostumeBuilder.JourneyCloakId, CostumeBuilder.RoseDressId };
            string[] staffs = { CostumeBuilder.SproutStaffId, CostumeBuilder.SunrayStaffId };

            int index = 0;
            for (int h = 0; h < 2; h++)
            {
                for (int b = 0; b < 2; b++)
                {
                    for (int s = 0; s < 2; s++)
                    {
                        float x = -10.5f + index * 3.0f;
                        GirlRig girl = CharacterFactory.CreateGirl(rowRoot);
                        girl.name = $"Girl_Comb_{index}_{hats[h]}_{bodies[b]}_{staffs[s]}";
                        girl.transform.localPosition = new Vector3(x, 0f, 5f);

                        girl.ApplyOutfit(OutfitSlot.Head, hats[h]);
                        girl.ApplyOutfit(OutfitSlot.Body, bodies[b]);
                        girl.ApplyOutfit(OutfitSlot.Staff, staffs[s]);

                        index++;
                    }
                }
            }
        }

        private void BuildDogShowcase()
        {
            Transform rowRoot = new GameObject("Row2_Dogs_Tiers_And_Forms").transform;
            rowRoot.SetParent(transform, false);

            // Tier 0: Companion
            DogRig dog0 = CharacterFactory.CreateDog(rowRoot);
            dog0.name = "Dog_Tier0_Companion";
            dog0.transform.localPosition = new Vector3(-10.0f, 0f, 0f);
            dog0.SetTier(DogTier.Companion);

            // Tier 1: SandPup (glowing collar)
            DogRig dog1 = CharacterFactory.CreateDog(rowRoot);
            dog1.name = "Dog_Tier1_SandPup";
            dog1.transform.localPosition = new Vector3(-6.5f, 0f, 0f);
            dog1.SetTier(DogTier.SandPup);

            // Tier 2: SpiritHound (sand ring)
            DogRig dog2 = CharacterFactory.CreateDog(rowRoot);
            dog2.name = "Dog_Tier2_SpiritHound";
            dog2.transform.localPosition = new Vector3(-3.0f, 0f, 0f);
            dog2.SetTier(DogTier.SpiritHound);

            // Tier 3: SunMoonLion Day (Sun Lion)
            DogRig lionDay = CharacterFactory.CreateDog(rowRoot);
            lionDay.name = "Dog_Tier3_SunLion";
            lionDay.transform.localPosition = new Vector3(1.0f, 0f, 0f);
            lionDay.SetTier(DogTier.SunMoonLion);
            lionDay.SetLionForm(DayPhase.Day);

            // Tier 3: SunMoonLion Night (Moon Lion)
            DogRig lionNight = CharacterFactory.CreateDog(rowRoot);
            lionNight.name = "Dog_Tier3_MoonLion";
            lionNight.transform.localPosition = new Vector3(5.5f, 0f, 0f);
            lionNight.SetTier(DogTier.SunMoonLion);
            lionNight.SetLionForm(DayPhase.Night);

            // Mounted Girl on Sun Lion
            DogRig mountLion = CharacterFactory.CreateDog(rowRoot);
            mountLion.name = "Dog_Tier3_Mounted";
            mountLion.transform.localPosition = new Vector3(10.0f, 0f, 0f);
            mountLion.SetTier(DogTier.SunMoonLion);
            mountLion.SetLionForm(DayPhase.Day);

            GirlRig mountGirl = CharacterFactory.CreateGirl(rowRoot);
            mountGirl.name = "Girl_Mounted";
            mountGirl.SetMounted(mountLion.Seat);
        }

        private void BuildCastAndEnemiesShowcase()
        {
            Transform rowRoot = new GameObject("Row3_Spirits_Props_Enemies").transform;
            rowRoot.SetParent(transform, false);

            // Spirits
            var dew = CharacterFactory.CreateSpirit("spirit.dew", rowRoot);
            dew.transform.localPosition = new Vector3(-11.0f, 0f, -4.5f);

            var ember = CharacterFactory.CreateSpirit("spirit.ember", rowRoot);
            ember.transform.localPosition = new Vector3(-9.5f, 0f, -4.5f);

            var steam = CharacterFactory.CreateSpirit("spirit.steam", rowRoot);
            steam.transform.localPosition = new Vector3(-8.0f, 0f, -4.5f);

            // Props
            var dummy = CharacterFactory.CreateDummy(rowRoot);
            dummy.transform.localPosition = new Vector3(-5.5f, 0f, -4.5f);

            var cage = CharacterFactory.CreateCage(rowRoot);
            cage.transform.localPosition = new Vector3(-3.5f, 0f, -4.5f);

            // Enemies
            var beetle = CharacterFactory.CreateEnemy(EnemyKind.SandBeetle, rowRoot);
            beetle.transform.localPosition = new Vector3(-1.0f, 0f, -4.5f);

            var eye = CharacterFactory.CreateEnemy(EnemyKind.SandEye, rowRoot);
            eye.transform.localPosition = new Vector3(1.5f, 0f, -4.5f);

            var sentinel = CharacterFactory.CreateEnemy(EnemyKind.Sentinel, rowRoot);
            sentinel.transform.localPosition = new Vector3(4.0f, 0f, -4.5f);

            var warden = CharacterFactory.CreateEnemy(EnemyKind.CageWarden, rowRoot);
            warden.transform.localPosition = new Vector3(8.0f, 0f, -4.5f);
        }

        private void BuildVfxAndArenasShowcase()
        {
            Transform vfxRoot = new GameObject("Row4_VFX_And_Arenas").transform;
            vfxRoot.SetParent(transform, false);

            // VFX: Projectile, SpellRing, Telegraph, Burst
            var proj = VfxFactory.Projectile(Palette.WaterBlue, 0.25f);
            proj.transform.SetParent(vfxRoot, false);
            proj.transform.localPosition = new Vector3(10.5f, 1.0f, -4.0f);

            var ring = VfxFactory.SpellRing(1.8f, Palette.LightGold);
            ring.transform.SetParent(vfxRoot, false);
            ring.transform.localPosition = new Vector3(10.5f, 0.05f, -5.5f);

            var tele = VfxFactory.Telegraph(MeshBuilder.Disc(1.5f, 24));
            tele.transform.SetParent(vfxRoot, false);
            tele.transform.localPosition = new Vector3(10.5f, 0.05f, -7.5f);
            var teleView = tele.GetComponent<TelegraphView>();
            if (teleView != null) teleView.SetFill(0.7f);

            // Arenas in distant background
            var towerArena = CharacterFactory.CreateArena(vfxRoot, 10f, 10f, showcase: false);
            towerArena.transform.localPosition = new Vector3(-16f, 0f, 18f);

            var showcaseArena = CharacterFactory.CreateArena(vfxRoot, 10f, 10f, showcase: true);
            showcaseArena.transform.localPosition = new Vector3(16f, 0f, 18f);
        }
    }
}
