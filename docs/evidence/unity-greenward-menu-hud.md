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
