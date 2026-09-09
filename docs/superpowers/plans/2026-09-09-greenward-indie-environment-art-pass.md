# Greenward Indie Environment Art Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn Greenward Valley into a cohesive stylized indie-quality game environment with readable gravel, real grass geometry, varied surfaces, authored architecture, and believable URP lighting using only free solutions.

**Architecture:** Preserve the deterministic `GreenwardWorldLayout` and gameplay bootstrap while decomposing the minified world builder into focused material, terrain-dressing, architecture, and lighting builders. Use shared URP materials and generated world-space textures for reliable self-contained rendering, supplemented by selectively imported CC0 models only when they improve silhouettes.

**Tech Stack:** Unity 6000.6.0f1, URP 17.6.0, C#, Unity Mesh API, URP Lit materials, Volume post-processing, Unity Test Framework, optional CC0 Quaternius models and Poly Haven textures.

## Global Constraints

- All imported assets and tools must be free for commercial use.
- Store source URL and license beside every imported asset group.
- Preserve the 96x64m world, 96.7m route, gameplay, HUD, waves, towers, and continuous Demon Walk.
- Do not add packages, switch render pipelines, hand-edit scene YAML, or introduce paid content.
- A fresh normal-gameplay capture is the final visual gate; compilation and object counts are insufficient.
- No region larger than a typical building footprint may read as one uniform color field.
- The style is warm, chunky, organic, World of Warcraft-adjacent fantasy—not Minecraft, Kenney, photorealism, or copied Warcraft assets.

---

### Task 1: Establish deterministic environment-quality tests

**Files:**
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Tests/EditMode/GreenwardEnvironmentTests.cs`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Tests/EditMode/TDAnnihilation.EditModeTests.asmdef`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/TDAnnihilation.Runtime.asmdef`
- Modify: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardWorldLayout.cs`

**Interfaces:**
- Consumes: `GreenwardWorldLayout.CreateRoute()`, `HeightAt(float,float)`, and `RouteLength(IReadOnlyList<Vector3>)`.
- Produces: `GreenwardWorldLayout.IsRoad(Vector2,float)`, `SurfaceRegionAt(Vector2)`, and deterministic sampling contracts used by Tasks 2–4.

- [ ] **Step 1: Add runtime and EditMode test assembly definitions**

Create `TDAnnihilation.Runtime.asmdef` with name `TDAnnihilation.Runtime` and root namespace `TDAnnihilation`. Create the Editor-only test assembly referencing `TDAnnihilation.Runtime` and `UnityEngine.TestRunner`/`UnityEditor.TestRunner`, with `optionalUnityReferences: ["TestAssemblies"]`.

- [ ] **Step 2: Write failing layout tests**

Test exact invariants:

```csharp
[Test]
public void RouteRemainsLongAndInsideWorld()
{
    var route = GreenwardWorldLayout.CreateRoute();
    Assert.That(route, Has.Count.EqualTo(11));
    Assert.That(GreenwardWorldLayout.RouteLength(route), Is.GreaterThan(90f));
    Assert.That(route.All(p => Mathf.Abs(p.x) <= 48f && Mathf.Abs(p.z) <= 32f), Is.True);
}

[Test]
public void RoadMaskIncludesRouteAndRejectsVillageLawn()
{
    foreach (Vector3 point in GreenwardWorldLayout.CreateRoute())
        Assert.That(GreenwardWorldLayout.IsRoad(new Vector2(point.x, point.z), 2.2f), Is.True);
    Assert.That(GreenwardWorldLayout.IsRoad(new Vector2(3f, 16f), 1.5f), Is.False);
}
```

- [ ] **Step 3: Run EditMode tests and observe RED**

Run the Unity EditMode suite. Expected: compile failure because `IsRoad` is undefined.

- [ ] **Step 4: Implement route-distance sampling**

Implement point-to-segment distance over the XZ route and return true when distance is within the requested half-width. Add a small `SurfaceRegion` enum for Meadow, Road, Riverbank, Corruption, and Castle.

- [ ] **Step 5: Re-run EditMode tests**

Expected: all Greenward layout tests pass with no Console errors.

- [ ] **Step 6: Commit checkpoint**

```powershell
git add docs/superpowers PROJECT_STATUS.md
git commit -m "test: define Greenward environment contracts"
```

