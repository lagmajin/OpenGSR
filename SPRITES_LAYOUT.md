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

## Notes

- `weapon_mp5.png` stays at root because it still has a reference
- Some UI-looking assets stay at root because they are still name-referenced in code or scene data
- The archive is for organization, not deletion

## Where to look first

- Enemy weapons: `Assets/Sprites/EnemyWeapon`
- Archived UI-like assets: `Assets/Sprites/Archive/UI`
- Archived weapon-like assets: `Assets/Sprites/Archive/Weapon`
- Misc archived assets: `Assets/Sprites/Archive/Other`
