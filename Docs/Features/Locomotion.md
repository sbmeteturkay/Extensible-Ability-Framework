# Locomotion Feature

## 1. Amac

Locomotion feature, player hareket ve donusunu fizik pipeline ile uyumlu bicimde yonetir.
Ability lock durumunu merkezi servisten okuyarak hareketi bloke edebilir.

## 2. Scope Siniri

Feature'in sorumlulugu:
- Move input'u okumak ve cache'lemek
- Rigidbody tabanli movement + rotation uygulamak
- Dead zone ve lock kontrolu

Feature disinda kalanlar:
- Ability davranislari
- Animator state kararlari

## 3. Runtime Bilesenleri

- `LocomotionInputGateway` (`ILocomotionInputReader`)
- `LocomotionRuntimeBootstrap`
- `LocomotionController` (`ILocomotionController`, `IFixedTickable`)
- `LocomotionDataSO`

Bagimlilik:
- `LocomotionController` -> `ILocomotionLockService` (shared)

## 4. Calisma Akisi

1. Gateway input vector'u cache'ler.
2. Controller `FixedTick`'te input'u okur.
3. Dead zone altinda erken cikis yapar.
4. Lock aciksa erken cikis yapar.
5. Yon normalize edilir.
6. `MovePosition` ve `MoveRotation` uygulanir.

## 5. Data Kontrati

`LocomotionDataSO` alanlari:
- `MoveSpeed`
- `RotationSpeedDegreesPerSecond`
- `InputDeadZone`

## 6. Entegrasyon Noktalari

- Input:
  - `LocomotionInputGateway` yalnizca input okumak ve cache'lemekle sorumludur
- Ability:
  - `ILocomotionLockService` uzerinden hareket kilidi okunur
- Animation:
  - Driver, animator verisini locomotion'dan itmek yerine pozisyon deltasi uzerinden hesaplar

## 7. Performans ve Guvenlik

- `FixedTick` icinde allocation yok.
- Runtime referanslari bootstrap asamasinda kurulur.
- Eksik config/referans durumunda bootstrap kendini guvenli sekilde kapatir.

## 8. Manuel Test Checklist

- Dead zone davranisi
- W/A/S/D + joystick input uyumu
- Lock acikken hareketin durmasi
- Rotation hizinin stabilitesi
- Rigidbody ile duvar/collision uyumu

## 9. Trade-off

- Su an duzlem tabanli hareket var (Y ekseni locomotion yok).
- Sprint/acceleration/air-control katmanlari sonraki fazda eklenebilir.
