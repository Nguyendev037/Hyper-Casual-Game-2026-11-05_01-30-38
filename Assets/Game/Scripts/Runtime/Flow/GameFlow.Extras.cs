using System.Collections.Generic;
using RHMG.Core;
using RHMG.Runtime.Combat;
using UnityEngine;

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
            AddListPanel();
            Label("Cài đặt", 0.81f);
            Button("Nhạc: " + (save.musicOn ? "Bật" : "Tắt"), 0.60f, ToggleMusic);
            Button("Âm thanh: " + (save.sfxOn ? "Bật" : "Tắt"), 0.49f, ToggleSfx);
            Button("Rung: " + (save.vibrationOn ? "Bật" : "Tắt"), 0.38f, ToggleVibration);
            Button("Xóa dữ liệu", 0.27f, ConfirmDeleteData);
            Button("Quay lại", 0.16f, () =>
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
            save = SaveData.CreateDefault();
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
                "Thiết kế & lập trình: KITTS Games (nhóm demo)\n" +
                "Hình khối, âm thanh tổng hợp bằng code — không dùng asset bên thứ ba\n" +
                "Unity 6000.6.8f1\nFont: font hệ thống thiết bị",
                0.42f,
                40);
            Button("Quay lại", 0.2f, ShowMenu);
        }
    }
}
