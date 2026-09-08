# TD Annihilation — project status

Last updated: 2026-09-08
Current branch: `codex/v1-playable-shell`
Base: `origin/main` at `042c5e9`
Pull request: https://github.com/DH-stream/TD-annialation/pull/3

## Session notes — 2026-09-08

- Sync source: `git fetch --all` found the latest pushed work on `origin/codex/v1-playable-shell`; this checkout tracks that branch.
- Starting revision: `6412934e3be0bc0ddb2a3877d523eb2c02c3b2e6`; PR #3 is open against `main`.
- Baseline: `npm install` completed with no vulnerabilities; the first `npm test` found one stale coin test fixture after the route expansion. The fixture was corrected and the final fresh run is 32/32 tests plus a successful production build.

## Current milestone

Phase 1.0 — First playable Greenward vertical slice.

## Confirmed direction

- Browser-first Vite + TypeScript + Babylon.js.
- Dungeon Defenders is the primary gameplay reference.
- Strategic top-down camera is the default: zoomable and angleable with bounded controls.
- Future co-op targets two different computers. Co-op is prepared through simulation/input boundaries, but is not the current focus.
- Supabase co-op boundary is now explicit: Realtime is a transport bridge only, with no login UI and no access to unrelated project tables or services.
- Friend mode now has automatic TD-lobby discovery through Realtime Presence. A room can advertise a password-protected session without exposing the password; the shared room code and password are still required to enter the protected match.
- The browser implementation intentionally discovers players in the TD lobby, not by scanning local Wi-Fi. After Tauri packaging, add a native mDNS/UDP LAN discovery layer and keep Supabase as the internet/session bridge.
- Blood splatter, dismemberment and gore presentation remain local per client in multiplayer; only deterministic gameplay/death event identifiers may cross the bridge.
- Future presentation includes restrained stylized gore: blood remains on the current map until the stage is complete, without harming gameplay readability.
- First completion of a stage on a map will later trigger gold confetti and a persistent first-win marker.
- Long-term target is a polished Steam game; every phase must be a coherent, testable vertical slice.

## Locked Greenward art direction

The Phase 0 scene is a foundation slice, but it must read as intentional charming low-poly rather than raw placeholder primitives. The visual target is chunky silhouettes, rich disciplined color, soft readable light and controlled surface character, using Synty POLYGON, Kenney, Quaternius, A Short Hike and Dungeon Defenders as reference points. The binding tone refinement is more medieval and magical, drawing on Kingdom Rush, Orcs Must Die and WoW's warm hand-painted light/shadow technique.

- Title/stage display font: Metal Mania, bundled locally with attribution and SIL Open Font License 1.1 in `THIRD_PARTY_LICENSES.md`.
- Imported character assets: Kenney Blocky Characters (`character-a` for mobs and `character-d` for the royal hero), including the authored idle, walk, melee and kick animation clips. The pack is kept under `public/assets/vendor/kenney/blocky-characters/` with its CC0 license.
- Imported environment assets: selected modular walls, roofs, trees, rocks, fences, banners, lanterns and fountain pieces from Kenney Fantasy Town Kit, kept under `public/assets/vendor/kenney/fantasy-town-kit/` with its CC0 license. The existing proxy layout remains as a safe fallback if an asset request fails.
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

## Visual v0 implementation pass

- [x] Replaced the enemy and hero placeholder silhouettes with real Kenney Blocky Characters GLB assets.
- [x] Replaced the most visible castle, tree, rock, fence, banner, lantern and shrine proxy geometry with real Kenney Fantasy Town Kit GLB assets loaded through a lazy, cached asset layer.
- [x] Loaded the GLB loader lazily so Solo's initial application does not pay the loader cost before the asset request.
- [x] Connected authored `walk`, `idle`, `attack-melee-right` and `attack-kick-right` clips; J and K are one-shot actions with visible HUD cooldowns.
- [x] Replaced the former near-straight enemy route with a shared S-shaped Greenward waypoint path that ends at the castle gate.
- [x] Added world-space coin drops on kills and proximity pickup by the hero; coin pickup, not the remote client, grants the gold.
- [ ] Replace the remaining gameplay proxy geometry (towers, combat effects and future map dressing) with a consistent environment/character treatment and finish the shared toon/cel material ramp.

## Performance — budgets and verification

The following is the current v0 budget record. It is intentionally evidence-based; items that need a later soak or instance pass remain open.

