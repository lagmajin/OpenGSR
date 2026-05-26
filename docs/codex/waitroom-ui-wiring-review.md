# waitroom-ui-wiring review memo

Date: 2026-05-26

## Goal

Rescue only the useful parts from `codex/waitroom-ui-wiring` into `main` without reintroducing regressions.

## Current status

- `main` already has the rescued waitroom UI assets and sprite renames committed.
- The branch is still much broader than waitroom UI.
- Rough scope comparison: `main...codex/waitroom-ui-wiring` is `30 / 9` commits, with about `254 files changed`.

## Safe / useful rescue candidates

These look aligned with the waitroom flow and are worth keeping or re-checking:

- `Assets/Scripts/Match/AsmExport/ClientWaitRoom.cs`
  - `Map` field addition is coherent with room creation / room state transport.
- `Assets/Scripts/Scene/OnlineLobbyScene.cs`
  - Player character propagation on room create/join is useful.
  - Closing the dialog after send is reasonable.
- `Assets/Scripts/Scene/WaitRoom/OnlineWaitRoomSceneEvent.cs`
  - Routing `OnNewGameStarted`, `GameStartRequested`, `ReadyRequested` through dedicated methods fits the current main-side structure.
- `Assets/Scripts/Scene/WaitRoom/OnlineWaitRoomSceneServer.cs`
  - `SendGameStartRequest`, `SendReadyRequest`, `SendUnReadyRequest` are useful if they stay consistent with the current network manager.
- `Assets/Scripts/MediateObject/WaitRoomMediateObject.cs`
  - Only if it keeps current logging / lookup behavior; do not strip diagnostics casually.
- `Assets/Scripts/Match/AsmExport/MatchRoom.cs`
  - Only if it does not regress `FindObjectOfType` or remove debug paths needed for troubleshooting.

## Dangerous / avoid merging as-is

These differences are too broad or clearly roll back existing work in `main`:

- `ProjectSettings/EditorBuildSettings.asset`
  - Removes `Assets/Scenes/Waitroom/OnlineWaitRoom.unity` from build scenes.
- `Assets/Scenes/Lobby/LobbyScene.unity`
  - Reintroduces old labels / old dialog wiring / nulls `soundMasterData`.
  - Adds prefab instance changes that look like a rollback of current lobby tuning.
- `Assets/Scripts/Audio/SimpleAudioManager.cs`
  - Removes reverb support.
- `Assets/Scripts/Systems/SettingsManager.cs`
  - Removes `SoundSettings.Reverb` and its application flow.
- `Assets/Scripts/UI/SoundSettingsUI.cs`
  - Removes the reverb toggle UI.
- `Packages/manifest.json` and `Packages/packages-lock.json`
  - Adds `com.unity.ai.assistant`, `com.unity.2d.sprite`, `jp.shiranui-isuzu.unity-mcp`, and related package churn.
- `Assets/Scripts/Network/ConnectToLobbyNetworkManager.cs`
  - Collapses a working connection path into a no-op.
- `Assets/Scripts/Core/TeamBalanceButton.cs`
  - Becomes effectively empty.
- `Assets/Scripts/UI/CommonCanvas.cs`
  - Becomes effectively empty.
- `Assets/Scripts/UI/LoadingSceneCanvas.cs`
  - Becomes effectively empty.
- `Assets/Scripts/UI/WeaponLimitDialog.cs`
  - Removes useful warnings and weakens diagnostics.
- `Assets/Scripts/Match/AsmExport/MatchData.cs`
  - Removes guardrails and auto-creation behavior for player status.
- `Assets/Scripts/UI/DamageTextSpawner.cs`
  - Collapses warning paths into silent early returns.

## Editor / tool area

Large editor-side additions exist, but they are outside the waitroom rescue scope:

- `Assets/Editor/CharacterSelectDialogPrefabBuilder.cs`
- `Assets/Editor/SpriteReferenceRepairWindow.cs`
- `Assets/Editor/SpriteSceneUsageReport.cs`
- `Assets/Editor/SpriteSheetAutoSliceWindow.cs`

These may be useful later as tooling, but they should be treated as a separate topic from the waitroom rescue.

## Suggested next steps

1. Keep rescuing only waitroom-related scripts that still match the current `main` architecture.
2. Do not merge the manifest / build settings / audio reverb rollback / empty helper classes.
3. If a file has both good and bad changes, split it and port only the good part.
4. Treat the editor tools as a separate branch/topic.

## Current Status

- Safe helper surfaces are mostly exhausted.
- Remaining branch drift is concentrated in risky runtime, scene, build settings, or network-test files.
- The last low-risk additions that were still worth taking were:
  - `Assets/Editor/SpriteSceneUsageReport.cs`
  - `Assets/Editor/SpriteReferenceRepairWindow.cs`
  - `Assets/Editor/SpriteSheetAutoSliceWindow.cs`
  - `Assets/Editor/CharacterSelectDialogPrefabBuilder.cs`
  - `Assets/Editor/MCP/MCPServer.cs`
  - `Assets/TextMesh Pro/Fonts/NotoSansJP-VF.ttf`
  - `Assets/TextMesh Pro/Fonts/NotoSansJP-VF SDF.asset`
  - `Assets/TextMesh Pro/Resources/TMP Settings.asset`
  - `Assets/Scripts/UI/Shop/ShopCatalogFactory.cs`
  - `Assets/Scripts/Networking/ClientNetworkManager.cs`
  - `Assets/Scripts/Editor/AssemblyCompileTimeWindow.cs`

