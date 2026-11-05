# Red Hair Magic Girl (Unity)

Mobile action game for Android, built with Unity 6 (URP, C#).

## Play the demo

[Download the Android demo APK (v0.5.0, 61 MB)](https://media.githubusercontent.com/media/Nguyendev037/Hyper-Casual-Game-2026-11-05_01-30-38/main/Builds/Android/RedHairMagicGirl_demo_v0.5.0.apk)

Install on an Android ARM64 device or an ARM64-compatible emulator. This is a demo for playtesting.

## Requirements
- **Unity 6000.6.8f1** (install via Unity Hub; add *Android Build Support* only if you want to build an APK).
- **Git LFS** — images and fonts are stored with LFS. Install it once: `git lfs install`.

## Get the project
```bash
git lfs install
git clone https://github.com/Nguyendev037/Hyper-Casual-Game-2026-11-05_01-30-38.git
```
If you cloned before installing LFS, run `git lfs pull` inside the folder.

## Run in the Editor
1. Unity Hub → **Add project from disk** → choose the cloned folder → open with 6000.6.8f1.
2. Wait for the first import (Library is generated locally). Project setup runs automatically; you can re-run it from the menu **RHMG → Setup Project**.
3. Open `Assets/Game/Scenes/Main.unity` and press **Play**.
4. Controls: drag on the lower half of the Game view (mouse or touch) to move; attacks are automatic. Use a portrait aspect ratio (e.g. 1080×1920) in the Game view.

## Build an Android APK
- Menu **RHMG → Build Android APK** (ARM64, IL2CPP). Output: `Builds/Android/`. Switch the Editor platform to Android first (File → Build Profiles); a build guard stops builds made from another active platform, because URP would strip the lit shaders (floor and walls disappear).
- Command line:
  ```bash
  Unity -batchmode -quit -projectPath "<project folder>" -buildTarget Android -executeMethod RHMG.EditorTools.BuildAndroid.Build
  ```

## Tests
- Core rules (plain C#, no Unity needed): `dotnet test CoreTests/RHMG.CoreTests.csproj`
- PlayMode: Unity **Window → General → Test Runner → PlayMode**.

## Licenses
Fonts in `Assets/Game/Resources/RHMG/Fonts/` are under the SIL Open Font License (see the OFL files next to them).
