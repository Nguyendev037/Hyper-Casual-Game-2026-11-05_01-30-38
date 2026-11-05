using RHMG.Core;
using RHMG.Runtime.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                ForcePause();
            }
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
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
                hpText.text = "♥ " + hp + "/" + playerMaxHp + " · Tầng " +
                    (run == null ? 0 : run.CurrentFloor) + "/5 · " + save.gold + " vàng";
            }
            int seconds = Mathf.CeilToInt(clock.PhaseRemaining);
            if (clockText != null && (seconds != displayedClock || clock.Phase != displayedPhase))
            {
                displayedClock = seconds;
                displayedPhase = clock.Phase;
                clockText.text = (clock.Phase == DayPhase.Day ? "☀ Ngày " : "☾ Đêm ") + seconds + "s";
            }
            if (clockFill != null)
            {
                clockFill.fillAmount = clock.PhaseRemaining / DayNightClock.PhaseLength;
            }
        }

        void BindHudFields()
        {
            Text[] labels = root.GetComponentsInChildren<Text>(true);
            foreach (Text label in labels)
            {
                if (label.text.StartsWith("♥ ")) hpText = label;
                if (label.text.StartsWith("☀ Ngày ") || label.text.StartsWith("☾ Đêm ")) clockText = label;
            }
            Image[] images = root.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                Transform parent = image.transform.parent;
                if (parent == null) continue;
                if (parent.name == "PlayerHealth") hpFill = image;
                if (parent.name == "PhaseCountdown") clockFill = image;
                if (image.name == "Cooldown" || parent.name == "Cooldown") commandFill = image;
            }
            displayedHp = int.MinValue;
            displayedClock = int.MinValue;
            UpdateHudBars();
        }
    }
}
