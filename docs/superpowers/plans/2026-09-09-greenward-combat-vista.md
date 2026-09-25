# Greenward Combat Vista Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a polished action-TD slice with a close fixed ARPG camera, visible village, hero combat and Ultra, ten escalating waves with a boss, a framed minimap, a naturally worn route, and decorative landscape beyond the playable bounds.

**Architecture:** Extend the existing bootstrap, enemy, camera, HUD, and procedural world systems without introducing a global manager. Extract focused combat and minimap components so gameplay state remains testable in EditMode while the bootstrap continues to own stage composition.

**Tech Stack:** Unity 6000.6.0f1, C#, Universal Render Pipeline, Unity Input System, Unity Test Framework, runtime-generated meshes/materials, free CC0 Quaternius assets.

## Global Constraints

- Use only assets that are free for commercial use; preserve source and license files.
- Keep WASD hero movement and physical collision.
- Use a fixed close three-quarter camera behind the hero with no free rotation or manual pan.
- Solo Greenward contains exactly ten waves; wave 10 contains a named boss and visible boss health bar.
- The minimap is lower-left, presentation-only, and does not render decorative distant landscape.
- Do not add inventory, equipment, skill trees, multiplayer, saves, procedural levels, or paid packages.
- Edit repository files first, mirror them into the open Unity project, and sync Unity-generated metadata back into the repository.

---

### Task 1: ARPG Camera and Visible Village Composition

**Files:**
- Modify: `UnityProject/Assets/Scripts/TD/TDStrategicCamera.cs`
- Modify: `UnityProject/Assets/Scripts/TD/GreenwardWorldBuilder.cs`
- Modify: `UnityProject/Assets/Scripts/TD/GreenwardGroundBuilder.cs`
- Test: `UnityProject/Assets/Tests/EditMode/GreenwardGameplayPresentationTests.cs`

**Interfaces:**
- Consumes: `TDStrategicCamera.SetTarget(Transform)` and `GreenwardWorldLayout.IsRoad(Vector2, float)`.
- Produces: `TDStrategicCamera.CalculateFollowPosition(Vector3, Vector3)`, `TDStrategicCamera.AddShake(float)`, and `TDStrategicCamera.ShakeAmplitude`.

- [ ] **Step 1: Write failing camera tests**

Add tests asserting that `CalculateFollowPosition` returns the fixed close offset behind the target, that no pan input API remains necessary, and that shake amplitude decays toward zero after `TickShake(1f)`.

```csharp
[Test]
public void ArpgCameraStaysBehindHeroAndShakeDecays()
{
    Vector3 position = TDStrategicCamera.CalculateFollowPosition(Vector3.zero, Vector3.forward);
    Assert.That(position.y, Is.GreaterThan(8f));
    Assert.That(position.z, Is.LessThan(-8f));
    float remaining = TDStrategicCamera.DecayShake(1f, 0.5f, 4f);
    Assert.That(remaining, Is.LessThan(1f));
}
```

- [ ] **Step 2: Run the test and verify RED**

Run the EditMode test assembly through Unity MCP. Expected: compilation failure because `CalculateFollowPosition` and `DecayShake` do not exist.

- [ ] **Step 3: Implement fixed follow and damped shake**

Replace Shift-pan behavior with a fixed local offset, hero-forward look-ahead, `SmoothDamp` follow, and a short Perlin-noise shake whose amplitude decays every frame. Keep camera rotation constrained to the authored three-quarter pitch.

- [ ] **Step 4: Recompose the village in the camera corridor**

Move the authored house, inn, smithy, market, well, mill, and prop placements into the area visible around the hero's route. Keep every building footprint outside `IsRoad(position, clearance)` and reduce or relocate foreground castle pieces that intersect the camera frustum.

- [ ] **Step 5: Verify and commit**

Run all EditMode tests, enter Play Mode, capture 1280×720 game views at the starting position and one route bend, and confirm at least three authored buildings are visible without blocking the hero. Commit with `feat: frame Greenward with ARPG camera`.

### Task 2: Hero Sword, Spells, and Ultra

**Files:**
- Create: `UnityProject/Assets/Scripts/TD/TDHeroCombat.cs`
- Modify: `UnityProject/Assets/Scripts/TD/TDHeroController.cs`
- Modify: `UnityProject/Assets/Scripts/TD/TDEnemyArchetype.cs`
- Modify: `UnityProject/Assets/Scripts/TD/TDStrategicCamera.cs`
- Test: `UnityProject/Assets/Tests/EditMode/TDHeroCombatTests.cs`

