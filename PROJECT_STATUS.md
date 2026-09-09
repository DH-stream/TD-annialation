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
