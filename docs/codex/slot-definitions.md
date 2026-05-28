# Slot Definitions

This project uses two different slot systems that are easy to mix up.

## Instant Item Slots

- Location:
  - `Assets/Scripts/Item/InstantItemSlot.cs`
  - `Assets/Scripts/Systems/UserSaveManager.cs`
  - `Assets/Scripts/Player/CharaController.cs`
- Purpose:
  - Store the player's equipped instant items
  - These are the 3 loadout slots shown in the shop / equipment flow
- Contents:
  - `HealthKit`
  - `FireBullet`
  - `PoisonBullet`
  - `PowerGrenadePack`
  - `ClusterGrenadePack`
  - `MagnetGrenadePack`
  - `MineGrenadePack`
- When they are filled:
  - Loaded from the equipped loadout on spawn / respawn
  - Consumed when the player uses the corresponding instant item
- What they do:
  - They do not directly mean "ammo"
  - They are loadout items that may refill grenade slots or apply another buff/effect

## Grenade Slots

- Location:
  - `Assets/Scripts/Interface/PlayerStatus.cs`
  - `Packages/com.opengs.logic/Player/PlayerStatus.cs`
  - `Assets/Scripts/Player/AsmExport/PlayerGrenadeComponent.cs`
- Purpose:
  - Store the actual throwable grenade inventory
  - The player can throw from these slots
- Capacity:
  - 3 slots
- Contents:
  - `Normal`
  - `Power`
  - `Magnetic`
  - `Mine`
  - `Cluster`
  - `Fire`
  - `Smoke`
- When they are filled:
  - By spawn / respawn recovery logic
  - By instant item effects such as grenade packs
  - By field items or match effects that call `RefillGrenade(...)`
- What they do:
  - They represent actual throwable ammo
  - `PlayerGrenadeComponent` consumes them when a grenade is thrown

## Important Difference

- `Instant Item Slot` = what the player has equipped as a loadout item
- `Grenade Slot` = what the player can throw right now
- A grenade pack instant item may refill grenade slots, but it is not itself a grenade slot

## Current Spawn Behavior

- Spawn / respawn loads equipped instant items into the instant item slots
- In `CharaController`, grenade slots are not yet auto-refilled from those equipped instant items
- If you want spawn-time grenade refill based on equipped instant items, that needs a dedicated hook and should be added explicitly
