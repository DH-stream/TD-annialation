# TD Annihilation — Visual Style Foundation

## Purpose

This document turns the visual feedback on the Phase 0 scene into an explicit target that can be checked before future art-polish work is accepted. The target is charming low-poly fantasy: intentional chunky silhouettes, rich but disciplined color, soft readable lighting and enough surface character to avoid raw primitive geometry.

## North star

The battlefield should feel like a warm, stylized fantasy board that could belong beside Dungeon Defenders, while borrowing the approachable low-poly discipline of Synty POLYGON, Kenney packs and Quaternius. A Short Hike is a reference for the softer lighting, painterly horizon and limited scene palette.

References:

- [Synty POLYGON](https://syntystore.com/collections/polygon)
- [Kenney](https://kenney.nl/assets)
- [Quaternius](https://quaternius.com/)
- [A Short Hike](https://ashorthike.com/)
- [Dungeon Defenders](https://store.steampowered.com/app/65800/Dungeon_Defenders/)

## Locked direction for the Greenward map

- Display title font: Metal Mania, bundled locally under SIL Open Font License 1.1.
- UI body font: a restrained readable system/UI stack until the final UI type system is selected.
- Palette: seven shared anchors only. Current map materials must draw from these values or from controlled lightness/roughness variants of them.

| Role | Hex |
| --- | --- |
| Deep horizon | `#142523` |
| Forest ground | `#3F5D47` |
| Warm path | `#B98A57` |
| Charcoal stone | `#59676D` |
| Dark wood | `#7B4B34` |
| Royal brass / magic highlight | `#D2A85B` |
| Stylized blood | `#8E2F2B` |

## Art QA rubric

Every visual pass is checked against these rules:

1. No bare flat void backgrounds. Use a gradient skybox or a coherent horizon/fog treatment.
2. Lock the map palette before adding geometry; do not introduce ad-hoc material colors.
3. Soften hard edges on hero-visible geometry. Prefer bevelled/rounded pack assets; where primitives remain, use edge treatment and softened silhouettes.
4. Use soft shadows and ambient/fill light; ambient occlusion is enabled when the renderer supports it.
5. Run a scale/silhouette pass. Gameplay-important objects must read at their intended importance, and trees must not overpower the castle.
6. Add controlled surface variation through material roughness, vertex gradients or restrained texture/noise; avoid pure unstyled single-color primitives.
7. Keep one shading language across environment and blocky mobs: chunky silhouettes with soft, readable stylized lighting.

## Map scale and layout

The current battlefield is a foundation-scale test board and is too small for the intended action tower-defense view. The next scene pass scales the playable board and composition together, increases the default camera framing, and leaves room for future waves, tower clusters and hero traversal. Scaling must not be a cosmetic zoom-only change: walkable space, landmark spacing and click-to-move bounds must grow with it.

The dark rectangular object near the shrine is not allowed to remain ambiguous. The hero preview must read as a deliberate chunky character silhouette; any remaining unowned plane is removed and covered by a browser screenshot check.

## Runtime and quality constraints

- Keep the scene data-driven through `SceneConfig`.
- Keep movement/input contracts independent from rendering and future network transport.
- Bundle font assets locally for offline Steam/Tauri builds; include the font license in the repository.
- Use the art QA rubric in `PROJECT_STATUS.md` alongside automated tests and browser smoke checks.
- This pass improves the foundation slice; it does not claim final production art, animation, VFX or asset sourcing are complete.
