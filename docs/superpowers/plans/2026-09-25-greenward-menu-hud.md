# Greenward Menu and HUD Implementation Plan

> For agentic workers: REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Replace the current OnGUI debug menu/HUD with a responsive UI Toolkit flow supporting Main Menu, Stage Select, Skill Tree, Gameplay HUD, and Result screens while preserving Greenward gameplay rules.

**Architecture:** Keep TDVerticalSliceBootstrap as the gameplay authority. Add small plain C# definitions for stages and session-local skill nodes, then let one TDVerticalSliceHUD controller bind UI Toolkit elements to existing bootstrap methods. UXML/USS provide layout and visual tokens; the runtime controller owns navigation and presentation only.

**Tech Stack:** Unity 6000.6.0f1, UI Toolkit, UXML/USS, existing Input System, Unity CLI/Pipeline, Unity Test Framework EditMode tests.

## Global Constraints

- Use UnityProject/ as the source of truth.
- Use UI Toolkit; do not add a new UI package.
- Use PanelSettings with Scale With Screen Size and a 1280x720 reference resolution.
- Preserve StartSoloStages, StartNextWave, ReturnToMenu, TogglePlacementMode, and placement validation.
- Keep stage and skill data separate from UI presentation.
- Show three stages: Greenward selectable; two future stages visible but locked.
- Use session-local Warden, Arcana, and Bastion skill branches.
- Do not invent skill gameplay effects; defer permanent persistence until a versioned save envelope exists.
- Preserve the three existing uncommitted project-settings files and do not stage them.
- Do not use pill-shaped badges or decorative UI that obscures the battlefield.
- Before editing a Unity scene or asset through the Editor, run unity status and use the connected Editor/Pipeline when available.
- Every Unity CLI command targets E:\TD-Annihilation-Unity-Git\UnityProject explicitly.

---

### Task 1: Add testable stage and skill-tree definitions

**Files:**
- Create: UnityProject/Assets/Scripts/TD/TDMenuDefinitions.cs
- Test: UnityProject/Assets/Tests/EditMode/TDMenuDefinitionsTests.cs

**Interfaces:**
- TDStageCatalog.CreateDefault() returns IReadOnlyList<TDStageDefinition>.
- TDStageDefinition exposes Id, DisplayName, Description, and IsSelectable.
- TDMenuScreen enum contains MainMenu, StageSelect, SkillTree, Settings, Gameplay, and Result.
- TDSkillTreeCatalog.CreateDefault() returns IReadOnlyList<TDSkillNodeDefinition>.
- TDSkillNodeDefinition exposes Id, Branch, DisplayName, Description, and PrerequisiteId.
- TDSkillTreeState exposes IsUnlocked(string), CanUnlock(string, IReadOnlyList<TDSkillNodeDefinition>), and TryUnlock(string, IReadOnlyList<TDSkillNodeDefinition>).

- [ ] Step 1: Write the failing EditMode tests.

Create tests for:
1. The default catalog contains exactly three stages, Greenward is the first selectable stage, and the other two are locked.
2. Warden Edge cannot unlock before Warden Grit, then both unlock in order.
3. The default skill catalog contains Warden, Arcana, and Bastion branches.

Use this exact test shape:

~~~csharp
[Test]
public void SkillTreeRequiresPrerequisiteBeforeUnlock()
{
    var nodes = TDSkillTreeCatalog.CreateDefault();
    var state = new TDSkillTreeState();

    Assert.That(state.TryUnlock("warden-edge", nodes), Is.False);
    Assert.That(state.TryUnlock("warden-grit", nodes), Is.True);
    Assert.That(state.TryUnlock("warden-edge", nodes), Is.True);
}
~~~

- [ ] Step 2: Run the focused tests and verify RED.

Run:

~~~powershell
unity test E:\TD-Annihilation-Unity-Git\UnityProject --editor-version 6000.6.0f1 --mode EditMode --filter TDMenuDefinitionsTests --report-format junit --output E:\TD-Annihilation-Unity-Git\td-menu-tests-red.xml --timeout 600
~~~

Expected: compile failure because the definitions do not exist yet.

- [ ] Step 3: Implement the smallest data contract.

