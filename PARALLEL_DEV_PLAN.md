# Parallel Development Plan

This project can be developed in parallel as long as the protocol contract stays
stable. The key rule is simple:

- `OpenGSCore` defines the contract.
- The server side becomes authoritative.
- The client side renders and reacts to that contract.
- When one side is incomplete, the other side should still compile and fall back
  cleanly.

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

## Current Best Next Pair

- Server: `S2` match server core
- Client: `M3` match flow and result loop polish

That pair gives the most gameplay value and reduces the risk of protocol drift.
