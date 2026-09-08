# Hero movement implementation plan

## Goal

Make the royal hero controllable in the foundation scene without coupling local controls to future co-op transport.

## Completed

- [x] Add a pure keyboard input state with default WASD bindings.
- [x] Support remappable movement keys through a small bindings object.
- [x] Add deterministic hero movement for keyboard input and click destinations.
- [x] Give keyboard movement priority over an existing click destination.
- [x] Add map interaction metadata so future selectable entities can be excluded from movement clicks.
- [x] Add a visible destination marker for click-to-move.
- [x] Parent the hero preview meshes under one movable transform.
- [x] Verify with unit tests and a production build.

## Deliberate non-goals

- No combat, enemies, towers, path blocking or navigation mesh yet.
- No persistence or online co-op implementation yet.
- No final character art; the Phase 0 preview geometry remains intentionally temporary.

## Follow-up

- Replace direct map metadata checks with the selection/interaction model introduced in Phase 1.
- Add controller support and a user-facing keybinding screen before Steam release.
- Add collision/navigation constraints once the first playable map layout is fixed.
