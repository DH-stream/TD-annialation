# Greenward Menu and HUD Verification

Date: 2026-09-25
Branch: codex/unity-vertical-slice
Unity: 6000.6.0f1
Project: E:/TD-Annihilation-Unity-Git/UnityProject

## Source and asset checks

- UXML parsed successfully with PowerShell XML parsing.
- Required screen and control names were present in GreenwardRoot.uxml.
- PanelSettings asset contains Scale With Screen Size, reference resolution 1280x720, and match 0.5.
- Pure C# checks passed for the three-stage catalog and skill prerequisites.

## Compilation

Command:

    dotnet build UnityProject/Assembly-CSharp.csproj --no-restore

Result: passed with 0 errors and 0 warnings.

Command:

    dotnet build UnityProject/Assembly-CSharp-Editor.csproj --no-restore

Result: passed with 0 errors and the existing GeminiEditorChat analyzer warning UAC0005.

Command:

    dotnet build UnityProject/TDAnnihilation.EditModeTests.csproj --no-restore

Result: passed with 0 errors and 0 warnings.

Command:

    unity recompile --project-path E:/TD-Annihilation-Unity-Git/UnityProject --format json

Result: passed; Unity reported failed=false, compilationFailed=false, errors=0, warnings=0.

## Test-runner limitation

The headless command:

    unity test E:/TD-Annihilation-Unity-Git/UnityProject --editor-version 6000.6.0f1 --mode EditMode --filter TDMenuDefinitionsTests --report-format junit --output E:/TD-Annihilation-Unity-Git/td-menu-tests.xml --timeout 600

was refused because the project is already open in the user's Unity Editor. The connected command runner also reported no reachable Pipeline server for run_tests/menu commands. The test assembly itself compiled successfully, but EditMode execution and Play Mode UI verification remain pending until that Editor exposes Pipeline or is safely closed by the user.

## Known scope limits

- Future stages have UI entries but no stage content yet.
- Skill nodes have session-local prerequisite state but no gameplay effects or permanent save.
- Settings is a navigation shell until the settings/save contract is migrated.
- The lower-left minimap frame is reserved; the full minimap system remains a separate implementation.

## Steam UI and placement pass

- Replaced the centered debug-style panels with a left-rail menu, authored stage/skill/result layouts and a compact gameplay HUD.
- Preserved all existing runtime button names and menu callbacks; the rewritten UXML parsed successfully with all required names present.
- Placement now creates a runtime MeshCollider for the authored terrain mesh, uses it for cursor ground sampling and ignores decoration colliders. Meadow remains buildable while road, riverbank, corruption and castle remain blocked.
- Newly placed towers rise from a compressed state with a short overshoot/settle and an auto-destroying dust burst.
- Scene inspection found active serialized `Grass Meadow Batch` objects and no gameplay code that disables them; Play Mode visual confirmation is still pending because the Unity Pipeline is unreachable.

## Additional verification

- `dotnet build UnityProject/Assembly-CSharp.csproj --no-restore`: passed with 0 errors and 0 warnings when run sequentially.
- `dotnet build UnityProject/TDAnnihilation.EditModeTests.csproj --no-restore`: passed with 0 errors and 0 warnings when run sequentially.
- `dotnet build UnityProject/Assembly-CSharp-Editor.csproj --no-restore`: passed with 0 errors and the existing `GeminiEditorChat.cs` warning UAC0005.
- `unity recompile --project-path E:/TD-Annihilation-Unity-Git/UnityProject --format json`: passed; Unity reported `up_to_date`, no errors and no warnings.
- Unity test execution remains blocked by the already-open Editor; the corrected CLI invocation reached the precondition and refused to run without closing it.

## Supplied HUD asset pass

- Imported six user-provided free PNG assets under `Assets/Resources/TDAnnihilation/UI/HUD/`: Attack, Coins, Mana, Health, HeavyAttack and MegaAttack.
- Added top-left visual status zones for Coins, Mana and Health. Coins bind to `TDResourceState.gold`; Health uses the existing defensive lives value; Mana displays `FULL` until a mana system exists.
- Added bottom-center visual action zones for Attack, Heavy Attack and Mega Attack. Attack is wired to the existing hero attack path; Heavy and Mega remain visibly locked and do not change gameplay state.
- Kept Build/radial selection and Start Wave in the same command deck, with the supplied assets serving as the primary visual language.
- `unity recompile --project-path E:/TD-Annihilation-Unity-Git/UnityProject --format json`: passed with `compilationFailed=false`, errors=0 and warnings=0.
- Sequential runtime and test assembly builds passed with 0 errors and 0 warnings. Editor assembly build passed with the existing `GeminiEditorChat.cs` UAC0005 warning.
- UXML parsing confirmed all twelve new image/value element names. EditMode execution was attempted with `GreenwardGameplayPresentationTests` and refused because the project is already open in Unity Editor PID 27792.
- All six imported PNGs were checked at 1448x1086 with transparent corner pixels (`cornerAlpha=0`).

## Command deck and village menu pass

- Added the provided TD Annihilation logo as a transparent, generated PNG under `Assets/Resources/TDAnnihilation/UI/TDAnnihilationLogo.png`; no paid or external asset dependency was added.
- Replaced the menu wordmark text with the logo and moved the live menu camera to the village area so villagers and authored scenery remain visible behind the menu.
- Replaced the lower gameplay text panel with a visual command deck for coins, Build, Attack, attack types and wave start. Build opens a centered radial selector; dragging/releasing on the active Arcane Watchtower card enters placement mode.
- Increased tower placement dust visibility with a world-space cone burst, larger opaque particles and a slightly raised emitter position.
- UXML name validation passed for the new logo, command deck and wheel controls. Unity recompile passed with 0 errors and 0 warnings; sequential runtime/test builds passed with 0 errors. Editor build retained the existing UAC0005 warning.

## Arcane tower asset pass

- Added the supplied `arcanetower.glb` under `Assets/Resources/TDAnnihilation/ArcaneTower.glb`.
- Added Unity glTFast `6.19.0` so the GLB can be imported as a Unity asset after the open Editor session reloads packages.
- The runtime now loads the imported model for the Arcane Watchtower and placement ghost, scales it to the existing tower footprint and preserves the procedural tower fallback if import is unavailable.
- Placement validation, tower targeting, projectile behavior and the existing rise/bounce/dust animation remain unchanged.
- `unity recompile --project-path E:/TD-Annihilation-Unity-Git/UnityProject --format json`: passed with `compilationFailed=false`, errors=0 and warnings=0.
- The currently open Editor still reports the GLB as `DefaultImporter`; model import and Play Mode appearance remain pending an Editor restart/reimport.
