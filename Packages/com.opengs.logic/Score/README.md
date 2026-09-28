# Score Layer Guide

This folder now keeps only the score types that are actively used by the match flow.

## Canonical responsibilities

- `MatchFinalScore.cs`
  - Per-match final score containers for DeathMatch, TeamDeathMatch, and CTF.
- `MatchPlayerFinalScore.cs`
  - Per-player final score data: kills, deaths, suicides, total points, rank.
- `PlayerFinalScoreCalcurator.cs`
  - Legacy-compatible wrapper around the correctly spelled `PlayerFinalScoreCalculator`.
- `PlayerLifeTimeScore.cs`
  - Lifetime / account-wide match counters.
- `Match\Result\*`
  - Match result evaluators and result payloads used by the match flow.
- `Match\Result\MatchResultResolver.cs`
  - Central resolver shared by the legacy `MatchResultService` and `MatchResultFactory`.

## Removed legacy placeholders

- `MatchResultScore.cs`
- `InGameScore.cs`
- `MissionFinalScore.cs`
- `MissionResultScore.cs`
- `AllPlayerMissionFinalSocre.cs`

These files were empty or duplicated responsibilities that are now handled by the canonical score and result classes above.
