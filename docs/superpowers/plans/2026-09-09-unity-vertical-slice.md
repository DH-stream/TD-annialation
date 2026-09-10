# TD Annihilation Unity Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build and verify a free-asset Unity Greenward vertical slice with a WoW-adjacent stylized fantasy presentation and a playable hero-plus-towers wave loop.

**Architecture:** Keep simulation rules and presentation references separate. Use Unity prefabs, ScriptableObjects, Animator Controllers, and standard URP components where they fit; keep the first slice intentionally small and inspectable. The existing Babylon implementation remains untouched.

**Tech Stack:** Unity 6000.6.0f1, URP 17.6, C#, Input System 1.20, AI Navigation 2.0.14, Unity Test Framework, CoplayDev Unity MCP, free CC0 assets.

## Global Constraints

- Free assets only; record source and license for every imported external asset.
- Do not add, update, or remove Unity packages without explicit approval.
- Do not merge to `main` or replace the Babylon version.
- Avoid Minecraft geometry and Kenney-style toy silhouettes.
- Verify actual Play Mode behavior from the normal gameplay camera.

### Task 1: Unity project baseline and asset manifest

**Files:**
- Create: `Assets/Art/ThirdParty/README.md`
- Create: `Assets/Art/Greenward/README.md`
- Modify: `docs/superpowers/specs/2026-09-09-unity-vertical-slice-design.md`

**Interfaces:**
- Produces the verified source/license list and the import target folders used by later scene work.

- [ ] Confirm the Unity project path, version, active scene, package manifest, and existing MCP provider.
- [ ] Select only free candidates whose source pages explicitly state a reusable license. Prefer Quaternius CC0 character, bestiary, animation, medieval village, and fantasy prop packs.
- [ ] Download/import only the selected assets into `Assets/Art/ThirdParty/<source>/` and record URL, license, date checked, file formats, rig status, and animation coverage in `README.md`.
- [ ] Reject any model that is not visually compatible, lacks a usable rig, or cannot be imported without adding an unapproved package.
- [ ] Run Unity asset search and console checks; expected result is a clean import or a recorded blocker with no silent fallback.

### Task 2: Data and simulation contracts

**Files:**
- Create: `Assets/Scripts/TD/TDWaveDefinition.cs`
- Create: `Assets/Scripts/TD/TDEntityState.cs`
- Create: `Assets/Scripts/TD/TDVerticalSliceSimulation.cs`
- Create: `Assets/Tests/EditMode/TDVerticalSliceSimulationTests.cs`

**Interfaces:**
- `TDWaveDefinition` exposes enemy-count and enemy-type data for waves 1–3.
- `TDEntityState` exposes health, alive state, cooldown timestamps, and coin reward.
- `TDVerticalSliceSimulation.StartWave()`, `ApplyDamage()`, `CollectCoins()`, and `Reset()` are deterministic and presentation-agnostic.

- [ ] Write EditMode tests for wave sequencing, damage/death, cooldown rejection, and coin reward before implementing the service.
- [ ] Implement the smallest deterministic state model required by the tests; do not add persistence or networking.
- [ ] Run the focused EditMode tests and confirm they pass without a scene.

### Task 3: Greenward arena and camera

**Files:**
- Create: `Assets/Scenes/GreenwardVerticalSlice.unity`
- Create: `Assets/Prefabs/Environment/GreenwardCastle.prefab`
- Create: `Assets/Prefabs/Environment/GreenwardTowerSite.prefab`
- Create/Modify: `Assets/Scripts/TD/TDGreenwardBootstrap.cs`

**Interfaces:**
- The scene contains `GreenwardRoot`, `Main Camera`, `Directional Light`, global volume, `HeroSpawn`, `CastleGate`, `EnemyPath`, and two valid tower sites.
- `TDGreenwardBootstrap` exposes `ResetSlice()` and `FrameGameplayCamera()` for developer verification.

- [ ] Build the arena from imported free assets and simple authored shapes only where they improve silhouette or fill gaps.
- [ ] Use a raised, readable path and castle composition; avoid a flat green plane and black void.
- [ ] Configure camera follow/zoom bounds for strategic visibility and test from gameplay height.
- [ ] Validate hierarchy, camera, lighting, and scene references through MCP.

