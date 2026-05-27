# Grenade Types

Current grenade types in the project and how they are wired.

## Core enum

- `EGrenadeType` lives in `Packages/com.opengs.logic/Item/Grenade.cs`
- Current values:
  - `Normal`
  - `Power`
  - `Magnetic`
  - `Mine`
  - `Cluster`
  - `ClusterChild`
  - `Fire`
  - `Empty`

## Runtime throw path

- `Assets/Scripts/Player/AsmExport/PlayerGrenadeComponent.cs`
  - Picks a grenade type from `EGrenadeType`
  - Resolves a matching entry from `AllGrenadeListMasterData`
  - Instantiates the prefab and launches it via `GrenadeProjectileController`

## Projectile prefabs

- `Assets/Prefabs/Weapon/Projectile/NormalGrenade.prefab`
- `Assets/Prefabs/Weapon/Projectile/PowerGrenade.prefab`
- `Assets/Prefabs/Weapon/Projectile/FireGrenade.prefab`
- `Assets/Prefabs/Weapon/Projectile/ClusterGrenade.prefab`
- `Assets/Prefabs/Weapon/Projectile/MineGrenade.prefab`
- `Assets/Prefabs/Weapon/Projectile/MagneticGrenade.prefab`

## Master data

- `Assets/Resources/MasterData/Grenade/AllGrenadeListMasterData.asset`
  - Central grenade list used by player throw logic
- `Assets/Resources/MasterData/Grenade/NormalGrenade.asset`
- `Assets/Resources/MasterData/Grenade/PowerGrenade.asset`
- `Assets/Resources/MasterData/Grenade/FireGrenade.asset`
- `Assets/Resources/MasterData/Grenade/ClusterGrenade.asset`
- `Assets/Resources/MasterData/Grenade/MineGrenade.asset`
- `Assets/Resources/MasterData/Grenade/MagneticGrenade.asset`

## Special behavior

- `Normal`, `Power`, `Fire`, `Mine`, `Magnetic`
  - Use the shared `GrenadeProjectileController`
- `Cluster`
  - Uses `GrenadeProjectileController` with child projectile spawning enabled
- `Smoke`
  - There is an old `SmokeGrenadeController`, but it is not currently part of `EGrenadeType`
  - If we want smoke as a real throw type, it needs a separate wiring pass

## Notes

- `ClusterChild` exists in the core enum, but it is currently treated as an internal / child-only concept
- If you add a new grenade type, update:
  - `EGrenadeType`
  - `AllGrenadeListMasterData`
  - the projectile prefab under `Assets/Prefabs/Weapon/Projectile`
  - `InstantItemThumbnailMasterData` if the UI needs an icon