- **A1 — bundle loading: pass for current target.** Babylon imports now use per-module paths. SSAO2 and the GLB loader are lazy-loaded. The production entry is 1,369.80 kB minified / 337.44 kB gzip; its split application chunks are 670.78 kB / 163.58 kB gzip and 224.85 kB / 58.93 kB gzip. The initial transferred JavaScript remains below the 1 MB gzip target, while the minified-chunk warning is retained for future tightening.
- **A2 — frame instrumentation: pass for instrumentation, measurement follow-up open.** `SceneInstrumentation` captures frame, render and active-mesh evaluation counters in `src/game/createScene.ts`. A repeatable desktop 60 fps / integrated 30 fps evidence run still needs to be recorded on representative hardware.
- **A3 — pooling: open.** Enemy and coin visuals are currently disposed when removed. A pooled enemy/projectile/gore runtime is required before the endless soak target can pass.
- **A4 — static instancing: open.** Repeated trees and barricade parts are still separate meshes. Convert them to instances and record draw-call counts before production content work.
- **A5 — gore decal cap: open by design.** Gore is not in this v0 pass yet. Once added, the hard requirement is 150–300 active decals per stage with oldest-first eviction.
- **A6 — budget checks: pass as documentation, not yet CI-enforced.** This section records the current bundle and runtime targets; automated bundle/frame budget assertions remain follow-up work.

## Final-product gap check

- **B1 — hero combat: partial.** Basic and special attack animation clips are wired to J/K with cooldown display. Damage resolution, combat VFX, screen shake and a full hero/tower combat pass remain open.
- **B2 — coin economy: pass for the v0 loop.** A tower kill creates a coin at the death position; the hero must approach it before gold increases. Covered by `stageSimulation.test.ts` and visible as a rotating world pickup.
- **B3 — Endless verification: partial.** Endless mode and scaling wave data exist, but a multi-wave soak capture is still required.
- **B4 — skill persistence: open.** The current skill tree is interactive but still session-local; persistence across reload/new session is required.

## Supabase bridge status

- [x] Added `src/game/network/supabaseRealtimeBridge.ts` with a TD-owned Realtime Broadcast namespace: `td-annihilation:v1:room:<ROOM_CODE>`.
- [x] Friend mode presents a shareable room code, an optional session password and no-login wording; protected match channels use a SHA-256-derived channel suffix so the password is not present in a readable channel name.
- [x] Added a TD-owned `td-annihilation:v1:lobby` Presence channel. The Friend setup view lists other open TD clients automatically, shows whether their session is locked, and lets the player copy a discovered room code into the join field.
- [x] The bridge uses only the public client configuration from `.env.example`, never a service-role/secret key.
- [x] No tables, migrations, policies or existing rows in the shared Supabase project were read or modified. The bridge uses only TD-owned Realtime Broadcast/Presence channel names and is therefore isolated from the other projects in that Supabase project.
- [x] The payload boundary is gameplay intent/state only. Blood splatter, decals and dismemberment remain local presentation and are deliberately not part of the transport contract.
- [ ] Add password verification/join handshake, host/guest authority, authoritative snapshots and reconnection handling in the next co-op pass. The current derived channel name prevents casual channel discovery but is not a server-enforced authentication boundary.
- [ ] Add Tauri-only same-Wi-Fi discovery with mDNS/UDP; browser Presence cannot prove that two clients share a physical network.

## Art compliance — Phase 0 pass

The updated art-direction instructions are treated as acceptance criteria for this foundation scene. Evidence below is from the current source and the browser QA capture shown during this task.

