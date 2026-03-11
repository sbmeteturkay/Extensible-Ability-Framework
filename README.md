# CLAUDE.md — Unity Mobile Game Project Rules

Bu dosya AI asistanlar (Claude vb.) ve yeni geliştiriciler için projenin
temel kurallarını, mimarisini ve beklentilerini tanımlar.
Her yeni chat başında bu dosya bağlam olarak verilmelidir.

---

## 🎯 Proje Bağlamı

- **Platform:** iOS & Android (Unity Mobile)
- **Boyut:** Çoğunlukla 3D, bazı 2D sahneler
- **Mimari:** Feature-based (özelliğe göre modüler yapı)
- **Dil:** C# (.NET / Unity)
- **Unity Versiyonu:** *(buraya yaz)*
- **Hedef:** Temiz, sürdürülebilir, takım ve AI dostu kod tabanı

---

## 📦 Teknoloji Stack

| Paket | Amaç |
|-------|------|
| **VContainer** | Dependency Injection |
| **MessagePipe** | Feature arası event sistemi |
| **UniTask** | Async/await, Coroutine yerine |
| **PrimeTween** | Animasyon ve tweening |

---

## 📁 Klasör Yapısı

### Temel Kural
> `Features/` = oyun mantığı (kod, prefab, data, animasyon).
> Ham sanat varlıkları (model, texture, material) `Art/` altında yaşar.
> İkisini birbirine prefab bağlar.

```
Assets/
├── _Project/
│   ├── Features/                        # Oyun mekaniği — her özellik kendi klasöründe
│   │   └── Player/                      # Örnek feature
│   │       ├── Scripts/
│   │       ├── Prefabs/
│   │       ├── Data/                    # ScriptableObject asset'leri
│   │       ├── Animations/
│   │       └── Tests/
│   │
│   ├── Core/                            # Oyunun iskeleti — oyuna özel hiçbir şey içermez
│   │   ├── Installers/
│   │   │   ├── GameLifetimeScope.cs     # VContainer root scope
│   │   │   └── AppLifetimeScope.cs      # Oyuna özel bind'lar (template'den çekilmez)
│   │   ├── EventSystem/
│   │   │   └── Events/
│   │   │       ├── PlayerEvents.cs      # MessagePipe event tipleri (struct)
│   │   │       ├── GameStateEvents.cs
│   │   │       └── ...
│   │   ├── GameManager/
│   │   ├── AudioService/
│   │   ├── SaveSystem/
│   │   └── SceneManagement/
│   │
│   ├── Shared/                          # 2+ feature'ın kullandığı ortak kod
│   │   ├── Scripts/
│   │   │   ├── Extensions/
│   │   │   ├── Utilities/
│   │   │   │   ├── ObjectPool.cs
│   │   │   │   └── Timer.cs
│   │   │   └── Interfaces/
│   │   │       ├── IDamageable.cs
│   │   │       ├── ICollectible.cs
│   │   │       └── IPoolable.cs
│   │   └── Data/
│   │
│   └── Settings/                        # Unity config asset'leri (kod değil)
│       ├── InputSystem.inputactions
│       ├── UniversalRenderPipeline.asset
│       └── AudioMixer.mixer
│
├── Art/
├── Audio/
├── Scenes/
│   ├── Boot.unity
│   ├── MainMenu.unity
│   ├── Levels/
│   └── _Dev/
└── Plugins/
```

---

## ✍️ Naming Conventions

### C# Dosyaları & Sınıflar
| Tür | Format | Örnek |
|-----|--------|-------|
| Class | PascalCase | `PlayerController` |
| Interface | IPascalCase | `IDamageable` |
| ScriptableObject | PascalCase + SO | `EnemyDataSO` |
| Enum | PascalCase | `GameState` |
| Enum value | PascalCase | `GameState.Playing` |
| Private field | _camelCase | `_currentHealth` |
| Public property | PascalCase | `CurrentHealth` |
| Method | PascalCase | `TakeDamage()` |
| Const | UPPER_SNAKE | `MAX_HEALTH` |
| MessagePipe event | PascalCase + Event | `PlayerDiedEvent` |

