# Workstream Organization

This memo keeps the current work split readable when the repository has multiple active threads.

## Current Layout

- `X:\Dev\OpenGSR-main`
  - Clean `main` worktree
  - Use this for direct `main` development
  - Keep it small, current, and ready to push
- `X:\Dev\OpenGSR`
  - Older worktree
  - Contains the broader mixed rescue branch work
  - Do not assume it is ready for `main` without review

## Active Workstreams

### 1. Lobby / Waitroom UI Wiring

Files in this lane are the ones that make the room flow usable:

- `Assets/Scenes/Lobby/LobbyScene.unity`
- `Assets/Scripts/Scene/OnlineLobbyScene.cs`
- `Assets/Scripts/Scene/OnlineWaitRoomScene.cs`
- `Assets/Scripts/Network/GeneralServerNetworkManager.cs`
- `Assets/Editor/CharacterSelectDialogPrefabBuilder.cs`
- `Assets/Prefabs/UI/CreateRoomDialog.prefab`
- `Assets/Prefabs/UI/RoomSlot.prefab`

### 2. Runtime Cleanup / Behavior Simplification

These changes are mostly code cleanup, dead code removal, and small behavior alignment:

- `Assets/Scripts/Weapon/**`
- `Assets/Scripts/Network/LagCompensation/ClientPositionReceiver.cs`
- `Assets/Scripts/Item/FieldItemNetworkManager.cs`
- `Assets/Scripts/Manager/AsmExport/OnlineLoadingManager.cs`
- `Assets/Scripts/Core/SaveFile.cs`
- `Assets/Scripts/BaseLib/GameSetting.cs`
- `Assets/Scripts/Editor/AssemblyCompileTimeWindow.cs`

### 3. Deleted or Retired Gameplay Files

These were removed after confirming they were not referenced in the current flow:

- `Assets/Scripts/Match/SUVMatchMainScript.cs`
- `Assets/Scripts/Player/CTFAIPlayer.cs`
- `Assets/Scripts/Weapon/Controller/Grenade/MagneticGrenadeController.cs`
- `Assets/Scripts/Weapon/Controller/Grenade/MineGrenadeController.cs`
- `Assets/Scripts/Weapon/FireGrenadeController.cs`

### 4. Editor / Asset Noise

These are often generated or environment-specific, so treat them carefully:

- `Assets/Sprites/Channel/*.png.meta`
- `Assets/TextMesh Pro/**`
- `OpenGSR.slnx`
- `ProjectSettings/Packages/`
- `UserSettings/AI.Generators/`
- `Logs/`

## Rule Of Thumb

- Keep `main` for the current clean baseline.
- If a diff touches more than one workstream, split it before committing.
- If a file is only editor/environment noise, keep it out of gameplay commits unless there is a strong reason.
