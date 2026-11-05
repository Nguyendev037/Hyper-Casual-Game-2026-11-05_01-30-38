using System.Collections.Generic;
using RHMG.Core;
using RHMG.Runtime.Combat;
using RHMG.Runtime.UI;
using UnityEngine;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        SaveData showcaseSave;
        WardrobeService showcaseWardrobe;

        void TryOpenShowcase()
        {
            if (!save.unlockedSpiritIds.Contains("spirit.ember"))
            {
                Toast("Hoàn thành tầng 5 để mở");
                return;
            }
            OpenShowcase();
        }

        void OpenShowcase()
        {
            if (mode != Mode.Showcase)
            {
                showcaseReturnMode = mode;
            }
            showcaseSave = save.DeepClone();
            showcaseSave.dogSealMask = DogProgress.AllSevenMask;
            for (int index = 0; index < Catalog.Outfits.Count; index++)
            {
                string id = Catalog.Outfits[index].Id;
                if (!showcaseSave.ownedOutfitIds.Contains(id))
                {
                    showcaseSave.ownedOutfitIds.Add(id);
                }
            }
            showcaseWardrobe = new WardrobeService(showcaseSave, true);
            Log("showcase_open", "showcase");
            SetMode(Mode.Showcase);
            SetPaused(true);
            if (combat != null)
            {
                Destroy(combat.gameObject);
                combat = null;
            }
            string[] outfits = new List<string>(showcaseWardrobe.EquippedIds()).ToArray();
            combat = CombatSession.Create(modeRoot, new CombatSetup
            {
                Stats = StatsCalculator.Compute(outfits, new string[0], showcaseSave.dogSealMask, true),
                SpiritId = "spirit.steam",
                DogTier = DogTier.SunMoonLion,
                Mounted = true,
                Showcase = true,
                Clock = clock,
                EquippedOutfitIds = outfits
            });
            hasPlayerVitals = false;
            AttachDayNightView();
            combat.Running = false;
            DrawShowcaseScreen();
        }

        void DrawShowcaseScreen()
        {
            Clear();
            Label("PHÒNG TRÌNH DIỄN", 0.88f, 80);
            Label("Dữ liệu minh họa, không ảnh hưởng tiến trình", 0.75f, 40);
            ShowcaseButton("Ngày/đêm", 0, () => clock.ForceToggle());
            ShowcaseButton("Trang phục", 1, DrawWardrobe);
            ShowcaseButton("Quay lại", 2, ReturnFromShowcase);
        }

        void ShowcaseButton(string label, int column, System.Action action)
        {
            var button = UiKit.Button(root, label, new Vector2(300, 150),
                new Color(0.22f, 0.18f, 0.22f, 0.96f));
            RectTransform rect = (RectTransform)button.transform;
            float left = 0.04f + column * 0.32f;
            rect.anchorMin = new Vector2(left, 0.10f);
            rect.anchorMax = new Vector2(left + 0.28f, 0.10f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, 150f);
            rect.anchoredPosition = Vector2.zero;
            button.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 44;
            button.onClick.AddListener(() => audioHub.Play("ui"));
            button.onClick.AddListener(() => action());
            sizeAudit.Capture(root, mode.ToString());
        }

        void ReturnFromShowcase()
        {
            if (showcaseReturnMode == Mode.Menu)
            {
                if (combat != null)
                {
                    Destroy(combat.gameObject);
                    combat = null;
                }
                showcaseSave = null;
                showcaseWardrobe = null;
                wardrobe = new WardrobeService(save);
                ShowMenu();
                return;
            }
            SetMode(Mode.Rest);
            if (combat != null)
            {
                Destroy(combat.gameObject);
                combat = null;
            }
            showcaseSave = null;
            showcaseWardrobe = null;
            wardrobe = new WardrobeService(save);
            StartRestCombat();
            DrawRest();
        }

        void StartRestCombat()
        {
            string[] outfits = new List<string>(wardrobe.EquippedIds()).ToArray();
            IEnumerable<string> upgrades = run == null ? new string[0] : run.Upgrades.Taken;
            PlayerStats stats = StatsCalculator.Compute(outfits, upgrades, save.dogSealMask, false);
            playerMaxHp = Mathf.CeilToInt(stats.MaxHp);
            combat = CombatSession.Create(modeRoot, new CombatSetup
            {
                Stats = stats,
                Spirits = run == null ? null : run.Spirits.Slots,
                DogTier = DogProgress.GetTier(save.dogSealMask),
                Mounted = false,
                Showcase = false,
                Clock = clock,
                EquippedOutfitIds = outfits
            });
            hasPlayerVitals = false;
            AttachDayNightView();
            combat.SpawnDummy(combat.PlayerPosition + Vector3.forward * 3f);
            combat.Running = true;
            SetPaused(false);
        }
    }
}
