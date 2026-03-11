# OpenGSR Script Boundaries

This document defines ownership boundaries to reduce coupling while the project is being migrated.

## 1) Scene Layer
- `Assets/Scripts/Scene/*`: scene lifecycle and user-flow orchestration only.
- `Assets/Scripts/Scene/SceneController/*`: input/UI orchestration and scene-local control logic.
- `Assets/Scripts/MediateObject/*`: references and data handoff between scene and systems.
- Rule: avoid direct save/network protocol parsing in scene classes when possible.

## 2) Save Layer
- `Assets/Scripts/Systems/*Save*` and `JsonStorage`: persistence and schema migration only.
- Rule: gameplay and UI should call managers, not read/write files directly.

## 3) Audio Layer
- `Assets/Scripts/Audio/*`: runtime playback and routing.
- `Assets/Scripts/MasterData/SoundMasterData*`: clip lookup and validation.
- Rule: gameplay code should request by semantic enum/type, not hardcoded resource paths.

## 4) Event Layer
- `Assets/Scripts/Network/GameEventBroker*`: in-process event publish/subscribe.
- `Assets/Scripts/Event/*`: event DTOs.
- Rule: event payloads should stay serializable/plain and avoid direct Unity object dependencies.

## 5) Network Naming
- Runtime networking is split historically across:
  - `Assets/Scripts/Network/*` (legacy and protocol builders)
  - `Assets/Scripts/Networking/*` (client and foundation request/response)
- Rule: put new request/response client foundation code under `Networking/Foundation`.
- Rule: do not introduce a third top-level networking folder.
