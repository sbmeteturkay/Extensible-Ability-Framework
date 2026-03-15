# Smoke Checklist

Bu checklist, her buyuk degisiklikten sonra 5-10 dakikada dogrulama icindir.

## Ability Core

1. Slot tetikleme
- Klavye ve HUD butonu ayni slotu tetikliyor.

2. Cooldown
- Baslatma, update ve bitis eventleri HUD'da dogru gorunuyor.

3. Energy
- Ortak enerji slider'i tum ability kullanimlarinda dogru azalip doluyor.

## Ability Types

1. Dash
- Engel yokken tam mesafe dash.
- Engel varken engelde kesilme (icinden gecmeme).

2. Projectile
- Projectile pool altinda spawn/reuse.
- Impact VFX pool reuse.
- Impact SFX owner audio source ile calisiyor.

3. AOE
- Hit delay dogru.
- Hit visual + hit vfx tetikleniyor.

## Animation

1. Ability trigger
- Trigger + slot index animator parametreleri gidiyor.

2. Ability speed
- Config'ten gelen anim speed parametresi cast aninda uygulanýyor.

## Stability

1. Missing config
- Eksik module/mechanic durumda sistem crash etmeden warning ile devam ediyor.

2. Scene reload
- Reload sonrasi pool root yapisi ve spawn parent'lari dogru kalýyor.