**Interfaces:**
- Consumes: `TDEnemyController.TakeDamage(float)` and `TDStrategicCamera.AddShake(float)`.
- Produces: `TDCombatRules.IsInSwordArc(...)`, `TDCombatState.AddCharge(float)`, `TDCombatState.TrySpendUltra()`, `TDHeroCombat.SwordCooldown01`, `SpellQCooldown01`, `SpellECooldown01`, and `UltraCharge01`.

- [ ] **Step 1: Write failing pure combat-rule tests**

```csharp
[Test]
public void SwordHitsOnlyTargetsInsideRangeAndForwardArc()
{
    Assert.That(TDCombatRules.IsInSwordArc(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 1.5f), 2f, 100f), Is.True);
    Assert.That(TDCombatRules.IsInSwordArc(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -1f), 2f, 100f), Is.False);
}

[Test]
public void UltraRequiresFullChargeAndConsumesIt()
{
    var state = new TDCombatState(100f);
    state.AddCharge(100f);
    Assert.That(state.TrySpendUltra(), Is.True);
    Assert.That(state.Charge01, Is.Zero);
}
```

- [ ] **Step 2: Run tests and verify RED**

Expected: compilation failure because `TDCombatRules` and `TDCombatState` do not exist.

- [ ] **Step 3: Implement minimal combat state and hit rules**

Create pure C# range/arc checks and clamped Ultra charge. Do not query input or scene objects from these types.

- [ ] **Step 4: Implement runtime attacks**

Attach `TDHeroCombat` beside `TDHeroController`. Map left mouse to sword, Q to a forward arcane burst, E to a radial ward pulse, and R to Ultra. Use non-allocating enemy iteration where practical, enforce cooldowns, charge Ultra from confirmed damage, and trigger the existing Animator with graceful fallback when attack states are absent.

- [ ] **Step 5: Add readable generated VFX and feedback**

Use short-lived URP emissive meshes, expanding rings, directional slash arcs, impact flashes, and light pulses. Ultra applies a brief hit-stop and calls `AddShake` once; it must not permanently modify camera offset or time scale.

- [ ] **Step 6: Verify and commit**

Run all tests. In Play Mode verify every input, cooldown rejection, enemy damage, Ultra charge/consumption, and camera recovery. Commit with `feat: add hero combat and Ultra`.

### Task 3: Ten-Wave Escalation and Boss Health

**Files:**
- Modify: `UnityProject/Assets/Scripts/TD/TDGameFlow.cs`
- Modify: `UnityProject/Assets/Scripts/TD/TDEnemyArchetype.cs`
- Modify: `UnityProject/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs`
- Test: `UnityProject/Assets/Tests/EditMode/TDWaveProgressionTests.cs`

**Interfaces:**
- Consumes: `TDGameFlow(int maxWaves)` and `TDEnemyController.Configure(...)`.
- Produces: `TDWaveRules.GetComposition(int)`, `TDWaveComposition`, `TDEnemyController.CurrentHealth`, `MaxHealth`, `IsBoss`, and `DisplayName`; bootstrap property `ActiveBoss`.

- [ ] **Step 1: Write failing progression tests**

```csharp
[Test]
public void GreenwardHasTenEscalatingWavesAndBossOnTen()
{
    TDWaveComposition first = TDWaveRules.GetComposition(1);
    TDWaveComposition ninth = TDWaveRules.GetComposition(9);
    TDWaveComposition tenth = TDWaveRules.GetComposition(10);
    Assert.That(ninth.TotalThreat, Is.GreaterThan(first.TotalThreat));
    Assert.That(tenth.HasBoss, Is.True);
    Assert.That(tenth.BossName, Is.EqualTo("Gorath, Breaker of Greenward"));
}
```

- [ ] **Step 2: Run tests and verify RED**

Expected: compilation failure because wave composition types do not exist.

- [ ] **Step 3: Implement deterministic wave rules**

Define count, health, speed, elite count, spawn cadence, and boss parameters for waves 1–10. Clamp requests to this range and make `TotalThreat` strictly increase.

- [ ] **Step 4: Integrate spawning and boss state**

Set `stageWaveCount` to 10, spawn normal/elite mixtures from composition, and create the named boss only on wave 10. Expose health without allowing HUD code to mutate it. Clear `ActiveBoss` naturally when the boss is destroyed.

- [ ] **Step 5: Verify and commit**

Run all tests and use Unity evaluation to advance through all ten wave definitions. In Play Mode confirm the boss is distinct, smaller enemies remain below hero scale, and victory occurs only after wave 10 is cleared. Commit with `feat: add ten-wave boss progression`.

### Task 4: Framed Minimap and Combat HUD

