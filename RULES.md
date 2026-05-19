# OpenGSR Game Rules

This document captures the current and legacy rule structure for OpenGSR.
It is meant to help keep the game design, client code, and `OpenGSCore`
aligned.

## Current Canonical Rule Set

The canonical rule implementations live in `OpenGSCore` and are the ones the
game should treat as the source of truth.

### DeathMatch

- Free-for-all mode
- Highest kill count wins
- Supports a result screen with ranked players

### TeamDeathMatch

- Two-team mode
- Team kill total decides the winner
- Best fit for fast team combat

### CaptureTheFlag

- Objective-driven team mode
- Flag capture and return events matter
- Team score is based on objective progress

### Survival

- Individual survival mode
- Death and elimination matter more than raw kills

### TeamSurvival

- Team-based survival mode
- Last surviving team or equivalent survival condition wins

## Legacy Client-Side Rule Scaffold

The client project still contains an older rule scaffold in
[`Assets/Scripts/Match/MatchRule.cs`](/x:/Dev/OpenGSR/Assets/Scripts/Match/MatchRule.cs).

That file contains placeholder rule classes such as:

- `DeathMatchRule`
- `TeamDeathMatchRule`
- `CTFMatchRule`
- `SurvivalMatchRule`
- `TeamSurvivalMatchRule`
- `ArmsMatchRaceRule`

These classes are useful as historical context, but the canonical gameplay
logic should follow the `OpenGSCore` evaluators and match rule factory.

## Mode Naming

The project has had several naming styles over time:

- `DeathMatch`
- `TeamDeathMatch`
- `CaptureTheFlag`
- `Survival`
- `TeamSurvival`

There are also older abbreviated names in some legacy code paths:

- `DM`
- `TDM`
- `CTF`
- `SUV`
- `TSUV`

New work should prefer the canonical `OpenGSCore` names.

## Weapon Families

The game uses these high-level weapon families when grouping, filtering, or
applying weapon limits:

- `AR` - Assault Rifle
- `SR` - Sniper Rifle
- `SMG` - Submachine Gun
- `SG` - Shotgun
- `MG` - Machine Gun
- `HG` - Handgun
- `GR` - legacy gunner / handgun-family label used by older UI
- `Special Weapon` - unique or special-purpose weapons such as launchers and
  flamethrowers

These are the player-facing families. Specific weapon IDs and variants still
live in code and master data.

## Rule Flow

The rule layer should answer these questions:

- When does the match end?
- Who or what wins?
- What values should the result screen show?
- Can the player respawn?
- What events are important enough to send to the server?

## Practical Rule Split

For the current project structure, the safest split is:

- `OpenGSCore` owns the canonical rule definitions and result evaluators
- client scenes and UI consume the result payloads
- local test servers mimic the same contract for development

## Recommended Next Rule Work

1. Keep the canonical rule names stable.
2. Remove ambiguity between legacy abbreviations and canonical names.
3. Make sure every rule produces a predictable result payload.
4. Keep result UI aligned with the keys produced by the evaluators.
