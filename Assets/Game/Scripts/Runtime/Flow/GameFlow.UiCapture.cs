#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using RHMG.Core;
using RHMG.Runtime.Combat;
using UnityEngine;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        // Short deterministic visual tour; gameplay acceptance still runs the separate T9/T10 probes.
        IEnumerator CaptureUiTour(Action<string> fail)
        {
            automationActive = true;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture("00_menu");
            // SPEC-P01 section 4: four title frames 0.5 s apart prove the key-art animation runs.
            yield return Capture("00_title");
            for (int frame = 1; frame <= 3; frame++)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Capture("00_title_" + frame);
            }
            StartRun();
            combat.GodMode = true;
            float until = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < until)
            {
                if (mode == Mode.LevelUp) ChooseLevelUpCard(run.Cards[0]);
                if (mode == Mode.Tower) combat.SetMoveInput(new Vector2(0.2f, 0.8f));
                yield return null;
            }
            combat.SetMoveInput(Vector2.zero);
            yield return Capture("01_combat");
            run.Progress.GrantFreeLevelUp();
            OpenLevelUpScreen();
            yield return Capture("02_levelup");
            ChooseLevelUpCard(run.Cards[0]);
            Pause();
            yield return Capture("06_pause");
            ContinuePause();
            DebugJumpToFloor(6);
            // Jump to the next guardian, then complete it through the actual station transition.
            DebugJumpToFloor(10);
            run.CompleteFloor();
            run.ClaimStationReward();
            EnterStation();
            pendingTileIndex = 0;
            pendingTileKind = StationTile.SpiritUp;
            ShowSpiritUpPick();
            yield return Capture("03_station");
            ReturnToStation();
            DrawWardrobe();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Capture("04_wardrobe");
            SetPaused(false);
            // SPEC-P04 section 3: one combat frame per chapter floor set (the floor-1 frame is 01_combat).
            foreach (int chapterFloor in new[] { 21, 41, 61, 81 })
            {
                DebugJumpToFloor(chapterFloor);
                EnterCombat(true);
                combat.GodMode = true;
                until = Time.realtimeSinceStartup + 2f;
                while (Time.realtimeSinceStartup < until)
                {
                    combat.SetMoveInput(new Vector2(0.2f, 0.8f));
                    yield return null;
                }
                combat.SetMoveInput(Vector2.zero);
                yield return Capture("08_combat_f" + chapterFloor);
            }
            DebugJumpToFloor(100);
            EnterCombat(true);
            combat.GodMode = true;
            // Bring the boss into the player's view while preserving its actual live AI.
            until = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < until)
            {
                combat.SetMoveInput(Vector2.up);
                yield return null;
            }
            combat.SetMoveInput(Vector2.zero);
            yield return Capture("05_boss");
            run.PlayerDied();
            DrawDeathResult();
            yield return Capture("07_result");
            if (captureFailed) fail?.Invoke("UI screenshot readback failed");
            Debug.Log("[W3] UI tour captured 8 screens language=" + Loc.Code(Loc.Current));
            automationActive = false;
            Time.timeScale = 1f;
        }
    }
}
#endif
