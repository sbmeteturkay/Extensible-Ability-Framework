# Extensible Ability Framework

Extensible Ability Framework is a feature-oriented Unity sample project built around modular gameplay systems.
At its core is a data-driven ability pipeline, while locomotion, animation, HUD, and feedback flows remain separated behind the same architectural rules.

This repository is intended to demonstrate extensibility, clean responsibility boundaries, and a production-minded authoring workflow.
The README focuses on the project-level architectural decisions and design patterns that shaped the implementation.

## Case Focus: Extensible Ability System

This case is centered on building an extensible ability framework, not a content-complete game loop.
The primary value is the ability pipeline and its authoring model:

- One root ability asset (`AbilityDataSO`) for each skill
- Executor-driven mechanic strategy (`AbilityExecutorSO`)
- Mechanic-specific required payload (`AbilityMechanicConfigSO`)
- Optional, composable behavior extensions (`AbilityOptionalModuleSO`)
- Event-driven runtime flow to HUD and animation without direct feature coupling

## Overview

Core focus areas of the project:

- Data-driven ability authoring
- Feature-based folder and scope separation
- Dependency-injected runtime composition
- Event-driven communication between features
- Mobile-conscious pooling and allocation control
- Minimal runtime code changes when adding new ability variants

## Highlights

- Single-entry ability authoring through `AbilityDataSO`
- Dedicated executors for Dash, Projectile, and AOE mechanics
- Shared energy and cooldown flow with HUD integration
- Hit feedback pipeline: flash, scale, bounce, and hit VFX
- Pool-based spawning for projectiles and visual effects
- Player locomotion lock and animation integration
- Foldable inspector workflow with built-in validation
- Optional module rows show Runs At execution-stage badges in the inspector

## Architecture Snapshot

Instead of concentrating gameplay logic inside a few large scripts, the project separates responsibilities by feature:

- `Ability System`
  - Triggering, validation, cooldown, energy, execution, and feedback
- `Locomotion`
  - Rigidbody-based movement, turning, dead-zone handling, and lock control
- `Animation`
  - Stable transfer of motion data into animator parameters
- `Shared`
  - Pooling, event contracts, and common services

Runtime composition is handled with `VContainer`, while cross-feature communication is routed through `MessagePipe` events.

### Assembly Boundary Rule

- Runtime code is split into separate assemblies (`Core`, `Feature/*`, `Shared`) to enforce compile-time boundaries.
- Features are expected to communicate through `Shared` contracts and events instead of direct concrete references across feature assemblies.
- This keeps feature evolution isolated and makes accidental cross-feature coupling visible early.

## Why This Structure

The goal was not only to build a working demo, but to establish a foundation that can grow without turning into a tightly coupled gameplay script cluster.

In practice this means:

- A new ability variant should usually be introduced by creating a new asset.
- A new mechanic should require a new `executor`, and only when necessary a new module.
- HUD, animation, and runtime execution should not depend on each other directly; they communicate through explicit contracts.

This keeps both the authoring workflow and long-term maintenance more predictable.

## Architectural Decisions

- `Feature-based structure`
  - Decision: Gameplay code is organized by feature boundaries (`Ability`, `Locomotion`, `Animation`, `PlayerControl`).
  - Why: We needed independent iteration without editing unrelated systems.

- `Assembly-level separation`
  - Decision: Features are compiled as separate assemblies, with cross-feature contracts in `Shared`.
  - Why: We wanted compile-time enforcement of boundaries, not convention-only discipline.

- `Dependency Injection (VContainer)`
  - Decision: Runtime composition is done via scopes/installers instead of scene lookups or service locator.
  - Why: We needed explicit ownership and predictable dependency graphs for player-prefab and scene services.

- `Event-driven communication (MessagePipe)`
  - Decision: HUD, animation, input, and execution communicate through published events.
  - Why: We needed loose coupling between runtime systems that react to shared state.

- `Data-driven ability authoring`
  - Decision: Ability behavior is authored through `AbilityDataSO + Executor + MechanicConfig + OptionalModules`.
  - Why: Most new skills should be content additions, not runtime code changes.

