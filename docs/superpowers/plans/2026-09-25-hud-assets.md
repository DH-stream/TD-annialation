# Greenward HUD Assets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the text-only gameplay HUD zones with the six supplied fantasy HUD assets while preserving current Build, attack and resource behavior.

**Architecture:** Import the PNGs into the existing Resources UI folder, expose them as UI Toolkit `Image` elements, and place value labels over the empty fields. `TDVerticalSliceHUD` remains the single binding point: gold uses `TDResourceState.gold`, health uses the existing lives fallback, mana displays a clearly marked static reserve until a mana system exists, and Heavy/Mega remain disabled action cards.

**Tech Stack:** Unity 6 UI Toolkit, UXML, USS, C#, Unity CLI, existing `TDVerticalSliceHUD` and `TDVerticalSliceBootstrap`.

## Global Constraints

- Use only the six user-provided free PNG assets; add no paid/external dependency.
- Keep the HUD rectangular and authored; do not add decorative pills, badges or chips.
- Preserve existing Build/radial selection, attack input, menu camera and tower placement behavior.
- Do not modify the user's existing ProjectSettings, URP settings or solution changes.
- If no mana or hero-health source exists, use the explicit UI fallbacks from the approved design rather than adding gameplay systems.

### Task 1: Import and name the HUD assets

**Files:**
- Create: `UnityProject/Assets/Resources/TDAnnihilation/UI/HUD/Attack.png`
- Create: `UnityProject/Assets/Resources/TDAnnihilation/UI/HUD/Coins.png`
- Create: `UnityProject/Assets/Resources/TDAnnihilation/UI/HUD/Mana.png`
- Create: `UnityProject/Assets/Resources/TDAnnihilation/UI/HUD/Health.png`
- Create: `UnityProject/Assets/Resources/TDAnnihilation/UI/HUD/HeavyAttack.png`
- Create: `UnityProject/Assets/Resources/TDAnnihilation/UI/HUD/MegaAttack.png`

**Interfaces:**
- Produces six Resources textures loaded by `Resources.Load<Texture2D>("TDAnnihilation/UI/HUD/<name>")`.

- [ ] **Step 1: Copy the six user assets to stable project names.**

  Use the supplied temporary paths in this order: attack=`c052ca74`, coins=`497bde72`, mana=`62e75804`, health=`9fd7abfa`, heavy=`249af1d9`, mega=`aa5699fd`.

- [ ] **Step 2: Confirm the binary assets are present and distinct.**

  Run:

  ```powershell
  Get-ChildItem UnityProject/Assets/Resources/TDAnnihilation/UI/HUD/*.png |
    Select-Object Name, Length
  ```

  Expected: six PNGs with non-zero, distinct file sizes.

- [ ] **Step 3: Commit the imported assets.**

  ```powershell
  git add UnityProject/Assets/Resources/TDAnnihilation/UI/HUD
  git commit -m "feat: add Greenward HUD asset textures"
  ```

### Task 2: Build the visual HUD zones and bindings

**Files:**
- Modify: `UnityProject/Assets/Resources/TDAnnihilation/UI/GreenwardRoot.uxml`
- Modify: `UnityProject/Assets/Resources/TDAnnihilation/UI/Greenward.uss`
- Modify: `UnityProject/Assets/Scripts/TD/TDVerticalSliceHUD.cs`

**Interfaces:**
- `GreenwardRoot.uxml` produces named `Image` elements: `hud-coins-image`, `hud-mana-image`, `hud-health-image`, `hud-attack-image`, `hud-heavy-image`, `hud-mega-image`.
- It produces named value labels: `hud-coins-value`, `hud-mana-value`, `hud-health-value`, `hud-attack-value`, `hud-heavy-value`, `hud-mega-value`.
- `TDVerticalSliceHUD.RefreshGameplayValues()` updates the six labels without changing the existing state model.

