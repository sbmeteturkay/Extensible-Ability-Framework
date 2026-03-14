# Ability Feature Scope Guide

This document defines the runtime scope boundaries and authoring contract for the Ability feature.

## 1. Scope Topology

GamePlayLifetimeScope (parent)
- AbilitySceneLifetimeScope (child): input + HUD presentation
- AbilityPlayerLifetimeScope (child): runtime ability logic and services

Both child scopes communicate through MessagePipe brokers registered in the parent gameplay scope.

## 2. Scope Responsibilities

### GamePlayLifetimeScope
Owns shared event brokers used across features.

Registered ability-related brokers:
- AbilityTriggerRequestedEvent
- AbilityTriggeredEvent
- AbilityExecutionFailedEvent
- AbilityExecutionDiagnosticEvent
- AbilityCooldownStartedEvent
- AbilityCooldownUpdatedEvent
- AbilityCooldownCompletedEvent
- AbilityEnergyChangedEvent
- AbilityLoadoutSlotAssignedEvent

### AbilitySceneLifetimeScope
Scene/UI side only.

Responsibilities:
- Capture input intent
- Publish trigger request events
- Present HUD state from events

Typical components:
- AbilityInputGateway
- AbilityHudPresenter

### AbilityPlayerLifetimeScope
Player runtime side only.

Responsibilities:
- Build and validate ability runtime from loadout
- Execute abilities
- Manage cooldown and energy
- Apply overrides
- Publish execution events

Main bindings:
- IAbilityFactory -> AbilityFactory
- IAbilityController -> AbilityController
- ICooldownService -> CooldownService
- IEnergyService -> EnergyService
- IPooledVfxService -> PooledVfxService
- AbilityRuntimeBootstrap (component in hierarchy)

## 3. Ability Authoring Contract

Each ability asset must be AbilityDataSO and must define:
- Executor (required)
- Modules (required by executor, one module per module type)
- Optional Overrides
- Common fields: cooldown, energy, movement policy, targeting, icon

Validation guardrails (editor + runtime):
- Executor must exist
- No negative cooldown/energy/min lock values
- No null module entries
- No duplicate module type in module list
- No null override entries
- Executor-specific validation must pass

## 4. Execution Flow

1. Scene input publishes AbilityTriggerRequestedEvent(slotKey)
2. AbilityController resolves slot and data
3. Pre-trigger overrides run (ordered by override order)
4. Cooldown check
5. CanExecute check
6. Energy consume
7. Ability execute via executor
8. Cooldown start + triggered event publish
9. Post-execute overrides run

Failure path:
- AbilityExecutionFailedEvent is always published
- AbilityExecutionDiagnosticEvent is also published with detailed context

## 5. Diagnostic Telemetry

AbilityExecutionDiagnosticEvent fields:
- SlotKey
- AbilityKey
- Reason
- Stage
- Source
- Message

Typical Stage values:
- ResolveSlot
- ResolveData
- BeforeTriggerOverrides
- CooldownCheck
- CanExecute
- EnergyCheck
- ExecuteAsync

Use this event for logs, in-game debug panels, or analytics adapters.

## 6. How To Add a New Ability

For a new content variant (same mechanic):
1. Create or reuse an executor asset
2. Create AbilityDataSO asset
3. Create module asset(s) required by executor
4. Assign executor + module(s) on AbilityDataSO
5. Add asset to loadout slot
6. Test trigger, cooldown, energy, and telemetry

For a new mechanic type:
1. Create new AbilityExecutorSO implementation
2. Define new module SO type(s)
3. Implement executor TryValidate + ExecuteAsync
4. Create data/module assets and bind to loadout

## 7. Design Boundaries

- Executor: core mechanic runtime logic
- Module: mechanic-specific data payload
- Override: runtime modification layer (cross-cutting behavior)

Rule of thumb:
- If value is static per ability, keep it in AbilityDataSO/module
- If value changes by runtime condition, use override
- If behavior is fundamentally different, add new executor

## 8. Trade-offs

Current trade-off:
- Strong validation and explicit layering reduce bad config risk
- Authoring has more assets (data + module + optional overrides)

This is intentional for long-term scalability and safer iteration.
