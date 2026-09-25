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