1. **Pass — no bare flat void.** `src/game/createScene.ts` sets a palette-derived clear color, exponential horizon fog and fog color before scene construction; the browser capture shows a coherent dark-green horizon instead of an empty black field.
2. **Pass — locked palette.** The six scene materials are created from `SceneConfig.colors` (`#3F5D47`, `#B98A57`, `#59676D`, `#7B4B34`, `#D2A85B`, plus blood reserved for future decals); the scene uses no undocumented material hue. Torch light now copies the brass material color.
3. **Pass — chamfered hero-visible hard edges.** `createChamferedBox()` builds an eight-sided 0.12 scene-unit corner chamfer through Babylon polygon extrusion; the browser capture shows softened path/castle corners and no raw box edge treatment dependency.
4. **Pass — soft shadows and AO.** `ShadowGenerator` uses blurred exponential filtering, bias/normal-bias and reduced darkness; `SSAO2RenderingPipeline` is attached to the strategic camera. The capture shows filled shadows under pads, barricades and trees.
5. **Pass — scale/silhouette coherence.** The board uses a uniform `mapRoot` scale of `1.5`; castle towers remain the dominant landmark while the largest tree stays below tower height. The same-angle browser capture shows the corrected relationship.
6. **Pass — surface variation.** Primitive meshes receive height-based vertex light gradients plus small facet variation through `addSubtleVertexVariation()`; imported Kenney meshes retain their authored colormap variation while material roughness and emissive magic remain controlled per palette role.
7. **Pass — shading language documented.** The environment uses warm `StandardMaterial` shading modulated by vertex gradients, hemispheric fill, blurred shadows and AO; future creatures are required to use a matching toon/cel ramp, documented in the visual spec and future acceptance criteria.
8. **Pass — stray shrine plane resolved.** The hero is a named capsule/rider silhouette under `heroRoot`; no unowned dark plane remains near the shrine in the browser capture.
9. **Pass — moodboard exists.** `docs/superpowers/specs/greenward-moodboard.md` contains five reference images, source pages and the locked seven-color palette.
10. **Pass — art QA is part of project verification.** This section and the checklist above sit next to the automated test, build, desktop/mobile and interaction verification records.

## Atmosphere & regression pass — fresh 2026-09-08 evidence

This section supersedes the earlier Phase 0 art-compliance claims for no-void coverage (item 1), locked rendered palette (item 2) and scale/silhouette coherence (item 5). Those claims must not be reused as evidence after later environment changes. The current comparison target is `docs/superpowers/specs/greenward-moodboard.md`; fresh captures are stored at `docs/evidence/greenward-atmosphere-default.jpg` and `docs/evidence/greenward-atmosphere-orbit.jpg`.

1. **Pass — no bare void.** `createHorizonDome()` creates an infinite-distance, fog-independent horizon shell using the locked horizon/ground colors. Fresh 1280×720 default and middle-drag orbit captures show the full camera frustum covered at both tested bounded angles with no raw black clear color.
2. **Pass — locked rendered palette.** The configured ground remains `#3F5D47` (`[63,93,71]`). Fresh unshadowed screenshot samples were `[63,92,58]`, `[63,93,56]` and `[63,93,56]`: red/green are within 1 level and blue is within 15 levels after ACES, warm key, cool fill, AO and JPEG capture. This is the documented acceptance tolerance for this pass.
3. **Pass — scale and silhouette coherence.** The castle baseline moved behind the route endpoint (`castleZ = -12.5`), its roofs stay below the HUD card in the default capture, and the moved-right rock cluster no longer touches the route edge. The shrine/fountain, route and build pads remain readable ahead of the background buildings.
4. **Pass — intentional diorama composition.** In the default capture the eye lands first on the bright brass shrine orb over the pale circular fountain, then follows the ochre S-route toward the castle. This establishes one central focal point while keeping the castle as the destination landmark.
5. **Pass — placement and HUD readability.** The default capture shows no building floating over or clipping into the path and no static prop covering the top-right stage card or bottom command panel. `src/styles.css` anchors the stage card at the top-right instead of over the shrine.
6. **Pass — ambient motion.** Imported tree roots are registered by `registerWindNode()` and receive layered oscillation in `updateAmbient()`. Procedural flame meshes and their point lights use independent high-frequency flicker; the environment replacement now explicitly preserves `lantern-flame-*` meshes so it no longer animates disposed references.
7. **Pass — ambient particles.** `createAmbientMotes()` creates twelve low-density brass motes and `updateAmbient()` drifts each on distinct phase/frequency values. They remain local presentation and do not enter the multiplayer payload.
8. **Pass — warm/cool lighting.** ACES exposure is calibrated to `0.62`; a warm directional key (`[1,0.84,0.66]`) is balanced by a cooler hemispheric fill (`[0.58,0.72,0.68]`). The fresh captures preserve green ground, readable roof facets and soft shadows rather than the earlier cream washout.
9. **Pass — signs of habitation.** Two imported CC0 chimney assets sit on the outer roofs. Four softly emissive smoke puffs rise, drift, scale and fade continuously; both chimney smoke columns are visible in the fresh default and orbit captures.
10. **Pass — optional audio intentionally omitted.** No ambient-audio infrastructure exists yet, so wind/bird/village audio was not introduced under the explicitly non-blocking stretch clause.