- `Object pooling as baseline`
  - Decision: Projectile/VFX spawning uses pool services by default.
  - Why: Mobile constraints require stable frame-time and low allocation churn.

- `Single-asset ability authoring`
  - Decision: Mechanic config and optional modules are embedded under one root `AbilityDataSO`.
  - Why: We wanted faster authoring and easier review of ability-level changes.
    
### Trade-Offs

- `Assembly boundaries`
  - Extra assembly reference management is required during refactors.
- `Event-driven flow`
  - Runtime debugging follows event chains rather than direct call stacks.
- `PlayerControlState orchestration`
  - `PlayerControlState` currently coordinates multiple feature-facing components in one place for deterministic character switching.
  - This is intentional for case scope, but a deeper interface abstraction can further reduce orchestration-level coupling later.

## Design Patterns

- `Strategy`: each ability's core behavior is defined by the selected `AbilityExecutorSO`, allowing Dash, Projectile, and AOE mechanics to share the same runtime pipeline.
- `Factory`: `AbilityFactory` centralizes runtime `IAbility` creation so instantiation rules do not leak into loadout or bootstrap code.
- `Observer / Pub-Sub`: cooldown, energy, trigger, and loadout updates are broadcast through events, allowing UI, animation, and gameplay systems to stay synchronized without direct references.
- `Template / Hook`: the optional module model injects behavior into `before/after execute` stages, making it possible to extend execution with SFX, VFX, or policy logic without rewriting the main flow.
- `Object Pool`: projectile and VFX instances are reused through shared pooling infrastructure to keep runtime allocation pressure low.

## Creating a New Ability

One of the main goals of the project is to make new ability creation predictable.
The intended workflow is:

1. Create a new `AbilityDataSO`.
2. Select an `AbilityExecutorSO` based on the mechanic type.
3. Create the required `AbilityMechanicConfigSO` sub-asset for that executor.
4. Add optional modules only for non-essential extensions such as feedback or auxiliary policies.
5. Add the ability to the loadout in the desired slot order.
6. Assign that `AbilityLoadoutSO` to the player-side `AbilityRuntimeBootstrap`.
7. Let the existing runtime pipeline handle triggering, validation, execution, cooldown, energy, HUD, and animation events.

This separation is intentional:

- `Executor` defines the core mechanic.
- `MechanicConfig` stores the executor-specific required data.
- `Optional Modules` add behavior that should remain optional and composable.

As a rule of thumb:

- `New content variant`: create a new `AbilityDataSO`
- `New mechanic`: create a new executor and mechanic config
- `New optional behavior`: create a new module

<table>
  <tr>
    <td width="55%" valign="top">
      
## Ability Authoring Model
The same `AbilityDataSO` remains the root authoring asset for every ability.
What changes from ability to ability is the selected executor, the required mechanic config for that executor, and the set of optional modules attached to the asset.

This is the key extensibility rule in the project:

- `AbilityDataSO` stays stable as the common entry point.
- `AbilityExecutorSO` can change when the mechanic changes.
- `AbilityMechanicConfigSO` changes with the executor because it stores required mechanic-specific data.
- `AbilityOptionalModuleSO` instances stay reusable and composable across multiple abilities.
  
### Reusable Module Library
- Modules are reusable across abilities.
- Executors define core mechanics.
- Modules run at `BeforeTrigger`, `BeforeExecute`, `AfterExecute`.

    </td>
    <td width="100%" valign="middle">
      <img width="720" height="1080" alt="image" src="https://github.com/user-attachments/assets/775d8073-57d6-42e6-9818-25ea378474a6" />
    </td>
    
  </tr>
</table>

```mermaid
flowchart TD
    A["AbilityDataSO<br/>Common ability asset"] --> B["AbilityExecutorSO<br/>Core mechanic strategy"]
    A --> C["AbilityMechanicConfigSO<br/>Required mechanic-specific data"]
    A --> D["AbilityOptionalModuleSO[]<br/>Reusable optional extensions"]

    B --> B1["DashExecutorSO"]
    B --> B2["ProjectileExecutorSO"]
    B --> B3["AoeExecutorSO"]

    C --> C1["Dash config"]
    C --> C2["Projectile config"]
    C --> C3["AOE config"]

    D --> D1["SFX / VFX"]
    D --> D2["Animation"]
    D --> D3["Policy / feedback"]

    A --> E["AbilityLoadoutSO"]
    E --> F["AbilityRuntimeBootstrap"]
    F --> G["Shared runtime pipeline"]
```

