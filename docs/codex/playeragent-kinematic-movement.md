# PlayerAgent Kinematic Movement

## Goal

`PlayerAgent` is intended to move mostly by script, not by dynamic 2D physics.
`Rigidbody2D` remains only to keep trigger interactions and contact callbacks stable.

## Core Policy

- Use `RigidbodyType2D.Kinematic`
- Set `gravityScale = 0`
- Do not use `AddForce` for normal movement, ground adhesion, or falling
- Treat movement as "desired delta per fixed tick"
- Before applying that delta, cast the player collider shape and trim movement to the hit surface
- Keep `jumpHeight` as the designer-facing jump value and derive the actual jump velocity from gravity

## FixedUpdate Flow

1. Probe current grounded state
2. Consume buffered jump input with coyote time support
3. Update horizontal velocity by script
4. Update vertical velocity by scripted gravity
5. Move on the combined scripted delta with cast-based clipping
6. Snap slightly downward to stay attached to walkable ground
7. Probe grounded state again for the final frame result

## Grounding

- Grounding is based on a downward `BoxCast`
- A surface counts as ground only if its normal is within `maxGroundAngle`
- Landing resets downward speed and notifies landing-related systems
- `groundProbeDistance`, `groundSnapDistance`, `coyoteTime`, and `jumpBufferTime` are the main tuning knobs for feel
- Small gaps and shallow slopes are handled by a short downward snap after movement

## Collision Resolution

- Horizontal and vertical movement are resolved separately
- Each axis checks the next movement distance plus a small skin width
- If a hit is found, movement stops just before the surface
- Downward hits on walkable normals set grounded
- Upward hits cancel jump rise
- Horizontal hits cancel horizontal movement into walls

## Why This Direction

The previous approach mixed:

- `Rigidbody2D`
- `transform.Translate`
- post-move penetration correction
- manual gravity

That combination tends to cause:

- sinking into floors
- jitter against slopes and corners
- being pushed through edges
- unstable falling near scene boundaries

The current direction keeps the movement deterministic and easier to tune.
