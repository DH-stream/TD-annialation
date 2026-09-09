# Greenward Valley Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the compact floating arena with a 90-by-60-metre inhabited Greenward valley and make the Demon Walk animation loop continuously during path traversal.

**Architecture:** Keep gameplay state in the existing `TDVerticalSliceBootstrap` and move world composition into focused layout, world-builder, ambient-motion, camera, and animation-import units. Geometry and landmarks are deterministic so EditMode tests can validate scale and route length, while Unity Play Mode and gameplay-camera captures remain the authority for visual and animation approval.

**Tech Stack:** Unity 6000.6.0f1, C#, URP 17.6, Unity Animator/ModelImporter, Unity Test Framework, existing Quaternius CC0 FBX assets, additional free Quaternius CC0 Medieval Village and Stylized Nature assets when their free downloads import cleanly.

## Global Constraints

- Use free assets only and preserve source and license evidence under `Assets/Art/ThirdParty`.
- Do not modify packages or replace the existing Babylon/Vite application.
- Keep the work in `codex/unity-vertical-slice`; do not merge to `main`.
- The normal gameplay camera must never expose a floating plane or black void.
- Gameplay movement remains authoritative; enemy root motion stays disabled.
- All visual approval must use actual Play Mode captures from the normal gameplay camera.

---

### Task 1: Import and document the free environment palette

**Files:**
- Modify: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Art/ThirdParty/README.md`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Art/ThirdParty/Quaternius/MedievalVillage/License.txt`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Art/ThirdParty/Quaternius/StylizedNature/License.txt`
- Import: selected FBX and texture files under the two folders above

**Interfaces:**
- Consumes: official Quaternius Medieval Village MegaKit and Stylized Nature MegaKit free-download pages.
- Produces: Unity-indexed environment models and textures discoverable under `Assets/Art/ThirdParty/Quaternius`.

- [ ] **Step 1: Download only the free editions from the official source pages**

Use:

```text
https://quaternius.com/packs/medievalvillagemegakit.html
https://quaternius.com/packs/stylizednaturemegakit.html
```

Do not claim or purchase Source/Pro editions. Preserve each supplied license file unchanged.

- [ ] **Step 2: Import and inventory the assets**

Copy the free FBX files, textures, and licenses into the exact folders above, force an Unity asset refresh, then search those folders with previews disabled. Expected: FBX assets appear as `UnityEngine.GameObject`, textures appear as `Texture2D`, and the Console contains no importer errors.

- [ ] **Step 3: Record source, license, and selected visual roles**

Append entries to `Assets/Art/ThirdParty/README.md` identifying the official source URL, `CC0 1.0`, import date, and intended roles: village architecture/props and trees/rocks/plants.

- [ ] **Step 4: Commit the repository-side evidence update**

The external Unity project is not currently the Git worktree, so update `PROJECT_STATUS.md` only after Play Mode validation and commit that evidence with the final task.

---

### Task 2: Define deterministic valley geography and route

**Files:**
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardWorldLayout.cs`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/TDAnnihilation.Runtime.asmdef`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Tests/EditMode/GreenwardWorldLayoutTests.cs`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Tests/EditMode/TDAnnihilation.EditModeTests.asmdef`

**Interfaces:**
- Produces: `GreenwardWorldLayout.Width`, `Depth`, `HeightAt(float x, float z)`, `CreateRoute()`, and landmark positions used by the world builder, enemy movement, and camera.
- Consumes: no scene objects or assets.

- [ ] **Step 1: Write failing layout tests**

Create the runtime assembly definition:

```json
{
  "name": "TDAnnihilation.Runtime",
  "rootNamespace": "TDAnnihilation",
  "references": [],
  "autoReferenced": true
}
```

Create the EditMode test assembly definition:

```json
{
  "name": "TDAnnihilation.EditModeTests",
  "rootNamespace": "TDAnnihilation.Tests",
  "references": ["TDAnnihilation.Runtime"],
  "includePlatforms": ["Editor"],
  "optionalUnityReferences": ["TestAssemblies"],
  "autoReferenced": false
}
```

```csharp
using NUnit.Framework;
using UnityEngine;

