# MovementSystem — Agent Reference

## Overview

The MovementSystem handles all player locomotion: walking, sprinting, crouching, sliding, jumping, wall-running, vaulting, head-bobbing, and camera look. It is **Rigidbody-driven** — every movement state applies forces or velocity to the player's `Rigidbody` rather than moving the transform directly.

Input comes from a single generated `MovementSystem` C# class (from `MovementSystem.inputactions`). The input asset defines two action maps: `Movement` (locomotion) and `Weapon` (consumed by the WeaponSystem). The `Movement` map is enabled here; the `Weapon` map is enabled in `WeaponController`.

---

## Component Map

| Script | Responsibility |
|---|---|
| `Movement.cs` | Core player controller — owns the Rigidbody, all locomotion states, and the `Look()` camera rotation method |
| `MoveCamera.cs` | Keeps the camera rig's world position locked to the player by copying `player.position` in `LateUpdate` |
| `Parkour.cs` | Obstacle vault — detects vaultable surfaces via raycast and lerps the player over them kinematically |
| `SpeedFX.cs` | FOV push effect — widens camera FOV based on horizontal speed and wall-jump boost |

All four components live on (or are children of) the same player GameObject. They communicate by reading public fields on `Movement` directly.

---

## Movement.cs — Detailed Breakdown

### Key Configurable Fields

| Field | Default | Purpose |
|---|---|---|
| `runSpeed` | 6500 | Force applied while sprinting |
| `walkSpeed` | 1000 | Force applied while walking |
| `crouchSpeed` | 2000 | Force applied while crouching |
| `maxSpeed` | 7 | Velocity cap — also written by `WeaponController` during ADS |
| `sensitivity` | 50 | Mouse sensitivity scalar |
| `jumpForce` | 450 | Impulse applied on jump |
| `slideForce` | 400 | Forward impulse applied on slide start |
| `slideThreshold` | 4.5 | Minimum speed needed to transition crouch → slide |
| `wallrunForce` | 3500 | Continuous forward force during wall-run |
| `maxWallRunCameraTilt` | 15° | Maximum camera roll during wall-run |
| `maxSlopeAngle` | 35° | Maximum floor angle that counts as `grounded` |

### Locomotion States

States are not mutually exclusive flags in an enum — they are boolean properties derived from input + physics conditions:

| State | Condition |
|---|---|
| `grounded` | A contact normal within `maxSlopeAngle` of up exists (`OnCollisionStay`) |
| `isSprinting` | Sprint button held + `grounded` + not wall-running + not sliding + not crouching |
| `crouching` | Crouch button held (set via `StartCrouch` / `StopCrouch`) |
| `isSliding` | Crouched while moving above `slideThreshold`; exits when speed drops below `slideExitSpeed` |
| `isWallRunning` | Wall detected on left or right + Wallrun button held + not grounded |
| `jumping` | Public bool — set true during jump, not currently read back inside this script |

### Input Bindings (Movement action map)

| Action | Handler |
|---|---|
| `Walk` (Vector2) | Read each `FixedUpdate` in `Move()` |
| `MouseLook` (Vector2) | Read each `Update` in `Look()` |
| `Jump` (Button) | `Jump()` callback — suppressed when a wall is detected |
| `Crouch` (Button, hold) | `StartCrouch` / `StopCrouch` |
| `Sprint` (Button, hold) | Sets `sprintInputHeld` |
| `Wallrun` (Button, hold) | Sets `wallrunButtonHeld`; release triggers `WallJump()` |

### Look() — Camera Rotation

`Look()` runs every `Update`. It accumulates **mouse intent** separately from recoil:

- `desiredX` — accumulated yaw from mouse input
- `xRotation` — accumulated pitch from mouse input, clamped to [−90°, +65°]

The actual camera rotation is:

```
playerCam.localRotation = Euler(xRotation + recoilPitchOffset, desiredX + recoilYawOffset, wallRunCameraTilt)
```

`recoilPitchOffset` and `recoilYawOffset` are **written every frame by `WeaponRecoil`**. Recovery inside `WeaponRecoil` interpolates these offsets back toward zero — the player's intended aim (`xRotation`, `desiredX`) is preserved and never overwritten by recoil.

> **Note on pitch sign:** `xRotation` is inverted — looking up means a more negative value. `WeaponRecoil` accounts for this when mapping vertical kick to `recoilPitchOffset`.

### Head Bobbing

`HeadBob()` runs every `FixedUpdate`. When grounded and moving (`rb.linearVelocity.magnitude > 0.5`), it oscillates `camera.transform.localPosition.y` using `Mathf.Sin(timer) * runBobAmount`.

The public field `bobOffset` carries the current Y delta. `WeaponSway` reads this to stay in sync — **do not also write `camera.transform.localPosition.y` from other scripts**, as that is owned by this method.

