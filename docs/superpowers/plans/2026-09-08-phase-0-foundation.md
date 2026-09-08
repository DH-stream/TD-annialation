# Phase 0 Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a polished browser-runnable Babylon.js foundation scene for TD Annihilation with a strategic top-down camera, deliberate fantasy presentation, and co-op-ready simulation boundaries.

**Architecture:** Vite starts a small TypeScript application. Babylon presentation code owns the scene and camera, while plain simulation contracts and pure scene configuration stay free of Babylon imports. The initial UI is a lightweight DOM HUD shell; future players will emit `PlayerInput` commands through a transport boundary, but Phase 0 has no network implementation.

**Tech Stack:** TypeScript, Vite, Babylon.js, Vitest, DOM/CSS.

## Global Constraints

- Primary gameplay reference is Dungeon Defenders; Orcs Must Die! is only a secondary feedback reference.
- Strategic top-down/3D camera is the default, approximately 55–65 degrees, with bounded zoom and pitch.
- Future online co-op targets two different computers; Supabase/WebRTC is explicitly out of Phase 0.
- Gore is restrained and readable; any blood must not obscure paths, build locations or UI markers.
- Every phase must produce a coherent, testable vertical slice rather than a disposable prototype.
- `PROJECT_STATUS.md` is the handoff file and must record exact verification results and next steps.

---

### Task 1: Scaffold the TypeScript app and pure camera configuration

**Files:**
- Create: `package.json`
- Create: `package-lock.json` via `npm install`
- Create: `tsconfig.json`
- Create: `vite.config.ts`
- Create: `index.html`
- Create: `src/game/config/sceneConfig.ts`
- Test: `src/game/config/sceneConfig.test.ts`

**Interfaces:**
- Produces `SceneConfig`, `DEFAULT_SCENE_CONFIG`, and `clampCameraPitch(pitchRadians: number): number` for later scene code.

- [x] **Step 1: Create the package and tool configuration**

Create a Vite TypeScript package with scripts `dev`, `build`, `preview`, and `test`, runtime dependency `babylonjs`, and development dependencies `typescript`, `vite`, and `vitest`.

- [x] **Step 2: Write the failing configuration test**

```ts
import { describe, expect, it } from 'vitest';
import { DEFAULT_SCENE_CONFIG, clampCameraPitch } from './sceneConfig';

describe('scene configuration', () => {
  it('uses a strategic top-down default camera angle', () => {
    expect(DEFAULT_SCENE_CONFIG.camera.alpha).toBeCloseTo(-Math.PI / 2);
    expect(DEFAULT_SCENE_CONFIG.camera.beta).toBeCloseTo(Math.PI / 3);
    expect(DEFAULT_SCENE_CONFIG.camera.lowerRadiusLimit).toBeLessThan(
      DEFAULT_SCENE_CONFIG.camera.upperRadiusLimit,
    );
  });

  it('clamps camera pitch to the readable battlefield range', () => {
    expect(clampCameraPitch(0)).toBeCloseTo(DEFAULT_SCENE_CONFIG.camera.lowerBetaLimit);
    expect(clampCameraPitch(Math.PI)).toBeCloseTo(DEFAULT_SCENE_CONFIG.camera.upperBetaLimit);
  });
});
```

- [x] **Step 3: Run the test and verify the expected RED failure**

Run: `npm test -- --run src/game/config/sceneConfig.test.ts`
Expected: FAIL because `src/game/config/sceneConfig.ts` does not exist yet.

- [x] **Step 4: Implement the minimal configuration module**

```ts
export type SceneConfig = {
  camera: {
    alpha: number;
    beta: number;
    radius: number;
    lowerBetaLimit: number;
    upperBetaLimit: number;
    lowerRadiusLimit: number;
    upperRadiusLimit: number;
  };
  colors: {
    ground: string;
    path: string;
    stone: string;
    wood: string;
    brass: string;
    magic: string;
    blood: string;
  };
};

export const DEFAULT_SCENE_CONFIG: SceneConfig = {
  camera: {
    alpha: -Math.PI / 2,
    beta: Math.PI / 3,
    radius: 28,
    lowerBetaLimit: Math.PI * 0.24,
    upperBetaLimit: Math.PI * 0.44,
    lowerRadiusLimit: 14,
    upperRadiusLimit: 44,
  },
  colors: {
    ground: '#304936',
    path: '#ad8a5b',
    stone: '#5c6870',
    wood: '#6d432c',
    brass: '#c59b52',
    magic: '#5ec7e8',
    blood: '#6b2422',
  },
};

export function clampCameraPitch(pitchRadians: number): number {
  return Math.min(
    DEFAULT_SCENE_CONFIG.camera.upperBetaLimit,
    Math.max(DEFAULT_SCENE_CONFIG.camera.lowerBetaLimit, pitchRadians),
  );
}
```

