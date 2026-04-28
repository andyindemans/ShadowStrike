# WeaponSystem — Agent Reference

## Overview

The WeaponSystem is a three-layer, event-driven architecture:

1. **Data** — `ScriptableObject` assets define weapon config, ammo ballistics, attachment modifiers, and pose/recoil tuning.
2. **Firing** — a strategy-pattern layer that resolves a shot into a hit or a spawned projectile.
3. **Runtime** — `MonoBehaviour` components composed on prefabs that handle inventory, input, stat resolution, animation, recoil, ADS pose, sprint pose, and viewmodel sway.

No central "tick coordinator" exists. The `Weapon` component raises C# events (`OnFired`, `OnReloadStart`, etc.) and every other system subscribes. When adding new reactions (audio, UI, VFX, animation), **subscribe to events — do not poll `Weapon` state**.

---

## Directory Structure

```
WeaponSystem/
  Data/           ScriptableObject configs
  Firing/         IFirer strategy, FireContext, helpers
  Runtime/        MonoBehaviour components + WeaponStats struct
```

Asset instances live in `Assets/WeaponAssets/`. The `CreateAssetMenu` path root is `ShadowStrike/Weapons/…`.

---

## Layer 1 — Data

### WeaponData _(ScriptableObject)_

The master config for one weapon type. Create via `Assets > ShadowStrike > Weapons > Weapon Data`.

| Field group | Key fields |
|---|---|
| Identity | `id`, `displayName` |
| Firing | `firingMode` (Hitscan / Projectile), `fireRate` (rounds/sec), `range`, `baseRecoil`, `recoilProfile` |
| Spread | `hipSpread`, `adsSpread` (degrees) |
| Magazine | `magSize`, `reloadDuration` |
| Handling | `equipDuration`, `holsterDuration`, `aimFOV`, `aimMoveSpeedMultiplier`, `adsProfile`, `oneHanded`, `sprintPoseProfile` |
| Ammo | `acceptedAmmo` (list), `defaultAmmo` |
| Visuals | `viewModelPrefab`, `animatorOverride` |
| Customization | `allowedAttachmentSlots` (flag mask of `AttachmentSlot`) |

### AmmoType _(ScriptableObject)_

Defines the ballistic properties of one ammunition family. Create via `Assets > ShadowStrike > Weapons > Ammo Type`.

| Field | Purpose |
|---|---|
| `baseDamage` | Damage per pellet/bullet |
| `muzzleVelocity` | m/s — initial speed for `ProjectileFirer` |
| `gravityMultiplier` | 0 = straight line, 1 = full gravity |
| `projectilePrefab` | Assign for projectile mode; null for hitscan-only |
| `pelletsPerShot` | 1 = single bullet, >1 = shotgun scatter |
| `reserveCap` | Maximum reserve ammo count |

### AttachmentData _(abstract ScriptableObject)_

Base class for all attachments. Concrete subclasses:

| Subclass | Menu path | Extra fields |
|---|---|---|
| `BarrelAttachment` | `…/Attachments/Barrel` | — |
| `MuzzleAttachment` | `…/Attachments/Muzzle` | `muzzleFlashPrefab` |
| `ScopeAttachment` | `…/Attachments/Scope` | `overrideAimFOV`, `aimFOVOverride`, `scopeOverlayPrefab` |

All share two modifier passes (see Stat Resolution below):

- **Additive:** `damageAdd`, `spreadAdd`, `recoilAdd`, `rangeAdd`
- **Multiplicative (default 1):** `damageMult`, `spreadMult`, `recoilMult`, `rangeMult`, `aimSpeedMult`

`AttachmentSlot` is a `[Flags]` enum (`Barrel`, `Muzzle`, `Scope`, `Grip`). Each concrete class sets its own `slot` in `OnValidate()`. `WeaponData.allowedAttachmentSlots` is a bitmask; `Weapon.CanAccept()` enforces it.

### RecoilProfile _(struct, embedded on WeaponData)_