namespace TDAnnihilation.Tests
{
    public sealed class GreenwardWorldLayoutTests
    {
        [Test]
        public void RouteCrossesLargeValleyAndContainsStrategicTurns()
        {
            Vector3[] route = GreenwardWorldLayout.CreateRoute();
            float length = 0f;
            for (int i = 1; i < route.Length; i++) length += Vector3.Distance(route[i - 1], route[i]);
            Assert.That(route.Length, Is.GreaterThanOrEqualTo(10));
            Assert.That(length, Is.GreaterThan(70f));
            Assert.That(route[0].x, Is.LessThan(-35f));
            Assert.That(route[^1].x, Is.GreaterThan(35f));
        }

        [Test]
        public void TerrainHasHeightVariationAndRouteSitsAboveGround()
        {
            Assert.That(GreenwardWorldLayout.HeightAt(35f, 8f) - GreenwardWorldLayout.HeightAt(-35f, -8f), Is.GreaterThan(2f));
            foreach (Vector3 point in GreenwardWorldLayout.CreateRoute())
                Assert.That(point.y, Is.GreaterThanOrEqualTo(GreenwardWorldLayout.HeightAt(point.x, point.z)));
        }
    }
}
```

- [ ] **Step 2: Run EditMode tests and confirm the red state**

Run the two tests through Unity Test Framework. Expected: compile failure because `GreenwardWorldLayout` does not exist.

- [ ] **Step 3: Implement the layout contract**

```csharp
using UnityEngine;

namespace TDAnnihilation
{
    public static class GreenwardWorldLayout
    {
        public const float Width = 96f;
        public const float Depth = 64f;
        public static readonly Vector3 Portal = new Vector3(-41f, 1.2f, -11f);
        public static readonly Vector3 Village = new Vector3(0f, 1.8f, 8f);
        public static readonly Vector3 Castle = new Vector3(41f, 5.4f, 8f);

        public static float HeightAt(float x, float z)
        {
            float eastRise = Mathf.InverseLerp(-48f, 48f, x) * 4.5f;
            float valley = -Mathf.Exp(-(z * z) / 170f) * 1.1f;
            float hills = Mathf.Sin(x * 0.105f) * 0.55f + Mathf.Cos(z * 0.16f) * 0.45f;
            return eastRise + valley + hills;
        }

        public static Vector3[] CreateRoute()
        {
            Vector2[] points = {
                new Vector2(-43f, -11f), new Vector2(-35f, -5f), new Vector2(-28f, 4f),
                new Vector2(-18f, 10f), new Vector2(-8f, 7f), new Vector2(1f, 1f),
                new Vector2(11f, -3f), new Vector2(21f, 2f), new Vector2(29f, 10f),
                new Vector2(38f, 9f), new Vector2(43f, 8f)
            };
            Vector3[] route = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
                route[i] = new Vector3(points[i].x, HeightAt(points[i].x, points[i].y) + 0.3f, points[i].y);
            return route;
        }
    }
}
```

- [ ] **Step 4: Run the tests and verify green**

Run the same EditMode filter. Expected: 2 passed, 0 failed.

---

### Task 3: Build the continuous terrain and world districts

**Files:**
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardWorldBuilder.cs`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardAmbientMotion.cs`
- Modify: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs`

**Interfaces:**
- Consumes: `GreenwardWorldLayout` and optional imported environment prefabs.
- Produces: `Greenward World` root containing `Terrain`, `Road`, `Corrupted Lowlands`, `Village`, `Castle`, `Forest Boundary`, `Water`, and `Ambient Life` children.

- [ ] **Step 1: Create a reusable world-builder entry point**

```csharp
namespace TDAnnihilation
{
    public static class GreenwardWorldBuilder
    {
        public static Transform Build(Vector3[] route)
        {
            var root = new GameObject("Greenward World").transform;
            BuildTerrain(root);
            BuildWaterAndBridge(root);
            BuildRoad(root, route);
            BuildCorruptedLowlands(root);
            BuildVillage(root);
            BuildCastle(root);
            BuildForestBoundary(root);
            BuildAmbientLife(root);
            return root;
        }
    }
}
```

