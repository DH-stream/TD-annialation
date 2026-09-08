# TD Annihilation — project status

Last updated: 2026-09-08
Current branch: `codex/v1-playable-shell`
Base: `origin/main` at `042c5e9`
Pull request: https://github.com/DH-stream/TD-annialation/pull/3

## Current milestone

Phase 1.0 — First playable Greenward vertical slice.

## Confirmed direction

- Browser-first Vite + TypeScript + Babylon.js.
- Dungeon Defenders is the primary gameplay reference.
- Strategic top-down camera is the default: zoomable and angleable with bounded controls.
- Future co-op targets two different computers. Co-op is prepared through simulation/input boundaries, but is not the current focus.
- Supabase co-op boundary is now explicit: Realtime is a transport bridge only, with no login UI and no access to unrelated project tables or services.
- Blood splatter, dismemberment and gore presentation remain local per client in multiplayer; only deterministic gameplay/death event identifiers may cross the bridge.
- Future presentation includes restrained stylized gore: blood remains on the current map until the stage is complete, without harming gameplay readability.
- First completion of a stage on a map will later trigger gold confetti and a persistent first-win marker.
- Long-term target is a polished Steam game; every phase must be a coherent, testable vertical slice.

## Locked Greenward art direction

The Phase 0 scene is a foundation slice, but it must read as intentional charming low-poly rather than raw placeholder primitives. The visual target is chunky silhouettes, rich disciplined color, soft readable light and controlled surface character, using Synty POLYGON, Kenney, Quaternius, A Short Hike and Dungeon Defenders as reference points. The binding tone refinement is more medieval and magical, drawing on Kingdom Rush, Orcs Must Die and WoW's warm hand-painted light/shadow technique.

- Title/stage display font: Metal Mania, bundled locally with attribution and SIL Open Font License 1.1 in `THIRD_PARTY_LICENSES.md`.
- UI body font: readable system/UI stack until the final UI type system is selected.
- Greenward palette anchors: `#142523` horizon, `#3F5D47` ground, `#B98A57` path, `#59676D` stone, `#7B4B34` wood, `#D2A85B` brass/magic highlight and `#8E2F2B` blood.
- The board is uniformly enlarged so walkable space grows with the composition; camera framing remains strategic and bounded.
- Horizon fog replaces the bare void treatment; soft blurred shadows, ambient fill and ambient occlusion are enabled.
- Primitive surfaces receive controlled vertex-color variation and visible hard edges receive a subtle edge treatment.
- Reusable medieval/magical layer: torch poles and flames, rune rings on build pads and restrained emissive shrine/build-pad glow.
- The hero preview uses a chunky capsule silhouette; the former ambiguous dark rectangular shape near the shrine is no longer used.
- Future creatures must use the same stylized shading ramp as the environment rather than literal voxel cubes; gore remains optional, stylized and deterministic for co-op.

### Art QA checklist

- [x] No bare flat void background; use gradient skybox or coherent horizon/fog.
- [x] Map uses a locked 5–7 color palette with no ad-hoc material colors.
- [x] Hero-visible hard edges are bevelled/rounded or receive deliberate edge treatment.
- [x] Soft shadows, ambient fill and ambient occlusion preserve readability.
- [x] Gameplay-important silhouettes have correct scale; trees do not overpower the castle.
- [x] Every material has controlled surface variation.
- [ ] Environment and chunky stylized mobs share a consistent toon/cel shading language.
- [x] Dark stray plane/debug geometry is resolved before visual approval.

Full target and references: `docs/superpowers/specs/2026-09-08-visual-style-foundation-design.md`.

## Art compliance — Phase 0 pass

The updated art-direction instructions are treated as acceptance criteria for this foundation scene. Evidence below is from the current source and the browser QA capture shown during this task.