The external Unity project is not in this repository, so record verified Unity changes in `PROJECT_STATUS.md`.

---

### Task 2: Build a reusable varied-surface material system

**Files:**
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardMaterialLibrary.cs`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Editor/GreenwardTextureGenerator.cs`
- Create generated assets under: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Art/Generated/Greenward/Surfaces/`
- Modify: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardWorldBuilder.cs`

**Interfaces:**
- Consumes: deterministic world seed `731`.
- Produces: `GreenwardMaterialLibrary.Grass`, `Soil`, `Gravel`, `Mud`, `Stone`, `Timber`, `Plaster`, `Roof`, `Water`, and `Corruption`; each returns one cached shared `Material`.

- [ ] **Step 1: Add a texture-generator validation test**

Test that a generated 128x128 texture contains at least 24 distinct quantized colors, has nonzero luminance variance, uses Repeat wrap mode, and does not allocate a new texture when requested twice with the same key.

- [ ] **Step 2: Run tests and observe RED**

Expected: compile failure because `GreenwardTextureGenerator` and `GreenwardMaterialLibrary` do not exist.

- [ ] **Step 3: Generate authored seamless textures**

Generate albedo and normal-detail textures using layered fractal noise, cellular stone/gravel marks, color ramps, and deterministic seeds. Generate separate texture families:

- grass: olive, emerald, dry straw, and dark-soil flecks;
- gravel: pale stones, brown fines, charcoal chips, and wheel-rut darkening;
- masonry: warm/cool stone blocks with mortar and moss;
- timber: vertical grain and value bands;
- plaster: warm cream with dirt and faded ochre variation;
- roof: red-brown tiles with dark seams;
- corruption: desaturated soil with violet fissures.

Save PNG assets through the Editor API, set sRGB correctly, Repeat wrapping, bilinear filtering, and platform max size 512.

- [ ] **Step 4: Create shared URP materials**

Use `Universal Render Pipeline/Lit`; assign generated albedo/detail-normal maps, metallic 0, surface-appropriate smoothness, normal strength, and tiling. Water may use URP Lit Transparent with low alpha and high smoothness. Use one cached material per semantic surface; never call `renderer.material` in world generation.

- [ ] **Step 5: Replace the builder color dictionary**

Remove the `Dictionary<Color, Material>` and route every renderer through semantic materials. Split the 47-line minified builder into readable methods without changing gameplay ownership.

- [ ] **Step 6: Compile, inspect representative renderers, and rerun tests**

Expected: no compilation errors; road, terrain, buildings, and trees share semantic materials; generated textures meet variance tests.

- [ ] **Step 7: Commit checkpoint**

Update `PROJECT_STATUS.md`, then commit:

```powershell
git add PROJECT_STATUS.md
git commit -m "docs: record Greenward surface pipeline"
```

---

### Task 3: Replace slab roads and painted grass with authored ground dressing

**Files:**
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardGroundBuilder.cs`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardGroundScatter.cs`
- Modify: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardWorldBuilder.cs`
- Test: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Tests/EditMode/GreenwardEnvironmentTests.cs`

**Interfaces:**
- Consumes: layout road-distance and surface-region queries plus Task 2 shared materials.
- Produces: `GreenwardGroundBuilder.Build(Transform,IReadOnlyList<Vector3>)` and deterministic `GreenwardGroundScatter.CreateSamples(int)`.

- [ ] **Step 1: Write failing scatter tests**

Assert that 500 requested samples are deterministic, remain within world bounds, exclude road centers/water/building footprints, contain at least three scale buckets, and place at least 60% of samples in camera-relevant village/roadside areas.

- [ ] **Step 2: Run focused tests and observe RED**

Expected: compile failure because `GreenwardGroundScatter` is undefined.

- [ ] **Step 3: Create an organic road ribbon**

Generate one continuous mesh along the route with smoothed joins, width variation between 3.4m and 4.6m, irregular edges, terrain-following vertices, UVs aligned by traveled distance, and a gravel material. Add separate narrow meshes for two darker wheel ruts.

- [ ] **Step 4: Dress the road**

Place shared-mesh stones in three size classes, occasional shallow mud patches, grass tufts at margins, and sparse weeds. Use deterministic spacing and avoid obvious repeating intervals. Remove every rectangular `Worn Kingdom Road` cube.