## Current Feature Set

### Ability System

- `AbilityDataSO`-centered authoring model
- `Executor + MechanicConfig + OptionalModule` separation
- Loadout-to-HUD mapping based on slot order
- Shared energy and cooldown state
- Domain and presentation event separation

Detailed documentation:
- [Ability System Docs](Docs/Features/AbilitySystem.md)

### Locomotion

- Rigidbody-based movement
- Cached input and fixed-tick execution
- Ability-driven movement lock integration

Detailed documentation:
- [Locomotion Docs](Docs/Features/Locomotion.md)

### Animation

- Stable movement parameter updates
- Animator integration for ability trigger, slot index, and playback speed
- Clip override flow based on loadout slots

Detailed documentation:
- [Animation Docs](Docs/Features/Animation.md)

## Technical Documentation

In addition to this presentation-oriented README, the repository includes more detailed technical references:

- [Technical README](TECHNICAL_README.md)
- [Ability Feature Scope](Docs/AbilityFeatureScope.md)
- [Ability System Docs](Docs/Features/AbilitySystem.md)
- [Locomotion Docs](Docs/Features/Locomotion.md)
- [Animation Docs](Docs/Features/Animation.md)
- [Smoke Checklist](Docs/SmokeChecklist.md)

## Project Structure

Top-level structure:

- `Assets/_Project/_Scripts/Core`
  - Root composition and shared setup
- `Assets/_Project/_Scripts/Feature`
  - Gameplay features such as Ability, Locomotion, and Animation
- `Assets/_Project/_Scripts/Shared`
  - Infrastructure used by more than one feature
- `Assets/_Project/Data`
  - Authoring assets

This structure keeps feature code and shared infrastructure separate, making the repository easier to navigate and extend.

```mermaid
flowchart TD
    Repo["Extensible Ability Framework"] --> Project["Assets/_Project"]
    Repo --> Docs["Docs"]
    Repo --> Scenes["Scenes"]

    Project --> Scripts["Scripts"]
    Project --> Data["Data"]

    Scripts --> Core["Core"]
    Scripts --> Feature["Feature"]
    Scripts --> Shared["Shared"]

    Feature --> Ability["AbilitySystem"]
    Feature --> Locomotion["Locomotion"]
    Feature --> Animation["Animation"]

    Ability --> AbilityNodes["Data / Executors / Modules / Runtime / UI"]
    Locomotion --> LocomotionNodes["Data / Runtime / Input"]
    Animation --> AnimationNodes["Data / Runtime"]
    Shared --> SharedNodes["Events / Interfaces / Services / Extensions"]
```

For a more detailed structure diagram:
- [Project Structure Docs](Docs/ProjectStructure.md)

## Demo Video and APK

https://github.com/user-attachments/assets/af30aaaf-b516-4e12-8170-1c09117d0fe1

Android APK: [Download APK](https://drive.google.com/file/d/1y63BeGfWiu8UeYxBStyccs3LB3xyfPip/view?usp=sharing)

## Running The Project

1. Open the project in Unity Hub with version `6000.3.8f1`.
2. Load `Assets/_Project/Scenes/Gameplay.unity`.
3. Enter Play Mode and test the ability, HUD, locomotion, and animation flow.

## Controls

- Move: `W / A / S / D`
- Ability Slots: `1 / 2 / 3`
- Switch Character: `Left Shift`
- Mobile: On-screen ability buttons + virtual joystick

## Notes

- This repository is a technical gameplay framework sample rather than a content-complete game.
- More detailed rationale behind the implementation is documented in `TECHNICAL_README.md`.
- Validation has been primarily manual; fast verification steps are listed in `Docs/SmokeChecklist.md`.
