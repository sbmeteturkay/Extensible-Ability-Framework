# Animation Feature

## 1. Purpose

The Animation feature pushes stable movement data into animator parameters and applies ability-driven trigger and playback speed updates received through events.

## 2. Scope Boundary

Responsibilities of this feature:
- Computing movement parameters and writing them into the animator
- Consuming ability trigger, slot index, and playback speed signals
- Applying runtime clip override mapping

Out of scope:
- Ability execution decisions
- Locomotion movement calculations

## 3. Runtime Components

- `PlayerAnimationDriver`
- `PlayerAnimationConfigSO`

Config scope:
- Parameter names
- Threshold, hold, and damping settings
- Ability-related animator parameter configuration
- Slot source clip list

## 4. Runtime Flow

1. `FixedUpdate`
- Velocity is derived from position delta.
- Local movement components are normalized.
- `isMoving` is decided through threshold and hold logic.

2. `Update`
- Animator float and bool parameters are written.
- Pending ability signals are consumed.

3. Event flow
- `AbilityTriggeredEvent` (domain)
- `AbilityLoadoutSlotAssignedEvent` (presentation)

## 5. Integration Points

- Locomotion
  - Motion data is derived from position delta rather than reading directly from the locomotion component
- Ability
  - `AbilityTriggeredEvent` can provide trigger, slot index, and playback speed
- Loadout
  - `AbilityLoadoutSlotAssignedEvent` updates slot-to-clip mapping

## 6. Stability Decisions

- Animator parameter hashes are cached.
- Float writes are filtered with epsilon checks.
- Damping can be enabled or disabled through config.
- If setup is incomplete, the driver falls back to a safe no-op behavior.

## 7. Manual Test Checklist

- Correct flow for `MoveX`, `MoveY`, `Speed`, and `IsMoving`
- Ability trigger and slot index reaching the animator
- Ability playback speed updating at cast time
- Correct slot clip override mapping

## 8. Trade-Offs

- Runtime velocity-based animation flow was preferred over root motion.
- More advanced layer-based blend and state callback integrations were intentionally left lightweight.
