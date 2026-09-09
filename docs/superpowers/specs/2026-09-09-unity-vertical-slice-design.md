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
