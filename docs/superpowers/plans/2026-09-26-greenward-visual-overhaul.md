# Greenward HUD and Lighting Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the minimap placeholder, refine the combat HUD, add player-configurable attack keys, and improve Greenward lighting without changing gameplay rules or removing grass.

**Architecture:** Keep the current UI Toolkit HUD and bind it to `TDVerticalSliceBootstrap` data. A small PlayerPrefs-backed attack binding model feeds both input and the visible key labels; a minimap view projects the existing route and live world entities into the HUD. Inspect and capture the existing URP setup first, then make only evidence-backed lighting adjustments.

**Tech Stack:** Unity 6.6.0f1, URP 17.6.0, UI Toolkit, Input System 1.20, Unity Test Framework EditMode, Unity CLI/Pipeline.

## Global Constraints

- Source of truth: `E:/TD-Annihilation-Unity-Git/UnityProject`; do not edit the separate `E:/tower defense game` prototype.
- Work on `codex/lighting-overhaul`; preserve all pre-existing working-tree changes and stage only task-specific files.
- Extend the current UI Toolkit HUD; add no UI package and do not migrate render pipelines.
- Attack keys default to the current F/Q/E bindings and can be rebound in Settings; unrelated movement/gameplay controls remain unchanged.
- Minimap geometry and markers come from the actual Greenward route and live gameplay state, not decorative mock art.
- Preserve the authored Greenward grass, environment, routes, towers, enemies, combat, and balance.
- Inspect/reuse current URP volumes, renderer features, lights, and probes before changing them; do not duplicate existing effects.
- Before/after lighting captures use the same scene, camera, resolution, and gameplay state.
- Target HUD checks: 1280x720 and a wider desktop aspect ratio.

---

## Track A — HUD, minimap, and remappable attack keys

### Task 1: Add tested attack-key binding state

**Files:**
- Create `UnityProject/Assets/Scripts/TD/TDAttackKeyBindings.cs`
- Create `UnityProject/Assets/Tests/EditMode/TDAttackKeyBindingsTests.cs`
- Modify `UnityProject/Assets/Tests/EditMode/TDAnnihilation.EditModeTests.asmdef` to reference the already-installed `Unity.InputSystem` assembly

**Interfaces:** `GetKey(TDAttackType)`, `TrySetKey(TDAttackType, Key)`, `Save()`, and `static Load()`; defaults are Light=F, Heavy=Q, Mega=E.

- [ ] Write EditMode tests for defaults, a successful remap, rejecting duplicate attack keys, rejecting `Key.None`/Escape, and falling back to defaults for malformed stored values.
- [ ] Run the focused test filter and verify it fails because the binding model is absent.
- [ ] Implement the small model using `UnityEngine.InputSystem.Key` and PlayerPrefs; store three integer enum values under one namespaced key, validate on load, and never change unrelated actions.
- [ ] Run the focused tests; clear only the test-owned PlayerPrefs key during setup/teardown.
- [ ] Commit only the binding model, focused test, generated metadata, and test-assembly reference.

### Task 2: Expose the existing Greenward data and test minimap projection

**Files:**
- Modify `UnityProject/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs`
- Create `UnityProject/Assets/Scripts/TD/TDGreenwardMinimapMath.cs`
- Create `UnityProject/Assets/Tests/EditMode/TDGreenwardMinimapMathTests.cs`

**Interfaces:** Read-only access to the current route, build bounds, castle target, hero transform, active enemies, and placed tower transforms. `TDGreenwardMinimapMath.WorldToMap(Vector3 world, Bounds bounds, Rect mapRect)` maps world X/Z into the framed map rectangle with Z inverted for screen coordinates.

- [ ] Add tests proving world-bound corners map to the expected map corners, the center maps to the center, and changing only world Y does not change the marker position.
- [ ] Run the focused tests and verify the helper is missing.
- [ ] Add only read-only bootstrap accessors over the existing runtime state; do not copy or reconstruct a second route/entity list.
- [ ] Implement the coordinate conversion with non-zero minimum bounds and guard zero-width/zero-depth bounds by returning the map center.
- [ ] Run focused tests and verify the scene's Greenward route/bounds are used by the minimap data source.
- [ ] Commit only bootstrap accessors, helper, and tests.

### Task 3: Draw the real Greenward minimap

**Files:**
- Create `UnityProject/Assets/Scripts/TD/TDGreenwardMinimap.cs`
- Modify `UnityProject/Assets/Resources/TDAnnihilation/UI/GreenwardRoot.uxml`
- Modify `UnityProject/Assets/Resources/TDAnnihilation/UI/Greenward.uss`
- Modify `UnityProject/Assets/Scripts/TD/TDVerticalSliceHUD.cs`

**Interfaces:** `TDGreenwardMinimap.Bind(TDVerticalSliceBootstrap)` and `Refresh()`; draw using the shared projection helper and existing runtime route/entities.

- [ ] Add a named map drawing element inside the current lower-left minimap frame and retain its existing responsive anchor.
- [ ] Implement one custom UI Toolkit visual element that draws the actual route, goal, hero, tower markers, and live enemies from the bound bootstrap data; redraw only when refreshed and use no additional camera/render texture.
- [ ] Bind it from the existing HUD startup path and refresh marker positions from the existing HUD update loop.
- [ ] Add/adjust EditMode checks for route projection and an empty/missing route so the frame remains valid and no fake route is drawn.
- [ ] Recompile in the connected Editor and inspect the live minimap against Greenward's authored route.
- [ ] Commit only the minimap, HUD wiring, UXML/USS, and relevant tests.