- [ ] **Step 5: Create actual grass geometry**

Create one reusable crossed-blade cluster mesh and combine nearby samples into district batches. Vary scale, yaw, lean, and three material tint variants. Keep the visual budget below 24 combined grass renderers and 6,000 visible blade clusters.

- [ ] **Step 6: Shape river banks and bridge approaches**

Create bank ribbons with soil/stone transitions, shoreline rocks, reed clusters, and a slightly varied water surface. Raise/lower approach vertices so road, banks, and bridge meet without floating or intersecting slabs.

- [ ] **Step 7: Compile, test, and capture a road-focused frame**

Expected: zero new Console errors. From the gameplay camera, gravel, ruts, stones, grass blades, and irregular edges must be identifiable without labels.

- [ ] **Step 8: Commit checkpoint**

Update `PROJECT_STATUS.md`, then commit `docs: record Greenward ground dressing`.

---

### Task 4: Author architecture, foliage, and narrative prop clusters

**Files:**
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardArchitectureBuilder.cs`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardFoliageBuilder.cs`
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardPropBuilder.cs`
- Optionally import selected CC0 files under: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Art/ThirdParty/Quaternius/Environment/`
- Modify: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardWorldBuilder.cs`

**Interfaces:**
- Consumes: Task 2 semantic materials and Task 3 exclusion masks.
- Produces: focused `BuildVillage`, `BuildCastle`, `BuildCorruption`, `BuildBoundaries`, and `BuildNarrativeProps` methods.

- [ ] **Step 1: Audit candidate CC0 models before import**

Inspect silhouettes and material compatibility from official Quaternius packs. Import only assets that materially outperform current geometry. Add `LICENSE.txt` and `SOURCE.txt` beside imported files. Do not import whole packs blindly.

- [ ] **Step 2: Replace primitive houses**

Each house receives a fitted stone foundation, plaster/timber material breakup, overhanging roof, door, two or more windows, chimney, trim, and path-facing orientation. Vary footprint and roofline while preserving one architectural family.

- [ ] **Step 3: Recompose Sunspire Keep**

Scale the keep to gameplay framing; add an explicit gate opening, layered wall depth, battlements, tower roof silhouettes, courtyard hierarchy, banners, braziers, and terrain-integrated foundations. Ensure no tower is accidentally cropped at the normal camera target.

- [ ] **Step 4: Rebuild foliage silhouettes**

Use asymmetrical multi-cluster crowns, trunk taper, roots, shrubs, dead trees, reeds, flowers, and hue variation. Apply deterministic variation while maintaining designed forest masses and sightline openings.

- [ ] **Step 5: Build narrative prop clusters**

Create readable clusters rather than random scatter:

- forge: anvil, woodpile, tools, coal sacks, warm fire;
- market: stalls, crates, barrels, baskets, produce;
- mill: sacks, cart, split timber, turning wheel;
- farms: furrows, fences, scarecrow, tools;
- guard post: weapon rack, brazier, banners;
- corruption: abandoned cart, broken fence, bones, ruined supplies.

- [ ] **Step 6: Verify hierarchy and renderer sharing**

Inspect each district root. Expected: no missing scripts; semantic names; shared materials; no duplicate world roots; no large untextured cube remains as a primary building.

- [ ] **Step 7: Capture village, castle, and corruption frames**

Reject the task if any frame reads as random primitive placement, exposes map edges, or lacks foreground/midground/background separation.

- [ ] **Step 8: Commit checkpoint**

Update `PROJECT_STATUS.md`, then commit `docs: record Greenward authored environment`.

---

### Task 5: Establish production-style URP lighting and atmosphere

**Files:**
- Create: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardLightingBuilder.cs`
- Modify: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/TDStrategicCamera.cs`
- Modify: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Scripts/TD/GreenwardWorldBuilder.cs`
- Modify via Unity Editor API: scene light, Global Volume profile, reflection probe, and compatible quality settings.

**Interfaces:**
- Consumes: stable world geometry from Tasks 3–4.
- Produces: `GreenwardLightingBuilder.Configure(Camera)`, local-light helpers, and final camera composition.

- [ ] **Step 1: Capture baseline render metrics and current settings**

Record render pipeline, shadow distance/resolution, active Volume overrides, directional-light orientation/intensity/color, ambient mode, fog, renderer count, batches, triangles, and Game view aspect ratio.

