# OpenGSR Milestones

This document tracks the next most important feature milestones for the project.
It is based on the current repository state, the rescued scene/resource layout, and
the unfinished code paths that still appear in the gameplay flow.

## M0. Scene Name And Transition Cleanup

Goal: make all scene transitions consistent and safe.

Scope:
- `Assets/Scripts/MasterData/GeneralSceneMasterData.cs`
- `Assets/Scripts/Scene/OnlineLobbyScene.cs`
- `Assets/Scripts/Scene/OfflineWaitRoomScene.cs`
- `Assets/Scripts/Scene/AbstractScene.cs`

Why this matters:
- Several scene names still look inconsistent across code and assets.
- Wrong names here can break startup, title return, lobby return, and wait room return flows.

Done when:
- All hardcoded scene names are aligned with the actual scene assets.
- Startup, logout, title return, and wait room return all load correctly.

## M1. Online Lobby Completion

Goal: make online room discovery and room creation usable end-to-end.

Scope:
- `Assets/Scripts/Scene/OnlineLobbyScene.cs`
- `Assets/Scripts/Manager/AsmExport/MatchRoomManager.cs`
- `Assets/Scripts/WaitroomNetworkManager.cs`
- related network manager / room UI code

Why this matters:
- The lobby is the main gateway into online play.
- Room list refresh, filtering, room creation, and joining still look partially stubbed.

Done when:
- Players can create a room, see it in the list, join it, and return safely.
- Room refresh and mode filters work from the UI.

## M2. Offline Wait Room Playability

Goal: make offline play start reliably from the wait room.

Scope:
- `Assets/Scripts/Scene/OfflineWaitRoomScene.cs`
- related offline match setup code

Why this matters:
- Offline mode is the safest fallback for testing gameplay.
- It also serves as a quick local loop for debugging maps and rule setup.

Done when:
- Map selection works.
- Bot count adjustment works.
- A match can be started from offline wait room without manual fixes.

## M3. Match Flow And Result Loop

Goal: complete the full match lifecycle.

Scope:
- `Assets/Scripts/Match/AbstractMatchMainScript.cs`
- `Assets/Scripts/Match/AsmExport/MatchRoom.cs`
- rule/evaluator code in `Assets/Scripts/Match/`
- `Packages/com.opengs.logic/Match/Rule/*`

Why this matters:
- This is the core game loop: start, fight, finish, show result, return.
- A completed loop gives the project a real playable backbone.

Done when:
- A match can run from start to result without manual intervention.
- Win/loss evaluation is rule-correct for the main modes.

## M4. Account, Save, And Shop Flow

Goal: make player progression persistent and useful.

Scope:
- `Assets/Scripts/Scene/CreateNewAccountScene.cs`
- `Assets/Scripts/UI/Shop/OnlineShopService.cs`
- `Assets/Scripts/Systems/UserSaveManager.cs`
- `Assets/Scripts/Systems/EquipmentSaveManager.cs`

Why this matters:
- Shop and account flows are visible player-facing systems.
- They are still mostly stubbed or placeholder-driven.

Done when:
- Account creation is functional.
- Credits, ownership, and equipment state persist across sessions.
- Shop actions are backed by real data instead of dummy returns.

## M5. Combat Feedback And Polish

Goal: improve the feel of combat and the quality of feedback.

Scope:
- `Assets/Scripts/UI/DamageTextSpawner.cs`
- `Assets/Scripts/UI/KillLogEventListener.cs`
- `Assets/Scripts/Systems/EffectService.cs`
- `Assets/Scripts/Network/LagCompensation/*`

Why this matters:
- These systems do not block the game from running, but they strongly affect polish.
- They also help with debugging and make combat easier to read.

Done when:
- Damage feedback is accurate enough to trust.
- Kill log, hit effects, and network prediction feel coherent.

## M6. Item Usage And Resource Loop

Goal: finish the missing "use item" path and make item resources consistent.

Scope:
- `Assets/Scripts/Player/CharaController.cs`
- `Assets/Scripts/Match/MatchEventProvider.cs`
- `Assets/Scripts/Item/InstantItemSlot.cs`
- `Assets/Scripts/Item/FieldItemNetworkManager.cs`
- `Assets/Scripts/Player/AsmExport/PlayerGrenadeComponent.cs`
- `Packages/com.opengs.logic/Item/*`

Why this matters:
- Field-item spawning exists, but activation and usage are still split across
  several partial layers.
- Instant items are equipped and saved, but `UseItem` is still a stub in the
  main player controller.
