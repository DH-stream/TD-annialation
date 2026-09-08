# TD Annihilation — v1 playable loop plan

## Goal

Turn the verified Phase 0 foundation into a coherent first-play vertical slice: a real main menu, Solo/Play with a Friend and Endless/Stages choices, a usable settings screen with remappable movement, an animated data-driven skill tree, and one playable Greenward wave loop with tower placement and win/lose feedback.

## Constraints

- Preserve the pure `PlayerInput`/simulation boundaries so online co-op can replace transport later.
- Do not claim online multiplayer in this slice; Play with a Friend is a clearly labelled local/preparation flow until Supabase/lobby transport is implemented.
- Keep Greenward's locked palette, Metal Mania display treatment and art QA rubric.
- Use deterministic, testable simulation code for enemies, towers, waves and progression rules.

## Implementation slices

1. **Art compliance handoff:** add moodboard/evidence, remove remaining palette one-offs, verify chamfered scene, desktop/mobile/browser checks. **Done.**
2. **Menu shell:** introduce typed menu state and a DOM shell with title, mode selection, Endless/Stages selection, Settings and Skill Tree routes. **Done.**
3. **Settings:** add persisted keyboard bindings with capture UI and restore defaults; feed the selected bindings into the existing input source. **Done.**
4. **Skill tree:** add data-driven node definitions and a radial DOM presentation with inner-to-outer reveal, hover states, prerequisite checks and purchase animation. **Done.**
5. **Playable Greenward loop:** add a deterministic fixed path, enemy wave state, build-pad tower placement, target/damage cadence, base health and stage/endless win/lose states. **Done for the first v1 slice.**
6. **Verification:** unit tests for menu state, bindings and simulation; production build; desktop/mobile browser flow; document the result in `PROJECT_STATUS.md`. **Done for this slice.**

## v1 completion bar

- A new user can open the app, select Solo or the explicitly labelled friend-preparation path, choose Stages or Endless, start Greenward, place at least one tower and survive/lose a wave through visible state changes.
- Settings can remap all four movement directions without a reload and persist them locally.
- Skill Tree opens with animated radial node reveal; hover gives feedback; valid purchases animate and unlock outward nodes.
- No online networking, final content breadth, gore or Steamworks integration is implied by this slice.