Fresh verification for this exact pass:

- `npm test -- --run` — 26 tests passed across 8 files, including the new locked-horizon regression assertion.
- `npm run build` — exit code 0; 949 modules transformed. Entry output is 1,372.67 kB minified / 338.51 kB gzip and remains below the 1 MB gzip initial-load budget; the existing >500 kB chunk warning remains open performance work.
- Desktop browser QA — 1280×720 default and alternate orbit captures above; the orbit was performed with the documented middle-button control.
- Palette readback — six screenshot samples recorded, including three unobstructed ground samples and two horizon samples (`[40,59,51]`).
- Mobile browser smoke check — a clean 390×844 initialization rendered without horizontal overflow at `docs/evidence/greenward-atmosphere-mobile-390x844.jpg`. Mobile is not a supported gameplay target and receives no layout/camera optimization; desktop remains the visual acceptance platform.
- `git diff --check` — no whitespace errors; PowerShell reported only expected LF→CRLF working-copy notices.

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
- Environment asset pass — the visible Greenward castle, trees, rocks, fences, banner, lanterns and fountain now use CC0 Kenney Fantasy Town Kit assets; a shared-material disposal bug that washed the battlefield white was fixed, and fresh desktop QA shows the locked dark-green ground palette.
- v1 browser flow — Solo → Stages → Enter the Greenward rendered the gameplay HUD; clicking a rune pad placed a visible tower and deducted 50 gold; starting a wave updated the state and disabled the wave button while enemies advanced.
- Settings browser flow — all four movement rows rendered with the corrected A/D semantics; a live W → I remap updated immediately and persisted locally.
- Skill tree browser flow — radial nodes rendered with inner-to-outer animation; purchasing Royal Oath consumed one point and made the inner ring available.
- `npm test -- --run` — 23 tests passed across 8 test files after adding attack-input, coin pickup and bridge-boundary coverage.
- `npm run build` — completed with exit code 0; TypeScript checks and Vite production build passed. Babylon is now split into tree-shakeable application chunks; SSAO2 and Supabase remain lazy-loaded.
- Browser desktop QA — fresh Vite instance rendered the styled menu and playable scene after the modular Babylon shadow-component fix; Kenney hero/enemy GLBs loaded and enemies followed the new multi-turn path.
- Browser Friend-mode QA — room-code field rendered with the explicit no-login copy; entering the Greenward preserved local play when Supabase environment variables were intentionally absent.
- Browser HUD QA — BASIC/SPECIAL READY indicators and J/K control copy rendered in gameplay.

## Known risks

- The scene still uses composed primitive environment geometry. Characters are now real licensed GLB assets, but the castle, trees and towers need an authored environment pack pass.
- The current materials approximate hand-painted depth with vertex gradients; authored texture atlases and toon/cel creature shading are still future work.
- The current combat is intentionally a small deterministic v1 slice: one enemy family, one tower family and a short Greenward path. It is not yet content-complete or Steam-ready.
- Supabase Broadcast is now implemented as a first transport slice, but authority, snapshots, reconnection, save data, Steamworks and Tauri integration remain open. The shared Supabase project has not been modified.
- Browser Friend discovery is intentionally broader than same-Wi-Fi discovery: anyone with the public client configuration and the TD lobby open may appear. Passwords are not advertised, but the current browser bridge still needs a proper server-authoritative join handshake before it should be treated as a final security boundary.

## What is next

1. Replace the remaining environment proxies with an authored CC0 low-poly environment pack and complete the shared toon/cel ramp.
2. Add persistent stage completion, first-clear confetti and skill-tree persistence.
3. Add stylized persistent gore locally: bounded blood decals, deterministic host event IDs and independently toggleable dismemberment.
4. Complete Supabase host/guest authority, snapshots and reconnection for the two-PC Friend lobby.
5. Finish the performance pass: pooling, instancing, multi-wave soak and automated budget assertions.
6. Continue Steam preparation: controller support, Steamworks integration, cloud saves, achievements and Steam Deck validation.

### Binding Phase 1+ creature and gore acceptance criteria

