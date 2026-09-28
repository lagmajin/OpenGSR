# OpenGSCore Milestones

This repository defines the shared gameplay contract. The near-term goal is to
remove ambiguity before client and server work diverge again.
It should also stay usable as a standalone pure C# domain library if the Unity
client is replaced later.

## C0. Canonical Message Contract

Goal: keep the shared message surface stable and explicit.

Scope:
- `MessageType.cs`
- `GameMode.cs`
- any shared request/response DTOs added to the package

Why this matters:
- The project still carries several legacy aliases and normalizers.
- If the canonical names drift, client and server fixes will keep re-breaking
  each other.

Done when:
- New code uses the canonical message names first.
- Legacy aliases remain only as compatibility shims.
- Mode names and normalization rules are documented in one place.

Current status:
- incoming network handlers are being updated to normalize legacy names before
  dispatch
- the shared contract should stay authoritative for client, server, and local
  test code
- canonical message names are now also propagated on the general server output

## C1. Match Rule Coverage And Factory Cleanup

Goal: make rule creation deterministic for every supported mode.

Scope:
- `Match/Rule/MatchRuleFactory.cs`
- `Match/Rule/*`
- `GameMode.cs`

Why this matters:
- The rule factory is the gatekeeper for match behavior.
- If a mode is missing or mapped loosely, the server and client can no longer
  agree on how a match should end.

Done when:
- Every supported mode has a concrete rule path.
- Team and solo variants are represented without hidden fallbacks.
- Unsupported modes fail clearly instead of silently drifting.

## C2. Room, Result, And Score Model Completion

Goal: finish the shared room/result objects that the game loop depends on.

Scope:
- `Room/AbstractGameRoom.cs`
- `Match/Result/*`
- `Score/*`
- `PlayerInfo` and any score payloads shared with the server

Why this matters:
- The current shared objects still have placeholder returns and incomplete
  result surfaces.
- Room lifecycle and result generation need to be trustworthy before the match
  flow can be completed end to end.

Done when:
- Room creation stores the expected metadata consistently.
- Result objects are never returned as null in the finished path.
- Player and team score payloads are coherent across all callers.

## C3. Item And Event Contract

Goal: define the shared item-use and event vocabulary.

Scope:
- `Item/*`
- `Event/*`
- `Match/MatchEventProvider.cs`
- any item- or field-item-related DTOs

Why this matters:
- Item usage already exists in pieces, but the shared contract is still thin.
- A stable item contract will make the client and server implementations much
  easier to finish in parallel.

Done when:
- Instant-item use has a shared request/response shape.
- Field-item and combat events use shared names rather than ad hoc strings.
- The core package can be consumed without relying on client-local guesses.

## C4. Unity-Free Domain Kernel

Goal: keep the shared game domain entirely free of Unity dependencies.

Scope:
- `Player/PlayerStatus.cs`
- `Item/*`
- `Match/*`
- `Room/*`
- `Event/*`
- `GameScene.cs`
- any shared DTOs added later

Why this matters:
- If the Unity client is removed, the core package should still describe the
  game clearly on its own.
- The current project already has enough gameplay state to justify a real
  engine-agnostic domain model.
- This makes server validation, headless tests, and later client rewrites much
  safer.

Done when:
- no new core code depends on `UnityEngine`
- gameplay rules are represented through plain C# types
- client and server can both consume the same shared state objects

## Suggested Order

1. `C0`
2. `C1`
3. `C2`
4. `C3`
5. `C4`
