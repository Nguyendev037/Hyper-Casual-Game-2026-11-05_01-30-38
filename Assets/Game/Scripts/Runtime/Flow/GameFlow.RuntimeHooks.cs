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
            if (mode == Mode.Tower || mode == Mode.Rest || mode == Mode.Showcase)
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
            Time.timeScale = paused ? 0f : 1f;
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
            if (commandFill != null)
            {
                commandFill.fillAmount = combat.CommandCooldown01;
            }
            int hp = Mathf.CeilToInt(playerHp);
            if (hpText != null && hp != displayedHp)
            {
                displayedHp = hp;
                hpText.text = hp + "/" + playerMaxHp;
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