**Files:**
- Create: `UnityProject/Assets/Scripts/TD/TDMinimapController.cs`
- Create: `UnityProject/Assets/Scripts/TD/TDVerticalSliceHUD.cs`
- Modify: `UnityProject/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs`
- Test: `UnityProject/Assets/Tests/EditMode/TDMinimapTests.cs`

**Interfaces:**
- Consumes: hero transform, `TDVerticalSliceBootstrap.ActiveBoss`, `TDHeroCombat` cooldown/charge properties, and scene layers.
- Produces: `TDMinimapProjection.WorldToMap(Vector3, Rect, Bounds)` and a lower-left minimap render texture with marker overlay.

- [ ] **Step 1: Write failing projection tests**

```csharp
[Test]
public void WorldCenterMapsToMinimapCenter()
{
    Rect rect = new Rect(20f, 20f, 180f, 180f);
    Bounds world = new Bounds(Vector3.zero, new Vector3(96f, 1f, 64f));
    Assert.That(TDMinimapProjection.WorldToMap(Vector3.zero, rect, world), Is.EqualTo(rect.center));
}
```

- [ ] **Step 2: Run tests and verify RED**

Expected: compilation failure because `TDMinimapProjection` does not exist.

- [ ] **Step 3: Implement minimap rendering**

Create an orthographic top-down camera with a dedicated render texture and culling configuration. Render playable terrain/path/buildings, exclude distant-landscape objects, and draw hero, enemy, boss, tower, and objective markers using the tested projection.

- [ ] **Step 4: Implement coherent HUD framing**

Extract the existing `TDVerticalSliceHUD` class from the bootstrap file into its own file without changing behavior, then draw the minimap in the lower-left with a dark translucent inset, stone/aged-metal layered border, clipped corners, and restrained gold accents matching the existing header. Add bottom-center sword/Q/E cooldowns and Ultra charge; add the boss bar at top center only while `ActiveBoss` exists.

- [ ] **Step 5: Verify and commit**

Run all tests. Capture gameplay with normal enemies and with the boss; verify markers remain inside the frame, the boss bar tracks actual damage, and the minimap does not obscure existing controls. Commit with `feat: add minimap and combat HUD`.

### Task 5: Worn Trail, Living World, and Distant Landscape

**Files:**
- Modify: `UnityProject/Assets/Scripts/TD/GreenwardGroundBuilder.cs`
- Modify: `UnityProject/Assets/Scripts/TD/GreenwardWorldBuilder.cs`
- Modify: `UnityProject/Assets/Scripts/TD/GreenwardMaterialLibrary.cs`
- Test: `UnityProject/Assets/Tests/EditMode/GreenwardGameplayPresentationTests.cs`

**Interfaces:**
- Consumes: route list and `GreenwardWorldLayout.IsRoad`.
- Produces: `GreenwardGroundBuilder.RouteVisibility` tuning constant and collider-free `Distant Landscape` hierarchy excluded from minimap rendering.

- [ ] **Step 1: Write failing world-safety tests**

Add tests asserting route surface width is below the previous 4.1-meter ribbon, decoration clearance remains at least 2.6 meters, and all generated distant-landscape roots are designated non-interactive/minimap-excluded by the builder contract.

- [ ] **Step 2: Run tests and verify RED**

Expected: failure because the new route constants and distant-landscape contract do not exist.

- [ ] **Step 3: Build the worn trail treatment**

Replace the uniform gravel ribbon with layered compacted soil and sparse fine gravel. Use irregular alpha-clipped or mesh edges, grass intrusion along edges and selected center sections, subtle low-contrast ruts, and stones only beyond enemy clearance. Preserve the existing uneven terrain height.

- [ ] **Step 4: Populate the playable village**

Add fences, carts, barrels, benches, crop clusters, warm windows, smoke/fire accents, and inhabitants in authored activity pockets. Give physical foreground objects colliders and reject placements intersecting the enemy corridor.

- [ ] **Step 5: Build non-interactive horizon scenery**

Create low-detail terrain skirts, layered hills, forest masses, ruin silhouettes, and one or two distant landmarks beyond the playable rectangle. Put them under `Distant Landscape`, remove colliders, use restrained fog/value falloff, and exclude the hierarchy from minimap culling.

- [ ] **Step 6: Integrated verification and commit**

Run the complete EditMode suite, recompile cleanly, and perform Play Mode checks for collision, path traversal, camera occlusion, all combat inputs, waves, boss HUD, minimap, and animation continuity. Capture final 1280×720 views from at least three gameplay positions. Review the full branch diff against `main`, fix regressions, then commit with `feat: polish Greenward living world` and push the branch.
