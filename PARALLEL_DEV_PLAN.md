# Parallel Development Plan

This project can be developed in parallel as long as the protocol contract stays
stable. The key rule is simple:

- `OpenGSCore` defines the contract.
- The server side becomes authoritative.
- The client side renders and reacts to that contract.
- When one side is incomplete, the other side should still compile and fall back
  cleanly.

## Repository Roles

Treat the repositories as one coordinated system, with the native client as the
primary client-development lane:

- `OpenGSCore` defines shared contracts, DTOs, and pure domain rules.
- `OpenGSServer` owns authority, validation, persistence, and transport.
- `OpenGSNativeClient` is the primary client target for active gameplay,
  rendering, and map-recovery integration.
- `OpenGSR` is the Unity companion lane for contract/reference behavior,
  Unity scene validation, and editor automation. Keep it independently
  buildable; it must not become a runtime dependency of the native client.

Neither client repository is a second source of truth for game rules. If a
feature needs real gameplay authority, it belongs in Core or Server first,
then both clients follow the contract. Keep native-client C++/CMake work and
Unity assets/code in their own repositories; share reviewed data and contract
definitions through explicit formats and paths, not by reaching into the
other checkout at runtime.

## Native Primary / Unity Companion Plan

Keep most implementation effort on `OpenGSNativeClient`. Use `OpenGSR` for two
scoped tasks that directly support native-client progress:

### U1. Make Unity MCP connection checks reliable

The project already contains a Unity MCP package, an HTTP-to-stdio bridge, and
editor audit tools. Their endpoint configuration was inconsistent: the package
defaults to port `27182` (and can select another port in its range),
`codex_mcp.json` pointed to `27186`, `docs/codex-memo.md` said `27184`, and
`MCP_USAGE.md` documented `27182`. The running Editor initially reported the
server as not initialized. After initializing and starting it from Unity
Preferences, read-only MCP calls succeeded on `27182` for health, hierarchy,
console logs, and a scene screenshot.

Next steps:

1. Discover the OpenGS Unity instance by probing the package's documented
   local port range and matching its `/health` project name.
2. Keep `mcp_http_bridge.py`, `codex_mcp.json`, and `MCP_USAGE.md` on the same
   discovery path and project identity.
3. Recheck health, hierarchy, console reads, and screenshot capture from the
   bridge before using MCP for scene edits. Keep write-capable tools behind
   explicit, task-scoped operations.

Done when the bridge selects the OpenGS editor even if its port changes, and
read-only scene/console checks succeed from a fresh Codex session.

### U2. Build Unity map previews from recovered map data

Use the reviewed MPF recovery/export outputs from `OpenGSNativeClient` as
inputs to a deterministic Unity Editor importer or generator. Start with one
reviewed numbered map and emit an isolated preview under a generated-map
folder; do not overwrite hand-authored production scenes. Preserve Unity
`.meta` files and references, and record source map ID, asset hashes, skipped
objects, and orientation/placement decisions in an audit report.

Done when the same source data produces a repeatable Unity preview, its
placement/orientation report agrees with the native map audit, and Unity MCP
can capture the generated scene for visual comparison. Only promote a preview
to a production scene after its visual and collision checks pass.

#### Initial Waterfall preview

- Added `Assets/Scripts/Editor/NativeMapPreviewBuilderWindow.cs` with a
  `Tools/OpenGS/Build Native Map Preview` command. It reads the native map JSON
  directly, refuses to overwrite an existing generated folder, and writes an
  isolated scene plus a placement/hash audit report.
- Built `Assets/GeneratedMapPreviews/Waterfall/WaterfallPreview.unity` from
  `OpenGSNativeClient/assets/maps/Waterfall.json`: 106 sprite placements, 49
  unique source images, 23 source images copied into the preview folder, and
  2,532 box colliders. The map has no polygon colliders.
- Kept the 18 cropped placements across three animation/flag textures as
  separate Unity Sprite sub-assets. Verified all 106 renderers have sprites,
  all 2,532 colliders load, and reused Unity textures have matching SHA-256
  hashes to their native source files.
- Opened the generated scene additively for visual inspection; the production
  `WaterFall.unity` scene and existing sprite assets were not edited.

### Work cadence and boundaries

- Keep the native client as the active feature lane; start with MCP discovery,
  then use the map-preview importer as a bounded Unity subtask.
- Make no protocol changes in either client without updating the shared
  Core/Server contract and coordinating both clients.
- Do not modify editor-local `UserSettings` as part of gameplay or map commits.
- Keep each repository's dirty files isolated and review its status before
  switching workspaces or committing.

## Shared Contract First

Before starting a new slice, lock these items:

- message type names
- JSON field names
- scene names
- result payload structure

If a change touches one of those items, update both the client and the server
lane in the same pass.

## Client Lane

This lane keeps the game playable and visible.

- lobby UI and room selection
- wait room UI and player setup
- loading scene progress display
- match result scene and player list
- combat feedback and polish

## Server Lane

This lane keeps the game state authoritative.

- lobby/account authority
- room list and join validation
- match server core
- loading handshake
- mission route

## Safe Parallel Pairs

These are the best work pairs because they touch the same contract but do not
fight for the same files.

### Pair 1: Lobby And Join Flow

- Server: room creation, room list, join validation
- Client: online lobby selection, refresh, join transition

Shared acceptance:
- create a room
- see it in the list
- join it
- return without stale state

### Pair 2: Match End And Result

- Server: match end notification and result payload
- Client: result scenes and result list display

Shared acceptance:
- a match can finish
- the winner is visible
- the player list is displayed
- the next scene is reachable

### Pair 3: Loading Handshake

- Server: loading start/progress/complete approval
- Client: loading UI and map-enter gating

Shared acceptance:
- loading progress is visible
- map entry is allowed only after approval

### Pair 4: Mission Route

- Server: mission route contract
- Client: mission lobby / offline mission wait room

Shared acceptance:
- mission path no longer dead-ends
- mission scenes can connect and continue

## Suggested Working Cadence

1. Pick one client slice and one server slice that share the same contract.
2. Implement the contract on the server side first if the state must be
   authoritative.
3. Implement the client reaction immediately after.
4. Run a build on both repos or both project files.
5. Commit once the pair reaches a usable milestone.

## OpenGSR Scope

When working in `OpenGSR`, keep the focus on core logic and contract wiring:

- network request and response handling
- scene state transitions that depend on shared contracts
- local adapters that keep the client compiling and testable
- state display only when it directly reflects server-owned data

Avoid expanding `OpenGSR` into a separate rule owner. UI polish, one-off
presentation experiments, and duplicate rule logic should stay out unless they
are needed to keep the client contract alive.

## Current Best Next Pair

- Server: `S2` match server core
- Client: `M3` match flow and result loop polish

That pair gives the most gameplay value and reduces the risk of protocol drift.
