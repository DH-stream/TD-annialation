# TD Annihilation project status

## Unity vertical slice

The Unity production slice is currently in the external Unity project at `C:\TD-Annihilation-Unity\TD-Annihilation-Unity` on Unity `6000.6.0f1`.

Verified in the Unity Editor on 2026-09-09:

- `Assets/Scenes/SampleScene.unity` now generates a 96x64m Greenward Valley with an elevated strategic camera, sculpted terrain, a 96.7m invasion route, river and bridge, inhabited village, corrupted portal lowlands, castle high ground, forest boundaries, lighting, bootstrap, and HUD.
- Free Quaternius CC0 Warrior and Demon FBX assets are imported under `Assets/Art/ThirdParty/Quaternius` and loaded through `Assets/Resources/TDAnnihilation`.
- Animator controllers use the imported Warrior Idle and Demon Walk clips; Demon Walk is configured to loop with root motion disabled.
- Play Mode spawned the hero, tower, demons, projectiles, coin drops, wave progression, and live HUD counters without console errors.
- A verified visual capture is at `C:\TD-Annihilation-Unity\TD-Annihilation-Unity\Assets\Screenshots\greenward_valley_final.png`.

Known environment issue: the Unity MCP package still reports failed local HTTP/WebSocket transport startup because `uvx` is not available to its launcher, even though the editor command bridge is currently usable. The Unity project is not yet merged into this Babylon/Vite repository.
# Unity migration status — 2026-09-09

- Unity 6000.6.0f1 project is now versioned under `UnityProject/` on `codex/unity-vertical-slice`.
- Original external Unity working copy remains at `C:/TD-Annihilation-Unity/TD-Annihilation-Unity`.
- Babylon gameplay reference is preserved on `codex/v1-playable-shell`; `main` contains only README.
- Visual/gameplay correction pass added uneven terrain sampling, sand/gravel road readability, Input System hero movement, bounded Shift+WASD/arrow camera pan, smaller raiders, and periodic larger high-health elites.
- Unity EditMode: 3/3 focused tests passed. Clean Play Mode: zero Console messages after 12 seconds.
- Migration gap matrix: `docs/migration/2026-09-09-babylon-to-unity-inventory.md`.
- First gameplay integration chain now owns explicit `MainMenu → Build → Wave → Victory/Defeat → MainMenu` transitions in C#.
- Runtime verification: menu action entered Build, wave 1 spawned five enemies, enemies reached waypoint 5, three waves completed as Victory with 18 defeated, and Return to Menu reset the flow. Console remained empty.
- Unity EditMode after integration: 4/4 tests passed.

## UI Toolkit menu and HUD — 2026-09-25

- Added the UI Toolkit foundation for Main Menu, Stage Select, Skill Tree, Gameplay HUD, Settings shell, and Result screens.
- Greenward is selectable; The Ashen Pass and The Sunken Keep are visible as locked future stages.
- Session-local Warden, Arcana, and Bastion skill-node state is wired with prerequisite checks but has no gameplay effects or persistence yet.
- Replaced the legacy TDVerticalSliceHUD OnGUI rendering with UXML/USS and runtime binding to the existing game-flow methods.
- Runtime and editor assemblies build with zero errors; the existing GeminiEditorChat analyzer warning remains unchanged.
- Play Mode verification is pending because the open Unity Editor is not currently exposing a reachable Pipeline server to the Unity CLI test/command runner.
