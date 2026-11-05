using System;
using System.Collections.Generic;
using RHMG.Core;
using RHMG.Runtime.Combat;
using RHMG.Runtime.UI;
using RHMG.Runtime.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    // Survivor loop (plan 04): continuous floors, spirit-stone level-ups, stations every 5 floors, checkpoints.
    public sealed partial class GameFlow
    {
        const float LevelUpWatchdogSeconds = 2f;

        /// <summary>Debug builds only: replaces the computed stats (reference build of the boss test). null in real play.</summary>
        PlayerStats debugStatsOverride;

        PlayerStats ComputeStats()
        {
            if (debugStatsOverride != null)
            {
                return debugStatsOverride;
            }

            return StatsCalculator.Compute(wardrobe.EquippedIds(), run.Upgrades.Taken, save.dogSealMask, false);
        }

        void DiscardCombat()
        {
            if (combat == null)
            {
                return;
            }

            combat.Running = false;
            combat.gameObject.SetActive(false);
            Destroy(combat.gameObject);
            combat = null;
        }

        CombatSetup MakeSetup(bool station, IList<StationTile> tiles)
        {
            CombatSetup setup = new CombatSetup();
            setup.Stats = currentStats;
            setup.Spirits = run.Spirits.Slots;
            setup.DogTier = DogProgress.GetTier(save.dogSealMask);
            setup.Mounted = false;
            setup.Showcase = false;
            setup.Station = station;
            setup.StationTiles = tiles;
            setup.Clock = clock;
            setup.EquippedOutfitIds = new List<string>(wardrobe.EquippedIds()).ToArray();
            setup.StartHp = run.Hp;
            return setup;
        }

        void SubscribeCombat()
        {
            combat.PlayerDied += OnPlayerDied;
            combat.Noticed += OnCombatNotice;
            combat.DayNightForced += OnDayNightForced;
        }

        void OnCombatNotice(string message)
        {
            Toast(message);
        }

        void OnDayNightForced()
        {
            clock.ForceToggle();
        }

        // ---------- combat floors ----------

        /// <summary>Starts the current floor. A new session is built after a station / at run start; otherwise the arena is reused.</summary>
        void EnterCombat(bool newSession)
        {
            SetMode(Mode.Tower);
            SetPaused(false);
            bool bossFloor = run != null && run.CurrentFloor >= 1 && run.CurrentFloor <= SurvivorTuning.FloorCount &&
                FloorPlan.Kind(run.CurrentFloor) != FloorKind.Normal;
            audioHub.PlayMusic(bossFloor ? "music_boss" : "music_loop");
            if (bossFloor) audioHub.Play("boss_warn");
            currentStats = ComputeStats();
            playerMaxHp = Mathf.CeilToInt(currentStats.MaxHp);
            if (combat == null || combat.IsStation)
            {
                newSession = true;
            }

            if (newSession)
            {
                DiscardCombat();
                combat = CombatSession.Create(modeRoot, MakeSetup(false, null));
                hasPlayerVitals = false;
                SubscribeCombat();
                combat.FloorCleared += OnFloorCleared;
                combat.BossHealthChanged += OnBossHealthChanged;
                combat.BossDefeatedAt += ShowCage;
                combat.XpCollected += OnXpCollected;
                AttachDayNightView();
            }
            else
            {
                combat.ApplyStats(currentStats);
                combat.SetSpirits(run.Spirits.Slots);
            }

            combat.StartFloor(run.CurrentFloor);
            combat.Running = true;
            DrawHud();
            if (run.CurrentFloor == 1 && !save.tutorialSeen && !tutorialDisplayed)
            {
                Toast("Kéo nửa dưới màn hình để di chuyển — phép tự bắn");
                tutorialDisplayed = true;
            }
        }

        void OnFloorCleared()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugAutoWin = false;
#endif
            if (run == null)
            {
                return;
            }

            if (run.Phase != RunPhase.Combat)
            {
                // A level-up screen is open (the last stone was just picked): finish the floor once it closes.
                floorClearPending = true;
                return;
            }

            try
            {
                int floor = run.CurrentFloor;
                run.Hp = combat.PlayerHp;
                int floorReward = run.CompleteFloor();
                Log("floor_clear", floor + ":" + floorReward);
                SaveNow();
                if (run.Phase == RunPhase.Rescue)
                {
                    Log("run_complete", run.RunId);
                    ShowFinalRescue();
                }
                else if (run.Phase == RunPhase.Station)
                {
                    ShowStationEntry();
                }
                else
                {
                    EnterCombat(false);
                }

                // After the screen change: SetMode clears pending toasts.
                Toast(Loc.F("+{0} vàng", floorReward));
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogException(exception);
            }
        }

        // ---------- spirit stones and level-up ----------

        void OnXpCollected(int value)
        {
            if (run == null || (run.Phase != RunPhase.Combat && run.Phase != RunPhase.LevelUp))
            {
                return;
            }

            GemPickupsThisRun++;
            run.Progress.AddXp(value, currentStats == null ? 1f : currentStats.XpGainMult);
            if (run.Progress.PendingLevelUps > 0 && run.Phase == RunPhase.Combat && mode == Mode.Tower)
            {
                OpenLevelUpScreen();
            }
        }

        void OpenLevelUpScreen()
        {
            if (mode == Mode.LevelUp || run.Progress.PendingLevelUps <= 0)
            {
                return;
            }

            audioHub.Play("levelup");

            levelUpReturnMode = mode;
            run.OpenLevelUp();
            SetMode(Mode.LevelUp);
            SetPaused(true);
            levelUpOpenedAt = Time.unscaledTime;
            levelUpShown = false;
            choosing = false;
            DrawLevelUp();
            levelUpShown = true;
        }

        void DrawLevelUp()
        {
            Clear();
            if (DrawLayoutCards()) return;
            AddListPanel();
            Label(Loc.F("Lên cấp! Chọn một thẻ", run.Progress.Level), 0.80f, 64);
            IReadOnlyList<LevelUpCard> cards = run.Cards;
            for (int index = 0; index < cards.Count; index++)
            {
                LevelUpCard card = cards[index];
                string iconId;
                string title;
                string body;
                DescribeCard(card, out iconId, out title, out body);
                var button = UiKit.Card(root, iconId, title, body, new Vector2(880, 300));
                RectTransform rect = (RectTransform)button.transform;
                float centerY = 0.65f - index * 0.19f;
                rect.anchorMin = new Vector2(0.09f, centerY);
                rect.anchorMax = new Vector2(0.91f, centerY);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(0f, 300f);
                rect.anchoredPosition = Vector2.zero;
                button.onClick.AddListener(() => audioHub.Play("ui"));
                button.onClick.AddListener(() => ChooseLevelUpCard(card));
            }
            sizeAudit.Capture(root, mode.ToString());
        }

        void DescribeCard(LevelUpCard card, out string iconId, out string title, out string body)
        {
            iconId = UiSkin.IconIdFor(card.TargetId);
            switch (card.Kind)
            {
                case CardKind.Stat:
                    {
                        UpgradeDefinition def = Catalog.GetUpgrade(card.TargetId);
                        int stack = run.Upgrades.StackCount(card.TargetId) + 1;
                        title = Loc.T(def.Title);
                        body = Loc.T(def.Description) + "\nLv " + stack + "/" + def.MaxStacks;
                        break;
                    }
                case CardKind.SpiritNew:
                case CardKind.SpiritUp:
                    {
                        SpiritDefinition def = Catalog.GetSpirit(card.TargetId);
                        int level = run.Spirits.LevelOf(card.TargetId);
                        title = SpiritName(card.TargetId);
                        string lv = card.Kind == CardKind.SpiritNew ? "Lv 1" : "Lv " + level + " > " + (level + 1);
                        body = Loc.T(def.AutoText) + "\n" + Loc.T(def.CommandText) + "\n" + lv;
                        break;
                    }
                default:
                    {
                        UpgradeDefinition def = Catalog.GetUpgrade(Catalog.HealFallbackId);
                        title = Loc.T(def.Title);
                        body = Loc.T(def.Description);
                        iconId = null;
                        break;
                    }
            }
        }

        void ChooseLevelUpCard(LevelUpCard card)
        {
            if (choosing || run == null || run.Phase != RunPhase.LevelUp)
            {
                return;
            }

            choosing = true;
            try
            {
                run.ChooseCard(card);
                LevelUpsThisRun++;
                Log("level_up", card.Id);
                if (card.Kind == CardKind.Heal)
                {
                    combat?.Heal(Catalog.HealFallbackAmount);
                }

                if (run.Phase == RunPhase.LevelUp)
                {
                    choosing = false;
                    DrawLevelUp();
                    return;
                }

                ResumeAfterLevelUp();
            }
            catch (Exception exception)
            {
                choosing = false;
                Debug.LogException(exception);
            }
        }

        void ResumeAfterLevelUp()
        {
            choosing = false;
            currentStats = ComputeStats();
            playerMaxHp = Mathf.CeilToInt(currentStats.MaxHp);
            combat.ApplyStats(currentStats);
            combat.SetSpirits(run.Spirits.Slots);
            if (levelUpReturnMode == Mode.Station)
            {
                ReturnToStation();
                return;
            }

            SetMode(Mode.Tower);
            SetPaused(false);
            DrawHud();
            if (floorClearPending)
            {
                floorClearPending = false;
                OnFloorCleared();
            }
            else if (run.Progress.PendingLevelUps > 0)
            {
                OpenLevelUpScreen();
            }
        }

        // SPEC-P01 §7: timeScale is 0 while picking; if the card screen failed to appear, pick the first card.
        void CheckLevelUpWatchdog()
        {
            if (run == null || run.Phase != RunPhase.LevelUp || levelUpShown || mode != Mode.LevelUp)
            {
                return;
            }

            if (Time.unscaledTime - levelUpOpenedAt > LevelUpWatchdogSeconds && run.Cards.Count > 0)
            {
                Debug.LogError("[Flow] Level-up screen did not appear within 2 s, picking the first card");
                levelUpShown = true;
                ChooseLevelUpCard(run.Cards[0]);
            }
        }

        // ---------- stations ----------

        void ShowStationEntry()
        {
            string rescue = run.StationRescueSpiritId;
            if (!string.IsNullOrEmpty(rescue))
            {
                ShowStationRescue(rescue);
                return;
            }

            run.ClaimStationReward();
            EnterStation();
            if (run.CurrentFloor >= SurvivorTuning.FirstNoRescueStation)
            {
                Toast(Loc.F("+{0} vàng", SurvivorTuning.StationGoldNoRescue));
            }
        }

        void ShowStationRescue(string spiritId)
        {
            SetMode(Mode.Rescue);
            SetPaused(true);
            Clear();
            SpiritDefinition def = Catalog.GetSpirit(spiritId);
            Label(Loc.F("Lồng của {0} · {1}", SpiritName(spiritId), ElementName(def.Element)), 0.69f, 64);
            Label("Hộ vệ đã gục ngã. Mở lồng để giải cứu tinh linh.", 0.54f, 32);
            Button("Mở lồng", 0.39f, () => OpenStationCage(spiritId));
        }

        void OpenStationCage(string spiritId)
        {
            string rescued = null;
            try
            {
                rescued = run.ClaimStationReward();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogException(exception);
            }

            if (rescued != null)
            {
                Log("spirit_rescue", rescued);
                combat?.SetDogTier(DogProgress.GetTier(save.dogSealMask));
                combat?.SetSpirits(run.Spirits.Slots);
                audioHub.Play("rescue");
            }

            SaveNow();
            Clear();
            SpiritDefinition def = Catalog.GetSpirit(spiritId);
            Label(Loc.F("{0} được giải cứu!\nCún nhận ấn {1} — vòng cổ phát sáng", SpiritName(spiritId),
                ElementName(def.Element)), 0.57f, 64);
            Button("Tiếp", 0.25f, EnterStation);
        }

        void EnterStation()
        {
            SetMode(Mode.Station);
            audioHub.PlayMusic("music_calm");
            currentStats = ComputeStats();
            playerMaxHp = Mathf.CeilToInt(currentStats.MaxHp);
            List<StationTile> tiles = run.RollStationTiles(new SeededRng(seed + run.CurrentFloor * 131));
            DiscardCombat();
            combat = CombatSession.Create(modeRoot, MakeSetup(true, tiles));
            hasPlayerVitals = false;
            SubscribeCombat();
            combat.StationTileActivated += OnStationTile;
            combat.StationExit += OnStationExit;
            AttachDayNightView();
            stationTileUsed = false;
            SetPaused(false);
            combat.Running = true;
            DrawStationHud();
        }

        bool stationTileUsed;

        void DrawStationHud()
        {
            Clear();
            DrawJoystickArea();
            DrawTopHud("PlayerHealth", false);
            Text hint = HudText("HudHint", new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(1000f, 120f),
                TextAnchor.MiddleCenter, Color.white);
            hint.fontSize = 36;
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            hint.text = Loc.T("Đứng trên một ô 1 giây để dùng, đi vào cổng để rời trạm");
            UpdateHudBars();
        }

        void ReturnToStation()
        {
            SetMode(Mode.Station);
            SetPaused(false);
            DrawStationHud();
        }

        void OnStationTile(int index, StationTile kind)
        {
            if (mode != Mode.Station || run == null || run.Phase != RunPhase.Station || stationTileUsed)
            {
                return;
            }

            audioHub.Play("station_tile");
            pendingTileIndex = index;
            pendingTileKind = kind;
            switch (kind)
            {
                case StationTile.Fusion:
                    {
                        List<FusionRecipe> recipes = run.Spirits.FusableRecipes();
                        if (recipes.Count == 0)
                        {
                            Toast("Không còn cặp dung hợp");
                            combat.HealFraction(SurvivorTuning.StationHealFraction);
                            CompleteTile(index);
                        }
                        else
                        {
                            ShowFusionPick(recipes);
                        }

                        break;
                    }
                case StationTile.SpiritUp:
                    ShowSpiritUpPick();
                    break;
                case StationTile.Heal:
                    combat.HealFraction(SurvivorTuning.StationHealFraction);
                    audioHub.Play("rescue");
                    CompleteTile(index);
                    break;
                case StationTile.XpChest:
                    {
                        int ups = run.OpenXpChest();
                        CompleteTile(index);
                        if (ups > 0)
                        {
                            OpenLevelUpScreen();
                        }

                        break;
                    }
                default:
                    run.OpenCardChest();
                    CompleteTile(index);
                    OpenLevelUpScreen();
                    break;
            }
        }

        void CompleteTile(int index)
        {
            stationTileUsed = true;
            if (combat != null && combat.Station != null)
            {
                combat.Station.MarkUsed(index);
            }

            Log("station_tile", pendingTileKind.ToString());
        }

        void ShowFusionPick(List<FusionRecipe> recipes)
        {
            SetMode(Mode.StationPick);
            SetPaused(true);
            Clear();
            var titles = new List<string>();
            var descriptions = new List<string>();
            var icons = new List<Sprite>();
            var actions = new List<Action>();
            foreach (FusionRecipe recipe in recipes)
            {
                FusionRecipe selected = recipe;
                titles.Add(SpiritName(recipe.OutputId));
                descriptions.Add(SpiritName(recipe.IngredientA) + " + " + SpiritName(recipe.IngredientB));
                icons.Add(UiSkin.Icon(UiSkin.IconIdFor(recipe.OutputId)));
                actions.Add(() => DoFusion(selected));
            }
            if (DrawLayoutStation(titles, descriptions, icons, actions, "Chọn cặp dung hợp")) return;
            AddListPanel();
            Label("Chọn cặp dung hợp", 0.78f, 64);
            for (int i = 0; i < recipes.Count; i++)
            {
                FusionRecipe recipe = recipes[i];
                string text = SpiritName(recipe.IngredientA) + " + " + SpiritName(recipe.IngredientB) + " > " +
                    SpiritName(recipe.OutputId);
                Button(text, 0.60f - i * 0.12f, () => DoFusion(recipe));
            }

            Button("Quay lại", 0.14f, ReturnToStation);
        }

        void DoFusion(FusionRecipe recipe)
        {
            if (run.Spirits.Fuse(recipe.Id))
            {
                Log("fusion_confirm", recipe.Id);
                combat.SetSpirits(run.Spirits.Slots);
                audioHub.Play("fusion");
                combat.PlayBurstAtPlayer(Palette.LightGold, 3f);
                CompleteTile(pendingTileIndex);
            }
            else
            {
                Toast("Không còn cặp dung hợp");
                combat.HealFraction(SurvivorTuning.StationHealFraction);
                CompleteTile(pendingTileIndex);
            }

            ReturnToStation();
        }

        void ShowSpiritUpPick()
        {
            SetMode(Mode.StationPick);
            SetPaused(true);
            Clear();
            var titles = new List<string>();
            var descriptions = new List<string>();
            var icons = new List<Sprite>();
            var actions = new List<Action>();
            foreach (RunSpiritEntry entry in run.Spirits.Slots)
            {
                if (entry.level >= SurvivorTuning.SpiritMaxLevel) continue;
                string selected = entry.spiritId;
                titles.Add(SpiritName(selected));
                descriptions.Add("Lv " + entry.level + " > " + (entry.level + 1));
                icons.Add(UiSkin.Icon(UiSkin.IconIdFor(selected)));
                actions.Add(() => DoSpiritUp(selected));
            }
            if (DrawLayoutStation(titles, descriptions, icons, actions, "Chọn tinh linh để thăng cấp")) return;
            AddListPanel();
            Label("Chọn tinh linh để thăng cấp", 0.78f, 64);
            int row = 0;
            foreach (RunSpiritEntry entry in run.Spirits.Slots)
            {
                if (entry.level >= SurvivorTuning.SpiritMaxLevel)
                {
                    continue;
                }

                string id = entry.spiritId;
                string text = SpiritName(id) + "  Lv " + entry.level + " > " + (entry.level + 1);
                Button(text, 0.60f - row * 0.12f, () => DoSpiritUp(id));
                row++;
            }

            Button("Quay lại", 0.14f, ReturnToStation);
        }

        void DoSpiritUp(string spiritId)
        {
            if (run.Spirits.LevelUp(spiritId))
            {
                combat.SetSpirits(run.Spirits.Slots);
                audioHub.Play("fusion");
                CompleteTile(pendingTileIndex);
            }

            ReturnToStation();
        }

        void OnStationExit()
        {
            if (mode != Mode.Station || run == null || run.Phase != RunPhase.Station)
            {
                return;
            }

            if (run.Progress.PendingLevelUps > 0)
            {
                return;
            }

            try
            {
                run.Hp = combat.PlayerHp;
                run.LeaveStation();
                SaveNow();
                StationsLeftThisRun++;
                Log("station_leave", run.CurrentFloor.ToString());
                EnterCombat(true);
                Toast(Loc.F("Tầng {0}", run.CurrentFloor));
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogException(exception);
            }
        }

        void ShowFinalRescue()
        {
            SetMode(Mode.Rescue);
            SetPaused(true);
            Clear();
            audioHub.Play("victory");
            audioHub.PlayMusic("music_calm");
            Label("Anh trai được giải thoát!", 0.69f, 64);
            Label("Lõi tháp vỡ vụn. Chuyến đi đã hoàn tất.", 0.54f, 32);
            Button("Tiếp", 0.39f, CompleteRescue);
        }
    }
}
