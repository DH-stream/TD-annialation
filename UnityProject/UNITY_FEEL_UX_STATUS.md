# Greenward Valley feel/UX verification

Date: 2026-09-09
Project: Unity 6.3 LTS / Unity 6000.6.0f1
Scene: `Assets/Scenes/SampleScene.unity`

## Authored scene conversion

`SampleScene` now contains a saved `Greenward Static Environment` hierarchy. It is visible in Edit Mode and can be edited with normal Unity transforms before pressing Play. The one-click authoring command is `TD Annihilation/Greenward/Bake Static Scene`; it is explicit and never runs automatically.

Hand-authored scene content:

- terrain mesh, road ribbons, wheel tracks, river, bridge, village buildings, castle, corruption props, trees, grass, rocks, ambient NPC props, local lights, reflection probe
- `Greenward Gameplay References/Authored Path Waypoints` (11 editable path transforms)
- `Hero Spawn Point`, `Enemy Spawn Point`, and `Castle Target` scene markers

Runtime/data-driven content:

- hero and enemy prefab instances, wave spawning, enemy movement, coin drops/pickups, projectiles, tower instances and tower placement ghost
- gameplay reads the serialized waypoint/spawn references on `TDVerticalSliceBootstrap`; the build-area `Bounds` is serialized there as well. No static environment is instantiated during `Awake`.

Manual edit verification: moved the authored `Darth Artisan Oak Tree` by `+0.25` on X in Edit Mode, saved, reopened `SampleScene`, and confirmed its persisted position `(-45.75, 0.31, -29.00)`.

## Step 0 — systems found

- Hero: runtime object `Hero - Warden of Greenward`, created by `TDVerticalSliceBootstrap.SpawnHero()`, moved by `Assets/Scripts/TD/TDHeroController.cs`.
- Camera: scene object `Main Camera`, target assigned by `TDVerticalSliceBootstrap.Awake()`, followed by `Assets/Scripts/TD/TDStrategicCamera.cs` in `LateUpdate()` as a smoothed third-person chase camera. Right mouse orbits, wheel zooms, and the map bounds remain enforced.
- Loot: `TDEnemyController.Defeat()` calls `SpawnCoin()`, which creates `Coin Drop` and attaches `TDCoinPickup` in `TDVerticalSliceBootstrap.cs`.
- Tower/build: no pre-existing build-mode system was present. Build-mode ownership is now in `TDVerticalSliceBootstrap`; HUD entry is `BUILD TOWER (B)`, with left-click placement and Escape/right-click exit.
- Cinemachine: not installed in `Packages/manifest.json`; the existing follow component remains the correct implementation.
- Render pipeline: Universal Render Pipeline (`com.unity.render-pipelines.universal` 17.6.0). Ghost uses `Universal Render Pipeline/Lit` with transparent blending. Tree materials are converted to URP Lit at runtime to avoid the imported Built-in shader becoming magenta.
- Imported trees: `Assets/Art/ThirdParty/Darth_Artisan/Free_Trees`; runtime copies are in `Assets/Resources/TDAnnihilation/Environment/Trees/` and `GreenwardWorldBuilder.Tree()` now instantiates Oak/Fir/Poplar prefabs.
- Imported hero animations: `Assets/Art/ThirdParty/Blink/Art/Animations/Animations_Starter_Pack/`. `Assets/Resources/TDAnnihilation/WarriorRpg.controller` uses the humanoid Idle and RunForward clips and is selected automatically for the hero.

## Current player, environment, and village extension

- `TDHeroController` now uses the Input System: WASD moves camera-relative, Space jumps, Left/Right Shift sprints, and F attacks. During a wave, left click also attacks when tower placement is not active. `WarriorRpg.controller` contains Idle, Walk, Sprint, Jump, and Attack states sourced from Blink's imported clips.
- `GreenwardWorldBuilder` authors seven `Hearthvale Villager`/`Greenward Guard` objects into the saved scene. `GreenwardVillagerController` moves them to shelter and sets `Scared` during a wave, then returns them slowly to their work positions when the wave completes. `VillagerRpg.controller` contains Work and Flee states in addition to locomotion.
- `GreenwardWaterMotion` scrolls the river material and adds a small surface bob. `GreenwardBoundaryMist` authors four URP particle volumes around the map edge and drifts them at runtime. `GreenwardGroundBuilder` now scatters varied light, dark, and mid-tone grass blade clusters instead of the old single gradient batch.

