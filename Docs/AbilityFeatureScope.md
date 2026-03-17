# Ability Feature Scope Guide

This document defines runtime scope boundaries and the authoring contract for the Ability feature.

## 1. Scope Topology

GamePlayLifetimeScope (parent)
- AbilitySceneLifetimeScope (child): HUD presentation
- PlayerLifetimeScope (child): player-level shared services
  - AbilityPlayerLifetimeScope (grandchild): input gateway + runtime ability logic and services

Ability scene and player runtime communicate through MessagePipe brokers registered in the gameplay parent scope.

## 2. Scope Responsibilities

### GamePlayLifetimeScope
Owns shared event brokers used across features.

Registered ability-related brokers:
- Domain events:
  - AbilityTriggerRequestedEvent
  - AbilityTriggeredEvent
  - AbilityExecutionFailedEvent
  - AbilityExecutionDiagnosticEvent
  - AbilityCooldownStartedEvent
  - AbilityCooldownUpdatedEvent
  - AbilityCooldownCompletedEvent
  - AbilityEnergyChangedEvent
- Presentation events:
  - AbilityLoadoutSlotAssignedEvent

Event namespaces:
- CaseStudy.Shared.AbilitySystem.Events.Domain
- CaseStudy.Shared.AbilitySystem.Events.Presentation

### AbilitySceneLifetimeScope
Scene/UI side only.

Responsibilities:
- Present HUD state from events

Typical components:
- AbilityHudPresenter

### AbilityPlayerLifetimeScope
Player runtime side only.

Responsibilities:
- Capture input intent and publish trigger requests
- Build and validate ability runtime from loadout
- Execute abilities
- Manage cooldown and energy
- Run optional module hooks
- Publish execution events

Main bindings:
- IAbilityFactory -> AbilityFactory
- IAbilityController -> AbilityController
- ICooldownService -> CooldownService
- IEnergyService -> EnergyService
- IPooledVfxService -> PooledVfxService
- IAbilityInputGate -> AbilityInputGateway
- AbilityRuntimeBootstrap (component in hierarchy)

## 3. Ability Authoring Contract

Each ability asset must be AbilityDataSO and must define (single-asset authoring via embedded subassets):
- Executor (required)
- MechanicConfig (required by executor)
- OptionalModules
- Common fields: cooldown, energy, movement policy, targeting, icon

Validation guardrails (editor + runtime):
- Executor must exist
- No negative cooldown/energy/min lock values
- No null optional module entries
- No duplicate optional module type in module list
- Executor-specific validation must pass

## 4. Execution Flow

1. Player input gateway publishes AbilityTriggerRequestedEvent(slotIndex)
2. AbilityController resolves slot and data
3. Pre-trigger module hooks run (ordered by module order)
4. Cooldown check
5. Execution gate check
6. CanExecute check
7. Energy consume
8. Before-execute module hooks run
9. Ability execute via executor
10. Cooldown start + triggered event publish
11. After-execute module hooks run

Failure path:
- AbilityExecutionFailedEvent is always published
- AbilityExecutionDiagnosticEvent is also published with detailed context

## 5. Diagnostic Telemetry

AbilityExecutionDiagnosticEvent fields:
- SlotIndex
- AbilityKey
- Reason
- Stage
- Source
- Message

Typical Stage values:
- ResolveSlot
- ResolveData
- BeforeTriggerModules
- CooldownCheck
- ExecutionGate
- CanExecute
- EnergyCheck
- ExecuteAsync

## 6. Design Boundaries

- Executor: core mechanic runtime logic
- MechanicConfig: executor-specific required payload
- OptionalModule: optional, composable, cross-cutting feature addon

Rule of thumb:
- Static per-ability values: AbilityDataSO / MechanicConfig / OptionalModule
- Fundamentally different mechanic behavior: new executor

## 7. Trade-offs

- Explicit layering reduces bad-config risk
- Authoring speed is high, but custom inspector/tooling dependency is higher
