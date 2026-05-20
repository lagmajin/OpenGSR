# OpenGSR Asset Safety

This note is about preventing Unity asset regressions rather than restoring them after the fact.

## What tends to break
- Moving a Unity asset without its `.meta`
- Editing a scene while the inspector still has stale references
- Creating a prefab but not committing it right away
- Letting the same UI exist partly in scene objects and partly in master data

## Default rule
- Treat `asset + .meta + references` as one unit of change.

## Safe workflow
1. Identify the scope before editing.
2. Move or rename the asset together with its `.meta`.
3. Update scene, prefab, and master-data references in the same change.
4. Check `git status` for unexpected deletes or untracked files.
5. Verify with `git diff --name-status` before considering the change done.

## Prefer this structure
- Reusable UI or dialog: prefab + `.meta` + master-data entry
- Scene-local wiring: keep it in the scene, but avoid spreading the same reference across multiple scene objects
- Sprite groups: keep a stable folder, and move the `.meta` with the folder asset

## Red flags
- `fileID: 0` on a field that should clearly be wired
- a prefab or sprite folder showing up as untracked after a move
- scene files changing while the intended asset was not committed yet
- two places trying to own the same dialog or widget

## Practical habit
- If the asset matters, commit it soon.
- If the asset is only an experiment, keep it clearly isolated so it can be deleted safely later.
