# Ability System Feature Dokumani

## 1. Amac

Ability feature'i, input -> trigger -> validation -> execute -> feedback zincirini data-driven sekilde yonetir.
Sistem hem oyuncu runtime'i hem de scene HUD tarafi icin ayrik scope'lara bolunmustur.

## 2. Scope ve Bagimlilik Haritasi

- `GamePlayLifetimeScope`
  - Ability event broker'larini register eder.
- `PlayerLifetimeScope`
  - `ILocomotionLockService` register eder.
- `AbilityPlayerLifetimeScope`
  - `AbilityRuntimeBootstrap`
  - `AbilityController`, `AbilityFactory`
  - `CooldownService`, `EnergyService`
  - `IPooledVfxService`
- `AbilitySceneLifetimeScope`
  - `AbilityInputGateway`
  - `AbilityHudPresenter`

Iletisim kurali:
- Player ve Scene tarafi birbirini direkt tanimaz.
- Ortak kanal `CaseStudy.Shared.AbilitySystem.Events` eventleridir.

## 3. Ana Siniflar ve Sorumluluklar

### 3.1 Runtime ve Orkestrasyon

- `AbilityRuntimeBootstrap`
  - Loadout data'sini validate eder.
  - `AbilityContext` olusturur.
  - Controller konfigurasyonunu yapar.
  - HUD tarafi icin `AbilityLoadoutSlotAssignedEvent` publish eder.

- `AbilityController`
  - Trigger request dinler.
  - Cooldown ve enerji kontrollerini yapar.
  - Override pipeline calistirir.
  - Executor-based ability calistirir.
  - Basari/basarisizlik eventleri publish eder.

- `AbilityFactory`
  - `AbilityDataSO + Executor` icin runtime `ExecutorAbility` olusturur.

### 3.2 Data Katmani

- `AbilityDataSO`
  - Common alanlar: cooldown, energy, target group, movement policy, executor
  - `List<AbilityModuleSO>`
  - `List<AbilityOverrideSO>`
  - `AbilityKey` asset GUID ile otomatik atanir.

- `AbilityModuleSO` turevleri
  - `DashAbilityModuleSO`
  - `ProjectileAbilityModuleSO`
  - `AoeAbilityModuleSO`
  - `HitVisualProfileSO`

- `AbilityOverrideSO` turevleri
  - `AbilityExecutionValueOverrideSO`

- Slot ve hedefleme datasi
  - `SlotDefinitionSO`
  - `AbilityLoadoutSO`
  - `AbilityTargetingProfileSO`

### 3.3 Executor Katmani

- `DashExecutorSO`
  - Rigidbody `MovePosition` ile sureli dash.

- `ProjectileExecutorSO`
  - Projectile pool'dan runtime alir.
  - Spawn offset + aim ray ile launch direction hesaplar.

- `AoeExecutorSO`
  - `HitDelaySeconds` bekler.
  - `OverlapSphereNonAlloc` ile hedef toplar.
  - Hit visual ve hit vfx uygular.

### 3.4 Scene ve UI

- `AbilityInputGateway`
  - InputAction -> `AbilityTriggerRequestedEvent` publish.

- `AbilityHudPresenter`
  - Event subscribe ederek HUD state'ini gunceller.
  - Slot click'ten trigger event publish eder.

- `AbilityHudView` / `AbilityHudSlotWidget`
  - Energy slider, icon, cooldown fill/text render eder.

## 4. Event Akisi

1. `AbilityInputGateway` veya HUD click `AbilityTriggerRequestedEvent` publish eder.
2. `AbilityController` trigger'i alir.
3. Validation sirasi:
   - slot resolve
   - data resolve
   - override before-trigger
   - cooldown check
   - energy check
4. Ability execute edilir.
5. Cooldown baslatilir.
6. `AbilityTriggeredEvent` ve gerekirse failure/diagnostic eventleri yayinlanir.
7. HUD presenter cooldown + energy eventleriyle UI'yi gunceller.

## 5. Hit Visual ve VFX Semantigi

- `AoeAbilityModuleSO.HitDelaySeconds`
  - AOE hit isleminin ne zaman olacagini belirler.
- `HitVisualProfileSO.HitVisualDelaySeconds`
  - Flash/scale/bounce baslangic gecikmesi.
- `HitVisualProfileSO.HitVfxDelaySeconds`
  - Hit VFX spawn gecikmesi.
- `ProjectileAbilityModuleSO.ImpactVfxDelaySeconds`
  - Projectile impact VFX gecikmesi.

VFX politikasi:
- Ability tarafinda VFX spawnlari pool zorunlu.
- Pool servisi yoksa instantiate yerine warning + skip uygulanir.

## 6. Yeni Ability Ekleme Rehberi

### 6.1 Kod Yazmadan Yeni Icerik Varyanti

1. Yeni `AbilityDataSO` asset olustur.
2. Uygun executor asset referansini ata.
3. Gerekli module asset'lerini ekle.
4. Slot assignment icin `AbilityLoadoutSO`'da ilgili slota bagla.
5. Icon ve cooldown/energy degerlerini ayarla.

Bu akista mevcut C# dosyasi degistirilmez.

### 6.2 Yeni Mekanik Ekleme (Kodlu)

1. `AbilityModuleSO` turevi olustur (mekanik parametreleri).
2. `AbilityExecutorSO` turevi olustur (runtime davranis).
3. `TryValidate` ve `CanExecute` kurallarini ekle.
4. `AbilityDataSO` asset'inde yeni executor + module ile test et.

Not:
- `AbilityFactory` degistirmen gerekmez.
- `ExecutorAbility` catisi executor tabanli calistigi icin sistem acik kalir.

## 7. Hata Toleransi Notlari

- Bootstrap seviyesinde null/config guard'lari vardir.
- `AbilityDataSO.TryValidateConfiguration` zorunlu kontrolleri calistirir.
- Duplicate slot key durumunda son atama kazanir, warning log atilir.

## 8. Performans Notlari

- `OverlapSphereNonAlloc` kullanilir.
- Pooling projectile ve VFX tarafinda aktiftir.
- Slot key normalize islemi tek utility'de tutulur (`AbilitySlotKeyUtility`).
- HUD ve input akisi event-driven oldugu icin gereksiz polling azaltilir.

## 9. Bilinen Sinirlar

- Otomatik test coverage henuz sinirli.
- Override sistemi temel seviyede; daha genel composable override zinciri ileri faza acik.
