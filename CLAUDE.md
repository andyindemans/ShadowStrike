# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ShadowStrike is a first-person parkour shooter built in Unity **6000.4.4f1** (Unity 6). C# scripts are configured for `-langversion:latest` via [Assets/csc.rsp](Assets/csc.rsp). Input uses Unity's new Input System package; bindings live in [Assets/Scripts/MovementSystem/MovementSystem.inputactions](Assets/Scripts/MovementSystem/MovementSystem.inputactions) (a single generated `MovementSystem` C# class exposes both the `Movement` and `Weapon` action maps).

There is no build/lint/test tooling — open the project in the Unity Editor and use Play mode. The two scenes live in [Assets/Scenes/](Assets/Scenes/) (`SampleScene` and `DevRoom`).

## Architecture

Code is split into two top-level subsystems under [Assets/Scripts/](Assets/Scripts/):

### MovementSystem
[Movement.cs](Assets/Scripts/MovementSystem/Movement.cs) is the player controller — it owns the Rigidbody, ground/wall detection, sprint/crouch/slide/wallrun state, and camera `Look()`. Two public hooks are read by other systems:
- `Movement.recoilPitchOffset` / `recoilYawOffset` — additive Euler offsets applied on top of mouse input inside `Look()`. **WeaponRecoil writes these every frame**; recovery returns toward player intent without losing input. Pitch is inverted (looking up = more negative `xRotation`).
- `Movement.isSprinting`, `Movement.grounded`, `Movement.crouching` — read by weapon viewmodel scripts to sync with locomotion state.

[MoveCamera.cs](Assets/Scripts/MovementSystem/MoveCamera.cs), [Parkour.cs](Assets/Scripts/MovementSystem/Parkour.cs), and [SpeedFX.cs](Assets/Scripts/MovementSystem/SpeedFX.cs) are auxiliary.

### WeaponSystem
Three-layer separation under [Assets/Scripts/WeaponSystem/](Assets/Scripts/WeaponSystem/):