### Counter-Movement

`CounterMovement()` is called every `FixedUpdate` from `Move()`. It applies braking forces to:
1. Decelerate the player when directional input is released or reversed.
2. Cap diagonal movement to `maxSpeed`.

### Frictionless Collider

`Awake()` programmatically attaches a `PhysicsMaterial` with zero friction and `Minimum` combine mode so the player glides along walls and floors without unexpected drag from scene collider materials.

---

## Parkour.cs — Vault System

`Parkour` reads `Movement.grounded`, `Movement.isSprinting`, and `Movement.orientation` each `FixedUpdate`. Vault sequence:

1. **`CheckForWall()`** — casts a ray at shin height forward. If an obstacle is hit and its AABB top is below 65% of character height, `isWallVaultable = true`.
2. **`StartVault()`** — called when sprinting into a vaultable surface while grounded. Zeroes velocity, sets `rb.isKinematic = true`, computes the landing position via a downward scan ray.
3. **`TickVault()`** — lerps the player to the target position via `rb.MovePosition` each `FixedUpdate`.
4. **`EndVault()`** — restores physics, exits crouch state, relaunches with `preVaultSpeed` forward.

The vault calls `movement.StartCrouch(default)` / `movement.StopCrouch(default)` to handle crouch state transitions cleanly without duplicating logic.

---

## SpeedFX.cs — Speed FOV

Runs in `LateUpdate`. Computes a `speedT` in [0, 1] mapping horizontal speed from `maxSpeed` to `maxSpeed * 2`. Also computes a `boostT` from `movement.lastWallJumpTime` that decays over `boostDecay` seconds. The camera FOV is lerped toward `Lerp(baseFOV, maxFOV, max(speedT, boostT * boostStrength))`.

> **Important:** `WeaponController` also writes `Camera.fieldOfView` for ADS zoom. `SpeedFX` writes the same camera. Ensure `SpeedFX.baseFOV` matches `WeaponController.baseFOV`. The intended pattern is that `WeaponController` drives ADS FOV when aiming and `SpeedFX` drives speed FOV at other times — they both lerp, so blending happens naturally unless both are simultaneously active.

---

## Public API — Cross-System Hooks

These fields on `Movement` are read or written by the WeaponSystem:

| Field | Direction | Consumer |
|---|---|---|
| `recoilPitchOffset` | Written by WeaponSystem | `WeaponRecoil` writes, `Look()` adds on top of mouse input |
| `recoilYawOffset` | Written by WeaponSystem | `WeaponRecoil` writes, `Look()` adds on top of mouse input |
| `bobOffset` | Read by WeaponSystem | `WeaponSway` reads to sync viewmodel bob |
| `isSprinting` | Read by WeaponSystem | `WeaponController` blocks fire; `WeaponSprintPose` drives sprint rig |
| `grounded` | Read by WeaponSystem | `WeaponSway` gates bob to ground contact |
| `crouching` | Read by WeaponSystem | `WeaponSway` gates bob (no bob while crouching) |
| `maxSpeed` | Written by WeaponSystem | `WeaponController` multiplies it by `aimMoveSpeedMultiplier` on ADS |
| `lastWallJumpTime` | Read by SpeedFX | Used to compute wall-jump FOV boost decay |
| `camera` | Read by WeaponSystem | `WeaponController` reads this to find the view camera |
| `InputSystem` | Read by WeaponSystem | `WeaponController` instantiates its own `MovementSystem` for the Weapon map |

---

## Expanding the Movement System

### Adding a new locomotion state
1. Add the condition boolean(s) as properties or fields on `Movement`.
2. Add any needed input bindings to `MovementSystem.inputactions` and re-generate the C# class.
3. Subscribe to the new input action in `Awake()`.
4. Call the state logic from `FixedUpdate` (physics forces) or `Update` (camera/look).
5. Guard `Move()` / `CounterMovement()` with the new state if needed (see how `isSliding` returns early from `Move()`).

### Adding a new parkour move
1. Add the detection logic in a new `CheckFor*()` method called from `FixedUpdate`.
2. Implement `Start*()`, `Tick*()`, `End*()` following the vault pattern.
3. Gate activation on relevant `Movement` state flags (e.g. `isSprinting`, `grounded`).

### Adding a new camera effect
Follow the `recoilPitchOffset` / `recoilYawOffset` pattern: add `[HideInInspector] public float` offset fields to `Movement`, have the external script write them each frame, and add them inside `Look()` on top of the mouse-driven angles. Do **not** overwrite `xRotation` or `desiredX` directly.

### Adding a new speed-dependent FOV effect
Extend `SpeedFX.LateUpdate()` by compositing additional `t` values into `finalT` before the FOV lerp.
