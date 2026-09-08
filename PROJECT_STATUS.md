# TD Annihilation — project status

Last updated: 2026-09-08  
Current branch: `codex/phase-0-foundation`  
Base: `origin/main` at `042c5e9`

## Current milestone

Phase 0 — Foundation design approved; implementation not started yet.

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

## What is next

1. Review and commit this design/status baseline.
2. Create the implementation plan for Phase 0.
3. Scaffold the Vite/Babylon application with tests for pure scene configuration.
4. Build and manually inspect the composed battlefield scene and camera interactions.
5. Update this file with exact verification results and the next phase.

## Verification

Not run yet; no application files exist in the repository.

## Known limitations

- No gameplay systems exist yet.
- No final art, audio or VFX assets exist yet.
- Supabase and Steamworks are intentionally not connected in Phase 0.
- The visual companion reference screens live under ignored `.superpowers/` files and are not part of the game.

