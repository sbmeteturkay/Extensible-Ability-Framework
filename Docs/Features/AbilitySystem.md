# Ability System Feature

## 1. Amac

Ability feature, `input -> validate -> execute -> feedback` zincirini data-driven sekilde yonetir.
Ana hedef, yeni ability icerigini mevcut runtime kodunu degistirmeden asset uzerinden uretebilmektir.

## 2. Scope Siniri

Feature'in sorumlulugu:
- Ability tetikleme ve yurutme orkestrasyonu
- Cooldown ve enerji yonetimi
- Executor + module pipeline
- HUD ve animation'a event ile veri aktarma

Feature disinda kalanlar:
- Temel player hareket sistemi (Locomotion feature)
- Animator state machine tasarimi (Animation feature)

## 3. Runtime Mimarisi

Temel runtime siniflari:
- `AbilityRuntimeBootstrap`
- `AbilityController`
- `AbilityFactory`
- `CooldownService`
- `EnergyService`

Scope dagilimi:
- `AbilitySceneLifetimeScope`: input ve HUD tarafini baglar
- `AbilityPlayerLifetimeScope`: loadout, runtime servisleri ve execution tarafini barindirir

Calisma akisi:
1. Input/HUD `AbilityTriggerRequestedEvent` publish eder.
2. `AbilityController` slottan `AbilityDataSO` cozer.
3. Validation + before-trigger module hook'lari calisir.
4. Cooldown/energy/execution gate kontrolleri calisir.
5. Executor `ExecuteAsync` calisir.
6. Cooldown/energy/trigger eventleri publish edilir.
7. After-execute module hook'lari calisir.

## 4. Data Kontrati

Tek ana asset: `AbilityDataSO`
- Common: cooldown, energy, target groups, movement policy, icon
- Zorunlu: `AbilityExecutorSO`, `AbilityMechanicConfigSO`
- Opsiyonel: `List<AbilityOptionalModuleSO>`
- Kimlik: `AbilityKey` (asset GUID, otomatik)

Authoring ilkesi:
- Yeni icerik varyanti: yeni `AbilityDataSO`
- Yeni mekanik turu: yeni `AbilityExecutorSO` + yeni `AbilityMechanicConfigSO`
- Yeni opsiyonel davranis/feedback: yeni `AbilityOptionalModuleSO`

Mevcut executorlar:
- `DashExecutorSO`
- `ProjectileExecutorSO`
- `AoeExecutorSO`

## 5. Event Kontrati

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

## 6. Authoring Akisi

Kod yazmadan yeni ability varyanti:
1. `AbilityDataSO` olustur.
2. Executor referansi ata.
3. Executor'un istedigi `MechanicConfig` subasset'ini olustur.
4. Gerekli optional module'leri ekle.
5. `AbilityLoadoutSO` listesine ekle.

Yeni mekanik turu:
1. `AbilityMechanicConfigSO` turevi yaz.
2. `AbilityExecutorSO` turevi yaz.
3. `TryValidate` ve `ExecuteAsync` uygula.

Yeni opsiyonel davranis:
1. `AbilityOptionalModuleSO` turevi yaz.
2. Gerekli hook asamalarini uygula.
3. Ability asset'ine module olarak ekle.

## 7. Son Guncellemeler

- Event yapisi Domain/Presentation olarak ayrildi.
- Dash executor sweep tabanli collision check kullaniyor.
- `DashMechanicConfigSO` icinde `EnableDebugTelemetry` mevcut.
- `AbilityDataSOEditor` foldable bolumler + validation panel ile calisiyor.

## 8. Manuel Test Checklist

- Slot tetikleme (input + HUD)
- Cooldown baslatma/update/bitis
- Ortak enerji slider davranisi
- Dash engelde kesilme
- Projectile spawn/impact pooling
- AOE hit delay + hit visual
- Animator trigger/index/speed aktarimi

## 9. Entegrasyon Noktalari

- Input:
  - `AbilityTriggerRequestedEvent`
- HUD:
  - cooldown/energy ve slot assignment event akisi
- Locomotion:
  - `ILocomotionLockService`
- Animation:
  - `AbilityTriggeredEvent`
- Shared:
  - MessagePipe event broker
  - pooled VFX servisi

## 10. Trade-off

- Otomatik test kapsami sinirli; manuel smoke agirlikli.
- Module pipeline sade tutuldu; daha zengin chain sonraki faz.
- Energy regen sabit (`10/s`) ve configlesmedirilmadi.
