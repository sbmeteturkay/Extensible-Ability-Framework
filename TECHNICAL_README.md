# Extensible Ability Framework - Technical README

This document is the technical reference for the project. It complements the presentation-focused [README.md](README.md) and captures the implementation-level reasoning behind the current architecture.

## 1. Technical Scope Summary

- Dependency-injected feature architecture built with `VContainer` and `MessagePipe`
- Data-driven ability authoring through a single `AbilityDataSO` with embedded mechanic configs and optional modules
- Shared energy and cooldown flow with HUD presentation
- Dedicated executors for Dash, Projectile, and AOE mechanics
- Hit feedback pipeline including flash, scale, bounce, and hit VFX
- Pooled VFX spawning for projectile impacts, AOE casts, and hit feedback
- Player locomotion and animation integration

## 2. Tech Stack

- Unity: `6000.3.8f1`
- DI: `VContainer 1.17.0`
- Event Bus: `MessagePipe 1.8.1`
- Async: `UniTask 2.5.10`
- Tween: `PrimeTween 1.3.3`
- Input: `Input System 1.18.0`

## 3. Architectural Decisions and Rationale

### 3.1 Scope Topology

- `GamePlayLifetimeScope`: shared event broker registrations
- `PlayerLifetimeScope`: player-level shared services such as `ILocomotionLockService`
- `AbilityPlayerLifetimeScope`: player-side input gateway, ability domain services, and runtime bootstrap
- `AbilitySceneLifetimeScope`: scene-side HUD presentation
- `LocomotionLifetimeScope`: locomotion input, controller, and bootstrap
- `PlayerAnimationLifetimeScope`: animation driver

Rationale:
- The player prefab remains scene-independent while still communicating with scene-side UI and input through events.
- Features do not reference each other directly; shared communication is routed through MessagePipe.

### 3.1.1 Assembly Boundaries and Feature Independence

- Runtime code is separated into `Core`, `Feature/*`, and `Shared` assemblies.
- `Shared` hosts cross-feature contracts and event payloads (for example player switch contracts and ability presentation/domain events).
- `Feature` assemblies should depend on `Shared` abstractions for cross-feature collaboration rather than concrete types from other features.

Rationale:
- Boundaries are enforced at compile time, so dependency drift is caught early.
- Feature teams can iterate independently with lower regression risk.
- Assembly separation supports the case goal of extensibility with controlled coupling.

### 3.2 Data-Driven Ability Model

- Root asset: `AbilityDataSO`
- Required mechanic payload: `AbilityMechanicConfigSO`
- Execution strategy: `AbilityExecutorSO` derivatives
- Optional feature extensions: `AbilityOptionalModuleSO`
- Module hook pipeline: stage-based behavior injection without modifying the main controller flow
- Inspector-visible module execution-stage badges (Runs At) for faster authoring feedback

Rationale:
- The system follows the rule of `new content variant = new asset`.
- A genuinely new mechanic is isolated to a new executor or module class.
- The project avoids creating a dedicated `ScriptableObject` type for every individual skill.

### 3.2.1 Authoring Workflow Advantages

- Each ability keeps all authoring data inside a single `AbilityDataSO`.
- Mechanic config and optional modules are created as embedded sub-assets under the same asset.
- No additional `.asset` files are required for each config or module.

Rationale:
- The workflow stays fast because designers and developers work from a single inspector entry point.
- Asset sprawl is reduced, which makes the project easier to browse.
- Working around one main asset lowers the practical risk of noisy merge conflicts.

### 3.3 Slot and Identity Strategy

- Ability identity: `AbilityDataSO` asset GUID (`AbilityKey`)
- Slot identity: list index inside `AbilityLoadoutSO` (`SlotIndex`)
- Runtime mapping: `slotIndex -> ability`

Rationale:
- There is no manual enum or hardcoded slot ID maintenance.
- Loadout, HUD, and input all share the same ordering, which keeps mapping straightforward.

### 3.4 VFX and Performance Policy

- Ability-side VFX spawning goes through `IPooledVfxService`.
- If the pooling service is unavailable, spawning is skipped and a warning is logged instead of hard-failing.

Rationale:
- Pooling is treated as a core requirement rather than a later optimization.
- This reduces allocation spikes and keeps runtime behavior safer on mobile targets.

### 3.5 Hit Timing Semantics

- AOE abilities expose `HitDelaySeconds`.
- The actual hit and the hit visual are triggered from the same impact point in time.
- Additional visual offset remains available through `HitVisualDelaySeconds`.

Rationale:
- The system keeps the semantic idea of `when the hit happens` separate from `when the feedback should appear`, while still keeping both easy to align.

## 4. Design Patterns in Use

- `Strategy Pattern`
  - `AbilityExecutorSO` determines the concrete runtime behavior of an ability.
- `Template / Hook Pattern`
  - `AbilityOptionalModuleSO` attaches logic to before/after execution stages.
