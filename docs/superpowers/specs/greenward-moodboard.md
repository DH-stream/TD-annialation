# Greenward — per-map moodboard

This moodboard is the visual gate for the Greenward foundation and the first playable map. These are reference images and direction studies only; they are not assets to ship. The map keeps the richer medieval/magical readability of Kingdom Rush and Dungeon Defenders, with the softer low-poly atmosphere and palette discipline of A Short Hike and Kenney.

## Reference images

![Kingdom Rush — readable winding path and tower composition](https://www.kingdomrush.com/images/kingdom-rush/features/kr-tower_1.png)

![Kingdom Rush — saturated magic against a restrained environment](https://www.kingdomrush.com/images/kingdom-rush/features/kr-tower_2.png)

![Kingdom Rush — chunky medieval silhouettes and clear gameplay landmarks](https://www.kingdomrush.com/images/kingdom-rush/features/kr-tower_3.png)

![A Short Hike — warm natural palette and soft atmospheric depth](https://assets.topics.apps-jp.nintendo.com/image/2020/08/19062448264478/0/04.jpg)

![Kenney — disciplined, charming shape language and color blocking](https://kenney.nl/media/pages/assets/new-platformer-pack/dd87396174-1746559490/sample-a.png)

Source pages: [Kingdom Rush](https://www.kingdomrush.com/kingdom-rush), [A Short Hike press kit](https://ashorthike.com/press/), [Kenney Retro Fantasy Kit](https://www.kenney.nl/assets/retro-fantasy-kit), [Quaternius free assets](https://quaternius.com/).

## Locked Greenward palette

| Role | Hex | Use |
| --- | --- | --- |
| Deep horizon | `#142523` | Fog/sky boundary and deepest UI contrast |
| Forest ground | `#3F5D47` | Walkable board and foliage base |
| Warm path | `#B98A57` | Enemy route and warm navigation signal |
| Charcoal stone | `#59676D` | Castle, shrine and build-pad bases |
| Dark wood | `#7B4B34` | Barricades, torch poles and hero body |
| Royal brass / magic highlight | `#D2A85B` | Trim, runes, flames and readable interaction glow |
| Stylized blood | `#8E2F2B` | Future restrained gore layer only |

No scene material may introduce an eighth color. Lighting and vertex gradients may vary value/roughness, but hue identity stays inside this palette.

## Visual decisions

- Chunky silhouettes with chamfered corners and rounded hero forms; no raw 90-degree hero-visible boxes.
- Medieval details are functional readability cues: torch-lit castle, rune rings on build pads, shrine glow and future banners/stone trim.
- Magic is a repeated layer, not sparkle spam: emissive brass runes and soft auras against the restrained green/stone environment.
- Surface depth uses controlled vertex light gradients now; authored hand-painted texture atlases are the later asset-pack step.
- Future creatures use the same toon/cel ramp as the environment and get their hook from proportion and saturated magic color, not voxel-vs-realistic rendering contrast.

## QA gate

Before a Greenward visual milestone is accepted, compare a browser screenshot against this board and record the judgment in `PROJECT_STATUS.md`: the map must feel warm, readable, medieval/magical and intentionally low-poly rather than like unstyled primitives.
