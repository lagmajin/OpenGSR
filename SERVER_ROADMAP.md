# OpenGSR Server Roadmap

This is the server-side view of the remaining unfinished work. It is meant to be
short, execution-oriented, and easier to track than the full project roadmap.

## Recovered Legacy Constraints

The older OpenGSServer notes line up with the current direction and are worth
keeping explicit:

- the server should remain authoritative for state, sync, and validation
- the client should handle input, rendering, and prediction
- lobby traffic belongs on TCP
- match traffic belongs on RUDP / UDP-style real-time transport
- detailed collision and physics should stay off the server when possible
- room, player, chat, and ping state should live in one lobby authority
- shared game logic should continue to move into `OpenGSCore`

See [LEGACY_SERVER_NOTES.md](/x:/Dev/OpenGSR/LEGACY_SERVER_NOTES.md) for the
longer recovered summary.

## S0. Protocol Hardening

Goal: keep client, core, and server message names aligned.

Targets:
- `Packages/com.opengs.logic/MessageType.cs`
- `PROTOCOL.md`
- `Assets/Scripts/NetworkTest/*`
- `Assets/Scripts/Network/*`

Done when:
- Canonical message names are used by new code.
- Legacy aliases only exist for compatibility.
- Payload field names are documented and consistent.

## S1. Lobby And Account Authority

Goal: make lobby, account, shop, and friend state authoritative.

Targets:
- `Assets/Scripts/Network/GeneralServerNetworkManager.cs`
- `Assets/Scripts/NetworkTest/LocalTestTcpServer.cs`
- `Assets/Scripts/NetworkTest/LocalTestServerWrapper.cs`

Done when:
- Login, account creation, room list, room creation, join, shop, and friend
  flows all use one backend contract.
- State survives reconnects in the intended environment.

## S2. Match Server Core

Goal: make the match RUDP layer authoritative instead of mostly debug logging.

Targets:
- `Assets/Scripts/Network/MatchRUDPServerNetworkManager.cs`
- `Assets/Scripts/NetworkTest/LocalTestMatchRUDPServer.cs`
- `Assets/Scripts/Match/TDMMatchMainScrip.cs`
- `Assets/Scripts/Match/CTFMatchMainScript.cs`
- `Assets/Scripts/Player/PlayerAgent.cs`
- `Assets/Scripts/Player/CharaController.cs`

Done when:
- Input, shots, deaths, respawns, scoring, and objective events are server-led.
- Clients consume authoritative state instead of local guesses.

## S3. Loading Handshake

Goal: wire loading start/progress/complete into the server flow.

Targets:
- `Assets/Scripts/Network/OnlineLoadingSceneNetworkManager.cs`
- `Assets/Scripts/Scene/OnlineLoadingScene.cs`

Done when:
- Loading start, progress, complete, and map-enter approval are actually sent
  and handled.

## S4. Mission Route

Goal: finish the mission/quest branch instead of leaving it as a dead end.

Targets:
- `Assets/Scripts/Scene/OnlineLobbyScene.cs`
- `Assets/Scripts/Scene/MissionLobbyScene.cs`
- `Assets/Scripts/Scene/MissionAndQuestLobbyScene.cs`
- `Assets/Scripts/Scene/OfflineMissionWaitRoom.cs`

Done when:
- Mission server selection, wait room, and launch flow all work end to end.

## S5. Integration Coverage

Goal: reduce regressions on server-critical flows.

Targets:
- `Assets/Scripts/NetworkTest/*`
- `Assets/Scripts/Network/*`

Done when:
- There is a repeatable local flow for login -> lobby -> wait room -> loading ->
  match -> result.
- Protocol changes fail fast in automated checks or scripted validation.

## Suggested Order

1. `S0`
2. `S1`
3. `S2`
4. `S3`
5. `S4`
6. `S5`

## Parallel Development

For a client/server split plan that matches these milestones, see
[PARALLEL_DEV_PLAN.md](/x:/Dev/OpenGSR/PARALLEL_DEV_PLAN.md).
