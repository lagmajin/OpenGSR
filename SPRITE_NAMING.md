# Sprite Naming Guide

This guide is for naming and reorganizing sprite assets without breaking the project.

## Rules

- Keep any sprite that is still referenced by name or path in place until the reference is audited.
- Use folder names to express meaning first, then use file names for the asset role.
- For new assets, prefer lowercase prefixes that match the folder role.
- Archive-only assets use the `arch_` prefix.

## Current conventions

- `Assets/Sprites/EnemyWeapon/`: enemy weapon sprites
- `Assets/Sprites/Archive/UI/`: archived UI sprites, named with `ui_`
- `Assets/Sprites/Archive/Weapon/`: archived weapon sprites, named with `arch_weapon_`
- `Assets/Sprites/Archive/Other/`: miscellaneous archived sprites, named with `arch_other_`

## Active root sprites

The following root sprites are still intentionally left in place because they have active references or string-based loads:

- `CTF.png`
- `Don_Body.png`
- `GameStart.png`
- `health.png`
- `Logo.png`
- `scope.png`
- `SUV.png`
- `TDM.png`
- `weapon_mp5.png`

## Suggested future naming

- UI screens and icons: `ui_<purpose>.png`
- Player or character sprites: `char_<name>.png`
- Battle overlays and effects: `battle_<purpose>.png`
- Weapon display icons: `weapon_<name>.png`

## Safe workflow

- First check GUID references.
- Then check string-based name references.
- If either exists, do not rename casually.
- If a rename is still needed, move the `*.meta` together and update all references in the same change.

## Helper tool

- Use `OpenGSR/Tools/Sprite Rename Audit` to generate a rename-safety report for active root sprites
