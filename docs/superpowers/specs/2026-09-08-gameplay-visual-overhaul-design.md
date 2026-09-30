# TD Annihilation — Gameplay & Visual Overhaul design

## Current evidence — 2026-09-08

The latest branch still contains freeform placement, a green/red preview and the connected fortress. The simulation had one generic enemy shape and no lifecycle state; the renderer played its walk animation forever. That did not meet the monster or combat-animation goals.

## Vertical-slice outcome

A Greenward run must support this readable loop: place defenses, start a wave, distinguish incoming enemy types, use the king’s attacks, collect dropped coins, then make another placement decision before the next wave.

## Enemy slice

Use one coherent CC0 Quaternius Ultimate Monsters family:

| Role | Simulation id | Model | Gameplay role | Visual states |
| --- | --- | --- | --- | --- |
| fast harrier | `skitter` | Green Spiky Blob | fast, fragile path pressure | spawn, idle, walk, bite, hit, death |
| raider | `raider` | Orc | normal melee baseline | spawn, idle, run, punch, hit, death |
| brute | `brute` | Yeti | slow, resilient gate threat | spawn, idle, run, punch, hit, death |

The simulation owns the archetype and lifecycle phase. The Babylon layer selects the matching model and semantic animation; it never decides combat outcomes. A hit changes an enemy to `hit`; a lethal hit changes it to `dying`, awards a coin once, then retains the enemy through its death animation. Reaching the gate enters `attacking` long enough to telegraph a strike before base damage. Spawn waits briefly before movement.

Spawn mixes are deterministic: early waves teach harriers and raiders; the brute joins in wave 2. This gives different problems through speed, health, reward and animation without random results or a content framework.

## Guardrails

- Keep glTF assets in `public/assets/vendor/quaternius/ultimate-monsters/`; record attribution and CC0 terms.
- Reuse the existing Babylon asset-container and pool pattern, with one pool per monster kind.
- Retain the existing pure simulation module and existing coin/economy behavior.
- Preserve the connected fortress and freeform tower constraints.
- Do not add a menu, mount system, cosmetic store, online feature or generic content factory in this slice.

## Later milestones

1. **Playable core:** finish monsters, two contrasting tower choices, actual multi-wave and coin proof.
2. **Game feel:** tune hero reactions, impact/recovery, motion, coin attraction and wave rhythm.
3. **Visual cohesion:** terrain depth, castle details, lighting, VFX and HUD pass.
4. **Extensibility:** alternate hero skin switch, mount variant, endless/progression regression checks and performance measurement.
