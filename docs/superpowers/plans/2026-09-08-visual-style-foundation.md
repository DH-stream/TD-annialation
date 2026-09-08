# Visual Style Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refresh the Phase 0 battlefield into an intentional charming low-poly foundation, lock the Greenward art direction, bundle Metal Mania locally, and increase the playable map scale.

**Architecture:** Keep all map colors in `SceneConfig`, use a single map-root transform to enlarge the existing composition coherently, and keep the browser-facing input/movement boundary unchanged. Add lighting and edge treatment at scene construction time so future environment pieces inherit the same visual language.

**Tech Stack:** Vite, TypeScript, Babylon.js, CSS, `@fontsource/metal-mania`, Vitest.

## Global Constraints

- No external font network request at runtime.
- Greenward uses the seven locked palette anchors from `docs/superpowers/specs/2026-09-08-visual-style-foundation-design.md`.
- Preserve click-to-move, WASD input and strategic camera orbit/zoom.
- Keep the map a coherent foundation slice; do not add combat, towers or co-op in this pass.
- Run the complete test suite, production build and browser visual smoke check before handoff.

---

### Task 1: Record visual target and add the font dependency

**Files:**
- Create: `docs/superpowers/specs/2026-09-08-visual-style-foundation-design.md`
- Create: `docs/superpowers/plans/2026-09-08-visual-style-foundation.md`
- Modify: `PROJECT_STATUS.md`
- Modify: `package.json`
- Modify: `package-lock.json`
- Modify: `src/styles.css`
- Modify: `index.html`
- Create: `THIRD_PARTY_LICENSES.md`

**Interfaces:**
- Produces the locked art rubric, the local `Metal Mania` font asset and the documentation checkpoint used by later tasks.

- [x] **Step 1: Write the design and execution documents.**
- [x] **Step 2: Install the local font package.**

Run:

```text
npm install @fontsource/metal-mania@5.3.0
```

Expected: `package.json` and `package-lock.json` include `@fontsource/metal-mania`, whose package metadata declares `OFL-1.1`.

- [x] **Step 3: Apply the display font only to titles.**

```css
@import '@fontsource/metal-mania';

:root {
  --font-display: 'Metal Mania', serif;
}

.hud-brand,
.hud-stage strong {
  font-family: var(--font-display);
}
```

- [x] **Step 4: Include the exact font license and attribution.**

`THIRD_PARTY_LICENSES.md` must identify `Metal Mania`, its author, source URL and the full SIL Open Font License 1.1 text.

- [x] **Step 5: Run the app build.**

Run: `npm run build`

Expected: exit code 0; the existing Babylon bundle-size warning may remain.

- [x] **Step 6: Commit.**

```text
git add package.json package-lock.json src/styles.css index.html THIRD_PARTY_LICENSES.md PROJECT_STATUS.md docs/superpowers/specs/2026-09-08-visual-style-foundation-design.md docs/superpowers/plans/2026-09-08-visual-style-foundation.md
git commit -m "feat: establish Greenward visual direction"
```

### Task 2: Enforce palette and atmospheric lighting

**Files:**
- Modify: `src/game/config/sceneConfig.ts`
- Modify: `src/game/config/sceneConfig.test.ts`
- Modify: `src/game/createScene.ts`

**Interfaces:**
- Consumes `SceneConfig.colors`.
- Produces one palette-driven battlefield with horizon fog, soft shadow filtering and ambient occlusion enabled for the active camera.

- [x] **Step 1: Write a failing palette test.**

```ts
it('uses the locked Greenward ground anchor', () => {
  expect(DEFAULT_SCENE_CONFIG.colors.ground).toBe('#3F5D47');
});
```

- [x] **Step 2: Run the focused test to verify RED if the palette is not yet locked.**

Run: `npm test -- --run src/game/config/sceneConfig.test.ts`

Expected: the new assertion fails against the current placeholder palette.

- [x] **Step 3: Set the seven Greenward palette anchors.**

Use the exact values in the visual style spec for `ground`, `path`, `stone`, `wood`, `brass`, `magic` and `blood`, reusing `brass` for the magic highlight instead of adding an eighth color.

- [x] **Step 4: Add soft shadows and ambient occlusion.**

