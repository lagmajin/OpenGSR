# Sprites Layout

`Assets/Sprites` は、Unity で今使っている素材を置く場所として扱う。

## Current top-level rule

- Root: actively used sprites that are still referenced by scene, prefab, master data, or code-path loading
- `EnemyWeapon/`: enemy weapon sprites that were grouped by name and usage checks
- `Archive/`: assets that looked unused after GUID and name-string checks

## Archive policy

- Move only after checking both:
  - GUID reference search
  - name-string search for path-based loading
- Keep `*.meta` together with the asset when moving
- If a sprite is later needed again, move it back together with its `*.meta`
- Archived asset names use an `arch_` prefix plus a normalized lowercase name
- Active root assets keep their original names until they are proven safe to rename

## Notes

- `weapon_mp5.png` stays at root because it still has a reference
- Some UI-looking assets stay at root because they are still name-referenced in code or scene data
- The archive is for organization, not deletion

## Where to look first

- Enemy weapons: `Assets/Sprites/EnemyWeapon`
- Archived UI-like assets: `Assets/Sprites/Archive/UI`
- Archived weapon-like assets: `Assets/Sprites/Archive/Weapon`
- Misc archived assets: `Assets/Sprites/Archive/Other`

## Helper tool

- Use `OpenGSR/Tools/Sprite Scene Usage Report` to generate a scene-by-scene sprite reference list
- The report scans both scenes and prefabs, then writes Markdown and JSON under `Assets/Reports`
- It is meant as a recovery aid, not a deletion tool

## Naming guide

- See [SPRITE_NAMING.md](./SPRITE_NAMING.md) for the active naming conventions and safe rename rules
- Use `OpenGSR/Tools/Sprite Rename Audit` when you want to check whether root sprites are safe to rename
