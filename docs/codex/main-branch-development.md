# Main Branch Development

This repository is set up for direct work on `main` during solo development.

## Default Flow

1. Work on `main`.
2. Keep changes small and intent-focused.
3. Commit when the change is in a clean, usable state.
4. Push directly to `origin/main`.

## When To Branch

Create a short-lived branch only when the work is risky or long-running:

- large refactors
- experimental changes
- destructive cleanup
- changes that should not be mixed with the current task

## Commit Sizing

Prefer commits that can be explained in one sentence:

- lobby/UI wiring
- network behavior
- asset/meta cleanup
- gameplay logic cleanup

If a change would be hard to describe clearly, split it first.

## Safety Rule

If unrelated edits start piling up, pause and separate them before touching `main` again.

## Main-PC Marker

- Marker path: `%USERPROFILE%\.codex-mainpc`
- If this file exists, treat the current PC as the main development machine
- On the main development machine, direct commits and pushes to `main` are OK
- On other machines, prefer a short-lived branch first and merge back later