- Grenade, booster, and consumable behavior all belong in one coherent resource
  model.

Done when:
- Instant items can be used from input and consumed from a slot.
- Field-item pickup, inventory state, and player UI stay in sync.
- The server/core contract defines what item usage means for the match layer.

## M7. Input, Weapon, And Item Action Unification

Goal: route all player intent through one shared action layer.

Scope:
- `Assets/Scripts/Player/AsmExport/PlayerInput.cs`
- `Assets/Scripts/Player/AsmExport/PlayerController.cs`
- `Assets/Scripts/Player/CharaController.cs`
- `Assets/Scripts/Item/InstantItemSlot.cs`
- `Assets/Scripts/Player/AsmExport/WeaponSlots.cs`

Why this matters:
- Weapon swap, reload, item use, and grenade input still branch through too
  many local paths.
- Input handling needs one canonical path before more network authority work is
  added.

Done when:
- Reload, weapon swap, item use, and grenade input all route through the same
  intent layer.
- Slot state and equipped state stay consistent across scene transitions.

## M8. In-Match HUD And Combat Feedback Sync

Goal: make combat feedback and HUD state come from the same data contract.

Scope:
- `Assets/Scripts/UI/CTF/CTFScoreUIManager.cs`
- `Assets/Scripts/UI/DamageTextSpawner.cs`
- `Assets/Scripts/UI/KillLogEventListener.cs`
- `Assets/Scripts/Manager/UIManagerInGameScript.cs`
- `Assets/Scripts/BaseLib/UI/AbstractMatchResultUIManager.cs`

Why this matters:
- Score, damage, and kill feedback are split across several presentation paths.
- The result screen should be fed by the same data that drives the HUD.

Done when:
- HUD score, timer, kill log, damage text, and result summary all agree on
  match state.
- Missing weapon or player details fail gracefully instead of breaking the feed.

## M9. Bot, AI, And Offline Match Parity

Goal: make offline matches and bots behave like a real fallback game mode.

Scope:
- `Assets/Scripts/Player/AIPlayerController.cs`
- `Assets/Scripts/Enemy/*`
- `Assets/Scripts/Scene/OfflineWaitRoomScene.cs`
- `Assets/Scripts/Match/SUVMatchMainScript.cs`
- `Assets/Scripts/Match/DMMatchMainScript.cs`
- `Assets/Scripts/Match/CTFMatchMainScript.cs`

Why this matters:
- Offline play is only useful if bots can fill the gap with predictable
  behavior.
- AI and bot setup are scattered across several partial systems.

Done when:
- Offline matches can start with bots without manual scene fixes.
- At least one rule path can complete with bots and return a valid result.

## M10. Mission And Quest Route Completion

Goal: make the mission branch playable end to end.

Scope:
- `Assets/Scripts/Scene/MissionLobbyScene.cs`
- `Assets/Scripts/Scene/MissionAndQuestLobbyScene.cs`
- `Assets/Scripts/Scene/OfflineMissionWaitRoom.cs`
- `Assets/Scripts/Scene/MissionResultScene.cs`
- `Assets/Scripts/Scene/Result/*`

Why this matters:
- The mission branch still contains placeholder throws and dead-end
  transitions.
- The route should behave like a real alternate game mode, not a stub.

Done when:
- Mission selection, wait room, loading, result, and return flow all work end
  to end.
- No mission scene terminates immediately because of placeholder code.

## M11. Master Data And Content Pipeline

Goal: keep maps, modes, and content selection data-driven.

Scope:
- `Assets/Scripts/MasterData/*`
- `Assets/Scripts/Scene/ExportAssets/*`
- `Assets/Scripts/Systems/WeaponSandboxTestHelper.cs`
- `Assets/Scripts/Manager/GameModeSelectManager.cs`

Why this matters:
- Scene and content selection still depend on fragile string and data wiring.
- Master data issues can break startup, map selection, and content previews.

Done when:
- Missing or incomplete content data is handled safely.
- Map, mode, sound, and thumbnail selection stay data-driven and consistent.

## M12. Persistence, Friends, And Social State

Goal: make long-lived player state reliable across sessions.

Scope:
- `Assets/Scripts/Systems/UserSaveManager.cs`
- `Assets/Scripts/Systems/EquipmentSaveManager.cs`
- `Assets/Scripts/Systems/FriendManager.cs`
- `Assets/Scripts/UI/FriendListUI.cs`
- `Assets/Scripts/UI/FriendItem.cs`

