# Arcane Tower Attacks and Enemy Hit Reaction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the plain arcane sphere with a weighted, particle-trailing projectile and make damaged enemies visibly play the existing `GetHit` animation.

**Architecture:** Keep the current runtime-generated tower, projectile, and enemy classes in `TDVerticalSliceBootstrap.cs`. Extend the existing editor authoring path in `GreenwardSceneAuthoring.cs` so the Demon controller receives a `Hit` trigger and a `GetHit` state; do not add prefabs, packages, or a new combat framework.

**Tech Stack:** Unity 6000.6.0f1, URP, C#, built-in `ParticleSystem`, `TrailRenderer`, Animator Controller authoring API, Unity Pipeline CLI.

## Global Constraints

- Reuse the existing single arcane tower and `GetHit.fbx` asset.
- Add no packages, dependencies, external assets, or persistence.
- Preserve gameplay values, tower range/fire rate, enemy health, route movement, and existing visual palette except for the requested projectile/hit feedback.
- Keep movement authoritative in code and root motion disabled.
- Do not touch the four pre-existing modified Unity settings files.

---

### Task 1: Add weighted arcane projectile feedback

**Files:**
- Modify: `UnityProject/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs` in `TDTowerController` and `TDProjectile`
- Test: existing Pipeline `run_tests` command plus a live Play Mode smoke check

**Interfaces:**
- `TDTowerController` continues selecting the nearest `TDEnemyController` and still deals `16f` damage.
- `TDProjectile.Configure(TDEnemyController enemy, float hitDamage, Color color)` owns projectile movement, trail particles, impact burst, and one-time damage.

- [x] **Step 1: Record the current baseline**

Run:

```powershell
unity command console_status --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json
unity command run_tests --project-path E:\TD-Annihilation-Unity-Git\UnityProject --mode editor --filter GreenwardGameplayPresentationTests --filter_type testName --timeout 120 --format json
```

Expected: no compile errors; the existing scale test may remain the known pre-existing failure (`Hero` is `1.5`).

- [x] **Step 2: Replace only the projectile construction call**

Keep the tower's target selection and damage unchanged, but pass the existing `projectileColor` into the projectile:

```csharp
projectile.AddComponent<TDProjectile>().Configure(nearest, 16f, projectileColor);
```

- [x] **Step 3: Add the minimum projectile visuals**

Keep the existing core sphere, then add a `TrailRenderer` and child `ParticleSystem` using URP materials created by the existing local material helper. Configure the trail and particles from `projectileColor`; use `StopAction.Destroy` for the impact burst so no effect object remains in the scene.

- [x] **Step 4: Add weighted movement without changing targeting rules**

Start at a low speed, accelerate toward a capped speed, steer toward `target.transform.position + Vector3.up`, and add a small vertical arc. Preserve the existing impact distance threshold and guard the damage call with a private `damageApplied` flag.

- [x] **Step 5: Recompile and smoke-test**

Run:

```powershell
unity recompile --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json
unity command editor_play --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json
```

Observe one wave from the normal gameplay camera: the bolt has a visible trailing effect, does not teleport, reaches an enemy, applies one hit, and cleans itself up. Stop Play Mode after the check.

- [x] **Step 6: Commit the isolated projectile change**

```powershell
git add -- UnityProject/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs
git commit -m "Improve arcane projectile feedback"
```

### Task 2: Add the enemy hit animation

**Files:**
- Modify: `UnityProject/Assets/Editor/GreenwardSceneAuthoring.cs`
- Modify: `UnityProject/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs` in `TDEnemyController`
- Test: existing Pipeline EditMode tests and live damage smoke check

**Interfaces:**
- `GreenwardSceneAuthoring.EnsureAnimationControllers()` authors `Assets/Resources/TDAnnihilation/Demon.controller` with `Walk`, `Hit`, a `Hit` trigger, and a return transition to `Walk`.
- `TDEnemyController.TakeDamage(float amount)` remains the shared damage entry point and triggers `Hit` when the parameter exists.

- [x] **Step 1: Author the Demon controller through Unity’s API**

Add a Demon-specific authoring method called by `EnsureAnimationControllers()`. Load the existing Demon walk clip from `Assets/Art/ThirdParty/Quaternius/Monsters/Demon.fbx`, load `GetHit` from `Assets/Art/ThirdParty/Blink/Art/Animations/Animations_Starter_Pack/Combat/GetHit.fbx`, add the `Hit` trigger, create the `Walk` and `Hit` states, add an Any State → Hit trigger transition, and add a Hit → Walk exit-time transition. Save the controller with `AssetDatabase.SaveAssets()`.

- [x] **Step 2: Trigger the animation from the shared damage path**

Cache whether the child Animator has a `Hit` trigger during `Configure()`. In `TakeDamage()`, reset and set that trigger before the existing health/defeat logic; do not change damage amounts, death behavior, route movement, or coin spawning.

```csharp
if (hasHitTrigger)
{
    animator.ResetTrigger("Hit");
    animator.SetTrigger("Hit");
}
```

- [x] **Step 3: Recompile and run tests**

Run:

```powershell
unity recompile --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json
unity command run_tests --project-path E:\TD-Annihilation-Unity-Git\UnityProject --mode editor --filter GreenwardGameplayPresentationTests --filter_type testName --timeout 120 --format json
```

Expected: no compile errors; the known scale assertion remains the only existing test failure unless it is independently fixed later.

- [x] **Step 4: Smoke-test repeated damage**

Run the wave from the normal gameplay camera and confirm a hit enemy plays `GetHit`, resumes `Walk`, continues along the route, and does not remain stuck when struck again.

- [x] **Step 5: Commit the isolated hit-reaction change**

```powershell
git add -- UnityProject/Assets/Editor/GreenwardSceneAuthoring.cs UnityProject/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs
git commit -m "Add enemy hit reaction animation"
```

## Final verification

- [x] `unity recompile --project-path E:\TD-Annihilation-Unity-Git\UnityProject --format json` reports `errors: 0`.
- [x] Pipeline `console_status` reports `consoleErrors: 0` and `compilationFailed: false`.
- [x] Existing EditMode tests are reported with the known scale assertion called out separately.
- [x] `git diff --check` passes for the implementation commits.
- [x] The four pre-existing Unity settings modifications remain uncommitted and untouched.
