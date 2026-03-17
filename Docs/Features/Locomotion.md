# Locomotion Feature

## 1. Purpose

The Locomotion feature manages player movement and turning in a way that stays aligned with the physics pipeline.
It can block movement by reading the current lock state from a shared locomotion lock service.

## 2. Scope Boundary

Responsibilities of this feature:
- Reading and caching move input
- Applying Rigidbody-based movement and rotation
- Handling dead zone filtering and movement lock checks

Out of scope:
- Ability behavior
- Animator state decisions

Dependency rule:
- Locomotion depends on shared lock contracts (`ILocomotionLockService`) for cross-feature interaction.
- It should not require direct runtime references to ability execution internals.

## 3. Runtime Components

- `LocomotionInputGateway` (`ILocomotionInputReader`)
- `LocomotionRuntimeBootstrap`
- `LocomotionController` (`ILocomotionController`, `IFixedTickable`)
- `LocomotionDataSO`

Dependency:
- `LocomotionController` -> `ILocomotionLockService` (shared)

## 4. Runtime Flow

1. The gateway caches the latest input vector.
2. The controller reads input during `FixedTick`.
3. It exits early when input stays below the dead zone.
4. It exits early when locomotion is locked.
5. The movement direction is normalized.
6. `MovePosition` and `MoveRotation` are applied.

## 5. Data Contract

`LocomotionDataSO` fields:
- `MoveSpeed`
- `RotationSpeedDegreesPerSecond`
- `InputDeadZone`

## 6. Integration Points

- Input
  - `LocomotionInputGateway` is responsible only for reading and caching input
- Ability
  - Movement lock is consumed through `ILocomotionLockService`
- Animation
  - The animation driver derives motion from position delta instead of pulling runtime values directly from locomotion

## 7. Performance and Safety

- No per-tick allocations are expected inside `FixedTick`.
- Runtime references are established during bootstrap.
- If required config or references are missing, bootstrap falls back to a safe disabled state.

## 8. Manual Test Checklist

- Dead zone behavior
- Keyboard and joystick input consistency
- Movement stopping correctly while lock is active
- Stable turning speed
- Rigidbody interaction with walls and collision

## 9. Trade-Offs

- Movement is currently planar only; there is no Y-axis locomotion layer.
- Sprint, acceleration, and air-control layers were intentionally left for a later phase.
