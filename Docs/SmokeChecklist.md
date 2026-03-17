# Smoke Checklist

Use this checklist after major changes for a quick 5-10 minute validation pass.

## Ability Core

1. Slot Trigger
- Keyboard and HUD button trigger the same slot.

2. Cooldown
- Start, update, and completion events are reflected correctly on the HUD.

3. Energy
- The shared energy slider decreases and refills correctly across all abilities.

## Ability Types

1. Dash
- Full dash distance with no obstacle.
- Dash stops correctly when blocked by an obstacle.

2. Projectile
- Projectile spawns and reuses under the pool root.
- Impact VFX is pooled and reused.
- Impact SFX plays through the owner audio source.

3. AOE
- Hit delay timing is correct.
- Hit visual and hit VFX are triggered.

## Animation

1. Ability Trigger
- Trigger and slot index reach animator parameters.

2. Ability Speed
- Animation speed from config is applied at cast time.

## Stability

1. Missing Config
- With missing module/mechanic data, the system logs warnings and does not crash.

2. Scene Reload
- After reload, pool root structure and spawn parents remain correct.
