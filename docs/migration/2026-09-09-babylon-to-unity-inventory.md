# Babylon to Unity migration inventory

Source of truth after migration: `UnityProject/`. Babylon remains available on `codex/v1-playable-shell` as historical and behavioral reference only.

## Verified baselines

- Unity root: `UnityProject/` in this repository; original working copy remains untouched at `C:/TD-Annihilation-Unity/TD-Annihilation-Unity`.
- Unity version: 6000.6.0f1, URP, active prototype scene `Assets/Scenes/SampleScene.unity`.
- Babylon reference: branch `codex/v1-playable-shell`; `main` currently contains only `README.md`.
- Unity baseline includes `Assets`, `Packages`, `ProjectSettings`, `.meta` files, free Quaternius character art, and CC0 Poly Haven gravel textures. Generated Unity folders are excluded.

## Functional gap matrix

| Capability | Babylon reference | Unity baseline | Migration status |
|---|---|---|---|
| Main menu/navigation | Working Solo/Friend and mode flow | Debug HUD only | Port and restyle |
| Stages | Three-stage selection/data | Greenward prototype only | Port stage definitions; integrate Greenward first |
| Endless | Scaling wave data, partial soak evidence | Endless auto-wave prototype | Adapt to shared Unity wave service |
| Waves | Deterministic build/wave loop | Timed auto-spawn | Replace with explicit state-driven wave loop |
| Enemy pathing | Shared S-route | Working waypoint route | Keep Unity route; add data-driven enemy definitions |
| Enemy animation | Authored walk playback | Quaternius Walk controller | Working baseline; continue runtime verification |
| Elite enemies | Not identified as complete | Large/high-health archetype added | Prototype working; formalize as data |
| Towers | Rune-pad placement, targeting, damage | Two pre-placed targeting towers | Targeting/damage exists; placement must migrate |
| Free placement | Not present; rune pads only | Not present | Build new ghost/valid-surface placement per approved direction |
| Economy | Gold spending and proximity coin pickup | Gold rewards plus visual coin, no pickup award | Partially present; migrate pickup and spending rules |
| Hero movement | Click-to-move and remappable WASD | WASD added with Input System | Working baseline; add click-to-move later if retained |
| Hero attacks | J/K animation and cooldown UI | None | Port rules; add real damage/cooldowns/animation |
| Health/win/loss | Base health and terminal state | Lives decrement; no result flow | Port and connect to menu state |
| Skill tree | Interactive/prerequisite-aware, session-local | None | Port model and add versioned persistence |
| Settings | Persisted movement remapping | None | Port as Unity settings/save data |
| Save/load | Settings only; progression persistence explicitly open | None | Design versioned Unity save schema; no silent reset |
| Dev tools | Gameplay HUD/input evidence | Minimal HUD | Add deterministic wave/combat test controls |
| Friend/co-op | Discovery transport prototype, no authority | None | Architecture requirement only; deliberately deferred |
| Skins | Future requirement | Prefab reference is swappable | Preserve presentation/data separation; deferred content |

## First integration slice

The next implementation milestone is one complete chain: Unity main menu → mode selection → Greenward → explicit wave start → hero/tower combat → win or loss → result → menu. It must reuse the current Greenward world and Unity C# runtime. Babylon code is never loaded by Unity.

Free placement, coin pickup, progression, settings, additional stages, and Endless are integrated after this chain is demonstrably working. Multiplayer and skins remain architectural constraints rather than current deliverables.

## Data preservation

The Babylon branch uses browser-local settings and session-local skill-tree state. No verified durable skill progression exists to import. Before Unity persistence ships, define a versioned JSON save envelope and an explicit migration field. Do not claim browser-local data migration until the exact keys and payloads are inspected.