Serialized inline — no separate asset.

| Group | Fields |
|---|---|
| Camera kick | `verticalKick`, `verticalKickVariance`, `horizontalKick`, `horizontalBias` (−1…+1) |
| Recovery | `recoveryDelay`, `recoverySpeed`, `recoveryFraction` (0 = permanent, 1 = full snap back) |
| ADS multipliers | `hipCameraRecoilMultiplier`, `aimCameraRecoilMultiplier`, `aimVisualRecoilMultiplier` |
| Visual gun kick | `visualPositionKick`, `visualRotationKick`, `visualPositionStiffness`, `visualRotationStiffness`, `visualDampingRatio`, `visualImpulseScale` |

### ADSProfile _(struct, embedded on WeaponData)_

| Field | Purpose |
|---|---|
| `aimPositionOffset` | AimRig local translation delta when aiming |
| `aimRotationOffset` | AimRig local rotation delta (Euler) when aiming |
| `adsLerpSpeed` | 1/sec exponential decay rate (used for both aim-in and aim-out) |

### SprintPoseProfile _(struct, embedded on WeaponData)_

| Field | Purpose |
|---|---|
| `sprintPositionOffset` | SprintRig local translation delta while sprinting |
| `sprintRotationOffset` | SprintRig local rotation delta (Euler) |
| `bobPositionAmplitude` | Per-axis position amplitude for the cyclic sprint bob |
| `bobRotationAmplitude` | Per-axis rotation amplitude for the cyclic sprint bob |
| `bobFrequency` | Cycles per second |
| `lerpSpeed` | 1/sec exponential decay rate |

---

## Layer 2 — Firing

### IFirer / FireContext

```
interface IFirer { void Fire(in FireContext ctx); }
```

`FireContext` is a plain struct passed by `in` — zero allocation per shot. Fields:

| Field | Type | Content |
|---|---|---|
| `origin` | Vector3 | Muzzle world position |
| `direction` | Vector3 | Normalized aim direction |
| `stats` | WeaponStats | Resolved effective stats (carries all modifiers) |
| `ammo` | AmmoType | Currently loaded ammo |
| `owner` | Transform | Player transform (used to avoid self-hits in projectile mode) |
| `aiming` | bool | Whether ADS is active (selects spread value) |

### HitscanFirer

- Picks spread based on `ctx.aiming ? ctx.stats.adsSpread : ctx.stats.hipSpread`.
- Calls `Spread.Apply()` to jitter the direction.
- Fires a `Physics.Raycast` up to `ctx.stats.range`.
- Calls `IDamageable.TakeDamage()` if the hit collider implements the interface.
- Instantiates `impactEffect` at the hit point.

### ProjectileFirer

- Respects `ctx.ammo.pelletsPerShot` — loops and spawns one `Projectile` per pellet.
- Calls `Spread.Apply()` independently per pellet.
- Calls `projectile.Launch(dir * muzzleVelocity, damage, gravityMultiplier, owner)`.

### Spread.Apply

Static helper. Applies independent random pitch and yaw offsets within ±`spreadDegrees` to a direction vector. Returns a new normalized direction.

### IDamageable

```csharp
interface IDamageable {
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, Transform source);
}
```

Implement this on enemy, destructible, and player health components. Neither firer cares about the concrete type.

### Projectile _(Runtime, used by ProjectileFirer)_

A self-contained `MonoBehaviour` on a prefab with a `Rigidbody`. After `Launch()`:
- Sets velocity and gravity from ammo params.
- On `OnCollisionEnter`: calls `IDamageable.TakeDamage`, spawns `impactEffect`, then self-destructs.
- Ignores collisions with the owner and with other in-flight `Projectile` instances (prevents shotgun pellets from hitting each other).
- Self-destructs after `lifetime` seconds regardless of collision.

---

## Layer 3 — Runtime

### Stat Resolution — WeaponStats.Resolve

