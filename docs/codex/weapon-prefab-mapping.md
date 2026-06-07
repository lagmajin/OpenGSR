# Weapon Prefab Mapping

This memo maps each `eWeaponType` to the most appropriate projectile prefab,
controller family, and the kind of per-weapon differences that should stay in
the prefab instead of being flattened into one shared asset.

## Principles

- Keep the weapon category controller shared when the firing model is the same.
- Keep the prefab per weapon when muzzle position, sprite shape, pellet count,
  recoil feel, or explosion behavior differs.
- Use `WeaponMasterData` for display sprites, sound, and high-level stats.
- Use prefab-level serialized values for weapon-specific firing feel.

## Recommended Mapping

| Weapon | Recommended Controller | Recommended Projectile / Weapon Prefab | Notes |
| --- | --- | --- | --- |
| `AK47` | `AssaultRifleController` | weapon prefab with `Bullet.prefab` | Standard AR tuning. |
| `M16` | `AssaultRifleController` | weapon prefab with `Bullet.prefab` | Same firing family, own muzzle/sprite. |
| `FAMAS` | `AssaultRifleController` | weapon prefab with `Bullet.prefab` | Burst/feel can diverge in prefab settings. |
| `F2000` | `AssaultRifleController` | weapon prefab with `Bullet.prefab` | Same as AR family. |
| `SteyAug` | `AssaultRifleController` | weapon prefab with `Bullet.prefab` | Keep its own muzzle offset. |
| `Scorpion` | `SMGController` | weapon prefab with `Bullet.prefab` | High ROF SMG. |
| `FN_P90` | `SMGController` | weapon prefab with `Bullet.prefab` | SMG family, own visuals. |
| `Uzi` | `SMGController` | weapon prefab with `Bullet.prefab` | Faster feel can be prefab-level. |
| `MP5` | `SMGController` | weapon prefab with `Bullet.prefab` | Same firing family. |
| `Scout` | `SniperRifleController` | weapon prefab with dedicated sniper projectile or `Bullet.prefab` | Single-shot long-range. |
| `Dragunov` | `SniperRifleController` | weapon prefab with dedicated sniper projectile or `Bullet.prefab` | Different muzzle and recoil feel. |
| `PSG1` | `SniperRifleController` | weapon prefab with dedicated sniper projectile or `Bullet.prefab` | Can share sniper logic. |
| `AWP` | `SniperRifleController` | weapon prefab with dedicated sniper projectile or `Bullet.prefab` | Heavy damage profile. |
| `MG42` | `MachineGunController` | weapon prefab with `Bullet.prefab` | Heat/recoil tuning differs. |
| `M60` | `MachineGunController` | weapon prefab with `Bullet.prefab` | Same family, different base stats. |
| `FNMinimi_SAW` | `MachineGunController` | weapon prefab with `Bullet.prefab` | Same family, own ammo feel. |
| `Glock` | `HandgunController` | weapon prefab with `Bullet.prefab` | Semi-auto pistol. |
| `DE` | `HandgunController` | weapon prefab with `Bullet.prefab` | Heavy pistol variant. |
| `LaserGun` | dedicated weapon controller | dedicated projectile prefab | Needs a custom shot model. |
| `BubbleGun` | dedicated weapon controller | dedicated projectile prefab | Likely not a normal bullet. |
| `ChirstmasGun` | dedicated weapon controller | dedicated projectile prefab | Special shot behavior. |
| `GrenadeLauncher` | `GrenadeLauncherController` | grenade projectile prefab | Uses `GrenadeProjectileController`. |

## Grenade Family

The grenade family should be treated as its own projectile line rather than a
normal bullet line.

| Projectile Prefab | Controller |
| --- | --- |
| `NormalGrenade.prefab` | `GrenadeProjectileController` |
| `PowerGrenade.prefab` | `GrenadeProjectileController` |
| `MagneticGrenade.prefab` | `GrenadeProjectileController` |
| `MineGrenade.prefab` | `GrenadeProjectileController` |
| `FireGrenade.prefab` | `GrenadeProjectileController` |
| `ClusterGrenade.prefab` | `GrenadeProjectileController` |
| `ChildClusterGrenade.prefab` | `ChildClusterGrenadeController` |

## Current Asset Reality

- The repository currently has one shared bullet projectile prefab:
  `Assets/Prefabs/Weapon/Projectile/Bullet.prefab`
- The grenade projectile line is already split into multiple prefabs.
- The old `BulletAgent` style scripts still exist, but the current gun
  controllers are the main path to preserve.
- `FieldAk47.prefab` and `FieldAwp.prefab` are world pickups, not firing
  projectiles.
