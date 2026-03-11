# CLAUDE.md — Unity Mobile Game Project Rules

Bu dosya AI asistanlar (Claude, Copilot vb.) ve yeni katılan geliştiriciler için projenin
temel kurallarını, mimarisini ve beklentilerini tanımlar.

---

## 🎯 Proje Bağlamı

- **Platform:** iOS & Android (Unity Mobile)
- **Boyut:** Çoğunlukla 3D, bazı 2D sahneler
- **Mimari:** Feature-based (özelliğe göre modüler yapı)
- **Dil:** C# (.NET / Unity)
- **Unity Versiyonu:** *(buraya yaz)*
- **Hedef:** Temiz, sürdürülebilir, takım dostu kod tabanı

---

## 📁 Klasör Yapısı

### Temel Kural
> `Features/` = oyun mantığı (kod, prefab, data, animasyon).
> Ham sanat varlıkları (model, texture, material) `Art/` altında yaşar.
> İkisini birbirine prefab bağlar.

```
Assets/
├── _Project/                        # Tüm proje kodu buraya — Unity default klasörlerine dokunma
│   ├── Features/                    # Her oyun özelliği kendi klasöründe
│   │   └── Player/                  # Örnek feature
│   │       ├── Scripts/             # Sadece .cs dosyaları
│   │       ├── Prefabs/             # Bu feature'a ait prefab'lar
│   │       ├── Data/                # ScriptableObject asset'leri (.asset)
│   │       ├── Animations/          # Animator controller + anim clip'ler
│   │       ├── PlayerLifetimeScope.cs   # VContainer — bu feature'ın DI scope'u
│   │       └── Tests/
│   │   # Diğer feature'lar: Enemy/, Inventory/, UI/, Camera/, Audio/ ...
│   │
│   ├── Core/                        # Oyunun iskeleti — feature'lardan bağımsız
│   │   ├── Installers/
│   │   │   ├── ProjectInstaller.cs  # Tüm core servisleri buraya bind edilir
│   │   │   └── GameLifetimeScope.cs # VContainer root scope
│   │   ├── GameManager/
│   │   ├── EventSystem/
│   │   ├── SaveSystem/
│   │   └── SceneManagement/
│   │
│   ├── Shared/                      # 2+ feature'ın kullandığı ortak kod
│   │   ├── Scripts/
│   │   │   ├── Extensions/          # static extension metodları
│   │   │   ├── Utilities/           # ObjectPool, Timer vb.
│   │   │   └── Interfaces/          # IDamageable, IInteractable vb.
│   │   └── Data/                    # Paylaşılan SO asset'leri
│   │
│   └── Settings/                    # Input, Physics, Render Pipeline ayarları
│
├── Art/                             # Ham sanat varlıkları — feature'dan bağımsız
│   ├── Models/
│   ├── Textures/
│   ├── Materials/
│   ├── Sprites/
│   └── VFX/
├── Audio/
│   ├── Music/
│   └── SFX/
├── Scenes/
│   ├── Boot.unity                   # Sadece init: scope kur, MainMenu'ya geç
│   ├── MainMenu.unity
│   └── Levels/
├── Plugins/                         # Third-party SDKs — dokunma
└── StreamingAssets/
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
| Event | OnPascalCase | `OnPlayerDied` |

### Asset Dosyaları
| Tür | Format | Örnek |
|-----|--------|-------|
| Prefab | PascalCase | `EnemySpider.prefab` |
| Scene | PascalCase | `Level_01.unity` |
| Material | M_PascalCase | `M_RockWall.mat` |
| Texture | T_PascalCase | `T_RockWall_D.png` |
| Animation | A_PascalCase | `A_Player_Run.anim` |
| Audio Clip | SFX_ / BGM_ | `SFX_Explosion.wav` |
| ScriptableObject | SO_PascalCase | `SO_EnemyData_Spider.asset` |

---

## 🏗️ Mimari Kurallar

### Feature Modülleri
- Her feature **kendi klasöründe yaşar:** Scripts, Prefabs, Data, Animations, Tests
- `Features/` = oyun mantığı. Ham art asset'leri (model, texture) buraya **girmez** → `Art/`'a gider
- Bir feature, **başka bir feature'ın iç sınıflarına doğrudan referans vermez**
- Featureler arası iletişim: **C# events (Action) veya ScriptableObject channel'ları** üzerinden
- Eğer iki feature aynı şeyi kullanıyorsa → **önce düşün:** gerçekten shared mi, yoksa bir feature'a mı ait? Shared'a taşımak için en az 2 kullanıcı gerekir.

### Dependency Injection — VContainer
- **DI framework: VContainer** — Zenject veya Service Locator kullanılmaz
- `Core/Installers/GameLifetimeScope.cs` → root scope, tüm core servisleri burada bind edilir
- Her feature, gerekiyorsa kendi `FeatureNameLifetimeScope.cs`'ini tanımlar
- MonoBehaviour'lar VContainer tarafından inject edilmez — sadece plain C# class'lar inject edilir
- MonoBehaviour'a bağımlılık gerekiyorsa: `[Inject]` ile constructor injection değil, **`LifetimeScope` üzerinden resolve**

### Script Kuralları
- Her MonoBehaviour'un **tek bir sorumluluğu** olmalı (SRP)
- `Update()` içinde **sık çağrılan işlemler** Awake'te cache'lenmeli — GetComponent çağırma
- Magic number kullanma → `const` veya `SerializeField` yap
- Coroutine yerine mümkünse **UniTask / async-await** tercih et
- `Find()`, `FindObjectOfType()` **production kodda yasak** — VContainer inject et ya da event kullan

### Mobile Performans
- Her sahne başlangıcında **GC alloc** minimize edilmeli
- Object pooling: tekrar eden nesneler için her zaman **pool kullan**
- `string` birleştirme döngülerde **StringBuilder** ile yap
- Texture compression: Android için **ETC2**, iOS için **ASTC**
- Draw call hedefi: mobil için **<100 per frame**

---

## 🤖 AI İçin Talimatlar

Bu bölüm AI asistanların (Claude vb.) bu projede nasıl davranması gerektiğini tanımlar.

### Genel Yaklaşım
- Kod yazarken **yukarıdaki naming conventions'a** her zaman uy
- Her yeni script veya asset için **hangi klasöre gittiğini açıkça belirt**
  - Örnek: `Features/Player/Scripts/PlayerDash.cs` veya `Shared/Scripts/Utilities/Timer.cs`
- Mevcut koda ekleme yaparken **klasör yapısını ve mimariyi bozmadan** ekle
- Mevcut kodla **tutarlı kal** — kendi stil tercihlerini dayatma

### YAPMA — Kesin Kurallar
- `FindObjectOfType`, `GameObject.Find` kullanma → VContainer ile inject et
- Magic string veya magic number bırakma
- Monolithic script yazma (bir script = bir sorumluluk)
- `Resources.Load` kullanma → Addressables tercih et
- Service Locator pattern önerme → bu projede VContainer kullanılıyor
- Sahnelere doğrudan referans verme → SceneManagement sistemi üzerinden git

### YAPMA — Süreç Kuralları
- **Sormadan büyük refactor yapma.** Refactor gerekiyorsa önce kapsamı açıkla, onay al
- **Tek hamlede çok fazla dosya değiştirme.** Değişikliği küçük adımlara böl, her adımı onayla
- Kendi pattern tercihini dayatma — seçenek sun, karar geliştiricide

### YAP — Aktif Beklentiler
- Mimariyi bozan bir şey istenirse **sessizce yapma, uyar ve alternatif sun**
  - Örnek: "Bu Feature A'dan Feature B'ye doğrudan referans ekler, mimariyi bozar. Bunun yerine event channel kullanabiliriz, ister misin?"
- Her yeni sistem için önce **interface tanımla**
- Featureler arası bağımlılık varsa **event veya SO channel** öner
- Kod bloğu yazarken **tam dosya yolu** ile başla: `// Features/Enemy/Scripts/EnemySpawner.cs`
- VContainer kullanımında **hangi scope'a bind edileceğini** belirt