Called by `Weapon.RebuildStats()` on every equip, attachment change, or ammo swap. Returns a **plain struct** (`WeaponStats`) — zero allocation.

Resolution order:
1. Seed from `WeaponData` + selected `AmmoType` (damage comes from ammo, not weapon).
2. **Additive pass** — loop all attachments, add `*Add` fields.
3. **Multiplicative pass** — loop all attachments, multiply by `*Mult` fields.
4. `ScopeAttachment` with `overrideAimFOV = true` overrides `aimFOV` directly in the multiplicative pass.

`WeaponStats` fields mirror the data layer plus resolved references to `RecoilProfile`, `ADSProfile`, `SprintPoseProfile`, `fireRate`, `oneHanded`.

**Always read `Weapon.EffectiveStats` at runtime.** Do not reach into `WeaponData` directly from gameplay code.

### Weapon _(MonoBehaviour)_

The authoritative state and event source for one weapon instance.

**Public API:**

| Method / Property | Purpose |
|---|---|
| `Initialize(AmmoReserve)` | Called by `PlayerInventory` before `OnEnable` fires — sets up reserve ref, seeds mag, resolves stats |
| `TryFire(origin, direction)` | Checks cooldown, mag, reload state; builds `FireContext`; calls `firer.Fire()`; raises `OnFired` |
| `StartReload()` / (internal) `FinishReload()` | Manages reload timing via `Invoke`; pulls from `AmmoReserve` on finish |
| `StartAim()` / `StopAim()` | Toggles `IsAiming`; raises `OnAimIn` / `OnAimOut` |
| `TriggerInspect()` | Raises `OnInspect` |
| `AddAttachment(a)` / `RemoveAttachment(a)` | Slot-replaces or removes; calls `RebuildStats` + raises `OnAttachmentChanged` |
| `EffectiveStats` | Read-only property returning the current resolved `WeaponStats` |
| `IsReloading` | Read-only — true during reload window |
| `IsAiming` | Read-only — true while ADS is active |

**Events raised:**

| Event | Signature | When |
|---|---|---|
| `OnFired` | `Action` | After each successful shot |
| `OnReloadStart` | `Action` | When reload begins |
| `OnReloadFinish` | `Action` | When reload completes |
| `OnEquip` | `Action` | When the weapon's `GameObject` is enabled (after `Initialize`) |
| `OnHolster` | `Action` | When the weapon's `GameObject` is disabled |
| `OnAimIn` | `Action` | When ADS starts |
| `OnAimOut` | `Action` | When ADS ends |
| `OnInspect` | `Action` | When inspect is triggered |
| `OnAmmoChanged` | `Action<int, int>` | After any mag or reserve count change — args: (mag, reserve) |
| `OnAttachmentChanged` | `Action<AttachmentData>` | After an attachment is added or removed |

### PlayerInventory _(MonoBehaviour)_

Manages two weapon slots. Start-up sequence:
1. Disables all slot GameObjects.
2. Calls `weapon.Initialize(reserve)` on each slot while inactive.
3. Enables only the active slot — this fires `OnEnable` → `OnEquip` / `OnAmmoChanged` cleanly.

**API:**

| Method | Purpose |
|---|---|
| `Equip(int slot)` | Disables old active, enables new — raises `OnWeaponChanged(old, new)` |
| `Cycle(int direction)` | Wraps around to the next non-null slot |
| `PickUp(Weapon, int preferredSlot)` | Adds a picked-up weapon; replaces active slot if full |

**Event:** `OnWeaponChanged(Weapon oldActive, Weapon newActive)` — subscribed by `WeaponController` and can be subscribed by UI.

**`AmmoReserve`** is a separate `MonoBehaviour` on the same GameObject. It holds a `Dictionary<AmmoType, int>` tracking reserve counts per ammo type and exposes `GetCount`, `Add`, `TryConsume`, and `OnReserveChanged`.

### WeaponController _(MonoBehaviour)_

The **input bridge**. Enables the `Weapon` action map from `MovementSystem.inputactions`.