### Task 4: Hero and enemy presentation

**Files:**
- Create: `Assets/Prefabs/Characters/KingAlric.prefab`
- Create: `Assets/Prefabs/Characters/MeleeRaider.prefab`
- Create: `Assets/Prefabs/Characters/FastRaider.prefab`
- Create: `Assets/Prefabs/Characters/HeavyRaider.prefab`
- Create: `Assets/Animators/KingAlric.controller`
- Create: `Assets/Animators/Raider.controller`
- Create/Modify: `Assets/Scripts/TD/TDHeroController.cs`
- Create/Modify: `Assets/Scripts/TD/TDEnemyController.cs`

**Interfaces:**
- `TDHeroController` receives movement and attack input but owns no cosmetic identity.
- `TDEnemyController` consumes path/state data and drives idle, move, attack, hit, and death animation parameters.
- Both prefabs expose a replaceable visual root and animator reference.

- [ ] Configure imported models with correct scale, humanoid/avatar settings, root-motion policy, and loop settings.
- [ ] Build Animator Controller states and parameters for idle, move, attack, hit, and death.
- [ ] Add hero movement, basic attack, special attack, recovery, and skin swap without changing stats or input code.
- [ ] Add enemy movement, attack lock, hit reaction, death lock, and path arrival behavior.
- [ ] Verify no enemy moves while attacking or after death, and inspect foot placement from the normal camera.

### Task 5: Towers, projectiles, impact, and coins

**Files:**
- Create: `Assets/Prefabs/Towers/ArcaneSpire.prefab`
- Create: `Assets/Prefabs/Towers/EmberBallista.prefab`
- Create: `Assets/Prefabs/VFX/ImpactBurst.prefab`
- Create: `Assets/Scripts/TD/TDTowerController.cs`
- Create: `Assets/Scripts/TD/TDProjectile.cs`
- Create: `Assets/Scripts/TD/TDCoinPickup.cs`

**Interfaces:**
- `TDTowerController` selects targets, fires visible projectiles, and applies role-specific damage/cooldowns.
- `TDProjectile` travels to one target and emits a bounded impact effect.
- `TDCoinPickup` exposes magnet radius and pickup callback.

- [ ] Author two readable silhouettes with distinct range, fire rate, color, and projectile behavior.
- [ ] Implement target selection and projectile impact using the simulation contract.
- [ ] Spawn coins on enemy death and magnet them to the hero before crediting gold.
- [ ] Verify tower placement validity, targeting readability, projectile ownership, and pickup audio/visual feedback.

### Task 6: UI, developer menu, and audio hooks

**Files:**
- Create: `Assets/UI/GreenwardHUD.uxml`
- Create: `Assets/UI/GreenwardHUD.uss`
- Create: `Assets/Scripts/TD/TDVerticalSliceHUD.cs`
- Create: `Assets/Audio/README.md`

**Interfaces:**
- HUD shows wave, gold, base health, attack readiness, and controls.
- Developer actions call `StartWave()`, `ResetSlice()`, `SwapSkin()`, and test-spawn methods without bypassing simulation rules.

- [ ] Build a restrained warm fantasy HUD that keeps the battlefield readable.
- [ ] Add dev shortcuts/buttons for the required verification flows.
- [ ] Add free-only audio hooks and document missing clips if no compatible free source is available.
- [ ] Verify UI at the target desktop resolution and confirm no HUD element obscures the play space.

### Task 7: Integrated verification and evidence

**Files:**
- Modify: `PROJECT_STATUS.md`
- Create: `docs/evidence/unity-greenward-vertical-slice.md`

- [ ] Run console checks, EditMode tests, PlayMode tests, scene validation, and a development build.
- [ ] Play the real loop from the normal camera: move hero, attack, start waves, watch towers fire, collect coins, swap skin, and reset.
- [ ] Capture screenshots/video only from actual gameplay state; compare against the Babylon baseline without treating arranged editor views as approval evidence.
- [ ] Record exact working behavior, open blockers, asset licenses, and performance observations.
- [ ] Review the full branch against current `origin/main`; do not merge or replace the Babylon version.
