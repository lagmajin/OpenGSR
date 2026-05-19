# OpenGSR Architecture Boundary

This note records the intended split between shared domain logic, backend
authority, and the Unity client.

## Core

Core should own pure game state and rules.

Keep here:
- player status and resource rules
- item effects and durations
- match rules and result evaluation
- room / wait room / loading state data
- message contracts and DTOs

Do not keep here:
- `UnityEngine` types
- `MonoBehaviour`
- scene or prefab references
- animation, UI, or audio code

## Server

Server should own authority and transport.

Keep here:
- TCP / RUDP endpoints
- room and lobby authority
- match loop orchestration
- validation and logging
- smoke-test harnesses

Do not keep here:
- Unity scene logic
- rendering or input code
- client-only presentation state

## Unity Client

Unity should be a thin adapter.

Keep here:
- input
- visuals
- UI
- animation
- sound
- scene transition glue
- local prediction or presentation smoothing

Do not keep here:
- permanent gameplay rules
- authoritative match state
- item effect rules
- transport contract definitions

## Migration Order

1. Move state and rule objects into `OpenGSCore`.
2. Make `OpenGSServer` consume those objects directly.
3. Reduce Unity scripts to adapter code.
4. Remove duplicate local rule copies once the shared path is stable.

## Practical Rule

If a feature must still make sense after Unity is removed, it belongs in Core or
Server first.