Runtime verification for this extension: `Temp/thirdperson.png` shows the hero framed from behind with the authored castle, trees, and grass; the hero animator resolves to `WarriorRpg`; movement moved the hero from `(20.39, 1.17, 10.59)` to `(40.72, 3.84, 29.00)` while the camera followed to `(38.20, 7.33, 25.39)`; a Space press reported `grounded=False`; wave start entered `Wave`; defeating the spawned enemies returned the flow to `Build` and villagers reported `Scared=False` after returning. The connected Editor console was clean after the final bake and recompile.

## Acceptance evidence

| Item | Status | Evidence |
|---|---|---|
| 1. Coin magnet radius and pull | PASS | Play Mode frame pair `Temp/coin-pull-before.png` and `Temp/coin-pull-collect.png`; a coin was spawned 4.5 world units from the hero and moved into the hero radius. |
| 2. Pickup pop and particle burst | PASS | `TDCoinPickup` scales 1.0 → 1.7 → 0 and emits `Coin Pickup Burst`; `Temp/coin-burst-evidence.png` shows the burst around the hero. |
| 3. Coin Play Mode evidence | PASS | Gold changed 132 → 144 and the evidence coin was destroyed after collection; Console stayed clear. |
| 4. Camera follows hero | PASS | Camera position changed from `(10.00, 21.55, -19.00)` to `(-23.69, 21.08, -13.05)` after moving the hero to `(-30, 20)`; smoothing/strategic angle preserved. |
| 5. Cinemachine preference | N/A | Package is absent, so no virtual camera was added. |
| 6. Camera Play Mode evidence | PASS | `Temp/camera-follow.png` captured the hero at the moved map position with the camera reframed. |
| 7. Ghost preview follows target | PASS | `Temp/ghost-green.png` shows the translucent tower preview at a meadow target. |
| 8. Green/red validity feedback | PASS | `Temp/ghost-green.png` at `(14, 8)` and `Temp/ghost-red.png` on the route at `(16, -5)`; green meadow, red road. |
| 9. Pipeline-safe ghost material | PASS | Runtime renderer reported `Universal Render Pipeline/Lit`; no magenta output and no Console errors. |
| 10. Exit/disappear/placement | PASS | Off-map preview returned `offSurfaceVisible=False`; valid placement returned `placed=True gold=80 ghostVisible=False`. Escape/right-click paths call the same exit method. |
| 11. Console/build verification | PASS with one known pre-existing build warning | Final Play Mode Console: 0 messages. StandaloneWindows64 player build succeeded at `Build/GreenwardAuthoredScene.exe` (148,520,815 bytes across the output folder, 0 build errors). Unity reported one existing warning: missing RuntimePipelineConfig; the earlier missing-camera-script and stripped-debug-shader warnings did not recur in this authored-scene build. |

## Authored-scene verification

| Check | Status | Evidence |
|---|---|---|
| Edit Mode shows the full world | PASS | `Temp/authored-scene-editmode.png`; hierarchy contains `Greenward Static Environment` and `Greenward Gameplay References` while not playing. |
| Static generation no longer runs on Play | PASS | `TDVerticalSliceBootstrap.Awake()` calls `LoadAuthoredArena()` only; `GreenwardWorldBuilder.Build()` is referenced only by the explicit Editor bake command. |
| Serialized gameplay references | PASS | Bootstrap has 11 waypoint references plus hero/enemy/castle markers and a serialized 90 × 16 × 58 build bounds. |
| Manual edit survives save/reopen | PASS | Authored tree position remained `(-45.75, 0.31, -29.00)` after reopening the scene. |
| Play Mode uses authored path | PASS | Started a wave after scene reload; 5 `TDEnemyController` instances spawned and Console stayed clear. |

Evidence is frame-based because the connected Unity CLI exposes screenshots and Play Mode state, but no video recording endpoint.
