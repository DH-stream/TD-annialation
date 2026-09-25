# Greenward Menu and HUD Design Specification

## Objective

Replace the current debug-oriented `OnGUI` menu/HUD with a real UI Toolkit presentation layer that supports the current Greenward vertical slice and future stages and skill-tree progression without requiring a UI rewrite.

## Product flow

The navigation flow is:

`Main Menu -> Stage Select -> Gameplay -> Result`

The skill tree is available from the main menu and stage-select context. Gameplay remains focused on the battlefield; it does not open the full skill tree during a wave in this first pass.

### Main Menu

- Title: `TD ANNIHILATION`.
- Primary action: `PLAY`.
- Secondary actions: `SKILL TREE`, `SETTINGS`, and `QUIT`.
- `PLAY` opens Stage Select instead of starting Greenward directly.
- Settings is a navigable shell only until the existing settings/save contract is migrated.
- Quit is disabled or hidden in editor-only contexts and quits the standalone player when supported.

### Stage Select

- Show three stage slots to match the migration inventory.
- Greenward is the only active/selectable stage in this pass.
- Future stages remain visible as locked, clearly labelled, non-interactive entries; no fake completion data is shown.
- Selecting Greenward starts the existing solo flow and preserves the current reset behavior for gold, lives, defeated count, and wave.
- Back returns to Main Menu.

### Skill Tree

- Use three expandable branches: `WARDEN`, `ARCANA`, and `BASTION`.
- Warden covers hero combat and survival, Arcana covers spells and energy attacks, and Bastion covers towers and defense.
- The visual model supports node states: locked, available, selected, and unlocked.
- Prerequisite connectors are visible and are data-driven rather than encoded in USS or screen coordinates.
- The first implementation owns session-local UI state only. Permanent progression is explicitly deferred until a versioned save envelope is designed.
- The screen must not invent gameplay effects that do not exist in the current simulation.

### Gameplay HUD

- Top-left: gold, lives, and current stage name.
- Top-center: current wave and wave state.
- Top-right: compact objective/status text.
- Lower-left: presentation-only minimap reserved for the planned route, gate, hero, enemies, towers, and boss marker.
- Bottom-center: hero attack/spell action area with cooldown and readiness states.
- Right side: build controls during the Build phase only, including tower cost and next-wave action.
- Top-center boss health remains available for boss waves and is hidden otherwise.
- Victory/Defeat use the same visual language and provide Result actions for replay, stage select, and main menu.

## Visual language

- Warm fantasy palette: deep forest green and charcoal panels, parchment/gold text accents, violet arcane accents, and restrained red for danger.
- Panels use straight edges, layered borders, and subtle stone/aged-metal framing. Avoid pill-shaped controls, badge clusters, and decorative UI that competes with the battlefield.
- Primary actions use clear rectangular buttons with hover, focus, pressed, disabled, and selected states.
- UI remains readable over the low-poly Greenward environment without obscuring the route, hero, towers, or enemies.
- Use existing project resources and Unity defaults where possible. Do not add a package solely for UI decoration.

## UI Toolkit architecture

- Create a shared `PanelSettings` asset using Scale With Screen Size with a 1280x720 reference resolution.
- Use one shared USS token layer for palette, typography, spacing, borders, and state colors.
- Keep screen markup in UXML with a shared root and separate screen containers for Main Menu, Stage Select, Skill Tree, Gameplay HUD, and Result.
- Use a focused runtime controller to switch screens and bind current game state; screen views must not own wave, health, tower, or save logic.
- Keep stage definitions, skill definitions, and prerequisites in data-facing C# types so new stages/nodes can be added without duplicating UI code.
- Preserve current game-flow methods as the first integration seam: `StartSoloStages`, `StartNextWave`, `ReturnToMenu`, and placement-mode actions.
- Remove the current HUD's `OnGUI` rendering only after the UI Toolkit path is active and verified, to avoid a period with no visible controls.

## Scope

### Included

- UI Toolkit project assets and runtime wiring.
- Main Menu, Stage Select, Skill Tree shell, Gameplay HUD, and Result screens.
- Responsive desktop layout and keyboard/mouse navigation.
- Greenward integration with the existing game-flow state.
- Locked future-stage presentation without fake functionality.
- Session-local skill-tree selection states and prerequisite visualization.

### Deferred

- Permanent skill-tree persistence and migration from Babylon browser-local state.
- Actual skill effects that are not present in the current Unity simulation.
- Settings persistence and movement remapping migration.
- Additional stage content, stage-specific art, multiplayer, and online services.
- Full minimap implementation if the existing combat-vista minimap has not yet been integrated.

## Acceptance criteria

1. Unity opens the UI Toolkit root without compile errors or missing UXML/USS references.
2. Main Menu reaches Stage Select; Greenward starts the existing solo loop; Back actions return correctly.
3. Stage Select shows one active Greenward entry and two locked future entries without fake progress.
4. Skill Tree shows Warden, Arcana, and Bastion with visible prerequisite/state changes that remain session-local.
5. Gameplay HUD shows the current resources and wave state without changing gameplay rules or hiding the battlefield.
6. Build, next-wave, victory, defeat, and return-to-menu actions still call the existing game-flow behavior.
7. The layout remains usable at the target 1280x720 reference resolution and scales without overlapping primary controls.
8. No new UI package is added, and the existing uncommitted project settings changes remain untouched.

