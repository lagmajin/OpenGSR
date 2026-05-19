# Network Transport Policy

This document is the practical transport split for OpenGSR.

## Core Rule

- Use `TCP` for authoritative, durable, or coordination-heavy flows.
- Use `RUDP` for time-sensitive match gameplay where latency matters more than
  guaranteed delivery.
- Do not put lobby or wait-room authority into the match transport just because
  the existing helper names happen to live there.

## TCP Boundary

These flows belong on `TCP`:

- authentication and account setup
- room creation, join, leave, delete, and room list refresh
- lobby chat and lobby presence
- wait-room authority and readiness
- match server discovery and handoff
- loading start, progress, completion, and enter-map approval
- shop, profile, friend, and inventory state
- match result delivery and post-match persistence

Canonical message examples:

- `LoginRequest`
- `CreateAccountRequest`
- `LogoutRequest`
- `CreateRoomRequest`
- `JoinRoomRequest`
- `LeaveRoomRequest`
- `RoomListUpdateRequest`
- `LobbyChatRequest`
- `MatchServerInfoRequest`
- `PlayerReadyRequest`
- `GameStartNotification`
- `ItemSpawnNotification`
- `ItemDespawnNotification`
- `MatchEndNotification`
- `PlayerInfoRequest`
- `ShopStateRequest`
- `ShopPurchaseRequest`
- `ShopEquipRequest`
- `ShopUnequipRequest`
- `FriendRequest`
- `FriendApproveRequest`
- `FriendListRequest`

## RUDP Boundary

These flows belong on `RUDP`:

- player movement and state sync
- weapon fire, hit, damage, and death events
- respawn and match timing
- round progression and match phase changes
- objective events such as CTF flag changes
- low-latency item use inside the match
- ping and other real-time telemetry

Canonical message examples:

- `PlayerPositionUpdate`
- `PlayerShot`
- `PlayerDamage`
- `PlayerDeath`
- `GameStateSync`
- `PlayerRespawn`
- `RespawnCountdown`
- `RoundStart`
- `RoundEnd`
- `MatchPause`
- `MatchResume`
- `MatchTimeSync`
- `FlagCaptured`
- `FlagLost`
- `FlagReturn`
- `FlagBurst`
- `FlagPickup`
- `PlayerInput`
- `ShootRequest`
- `ItemUseRequest`
- `WeaponChange`
- `WeaponPickup`
- `GrenadeThrow`
- `PingRequest`
- `PingResponse`

## Current Mixing To Remove

These are the main mixed areas that should be cleaned up next:

- `LobbyEnter`, `LobbyLeave`, `LobbyChat`
- `WaitRoomEnter`, `WaitRoomLeave`, `WaitRoomChat`
- `WaitRoomPlayerReady`, `WaitRoomPlayerUnready`
- `WaitRoomSettingsChange`, `WaitRoomKickPlayer`, `WaitRoomOwnerChange`
- `WaitRoomStartCountdown`, `WaitRoomCancelCountdown`
- `WaitRoomUpdateNotification`
- `RoomListUpdate`, `RoomCreated`, `RoomDeleted`, `RoomFull`

These names currently live in the RUDP helper layer, but they describe control
plane behavior. They should be moved into the TCP-side contract or wrapped by
TCP-facing canonical names.

## Current Code Hotspots

The following files are the main places to keep aligned while cleaning up the
split:

- `Assets/Scripts/Network/GeneralServerNetworkManager.cs`
- `Assets/Scripts/Network/MatchRUDPServerNetworkManager.cs`
- `Assets/Scripts/Network/OnlineLoadingSceneNetworkManager.cs`
- `Assets/Scripts/WaitroomNetworkManager.cs`
- `Assets/Scripts/NetworkTest/LocalTestTcpServer.cs`
- `Assets/Scripts/NetworkTest/LocalTestMatchRUDPServer.cs`
- `Assets/Scripts/NetworkTest/LocalTestServerWrapper.cs`

## Recommended Order

1. Move control-plane messages to the TCP contract.
2. Keep only live match state in the RUDP contract.
3. Update the local test servers to match the split.
4. Remove any fallback paths that still use the wrong transport by default.

