# Project Structure

This document summarizes repository layout and layer boundaries for quick onboarding.

## High-Level Structure

```mermaid
flowchart TD
    Repo["Extensible Ability Framework"] --> Project["Assets/_Project"]
    Repo --> Docs["Docs"]
    Repo --> Scenes["Scenes"]
    Repo --> External["Art / Audio / External Content"]

    Project --> Scripts["Scripts"]
    Project --> Data["Data"]
    Project --> Settings["Settings"]

    Scripts --> Core["Core"]
    Scripts --> Feature["Feature"]
    Scripts --> Shared["Shared"]

    Feature --> Ability["AbilitySystem"]
    Feature --> Locomotion["Locomotion"]
    Feature --> Animation["Animation"]
```

## Script Layer Boundaries

```mermaid
flowchart LR
    Core["Core"] --> Shared["Shared Contracts / Services"]
    Ability["AbilitySystem"] --> Shared
    Locomotion["Locomotion"] --> Shared
    Animation["Animation"] --> Shared

    Ability -. event .-> Animation
    Ability -. lock service .-> Locomotion
```

## Folder Intent

- `Assets/_Project/_Scripts/Core`
  - Root composition, gameplay scope setup, and application skeleton.
- `Assets/_Project/_Scripts/Feature`
  - Feature-oriented gameplay implementations.
- `Assets/_Project/_Scripts/Shared`
  - Cross-feature contracts and reusable services.
- `Assets/_Project/Data`
  - Authoring assets and feature data.
- `Docs`
  - Technical notes, feature documentation, and validation guides.

## Current Feature Breakdown

### Ability System

- `Data`
- `Executors`
- `Mechanics`
- `Modules`
- `Runtime`
- `UI`
- `Editor`

### Locomotion

- `Data`
- `Runtime`
- `Input`

### Animation

- `Data`
- `Runtime`

## Notes

- `Shared` should only contain systems that are truly reused by two or more features.
- Features should not depend on each other's implementation details; communication should happen through events, interfaces, or shared service contracts.
