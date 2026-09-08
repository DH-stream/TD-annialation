# TD Annihilation — project status

Last updated: 2026-09-08
Current branch: `codex/hero-movement`
Base: `origin/main` at `042c5e9`
Pull request: https://github.com/DH-stream/TD-annialation/pull/2

## Current milestone

Phase 0.6 — Foundation scene with first visual style pass and playable hero interaction.

## Confirmed direction

- Browser-first Vite + TypeScript + Babylon.js.
- Dungeon Defenders is the primary gameplay reference.
- Strategic top-down camera is the default: zoomable and angleable with bounded controls.
- Future co-op targets two different computers. Co-op is prepared through simulation/input boundaries, but is not the current focus.
- Future presentation includes restrained stylized gore: blood remains on the current map until the stage is complete, without harming gameplay readability.
- First completion of a stage on a map will later trigger gold confetti and a persistent first-win marker.
- Long-term target is a polished Steam game; every phase must be a coherent, testable vertical slice.

## Locked Greenward art direction

The Phase 0 scene is a foundation slice, but it must read as intentional charming low-poly rather than raw placeholder primitives. The visual target is chunky silhouettes, rich disciplined color, soft readable light and controlled surface character, using Synty POLYGON, Kenney, Quaternius, A Short Hike and Dungeon Defenders as reference points.

- Title/stage display font: Metal Mania, bundled locally with attribution and SIL Open Font License 1.1 in `THIRD_PARTY_LICENSES.md`.
- UI body font: readable system/UI stack until the final UI type system is selected.
- Greenward palette anchors: `#142523` horizon, `#3F5D47` ground, `#B98A57` path, `#59676D` stone, `#7B4B34` wood, `#D2A85B` brass/magic highlight and `#8E2F2B` blood.
- The board is uniformly enlarged so walkable space grows with the composition; camera framing remains strategic and bounded.
- Horizon fog replaces the bare void treatment; soft blurred shadows, ambient fill and ambient occlusion are enabled.
- Primitive surfaces receive controlled vertex-color variation and visible hard edges receive a subtle edge treatment.
- The hero preview uses a chunky capsule silhouette; the former ambiguous dark rectangular shape near the shrine is no longer used.

### Art QA checklist

- [x] No bare flat void background; use gradient skybox or coherent horizon/fog.
- [x] Map uses a locked 5–7 color palette with no ad-hoc material colors.
- [x] Hero-visible hard edges are bevelled/rounded or receive deliberate edge treatment.
- [x] Soft shadows, ambient fill and ambient occlusion preserve readability.
- [x] Gameplay-important silhouettes have correct scale; trees do not overpower the castle.
- [x] Every material has controlled surface variation.
- [ ] Environment and blocky mobs share a consistent stylized shading language.
- [x] Dark stray plane/debug geometry is resolved before visual approval.

Full target and references: `docs/superpowers/specs/2026-09-08-visual-style-foundation-design.md`.

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
- Added the Greenward visual style specification and implementation plan.
- Bundled Metal Mania locally for offline/commercial distribution with its license recorded.
- Enlarged the battlefield composition by a uniform map-root scale and kept click-to-move scale-aware.
- Added palette-driven fog, softened shadows, ambient occlusion, controlled vertex-color variation and primitive edge treatment.
- Replaced the boxy hero preview body with a low-tessellation capsule to remove the ambiguous shrine-area plane reading.

## Verification completed

- `npm install` — completed; 56 packages audited, 0 vulnerabilities.
- `npm test -- --run src/game/config/sceneConfig.test.ts` — 3 tests passed.
- `npm test -- --run src/game/createScene.test.ts src/game/config/sceneConfig.test.ts` — 3 tests passed.
- `npm run build` — completed with exit code 0; TypeScript checks and Vite production build passed.
- `npm test -- --run` — 9 tests passed across 4 test files, including palette and enlarged-map bounds regressions.
- `npm install @fontsource/metal-mania@5.3.0` — completed; package metadata declares `OFL-1.1` and no vulnerabilities were found.
- `npm run dev -- --host 127.0.0.1 --port 5174` — Vite served the app at `http://127.0.0.1:5174/`.
- Browser/IAB smoke check — page identity `TD Annihilation`, meaningful canvas/HUD rendered, no framework error overlay observed.
- Desktop visual check — castle sits behind the battlefield; path, four build pads and scene landmarks are readable.
- Movement smoke check — clicking a map surface shows a destination marker and moves the royal hero without treating a short click as camera orbit; WASD input is covered by unit tests.
- Interaction check — pointer drag changed the ArcRotateCamera orbit; wheel scrolling changed camera zoom while staying inside configured limits.
- Mobile visual check — temporary 390×844 viewport rendered without horizontal overflow; HUD remained readable. Viewport was restored after QA.
- Art QA browser check — Metal Mania rendered locally, larger battlefield was readable, horizon was fogged rather than void-black, click-to-move remained functional, shadows/AO were visible, camera orbit remained functional and the shrine-area box/plane reading was removed.

## Known risks

- Vite reports a large Babylon bundle: approximately 7.9 MB uncompressed and 1.66 MB gzip. This is acceptable for the foundation checkpoint but should be addressed before production distribution, likely by using tree-shakeable Babylon imports or code splitting.
- The scene still uses intentionally composed primitive geometry. Established asset-pack sourcing (Kenney, Synty POLYGON or Quaternius) and final GLB/environment/character art remain later vertical-slice work.
- The map is still a foundation/v0 sandbox: no combat, enemies, towers, navigation, save data, Supabase, WebRTC, Steamworks or Tauri integration exists yet.

## What is next

1. Review the visual style pass and hero movement branch against `main`.
2. Source the first consistent low-poly asset pack and create a per-map moodboard before Phase 4 art work.
3. Plan Phase 1 core loop: data-driven enemy/tower types, fixed path, five-enemy wave, tower targeting and win/lose state.
4. Preserve the current `GameState`/`PlayerInput` boundaries while adding the first deterministic simulation step.