```ts
const shadows = new ShadowGenerator(1024, sun);
shadows.useBlurExponentialShadowMap = true;
shadows.blurKernel = 32;

const ambientOcclusion = new SSAO2RenderingPipeline('greenward-ambient-occlusion', scene, {
  ssaoRatio: 0.7,
  blurRatio: 0.7,
});
ambientOcclusion.radius = 2.2;
ambientOcclusion.totalStrength = 1.0;
ambientOcclusion.base = 0.35;
scene.postProcessRenderPipelineManager.attachCamerasToRenderPipeline('greenward-ambient-occlusion', camera);
```

Mark the battlefield as a shadow receiver and non-ground geometry as shadow casters. Use the locked deep horizon color for the clear/fog treatment.

- [x] **Step 5: Run focused and full tests.**

Run: `npm test -- --run src/game/config/sceneConfig.test.ts`

Expected: all focused tests pass.

Run: `npm test -- --run`

Expected: all test files pass.

- [x] **Step 6: Commit.**

```text
git add src/game/config/sceneConfig.ts src/game/config/sceneConfig.test.ts src/game/createScene.ts
git commit -m "feat: add Greenward palette and soft lighting"
```

### Task 3: Enlarge the map and remove placeholder-looking geometry

**Files:**
- Modify: `src/game/config/sceneConfig.ts`
- Modify: `src/game/createScene.ts`
- Modify: `src/main.ts`
- Modify: `src/game/sim/heroMovement.ts`
- Modify: `src/game/sim/heroMovement.test.ts`

**Interfaces:**
- Consumes the existing hero movement contract.
- Produces a uniformly scaled map root, world-to-local click conversion, larger camera framing and a hero silhouette that does not read as a stray plane.

- [x] **Step 1: Write a failing bounds/scale regression test.**

```ts
it('keeps the hero destination in the enlarged local map bounds', () => {
  const result = advanceHeroPosition(position, { ...idleInput, moveX: 1 }, null, 10);
  expect(result.position.x).toBeLessThanOrEqual(28);
});
```

- [x] **Step 2: Run the focused test to verify the missing bound fails.**

Run: `npm test -- --run src/game/sim/heroMovement.test.ts`

Expected: the new bound assertion fails before bounds are added.

- [x] **Step 3: Add explicit enlarged map bounds and scale-aware picking.**

Use a uniform `mapRoot` scale of `1.5`, return it from `createGameScene`, and divide picked world coordinates by `mapRoot.scaling.x` before passing them to the local hero movement simulation. Increase the default camera radius to keep the larger composition legible.

- [x] **Step 4: Replace the hero box preview with a chunky capsule silhouette and soften visible primitive edges.**

Use a low-tessellation capsule for the horse body, keep the rider and ring aligned under `heroRoot`, and apply the same subtle edge treatment to castle, path and build-pad meshes. Do not leave an unowned dark plane near the shrine.

- [x] **Step 5: Run focused and full tests.**

Run: `npm test -- --run src/game/sim/heroMovement.test.ts`

Expected: focused hero tests pass.

Run: `npm test -- --run`

Expected: all test files pass.

- [x] **Step 6: Commit.**

```text
git add src/game/config/sceneConfig.ts src/game/createScene.ts src/main.ts src/game/sim/heroMovement.ts src/game/sim/heroMovement.test.ts
git commit -m "feat: enlarge Greenward battlefield"
```

### Task 4: Browser art QA and handoff documentation

**Files:**
- Modify: `PROJECT_STATUS.md`
- Modify: `docs/superpowers/plans/2026-09-08-visual-style-foundation.md`

- [x] **Step 1: Run the production build.**

Run: `npm run build`

Expected: exit code 0.

- [x] **Step 2: Run the browser smoke check.**

Run: `npm run dev -- --host 127.0.0.1 --port 5175`

Check the rendered scene at `http://127.0.0.1:5175/`:

1. Metal Mania is visible on the title/stage headings.
2. The horizon is not a bare black/navy void.
3. The enlarged battlefield has readable spacing and still supports click-to-move.
4. The hero reads as a deliberate silhouette; no stray dark plane remains by the shrine.
5. Soft shadows/AO and surface variation are visible without obscuring path/build-pad readability.
6. Scroll zoom and drag orbit remain bounded and functional.

- [x] **Step 3: Record the results and any remaining risks in `PROJECT_STATUS.md`.**

- [x] **Step 4: Mark this plan complete and commit.**

```text
git add PROJECT_STATUS.md docs/superpowers/plans/2026-09-08-visual-style-foundation.md
git commit -m "docs: record Greenward art QA"
```
