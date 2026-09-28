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
- Normal and dropped weapons are claimed by the first player who touches them.
- If the toucher can equip the weapon, pickup happens immediately.
- The claim is only a short-lived anti-duplicate guard, not a long lock.
- Normal weapon drops preserve the current magazine count.
- When a player swaps between main and secondary weapon slots, that weapon is refilled to full magazine.
- Special weapons are use-limited and are handled separately from normal weapons.
- Special weapon field pickups can be taken even if the player is already holding a normal weapon.
- Special weapons are effectively disposable field items: once picked up, they stay in the special slot until their ammo runs out.
- Special weapon ammo is still tracked per weapon, and the special slot consumes it on use.
- Special weapons are cleared on player death.
- Cluster grenades explode immediately on contact with stage objects or players.
- Field weapon pickups do not use gravity in the world and keep their rotation frozen.

## Flag State

- Where a flag is comes from the shared vocabulary: `FlagOnStand`, `FlagCapturedPlayer`, `FlagOnGround`.
- The server holds where every flag is. The client raises a claim when a flag moves and adopts the ruling.
- A claim names the flag's team, not the carrier's team: a red player picks up the blue flag.
- A player cannot pick up its own team's flag. Recovering it is a return, which is a different rule.
- A flag is one object: it cannot be picked up twice at once, so the second claimant does not get it.
- Only the player actually holding a flag can report it lost.
- A player who leaves puts the flag they were carrying back on its stand.

## Scoring

- The server scores. The client does not count a capture itself and does not push a score.
- A delivery scores only while the carrying team's own flag is on its stand, and only when that player is the one holding the enemy flag.
- A refused delivery is answered to the player who made it, with the reason, rather than being dropped.
- A score resets both flags to their stands, and the delivered flag is destroyed.
- `FlagReturn` restores state and is not a score by itself.

## Event Meaning

- `FlagCaptured`: the enemy flag was delivered to your stand and the team scored.
- `FlagLost`: a carrier died and dropped the flag on the ground.
- `FlagPickup`: someone picked up a dropped flag and started carrying it again.
- `FlagReturn`: the owning team recovered its own flag back to base. This restores state, but it does not add score by itself. It carries a `ReturnReason`, so a flag that came back by itself is not mistaken for one a player carried home.
- `FlagBurst`: the server destroyed a flag, which happens as part of a score. The
  server decides whose flag it was and tells the whole room. A client never claims
  this: a flag going is the rule's outcome, so a client that could assert it could
  destroy a flag sitting safely on its own stand.
- `FlagCaptureRefused`: a delivery that did not score, answered to the player who made it.
