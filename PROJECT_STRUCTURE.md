# Proje Klasör Yapısı Rehberi

Bu dosya yeni geliştiricilerin ve AI asistanların klasör yapısını anlaması için rehberdir.

---

## Temel Prensipler

> **Her şey `Assets/_Project/` altında.** Unity'nin varsayılan klasörlerine (`Plugins` hariç) doğrudan kod eklenmez.
> **`Features/` = oyun mantığı.** Ham sanat varlıkları (model, texture, material) `Art/` altında yaşar. İkisini prefab bağlar.
> **Featureler birbirini tanımaz.** Haberleşme sadece `Core/EventSystem` üzerinden — C# events.

---

## Tam Klasör Yapısı

```
Assets/
│
├── _Project/
│   ├── Features/                        # Oyun mekaniği ve sistemler
│   │   └── Player/                      # Örnek feature
│   │       ├── Scripts/                 # Sadece .cs dosyaları
│   │       ├── Prefabs/                 # Bu feature'a ait prefab'lar
│   │       ├── Data/                    # ScriptableObject asset'leri (.asset)
│   │       ├── Animations/              # Animator controller + clip'ler
│   │       └── Tests/                   # NUnit testleri
│   │
│   ├── Core/                            # Oyunun iskeleti — feature'lardan bağımsız
│   │   ├── Installers/
│   │   │   ├── GameLifetimeScope.cs     # VContainer root scope
│   │   │   └── ProjectInstaller.cs     # tüm singleton bind'ları
│   │   ├── EventSystem/
│   │   │   └── Events/
│   │   │       ├── PlayerEvents.cs      # OnPlayerDied, OnHealthChanged
│   │   │       ├── ScoreEvents.cs       # OnScoreChanged
│   │   │       └── GameStateEvents.cs   # OnGameStarted, OnGameOver
│   │   ├── GameManager/
│   │   │   ├── GameManager.cs           # state machine: Menu/Playing/GameOver
│   │   │   └── GameState.cs             # enum
│   │   ├── AudioService/
│   │   │   ├── AudioService.cs          # play/stop/fade, BGM + SFX
│   │   │   └── IAudioService.cs
│   │   ├── SaveSystem/
│   │   │   ├── SaveService.cs
│   │   │   ├── SaveData.cs              # serialize edilen veri modeli
│   │   │   └── ISaveService.cs
│   │   └── SceneManagement/
│   │       ├── SceneLoader.cs
│   │       └── LoadingScreen.cs
│   │
│   ├── Shared/                          # 2+ feature'ın kullandığı ortak kod
│   │   ├── Scripts/
│   │   │   ├── Extensions/
│   │   │   │   ├── VectorExtensions.cs
│   │   │   │   └── TransformExtensions.cs
│   │   │   ├── Utilities/
│   │   │   │   ├── ObjectPool.cs        # generic pool sistemi
│   │   │   │   └── Timer.cs
│   │   │   └── Interfaces/
│   │   │       ├── IDamageable.cs
│   │   │       ├── ICollectible.cs
│   │   │       └── IPoolable.cs
│   │   └── Data/                        # paylaşılan SO asset'leri
│   │       └── SO_GameSettings.asset
│   │
│   └── Settings/                        # Proje geneli konfigürasyon asset'leri
│       ├── InputSystem.inputactions      # Input System action map
│       ├── UniversalRenderPipeline.asset # URP ayarları
│       └── AudioMixer.mixer             # Master/BGM/SFX kanalları
│
├── Art/                                 # Ham sanat varlıkları — feature'dan bağımsız
│   ├── Models/
│   ├── Textures/
│   ├── Materials/
│   ├── Sprites/
│   └── VFX/
│
├── Audio/
│   ├── Music/
│   └── SFX/
│
├── Scenes/
│   ├── Boot.unity                       # SADECE init — scope kur, MainMenu'ya geç
│   ├── MainMenu.unity
│   ├── Levels/
│   └── _Dev/                            # Test sahneleri — build'e dahil edilmez
│
├── Plugins/                             # Third-party SDKs — dokunma
│   ├── VContainer/
│   ├── UniTask/
│   └── PrimeTween/
│
└── StreamingAssets/
```

---

## Features/ — Ne Zaman Yeni Klasör Açılır?

Bir özellik şu kriterleri taşıyorsa kendi feature klasörünü hak eder:
- Belirgin bir oyun mekaniği veya sistem (Player, Enemy, Shop, Dialogue)
- En az 2+ script, prefab veya asset içeriyor
- Başka bir feature'ı değiştirmeden bağımsız geliştirilebilir

---

## Core/ — Ne Buraya Girer?

Oyunun iskeletini oluşturan, **başka bir oyuna kopyalasan hiç değiştirmeden çalışacak** sistemler. Oyuna özel hiçbir şey içermez.

| Klasör | İçerik |
|--------|--------|
| `Installers/` | VContainer root scope ve bind'lar |
| `EventSystem/` | Global C# event kanalları |
| `GameManager/` | Oyun state makinesi |
| `AudioService/` | BGM + SFX yönetimi |
| `SaveSystem/` | Kayıt/yükleme altyapısı |
| `SceneManagement/` | Sahne geçişleri, loading screen |

---

## Settings/ — Ne Buraya Girer?

Kod değil, **Unity konfigürasyon asset'leri:**
- Input System action map (`.inputactions`)
- URP render pipeline asset'i
- Physics material'ları
- Audio Mixer asset'i

Feature'a ait değil, projenin altyapısına ait oldukları için burada yaşarlar.

---

## Karar Ağacı: Nereye Koyacaksın?

```
Yeni bir şey ekleyeceksin
          │
          ▼
Kod mu, konfigürasyon asset'i mi?
    │                   │
   Kod               Config asset
    │                   │
    ▼                   ▼
Belirli bir          Settings/
oyun özelliğine
mi ait?
    │           │
   Evet        Hayır
    │           │
    ▼           ▼
Features/   Başka bir oyuna taşısan çalışır mı?
                │               │
               Evet            Hayır
                │               │
                ▼               ▼
             Core/        2+ feature kullanıyor mu?
                                │               │
                              Evet            Hayır
                                │               │
                                ▼               ▼
                            Shared/       Features/ içinde
                                          ilgili klasöre taşı
```

---

## Scenes/ Organizasyonu

```
Scenes/
├── Boot.unity       # SADECE init: GameLifetimeScope yükle, MainMenu'ya geç
├── MainMenu.unity
├── Levels/
│   ├── Level_01.unity
│   └── Level_02.unity
└── _Dev/            # Test sahneleri — Build Settings'e eklenmez
    └── Sandbox.unity
```

Boot sahnesi kuralı: gameplay kodu içermez, sadece VContainer scope'u ayağa kaldırır ve ilk sahneye geçer.

---

## Art/ Texture Naming

Texture suffix'leri (Unity/PBR standardı):

| Suffix | Anlam |
|--------|-------|
| `_D` | Diffuse / Albedo |
| `_N` | Normal Map |
| `_M` | Metallic |
| `_R` | Roughness |
| `_AO` | Ambient Occlusion |
| `_E` | Emission |
