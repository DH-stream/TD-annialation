# Greenward Steam UI and Placement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the placeholder-like Greenward menu/HUD and fix tower placement feedback without changing the existing game loop or combat rules.

**Architecture:** Keep UI Toolkit and the existing `TDVerticalSliceHUD` controller, replacing only its UXML/USS contract and element bindings. Keep placement ownership in `TDVerticalSliceBootstrap`; use a small compositional `TDTowerPlacementAnimation` component for the one-shot tower arrival effect. Use physics only for explicit blockers and preserve the authored grass scene as-is.

**Tech Stack:** Unity UI Toolkit, Unity Input System, Unity ParticleSystem, existing Greenward runtime/editor scripts, EditMode tests.

**Specs:**
- `docs/superpowers/specs/2026-09-25-greenward-steam-ui-ux.md`
- `docs/superpowers/specs/2026-09-25-greenward-placement-feedback.md`

## Global Constraints

- Do not add packages or external art assets.
- Preserve existing serialized field names and existing menu callback names.
- Preserve combat, wave, cost and skill-tree behavior.
- Do not disable authored grass in gameplay.
- Do not edit unrelated user-modified ProjectSettings or render settings files.

## Review Focus

- Existing callbacks still resolve after the UXML rewrite — verify all named buttons/elements are present.
- HUD stays readable at 1280x720 and does not cover the battlefield — parse UXML and inspect USS dimensions.
- Meadow placement ignores scenery colliders while road/castle remain invalid — add focused placement-rule checks.
- Tower animation does not delay combat initialization or leak particles — compile and inspect component lifecycle.
- Grass is not duplicated or disabled — inspect scene and runtime code paths.

### Task 1: Replace the UI composition

**Files:**
- Modify: `UnityProject/Assets/Resources/TDAnnihilation/UI/GreenwardRoot.uxml`
- Modify: `UnityProject/Assets/Resources/TDAnnihilation/UI/Greenward.uss`
- Modify: `UnityProject/Assets/Scripts/TD/TDVerticalSliceHUD.cs`

- [ ] Replace the centered menu boxes with the authored left-rail menu, stage detail layout, skill-tree layout and result layout while retaining all existing named controls.
- [ ] Replace the giant gameplay panels with compact resource, wave, minimap and build-deck elements.
- [ ] Update HUD bindings for the new elements and add a short screen-entry transition using existing UI Toolkit scheduling/classes.
- [ ] Parse the UXML and assert all runtime `Q<>` names still exist.
- [ ] Commit: `feat: restyle Greenward menu and gameplay HUD`

### Task 2: Fix placement validation and ground sampling

**Files:**
- Modify: `UnityProject/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs`
- Modify: `UnityProject/Assets/Scripts/TD/GreenwardWorldLayout.cs` only if a small reusable placement predicate is needed
- Test: `UnityProject/Assets/Tests/EditMode/GreenwardGameplayPresentationTests.cs`

- [ ] Replace the infinite `y=0` cursor plane with an authored-ground raycast and keep the height-function fallback for scripted preview calls.
- [ ] Change collider validation so only placed towers and explicit gameplay blockers can reject a meadow site; grass and decoration remain passable.
- [ ] Preserve forbidden surface-region checks and the existing tower-spacing rule.
- [ ] Add focused checks for meadow/road/castle decisions and compile the runtime assembly.
- [ ] Commit: `fix: make Greenward tower placement respect buildable meadow`

### Task 3: Add tower rise and dust feedback

**Files:**
- Create: `UnityProject/Assets/Scripts/TD/TDTowerPlacementAnimation.cs`
- Modify: `UnityProject/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs`

- [ ] Add a one-shot component that animates local tower height/position with an overshoot-and-settle curve and restores the final transform.
- [ ] Emit a short auto-destroying ParticleSystem burst at the base using existing runtime particle material helpers or a local equivalent.
- [ ] Attach the component only for towers created by `SpawnTower`; existing tower targeting remains active.
- [ ] Verify the source has no unbounded particle/object lifetime and compile both runtime and editor assemblies.
- [ ] Commit: `feat: animate tower placement arrival`

### Task 4: Verify authored grass and final integration

**Files:**
- Modify: `docs/evidence/unity-greenward-menu-hud.md`

- [ ] Confirm no gameplay code disables `Grass Meadow Batch` or rebuilds duplicate grass.
- [ ] Run `dotnet build` for runtime, editor and EditMode test projects.
- [ ] Run `unity recompile --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json`.
- [ ] Attempt the low-risk Unity CLI status/console probe and record if Pipeline remains unreachable.
- [ ] Update evidence with the exact validation results and any remaining manual Play Mode check.
- [ ] Commit: `docs: record Greenward UI and placement verification`
