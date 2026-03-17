# Ability System Feature

## 1. Purpose

The Ability feature manages the `input -> validate -> execute -> feedback` chain in a data-driven way.
Its main goal is to allow new ability content to be authored through assets without requiring changes to the existing runtime flow.

## 2. Scope Boundary

Responsibilities of this feature:
- Ability triggering and execution orchestration
- Cooldown and energy management
- Executor and optional module pipeline
- Event-based data flow to HUD and animation

Out of scope:
- Core player movement system (Locomotion feature)
- Animator state machine design (Animation feature)

## 3. Runtime Architecture

Core runtime classes:
- `AbilityRuntimeBootstrap`
- `AbilityController`
- `AbilityFactory`
- `CooldownService`
- `EnergyService`

Scope split:
- `AbilitySceneLifetimeScope`: wires scene-side HUD presentation
- `AbilityPlayerLifetimeScope`: owns input gateway, loadout, runtime services, and execution flow

Runtime flow:
1. `AbilityInputGateway` publishes `AbilityTriggerRequestedEvent`.
2. `AbilityController` resolves the target `AbilityDataSO` from the slot.
3. Validation and before-trigger module hooks are executed.
4. Cooldown, energy, and execution gates are evaluated.
5. The selected executor runs `ExecuteAsync`.
6. Cooldown, energy, and trigger events are published.
7. After-execute module hooks are executed.

## 4. Data Contract

Single root asset: `AbilityDataSO`
- Common fields: cooldown, energy cost, target groups, movement policy, icon
- Required references: `AbilityExecutorSO`, `AbilityMechanicConfigSO`
- Optional extensions: `List<AbilityOptionalModuleSO>`
- Identity: `AbilityKey` (auto-generated from the asset GUID)

Authoring rules:
- New content variant: create a new `AbilityDataSO`
- New mechanic type: create a new `AbilityExecutorSO` and `AbilityMechanicConfigSO`
- New optional behavior or feedback: create a new `AbilityOptionalModuleSO`

Current executors:
- `DashExecutorSO`
- `ProjectileExecutorSO`
- `AoeExecutorSO`

## 5. Event Contract

Domain events (`CaseStudy.Shared.AbilitySystem.Events.Domain`):
- `AbilityTriggerRequestedEvent`
- `AbilityTriggeredEvent`
- `AbilityExecutionFailedEvent`
- `AbilityExecutionDiagnosticEvent`
- `AbilityCooldownStartedEvent`
- `AbilityCooldownUpdatedEvent`
- `AbilityCooldownCompletedEvent`
- `AbilityEnergyChangedEvent`

Presentation events (`CaseStudy.Shared.AbilitySystem.Events.Presentation`):
- `AbilityLoadoutSlotAssignedEvent`

## 6. Authoring Workflow

Adding a new ability variant without writing code:
1. Create an `AbilityDataSO`.
2. Assign the executor reference.
3. Create the mechanic config sub-asset required by that executor.
4. Add any optional modules.
5. Add the ability to `AbilityLoadoutSO`.

Adding a new mechanic type:
1. Create a new `AbilityMechanicConfigSO` derivative.
2. Create a new `AbilityExecutorSO` derivative.
3. Implement `TryValidate` and `ExecuteAsync`.

Adding a new optional behavior:
1. Create a new `AbilityOptionalModuleSO` derivative.
2. Implement the required hook stages.
3. Add the module to the ability asset.

## 7. Current Implementation Notes

- Event contracts are split into Domain and Presentation layers.
- Dash uses shape-cast-based collision checks.
- `DashMechanicConfigSO` includes `EnableDebugTelemetry`.
- `AbilityDataSOEditor` uses foldable sections and a validation panel.
- Optional module rows show execution-stage badges in the inspector (`Runs At`).
- The interaction demo is intentionally kept simple with a single receiver (`DummyAbilityTarget`).
- Visual intent is decided by the ability layer through `AbilityVisualCommand`; the receiver only applies it.

## 8. Manual Test Checklist

- Slot triggering from input and HUD
- Cooldown start, update, and completion flow
- Shared energy slider behavior
- Dash stopping correctly against obstacles
- Projectile spawn and impact pooling
- AOE hit delay and hit feedback timing
- Animator trigger, slot index, and playback speed updates

## 9. Integration Points

- Input
  - `AbilityTriggerRequestedEvent`
- HUD
  - Cooldown, energy, and slot-assignment event flow
- Locomotion
  - `ILocomotionLockService`
- Animation
  - `AbilityTriggeredEvent`
- Shared
  - MessagePipe event broker
  - Pooled VFX service

## 10. Trade-Offs

- Automated test coverage is limited; validation is primarily manual and smoke-oriented.
- Energy regeneration is fixed at `10/s` and is not yet externalized into a dedicated config asset.
