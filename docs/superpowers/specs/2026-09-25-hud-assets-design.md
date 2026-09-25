# Greenward HUD Asset Integration Design

Date: 2026-09-25
Branch: codex/unity-vertical-slice

## Goal

Use the supplied free HUD graphics as the visual language for the gameplay HUD while keeping the current Greenward interaction model and runtime values intact.

## Layout

- Top-left status stack: Coins, Mana and Health remain visible during gameplay.
- Bottom-center command deck: Attack is the primary action, with Heavy Attack and Mega Attack as secondary actions.
- Existing Build and attack-type controls remain in the command deck, but are visually subordinate to the supplied action assets.
- The HUD remains rectangular and authored; no additional pill, badge or chip styling is introduced.

## Asset mapping

| HUD function | Supplied asset | Runtime value |
| --- | --- | --- |
| Attack | attack panel | player attack/action state |
| Coins | coins panel | `TDResourceState.gold` |
| Mana | mana panel | initial UI value until mana gameplay exists |
| Health | health panel | hero health value when available; otherwise current lives as the defensive value |
| Heavy Attack | heavy attack panel | locked/placeholder action state until the ability is implemented |
| Mega Attack | mega attack panel | locked/placeholder action state until the ability is implemented |

## Text treatment

- Use the asset's existing all-caps title as the label; do not duplicate it with a second text label.
- Put dynamic values inside the empty bar area using a compact serif-like/gold treatment that matches the asset palette.
- Keep values short (`120`, `FULL`, `LOCKED`) and center them inside the bar so they remain readable at 16:9 and lower resolutions.
- Use a subdued dark shadow/outline only where needed for contrast against the bright frame.

## Interaction

- Attack remains the existing primary player attack path.
- Heavy Attack and Mega Attack are visible but disabled until their gameplay systems exist; disabled state uses reduced opacity and does not intercept the existing attack input.
- Coins, Mana and Health are display-only status panels.
- Build/radial selection keeps its current behavior and is not replaced by these assets.

## Implementation boundary

- Add the six provided PNGs under `Assets/Resources/TDAnnihilation/UI/HUD/` with stable resource names.
- Extend the existing `GreenwardRoot.uxml`, `Greenward.uss` and `TDVerticalSliceHUD` only; do not introduce a new UI framework or runtime dependency.
- Preserve the current menu, scenic camera, tower placement animation and user-owned ProjectSettings changes.
- If the current runtime has no mana or hero-health source, use an explicit display fallback rather than inventing a new gameplay system in this pass.

## Verification

- Validate all six assets import and have usable alpha/background treatment.
- Parse UXML and verify all new element names.
- Run Unity recompile and sequential runtime/test/editor C# builds.
- Attempt the Unity EditMode test command; report the existing open-Editor/Pipeline limitation if it prevents execution.
- Play Mode visual review remains the final acceptance step when the connected Unity Editor is available.
