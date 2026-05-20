# OpenGSR AI Work Instructions

This document is a copy-paste friendly prompt template for letting an AI edit the OpenGSR workspace safely.
日本語で使いたい場合は、そのまま下のテンプレだけ使って大丈夫です。

Before editing any Unity asset, read this first:
- [ASSET_SAFETY.md](./ASSET_SAFETY.md)
- [SPRITES_LAYOUT.md](./SPRITES_LAYOUT.md) when reorganizing anything under `Assets/Sprites`
- [SPRITE_NAMING.md](./SPRITE_NAMING.md) before renaming any sprite asset

## When to use
- Asset recovery
- Prefab and scene reference cleanup
- Folder moves and `.meta` preservation
- Small, scoped code fixes in a known area

- Prevention checklist: [ASSET_SAFETY.md](./ASSET_SAFETY.md)

## Core rules
- Always read `ASSET_SAFETY.md` before touching Unity assets.
- Read `SPRITES_LAYOUT.md` before reorganizing `Assets/Sprites`.
- Read `SPRITE_NAMING.md` before renaming sprites or sprite folders.
- Work inside the requested scope only.
- Treat each Unity asset as `file + .meta` unless it is a pure text file.
- When moving assets, preserve GUIDs by moving the `.meta` file with the asset.
- If a prefab, scene, or master data file references an asset, update the reference in the same task.
- After changes, verify with `git status` and `git diff --name-status`.
- Do not revert unrelated user changes.

## Safe prompt template
```text
Work only in: <scope>

Goal:
<what should be restored, moved, fixed, or cleaned up>

Rules:
- Do not touch files outside the scope.
- Keep Unity assets and their .meta files together.
- If you move or rename an asset, update all references in the same change.
- Preserve unrelated edits already in the worktree.
- After editing, report the exact files changed and summarize any remaining risk.

Verification:
- Show the relevant git status and changed file list.
- Confirm whether any references still point to the old path or GUID.
```

## 日本語版ひな形
```text
作業範囲: <scope>

やること:
<復旧 / 移動 / 修正 / 整理したい内容>

ルール:
- 範囲外のファイルは触らない。
- Unityアセットは本体と .meta を必ずセットで扱う。
- 移動や改名をしたら、同じ作業で参照先も直す。
- 既存の未コミット変更は壊さない。
- 変更後は git status と git diff --name-status を確認する。

確認:
- 変更したファイル一覧を出す。
- 旧パスや旧GUID参照が残っていないか確認する。
```

## Asset-specific checklist
- Prefab: keep `.prefab` and `.prefab.meta` together.
- Sprite/image: keep `.png` and `.png.meta` together.
- Scene: check inspector references after moves.
- Master data: confirm GUID-backed fields after any restore or rename.

## Good examples
- "Restore only `Assets/Prefabs/UI` and fix the master data reference."
- "Move the enemy weapon sprites into `Assets/Sprites/EnemyWeapon` and keep the meta files in sync."
- "Audit deleted assets under `Assets/Sprites` and recover only the missing ones from history."
