# Legacy OpenGS Document Map

This file points to older OpenGS design documents that appear to have been
written before the current `OpenGSR` repo structure stabilized.

## Found Legacy Documents

### Project Structure

- `X:\Dev\OpenGS_Project_Structure.md`

What it contains:

- the original 3-part project split:
  - `OpenGS` client
  - `OpenGSCore` shared logic
  - `OpenGSServer` server app
- a directory-level map of the client/server/core layout
- the historical mode list:
  - DeathMatch
  - Survival
  - TeamDeathMatch
  - TeamSurvival
  - Capture The Flag
  - Arms Race
  - Mission
  - One Shot Kill
- lag compensation notes for client and server

### Network Event Architecture

- `X:\Dev\OpenGS_Network_Event_Architecture.md`

What it contains:

- event-layer architecture for client, server, and local test servers
- client message broker notes
- RUDP message type grouping
- server-side event handling flow
- local test server responsibilities

### Legacy Class Index

- `X:\Dev\OpenGS\Assets\Scripts\CLASS_INDEX.md`

What it contains:

- a broad historical class index
- references to old `Docs/GAME_RULES.md`
- references to mission, CTF, TDM, TSUV, and other older systems

## Important Note

`X:\Dev\OpenGS\Assets\Scripts\CLASS_INDEX.md` points at
`../../Docs/GAME_RULES.md`, but that file is not currently present in the
workspace search results I checked.

That likely means one of the following:

- the file was renamed
- the file lived in a different old project folder
- the file was removed but the reference remained

## How To Use These Documents

These legacy files are best used as:

- a historical reference for missing systems
- a reminder of old mode names and architecture boundaries
- a source of feature names that should be reconciled with `SPEC.md` and
  `RULES.md`

## Suggested Follow-Up

If you want to recover more old design intent, the next useful step is to search
for:

- `GAME_RULES.md`
- `Mission`
- `Arms Race`
- `One Shot Kill`
- `TSuv`
- `Capture The Flag`

across the older `OpenGS` folder and any sibling project folders.