- `WeaponListMasterData.asset` still uses a few legacy field names such as
  `Ak47`, `M4`, `Usa`, `Spas`, and `PSG`, so prefab naming should be normalized
  carefully instead of assuming the enum names already match the asset keys.
- `WeaponVisualResolver` now absorbs several common weapon aliases, so new
  prefabs can follow the canonical names while old assets keep working.

## Legacy Alias Decisions

These are the concrete normalization choices for the older asset names:

- `Ak47` -> `AK47`
- `M4` -> `M16`
- `Usa` -> `Uzi`
- `Spas` -> `Shotgun` family
- `PSG` -> `PSG1`

The important part is that the canonical prefab and display path should use the
normalized name, while the legacy alias remains only as an import/lookup
compatibility layer.

## First Completed Prefabs

The following weapon prefabs have been started as templates:

- `Assets/Prefabs/Weapon/Guns/Pistol/Glock.prefab`
- `Assets/Prefabs/Weapon/Guns/Pistol/DE.prefab`
- `Assets/Prefabs/Weapon/Guns/AR/AK47.prefab`
- `Assets/Prefabs/Weapon/Guns/AR/FAMAS.prefab`
- `Assets/Prefabs/Weapon/Guns/AR/F2000.prefab`
- `Assets/Prefabs/Weapon/Guns/AR/M4.prefab`
- `Assets/Prefabs/Weapon/Guns/AR/M16.prefab`
- `Assets/Prefabs/Weapon/Guns/SMG/Uzi.prefab`
- `Assets/Prefabs/Weapon/Guns/SMG/Scorpion.prefab`
- `Assets/Prefabs/Weapon/Guns/SMG/FN_P90.prefab`
- `Assets/Prefabs/Weapon/Guns/SMG/MP5.prefab`
- `Assets/Prefabs/Weapon/Guns/Sniper/PSG.prefab`
- `Assets/Prefabs/Weapon/Guns/Sniper/AWP.prefab`
- `Assets/Prefabs/Weapon/Guns/Sniper/Scout.prefab`
- `Assets/Prefabs/Weapon/Guns/Sniper/Dragunov.prefab`
- `Assets/Prefabs/Weapon/Guns/AR/SteyrAug.prefab`
- `Assets/Prefabs/Weapon/Guns/Shotgun/Spas.prefab`
- `Assets/Prefabs/Weapon/Guns/MG/M60.prefab`
- `Assets/Prefabs/Weapon/Guns/MG/MG42.prefab`

The firearm templates currently point at the shared `Bullet.prefab` projectile
and use the matching weapon master data sprites as the first visual pass.
`Spas` is currently a template-only shotgun family prefab because the repo does
not yet have a dedicated canonical shotgun master data asset.

## Practical Next Step

If we want to fully support all weapon variants cleanly, the next useful step
is to create one prefab per weapon under a structured folder such as:

- `Assets/Prefabs/Weapon/Guns/AR/`
- `Assets/Prefabs/Weapon/Guns/SMG/`
- `Assets/Prefabs/Weapon/Guns/Sniper/`
- `Assets/Prefabs/Weapon/Guns/Pistol/`
- `Assets/Prefabs/Weapon/Guns/MG/`
- `Assets/Prefabs/Weapon/Guns/Special/`

Each prefab should own:

- the correct `muzzle` transform
- the correct sprite layout
- the correct controller family
- the weapon-specific tuning values
- a matching `fieldWeaponPrefab` when the weapon can be dropped

Dropped field pickups live under `Assets/Prefabs/Weapon/World/` and use
`FieldWeaponController` to equip the paired player-held prefab.

## Build Order Recommendation

The safest order is to build the weapons in the same buckets used by the
current controller families:

1. Pistols: `Glock`, `DE`
2. Assault rifles: `AK47`, `M16`, `FAMAS`, `F2000`, `SteyAug`
3. SMGs: `Scorpion`, `FN_P90`, `Uzi`, `MP5`
4. Snipers: `Scout`, `Dragunov`, `PSG1`, `AWP`
5. Machine guns: `MG42`, `M60`, `FNMinimi_SAW`
6. Specials: `LaserGun`, `BubbleGun`, `ChirstmasGun`
7. Grenade launchers and grenade variants

This order works well because:

- the shared bullet projectile and hit utility are already in place
- the controller families already exist for most of the standard firearms
- the grenade family already has a richer projectile implementation

## Per-Weapon Prefab Checklist

For each weapon prefab, verify these fields before considering it done:

- muzzle transform exists and points the correct way
- gun sprite fits the silhouette
- shot effect spawns at the muzzle
- shell casing spawns at the correct side
- `bulletPrefab` references the correct projectile
- `Name` matches the weapon master data key
- `damage`, `magazine`, `bulletSpeed`, and `shotDelay` feel correct
- the prefab uses the right controller family