- [x] **Step 5: Run the focused test and verify GREEN**

Run: `npm test -- --run src/game/config/sceneConfig.test.ts`
Expected: PASS with 2 tests.

- [x] **Step 6: Commit the scaffold**

```bash
git add package.json package-lock.json tsconfig.json vite.config.ts index.html src/game/config/sceneConfig.ts src/game/config/sceneConfig.test.ts
git commit -m "feat: scaffold phase 0 scene foundation"
```

### Task 2: Add co-op-ready contracts and compose the Babylon battlefield

**Files:**
- Create: `src/game/sim/types.ts`
- Create: `src/game/createScene.ts`
- Create: `src/main.ts`
- Create: `src/styles.css`

**Interfaces:**
- `GameState` is plain data with `phase`, `players`, `cameraTarget` and `stageId`.
- `PlayerInput` is a command payload with `playerId`, movement vector, action flags and timestamp.
- `NetworkTransport` exposes `connect`, `disconnect`, `sendInput`, `onState` and `onError` without implementing a provider.
- `createGameScene(canvas: HTMLCanvasElement, config?: SceneConfig): { engine: Engine; scene: Scene }` creates the Babylon presentation.

- [x] **Step 1: Add the simulation contracts without Babylon imports**

```ts
export type GamePhase = 'foundation' | 'playing' | 'victory' | 'defeat';

export type PlayerInput = {
  playerId: string;
  moveX: number;
  moveZ: number;
  basicAttack: boolean;
  specialAttack: boolean;
  issuedAt: number;
};

export type GameState = {
  phase: GamePhase;
  stageId: string;
  cameraTarget: { x: number; y: number; z: number };
  players: Record<string, { x: number; y: number; z: number; health: number }>;
};

export type NetworkTransport = {
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  sendInput(input: PlayerInput): void;
  onState(listener: (state: GameState) => void): () => void;
  onError(listener: (error: Error) => void): () => void;
};
```

- [x] **Step 2: Compose the scene with deliberate geometry and materials**

Implement `createGameScene` with Babylon primitives for the Phase 0 sandbox: a large ground, winding path strips, central build pads, a castle gate, stone walls, wooden barricades, banners, warm/cool lighting, a sky-colored clear color and an ArcRotateCamera using `DEFAULT_SCENE_CONFIG`. Use named meshes and consistent palette tokens so later GLB assets can replace individual visual groups. The primitives are an intentional Phase 0 deviation from final production art, not a replacement for the later asset pass.

- [x] **Step 3: Add camera interaction and engine lifecycle**

Enable wheel zoom and pointer orbit through Babylon’s ArcRotateCamera, keep beta/radius inside the configured bounds, resize the engine with `window.resize`, and start the render loop. Dispose the engine if the page lifecycle requires teardown.

- [x] **Step 4: Add the canvas shell and HUD-ready page styling**

Create an accessible full-viewport canvas with a small code-native HUD shell: game title, stage label, a `FOUNDATION SCENE` status, camera hint and empty resource slots reserved for later economy/UI. Use a dark slate UI surface, warm brass accent and readable system fallbacks; no gameplay claims or fake values.

- [x] **Step 5: Run the production build**

Run: `npm run build`
Expected: Vite emits a `dist/` build with exit code 0.

- [x] **Step 6: Commit the composed scene**

```bash
git add src/game/sim/types.ts src/game/createScene.ts src/main.ts src/styles.css
git commit -m "feat: add strategic fantasy battlefield scene"
```

### Task 3: Browser verification and project handoff update

**Files:**
- Modify: `PROJECT_STATUS.md`

**Interfaces:**
- Consumes the running app from Task 2 and the commands from Tasks 1–2.
- Produces a verified Phase 0 status record with exact outcomes and next phase.

- [x] **Step 1: Start the development server**

Run: `npm run dev -- --host 127.0.0.1`
Expected: Vite reports a local URL.

- [x] **Step 2: Verify the first viewport in Browser/IAB**

Open the local URL. Confirm the battlefield, castle gate, path, build pads and HUD render without console errors. Confirm the default view is strategic top-down.

- [x] **Step 3: Verify camera interaction**

Use the mouse wheel to zoom in and out, then drag with the middle mouse button to orbit. Confirm the camera remains bounded and the map remains readable at both limits.

- [x] **Step 4: Verify the focused tests and build again**

Run: `npm test -- --run`
Expected: all tests pass.
Run: `npm run build`
Expected: exit code 0.

- [x] **Step 5: Update the handoff file with evidence**

Replace the “not run yet” verification section in `PROJECT_STATUS.md` with the actual test/build commands, browser URL behavior, visual checks and known Phase 0 limitations. Set the next milestone to Phase 1 core loop.

- [x] **Step 6: Run final diff checks and commit the handoff**

```bash
git diff --check
git add PROJECT_STATUS.md
git commit -m "docs: record phase 0 verification status"
```
