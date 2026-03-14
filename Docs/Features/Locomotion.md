# Locomotion Feature Dokumani

## 1. Amac

Locomotion feature'i oyuncu hareket ve donusunu, input kaynagindan ayrik bir servis katmaninda yonetir.
Ability lock durumuna saygili calisir.

## 2. Scope ve Bilesenler

- `LocomotionLifetimeScope`
  - `LocomotionInputGateway` register
  - `LocomotionRuntimeBootstrap` register
  - `LocomotionController` register (`ILocomotionController`)
  - `ILocomotionInputReader` -> `LocomotionInputGateway`

Bagimlilik:
- `LocomotionController`, `ILocomotionLockService` bagimliligini `PlayerLifetimeScope` uzerinden alir.

## 3. Ana Siniflar

- `LocomotionDataSO`
  - Move speed
  - Rotation speed
  - Input dead zone

- `LocomotionInputGateway`
  - InputAction'dan move vector oku ve cache'le.
  - `ILocomotionInputReader` implement eder.

- `LocomotionRuntimeBootstrap`
  - Transform, Rigidbody, data ve input baglantilarini kurar.
  - `LocomotionContext` olusturup controller'a verir.

- `LocomotionController`
  - `IFixedTickable`
  - Dead zone kontrolu
  - Direction normalize
  - Rigidbody `MovePosition` ve `MoveRotation`
  - Lock aciksa hareketi durdurur

## 4. Calisma Akisi

1. Input gateway move input'u cache'ler.
2. Controller her `FixedTick`'te input'u okur.
3. Dead zone altinda ise erken cikis.
4. Lock aciksa erken cikis.
5. Move direction ve distance hesaplanir.
6. Rigidbody ile position/rotation uygulanir.

## 5. Tasarim Kararlari

- `Update` yerine `FixedTick`:
  - Rigidbody hareketiyle tutarlilik icin.
- Input gateway ayrimi:
  - Kontrol mantigi ile input sistemi ayrik kalir.
- Lock service bagimliligi:
  - Ability gibi diger feature'lar hareketi merkezi sekilde bloke edebilir.

## 6. Performans ve Guvenlik

- `GetComponent` cache bootstrap/awake asamasinda yapilir.
- `FixedTick` icinde allocation yapilmaz.
- Eksik data/referans durumunda bootstrap component kendini devre disi birakir.

## 7. Bilinen Sinirlar

- Su an yalnizca duzlem tabanli hareket (Y ekseni hareket yok).
- Sprint/acceleration/air-control gibi advanced behavior katmanlari eklenmedi.
