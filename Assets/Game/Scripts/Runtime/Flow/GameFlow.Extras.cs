using System.Collections.Generic;
using RHMG.Core;
using RHMG.Runtime.Combat;
using RHMG.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RHMG.Runtime
{
    public sealed partial class GameFlow
    {
        void ShowSettings()
        {
            deleteArmed = false;
            bool fromPause = mode == Mode.Pause;
            SetMode(fromPause ? Mode.Pause : Mode.Menu);
            Clear();
            if (!fromPause)
            {
                Image backdrop = UiKit.Background(root, "bg_tower");
                RectTransform tint = UiKit.Panel(root, "TowerTint", Vector2.zero, Vector2.one,
                    new Color(0.15f, 0.20f, 0.32f, 0.35f));
                tint.SetAsFirstSibling();
                backdrop.transform.SetAsFirstSibling();
            }
            AddListPanel();
            Label("Cài đặt", 0.81f, 80);
            Button(Loc.F("Ngôn ngữ: {0}", Loc.NativeName(Loc.Current)), 0.66f, CycleLanguage);
            Button(Loc.F("Nhạc: {0}", Loc.T(save.musicOn ? "Bật" : "Tắt")), 0.56f, ToggleMusic);
            Button(Loc.F("Âm thanh: {0}", Loc.T(save.sfxOn ? "Bật" : "Tắt")), 0.46f, ToggleSfx);
            Button(Loc.F("Rung: {0}", Loc.T(save.vibrationOn ? "Bật" : "Tắt")), 0.36f, ToggleVibration);
            Button("Xóa dữ liệu", 0.26f, ConfirmDeleteData);
            Button("Quay lại", 0.15f, () =>
            {
                if (fromPause)
                {
                    OpenPause();
                }
                else
                {
                    ShowMenu();
                }
            });
        }

        // En → Vi → Ko → En, saved immediately; every screen is rebuilt on navigation so redraw is enough.
        void CycleLanguage()
        {
            ApplyLanguage(Loc.Next(Loc.Current));
            SaveNow();
            ShowSettings();
        }

        public void ApplyLanguage(Language language)
        {
            Loc.Current = language;
            save.language = Loc.Code(language);
            if (toastText != null) toastText.font = UiSkin.Body;
        }

        void ToggleMusic()
        {
            save.musicOn = !save.musicOn;
            audioHub.MusicOn = save.musicOn;
            audioHub.StartMusic();
            SaveNow();
            ShowSettings();
        }

        void ToggleSfx()
        {
            save.sfxOn = !save.sfxOn;
            audioHub.SfxOn = save.sfxOn;
            SaveNow();
            ShowSettings();
        }

        void ToggleVibration()
        {
            save.vibrationOn = !save.vibrationOn;
            audioHub.VibrationOn = save.vibrationOn;
            SaveNow();
            ShowSettings();
        }

        void ConfirmDeleteData()
        {
            if (!deleteArmed)
            {
                deleteArmed = true;
                Toast("Chạm Xóa dữ liệu lần nữa để xác nhận");
                return;
            }
            deleteArmed = false;
            saves.DeleteAll();
            string keepLanguage = save.language;
            save = SaveData.CreateDefault();
            save.language = keepLanguage; // deleting progress should not flip the UI language
            rewards = new RewardService(save);
            fusion = new FusionService(save, Catalog.DemoTrialSpiritIds);
            wardrobe = new WardrobeService(save);
            SaveNow();
            ShowMenu();
        }

        void ShowCredits()
        {
            SetMode(Mode.Credits);
            Clear();
            AddListPanel();
            Label("Ghi công", 0.8f, 64);
            Label(
                Loc.T("Thiết kế & lập trình: KITTS Games (nhóm demo)") + "\n" +
                Loc.T("Ảnh 2D tạo bằng AI; mô hình 3D và âm thanh tổng hợp bằng code") + "\n" +
                "Unity 6000.6.8f1\nFonts: Be Vietnam Pro, Noto Sans KR (SIL OFL 1.1)",
                0.42f,
                40);
            Button("Quay lại", 0.2f, ShowMenu);
        }
    }
}