### Asset Dosyaları
| Tür | Format | Örnek |
|-----|--------|-------|
| Prefab | PascalCase | `EnemySpider.prefab` |
| Scene | PascalCase | `Level_01.unity` |
| Material | M_PascalCase | `M_RockWall.mat` |
| Texture | T_PascalCase + suffix | `T_RockWall_D.png` |
| Animation | A_PascalCase | `A_Player_Run.anim` |
| Audio Clip | SFX_ / BGM_ | `SFX_Explosion.wav` |
| ScriptableObject | SO_PascalCase | `SO_EnemyData_Spider.asset` |

---

## 🏗️ Mimari Kurallar

### Feature Modülleri
- Her feature **kendi klasöründe yaşar:** Scripts, Prefabs, Data, Animations, Tests
- `Features/` = oyun mantığı — ham art asset'leri buraya **girmez** → `Art/`
- Bir feature, **başka bir feature'ın sınıflarına doğrudan referans vermez**
- Featureler arası iletişim: **yalnızca MessagePipe** üzerinden
- 2+ feature aynı şeyi kullanıyorsa → `Shared/`'a taşı
- **Featureler tek tek geliştirilir** — bir feature tamamlanmadan diğerine geçilmez

### Genel Yazılım Prensipleri
- **SRP** — Her sınıfın tek bir sorumluluğu olmalı
- **OCP** — Mevcut kodu değiştirmek yerine extend et
- **DIP** — Somut sınıflara değil, interface'lere bağımlı ol
- **YAGNI** — İhtiyaç olmayan şeyi yazma
- **DRY** — Tekrar eden kodu Shared'a taşı

### Dependency Injection — VContainer
- **DI framework: VContainer** — başka DI çözümü kullanılmaz
- `Core/Installers/GameLifetimeScope.cs` → root scope, tüm core servisler burada
- `Core/Installers/AppLifetimeScope.cs` → oyuna özel bind'lar
- Plain C# class'lar constructor injection ile inject edilir
- MonoBehaviour'lar `[Inject]` metodu ile inject edilir
- **Singleton pattern kullanılmaz** → VContainer Singleton lifetime kullan

### Event Sistemi — MessagePipe
- Feature arası iletişim **yalnızca MessagePipe** üzerinden
- **ScriptableObject event channel kullanılmaz**
- **Static C# event kullanılmaz**
- Event tipleri `readonly struct` olarak tanımlanır → GC dostu
- Event tipleri `Core/EventSystem/Events/` altında yaşar
- Subscribe/unsubscribe `DisposableBag` ile yönetilir

```csharp
// Event tipi — Core/EventSystem/Events/PlayerEvents.cs
public readonly struct PlayerDiedEvent
{
    public readonly int Score;
    public PlayerDiedEvent(int score) => Score = score;
}

// Publish eden taraf
_publisher.Publish(new PlayerDiedEvent(score));

// Subscribe eden taraf
_subscriber.Subscribe(e => HandlePlayerDied(e)).AddTo(_bag);
```

### Script Kuralları
- Her MonoBehaviour'un **tek bir sorumluluğu** olmalı
- `Update()` içinde GetComponent çağırma → Awake'te cache'le
- Magic number kullanma → `const` veya `SerializeField`
- **Coroutine kullanma** → UniTask kullan
- `Find()`, `FindObjectOfType()` **yasak** → VContainer inject et
- **Singleton pattern kullanılmaz**
- Animator string'leri `static readonly int` hash olarak cache'le

### Mobile Performans
- GC alloc minimize edilmeli — özellikle Update() ve hot path'lerde
- Tekrar eden nesneler için **ObjectPool** kullan
- `string` birleştirme döngülerde **StringBuilder**
- Texture: Android → **ETC2**, iOS → **ASTC**
- Draw call hedefi: **<100 per frame**

---

## 🤖 AI İçin Talimatlar

