using RHMG.Core;
using RHMG.Runtime.Combat;
using RHMG.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        // Set while an automated run drives the game: losing window focus (e.g. a capture build
        // started from a terminal) must not open the pause screen mid-floor.
        bool automationActive;

        void OnApplicationPause(bool paused)
        {
            if (paused && !automationActive)
            {
                ForcePause();
            }
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused && !automationActive)
            {
                ForcePause();
            }
        }

        void ForcePause()
        {
            // Menu books and already-paused overlays have no live simulation to suspend.
            if (combat == null || !combat.Running) return;
            if (mode == Mode.Tower || mode == Mode.Station || mode == Mode.Rest || mode == Mode.Showcase)
            {
                Pause();
            }
        }

        void SetPaused(bool paused)
        {
            if (combat != null)
            {
                combat.Running = !paused;
            }
            clock.Paused = paused;
            Time.timeScale = paused ? 0f : RunTimeScale;
        }

        void StopCombat()
        {
            if (combat != null)
            {
                combat.Running = false;
                Destroy(combat.gameObject);
                combat = null;
            }
            clock.Paused = true;
        }

        void OnPhaseChanged(DayPhase phase)
        {
            combat?.SetLionForm(phase);
            audioHub.Play("transform");
            Toast(phase == DayPhase.Night
                ? "Đêm xuống — Thủy & Bóng Tối +10%"
                : "Bình minh — Hỏa & Ánh Sáng +10%");
        }

        void UpdateHudBars()
        {
            if (combat == null)
            {
                return;
            }
            float playerHp = combat.PlayerHp;
            float playerShield = combat.PlayerShield;
            if (hasPlayerVitals)
            {
                float damage = Mathf.Max(0f, lastPlayerHp - playerHp) + Mathf.Max(0f, lastPlayerShield - playerShield);
                audioHub.OnStrongHit(damage);
            }
            lastPlayerHp = playerHp;
            lastPlayerShield = playerShield;
            hasPlayerVitals = true;
            if (hpFill != null)
            {
                hpFill.fillAmount = combat.PlayerHp01;
            }
            int hp = Mathf.CeilToInt(playerHp);
            if (hpText != null && hp != displayedHp)
            {
                displayedHp = hp;
                hpText.text = hp + "/" + playerMaxHp;
            }
            if (run != null)
            {
                int level = run.Progress.Level;
                if (levelText != null && level != displayedLevel)
                {
                    displayedLevel = level;
                    levelText.text = "Lv " + level;
                }
                if (xpFill != null)
                {
                    float xp01 = Mathf.Clamp01(run.Progress.Xp / (float)XpCurve.Required(level));
                    if (!Mathf.Approximately(xp01, displayedXp))
                    {
                        displayedXp = xp01;
                        xpFill.anchorMax = new Vector2(xp01, 1f);
                    }
                }
            }
            if (timeText != null)
            {
                int left = combat.FloorTimed ? Mathf.CeilToInt(combat.TimeLeft) :
                    approvedHud ? Mathf.CeilToInt(clock.PhaseRemaining) : -1;
                if (left != displayedTime || approvedHud && displayedPhase != clock.Phase)
                {
                    displayedTime = left;
                    string remaining = left < 0 ? string.Empty : (left / 60) + ":" + (left % 60).ToString("00");
                    timeText.text = approvedHud ? Loc.T(clock.Phase == DayPhase.Day ? "Ngày" : "Đêm") + " " + remaining : remaining;
                    if (approvedHud) displayedPhase = clock.Phase;
                }
            }
            int floor = run == null ? 0 : run.CurrentFloor;
            if (floorText != null && floor != displayedFloor)
            {
                displayedFloor = floor;
                floorText.text = Loc.F("Tầng {0}/{1}", floor, Catalog.FloorCount);
            }
            if (goldText != null && save.gold != displayedGold)
            {
                displayedGold = save.gold;
                goldText.text = save.gold.ToString();
            }
            int seconds = Mathf.CeilToInt(clock.PhaseRemaining);
            if (clockText != null && (seconds != displayedClock || clock.Phase != displayedPhase))
            {
                if (clockIcon != null && clock.Phase != displayedPhase)
                {
                    Sprite phaseIcon = UiSkin.Icon(clock.Phase == DayPhase.Day ? "icon_sun" : "icon_moon");
                    if (phaseIcon != null) clockIcon.sprite = phaseIcon;
                }
                displayedClock = seconds;
                displayedPhase = clock.Phase;
                clockText.text = Loc.F(clock.Phase == DayPhase.Day ? "Ngày {0}s" : "Đêm {0}s", seconds);
            }
            if (clockFill != null)
            {
                clockFill.fillAmount = clock.PhaseRemaining / DayNightClock.PhaseLength;
            }
        }
    }
}
