# Extensible Ability Framework - Unity Case Study

Bu repo, Unit Game Developer case icin gelistirilen modul bazli bir gameplay altyapisini icerir.
Odak nokta Ability System olsa da, Locomotion ve Animation feature'lari da ayni mimari prensiplerle ayrik sekilde kurgulanmistir.

## 1. Case Kapsami Ozeti

- DI tabanli feature mimarisi (VContainer, MessagePipe)
- Data-driven ability authoring (tek bir `AbilityDataSO` catisi + module/executor/override)
- Paylasilan enerji/cooldown sistemi ve HUD sunumu
- Dash, Projectile ve AOE executor'lari
- Hit visual sistemi (flash, scale, bounce, hit vfx)
- VFX pooling (projectile impact, aoe cast, hit vfx)
- Player locomotion ve animator entegrasyonu

## 2. Tech Stack

- Unity: `6000.3.8f1`
- DI: `VContainer 1.17.0`
- Event Bus: `MessagePipe 1.8.1`
- Async: `UniTask 2.5.10`
- Tween: `PrimeTween 1.3.3`
- Input: `Input System 1.18.0`

## 3. Mimari Kararlar ve Gerekceleri

### 3.1 Scope Topolojisi

- `GamePlayLifetimeScope`: ortak event broker kayitlari
- `PlayerLifetimeScope`: oyuncu seviyesindeki ortak servisler (`ILocomotionLockService`)
- `AbilityPlayerLifetimeScope`: ability domain servisleri ve runtime bootstrap
- `AbilitySceneLifetimeScope`: scene tarafi input + HUD presenter
- `LocomotionLifetimeScope`: locomotion input/controller/bootstrap
- `PlayerAnimationLifetimeScope`: animation driver

Gerekce:
- Player prefab'i sahneden bagimsiz kalirken, scene UI/input ile event tabanli iletisim korunur.
- Feature'lar birbirini dogrudan referanslamaz; ortak kanal MessagePipe uzerinden kurulur.

### 3.2 Data-Driven Ability Modeli

- Tek roof data: `AbilityDataSO`
- Mekanik parametreleri: `AbilityModuleSO` turevleri
- Calistirma mantigi: `AbilityExecutorSO` turevleri
- Davranis degisimi / kural enjeksiyonu: `AbilityOverrideSO` turevleri

Gerekce:
- "Yeni icerik varyanti = yeni asset" hedefi saglanir.
- "Yeni mekanik = yeni executor/module class" sinirli ve kontrollu kalir.
- Yuzlerce skill senaryosunda her skill icin yeni `AbilityDataSO` sinifi yazma ihtiyaci kalkar.

### 3.3 Slot ve Kimlik Stratejisi

- Ability kimligi: `AbilityDataSO` asset GUID (`AbilityKey`)
- Slot kimligi: `SlotDefinitionSO` asset GUID (`SlotKey`)
- Runtime mapping: `slotKey -> ability`

Gerekce:
- Enum veya manuel id bakimi yok.
- Refactor/rename sonrasinda id stabilitesi korunur.

### 3.4 VFX ve Performans Karari

- Ability tarafi VFX spawn'lari `IPooledVfxService` uzerinden yapilir.
- Pool servisi yoksa instantiate fallback yerine skip + warning uygulanir.

Gerekce:
- Case'teki object pooling beklentisi net karsilanir.
- Mobilde GC spike riski azaltilir.

### 3.5 Hit Zamani Semantigi

- AOE icin `HitDelaySeconds` tanimlidir.
- Hit ile birlikte hit visual tetiklenir.
- Hit visual'in kendi offset'i icin `HitVisualDelaySeconds` vardir.

Gerekce:
- "Gercek vurus zamani" ile "visual feedback" hizli ama kontrol edilebilir sekilde ayristirilir.

## 4. Kullanilan Pattern'ler

- Strategy Pattern:
  - `AbilityExecutorSO` secimi ile yetenek calisma davranisi degisir.
- Template/Hook Pattern:
  - `AbilityOverrideSO` oncesi/sonrasi hook noktalarina baglanir.
- Factory Pattern:
  - `AbilityFactory`, runtime `IAbility` uretimini tek noktada toplar.
- Observer (Pub/Sub):
  - MessagePipe event akisiyla feature'lar arasi gevek baglanti kurulur.
- Object Pool:
  - `ProjectilePool` ve `PooledVfxService` tekrarli nesnelerde kullanilir.

## 5. Gereksinim Karsilama Matrisi

| Case beklentisi | Cozum | Durum |
|---|---|---|
| 3 farkli ability davranisi | Dash / Projectile / AOE executor'lari | Tamam |
| Data ve logic ayrimi | SO data + C# runtime/executor | Tamam |
| Pooling kullanimi | Projectile ve ability VFX pooling | Tamam |
| Error tolerance | Bootstrap ve validate kontrolleri + warning | Tamam |
| HUD geri bildirimi | Slot icon, cooldown, enerji slider | Tamam |
| Yeni ability eklenebilirligi | Module/executor/override modeli | Tamam |
| Featurelar arasi bagimsizlik | MessagePipe eventleri + scope ayrimi | Tamam |

## 6. Kurulum ve Calistirma

1. Unity Hub ile proje acilir (`6000.3.8f1`).
2. `Assets/_Project/Scenes/Gameplay.unity` sahnesi acilir.
3. Scene hiyerarsisinde scope parent baglantilari kontrol edilir.
4. Player prefab altinda ilgili feature scope'lari aktif oldugundan emin olunur.
5. Play mode'da input, HUD, ability ve hit testleri dogrulanir.

## 7. Test Senaryolari (Manual)

- Slot trigger:
  - Klavye/onscreen buton ile dogru slot tetikleniyor mu?
- Cooldown:
  - Ability kullanimindan sonra cooldown fill/text dogru guncelleniyor mu?
- Energy:
  - Tek slider tum skill'ler icin ortak enerji state'ini gosteriyor mu?
- AOE hit:
  - `HitDelaySeconds` sonrasi hedef etkileniyor mu?
  - Hit visual (flash/scale/bounce) beklenen anda calisiyor mu?
- Projectile:
  - Spawn offset, yon ve collision mask beklendigi gibi mi?
  - Impact ve hit vfx pool altinda reuse ediliyor mu?
- Locomotion lock:
  - Lock policy acik ability calisirken locomotion bloke oluyor mu?
- Animation:
  - Move parametreleri stabil mi?
  - `AbilityTriggeredEvent` ile ability trigger/indeks animatora gidiyor mu?

## 8. Trade-off ve Bilincli Tercihler

- Otomatik test coverage su an sinirli; case odagi nedeniyle manuel dogrulama agirlikli gidildi.
- `AbilityExecutionValueOverrideSO` temel bir override seti sunar; tam "volume profile" benzeri override stack ileri faza birakildi.
- Energy regen su an sabit (`10/s`) ve config asset'e alinmadi; hizli case iterasyonu icin bilincli sade tutuldu.

## 9. Dokusmanlar

- `Docs/Features/AbilitySystem.md`
- `Docs/Features/Locomotion.md`
- `Docs/Features/Animation.md`

## 10. Gelecek Faz Onerileri

- Editor tooling:
  - Ability authoring validator window
  - Runtime debug panel (energy, cooldown, lock state)
- Test:
  - Ability validator ve service katmani icin edit mode testleri
  - Core executor akisi icin play mode smoke testleri
- Override sistemi:
  - Daha genel ve zincirlenebilir override pipeline
