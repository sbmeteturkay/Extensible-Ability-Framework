# Animation Feature Dokumani

## 1. Amac

Animation feature'i, player hareket verisini animator parametrelerine stabil sekilde yazar ve ability tetiklerini animasyon katmanina aktarir.

## 2. Scope ve Bilesenler

- `PlayerAnimationLifetimeScope`
  - `PlayerAnimationDriver` component'ini hierarchy'den register eder.

- Data
  - `PlayerAnimationConfigSO`
    - Parametre adlari
    - Esik ve hold degerleri
    - Damping ayarlari
    - Ability trigger/index parametreleri

## 3. Ana Sinif: PlayerAnimationDriver

Sorumluluklar:
- `FixedUpdate`:
  - Pozisyon delta'sindan dunya hizi hesaplar.
  - Local velocity'ye cevirir.
  - `isMoving` kararini threshold + hold penceresiyle verir.
- `Update`:
  - Animator float/bool parametrelerini yazar.
  - Ability sinyallerini consume eder.
- Event entegrasyonu:
  - `AbilityTriggeredEvent` subscribe eder.
  - Opsiyonel `AbilityUsed` trigger + `AbilityIndex` parametresi set eder.

## 4. Stabilite Kararlari

- Animator hash cache:
  - Parametre isimleri `Animator.StringToHash` ile tek seferde cache'lenir.
- Yazim esigi:
  - Float parametreler sadece deger anlamli degistiginde yazilir (`FLOAT_WRITE_EPSILON`).
- Hareket karari:
  - Threshold + hold kombinasyonu sayesinde titreme ve kisa sifirlamalar azaltilir.

## 5. Bagimliliklar

- Zorunlu:
  - `Transform`
  - `Rigidbody`
  - `Animator`
  - `PlayerAnimationConfigSO`
- Opsiyonel:
  - `ISubscriber<AbilityTriggeredEvent>`

Eksik setup durumunda driver guvenli sekilde no-op davranir.

## 6. Animasyon Entegrasyon Rehberi

1. `PlayerAnimationConfigSO` olustur.
2. Animator parametre adlarini config ile eslestir.
3. Player prefab uzerindeki `PlayerAnimationDriver`'a config ata.
4. Ability trigger/index kullanilacaksa animatorda ilgili parametreleri ac.

## 7. Bilinen Sinirlar

- Root motion tabanli animasyon desteklenmiyor; runtime velocity tabanli calisiyor.
- Layer bazli blend logic ve state machine callback entegrasyonlari su an sade tutuldu.