| Input action | Effect |
|---|---|
| `Shoot` (hold) | Calls `TryShootOnce()` every `Update` while held (fire rate gated inside `Weapon`) |
| `Reload` | Calls `inventory.Active.StartReload()` |
| `Aim` (hold) | `ApplyAimIn()` / `ApplyAimOut()` |
| `SwitchWeapon` (axis) | `inventory.Equip(0)` or `inventory.Equip(1)` |
| `Inspect` | `inventory.Active.TriggerInspect()` |

**ADS side-effects (ApplyAimIn / ApplyAimOut):**
- Sets `viewCamera.fieldOfView` target to `EffectiveStats.aimFOV` / restores `baseFOV`. FOV is lerped in `Update`.
- Writes `movement.maxSpeed = baseMoveSpeed * EffectiveStats.aimMoveSpeedMultiplier` on aim-in, restores on aim-out.
- `firingWhileSprintingAllowed` flag controls whether `TryShootOnce()` is blocked while sprinting.

### WeaponRecoil _(MonoBehaviour)_

Drives two recoil outputs each `Update`:

1. **Camera recoil** — writes `movement.recoilPitchOffset` / `recoilYawOffset`. Subscribes to `weapon.OnFired`. Each shot:
   - Adds `−vKick` to `currentPitchOffset` (negative because pitch is inverted in `Movement.Look()`).
   - Adds horizontal kick with direction sampled from `horizontalBias`.
   - Accumulates a `permanentPitch`/`permanentYaw` residue = kick × (1 − `recoveryFraction`).
   - After `recoveryDelay`, exponentially decays `currentPitchOffset` toward `permanentPitch`.
2. **Visual gun kick** — applies a spring (`WeaponHolder` local pose). Each shot adds an impulse to `visualPosVelocity` / `visualRotVelocity`. Semi-implicit Euler integration runs every `Update`.

Switching weapons resets both permanent residues and visual spring state so one weapon's recoil doesn't bleed into the next.

### WeaponViewModel _(MonoBehaviour)_

Thin animation and VFX bridge. Subscribes to all `Weapon` events in `OnEnable`, unsubscribes in `OnDisable`.

**Animator parameter name constants** (use these, never re-stringify):

| Constant | Type | Trigger condition |
|---|---|---|
| `ParamFire` | Trigger | `OnFired` |
| `ParamReload` | Trigger | `OnReloadStart` |
| `ParamEquip` | Trigger | `OnEquip` |
| `ParamHolster` | Trigger | `OnHolster` |
| `ParamInspect` | Trigger | `OnInspect` |
| `ParamAiming` | Bool | `OnAimIn` (true) / `OnAimOut` (false) |
| `ParamEmpty` | Bool | `OnAmmoChanged` — true when mag == 0 |
| `ParamFireRate` | Float | Set on `OnEnable` from `weapon.data.fireRate` |

Also plays `muzzleFlash` ParticleSystem on fire. `animatorOverride` on `WeaponData` is applied in `OnEnable`.

### WeaponADSPose _(MonoBehaviour)_

Owns `AimRig`'s local pose. Each `Update`, lerps toward `ADSProfile.aimPositionOffset` / `aimRotationOffset` when aiming, back to rest when not. Uses exponential decay: `t = 1 − exp(−adsLerpSpeed × dt)`.

### WeaponSprintPose _(MonoBehaviour)_

Owns `SprintRig`'s local pose. Active when `movement.isSprinting && !firingWhileSprintingAllowed && !IsAiming`. Applies a Lissajous-style cyclic bob (`sin(phase)` for Y/pitch, `sin(2×phase)` for X/Z/yaw) on top of `sprintPositionOffset`/`sprintRotationOffset`. Lerps in/out with exponential decay.

### WeaponSway _(MonoBehaviour)_

