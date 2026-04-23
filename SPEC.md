# OpenGSR Game Specification

This document describes the intended game for OpenGSR as it is being built.
It reflects both the current implementation and the target direction.

## High-Level Vision

OpenGSR is a fast, `Soldat`-style 2D online shooting game.

The target experience is:

- side-view 2D combat with movement, recoil, jumps, rolls, boosters, and map traversal
- online multiplayer matches with room-based matchmaking
- match sizes up to about 16 players
- multiple competitive rule sets
- offline play for local testing and fallback play

The game should feel:

- quick to enter
- easy to read in combat
- skill-driven
- networked but still playable offline
- tactical without becoming slow

## Core Game Loop

1. Start the game.
2. Connect to the general server.
3. Enter the lobby.
4. Create or join a room.
5. Move to the wait room.
6. Select mode, map, and team setup if needed.
7. Enter loading.
8. Enter the match.
9. Play until the rule condition ends the match.
10. Show the result screen.
11. Return to the wait room or lobby.

That loop is the main spine of the project.

## Scene Flow

The current scene flow should stay conceptually simple:

- title
- general server connect
- lobby
- room / wait room
- loading
- match
- result
- back to lobby or wait room

Offline play follows the same pattern, but skips the live server dependency where
possible.

## Match Scale

The target match size is around 16 players.

That means the game should support:

- small-team or free-for-all pacing
- short-to-medium match duration
- fast player respawns where the rule allows it
- readable combat even when the screen is busy

## Game Structure

### Lobby

The lobby is where the player:

- connects to the game service
- creates or joins a room
- sees room lists and room filters
- chooses the next path into a match or mission route

### Wait Room

The wait room is where the player:

- sees the room state
- adjusts bots or team setup in offline play
- confirms readiness
- starts the match

### Loading

The loading scene is a handshake phase between client and server.

It exists so the game can:

- prepare assets
- notify the server of loading progress
- wait for map entry approval

### Match

The match is the core action phase.

Players move and fight in 2D maps with:

- weapons
- movement options
- team/objective rules
- scoring and death tracking

### Result

The result screen shows:

- the winning side or winner
- the player list and results
- a route back to the wait room or lobby

## Supported Rule Families

The current project structure suggests these core rules:

- DeathMatch
- TeamDeathMatch
- CaptureTheFlag
- Survival
- TeamSurvival

The game should be able to use the same match flow while swapping the rule layer.

### DeathMatch

- free-for-all
- winner is the player with the highest kill count
- result screen should show the top player and the full player list

### TeamDeathMatch

- two teams
- winner is the team with the higher kill count
- team score should be visible during the match
- result screen should show both team totals and the player list

### CaptureTheFlag

- two teams
- scoring is driven by objective play
- flag capture / return events are part of the match loop
- tie-break behavior should remain explicit in the rule evaluator

### Survival

- individual survival-oriented mode
- death count and elimination matter more than raw score

### TeamSurvival

- team-based survival mode
- last surviving team or equivalent survival condition wins
- team lives and alive counts should be visible to the server rule layer

## Networking Model

OpenGSR uses a split network model:

- the general server handles lobby, account, room, and loading authority
- the match server handles combat-related events and match state
- the client renders UI, sends player intent, and reacts to authoritative state

The shared contract lives in `OpenGSCore`.

New code should prefer canonical protocol names and payload fields.

## Authoritative Boundaries

The project should treat these as server-authoritative where possible:

- room membership
- room capacity
- readiness
- loading approval
- match end
- score updates
- objective events

The client should be responsible for:

- input capture
- presentation
- scene transitions
- local feedback
- temporary prediction or interpolation where needed

## Online and Offline Modes

The game should support both online and offline paths.

### Online

- uses the lobby
- uses room creation and joining
- uses the loading handshake
- uses the match server
- returns to the online wait room or lobby after a match

### Offline

- skips the live network dependency where possible
- still uses the same match and result flow
- is useful for debugging maps, bots, and rules

## Combat Feel Goals

The intended combat feel is:

- immediate movement response
- readable hits and deaths
- useful score feedback
- clear match end state
- low-friction re-entry into the next match

The game should feel more like a competitive arena shooter than a slow tactical sim.

## Match Presentation Goals

During a match the player should be able to read:

- current team or position
- current score or objective progress
- match timer
- recent combat feedback
- whether the match is about to end

The result screen should immediately answer:

- who won
- why they won
- how each player performed
- what to do next

## Item And Resource Model

The project already has several item-related systems in partial form.

The intended item model is:

- field pickups that spawn in the arena
- slot-based instant items that can be consumed from input
- combat resources such as grenades and booster fuel
- shop ownership and equipment state that persist between matches

From a player point of view, the item loop should feel like this:

1. pick up an arena item or equip an item before the match
2. use it through a dedicated input or slot
3. see the effect immediately in combat
4. have the UI and match state stay in sync

The canonical item-use work should eventually live in `OpenGSCore`, with the
client handling presentation and input and the server handling authority.

## UI and Presentation Goals

The UI should make it easy to understand:

- which room you are in
- which team you are on
- what the current score is
- when the match is loading
- when the match is over
- how to get back into play quickly

## Account and Progression

The project already has account, save, and shop scaffolding.

The intended long-term direction is:

- player account creation
- login
- saved equipment
- saved ownership
- shop and unlock flow

These systems should support the match game loop instead of getting in the way of it.

## Maximum Scope For The First Playable Version

The first strong playable version should include:

- lobby connection
- room create / join
- wait room flow
- at least one fully working match rule
- result screen
- offline fallback

Everything else should support that core loop rather than replace it.

## Mission Route

There is a second branch of the game flow for mission or quest play.

That path is not the main multiplayer loop, but it should remain compatible with the same account, scene, and server structure.

## Current Documentation Map

- `MILESTONES.md` tracks the client-side roadmap
- `SERVER_ROADMAP.md` tracks the server-side roadmap
- `PARALLEL_DEV_PLAN.md` explains how to develop both sides together
- `PROTOCOL.md` documents the network contract
- `RULES.md` documents the current and legacy game rule structure
- `LEGACY_DOC_MAP.md` points to older design documents and historical notes
- `ITEM_SYSTEMS.md` summarizes the current item-use implementation state
- `LEGACY_SERVER_NOTES.md` summarizes the recovered server design guidance

## Practical Design Rule

If a feature changes the match loop, room flow, loading flow, or result flow, it should be reflected in:

- the protocol contract
- the client scene logic
- the server roadmap or implementation

That keeps the game coherent while both sides are being built in parallel.