Implement `BuildTerrain` as a 49-by-33 vertex mesh spanning `GreenwardWorldLayout.Width` and `Depth`; sample `HeightAt` for every vertex, recalculate normals and bounds, and use a warm grass URP material. Extend terrain beyond the active route so the camera sees landscape rather than a platform edge.

- [ ] **Step 2: Create a road ribbon that follows terrain elevation**

For each route segment, generate left/right edge vertices at a 2.2-metre half-width, sample terrain height, add slightly irregular lateral offsets, and connect triangles. Add darker shoulder ribbons beneath the main road to create worn edges rather than rectangular brown blocks.

- [ ] **Step 3: Construct the three authored districts**

Implement the following fixed compositions:

```text
Corrupted Lowlands: portal arch, five ruin clusters, seven violet crystals, eight dead trees.
Village: four houses, forge, market, mill, two farm plots, bridge, fences, carts and barrels.
Castle: gatehouse, two round towers, wall runs, battlements, courtyard, six banners and four braziers.
Boundary: at least 45 trees, 20 rock clusters, river banks and four distant hill silhouettes.
```

Prefer matching imported Quaternius assets when available. Every asset lookup must have an authored geometry fallback so missing optional art does not break startup.

- [ ] **Step 4: Add localized ambient motion**

```csharp
using UnityEngine;

namespace TDAnnihilation
{
    public sealed class GreenwardAmbientMotion : MonoBehaviour
    {
        public enum Motion { Spin, Sway, Bob }
        [SerializeField] private Motion motion;
        [SerializeField] private float speed = 20f;
        [SerializeField] private float amplitude = 4f;
        private Quaternion origin;
        private Vector3 position;

        public void Configure(Motion value, float motionSpeed, float motionAmplitude)
        {
            motion = value; speed = motionSpeed; amplitude = motionAmplitude;
        }

        private void Awake() { origin = transform.localRotation; position = transform.localPosition; }
        private void Update()
        {
            if (motion == Motion.Spin) transform.Rotate(Vector3.forward, speed * Time.deltaTime, Space.Self);
            else if (motion == Motion.Sway) transform.localRotation = origin * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * speed) * amplitude);
            else transform.localPosition = position + Vector3.up * Mathf.Sin(Time.time * speed) * amplitude;
        }
    }
}
```

Use it on mill blades, banners, hanging signs, and localized magical motes. Add small Particle Systems for chimney smoke, torch embers, portal wisps, and fireflies; cap combined live ambient particles at 250.

- [ ] **Step 5: Replace compact arena construction in the bootstrap**

Replace the current five-point path and `BuildArena()` call with:

```csharp
path.AddRange(GreenwardWorldLayout.CreateRoute());
GreenwardWorldBuilder.Build(path.ToArray());
```

Move hero and tower positions to terrain-relative locations near the village and bridge. Preserve wave, damage, projectile, coin, and resource behavior.

- [ ] **Step 6: Compile and inspect the Unity Console**

Force script compilation and wait for editor readiness. Expected: zero C# errors and zero new world-builder warnings.

---

### Task 4: Fix the Demon locomotion loop and state ownership

**Files:**
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Editor/TDAnimationImportSetup.cs`
- Modify: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/TDVerticalSliceBootstrap.cs`
- Modify: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Resources/TDAnnihilation/Demon.controller`

**Interfaces:**
- Produces: looping Demon Walk import setting and `TDEnemyController` ownership of Animator state.
- Consumes: `CharacterArmature|Walk` from the Quaternius Demon FBX.

- [ ] **Step 1: Add a deterministic importer command**

```csharp
using UnityEditor;
using UnityEngine;