### Genel Yaklaşım
- Naming conventions'a **her zaman** uy
- Her yeni dosya için **tam klasör yolunu** belirt: `// Features/Player/Scripts/PlayerDash.cs`
- Mevcut kodla **tutarlı kal**, kendi stilini katma
- Genel yazılım prensiplerini (SRP, DIP, DRY vb.) her zaman göz önünde bulundur

### YAPMA — Teknik Kurallar
- `FindObjectOfType`, `GameObject.Find` kullanma → VContainer
- Magic string veya magic number bırakma
- Monolithic script yazma → SRP
- `Resources.Load` kullanma → Addressables
- **Singleton pattern kullanma** → VContainer
- **ScriptableObject event channel kullanma** → MessagePipe
- **Static C# event kullanma** → MessagePipe
- **Coroutine kullanma** → UniTask
- Service Locator kullanma → VContainer
- Feature'lar arası doğrudan referans verme → MessagePipe

### YAPMA — Süreç Kuralları
- **Sormadan kod veya dosya yaratma** — ne yaratacağını önce listele, onay al, sonra yaz
- **Sormadan refactor yapma** — önce kapsamı açıkla, onay al
- **Tek hamlede çok fazla dosya değiştirme** — adım adım ilerle
- **Birden fazla feature'ı aynı anda geliştirme** — odak tek feature'da kalmalı
- Kendi pattern tercihini dayatma — seçenek sun, karar geliştiricide

### YAP — Aktif Beklentiler
- Mimariyi bozan bir şey istenirse **uyar ve alternatif sun**
  - Örnek: *"Bu iki feature arasında doğrudan referans oluşturur. MessagePipe ile çözebiliriz, ister misin?"*
- Her yeni sistem için önce **interface tanımla**
- VContainer kullanımında **hangi scope'a bind edileceğini** belirt
- Yeni MessagePipe event gerekiyorsa **hangi dosyaya gideceğini** söyle

### Belirsizlik Durumunda
- Mimari karar gerektiriyorsa **2-3 seçenek sun**, karar geliştiricide
- "Feature mı, Shared mı, Core mu?" belirsizse **sor**
- "Art asset mi, feature data'sı mı?" → art ise `Art/`, config ise `Features/.../Data/`

---

## 🌿 Git Kuralları

### Branch Yapısı
```
main          → production-ready
develop       → aktif geliştirme
feature/xxx   → yeni özellik
fix/xxx       → bug fix
refactor/xxx  → iyileştirme
```

### Commit Formatı
```
[feat]     player dash ability eklendi
[fix]      enemy spawn pozisyon hatası düzeltildi
[refactor] PlayerController SRP'ye göre bölündü
[perf]     object pool enemy sisteme uygulandı
[docs]     CLAUDE.md güncellendi
[art]      player run animasyonu eklendi
```

---

## ✅ Yeni Feature Checklist

- [ ] `Features/FeatureName/` klasörü oluşturuldu
- [ ] Interface tanımlandı
- [ ] ScriptableObject data class'ı → `Features/FeatureName/Data/`
- [ ] Başka feature'larla iletişim MessagePipe üzerinden kuruldu
- [ ] VContainer scope gerekiyorsa tanımlandı
- [ ] Ham art asset'leri `Art/` klasöründe, prefab feature'da
- [ ] Magic number yok
- [ ] Tekrar eden nesneler için ObjectPool kullanıldı
- [ ] UniTask kullanıldı, Coroutine yok
- [ ] Singleton yok
- [ ] Temel test yazıldı
- [ ] CLAUDE.md ile çelişen karar alındıysa güncellendi

---

## 📌 Proje Spesifik Notlar

*(Buraya projeye özel kararları, SDK bilgilerini, kısıtlamaları ekle)*

- Kullanılan SDKs: *(örn: Addressables, Firebase)*
- Hedef platform: *(örn: iOS 13+, Android API 24+)*
- Özel kurallar: *(buraya projeye özgü eklemeler)*