- `Factory Pattern`
  - `AbilityFactory` centralizes runtime `IAbility` creation.
- `Observer / Pub-Sub`
  - MessagePipe events provide loose coupling between gameplay, HUD, animation, and input.
- `Object Pool`
  - Projectile and VFX instances are reused through shared pooling services.

## 5. Requirement Coverage Matrix

| Requirement | Solution | Status |
|---|---|---|
| Three different ability behaviors | Dash / Projectile / AOE executors | Complete |
| Separation of data and logic | ScriptableObject data + C# runtime/executors | Complete |
| Pooling for repeated objects | Projectile and VFX pooling | Complete |
| Error tolerance | Validation, guarded bootstrap flow, warning-based fallback | Complete |
| HUD feedback | Slot icons, cooldown visuals, shared energy slider | Complete |
| Extensible ability authoring | MechanicConfig + executor + optional module model | Complete |
| Low coupling between features | MessagePipe event contracts + scope separation | Complete |

## 6. Setup and Run

1. Open the project in Unity Hub with version `6000.3.8f1`.
2. Load `Assets/_Project/Scenes/Gameplay.unity`.
3. Verify the expected parent-child relationships between the scene and player scopes.
4. Ensure feature scopes under the player prefab are active.
5. Enter Play Mode and validate input, HUD, ability, locomotion, and hit feedback flow.

## 7. Manual Test Scenarios

- Slot trigger
  - Does the correct slot respond to keyboard or on-screen button input?
- Cooldown
  - Does cooldown UI update correctly after ability usage?
- Energy
  - Does the single energy slider represent the shared energy state for all abilities?
- AOE hit
  - Does the target react after `HitDelaySeconds`?
  - Does hit feedback appear at the expected moment?
- Projectile
  - Are spawn offset, direction, and collision mask behaving as expected?
  - Are impact VFX and hit VFX reused under the pool root?
- Locomotion lock
  - Is movement blocked while an ability with locomotion lock is active?
- Animation
  - Are movement parameters stable?
  - Does the animator receive ability trigger and slot index data correctly?

Quick validation checklist:
- [Docs/SmokeChecklist.md](Docs/SmokeChecklist.md)

## 8. Trade-Offs and Intentional Simplifications

- Automated test coverage is currently limited; manual validation was prioritized for the case scope.
- Runtime tuning remains intentionally lightweight through the module hook model; a richer module chain could be added later.
- Energy regeneration is currently fixed at `10/s` and was intentionally kept out of a separate config asset for faster iteration.
- Single-asset authoring was favored for speed and usability, with the understanding that deeper editor tooling can be added in a later phase if needed.

## 9. Additional Technical References

- [Docs/Features/AbilitySystem.md](Docs/Features/AbilitySystem.md)
- [Docs/Features/Locomotion.md](Docs/Features/Locomotion.md)
- [Docs/Features/Animation.md](Docs/Features/Animation.md)
- [Docs/AbilityFeatureScope.md](Docs/AbilityFeatureScope.md)
- [Docs/SmokeChecklist.md](Docs/SmokeChecklist.md)

## 10. Possible Next Steps

- Editor tooling
  - Ability authoring validator window
  - Runtime debug panel for energy, cooldown, and locomotion lock state
- Testing
  - Edit Mode tests for ability validation and service logic
  - Play Mode smoke tests for the main executor flow
- Module system
  - A more general and chainable override pipeline

## 11. Dash Physics Rationale

- Dash is implemented through Rigidbody-based movement using `MovePosition`.
- A transform-only teleport-style implementation was intentionally avoided so dash behavior remains consistent with the existing collider and rigidbody setup.
- In collision mode, obstacle detection is shape-cast based (`CapsuleCast`, `SphereCast`, `BoxCast`). This proved more reliable than a simple `SweepTest`, especially against smaller obstacles.
- The dash supports two behavior modes:
- `Block`: dash stops when a valid obstacle is detected on the path.
- `PhaseIfLandingValid`: dash may pass through intermediate obstacles, but only if the landing position is physically valid. If landing is invalid, the system falls back to block mode.
- Which layers can block dash movement is resolved from `AbilityDataSO.TargetGroups`, so dash only reacts to layers intentionally defined in the ability data.
- For performance, collider information is resolved and cached once at execution start; the dash loop itself does not perform repeated `GetComponent` or `GetComponentsInChildren` calls.
- This approach addresses the case expectation of explaining the physical reasoning behind dash implementation while also supporting a controlled phase-through option when design requires it.

## 12. Current Technical Notes (Mar 2026)

- Ability event contracts were split into `Domain` and `Presentation`.
- The pool root API was renamed to `GetPoolsRoot()`, while `GetAbilityPoolsRoot()` remains as an obsolete compatibility wrapper.
- Dash collision now uses shape casts and supports both block/phase modes with target-group-driven collision masks.
- Ability inspector sections are foldable and include validation support.


