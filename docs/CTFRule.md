# CTF Rule

## Core Loop

- Each team has a flag stand in its base.
- A flag sitting on a stand can be taken by the opposing team.
- If a player brings the enemy flag to their own stand while their own flag is still at base, the team scores.
- After a score, the flag state is reset.

## Flag Drop And Recovery

- If a flag carrier dies, the carried flag is dropped on the ground.
- The carrier's teammates can pick it up and keep carrying it.
- The opposing team can pick it up and return it to their own stand.

## Scoring Notes

- Only a successful capture scores points.
- Returning a dropped friendly flag to base is a reset action, not a score.

## Weapon Rules

- Normal field weapons are dropped world pickups under `Assets/Prefabs/Weapon/World/`.
- Normal field weapons auto-pick up when touched by an unarmed player.
- Normal weapon drops preserve the current magazine count.
- When a player swaps between main and secondary weapon slots, that weapon is refilled to full magazine.
- A dropped normal weapon can be picked up again and will restore its saved magazine count.
- Special weapons are use-limited and are handled separately from normal weapons.
- Special weapon field pickups can be taken even if the player is already holding a normal weapon.
- Special weapons stay equipped until their ammo runs out or the player dies, depending on the weapon behavior.
- Cluster grenades explode immediately on contact with stage objects or players.
- Field weapon pickups do not use gravity in the world and keep their rotation frozen.

## Event Meaning

- `FlagCaptured`: the enemy flag was delivered to your stand and the team scored.
- `FlagLost`: a carrier died and dropped the flag on the ground.
- `FlagPickup`: someone picked up a dropped flag and started carrying it again.
- `FlagReturn`: the owning team recovered its own flag back to base. This restores state, but it does not add score by itself.