1. **Pass — no bare flat void.** `src/game/createScene.ts` sets a palette-derived clear color, exponential horizon fog and fog color before scene construction; the browser capture shows a coherent dark-green horizon instead of an empty black field.
2. **Pass — locked palette.** The six scene materials are created from `SceneConfig.colors` (`#3F5D47`, `#B98A57`, `#59676D`, `#7B4B34`, `#D2A85B`, plus blood reserved for future decals); the scene uses no undocumented material hue. Torch light now copies the brass material color.
3. **Pass — chamfered hero-visible hard edges.** `createChamferedBox()` builds an eight-sided 0.12 scene-unit corner chamfer through Babylon polygon extrusion; the browser capture shows softened path/castle corners and no raw box edge treatment dependency.
4. **Pass — soft shadows and AO.** `ShadowGenerator` uses blurred exponential filtering, bias/normal-bias and reduced darkness; `SSAO2RenderingPipeline` is attached to the strategic camera. The capture shows filled shadows under pads, barricades and trees.
5. **Pass — scale/silhouette coherence.** The board uses a uniform `mapRoot` scale of `1.5`; castle towers remain the dominant landmark while the largest tree stays below tower height. The same-angle browser capture shows the corrected relationship.
6. **Pass — surface variation.** Every mesh with a `StandardMaterial` receives height-based vertex light gradients plus small facet variation through `addSubtleVertexVariation()`; material roughness and emissive magic are controlled per palette role.
7. **Pass — shading language documented.** The environment uses warm `StandardMaterial` shading modulated by vertex gradients, hemispheric fill, blurred shadows and AO; future creatures are required to use a matching toon/cel ramp, documented in the visual spec and future acceptance criteria.
8. **Pass — stray shrine plane resolved.** The hero is a named capsule/rider silhouette under `heroRoot`; no unowned dark plane remains near the shrine in the browser capture.
9. **Pass — moodboard exists.** `docs/superpowers/specs/greenward-moodboard.md` contains five reference images, source pages and the locked seven-color palette.
10. **Pass — art QA is part of project verification.** This section and the checklist above sit next to the automated test, build, desktop/mobile and interaction verification records.

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
- Added a reusable magical detail layer with rune rings on build pads, torch poles/flames and restrained glow treatment.
- Replaced the boxy hero preview body with a low-tessellation capsule to remove the ambiguous shrine-area plane reading.
- Corrected default horizontal keyboard bindings so A moves right and D moves left for the current strategic camera orientation; bindings remain remappable.
- Added the first playable local loop: build phase, three-stage campaign or endless selection, rune-pad tower placement, deterministic enemy waves, tower targeting, base health and win/lose state.
- Added a real menu shell for Solo or Play with a Friend preparation mode, followed by Stages or Endless selection.
- Added a persisted Settings screen for live movement-key remapping and a radial, animated Greenward skill tree with prerequisite-aware purchases.

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
- Art direction follow-up — updated spec requirements for medieval/magical details, warm hand-painted-style depth, shared creature shading and deterministic optional gore are recorded as binding future constraints.
- v1 browser flow — Solo → Stages → Enter the Greenward rendered the gameplay HUD; clicking a rune pad placed a visible tower and deducted 50 gold; starting a wave updated the state and disabled the wave button while enemies advanced.
- Settings browser flow — all four movement rows rendered with the corrected A/D semantics; a live W → I remap updated immediately and persisted locally.
- Skill tree browser flow — radial nodes rendered with inner-to-outer animation; purchasing Royal Oath consumed one point and made the inner ring available.
- `npm test -- --run` — 19 tests passed across 7 test files after adding the v1 simulation and progression coverage.
- `npm run build` — completed with exit code 0; TypeScript checks and Vite production build passed.

## Known risks

- Vite reports a large Babylon bundle: approximately 7.9 MB uncompressed and 1.66 MB gzip. This is acceptable for the foundation checkpoint but should be addressed before production distribution, likely by using tree-shakeable Babylon imports or code splitting.
- The scene still uses intentionally composed primitive geometry. Established asset-pack sourcing (Kenney, Synty POLYGON or Quaternius) and final GLB/environment/character art remain later vertical-slice work.
- The current materials approximate hand-painted depth with vertex gradients; authored texture atlases and toon/cel creature shading are still future work.
- The current combat is intentionally a small deterministic v1 slice: one enemy family, one tower family and a short Greenward path. It is not yet content-complete or Steam-ready.
- No Supabase lobby, WebRTC transport, save data, Steamworks or Tauri integration exists yet; Friend mode is explicitly a preparation path.
- The shared Supabase project has not been modified. The isolated bridge decision is documented in `docs/superpowers/specs/2026-09-08-supabase-coop-bridge.md`.

## What is next

1. Source the first consistent low-poly asset pack and replace the v1 visual proxies with authored GLB/texture assets while preserving Greenward's palette and art QA rubric.
2. Replace the current StandardMaterial creature proxy with the shared toon/cel creature shading language.
3. Add stage-completion persistence and the requested first-clear confetti effect.
4. Add stylized persistent gore: bounded blood decals, deterministic host event IDs and independently toggleable dismemberment.
5. Implement the Supabase-backed two-PC Friend lobby and deterministic co-op state replication.
6. Continue Steam preparation: controller support, Steamworks integration, cloud saves, achievements and Steam Deck validation.

### Binding Phase 1+ creature and gore acceptance criteria

- Enemies are chunky stylized low-poly creatures with an environment-matched toon/cel ramp; visual contrast comes from silhouette/proportion and saturated magic color, never literal voxel cubes versus realistic rendering.
- Persistent blood uses Babylon ground decals from a 4–8 variant stylized palette atlas, capped at 150–300 active decals per stage with oldest-first eviction.
- Dismemberment uses pre-split head/torso/limb child segments, probabilistic attack-dependent detachment and short physics impulses; gore and dismemberment are independently toggleable.
- Co-op gore outcomes use a host-agreed death/event id as the deterministic random seed so all clients see the same result.
