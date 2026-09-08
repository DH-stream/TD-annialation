# TD Annihilation — project status

Last updated: 2026-09-08
Current branch: `codex/hero-movement`
Base: `origin/main` at `042c5e9`
Pull request: https://github.com/DH-stream/TD-annialation/pull/2

## Current milestone

Phase 0.5 — Foundation scene with first playable hero interaction.

## Confirmed direction

- Browser-first Vite + TypeScript + Babylon.js.
- Dungeon Defenders is the primary gameplay reference.
- Strategic top-down camera is the default: zoomable and angleable with bounded controls.
- Future co-op targets two different computers. Co-op is prepared through simulation/input boundaries, but is not the current focus.
- Future presentation includes restrained stylized gore: blood remains on the current map until the stage is complete, without harming gameplay readability.
- First completion of a stage on a map will later trigger gold confetti and a persistent first-win marker.
- Long-term target is a polished Steam game; every phase must be a coherent, testable vertical slice.

## Research conclusions

The closest genre benchmarks show that a premium-feeling action tower-defense game needs a clear visual identity, readable combat feedback, meaningful hero/tower roles, progression, a good solo path and multiplayer that scales with the party. Steam planning must eventually include controller support, networking/lobbies, achievements, cloud saves, overlay and Steam Deck validation.

## What is done

- Confirmed GitHub repository and default branch (`main`).
- Created local Git checkout from latest `main`.
- Created branch `codex/phase-0-foundation`.
- Approved Phase 0 foundation design.
- Added design specification at `docs/superpowers/specs/2026-09-08-phase-0-foundation-design.md`.
- Created Vite + TypeScript + Babylon.js application scaffold.
- Added strategic battlefield scene with castle, path, build pads, shrine, barricades, trees, lighting and HUD shell.
- Added plain simulation contracts for future player input and network transport.
- Corrected the initial camera azimuth after visual QA found the castle blocking the foreground.
- Added click-to-move for the royal hero on map surfaces, with a destination marker.
- Added deterministic WASD movement with remappable keyboard bindings; keyboard movement overrides click-to-move.
- Added a focused implementation plan at `docs/superpowers/plans/2026-09-08-hero-movement.md`.

## Verification completed

- `npm install` — completed; 56 packages audited, 0 vulnerabilities.
- `npm test -- --run src/game/config/sceneConfig.test.ts` — 2 tests passed.
- `npm test -- --run src/game/createScene.test.ts src/game/config/sceneConfig.test.ts` — 3 tests passed.
- `npm run build` — completed with exit code 0; TypeScript checks and Vite production build passed.
- `npm test -- --run` — 7 tests passed across 4 test files, including keyboard input and hero movement.
- `npm run dev -- --host 127.0.0.1 --port 5174` — Vite served the app at `http://127.0.0.1:5174/`.
- Browser/IAB smoke check — page identity `TD Annihilation`, meaningful canvas/HUD rendered, no framework error overlay observed.
- Desktop visual check — castle sits behind the battlefield; path, four build pads and scene landmarks are readable.
- Movement smoke check — clicking a map surface shows a destination marker and moves the royal hero without treating a short click as camera orbit; WASD input is covered by unit tests.
- Interaction check — pointer drag changed the ArcRotateCamera orbit; wheel scrolling changed camera zoom while staying inside configured limits.
- Mobile visual check — temporary 390×844 viewport rendered without horizontal overflow; HUD remained readable. Viewport was restored after QA.

## Known risks

- Vite reports a large Babylon bundle: approximately 7.9 MB uncompressed and 1.66 MB gzip. This is acceptable for the foundation checkpoint but should be addressed before production distribution, likely by using tree-shakeable Babylon imports or code splitting.
- The scene still uses intentionally composed primitive geometry. Final GLB/environment/character art, gore VFX, audio and confetti are later vertical-slice work.
- The map is still a foundation/v0 sandbox: no combat, enemies, towers, navigation, save data, Supabase, WebRTC, Steamworks or Tauri integration exists yet.

## What is next

1. Review the hero movement branch against `main`.
2. Plan Phase 1 core loop: data-driven enemy/tower types, fixed path, five-enemy wave, tower targeting and win/lose state.
3. Preserve the current `GameState`/`PlayerInput` boundaries while adding the first deterministic simulation step.