Why this matters:
- Account, save, and social data should survive restarts without UI drift.
- Friend and equipment updates are currently spread across multiple layers.

Done when:
- Save/load, equipment, and friend state persist reliably.
- UI refreshes after add/remove/equip operations without stale entries.

## M13. Match Observability And Scripted Validation

Goal: reduce regressions with repeatable validation paths.

Scope:
- `Assets/Scripts/NetworkTest/*`
- `Assets/Scripts/Network/*`
- dedicated bootstrap or debug scenes

Why this matters:
- Current regression checks are mostly manual.
- More milestones means more ways to break protocol and scene flow.

Done when:
- There is a repeatable scripted path for login -> lobby -> wait room ->
  loading -> match -> result.
- Key protocol changes fail fast in tests or automation.

## Suggested Order

1. `M0`
2. `M1`
3. `M3`
4. `M2`
5. `M4`
6. `M7`
7. `M8`
8. `M5`
9. `M6`
10. `M11`
11. `M12`
12. `M9`
13. `M10`
14. `M13`

## Parallel Development

If you want to work on client and server at the same time, see
[PARALLEL_DEV_PLAN.md](/x:/Dev/OpenGSR/PARALLEL_DEV_PLAN.md).

## Server Side Roadmap

This section tracks the unfinished server-facing pieces that still look like mock,
stub, or local-test-only implementations.

If you only want the server plan, see [SERVER_ROADMAP.md](/x:/Dev/OpenGSR/SERVER_ROADMAP.md).

## S0. Protocol And Contract Hardening

Goal: keep client, core, and server message names and field names aligned.

Scope:
- `Packages/com.opengs.logic/MessageType.cs`
- `PROTOCOL.md`
- `Assets/Scripts/NetworkTest/*`
- `Assets/Scripts/Network/*`

Why this matters:
- A lot of the current networking code still accepts legacy aliases.
- If the contract drifts again, lobby and match flows will silently break.

Done when:
- New code uses canonical `OpenGSCore` message names.
- Legacy aliases are only kept for compatibility, not as the primary path.
- Room, account, and loading payload fields are documented in one place.

## S1. Authoritative Lobby And Account State

Goal: move lobby, account, shop, and friend state out of ad-hoc local mocks.

Scope:
- `Assets/Scripts/Network/GeneralServerNetworkManager.cs`
- `Assets/Scripts/NetworkTest/LocalTestTcpServer.cs`
- `Assets/Scripts/NetworkTest/LocalTestServerWrapper.cs`
- `Assets/Scripts/Scene/Account/LoginAndSignUpScene.cs`
- `Assets/Scripts/Scene/CreateNewAccountScene.cs`

Why this matters:
- `GeneralServerNetworkManager` is still mostly an in-memory simulation.
- `LocalTestTcpServer` currently owns most of the real behavior, which means the
  server contract is not yet centralized or durable.
- Account creation, login, credits, inventory, room list, and friend state all
  need the same source of truth.

Done when:
- Login, account creation, room creation, room join, shop, and friends all flow
  through one consistent backend contract.
- State survives a reconnect or server restart in the intended environment.
- Mock-only fallback logic is clearly separated from the authoritative path.

## S2. Match Server Core

Goal: make the match RUDP layer authoritative instead of mostly debug logging.

Scope:
- `Assets/Scripts/Network/MatchRUDPServerNetworkManager.cs`
- `Assets/Scripts/NetworkTest/LocalTestMatchRUDPServer.cs`
- `Assets/Scripts/Match/TDMMatchMainScrip.cs`
- `Assets/Scripts/Match/CTFMatchMainScript.cs`
- `Assets/Scripts/Player/PlayerAgent.cs`
- `Assets/Scripts/Player/CharaController.cs`

Why this matters:
- The current match network manager is an empty shell.
- `LocalTestMatchRUDPServer` can echo some events, but it still behaves like a
  test harness rather than a full authoritative match server.
- TDM and CTF already send kill and flag events, so the server layer needs to
  become the source of truth for state, scoring, and validation.

Done when:
- Player input, shots, deaths, respawns, score updates, and objective events are
  processed by a real match server path.
- Clients receive authoritative match state instead of relying on local guesses.
- The debug broadcast loop is replaced by real state progression.

## S3. Loading And Map Transition Handshake

Goal: wire the online loading scene to a real server handshake.

