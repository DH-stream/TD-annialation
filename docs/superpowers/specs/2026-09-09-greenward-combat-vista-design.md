# Greenward Combat Vista — Design Specification

## Objective

Turn the current Greenward prototype into a coherent action tower-defense vertical slice. The playable view must immediately show a believable village, preserve the current textured terrain, make traversal readable, and support direct hero combat through ten increasingly difficult waves.

All newly imported content must be free for commercial use. Existing CC0 Quaternius assets remain the preferred visual source.

## Camera and Composition

The gameplay camera uses a fixed, close ARPG-style three-quarter angle behind the hero. It follows smoothly without free rotation or manual panning. A small look-ahead in the hero's facing direction keeps threats visible while retaining the authored composition.

The village is repositioned around the hero's practical camera corridor so houses, market stalls, the smithy, mill, well, props, and inhabitants appear during normal play rather than only at map edges. Large landmarks must frame the image instead of occluding it. Foreground objects may fade or be excluded from the camera corridor if they block the hero.

## Hero Combat

The hero keeps WASD movement and physical collision.

- Left mouse button: short-range sword attack with a forward hit arc, cooldown, visible swing animation, and impact feedback.
- Q: targeted or forward offensive spell suitable for clustered enemies.
- E: defensive or area-control spell centered on the hero.
- R: Ultra attack, available only when its meter is full. It damages enemies in a large radius, uses a strong but brief visual burst, hit-stop, and a damped screen shake. The shake must remain readable and avoid prolonged camera displacement.

Successful damage and kills charge the Ultra meter. Inputs that are unavailable show cooldown or charge feedback instead of silently failing. Combat authority remains in the existing runtime components; no new global manager is introduced.

## Waves and Boss

Solo Greenward contains exactly ten waves. Enemy count, health, speed, and composition rise predictably, with elites introduced before the final wave. The increase should be noticeable without relying only on inflated health.

Wave 10 spawns one named boss with a distinct scale, color treatment, health multiplier, and supporting enemies. While the boss is alive, a prominent boss health bar appears at the top center and updates from the boss's actual health state. Defeating every enemy in wave 10 completes the stage.

## Minimap and HUD

A minimap sits in the lower-left corner and uses the same visual language as the rest of the HUD: dark translucent field, carved stone/aged-metal border, restrained gold accents, and high-contrast symbols.

The minimap is presentation-only and uses an orthographic top-down camera rendering to a texture. It shows:

- the playable terrain and worn path;
- the hero in gold/blue;
- enemies in red and the boss with a larger distinct marker;
- towers in violet;
- the protected gate/objective;

It does not render distant decorative landscape or accept clicks in this slice. The Ultra meter and spell cooldowns occupy a compact action bar near the bottom center, leaving the minimap unobstructed.

## Path and World Treatment

The monster route becomes a naturally worn trail rather than a laid road: desaturated compacted earth and fine gravel, irregular soft edges, partial grass regrowth in quieter strips, subtle wheel ruts, and occasional small stones outside the enemy clearance corridor. The trail remains readable through value and texture variation, not a bright continuous ribbon.

Interactive scenery receives colliders while enemy-route clearance is enforced during placement. The hero cannot walk through buildings, props, trees, or large rocks. Distant scenery outside the playable boundary uses collider-free low-detail terrain tiers, forest masses, hills, ruins, and skyline landmarks to remove the floating-plane appearance without expanding gameplay bounds.

## Architecture

- `TDStrategicCamera` becomes the fixed ARPG follow camera and owns damped shake.
- `TDHeroController` owns combat input and delegates attack resolution to a focused hero-combat component.
- `TDEnemyController` exposes current/max health and boss identity while retaining path traversal and damage authority.
- `TDVerticalSliceBootstrap` owns wave composition and creates the wave-10 boss using existing spawning patterns.
- `TDVerticalSliceHUD` displays action cooldowns, Ultra charge, and boss health.
- A minimap camera/renderer component owns the render texture and marker layer setup.
- `GreenwardWorldBuilder` and `GreenwardGroundBuilder` own visible composition, route treatment, collision placement, and non-interactive distant scenery.

## Validation

EditMode tests cover combat damage/range rules, Ultra charge and consumption, ten-wave progression, boss statistics, boss health reporting, fixed-camera offset/shake decay, path clearance, and minimap marker mapping.

Unity validation includes:

1. clean recompilation and passing existing plus new tests;
2. Play Mode verification of movement collision, sword/spell/Ultra inputs, and animation continuity;
3. full wave progression with wave-10 boss health bar;
4. game-view screenshots confirming visible houses, readable worn trail, coherent HUD/minimap, and distant landscape;
5. console review for newly introduced errors.

## Scope Boundaries

This pass does not add inventory, loot equipment, skill trees, multiplayer, save progression, procedural levels, minimap interaction, or paid assets. Combat and VFX are production-minded vertical-slice implementations using available free art and runtime-generated effects, not a final content-complete AAA combat system.