- [ ] **Step 2: Configure warm key and cool fill**

Set a warm late-afternoon directional key angled across the road and village, sky-based ambient fill, soft readable shadows, stable bias, and sensible shadow distance for the gameplay camera.

- [ ] **Step 3: Add environmental light hierarchy**

Add restrained point/spot lights only at justified sources: forge, braziers, portal, arcane towers. Warm sources use orange-gold; corruption/arcane sources use violet-blue. Lights must not flatten the scene or illuminate through the entire valley.

- [ ] **Step 4: Configure atmosphere and post-processing**

Use a Global Volume with ACES tonemapping, subtle bloom, modest ambient occlusion, gentle color adjustments, vignette below obvious visibility, and fog tuned to hide boundaries while retaining route readability. Add one reflection probe centered on the playable valley.

- [ ] **Step 5: Evaluate indirect-light support**

Inspect URP asset compatibility with Adaptive Probe Volumes. If supported without package or pipeline changes, create and bake the smallest useful probe setup. If current runtime-generated geometry prevents a reliable bake, use light probes/reflection/environment lighting and document that APV requires conversion to authored scene geometry.

- [ ] **Step 6: Recompose the strategic camera**

Frame village gameplay with route, hero, defenses, and layered landmarks visible. Clamp tracking so castle and portal become intentional reveals instead of cropped background accidents. Maintain enough scale that grass, gravel, and material variation are readable.

- [ ] **Step 7: Compile and inspect lighting**

Expected: no overexposed emissives, crushed black shadows, gray fog wash, visible light leaking, or uniform ambient illumination.

- [ ] **Step 8: Commit checkpoint**

Update `PROJECT_STATUS.md`, then commit `docs: record Greenward lighting pass`.

---

### Task 6: Integrated visual and gameplay acceptance

**Files:**
- Modify as findings require: Greenward environment scripts and generated assets only.
- Modify: `C:/Users/kristoma/OneDrive - Den Hartogh Logistics/Documents/GITHUB/TowerDefense/PROJECT_STATUS.md`
- Create captures under: `C:/TD-Annihilation-Unity/TD-Annihilation-Unity/Assets/Screenshots/`

**Interfaces:**
- Consumes: all prior task outputs.
- Produces: final verified scene state and evidence.

- [ ] **Step 1: Run all EditMode tests**

Expected: all Greenward environment/layout tests pass.

- [ ] **Step 2: Start a clean Play Mode session**

Clear Console before Play. Do not compile during the session. Run for at least 20 seconds.

- [ ] **Step 3: Verify gameplay invariants**

Query runtime state. Expected:

- route count 11 and length above 90m;
- at least one enemy has reached waypoint 2 or later;
- the same enemy’s Animator normalized time exceeds 2.0;
- lives, waves, towers, projectiles, and HUD remain operational;
- no duplicate world roots.

- [ ] **Step 4: Capture normal gameplay and district views**

Capture at least:

- standard village gameplay;
- gravel-road close gameplay framing;
- Sunspire Keep approach;
- Blightfall portal approach.

Use the actual Game view aspect ratio and inspect every image visually.

- [ ] **Step 5: Apply the visual rejection checklist**

Reject and revise if:

- the ground reads as a flat color;
- the road reads as brown slabs;
- grass exists only as terrain color;
- buildings read as scaled cubes;
- props appear randomly scattered;
- castle geometry is awkwardly cropped;
- water has hard accidental intersections;
- shadows float or ambient light is flat;
- fog obscures gameplay;
- the composition exposes empty map edges.

- [ ] **Step 6: Inspect Console and rendering statistics**

Expected: zero new errors/warnings from Greenward code, no missing scripts, shared materials in representative samples, no runaway particles, and stable rendering appropriate for the current desktop prototype.

- [ ] **Step 7: Final diff and scope review**

Compare against current `main`; confirm no unrelated repository files, packages, pipeline swaps, or gameplay changes were introduced.

- [ ] **Step 8: Update status and commit**

```powershell
git add PROJECT_STATUS.md
git commit -m "docs: validate Greenward indie environment pass"
```

- [ ] **Step 9: Present final evidence**

Show the best gameplay capture, list exact Unity test/Console/runtime evidence, cite imported asset licenses and sources, and identify any remaining production-art limitations without overstating quality.
