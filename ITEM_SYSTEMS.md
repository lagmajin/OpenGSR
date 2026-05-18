# Item Systems Status

This document summarizes the current item-related implementation state in
OpenGSR and the older OpenGS sources.

## What Already Exists

### Field Items

The project already has a real field-item loop:

- spawn timing and alternation logic in `Packages/com.opengs.logic/Item/FieldItemService.cs`
- runtime item state tracking in `Assets/Scripts/Item/FieldItemNetworkManager.cs`
- item spawn points in `Assets/Scripts/Core/Base/Gimmick/ItemSpawnPoint.cs`
- field-item classes in `Assets/Scripts/Core/Base/Item/AbstractFieldItem.cs`
- field-item event strings in `Packages/com.opengs.logic/Event/EventString.cs`

This means the game already supports the idea of:

- spawning pickup items in the arena
- being picked up by a player
- respawning or despawning them later

### Instant / Slot-Based Items

The project also has a partial instant-item system:

- slot storage in `Assets/Scripts/Item/InstantItemSlot.cs`
- save/load support in `Assets/Scripts/Systems/EquipmentSaveManager.cs`
- equipped-slot persistence in `Assets/Scripts/Systems/UserSaveManager.cs`
- player input entry points in `Assets/Scripts/Player/AsmExport/PlayerInput.cs`
- a usage entry point stub in `Assets/Scripts/Player/CharaController.cs`
- a match-level usage hook in `Assets/Scripts/Match/MatchEventProvider.cs`

So the design already expects the player to:

- equip instant items into slots
- trigger them from input
- forward the action into match logic
- bring up to 3 instant items into a match

This makes the instant-item system a pre-match loadout layer, not a separate
shop-only concept.

### Grenades And Booster

There is also a separate combat-resource layer:

- grenade count and refill/consume logic in `Assets/Scripts/Interface/PlayerStatus.cs`
- grenade throw handling in `Assets/Scripts/Player/AsmExport/PlayerGrenadeComponent.cs`
- booster consume/refill logic in `Assets/Scripts/Player/AsmExport/PlayerController.cs`
- booster-related UI in `Assets/Scripts/UI/PlayerStatusUIManager.cs`

This is not the same thing as shop items, but it is still part of the "item use"
experience from a player perspective.

## What Is Still Missing

The current gaps are mostly in the actual "use" path:

- `CharaController.UseItem(int num)` is empty
- `MatchEventProvider.UseInstantItem(EInstantItemType type)` is empty
- `InstantItemSlots` is only partially implemented
- there is no clearly authoritative network message path for instant-item use
- field-item pickup and instant-item activation are not yet fully unified in one
  canonical gameplay contract

## Legacy Signals

Old OpenGS sources also point to an item system that was intended to be broader:

- grenade packs
- heal / band-aid style instant items
- power-up / defense-up field items
- weapon pickup items

The old docs and code suggest the item system was meant to cover both:

- world pickups
- inventory/slot consumables

## Practical Reading

The short version is:

- yes, item use already exists in pieces
- no, it is not fully wired end-to-end yet
- the strongest half-finished area is instant-item activation from input into
  match/server logic

## Suggested Next Step

If we want to make the item system real, the next milestone should likely be:

1. define the canonical instant-item protocol in `OpenGSCore`
2. wire `UseItem` from input to match logic
3. make item consumption update player state and UI
4. keep field-item pickup and instant-item usage consistent
