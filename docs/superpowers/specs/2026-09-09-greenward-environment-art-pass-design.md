# Greenward Valley Environment Art Pass

**Date:** 2026-09-09  
**Status:** Approved direction, pending written-spec review

## Goal

Raise Greenward Valley from a readable prototype to a cohesive stylized indie-game environment. Every visible element must appear deliberately selected, authored, and integrated. The target is World of Warcraft-adjacent readability and warmth without copying its assets, literal Minecraft construction, or toy-like Kenney presentation.

All imported assets and tools must be free for commercial use. License files and source URLs must be stored beside imported content.

## Chosen approach

Use a hybrid authored pipeline:

- import a small, cohesive set of free CC0 medieval and nature assets where they outperform current primitives;
- retain custom geometry where it provides project identity;
- author project-specific terrain, road, grass, water, lighting, dressing, and district transitions;
- modify imported materials and composition so the result does not resemble an untouched asset-pack demo.

Candidate sources are Quaternius CC0 medieval/nature packs and Poly Haven CC0 ground textures. Only individual assets that fit the established chunky, organic fantasy language will be used.

## Environment composition

The existing 96x64m layout and three-district structure remain:

- **Blightfall Lowlands:** corrupted approach, portal, damaged ruins, dead vegetation, cracked ground, and magenta-violet contamination.
- **Hearthvale Village:** inhabited center with homes, forge, market, mill, farms, gardens, carts, fences, and lived-in clutter.
- **Sunspire Keep:** defensible eastern high ground with gatehouse, layered walls, towers, battlements, banners, braziers, and courtyard.

The camera must frame a polished playable neighborhood rather than reveal unfinished world edges or treat the whole map as a miniature board.

## Terrain and surface language

The ground may not use a single flat color. It will use a reusable URP material system with:

- macro color variation across the valley;
- grass, soil, moss, gravel, mud, rock, and corruption surface families;
- tiled albedo and normal detail;
- slope and height influence;
- noise-driven breakup that remains stable in world space;
- roughness variation and restrained normal strength;
- transition masks around roads, water, architecture, and district boundaries.

No visible region larger than a typical building footprint should read as a uniform color field.

## Gravel road

The invasion route must read immediately as a continuous gravel road:

- irregular, organic edges rather than rectangular slabs;
- compacted pale-brown gravel as the dominant surface;
- embedded stones in multiple sizes and values;
- darker wheel ruts and occasional mud;
- grass encroachment and weeds at the margins;
- subtle height variation and bank blending;
- bridge approaches that connect naturally instead of intersecting as separate primitives.

Gameplay readability takes priority: the full route must remain recognizable from the normal camera.

## Grass and foliage

Grass will be represented by varied, clustered blade meshes or cards rather than green terrain color alone. Distribution must:

- favor camera-visible village and roadside areas;
- avoid road centers, building footprints, water, and corruption;
- vary scale, lean, hue, and cluster size deterministically;
- include flowers, weeds, reeds, and low shrubs as accents;
- use density limits and shared materials to keep runtime cost appropriate for the prototype.

Trees will use asymmetrical trunks and layered crown clusters. Existing lollipop-tree repetition must be broken through controlled silhouette, scale, rotation, and color variation.

## Architecture and set dressing

Current primitive buildings and castle forms are replacement targets, not final assets. Buildings should have:

- foundations that meet the terrain;
- doors, windows, chimneys, roof overhangs, trim, and readable entrances;
- material breakup such as timber variation, plaster wear, moss, dirt, and stone accents;
- purposeful orientation toward paths and shared spaces;
- surrounding props that communicate use.

Props must be arranged in small narrative clusters: forge work area, market stalls, farm tools, mill supplies, guard post, castle courtyard, and corruption aftermath. Random scattering is not acceptable.

## Lighting and atmosphere

The lighting target is a warm late-afternoon key with cool environmental fill:

- one deliberate sun direction that supports gameplay silhouettes;
- physically plausible relative intensities;
- soft but readable shadows with contact grounding;
- ambient sky contribution rather than flat ambient color;
- local warm lights at braziers and forge;
- cool emissive contrast at towers and corruption;
- restrained fog for depth and hidden boundaries;
- reflection and indirect-light support;
- restrained color grading, bloom, ambient occlusion, and tone mapping.

Adaptive Probe Volumes or the strongest compatible URP indirect-light solution will be used only after confirming current render-pipeline support. No package additions are required.

## Water and transitions

The river receives shaped banks, stones, reeds, depth/color variation, and a material with controlled smoothness and transparency. Terrain, road, bridge, and river must meet without visible floating slabs or hard accidental intersections.

## Technical structure

- Keep deterministic placement in the existing Greenward world layout/builder.
- Split material creation, ground dressing, architecture, and lighting into focused helpers where the existing builder becomes difficult to review.
- Reuse shared materials and meshes; do not instantiate a unique material per object.
- Keep ambient movement allocation-free during frame updates.
- Preserve current enemy route, tower combat, HUD, wave behavior, and looping Demon Walk animation.
- Do not hand-edit scene YAML or add a second render pipeline.

## Acceptance criteria

The pass is accepted only when fresh normal-gameplay captures demonstrate:

1. the road reads as gravel without relying on HUD or explanation;
2. grass blades/tufts and varied vegetation are visible at gameplay distance;
3. terrain and major structures show multi-value, multi-material surface variation;
4. buildings read as authored structures rather than scaled cubes;
5. the castle is compositionally integrated and not awkwardly cropped;
6. river banks and bridge approaches meet naturally;
7. lighting provides warm key, cool fill, contact shadows, atmospheric depth, and grounded objects;
8. no map edge or empty void dominates the frame;
9. the scene compiles and runs with no new Console errors;
10. enemies traverse multiple waypoints while Walk normalized time exceeds 2.0.

Visual review is a required gate. Object counts or feature checklists alone do not establish quality.

## Validation

- Compile after each material/rendering integration.
- Inspect normal gameplay captures at the project’s actual Game view aspect ratio.
- Compare captures directly with the user-provided critique screenshot.
- Check the Unity Console after imports, shader compilation, and Play Mode.
- Verify material sharing and representative renderer counts.
- Verify road continuity, vegetation exclusions, camera framing, and animation progression in Play Mode.
- Document asset source, license, and any remaining production-art limitations.

## Scope boundaries

This pass does not add new combat systems, UI redesign, multiplayer, save data, paid assets, or a different render pipeline. Character animation changes are limited to preserving the already-fixed continuous Walk behavior.
