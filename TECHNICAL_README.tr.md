# Extensible Ability Framework - Technical README

Bu dokuman, projenin teknik referans kaynagidir. GitHub ana sayfasindaki sunum odakli `README.md` dosyasinin tamamlayicisidir.

## 1. Teknik Kapsam Ozeti

- DI tabanli feature mimarisi (VContainer, MessagePipe)
- Data-driven ability authoring (tek bir `AbilityDataSO` + embedded MechanicConfig/OptionalModules + executor)
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
- Mekanik parametreleri: `AbilityMechanicConfigSO` (executor'a zorunlu payload)
- Calistirma mantigi: `AbilityExecutorSO` turevleri
- Opsiyonel eklentiler: `AbilityOptionalModuleSO`
- Module hook pipeline: yeni module eklerken `AbilityController` degistirmeden asama-bazli davranis ekleme

Gerekce:
- "Yeni icerik varyanti = yeni asset" hedefi saglanir.
- "Yeni mekanik = yeni executor/module class" sinirli ve kontrollu kalir.
- Yuzlerce skill senaryosunda her skill icin yeni `AbilityDataSO` sinifi yazma ihtiyaci kalkar.

### 3.2.1 Authoring Avantaji

- Bir ability'nin tum authoring verisi tek `AbilityDataSO` dosyasinda tutulur.
- `MechanicConfig` ve `OptionalModules` ayni asset icinde embedded subasset olarak uretilir.
- Ayrica her config/module icin ayri `.asset` dosyasi olusturma ihtiyaci yoktur.

Gerekce:
- Icerik uretim akisi hizlanir; tasarimci/gelistirici tek inspector uzerinden ilerler.
- Dosya daginigi azaldigi icin projede gezinmek kolaylasir.
- Tek asset etrafinda calisildigindan git merge/cakisma riski pratikte azalir.

### 3.3 Slot ve Kimlik Stratejisi

- Ability kimligi: `AbilityDataSO` asset GUID (`AbilityKey`)
- Slot kimligi: `AbilityLoadoutSO` icindeki liste index'i (`SlotIndex`)
- Runtime mapping: `slotIndex -> ability`

Gerekce:
- Enum veya manuel slot id bakimi yok.
- Loadout, HUD ve input tarafi ayni liste sirasini paylasir; esleme sade kalir.

### 3.4 VFX ve Performans Karari

- Ability tarafi VFX spawn'lari `IPooledVfxService` uzerinden yapilir.
- Pool servisi yoksa spawn atlanir ve warning loglanir.

Gerekce:
- Object pooling beklentisi net karsilanir.
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
  - `AbilityOptionalModuleSO` before/after hook noktalarina baglanir.
- Factory Pattern:
  - `AbilityFactory`, runtime `IAbility` uretimini tek noktada toplar.
- Observer (Pub/Sub):
  - MessagePipe event akisiyla feature'lar arasi gevek baglanti kurulur.
- Object Pool:
  - `ProjectilePool` ve `PooledVfxService` tekrarli nesnelerde kullanilir.

## 5. Gereksinim Karsilama Matrisi

| Beklenti | Cozum | Durum |
|---|---|---|
| 3 farkli ability davranisi | Dash / Projectile / AOE executor'lari | Tamam |
| Data ve logic ayrimi | SO data + C# runtime/executor | Tamam |
| Pooling kullanimi | Projectile ve ability VFX pooling | Tamam |
| Error tolerance | Bootstrap ve validate kontrolleri + warning | Tamam |
| HUD geri bildirimi | Slot icon, cooldown, enerji slider | Tamam |
| Yeni ability eklenebilirligi | MechanicConfig/executor/module hook modeli | Tamam |
| Feature'lar arasi bagimsizlik | MessagePipe eventleri + scope ayrimi | Tamam |

## 6. Kurulum ve Calistirma

1. Unity Hub ile proje acilir (`6000.3.8f1`).
2. `Assets/_Project/Scenes/Gameplay.unity` sahnesi acilir.
3. Scene hiyerarsisinde scope parent baglantilari kontrol edilir.
4. Player prefab altinda ilgili feature scope'lari aktif oldugundan emin olunur.
5. Play mode'da input, HUD, ability ve hit testleri dogrulanir.

## 7. Manuel Test Senaryolari

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
  - `AbilityTriggeredEvent` ile ability trigger/slot index animatora gidiyor mu?

Ayrica hizli kontrol listesi:
- [Docs/SmokeChecklist.md](Docs/SmokeChecklist.md)

## 8. Trade-off ve Bilincli Tercihler

- Otomatik test coverage su an sinirli; case odagi nedeniyle manuel dogrulama agirlikli gidildi.
- Runtime davranis tuningleri module hook modeliyle verilir; daha zengin module zinciri ileri faza aciktir.
- Energy regen su an sabit (`10/s`) ve config asset'e alinmadi; hizli iterasyon icin bilincli sade tutuldu.
- Tek-asset authoring, kullanim rahatligi ve ekip hizi icin secildi; daha ince editor tooling ihtiyaci olursa ileri fazda genisletilebilir.

## 9. Teknik Dokumantasyon

- [Docs/Features/AbilitySystem.md](Docs/Features/AbilitySystem.md)
- [Docs/Features/Locomotion.md](Docs/Features/Locomotion.md)
- [Docs/Features/Animation.md](Docs/Features/Animation.md)
- [Docs/AbilityFeatureScope.md](Docs/AbilityFeatureScope.md)
- [Docs/LowTouchBacklog.md](Docs/LowTouchBacklog.md)
- [Docs/SmokeChecklist.md](Docs/SmokeChecklist.md)

## 10. Gelecek Faz Onerileri

- Editor tooling:
  - Ability authoring validator window
  - Runtime debug panel (energy, cooldown, lock state)
- Test:
  - Ability validator ve service katmani icin edit mode testleri
  - Core executor akisi icin play mode smoke testleri
- Module sistemi:
  - Daha genel ve zincirlenebilir override pipeline

## 11. Dash Fizik Tercihi

- Dash uygulamasi Rigidbody tabanli (`MovePosition`) secildi.
- Transform tabanli anlik pozisyon degisimi yerine physics uyumlu hareket tercih edildi; bu sayede dash davranisi mevcut collider/rigidbody yapisi ile ayni dilde calisir.
- Collision modunda engel kontrolu shape-cast tabanlidir (`CapsuleCast` / `SphereCast` / `BoxCast`). Bu tercih, kucuk obstacle'larda tekil `SweepTest`e gore daha tutarli bloklama verir.
- Dash icin iki davranis modu desteklenir:
- `Block`: hedef yolunda uygun layer'da bir engel varsa dash o noktada kesilir.
- `PhaseIfLandingValid`: yol uzerindeki engellerden gecmeye izin verilir, ancak hedef pozisyon collider acisindan gecerliyse. Hedef pozisyon uygun degilse sistem otomatik olarak block moduna duser.
- Hangi katmanlarin dash'i bloklayacagi `AbilityDataSO.TargetGroups` uzerinden cozulur; yani dash tum sahneyi degil, ability verisinde tanimlanan katmanlari dikkate alir.
- Performans acisindan collider resolve islemi execute basinda bir kez cache'lenir; dash dongusu icinde `GetComponent` / `GetComponentsInChildren` tekrar edilmez.
- Bu yapi, case'teki "fiziksel gerekceyi aciklama" beklentisini iki hedefle karsilar: ongorulebilir bloklama davranisi ve tasarim gerektiginde kontrollu faz-gecis opsiyonu.

## 12. Guncel Teknik Notlar (Mar 2026)

- Ability event yapisi `Domain` ve `Presentation` olarak ayrildi.
- Pool root API'si `GetPoolsRoot()` olarak notr hale getirildi (`GetAbilityPoolsRoot()` obsolete wrapper olarak korunuyor).
- Dash collision davranisi shape-cast tabanli calisiyor; block/phase modu ve target-group maskesi destekleniyor.
- Ability inspector bolumleri foldable hale getirildi.
