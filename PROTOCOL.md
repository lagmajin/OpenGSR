# OpenGSR Network Protocol

This project currently supports a canonical protocol plus legacy aliases.
New code should use the canonical names below. Existing clients and local test servers still accept the legacy aliases where noted.

## Canonical message types

### Authentication

- `LoginRequest`
- `LoginResponse`
- `CreateAccountRequest`
- `CreateAccountResponse`
- `LogoutRequest`
- `LogoutSuccessful`

### Lobby and rooms

- `CreateRoomRequest`
- `CreateRoomResponse`
- `JoinRoomRequest`
- `JoinRoomResponse`
- `LeaveRoomRequest`
- `LeaveRoomResponse`
- `RoomListUpdateRequest`
- `RoomListUpdateNotification`
- `LobbyChatRequest`
- `LobbyChatNotification`

### Match and readiness

- `MatchServerInfoRequest`
- `MatchServerInfoResponse`
- `PlayerReadyRequest`
- `PlayerReadyNotification`
- `GameStartNotification`
- `ItemSpawnNotification`
- `ItemDespawnNotification`

### Profile and shop

- `PlayerInfoRequest`
- `PlayerInfoResponse`
- `ShopStateRequest`
- `ShopStateResponse`
- `ShopPurchaseRequest`
- `ShopPurchaseResponse`
- `ShopEquipRequest`
- `ShopEquipResponse`

### Match result

- `MatchEndNotification`

## Legacy aliases currently accepted

- `LoginSuccessful` -> `LoginResponse`
- `LogoutSuccess` -> `LogoutSuccessful`
- `CreateNewWaitRoomRequest` -> `CreateRoomRequest`
- `CreateNewWaitRoomResponse` -> `CreateRoomResponse`
- `EnterWaitRoomRequest` -> `JoinRoomRequest`
- `EnterWaitRoomResponse` -> `JoinRoomResponse`
- `LeaveWaitRoomRequest` -> `LeaveRoomRequest`
- `LeaveWaitRoomResponse` -> `LeaveRoomResponse`
- `UpdateRoomRequest` -> `RoomListUpdateRequest`
- `UpdateRoomResponse` -> `RoomListUpdateNotification`
- `AddLobbyChat` -> `LobbyChatRequest`
- `SendEnterRoom` -> `JoinRoomRequest`
- `PlayerInfo` -> `PlayerInfoRequest`

## Field name conventions

Use these names in new payloads:

- `PlayerID`
- `PlayerName`
- `GlobalUserId`
- `RoomID`
- `RoomName`
- `Capacity`
- `GameMode`
- `TeamBalance`
- `Credits`
- `PurchasedItems`
- `Category`
- `Slot`
- `Success`
- `ErrorMessage`

Legacy casing such as `PlayerId`, `RoomId`, and `OwnerPlayerID` is still tolerated in a few places, but it should not be introduced in new code.

## Room flow

1. `RoomListUpdateRequest`
2. `RoomListUpdateNotification`
3. `CreateRoomRequest` -> `CreateRoomResponse`
4. `JoinRoomRequest` -> `JoinRoomResponse`
5. `LeaveRoomRequest` -> `LeaveRoomResponse`

## Notes

- `OpenGSCore.MessageType.Normalize(...)` is the canonical compatibility helper.
- New handlers should normalize incoming `MessageType` values before switching on them.
- When adding a new message, prefer defining it in `Packages/com.opengs.logic/MessageType.cs` first, then update server, client, and local test handlers together.