namespace TDAnnihilation.Editor
{
    public static class TDAnimationImportSetup
    {
        [MenuItem("TD Annihilation/Configure Demon Animation")]
        public static void ConfigureDemon()
        {
            const string path = "Assets/Art/ThirdParty/Quaternius/Monsters/Demon.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                if (!clips[i].name.EndsWith("|Walk")) continue;
                clips[i].loopTime = true;
                clips[i].loopPose = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            Debug.Log("TD Annihilation: Demon Walk configured to loop.");
        }
    }
}
```

- [ ] **Step 2: Rebuild the Demon controller after reimport**

Create a default state named `Walk` using `CharacterArmature|Walk`, set the controller on spawned Demons, set `Animator.applyRootMotion = false`, and call `animator.Play("Walk", 0, 0f)` after configuration.

- [ ] **Step 3: Keep Animator state aligned with gameplay movement**

Store the child Animator in `TDEnemyController.Configure`. While path traversal is active, ensure the active state remains `Walk`; later attack/death work may take ownership through explicit states, but this pass must not restart Walk each frame.

- [ ] **Step 4: Verify multiple animation cycles**

In Play Mode, inspect one Demon at two points at least four seconds apart. Expected: its position advances across waypoints, `AnimatorStateInfo.IsName("Walk")` remains true, and `normalizedTime` increases beyond `2.0` without the pose freezing.

---

### Task 5: Add strategic camera framing and validate the finished pass

**Files:**
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/TDStrategicCamera.cs`
- Modify: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scenes/SampleScene.unity`
- Modify: `C:/Users/kristoma/OneDrive - Den Hartogh Logistics/Documents/GITHUB/TowerDefense/PROJECT_STATUS.md`

**Interfaces:**
- Consumes: hero transform and Greenward world bounds.
- Produces: bounded isometric camera motion and fresh evidence artifacts.

- [ ] **Step 1: Implement bounded camera tracking**

```csharp
using UnityEngine;

namespace TDAnnihilation
{
    public sealed class TDStrategicCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 20f, -22f);
        [SerializeField] private float smoothTime = 0.35f;
        private Transform target;
        private Vector3 velocity;

        public void SetTarget(Transform value) => target = value;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 focus = target.position;
            focus.x = Mathf.Clamp(focus.x, -31f, 31f);
            focus.z = Mathf.Clamp(focus.z, -17f, 17f);
            transform.position = Vector3.SmoothDamp(transform.position, focus + offset, ref velocity, smoothTime);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
```

Assign the spawned hero through `SetTarget`, use a 52-degree perspective lens, and keep the castle or portal visible as contextual background without shrinking active combat into a miniature.

- [ ] **Step 2: Run EditMode tests**

Run the `TDAnnihilation.Tests.GreenwardWorldLayoutTests` filter. Expected: 2 passed, 0 failed.

- [ ] **Step 3: Run a Play Mode smoke test from the gameplay camera**

Exercise startup, Demon traversal, tower fire, at least one kill, coin spawn, wave continuation, camera tracking, and animation looping. Expected: no gameplay errors; no visible terrain edge or void; portal, village, bridge, mill, castle, forest, and water are identifiable.

- [ ] **Step 4: Capture final evidence**

Save normal-camera screenshots to:

```text
Assets/Screenshots/greenward_valley_gameplay.png
Assets/Screenshots/greenward_valley_portal.png
Assets/Screenshots/greenward_valley_castle.png
```

Record runtime state including enemy position, current Animator state, normalized time, defeated count, gold, lives, and active projectile count.

- [ ] **Step 5: Review performance and Console state**

Read rendering statistics during combat, record batches/triangles/frame timing when available, exit Play Mode, and confirm the final Console has no new C# or gameplay errors. Classify the known MCP `uvx`/WebSocket transport warning as environment-specific.

- [ ] **Step 6: Update status and commit evidence**

Update `PROJECT_STATUS.md` with the new screenshots, tests, runtime observations, asset sources, and remaining build limitations. Run `git diff --check`, then commit only tracked documentation changes:

```text
git add PROJECT_STATUS.md docs/superpowers/specs/2026-09-09-unity-vertical-slice-design.md docs/superpowers/plans/2026-09-09-greenward-valley-expansion.md
git commit -m "docs: record Greenward Valley validation"
```
