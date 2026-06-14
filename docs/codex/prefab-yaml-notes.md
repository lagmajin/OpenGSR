# Prefab YAML Notes

This memo records the practical Unity prefab rules we rely on when editing
`.prefab` files directly. It is not a full Unity serialization reference.

## When To Edit YAML

- Prefer the Unity Editor for structural changes.
- Edit YAML directly only for small, repeatable fixes or when cloning a known
  working prefab template.
- If a prefab contains nested objects, components, or cross references, verify
  the result in Unity after the edit.

## Core Rules

- A prefab file is YAML with one or more Unity object blocks.
- Each block starts with a Unity type tag such as `--- !u!1` for `GameObject`
  or `--- !u!114` for `MonoBehaviour`.
- The `&fileID` value identifies the serialized object inside the file.
- A `GameObject` lists its component file IDs under `m_Component`.
- A `Transform` block points back to its `m_GameObject`.
- A `MonoBehaviour` block points to its `m_GameObject` and to its script GUID
  via `m_Script`.

## Reference Shape

- Scene and prefab references use the shape:
  - `fileID`
  - `guid`
  - `type`
- `type: 3` is commonly used for prefab and asset references in this project.
- `type: 2` is commonly used for ScriptableObject or asset references.
- The referenced `fileID` must match the target object inside the asset.

## Meta Files

- Every asset file needs a matching `.meta` file.
- The `.meta` file owns the asset GUID.
- If a `.meta` file is missing or replaced, every reference to that asset will
  break until Unity regenerates the GUID and the references are repaired.
- Never invent GUIDs for an existing asset unless you are intentionally creating
  a brand-new asset.

## Naming Conventions Used Here

- Player-held weapon prefabs live under `Assets/Prefabs/Weapon/Guns/<family>/`.
- World pickup prefabs live under `Assets/Prefabs/Weapon/World/`.
- Canonical names should match the master data key, for example `AK47`,
  `M16`, `FAMAS`, `F2000`, `SteyrAug`.
- Keep the `GameObject.m_Name`, the prefab filename, and the master data key
  aligned whenever possible.
- Use alias handling in code for older names, not duplicate prefab names.

## Practical Prefab Pattern

For a weapon prefab in this repo, the stable pattern is:

- root `GameObject`
- root `Transform`
- `SpriteRenderer`
- `MultipleTags`
- weapon controller `MonoBehaviour`
- child `Muzzle` `GameObject` with its own `Transform`

For a world pickup prefab, the stable pattern is:

- root `GameObject`
- root `Transform`
- `FieldWeaponController`
- `Rigidbody2D`
- `SpriteRenderer`

## Weapon-Specific Fields

- `Name` should match the canonical weapon ID used by master data.
- `bulletPrefab` should point to the projectile prefab when the controller
  expects one.
- `fieldWeaponPrefab` should point to the matching world pickup prefab when the
  weapon is meant to drop.
- `data` should point to the matching `WeaponMasterData` asset when one exists.
- `muzzle` should point to the child muzzle transform, not the root transform.

## Sanity Checks

- The prefab opens in Unity without missing script warnings.
- The sprite reference resolves to the expected sprite.
- The weapon equips correctly from the world pickup.
- The weapon fires the expected projectile type.
- The master data entry resolves through `WeaponVisualResolver`.

## Current Repo Notes

- `AK47.prefab` and `WorldAK47.prefab` are the base template pair for the AR
  family.
- `M16`, `FAMAS`, `F2000`, and `SteyrAug` should follow the same pattern.
- `GM94.prefab` is a grenade-launcher style template, not an AR template.