Create TDMenuDefinitions.cs with:
- TDMenuScreen enum containing MainMenu, StageSelect, SkillTree, Settings, Gameplay, and Result.
- TDStageDefinition constructor taking id, display name, description, and selectable flag.
- TDStageCatalog.CreateDefault() returning greenward/Greenward selectable, stage-two/The Ashen Pass locked, and stage-three/The Sunken Keep locked.
- TDSkillBranch enum with Warden, Arcana, Bastion.
- TDSkillNodeDefinition constructor taking id, branch, display name, description, and prerequisite id.
- TDSkillTreeCatalog.CreateDefault() returning two nodes per branch: a root node with no prerequisite and a second node requiring the root.
- TDSkillTreeState backed by a private HashSet<string>; unknown ids, null definitions, already-unlocked nodes, and missing prerequisites return false.

- [ ] Step 4: Run the focused tests and verify GREEN.

Run the same command with output file td-menu-tests-green.xml. Expected: exit code 0 and all three tests pass.

- [ ] Step 5: Commit.

~~~powershell
git add UnityProject/Assets/Scripts/TD/TDMenuDefinitions.cs UnityProject/Assets/Tests/EditMode/TDMenuDefinitionsTests.cs
git commit -m "feat: add stage and skill tree menu definitions"
~~~

### Task 2: Create UI Toolkit markup and visual tokens

**Files:**
- Create: UnityProject/Assets/Resources/TDAnnihilation/UI/GreenwardRoot.uxml
- Create: UnityProject/Assets/Resources/TDAnnihilation/UI/Greenward.uss

**Interfaces:**
- Root contains screen containers named main-menu-screen, stage-select-screen, skill-tree-screen, settings-screen, gameplay-screen, and result-screen.
- Root contains stable controls named play-button, skill-tree-button, settings-button, quit-button, stage-greenward-button, stage-select-back-button, skill-tree-back-button, settings-back-button, placement-button, wave-button, result-replay-button, result-stage-select-button, and result-menu-button.
- Root contains labels named stage-value, gold-value, lives-value, wave-value, defeated-value, phase-value, status-value, result-title, result-copy, skill-details-title, skill-details-copy, and container skill-graph.

- [ ] Step 1: Create UXML with the approved screen hierarchy.

Create one root VisualElement with:
- Main Menu panel: title, subtitle, Play, Skill Tree, Settings, Quit.
- Stage Select panel: three rectangular stage cards, Greenward enabled, two future cards disabled, Back.
- Skill Tree panel: skill-graph container, details title/copy, Back.
- Settings shell panel: explanatory copy and Back.
- Gameplay overlay: top-left resources, top-center wave, status text, reserved lower-left minimap frame, bottom action panel, right-side build panel with Build Tower and Start Next Wave.
- Result panel: title, copy, Replay, Stage Select, Main Menu.

Use UI Toolkit buttons and labels only; keep all gameplay and skill behavior in the runtime controller.

- [ ] Step 2: Create USS with the approved visual language.

Define:
- Deep forest green/charcoal translucent panels.
- Parchment/gold text and borders.
- Violet reserved for Arcana state, restrained red for danger.
- Rectangular controls with zero radius, visible hover/focus/pressed/disabled states.
- Main Menu centered panel, horizontal stage list, three skill columns, and gameplay anchors at top-left, top-center, lower-left, bottom-center, and right.
- Screen hidden by default and visible only with class is-visible.
- 1280x720-friendly dimensions with flex growth and no fixed full-screen obstruction.

The required classes are screen, is-visible, menu-panel, screen-panel, primary-button, secondary-button, stage-card, gameplay-screen, hud-panel, hud-top-left, hud-top-center, minimap-frame, action-panel, build-panel, skill-graph, skill-branch, skill-node, is-unlocked, is-available, and is-selected.

- [ ] Step 3: Refresh and compile.

Run:

~~~powershell
unity recompile --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json
~~~

Expected: success true, no new compiler errors, and both assets imported below Assets/Resources/TDAnnihilation/UI.

- [ ] Step 4: Commit.

~~~powershell
git add UnityProject/Assets/Resources/TDAnnihilation/UI/GreenwardRoot.uxml UnityProject/Assets/Resources/TDAnnihilation/UI/Greenward.uss
git commit -m "feat: add Greenward UI Toolkit layout"
~~~

### Task 3: Replace OnGUI with the UI Toolkit runtime controller

**Files:**
- Create: UnityProject/Assets/Scripts/TD/TDVerticalSliceHUD.cs
- Modify: UnityProject/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs around the existing TDVerticalSliceHUD class and public properties

**Interfaces:**
- TDVerticalSliceHUD exposes ShowMainMenu(), ShowStageSelect(), ShowSkillTree(), ShowSettings(), ShowGameplay(), and ShowResult().
- TDVerticalSliceHUD consumes TDVerticalSliceBootstrap, TDStageCatalog.CreateDefault(), and TDSkillTreeCatalog.CreateDefault().
- TDVerticalSliceBootstrap adds read-only CurrentStageName and IsPlacementMode properties.

