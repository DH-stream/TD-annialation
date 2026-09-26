# Greenward HUD and Lighting Overhaul

## Goal

Bring the in-game presentation closer to the supplied HUD reference and improve visual depth while preserving the existing low-poly fantasy art direction, Greenward gameplay, and terrain. The Unity source of truth is `E:/TD-Annihilation-Unity-Git/UnityProject`; the separate `E:/tower defense game` web prototype is out of scope.

## HUD

- Extend the existing UI Toolkit HUD and its current visual language; do not migrate to another UI system or add a UI package.
- Redesign the lower action area around the supplied reference: three prominent attack controls, visible key hints, animated radial cooldown progress, and a readable remaining-time/ready state. Build and wave controls remain available and retain their current gameplay behavior.
- Make attack keys user-rebindable from Settings, with the current bindings as defaults. Use the installed Input System, persist overrides locally, show an explicit listening/cancel state while rebinding, prevent one key from silently occupying multiple attack actions, and ensure displayed key hints always reflect active bindings. Movement and unrelated actions are unchanged.
- Replace the minimap placeholder with a top-down, data-driven map of Greenward using the real route and playable bounds. Show the route, castle/goal, hero, towers, and live enemy positions from gameplay state. Do not use a decorative mock map or a second camera unless inspection proves the existing data cannot support an accurate view.
- Restyle the upper-right stage/defeated status into a compact, framed element consistent with the supplied reference. Keep the resource indicators and battlefield readable; avoid excessive decoration.

## Lighting and visual depth

- First inspect the active URP asset, renderer features, volume profile, scene lights, environment, and reflection setup. Existing lighting and ambient-occlusion work must be reused rather than duplicated.
- Tune only what the inspection and fixed-camera comparisons show is needed: directional key/fill balance, grounded soft shadows/AO, restrained tonemapping and bloom, subtle distance atmosphere, and reflection support.
- Preserve current grass, environment geometry/material palette, routes, towers, enemies, and combat behavior. No terrain rebuild or gameplay balance changes are part of this pass.
- Capture before/after images at matching scene, camera, resolution, and gameplay state; include a wide view and a closer view where practical. Avoid baking or global project-setting changes unless the comparison demonstrates they are required.

## Alternatives considered

1. **Extend the existing UI Toolkit and derive the minimap from gameplay data (recommended):** smallest migration, no extra package or render camera, and the map remains synchronized with the actual route/entities.
2. **Use a dedicated minimap camera/render texture:** more literal scene capture, but adds camera/render cost and needs filtering so HUD and transient effects do not leak into the map.
3. **Replace the HUD with a new Canvas or a third-party framework:** highest migration and maintenance cost without a demonstrated benefit; not selected.

## Boundaries and failure behavior

- HUD presentation reads the existing game/bootstrap state; it does not own wave, combat, placement, or resource rules.
- If minimap bounds or entity data are unavailable, keep the map frame valid and report the missing source rather than drawing an inaccurate route.
- If a requested lighting effect is unsupported or visually harmful in the current URP setup, retain the existing setting and document the limitation rather than adding a dependency or changing render pipelines.
- Input rebinding must safely cancel on Escape, handle duplicate bindings explicitly, and retain defaults if stored data is invalid.

## Verification and acceptance

1. The open editor and changes target `UnityProject/Assets/Scenes/Greenward.unity`; the separate web prototype is untouched.
2. The saved Greenward scene retains its grass and environment. Any apparent absence must be reproduced in the Game view before changing scene content.
3. The minimap route aligns with the actual Greenward route; goal, hero, towers, and live enemies track their world positions.
4. Cooldown rings/countdowns transition correctly through ready, cooling down, and ready again; key hints match Settings.
5. Each attack can be rebound, duplicate assignment is clearly handled, cancellation works, and bindings persist across restart without changing movement or attack mechanics.
6. Upper-right stage/defeated status and lower HUD match the supplied reference direction and remain legible at 1280x720 and a wider desktop resolution.
7. The visual comparison uses identical camera, scene, resolution, and gameplay state. Lighting changes improve depth without hiding grass, washing out established colors, or materially harming runtime performance.
8. Unity compiles cleanly and relevant EditMode tests pass. Record exact tests and visual evidence; do not claim unrun checks.

## Explicitly out of scope

New gameplay abilities, attack balance, enemy/tower behavior, map changes, new stages, skill-tree functionality, multiplayer, a renderer-pipeline migration, and replacing existing environment art.
