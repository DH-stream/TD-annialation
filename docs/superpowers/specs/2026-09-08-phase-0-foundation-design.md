# Phase 0 Foundation Design

**Date:** 2026-09-08
**Repository:** [DH-stream/TD-annialation](https://github.com/DH-stream/TD-annialation)
**Branch:** `codex/phase-0-foundation`

## Goal

Create the first visually intentional, browser-runnable foundation for TD Annihilation: a top-down fantasy battlefield with controllable zoom and camera angle, a coherent visual language, and architecture that can later support a two-computer co-op mode without moving core game logic into Supabase.

Phase 0 is a quality foundation, not a disposable prototype and not a claim of Steam readiness. It must already communicate the same product direction as the later game while remaining small enough to verify quickly.

## Quality direction

The primary genre reference is Dungeon Defenders: readable battlefield planning combined with a present hero, towers, progression and cooperative potential. Orcs Must Die! Deathtrap is used only as a secondary reference for punchy feedback and readable combat impact, not as the camera or overall design template. Deep Rock Galactic is used as a reference for role clarity, feedback, replayability and a good solo path.

The visual target is stylized fantasy with grounded lighting and restrained gore. We will not use generic primitives as the intended final art direction; temporary geometry may be used only where its silhouette, palette and lighting are deliberately composed. Blood decals/pools can remain on the current map until the stage ends, but they must not cover paths, build locations or important UI markers. A first-time stage victory will later trigger a short gold-toned confetti burst and a persistent first-win marker.

## Phase 0 scope

- Vite + TypeScript + Babylon.js web application.
- A composed fantasy battlefield scene: terrain, path, defensive/building landmarks, lighting and atmospheric background.
- ArcRotateCamera with a strategic default angle around 55–65 degrees, bounded zoom, bounded pitch and drag orbit.
- Clean canvas layout and a minimal HUD shell that establishes hierarchy without pretending gameplay systems already exist.
- Typed foundations for `GameState`, `PlayerInput` and a future `NetworkTransport` boundary.
- A small testable pure configuration module plus a production build check.
- `PROJECT_STATUS.md` maintained as the handoff document for future Codex sessions and the home PC.

## Explicit non-goals for Phase 0

- No Supabase client, authentication, lobby, WebRTC or online synchronization.
- No Tauri packaging or Steamworks SDK integration.
- No full wave simulation, tower combat, economy, hero attacks, progression tree or save system.
- No final character/enemy asset pipeline. Asset import boundaries may be documented, but final art production is a later vertical-slice task.

## Architecture

The browser entrypoint owns application startup and resize handling. Babylon presentation code owns the engine, scene, camera, lights and visual entities. Simulation types live outside Babylon so later wave, economy and combat code can run against plain data. Each local player will eventually produce `PlayerInput` commands; the simulation will consume those commands through a transport boundary. A future online implementation can provide a Supabase-signaled WebRTC transport without making Supabase part of the game rules.

Initial boundaries:

- `src/main.ts`: DOM canvas lookup, engine creation, scene startup and render loop.
- `src/game/createScene.ts`: Babylon scene composition and camera configuration.
- `src/game/sim/types.ts`: plain TypeScript state/input/transport contracts; no Babylon imports.
- `src/game/config/sceneConfig.ts`: pure Phase 0 scene constants and default camera values.
- `src/styles.css`: global page/canvas/HUD presentation only.

These boundaries are deliberately small. New towers, enemies and upgrades should later be data objects, while simulation behavior remains independent from Babylon rendering.

## Camera interaction

- Default: strategic top-down/3D angle, approximately 60 degrees from the ground plane.
- Mouse wheel: zoom in/out inside explicit minimum and maximum radius.
- Middle-mouse drag: orbit around the battlefield target.
- Pitch: bounded so the player can inspect the battlefield but cannot roll or lose the map.
- Later gamepad support will map to the same camera intent rather than inventing a second camera system.

## Quality gates

Phase 0 is complete only when all of the following are true:

1. `npm run build` succeeds from a clean install.
2. `npm run test` passes the pure scene configuration tests.
3. `npm run dev` shows the composed scene without console errors.
4. Camera zoom/orbit is smooth and bounded at a normal desktop viewport.
5. The scene has a deliberate palette, readable landmarks and a clear battlefield focal point.
6. `PROJECT_STATUS.md` records exact commands, current branch, known limitations and the next planned phase.

## Steam-quality implications

Steam integration is later, but the product plan must reserve space for Steam Input, lobbies/networking, achievements, cloud saves, overlay and Steam Deck validation. Steamworks documents these as distinct platform capabilities, so they will be treated as release requirements rather than retrofitted after gameplay is finished. The first implementation should keep input and persistence boundaries clean enough to add those integrations without rewriting the simulation.

References:

- [Orcs Must Die! Deathtrap on Steam](https://store.steampowered.com/app/2273980/)
- [Orcs Must Die! Deathtrap official page](https://robotentertainment.com/omdd)
- [Dungeon Defenders: Awakened on Steam](https://store.steampowered.com/app/1101190)
- [Deep Rock Galactic official FAQ](https://www.deeprockgalactic.com/faq-test-page)
- [Steamworks documentation](https://partner.steamgames.com/doc/home)