Scope:
- `Assets/Scripts/Network/OnlineLoadingSceneNetworkManager.cs`
- `Assets/Scripts/Scene/OnlineLoadingScene.cs`
- `Assets/Scripts/Scene/ConnectToMatchServerScene.cs`
- `Assets/Scripts/Scene/ConnectToGeneralServerScene.cs`

Why this matters:
- The loading scene currently calls empty network methods.
- `OnlineLoadingScene` already expects start/progress/complete and map entry
  approval, so this is an unfinished critical path rather than polish.

Done when:
- Loading start, progress, complete, and map-enter approval are actually sent
  and consumed.
- Timeout and fallback behavior are handled by the server protocol, not just by
  local scene logic.

## S4. Mission Server And Alternate Lobby Flow

Goal: finish the mission/quest route instead of leaving it as a dead end.

Scope:
- `Assets/Scripts/Scene/OnlineLobbyScene.cs`
- `Assets/Scripts/Scene/MissionLobbyScene.cs`
- `Assets/Scripts/Scene/MissionAndQuestLobbyScene.cs`
- `Assets/Scripts/Scene/OfflineMissionWaitRoom.cs`
- `Assets/Scripts/Scene/WaitRoom/OnlineWaitRoomSceneServer.cs`

Why this matters:
- The lobby already has a TODO for mission server switching.
- Several mission-related scenes still throw `NotImplementedException`.
- This looks like a parallel game mode path that needs its own server flow.

Done when:
- Mission server selection, connection, wait room, and launch flow all work end
  to end.
- Mission/quest scenes no longer terminate immediately with placeholder throws.

## S5. Integration Tests And Observability

Goal: make server-side behavior easier to validate and regress less often.

Scope:
- `Assets/Scripts/NetworkTest/*`
- `Assets/Scripts/Network/*`
- any dedicated test or bootstrap scenes

Why this matters:
- The current local server code is doing a lot of work, but it is still mostly
  validated by manual runs.
- There are several protocol-critical paths now, so deterministic test coverage
  will pay off quickly.

Done when:
- There is a repeatable local flow for login -> lobby -> wait room -> loading ->
  match -> result.
- Key protocol changes fail fast in tests or scripted validation instead of only
  failing during manual play.

## S6. Mission Server Authority

Goal: move the mission branch onto a dedicated server contract.

Scope:
- `Assets/Scripts/Scene/MissionLobbyScene.cs`
- `Assets/Scripts/Scene/MissionAndQuestLobbyScene.cs`
- `Assets/Scripts/Scene/OfflineMissionWaitRoom.cs`
- `Assets/Scripts/Network/GeneralServerNetworkManager.cs`
- `Assets/Scripts/NetworkTest/LocalTestTcpServer.cs`

Why this matters:
- Mission routing still depends too much on client-side scene switches.
- The server needs to own the mission branch state transitions.

Done when:
- Mission selection and launch are validated by the server contract.
- Mission flow survives reconnects and scene reloads in the intended
  environment.

## S7. Save, Shop, And Friend Authority

Goal: make persistent player state authoritative instead of ad hoc.

Scope:
- `Assets/Scripts/Systems/UserSaveManager.cs`
- `Assets/Scripts/Systems/EquipmentSaveManager.cs`
- `Assets/Scripts/Systems/FriendManager.cs`
- `Assets/Scripts/Network/GeneralServerNetworkManager.cs`
- `Assets/Scripts/NetworkTest/LocalTestTcpServer.cs`

Why this matters:
- Account, inventory, shop, and friend state need one authority source.
- Local mocks and saved state should not diverge from the server contract.

Done when:
- Save, equip, shop, and friend operations are backed by consistent backend
  state.
- Reconnects and restarts preserve the intended data.

## S8. Protocol Regression Coverage

Goal: reduce breakage from contract drift.

Scope:
- `Assets/Scripts/NetworkTest/*`
- `Assets/Scripts/Network/*`
- `Packages/com.opengs.logic/*`

Why this matters:
- Protocol drift is one of the easiest ways for the client and server to
  silently break.
- Milestone growth needs a repeatable safety net.

Done when:
- Canonical message names and payload shapes are covered by repeatable
  validation.
- Local test flows catch regressions before manual play.

## Suggested Server Order

1. `S0`
2. `S1`
3. `S2`
4. `S3`
5. `S4`
6. `S5`
7. `S6`
8. `S7`
9. `S8`

## Notes

- Keep this document short and action-oriented.
- If a milestone grows too large, split it into smaller issues instead of bloating the roadmap.
- If a scene name changes, update both the code and the master data together.