- [ ] Step 1: Write the wiring contract before coding.

The controller must implement these transitions:
- Initial state: Main Menu visible while the game phase is MainMenu.
- Main Menu Play: Stage Select.
- Greenward selection: game.StartSoloStages(), then Gameplay HUD.
- Build phase: build-panel visible.
- Wave phase: build-panel hidden.
- Build Tower: game.TogglePlacementMode().
- Start Next Wave: game.StartNextWave().
- Victory or Defeat: Result screen.
- Replay: game.ReturnToMenu(), game.StartSoloStages(), then Gameplay HUD.
- Stage Select result action: game.ReturnToMenu(), then Stage Select.
- Main Menu result action: game.ReturnToMenu(), then Main Menu.
- Skill node selection: update details and state classes; unlock only when prerequisites permit; do not change combat stats.

- [ ] Step 2: Implement TDVerticalSliceHUD.

Use a UIDocument on the bootstrap GameObject. In Start:
1. Get TDVerticalSliceBootstrap from the same GameObject.
2. Get or add UIDocument.
3. Load PanelSettings from Resources/TDAnnihilation/UI/GreenwardPanelSettings.
4. Load GreenwardRoot from Resources/TDAnnihilation/UI/GreenwardRoot.
5. Load Greenward.uss and add it to rootVisualElement.
6. Query all named elements.
7. Register button callbacks once.
8. Build the skill graph from TDSkillTreeCatalog.
9. Show Main Menu.

Refresh every frame:
- Write state.gold, state.lives, state.defeated, CurrentWave, CurrentStageName, and Phase to labels.
- Set build-panel display to Flex only in Build, None otherwise.
- Detect Victory/Defeat and open Result once.
- Never write directly to TDResourceState and never use OnGUI.

Skill graph rendering must create one branch column per TDSkillBranch, add a button per node, and apply is-unlocked, is-available, or default locked classes from TDSkillTreeState. Clicking a node updates details; clicking an available node calls TryUnlock and rebuilds the graph. Use prerequisite data from TDSkillNodeDefinition rather than hard-coded screen coordinates.

- [ ] Step 3: Move the old HUD class out of TDVerticalSliceBootstrap.cs.

Delete only the old TDVerticalSliceHUD class and its OnGUI methods from the end of TDVerticalSliceBootstrap.cs. Keep the type namespace as TDAnnihilation.TDVerticalSliceHUD so existing scene references remain valid. Do not change enemy, tower, projectile, wave, or placement code in this step.

- [ ] Step 4: Add the two bootstrap properties.

Add beside the existing Phase property:

~~~csharp
public string CurrentStageName => "GREENWARD";
public bool IsPlacementMode => placementMode;
~~~

- [ ] Step 5: Recompile and run tests.

~~~powershell
unity recompile --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json
unity test E:\TD-Annihilation-Unity-Git\UnityProject --editor-version 6000.6.0f1 --mode EditMode --filter TDMenuDefinitionsTests --report-format junit --output E:\TD-Annihilation-Unity-Git\td-menu-tests-runtime.xml --timeout 600
~~~

Expected: no duplicate type error, successful compilation, and all menu-definition tests pass.

- [ ] Step 6: Commit.

~~~powershell
git add UnityProject/Assets/Scripts/TD/TDVerticalSliceHUD.cs UnityProject/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs
git commit -m "feat: bind Greenward UI Toolkit HUD to game flow"
~~~

### Task 4: Create and assign PanelSettings through Unity CLI

**Files:**
- Create: UnityProject/Assets/Editor/TDGreenwardUIAuthoring.cs
- Create through Unity Editor: UnityProject/Assets/Resources/TDAnnihilation/UI/GreenwardPanelSettings.asset and its meta
- Modify through Unity Editor if needed: UnityProject/Assets/Scenes/Greenward.unity

**Interfaces:**
- Adds the menu command TD Annihilation/UI/Ensure Greenward UI.
- Ensures PanelSettings at the exact resource path with scaleMode ScaleWithScreenSize, referenceResolution (1280, 720), and match 0.5.
- Ensures the active Greenward bootstrap GameObject has one UIDocument assigned to GreenwardRoot and GreenwardPanelSettings.

- [ ] Step 1: Check the Editor before authoring.

Run:

~~~powershell
unity status --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json
unity command --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json
~~~