### Task 4: Add radial cooldown presentation and Settings rebinding

**Files:**
- Create `UnityProject/Assets/Scripts/TD/TDHudCooldownRing.cs`
- Modify `UnityProject/Assets/Resources/TDAnnihilation/UI/GreenwardRoot.uxml`
- Modify `UnityProject/Assets/Resources/TDAnnihilation/UI/Greenward.uss`
- Modify `UnityProject/Assets/Scripts/TD/TDVerticalSliceHUD.cs`

**Interfaces:** Each ring accepts normalized remaining cooldown and ready/disabled state; the HUD reads the three keys from `TDAttackKeyBindings` and updates the matching UXML key labels.

- [ ] Add a pure cooldown fraction test: remaining 0 gives ready/full, remaining equal to duration gives zero progress, and division by zero duration remains finite.
- [ ] Run the focused EditMode test and verify it fails before adding the presentation helper.
- [ ] Replace each linear cooldown track with a circular sweep around its existing attack icon, numeric countdown, and clear READY/WAIT/PAUSED labels; use UI Toolkit drawing and existing icon assets.
- [ ] Add one Settings row/button per attack; click starts listening, Escape cancels, a valid new key saves, a duplicate attack key is rejected with visible feedback, and displayed HUD key hints update immediately.
- [ ] Route attack key polling through the binding model while leaving movement, mouse attack, build, wave, and pause behavior unchanged.
- [ ] Recompile; test binding persistence/default fallback and inspect ready, cooldown, paused, and duplicate-binding states in Play Mode.
- [ ] Commit only the ring, UXML/USS/HUD wiring, and relevant tests.

### Task 5: Restyle top-right status and verify HUD at target sizes

**Files:**
- Modify `UnityProject/Assets/Resources/TDAnnihilation/UI/GreenwardRoot.uxml`
- Modify `UnityProject/Assets/Resources/TDAnnihilation/UI/Greenward.uss`
- Modify `UnityProject/Assets/Scripts/TD/TDVerticalSliceHUD.cs` only if a label binding is missing
- Create/update `docs/evidence/unity-greenward-visual-overhaul.md`

- [ ] Restyle the top-right stage/defeated display as a compact framed plaque in the supplied visual direction; preserve current values and keep resource indicators unchanged.
- [ ] Run all EditMode tests and recompile through Unity CLI against `E:/TD-Annihilation-Unity-Git/UnityProject`.
- [ ] Inspect Game view at 1280x720 and a wide desktop size; verify no overlap with the map, action panel, or battle view.
- [ ] In Play Mode, verify live route/entities, key remapping/persistence, radial cooldown progression, and unchanged Build/Wave actions.
- [ ] Record exact commands, tests, view sizes, and limitations in the evidence note; do not claim unrun checks.
- [ ] Commit only the top-right UI changes and evidence note.

---

## Track B — Lighting and visual depth

### Task 6: Capture baseline and inspect active URP lighting

**Files:**
- Create baseline images under `docs/evidence/greenward-lighting/before/`
- Create/update `docs/evidence/unity-greenward-visual-overhaul.md`

- [ ] Confirm `unity status` points to the target project and `Greenward` is the active scene; inspect the active PC URP asset, renderer features, scene light values, volume overrides, reflection probe, and existing AO.
- [ ] Enter the stable menu/build view without starting a wave; capture a wide and a closer Game view at fixed resolutions/camera state and save the images under the evidence folder.
- [ ] Record current exact settings and note that authored scene grass is present before considering visual changes.
- [ ] Review the captures and select only the one or two settings that demonstrably need tuning; if existing setup already meets a criterion, leave it unchanged.

### Task 7: Tune only demonstrated lighting issues and compare

**Files:**
- Modify `UnityProject/Assets/Scripts/TD/GreenwardLightingBuilder.cs` only if runtime-authored settings are the source of the issue
- Modify the specific active scene/profile/renderer asset through Unity CLI only if inspection proves an asset setting is the source
- Create matching after-images under `docs/evidence/greenward-lighting/after/`
- Update `docs/evidence/unity-greenward-visual-overhaul.md`

- [ ] Apply the minimum adjustment to key/fill balance, shadow/AO grounding, tonemapping/bloom, subtle distance atmosphere, or probe settings; do not duplicate existing AO, lights, or volumes.
- [ ] Re-capture with identical scene/camera/resolution/state and compare wide and close views side by side.
- [ ] Verify grass visibility, color readability, shadow stability, unchanged gameplay state, and no material frame-time regression in the same Game view.
- [ ] Revert/retune any setting that washes out the palette, creates shadow artifacts, obscures grass, or adds unnecessary cost; preserve the prior setup where no clear gain is visible.
- [ ] Run Unity compilation and the complete relevant EditMode suite; record exact outcomes and settings in the evidence note.
- [ ] Commit only verified lighting/profile changes and the evidence artifacts.

## Final review

- [ ] `git diff --check`; review the staged diff and confirm all earlier user changes remain accounted for and unstaged unless they were specifically part of this plan.
- [ ] Confirm no changes to route geometry, terrain/grass placement, combat rules, enemy/tower behavior, or attack balance.
- [ ] Report branch, commits, tests actually run, screenshots, and any remaining limitations.
