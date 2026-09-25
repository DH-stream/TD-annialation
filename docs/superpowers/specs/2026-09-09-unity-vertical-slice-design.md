# TD Annihilation Unity Vertical Slice Design

## Goal

Create an isolated Unity 6.3 LTS Greenward vertical slice that feels like a warm, chunky, heroic fantasy game closer to World of Warcraft than Minecraft or toy-like Kenney content.

## Art direction

- Use free assets only, with license evidence recorded beside the imported content.
- Prefer Quaternius CC0 fantasy characters, monsters, animation libraries, and medieval environment assets as the first candidate source.
- Treat source assets as ingredients: unify scale, palette, material response, lighting, silhouettes, and VFX so the scene reads as one world.
- Use organic, layered silhouettes and readable warm/cool color separation. Low-poly construction is acceptable; literal cubes, flat proxy grids, and asset-pack mashups are not.

## Slice scope

- Greenward arena with castle gate, raised terrain, path, cliffs, forest edge, and readable tower locations.
- One playable king with movement, basic attack, special attack, hit reaction, death, and a skin/visual swap hook.
- One polished melee enemy first, with additional fast and heavy variants only after the core state loop is stable.
- Two tower roles with visible projectiles and different targeting/damage behavior.
- Three short waves, coin drops, magnetic pickup, gold spending, and a next-wave loop.
- Developer shortcuts for spawning a wave, triggering attacks, swapping the hero skin, and resetting the slice.

## Architecture

Simulation data and rules remain separate from presentation. Wave, health, damage, coins, cooldowns, and tower targeting live in plain C# services/components with deterministic inputs. Hero, enemy, tower, VFX, camera, and UI presentation consume those states. Character visuals use Animator Controllers and skin references so cosmetic replacement does not alter input, stats, or progression.

## Verification

- Unity console is clear of compile errors after every script change.
- Scene hierarchy and required references are checked through Unity MCP.
- Play Mode verifies movement, wave start, enemy state transitions, tower fire, basic/special attack, coin pickup, and skin swap from the normal gameplay camera.
- EditMode/PlayMode tests cover wave progression, damage/cooldown rules, coin pickup, and tower targeting where the project test harness supports them.
- A Windows development build is attempted after the slice is playable; blockers are recorded rather than hidden.

## Non-goals

No paid assets, paid generation, monetization, multiplayer, full progression tree, endless content, or migration of the existing Babylon implementation until the Unity slice earns approval.

## Approved Greenward Valley expansion

The first compact runtime arena is not the target environment. Greenward must read as a continuous, inhabited fantasy valley rather than a decorated plane floating in empty space.

### Geography and composition

- Expand the playable landscape to approximately 90 by 60 metres, with scenery continuing beyond the playable boundary.
- Use cliffs, forest, water, distant hills, and atmospheric fog to conceal the edge of the map. No camera composition may expose a floating plane or black void.
- Organize the world into three connected districts:
  - western corrupted lowlands with a monster portal, ruined stones, dead trees, violet crystals, and siege debris;
  - central village district with a winding road, stream, bridge, mill, farms, market, forge, fences, and inhabited houses;
  - eastern castle high ground with retaining walls, gatehouse, towers, battlements, banners, braziers, and defensive plazas.
- Give the enemy route approximately four times the current travel length. It must curve through the geography, cross the bridge, pass the village edge, and climb toward the castle gate.
- Integrate tower locations into crossroads, bridge approaches, terraces, and castle defenses so placement opportunities appear to belong to the world.

### Art direction and atmosphere

- Progress the palette from cool violet and blue corruption near the portal, through forest green and warm timber in the village, toward gold, blue, and sunlit stone at the castle.
- Favor chunky irregular silhouettes, warm plaster, timber framing, rounded stone, moss, brass, painted cloth, and saturated magical accents.
- Use golden-hour directional lighting, soft shadows, cool valley mist, warm windows and torches, chimney smoke, moving flags, drifting leaves, restrained fireflies, and visible water movement.
- Build the scene around large readable landmarks: castle, bridge, mill, portal, village square, and ruined watchtower. Small props must reinforce these activities instead of being scattered randomly.
- Free third-party environment assets may be imported only after checking style, scale, technical quality, source, and license. Imported packs must be unified through materials, scale, lighting, and palette.

### Signs of life

- Place animated guards at the castle entrance and a small number of villagers near the market, forge, and farm.
- Add activity-specific details such as carts, produce, hay, tools, washing, barrels, and forge equipment.
- Keep ambient NPCs outside the combat route and tower-placement areas.
- Environmental movement should be localized and readable from the normal gameplay camera, not a screen-wide particle layer.

### Camera and gameplay readability

- Use an elevated isometric gameplay camera with smooth bounded hero tracking and enough strategic overview to read the next path segment.
- Do not frame the entire 90-by-60-metre world as a miniature. Nearby enemies, the hero, tower projectiles, coins, and terrain height changes must remain readable.
- Use foreground terrain, mid-ground gameplay, and background landmarks to create depth without obscuring enemies or valid tower areas.

### Demon animation correction

- Configure the imported Quaternius Demon Walk clip to loop through the Unity model importer.
- Use explicit Spawn, Walk, Hit, Attack, and Death Animator states as animation clips become connected to gameplay.
- Transition from Spawn to Walk and keep Walk active while the movement state is active.
- Keep gameplay movement authoritative and root motion disabled for path traversal.
- Validate by observing normalized animation time beyond one complete cycle while the same enemy crosses several waypoints, then repeat with a later wave.

### Acceptance criteria for this pass

- The normal gameplay camera cannot see a floating plane boundary or empty void.
- Portal, village, bridge, mill, castle, and surrounding terrain are visually identifiable without editor labels.
- The environment contains purposeful animated life and activity details while preserving path and combat readability.
- The enemy path is materially longer and creates several distinct tower-defense coverage decisions.
- A Demon continues its Walk animation through multiple loops and waypoints after spawning.
- Unity compiles without new errors, Play Mode exercises the enlarged route and tower loop, and final screenshots are captured from the real gameplay camera.
