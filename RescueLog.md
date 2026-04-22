# Scene Rescue Log

## Rules
- Copy scene files with `.meta` together.
- Keep scenes organized by feature folder under `Assets/Scenes`.
- Preserve `Assets/Resources` paths for referenced assets (do not relocate).
- Record each rescue batch in this file.

## Current Structure
- `Assets/Scenes/`
- `Assets/Scenes/Title/`
- `Assets/Scenes/Lobby/`
- `Assets/Scenes/Login/`
- `Assets/Scenes/Loading/`
- `Assets/Scenes/Map/`
- `Assets/Scenes/Result/`
- `Assets/Scenes/Setting/`
- `Assets/Scenes/Shop/`
- `Assets/Scenes/SingleMode/`
- `Assets/Scenes/Waitroom/`

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

## Script rescue batch
- Source: `x:/Dev/OpenGS/Assets/Scripts`
- Mode: copy only missing files (no overwrite)
- Copied scripts/meta: `355`
- Destination:
  - `Assets/Scripts/`

## Scene and resource rescue batch
- Source: `x:/Dev/OpenGS/Assets`
- Scope: `Assets/Scenes`, `Assets/Resources`, `Assets/Settings`
- Mode: copy only missing files (no overwrite)
- Copied files: `324`
- Destination:
  - `Assets/Scenes/`
  - `Assets/Resources/`
  - `Assets/Settings/`

## Scene rescue follow-up batch
- Source: `x:/Dev/OpenGS/Assets/Scenes`
- Scope: missing scene and lighting files only
- Mode: copy only missing files (no overwrite)
- Copied scene files: `65`
- Cleanup: removed `6` duplicate root scene copies that already had feature-folder versions
- Destination:
  - `Assets/Scenes/`
