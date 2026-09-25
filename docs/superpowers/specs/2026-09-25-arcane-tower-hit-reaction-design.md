# Arcane Tower Attacks and Enemy Hit Reaction

## Goal

Make the first tower attack readable and impactful, then make enemies visibly react when they take damage.

## Scope

### Arcane projectile

- Keep the existing single arcane tower as the first MVP tower.
- Replace the plain sphere projectile with a glowing energy core, a trailing magical particle effect, and a short impact burst.
- Give the projectile weight through acceleration, smooth target steering, and a small controlled arc; it must remain reliable against moving enemies.
- Apply damage exactly once on impact and destroy the projectile afterward.
- Reuse Unity built-in `ParticleSystem`, `TrailRenderer`, and URP materials; add no packages or external assets.

### Enemy hit reaction

- Reuse the existing `GetHit.fbx` animation already in the project.
- Add a `Hit` trigger and a hit state to the demon Animator Controller through the existing editor authoring path.
- Trigger the hit reaction from the shared `TDEnemyController.TakeDamage()` path so tower and hero damage use the same behavior.
- Return to the current movement animation after the short hit animation; keep movement authoritative in code and root motion disabled.
- Keep defeat behavior unchanged for this pass; death animation is a later improvement.

## Files and boundaries

- Extend `TDVerticalSliceBootstrap.cs` only where the current runtime tower, projectile, and enemy classes already live.
- Extend `GreenwardSceneAuthoring.cs` to author the `Hit` Animator state and transition.
- Do not add a new combat framework, dependency, prefab hierarchy, or persistence layer for this pass.

## Acceptance checks

- A tower shot is visibly distinct from the current sphere and leaves a magical trail.
- The shot visibly accelerates and curves toward its target instead of appearing to teleport.
- Impact damage is applied once and the projectile is cleaned up.
- A damaged enemy visibly plays `GetHit` and then resumes walking.
- Repeated damage does not leave the enemy stuck in the hit state.
- Unity compiles without new errors and the existing gameplay loop remains playable.
