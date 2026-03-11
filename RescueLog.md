# Scene Rescue Log

## Rules
- Copy scene files with `.meta` together.
- Keep scenes organized by feature folder under `Assets/Scenes`.
- Preserve `Assets/Resources` paths for referenced assets (do not relocate).
- Record each rescue batch in this file.

## Current Structure
- `Assets/Scenes/Title/`
- `Assets/Scenes/Lobby/`
- `Assets/Scenes/Login/`

## Rescued (from old OpenGS)
- `Assets/Scenes/Title/TitleScene.unity`
- `Assets/Scenes/Title/SplashScreen.unity`
- `Assets/Scenes/Title/old.unity`
- `Assets/Scenes/Title/old2.unity`
- `Assets/Scenes/Lobby/LobbyScene.unity`
- `Assets/Scenes/Login/LoginServerScene.unity`

## Rescued dependencies
- `Assets/Resources/BGM/TitleBGM.ogg`
- `Assets/Resources/MasterData/Scene/GeneralSceneMasterData.asset`
- `Assets/Resources/MasterData/Sound/System/SystemSoundMasterData.asset`
- `Assets/Resources/Sound/WaitRoom/Popup.wav`
- `Assets/Resources/Sound/sfx_game_win.wav`
- `Assets/Resources/Sound/sfx_UI_btn_click.wav`

## Audio batch rescue
- Source: `x:/Dev/OpenGS/Assets`
- Copied audio files: `167`
- Copied meta files: `167`
- Main destinations:
  - `Assets/Resources/BGM/`
  - `Assets/Resources/Sound/`
  - `Assets/AudioManager/`

## Scene scripts rescue
- Source: `x:/Dev/OpenGS/Assets/Scripts/Scene`
- Mode: copy only missing files (no overwrite)
- Copied scripts: `34`
- Conflict overwrite: `0`
- Destination:
  - `Assets/Scripts/Scene/`
  - `Assets/Scripts/Scene/Account/`
  - `Assets/Scripts/Scene/Result/`
  - `Assets/Scripts/Scene/SceneController/`
  - `Assets/Scripts/Scene/WaitRoom/`
  - `Assets/Scripts/Scene/ExportAssets/`