- **Data/** — `ScriptableObject` configs ([WeaponData](Assets/Scripts/WeaponSystem/Data/WeaponData.cs), [AmmoType](Assets/Scripts/WeaponSystem/Data/AmmoType.cs), [AttachmentData](Assets/Scripts/WeaponSystem/Data/AttachmentData.cs) + `Barrel`/`Muzzle`/`Scope` subclasses, [RecoilProfile](Assets/Scripts/WeaponSystem/Data/RecoilProfile.cs), [ADSProfile](Assets/Scripts/WeaponSystem/Data/ADSProfile.cs), [SprintPoseProfile](Assets/Scripts/WeaponSystem/Data/SprintPoseProfile.cs)). Authored assets live in [Assets/WeaponAssets/](Assets/WeaponAssets/). The `CreateAssetMenu` path is `ShadowStrike/Weapons/...`.
- **Firing/** — strategy pattern. [IFirer](Assets/Scripts/WeaponSystem/Firing/IFirer.cs) takes a [FireContext](Assets/Scripts/WeaponSystem/Firing/FireContext.cs) struct; [HitscanFirer](Assets/Scripts/WeaponSystem/Firing/HitscanFirer.cs) and [ProjectileFirer](Assets/Scripts/WeaponSystem/Firing/ProjectileFirer.cs) are the implementations, picked from `WeaponData.firingMode`. Damage flows through [IDamageable](Assets/Scripts/WeaponSystem/Firing/IDamageable.cs). [Spread.Apply](Assets/Scripts/WeaponSystem/Firing/Spread.cs) randomizes direction by degrees.
- **Runtime/** — `MonoBehaviour` components composed on prefabs.

#### Stat resolution
At weapon init / attachment change / ammo swap, [Weapon.RebuildStats](Assets/Scripts/WeaponSystem/Runtime/Weapon.cs) calls [WeaponStats.Resolve](Assets/Scripts/WeaponSystem/Runtime/WeaponStats.cs), which:
1. Seeds from `WeaponData` + selected `AmmoType`.
2. Applies attachments **additively first, multiplicatively second** (predictable scaling).
3. Returns a plain struct (zero-alloc per shot — `FireContext` carries it by value).

Anything that reads weapon stats at runtime (recoil, spread, ADS pose, FOV, move-speed multiplier) reads `Weapon.EffectiveStats`, **not** `WeaponData` directly.

#### Event bus on Weapon
[Weapon.cs](Assets/Scripts/WeaponSystem/Runtime/Weapon.cs) exposes events (`OnFired`, `OnReloadStart/Finish`, `OnEquip/Holster`, `OnAimIn/Out`, `OnInspect`, `OnAmmoChanged`, `OnAttachmentChanged`). Everything else (recoil, viewmodel/animator bridge, UI) subscribes — there is no central tick loop coordinating them. When adding a new visual/audio/UI reaction, subscribe to events; do not poll `Weapon` state.

[PlayerInventory](Assets/Scripts/WeaponSystem/Runtime/PlayerInventory.cs) holds two slots, calls `Initialize` while inactive (so `OnEnable` fires `OnEquip`/`OnAmmoChanged` cleanly on the active weapon), and raises `OnWeaponChanged(old, new)`.

[WeaponController](Assets/Scripts/WeaponSystem/Runtime/WeaponController.cs) is the input bridge. It also drives camera FOV lerp and applies `aimMoveSpeedMultiplier` to `Movement.maxSpeed` (caching/restoring base speed) on aim in/out.

#### Transform ownership (important)
The viewmodel rig has a chain of intermediate transforms, and **each transform has exactly one writer**. Do not have multiple scripts modify the same transform's local pose:

| Transform                | Owner                                                                                                                  |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------- |
| `PlayerCam` rotation     | [Movement.Look()](Assets/Scripts/MovementSystem/Movement.cs) (mouse + recoil offsets)                                  |
| `AimRig` local pose      | [WeaponADSPose](Assets/Scripts/WeaponSystem/Runtime/WeaponADSPose.cs) (lerps to `ADSProfile` while aiming)              |
| `SprintRig` local pose   | [WeaponSprintPose](Assets/Scripts/WeaponSystem/Runtime/WeaponSprintPose.cs) (lerps to `SprintPoseProfile` while sprinting & not allowed to fire) |
| `MotionRig` local pose   | [WeaponMotion](Assets/Scripts/WeaponSystem/Runtime/WeaponMotion.cs) (procedural walk bob, idle breathing, look-delay sway)  |
| `WeaponHolder` local pose| [WeaponRecoil](Assets/Scripts/WeaponSystem/Runtime/WeaponRecoil.cs) (visual gun kick offsets)                          |
| `Camera.fieldOfView`     | [WeaponController](Assets/Scripts/WeaponSystem/Runtime/WeaponController.cs) (ADS FOV lerp)                              |

The hierarchy is `PlayerCam → AimRig → SprintRig → MotionRig → WeaponHolder → viewmodel` so the effects compose. When adding a new pose driver, insert a new rig transform with its own owner rather than sharing.

[WeaponViewModel](Assets/Scripts/WeaponSystem/Runtime/WeaponViewModel.cs) is the animation/FX bridge — it subscribes to `Weapon` events and fires animator triggers/bools. Animator parameter names are constants on that class (`ParamFire`, `ParamReload`, etc.); reuse them rather than re-stringifying.

## Conventions

- Field grouping uses banner comments like `//Refs #=======================#` / `//Runtime #====================#` / `//Config #=====================#`. Keep this style when adding fields.
- `MonoBehaviour` references default to `GetComponentInParent<...>()` in `Awake` so prefabs work without manually wiring every slot — public fields are a fallback, not required wiring.
- Keep `WeaponStats.Resolve` allocation-free: it returns a struct and is called on every attachment change, but `FireContext` carries the resolved struct so per-shot firing is also alloc-free.
- Recoil/sway profile fields use `Mathf.Exp(-speed * dt)` framerate-independent lerp — match that pattern for new smoothed pose drivers rather than naive `Lerp(a, b, dt * speed)`.

## Out of scope (Unity-managed)

`Library/`, `Temp/`, `Logs/`, `UserSettings/`, and `*.csproj`/`*.sln*` are Unity-regenerated and gitignored — never edit them. The `.slnx` file ([ShadowStrike.slnx](ShadowStrike.slnx)) is the only solution file tracked.