- [ ] **Step 1: Add the image/value elements to UXML.**

  Use this structure for each visual zone, with the appropriate resource name and value label:

  ```xml
  <ui:VisualElement name="hud-status-deck" class="hud-status-deck">
      <ui:VisualElement class="hud-asset-zone">
          <ui:Image name="hud-coins-image" class="hud-asset-frame" />
          <ui:Label name="hud-coins-value" text="120" class="hud-asset-value" />
      </ui:VisualElement>
      <ui:VisualElement class="hud-asset-zone">
          <ui:Image name="hud-mana-image" class="hud-asset-frame" />
          <ui:Label name="hud-mana-value" text="FULL" class="hud-asset-value" />
      </ui:VisualElement>
      <ui:VisualElement class="hud-asset-zone">
          <ui:Image name="hud-health-image" class="hud-asset-frame" />
          <ui:Label name="hud-health-value" text="20" class="hud-asset-value" />
      </ui:VisualElement>
  </ui:VisualElement>
  <ui:VisualElement name="hud-action-deck" class="hud-action-deck">
      <ui:VisualElement class="hud-attack-zone primary">
          <ui:Image name="hud-attack-image" class="hud-action-frame" />
          <ui:Label name="hud-attack-value" text="READY" class="hud-action-value" />
      </ui:VisualElement>
      <ui:VisualElement class="hud-attack-zone disabled">
          <ui:Image name="hud-heavy-image" class="hud-action-frame" />
          <ui:Label name="hud-heavy-value" text="LOCKED" class="hud-action-value" />
      </ui:VisualElement>
      <ui:VisualElement class="hud-attack-zone disabled">
          <ui:Image name="hud-mega-image" class="hud-action-frame" />
          <ui:Label name="hud-mega-value" text="LOCKED" class="hud-action-value" />
      </ui:VisualElement>
  </ui:VisualElement>
  ```

- [ ] **Step 2: Add the USS composition.**

  Anchor `hud-status-deck` at top-left and `hud-action-deck` at bottom-center. Use `position: absolute`, `overflow: hidden`, `-unity-background-scale-mode: scale-to-fit`, compact gold text, and reduced opacity on `.disabled`. Keep frame sizes bounded so the layout remains readable at the existing 1280x720 reference resolution.

- [ ] **Step 3: Bind textures and values in `TDVerticalSliceHUD`.**

  Cache the six `Image` controls and six `Label` controls. In `Start`, load each texture from `TDAnnihilation/UI/HUD/<name>` and set `image` plus `ScaleMode.ScaleToFit`. In `RefreshGameplayValues`, use:

  ```csharp
  hudCoinsValue.text = game.State.gold.ToString();
  hudManaValue.text = "FULL";
  hudHealthValue.text = game.State.lives.ToString();
  hudAttackValue.text = "READY";
  hudHeavyValue.text = "LOCKED";
  hudMegaValue.text = "LOCKED";
  ```

- [ ] **Step 4: Verify the UI names and commit.**

  Parse UXML with PowerShell and assert all twelve names exist, then commit:

  ```powershell
  git add UnityProject/Assets/Resources/TDAnnihilation/UI/GreenwardRoot.uxml UnityProject/Assets/Resources/TDAnnihilation/UI/Greenward.uss UnityProject/Assets/Scripts/TD/TDVerticalSliceHUD.cs
  git commit -m "feat: integrate Greenward HUD asset zones"
  ```

### Task 3: Recompile and verify the integrated HUD

**Files:**
- Modify: `docs/evidence/unity-greenward-menu-hud.md`

**Interfaces:**
- Verification consumes the six imported assets and the UI bindings from Task 2; it does not alter gameplay state.

- [ ] **Step 1: Recompile through Unity CLI.**

  ```powershell
  unity recompile --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json
  ```

  Expected: `compilationFailed=false`, `errors=0`, `warnings=0`.

- [ ] **Step 2: Build runtime, test and editor assemblies sequentially.**

  ```powershell
  dotnet build UnityProject\Assembly-CSharp.csproj --no-restore
  dotnet build UnityProject\TDAnnihilation.EditModeTests.csproj --no-restore
  dotnet build UnityProject\Assembly-CSharp-Editor.csproj --no-restore
  ```

  Expected: runtime and test builds have zero errors; editor build may retain the known `GeminiEditorChat.cs` UAC0005 warning.

- [ ] **Step 3: Attempt the focused Unity test command.**

  ```powershell
  unity test E:\TD-Annihilation-Unity-Git\UnityProject --mode EditMode --filter GreenwardGameplayPresentationTests
  ```

  If Unity refuses because the project is already open, record that limitation rather than closing the user's Editor.

- [ ] **Step 4: Update evidence and commit.**

  Append the exact commands/results to `docs/evidence/unity-greenward-menu-hud.md`, run `git diff --check`, and commit:

  ```powershell
  git add docs/evidence/unity-greenward-menu-hud.md
  git commit -m "docs: record HUD asset verification"
  ```
