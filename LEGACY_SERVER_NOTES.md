# Legacy Server Notes

This file distills the useful parts of the older OpenGSServer text docs into a
short, actionable summary for the current `OpenGSR` work.

## Recovered Design Rules

The older docs consistently point to the same server model:

- the server should own authoritative state, synchronization, and validation
- the client should own rendering, input, and prediction
- detailed collision and physics should stay off the server when possible
- lobby traffic should use TCP
- match traffic should use RUDP / UDP-style real-time transport
- anti-cheat and rate limiting belong on the server side
- the server should stay lightweight and avoid unnecessary heavy simulation

These rules are present in `ServerArchitectureRules.md` and are also echoed in
the copilot instructions and comparison notes.

## Lobby Layer Takeaways

The old lobby comparison note suggests that the useful lobby work belongs in one
central manager rather than split between multiple half-finished server types.

Practical takeaways:

- combine room, player, chat, quick-match, and ping tracking into one source of
  truth
- keep room create / join / leave / delete logic together
- expose room stats and room list queries from the same authority
- prefer thread-safe data structures and explicit cleanup
- keep TCP server hosting as an implementation detail, not the main design

This lines up well with the current `SERVER_ROADMAP.md` S1 milestone.

## Match Layer Takeaways

The old match comparison note points to a future where a simple room / match
core is authoritative and transport-specific shells stay thin.

Practical takeaways:

- move match state progression into a room-like core
- keep event processing explicit and centralized
- treat player input, shots, kill events, respawns, and objective updates as
  server-led events
- use a small tick loop for state progression rather than a giant monolithic
  update path

This matches the current `SERVER_ROADMAP.md` S2 milestone.

## Core Integration Takeaways

The old integration note is mainly about removing duplicate definitions and
using `OpenGSCore` as the shared contract.

Practical takeaways:

- move shared game-object and player-logic types into `OpenGSCore`
- keep server-only objects server-side
- replace old custom time helpers with standard .NET time APIs where possible
- keep type aliases only as temporary compatibility glue

This is already the direction the current repo is taking with `OpenGSCore`.

## Useful Existing Server Features

The older server docs show that some feature families already existed or were
planned and are worth keeping in mind:

- lag compensation
- match XP / level calculation
- field item synchronization
- account / lobby state management
- cleanup of dead or unused classes

Those are useful because they show that the project was not just a lobby shell;
it had already started to grow into a full multiplayer stack.

## Cleanup / Priority Signals

The low-completion analysis and cleanup notes highlight the same weak spots:

- management server is incomplete
- account management still needs structural cleanup
- grenade cleanup behavior is unfinished
- game scene / server scene duplication should be reduced
- unused classes should be removed instead of kept forever

These are good candidates for future backlog items if we want to harden the
server side.

## What Was Not Useful

Some recovered text was mostly noise rather than design intent:

- `server_status.txt`
- `build_output.txt`

They are useful as forensic evidence, but not as a design source.

