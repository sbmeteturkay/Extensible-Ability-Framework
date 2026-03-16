# Animation Feature

## 1. Amac

Animation feature, hareket verisini animator parametrelerine stabil sekilde aktarir ve ability eventlerinden gelen tetik/speed bilgisini animatora yazar.

## 2. Scope Siniri

Feature'in sorumlulugu:
- Hareket parametrelerini hesaplayip animatora yazmak
- Ability trigger/index/speed sinyallerini consume etmek
- Runtime clip override eslesmesini uygulamak

Feature disinda kalanlar:
- Ability execute kararlari
- Locomotion hareket hesaplari

## 3. Runtime Bilesenleri

- `PlayerAnimationDriver`
- `PlayerAnimationConfigSO`

Config kapsami:
- Parametre adlari
- Threshold/hold ve damping ayarlari
- Ability hook parametreleri
- Slot source clip listesi

## 4. Calisma Akisi

1. `FixedUpdate`:
- Pozisyon delta'sindan velocity hesaplanir.
- Local hareket bilesenleri normalize edilir.
- `isMoving` threshold + hold ile karar verilir.

2. `Update`:
- Animator float/bool parametreleri yazilir.
- Pending ability sinyalleri consume edilir.

3. Event akisi:
- `AbilityTriggeredEvent` (domain)
- `AbilityLoadoutSlotAssignedEvent` (presentation)

## 5. Entegrasyon Noktalari

- Locomotion:
  - Hareket verisi dogrudan locomotion component'inden alinmaz; pozisyon deltasi uzerinden hesaplanir
- Ability:
  - `AbilityTriggeredEvent` ile trigger, slot index ve speed bilgisi alinabilir
- Loadout:
  - `AbilityLoadoutSlotAssignedEvent` ile slot-clip eslesmesi guncellenir

## 6. Stabilite Kararlari

- Parametre hash'leri cache edilir.
- Float yazimlari epsilon ile filtrelenir.
- Damping config ile ac/kapa yapilabilir.
- Setup eksiginde driver guvenli no-op davranir.

## 7. Manuel Test Checklist

- MoveX/MoveY/Speed/IsMoving dogru akiyor mu
- Ability trigger + slot index animatora gidiyor mu
- Ability speed parametresi cast aninda guncelleniyor mu
- Slot clip override eslesmesi dogru mu

## 8. Trade-off

- Root motion yerine runtime velocity bazli akis secildi.
- Layer bazli ileri seviye blend/state callback entegrasyonu sade tutuldu.
