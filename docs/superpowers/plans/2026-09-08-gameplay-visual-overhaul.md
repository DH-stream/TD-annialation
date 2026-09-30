# TD Annihilation — Gameplay & Visual Overhaul plan

> Execute the smallest verified slice first. “Done” requires a fresh test or actual-play artifact, not a historical status row.

## Baseline

- Branch: `codex/v1-playable-shell`
- Starting commit: `983df2f`
- Baseline on 2026-09-08: `npm test -- --run` — 36 tests pass; `npm run build` — passes, with an existing large-chunk warning.
- Reconciliation: fortress/freeform still present; generic perpetual-walk enemy renderer does not meet the new monster requirement.

## Slice 1 — state-driven monster trio

1. Add failing pure-simulation tests for deterministic archetype mixes, hit/death retention, and a gate attack telegraph.
2. Add the smallest lifecycle state to `EnemyState`, plus the three concrete archetype definitions and deterministic spawns.
3. Update hero/tower damage to transition to hit/dying and award coins once. Advance lifecycle timers before moving/targeting. Confirm the tests turn green.
4. Add the three CC0 Quaternius models and license notice.
5. Add a small monster asset helper parallel to the existing Kenney helper; map semantic lifecycle states to actual imported animation names.
6. Replace the single enemy renderer/pool with per-kind pools. Orient visuals from real path displacement and play state transitions only once.
7. Run unit tests and build. Launch the real game and record a wave containing all three kinds and their walk, hit, death, and gate attack states.

## Slice 2 — remaining playable core

1. Add one contrasting tower choice using the existing build interaction, only after a test defines the choice and cost/range/damage behavior.
2. Play through multiple waves, then endless mode. Capture tower fire, hero basic/special, coin drop and pickup, and the next-wave decision.
3. Verify skill persistence across reload.

## Slice 3 — feel and cohesion

1. Improve impact/recovery, hero readability, coin attraction and wave timing from actual-play observations.
2. Improve terrain depth, ambient life, lighting and castle context without reducing playfield readability.
3. Measure a representative combat frame time and record the result; address the existing bundle warning only if measurement or budget requires it.

## Slice 4 — extensibility checks

1. Separate hero skin selection from simulation and add one alternate visual skin behind a small developer control.
2. Build and verify a working mounted variant only after core combat is solid.
3. Add regression checks for endless, progression persistence, mobile viewport and real combat performance.

## Acceptance evidence for Slice 1

- Unit test output proving archetype order, hit → death retention/coin behavior, and gate attack timing.
- Fresh desktop and mobile gameplay captures showing the real camera, all three distinct silhouettes, and a completed placement/wave interaction.
- Fresh `npm test -- --run` and `npm run build` output.
- A `PROJECT_STATUS.md` entry which distinguishes verified work from open work.