Owns the **viewmodel's own `localPosition.y`** only. Applies a walk-bob sine wave in `LateUpdate` when grounded, not crouching, and moving. When the player is aiming, `speedScale = EffectiveStats.aimMoveSpeedMultiplier` reduces bob frequency and amplitude to match the slowed move speed. Lerps toward `restLocalPos.y` when not bobbing.

---

## Transform Ownership (critical — never break these rules)

Each transform in the viewmodel rig has exactly one writer. Adding a second writer to the same transform causes fighting:

| Transform | Owner script | What it drives |
|---|---|---|
| `PlayerCam` rotation | `Movement.Look()` | Mouse input + recoil offsets |
| `AimRig` local pose | `WeaponADSPose` | ADS position/rotation lerp |
| `SprintRig` local pose | `WeaponSprintPose` | Sprint pose + cyclic bob |
| `WeaponHolder` local pose | `WeaponRecoil` | Visual gun kick (spring) |
| viewmodel `localPosition.y` | `WeaponSway` | Walk bob |
| `Camera.fieldOfView` | `WeaponController` | ADS FOV lerp |

The hierarchy is:
```
PlayerCam → AimRig → SprintRig → WeaponHolder → viewmodel
```

When adding a new pose effect, **insert a new intermediate rig transform** and own it exclusively, rather than sharing an existing one.

---

## Expanding the Weapon System

### Adding a new weapon
1. Create a `WeaponData` asset (`ShadowStrike/Weapons/Weapon Data`).
2. Create an `AmmoType` asset for its ammunition.
3. Set `firingMode`, `fireRate`, `magSize`, `reloadDuration`, spread, `recoilProfile`, `adsProfile`, `sprintPoseProfile`.
4. Assign `viewModelPrefab` (the prefab should have a `Weapon` component with a `WeaponViewModel` child, `WeaponHolder` transform, and an `Animator`).
5. Assign the asset to a `PlayerInventory` slot — no code changes needed.

### Adding a new firing mode
1. Add a value to the `FiringMode` enum in `WeaponData.cs`.
2. Create a new class implementing `IFirer` in the `Firing/` folder.
3. Add a `case` in `Weapon.CreateFirer()` returning an instance of the new class.
4. `Weapon.TryFire()` and `WeaponStats.Resolve()` require no changes.

### Adding a new attachment slot
1. Add a flag to `AttachmentSlot` in `AttachmentData.cs`.
2. Create a new `ScriptableObject` subclass of `AttachmentData` in `Data/`. Lock its `slot` in `OnValidate()`.
3. Add stat fields if the new slot needs modifiers beyond the base set.
4. Update `WeaponStats.Resolve()` to read the new fields if they don't fit the existing additive/multiplicative pattern.
5. Set `allowedAttachmentSlots` on the relevant `WeaponData` assets.

### Adding a new stat modifier (attachment modifier field)
1. Add the field pair (`*Add` / `*Mult`) to `AttachmentData`.
2. Add a corresponding field to `WeaponStats`.
3. Read the new `WeaponData` field in `WeaponStats.Resolve()` seed step.
4. Add the additive and multiplicative pass lines in `Resolve()`.
5. Read `weapon.EffectiveStats.newField` wherever the stat is consumed.

### Adding a new weapon event reaction (audio, UI, VFX)
1. Create a `MonoBehaviour` on the weapon prefab or the player.
2. In `Awake`/`OnEnable`, get a reference to `Weapon` (use `GetComponentInParent<Weapon>()`) and subscribe to the relevant event.
3. Unsubscribe in `OnDisable`.
4. Do **not** poll `Weapon` state in `Update`.

### Adding a new pose effect
1. Insert a new intermediate `Transform` in the rig hierarchy at the right level.
2. Create a new `MonoBehaviour` that owns only that transform's local pose.
3. Read `PlayerInventory.Active.EffectiveStats` for any per-weapon tuning data — add a new profile struct to `WeaponData` if needed.
4. Use the exponential decay lerp pattern: `t = 1 − Mathf.Exp(−speed × Time.deltaTime)`.