If a ready Editor is available, drive it. If no Editor is reachable after ruling out Safe Mode, use the documented Unity CLI batch fallback and record it in evidence.

- [ ] Step 2: Implement an idempotent authoring command.

TDGreenwardUIAuthoring must:
1. Create Assets/Resources/TDAnnihilation/UI if needed.
2. Load or create GreenwardPanelSettings.asset.
3. Set ScaleWithScreenSize, reference resolution 1280x720, and match 0.5.
4. Find the active TDVerticalSliceBootstrap.
5. Get or add UIDocument without duplicating it.
6. Assign the exact UXML and PanelSettings assets.
7. Save the active scene and AssetDatabase.
8. Be safe to run repeatedly.

- [ ] Step 3: Invoke the command through the connected Unity CLI/Pipeline.

Discover the exact menu-item command name first. If the Editor exposes editor_execute_menu_item, run:

~~~powershell
unity command menu -- '{"path":"TD Annihilation/UI/Ensure Greenward UI"}' --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json
~~~

If the command name differs, use the exposed equivalent rather than guessing.

Expected: one PanelSettings asset, one UIDocument, and GreenwardRoot assigned in Greenward.unity.

- [ ] Step 4: Inspect the authored scene.

Use the discovered hierarchy/asset inspection command and verify:
- one TDVerticalSliceBootstrap;
- one TDVerticalSliceHUD;
- one UIDocument;
- source asset GreenwardRoot;
- panel settings asset GreenwardPanelSettings.

- [ ] Step 5: Commit only intended authored UI files.

~~~powershell
git add UnityProject/Assets/Editor/TDGreenwardUIAuthoring.cs UnityProject/Assets/Resources/TDAnnihilation/UI/GreenwardPanelSettings.asset UnityProject/Assets/Resources/TDAnnihilation/UI/*.meta UnityProject/Assets/Scenes/Greenward.unity
git commit -m "feat: configure Greenward UI Toolkit panel"
~~~

Do not stage the three pre-existing modified project/settings files.

### Task 5: Validate the complete flow and record evidence

**Files:**
- Modify: PROJECT_STATUS.md
- Create: docs/evidence/unity-greenward-menu-hud.md

**Interfaces:**
- Evidence records exact Unity CLI commands, test results, active scene, resolution, and known limitations.
- PROJECT_STATUS.md records UI Toolkit navigation/HUD without claiming permanent skill progression or unimplemented stages.

- [ ] Step 1: Run the complete EditMode suite.

~~~powershell
unity test E:\TD-Annihilation-Unity-Git\UnityProject --editor-version 6000.6.0f1 --mode EditMode --report-format junit --output E:\TD-Annihilation-Unity-Git\td-editmode-menu-hud.xml --timeout 600
~~~

Expected: exit code 0. If exit code 8, inspect the JUnit report and fix the failing test before continuing.

- [ ] Step 2: Enter Play Mode and inspect the Unity Console.

Use the connected Editor commands discovered with unity command to enter Play Mode and read the Console. Expected: no new compile errors, missing UXML/USS/PanelSettings errors, or duplicate legacy OnGUI HUD.

- [ ] Step 3: Verify the actual 1280x720 Game view.

Verify:
1. Main Menu shows Play, Skill Tree, Settings, and Quit.
2. Play opens Stage Select.
3. Greenward enters Build and shows resources/wave.
4. Two future stages are visible and disabled.
5. Skill Tree shows Warden/Arcana/Bastion and prerequisite state changes.
6. Build Tower and Start Next Wave invoke existing methods.
7. Build controls hide during Wave.
8. Victory/Defeat opens Result; each Result button returns correctly.
9. HUD panels do not cover the route, hero, towers, or enemies.

- [ ] Step 4: Record evidence and update status.

Create docs/evidence/unity-greenward-menu-hud.md with date, branch, exact commands, test result, Play Mode observations, and explicit limitations: future stages have no content, skill nodes have no gameplay effects, persistence/settings migration is deferred, and the minimap frame is reserved if the minimap system is not integrated.

Update PROJECT_STATUS.md with a short UI Toolkit status entry.

- [ ] Step 5: Review and commit documentation.

~~~powershell
git diff --check
git status --short
git diff --stat HEAD~1..HEAD
git add PROJECT_STATUS.md docs/evidence/unity-greenward-menu-hud.md
git commit -m "docs: record Greenward menu and HUD verification"
~~~

Expected: no whitespace errors; only intended UI files/docs changed; the three pre-existing project/settings modifications remain unstaged.