- Enemies are chunky stylized low-poly creatures with an environment-matched toon/cel ramp; visual contrast comes from silhouette/proportion and saturated magic color, never literal voxel cubes versus realistic rendering.
- Persistent blood uses Babylon ground decals from a 4–8 variant stylized palette atlas, capped at 150–300 active decals per stage with oldest-first eviction.
- Dismemberment uses pre-split head/torso/limb child segments, probabilistic attack-dependent detachment and short physics impulses; gore and dismemberment are independently toggleable.
- Co-op gore outcomes use a host-agreed death/event id as the deterministic random seed so all clients see the same result.

## Session resync — 2026-09-08

This is the single current reconciliation record. It replaces the earlier historical pass statements as evidence: every row below was checked against the current `codex/v1-playable-shell` worktree in this session. The visual comparison target was `docs/superpowers/specs/greenward-moodboard.md`.

| Area | Acceptance criterion | Status | Fresh evidence |
| --- | --- | --- | --- |
| Sync | Latest pushed work is checked out exactly | Pass | `git fetch --all`; branch is `codex/v1-playable-shell`, starting at `6412934e3be0bc0ddb2a3877d523eb2c02c3b2e6`, tracking its origin branch. |
| Baseline | Install, unit tests and production build are clean | Pass | `npm install`; final `npm test` = 10 files / 32 tests passed; final `npm run build` exits 0. |
| Phase 0 | Desktop scene and strategic HUD are usable | Pass | Fresh 1280×900 capture `.playwright-cli/page-2026-09-08T18-13-48-155Z.png`; Solo → Stages → Enter flow reached the live HUD with no console errors. |
| Phase 0 | Mobile viewport remains readable | Pass | Fresh 390×844 capture `.playwright-cli/page-2026-09-08T18-14-01-521Z.png`; no horizontal overflow or console errors. |
| Phase 0 | Camera orbit and bounded strategic view work | Pass | Fresh middle-drag orbit capture `.playwright-cli/page-2026-09-08T18-14-39-658Z.png`; the route, pads and HUD remain usable. |
| Art compliance | No bare void or hard ground/clear-color edge | Pass | Default, mobile and orbit captures above are fully covered by `greenward-horizon-ground` plus the infinite horizon dome; the current orbit has no black clear-color gap. |
| Art compliance | Locked Greenward material palette | Pass | `DEFAULT_SCENE_CONFIG.colors` is the seven-color source; fresh rendered ground remains green and the path/brass/stone/wood roles are visible without a new scene material hue. |
| Art compliance | Intentional low-poly edges and controlled surface variation | Pass | `createChamferedBox()` and `addSubtleVertexVariation()` remain applied; current captures show faceted, chamfered pads/path and rounded hero forms. |
| Art compliance | Soft shadows, fill and AO preserve readability | Pass | Current scene uses blurred shadow maps, hemispheric fill and lazy SSAO; fresh desktop/mobile captures retain readable pads, route and focal shrine. |
| Art compliance | Building scale and placement are coherent | Pass | Rechecked after the earlier stale claim: castle is now at local `z = -15` with 1.75–2.15 asset scales, and the default/orbit captures show it as a destination landmark without obscuring the HUD or route. |
| Art compliance | Environment and mobs share a toon/cel material ramp | Fail | Current characters/environment still use the existing StandardMaterial/Kenney treatment; no shared toon/cel ramp exists. |
| Art compliance | No stray dark debug plane | Pass | Fresh captures show no isolated dark plane; shrine/fountain and hero remain named scene objects. |
| Moodboard | The map reads warm, medieval/magical and intentionally low-poly | Pass | Compared fresh captures with the five Greenward references: warm path, brass shrine/lantern light, castle, runes, smoke and restrained motes now establish the intended foundation read. It remains intentionally less content-dense than the references. |
| Visual v0 | Kenney character/environment assets and lazy loader remain wired | Pass | `kenneyAssets.test.ts` passes; current browser captures show the loaded castle, trees, rocks, fences, fountain and character. |
| Visual v0 | Hero basic and special attacks resolve, cool down and have visible weight | Pass | `stageSimulation.test.ts` covers basic/special damage and coin creation; fresh `K` capture `.playwright-cli/page-2026-09-08T18-11-37-165Z.png` shows the large special shockwave and 3.2s cooldown. |
| Visual v0 | Shared S-shaped route reaches the castle | Pass | Current default and orbit captures visibly follow the multi-turn route; simulation route tests pass. |
| Visual v0 | Remaining proxies receive a shared final treatment | Fail | Towers and combat remain simplified geometry; the common toon/cel material ramp is still absent. |
| Final-product gap | Coin drops and hero-only pickup economy | Pass | Simulation tests cover drop/pickup; fresh Endless browser play placed towers, completed wave 1, then changed gold from 0 to 40 after approaching drops (`.playwright-cli/page-2026-09-08T17-59-03-842Z.png`). |
| Final-product gap | Endless mode is played through beyond one wave | Pass | Fresh session browser run completed waves 1 and 2 with Heart 20 retained (`.playwright-cli/page-2026-09-08T17-59-36-258Z.png`). |
| Final-product gap | Skill tree persists through reload | Pass | New `skillProgress.test.ts` passes; fresh browser purchase of Royal Oath reduced points to 4 and, after reload, the tree retained “Royal Oath ✓” and 4 points. |
| Atmosphere | Ambient motion | Pass | `updateAmbient()` drives tree wind, flame flicker and smoke drift; current default/orbit captures show the live particle/smoke layer. |
| Atmosphere | Ambient particles | Pass | Twelve phased motes are created by `createAmbientMotes()`; the fresh default/orbit captures show their restrained brass motion. |
| Atmosphere | Warm lighting mood | Pass | Current scene raises exposure to 0.8, uses the warm key, a brighter shrine and warm lantern lights; fresh final desktop/mobile captures show the brass glow pooling around the shrine and route. |
| Atmosphere | Signs of habitation | Pass | Castle, lanterns, chimney smoke, fencing and fountain are visible in the fresh captures; smoke continues through `createSmokePuffs()`. |
| Atmosphere | Optional ambient audio | Not-yet-started | Deliberately omitted under the non-blocking stretch clause. |
| Performance | A1: initial gzip JavaScript is below 1 MB | Pass | Final build: largest entry 396.39 kB gzip; supporting chunks 163.40 kB and 58.93 kB gzip. Vite’s >500 kB minified-chunk warning remains. |
| Performance | A2: representative desktop/integrated frame budget is recorded | Fail | `window.__TD_PERF__` instrumentation exists and gave a local smoke sample, but no representative 60 fps desktop / 30 fps integrated benchmark has been recorded. |
| Performance | A3: enemy and coin visual pooling | Pass | `src/main.ts` now returns removed enemy, character and coin visuals to pools and re-enables them on spawn; a `ponytail:` ceiling comment records the required future cap. |
| Performance | A4: static instancing is measured and verified | Fail | Repeated static content is not yet backed by an explicit instancing/draw-call measurement. |
| Performance | A5: gore decal cap and oldest-first eviction | Not-yet-started | Gore is still intentionally deferred; no decal pool/cap exists. |
| Performance | A6: automated bundle/frame budget checks | Fail | Build size is manually verified this session; CI assertions do not exist. |
| Co-op boundary | Realtime bridge stays TD-scoped and secret-free | Pass | `supabaseRealtimeBridge.test.ts` passes (5 tests); source remains Broadcast/Presence-only with public client configuration. |
| Co-op boundary | Authoritative join/host/snapshot/reconnect flow | Not-yet-started | The documented next co-op pass remains unimplemented. |

### Completed during this session

- Corrected the stale coin test fixture exposed by the expanded path, then added focused simulation coverage for basic and special hero attacks.
- Added real hero damage resolution, kill coins, cooldown-backed attack effects and a visible special shockwave; the initial fresh screenshot caught the edge-on/invisible ring regression, which was fixed before acceptance.
- Persisted purchased skill IDs and remaining points in local storage, with a focused reload test and browser reload proof.
- Added/reused pools for fallback enemies, Kenney enemy characters and coins.
- Reworked the current composition after fresh comparison: a horizon ground underlay removes the old diagonal edge; the castle is closer and correctly scaled; camera framing is tighter; shrine/lantern warmth, motes, smoke and a focal glow are visible in the final captures.

### Open work in required priority order

1. Finish the shared environment/creature toon-cel ramp and replace the remaining tower/combat proxy treatment.
2. Record representative desktop/integrated frame measurements, add explicit static-instance/draw-call evidence, and add automated performance-budget checks. Gore decal pooling/cap starts only when the deferred gore feature starts.
3. Only after those rows pass, resume the later roadmap: broader asset sourcing, creature shading, gore/dismemberment, first-win confetti, co-op authority and Steam preparation.