### Belirsizlik Durumunda
- Mimari karar gerektiren durumlarda **2-3 seçenek sun**, karar geliştiricide
- "Bu Feature mı, Shared mı, Core mu?" sorusu belirsizse **sor, tahmin etme**
- "Bu art asset mi, feature data'sı mı?" belirsizse → art ise `Art/`, SO/config ise `Features/.../Data/`

---

## 🌿 Git Kuralları

### Branch Yapısı
```
main          → production-ready, her zaman çalışır
develop       → aktif geliştirme
feature/xxx   → yeni özellik (feature/player-dash)
fix/xxx       → bug fix (fix/enemy-spawn-crash)
refactor/xxx  → kod iyileştirme
```

### Commit Mesajı Formatı
```
[feat] player dash ability eklendi
[fix] enemy spawn pozisyon hatası düzeltildi
[refactor] PlayerController tek sorumluluk prensibine göre bölündü
[perf] object pool sistemi enemy'lere uygulandı
[docs] CLAUDE.md güncellendi
[art] player run animasyonu eklendi
```

---

## ✅ Yeni Feature Checklist

Yeni bir özellik eklerken şu adımları takip et:

- [ ] `Features/FeatureName/` klasörü oluşturuldu (Scripts, Prefabs, Data, Animations, Tests)
- [ ] Interface tanımlandı (gerekiyorsa) → `Shared/Scripts/Interfaces/` veya feature içi
- [ ] ScriptableObject data class'ı yazıldı → `Features/FeatureName/Data/`
- [ ] VContainer scope gerekiyorsa `FeatureNameLifetimeScope.cs` oluşturuldu
- [ ] Başka feature'larla iletişim event/SO channel üzerinden kuruldu (doğrudan referans yok)
- [ ] Ham art asset'leri `Art/` klasörüne konuldu, prefab feature klasöründe
- [ ] Magic number yok — const veya SO kullanıldı
- [ ] Object pool kullanıldı (tekrar eden nesneler için)
- [ ] Temel test yazıldı
- [ ] CLAUDE.md ile çelişen bir karar alındıysa bu dosya güncellendi

---

## 📌 Proje Spesifik Notlar

*(Buraya projeye özel kararlarını, üçüncü taraf SDK bilgilerini, bilinen kısıtlamaları ekle)*

- Kullanılan SDKs: *(örn: DoTween, Addressables, Firebase)*
- Bilinen kısıtlamalar: *(örn: hedef min iOS 13, Android API 24)*
- Özel kurallar: *(buraya projeye özgü eklemeler yapabilirsin)*
