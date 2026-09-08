/// <reference types="vite/client" />

import { ArcRotateCamera } from '@babylonjs/core/Cameras/arcRotateCamera';
import { ArcRotateCameraPointersInput } from '@babylonjs/core/Cameras/Inputs/arcRotateCameraPointersInput';
import { VertexBuffer } from '@babylonjs/core/Buffers/buffer';
import { DirectionalLight } from '@babylonjs/core/Lights/directionalLight';
import { HemisphericLight } from '@babylonjs/core/Lights/hemisphericLight';
import { ShadowGenerator } from '@babylonjs/core/Lights/Shadows/shadowGenerator';
import '@babylonjs/core/Lights/Shadows/shadowGeneratorSceneComponent';
import { GlowLayer } from '@babylonjs/core/Layers/glowLayer';
import { Color3 } from '@babylonjs/core/Maths/math.color';
import { Vector3 } from '@babylonjs/core/Maths/math.vector';
import { Mesh } from '@babylonjs/core/Meshes/mesh';
import { MeshBuilder } from '@babylonjs/core/Meshes/meshBuilder';
import { TransformNode } from '@babylonjs/core/Meshes/transformNode';
import { Engine } from '@babylonjs/core/Engines/engine';
import { Scene } from '@babylonjs/core/scene';
import { SceneInstrumentation } from '@babylonjs/core/Instrumentation/sceneInstrumentation';
import { StandardMaterial } from '@babylonjs/core/Materials/standardMaterial';
import { PointLight } from '@babylonjs/core/Lights/pointLight';
import earcut from 'earcut';
import { DEFAULT_SCENE_CONFIG, type SceneConfig } from './config/sceneConfig';
import { GREENWARD_PATH } from './sim/stageSimulation';

declare global {
  interface Window {
    __TD_PERF__?: () => {
      fps: number;
      frameMs: number;
      renderMs: number;
      activeMeshesMs: number;
      activeMeshes: number;
    };
  }
}

export function getCameraSettings(config: SceneConfig): SceneConfig['camera'] {
  return config.camera;
}

function createMaterial(
  scene: Scene,
  name: string,
  color: string,
  roughness = 0.82,
): StandardMaterial {
  const material = new StandardMaterial(name, scene);
  const diffuseColor = Color3.FromHexString(color);
  material.diffuseColor = diffuseColor;
  material.specularColor = diffuseColor.scale(1 - roughness);
  return material;
}

function addSubtleVertexVariation(mesh: Mesh): void {
  const positions = mesh.getVerticesData(VertexBuffer.PositionKind);
  const material = mesh.material;
  if (!positions || !(material instanceof StandardMaterial)) {
    return;
  }

  let minY = Number.POSITIVE_INFINITY;
  let maxY = Number.NEGATIVE_INFINITY;
  for (let index = 1; index < positions.length; index += 3) {
    minY = Math.min(minY, positions[index]);
    maxY = Math.max(maxY, positions[index]);
  }

  const heightRange = Math.max(0.001, maxY - minY);
  const colors: number[] = [];
  for (let index = 0; index < positions.length; index += 3) {
    const heightLightness = (positions[index + 1] - minY) / heightRange;
    const facetVariation = ((index / 3) % 5) * 0.008;
    const lightness = 0.92 + heightLightness * 0.1 + facetVariation;
    colors.push(lightness, lightness, lightness, 1);
  }
  mesh.setVerticesData(VertexBuffer.ColorKind, colors, true, 4);
  mesh.useVertexColors = true;
}

function createChamferedBox(
  scene: Scene,
  name: string,
  dimensions: { width: number; height: number; depth: number },
): Mesh {
  const halfWidth = dimensions.width / 2;
  const halfDepth = dimensions.depth / 2;
  const chamfer = Math.min(0.12, halfWidth * 0.25, halfDepth * 0.25);
  const shape = [
    new Vector3(-halfWidth + chamfer, 0, -halfDepth),
    new Vector3(halfWidth - chamfer, 0, -halfDepth),
    new Vector3(halfWidth, 0, -halfDepth + chamfer),
    new Vector3(halfWidth, 0, halfDepth - chamfer),
    new Vector3(halfWidth - chamfer, 0, halfDepth),
    new Vector3(-halfWidth + chamfer, 0, halfDepth),
    new Vector3(-halfWidth, 0, halfDepth - chamfer),
    new Vector3(-halfWidth, 0, -halfDepth + chamfer),
  ];
  const block = MeshBuilder.ExtrudePolygon(
    name,
    { shape, depth: dimensions.height, sideOrientation: Mesh.DOUBLESIDE },
    scene,
    earcut,
  );
  block.position.y = dimensions.height / 2;
  return block;
}

function addBlock(
  scene: Scene,
  material: StandardMaterial,
  name: string,
  position: Vector3,
  dimensions: { width: number; height: number; depth: number },
  rotationY = 0,
): Mesh {
  const block = createChamferedBox(scene, name, dimensions);
  block.position.addInPlace(position);
  block.rotation.y = rotationY;
  block.material = material;
  return block;
}

function addTree(scene: Scene, wood: StandardMaterial, ground: StandardMaterial, position: Vector3, scale = 1): void {
  const trunk = MeshBuilder.CreateCylinder(`tree-trunk-${position.x}-${position.z}`, {
    diameter: 0.45 * scale,
    height: 2.4 * scale,
    tessellation: 8,
  }, scene);
  trunk.position = position.add(new Vector3(0, 1.2 * scale, 0));
  trunk.material = wood;

  const crown = MeshBuilder.CreateCylinder(`tree-crown-${position.x}-${position.z}`, {
    diameterTop: 0,
    diameterBottom: 3.2 * scale,
    height: 4.8 * scale,
    tessellation: 7,
  }, scene);
  crown.position = position.add(new Vector3(0, 4 * scale, 0));
  crown.material = ground;
}

function addBuildPad(
  scene: Scene,
  stone: StandardMaterial,
  brass: StandardMaterial,
  magic: StandardMaterial,
  position: Vector3,
  index: number,
): void {
  const base = MeshBuilder.CreateCylinder(`build-pad-${index}`, {
    diameter: 3.2,
    height: 0.18,
    tessellation: 12,
  }, scene);
  base.position = position;
  base.material = stone;
  base.metadata = { interaction: 'build-pad', buildPadId: index };

  const ring = MeshBuilder.CreateTorus(`build-pad-ring-${index}`, {
    diameter: 2.75,
    thickness: 0.09,
    tessellation: 24,
  }, scene);
  ring.position = position.add(new Vector3(0, 0.16, 0));
  ring.material = brass;
  ring.metadata = { interaction: 'build-pad', buildPadId: index };

  const rune = MeshBuilder.CreateTorus(`build-pad-rune-${index}`, {
    diameter: 1.85,
    thickness: 0.035,
    tessellation: 16,
  }, scene);
  rune.position = position.add(new Vector3(0, 0.21, 0));
  rune.material = magic;
  rune.metadata = { interaction: 'build-pad', buildPadId: index };
}

function addBarricade(scene: Scene, wood: StandardMaterial, position: Vector3, rotationY: number): void {
  addBlock(scene, wood, `barricade-rail-${position.x}-${position.z}`, position.add(new Vector3(0, 0.7, 0)), {
    width: 3.4,
    height: 0.28,
    depth: 0.3,
  }, rotationY);
  for (const offset of [-1.4, 1.4]) {
    const post = MeshBuilder.CreateCylinder(`barricade-post-${position.x}-${position.z}-${offset}`, {
      diameter: 0.28,
      height: 1.6,
      tessellation: 6,
    }, scene);
    post.position = position.add(new Vector3(Math.cos(rotationY) * offset, 0.8, Math.sin(rotationY) * offset));
    post.material = wood;
  }
}

function addCastle(scene: Scene, stone: StandardMaterial, wood: StandardMaterial, brass: StandardMaterial): void {
  addBlock(scene, stone, 'castle-wall-left', new Vector3(-11, 2.2, -11.5), { width: 10, height: 4.4, depth: 1.2 });
  addBlock(scene, stone, 'castle-wall-right', new Vector3(11, 2.2, -11.5), { width: 10, height: 4.4, depth: 1.2 });
  addBlock(scene, stone, 'castle-gatehouse', new Vector3(0, 3.6, -11.5), { width: 7.2, height: 7.2, depth: 1.8 });
  addBlock(scene, wood, 'castle-gate', new Vector3(0, 2.3, -10.45), { width: 4, height: 4.6, depth: 0.3 });

  for (const x of [-14.8, 14.8]) {
    const tower = MeshBuilder.CreateCylinder(`castle-tower-${x}`, { diameter: 3.4, height: 7, tessellation: 10 }, scene);
    tower.position = new Vector3(x, 3.5, -11.5);
    tower.material = stone;
    const cap = MeshBuilder.CreateCylinder(`castle-tower-cap-${x}`, { diameter: 4, height: 0.35, tessellation: 10 }, scene);
    cap.position = new Vector3(x, 7.2, -11.5);
    cap.material = brass;
  }
}

function addHeroMarker(scene: Scene, wood: StandardMaterial, brass: StandardMaterial): TransformNode {
  const heroRoot = new TransformNode('hero-root', scene);
  heroRoot.position = new Vector3(0, 0, 4.2);

  const horse = MeshBuilder.CreateCapsule('hero-horse-preview', {
    height: 1.8,
    radius: 0.55,
    tessellation: 8,
  }, scene);
  horse.position = new Vector3(0, 0.7, 0);
  horse.rotation.x = Math.PI / 2;
  horse.material = wood;
  horse.parent = heroRoot;

  const rider = MeshBuilder.CreateCylinder('hero-rider-preview', { diameter: 0.6, height: 1.3, tessellation: 8 }, scene);
  rider.position = new Vector3(0, 1.7, 0);
  rider.material = brass;
  rider.parent = heroRoot;

  const marker = MeshBuilder.CreateTorus('hero-marker', { diameter: 2.3, thickness: 0.08, tessellation: 28 }, scene);
  marker.position = new Vector3(0, 0.08, 0);
  marker.material = brass;

  marker.parent = heroRoot;
  return heroRoot;
}

function addLantern(
  scene: Scene,
  wood: StandardMaterial,
  brass: StandardMaterial,
  magic: StandardMaterial,
  position: Vector3,
): void {
  const post = MeshBuilder.CreateCylinder(`lantern-post-${position.x}-${position.z}`, {
    diameter: 0.16,
    height: 2.2,
    tessellation: 8,
  }, scene);
  post.position = position.add(new Vector3(0, -1.1, 0));
  post.material = wood;

  const lantern = MeshBuilder.CreateSphere(`lantern-${position.x}-${position.z}`, { diameter: 0.35, segments: 8 }, scene);
  lantern.position = position;
  lantern.material = brass;
  const flame = MeshBuilder.CreateSphere(`lantern-flame-${position.x}-${position.z}`, { diameter: 0.18, segments: 8 }, scene);
  flame.position = position.add(new Vector3(0, 0.25, 0));
  flame.material = magic;
  const light = new PointLight(`lantern-light-${position.x}-${position.z}`, position, scene);
  light.diffuse = brass.diffuseColor.clone();
  light.intensity = 0.8;
  light.range = 7;
}

export function createEnemyVisual(scene: Scene, config: SceneConfig, id: string): TransformNode {
  const root = new TransformNode(`enemy-root-${id}`, scene);
  const body = MeshBuilder.CreateCapsule(`enemy-body-${id}`, {
    height: 1.35,
    radius: 0.46,
    tessellation: 8,
  }, scene);
  body.position.y = 0.72;
  body.material = createMaterial(scene, `enemy-body-material-${id}`, config.colors.stone, 0.78);
  body.parent = root;

  const head = MeshBuilder.CreateSphere(`enemy-head-${id}`, { diameter: 0.7, segments: 8 }, scene);
  head.position.y = 1.62;
  head.material = createMaterial(scene, `enemy-head-material-${id}`, config.colors.wood, 0.82);
  head.parent = root;

  const crest = MeshBuilder.CreateCylinder(`enemy-crest-${id}`, { diameter: 0.22, height: 0.42, tessellation: 6 }, scene);
  crest.position = new Vector3(0, 2.08, 0);
  crest.material = createMaterial(scene, `enemy-crest-material-${id}`, config.colors.brass, 0.5);
  crest.parent = root;
  return root;
}

export function createTowerVisual(scene: Scene, config: SceneConfig, id: string): TransformNode {
  const root = new TransformNode(`tower-root-${id}`, scene);
  const base = MeshBuilder.CreateCylinder(`tower-base-${id}`, { diameter: 1.25, height: 0.5, tessellation: 10 }, scene);
  base.position.y = 0.25;
  base.material = createMaterial(scene, `tower-base-material-${id}`, config.colors.stone, 0.78);
  base.parent = root;

  const core = MeshBuilder.CreateCylinder(`tower-core-${id}`, { diameter: 0.62, height: 1.25, tessellation: 8 }, scene);
  core.position.y = 1.05;
  core.material = createMaterial(scene, `tower-core-material-${id}`, config.colors.wood, 0.82);
  core.parent = root;

  const crystal = MeshBuilder.CreateSphere(`tower-crystal-${id}`, { diameter: 0.42, segments: 8 }, scene);
  crystal.position.y = 1.78;
  crystal.material = createMaterial(scene, `tower-crystal-material-${id}`, config.colors.magic, 0.25);
  crystal.parent = root;
  return root;
}

export function createCoinVisual(scene: Scene, config: SceneConfig, id: string): TransformNode {
  const root = new TransformNode(`coin-root-${id}`, scene);
  const coin = MeshBuilder.CreateTorus(`coin-${id}`, {
    diameter: 0.42,
    thickness: 0.12,
    tessellation: 12,
  }, scene);
  coin.rotation.x = Math.PI / 2;
  coin.material = createMaterial(scene, `coin-material-${id}`, config.colors.brass, 0.32);
  coin.parent = root;
  const glint = MeshBuilder.CreateSphere(`coin-glint-${id}`, { diameter: 0.1, segments: 6 }, scene);
  glint.position.y = 0.18;
  glint.material = createMaterial(scene, `coin-glint-material-${id}`, config.colors.magic, 0.2);
  glint.parent = root;
  return root;
}

export function createGameScene(
  canvas: HTMLCanvasElement,
  config: SceneConfig = DEFAULT_SCENE_CONFIG,
): {
  engine: Engine;
  scene: Scene;
  heroRoot: TransformNode;
  destinationMarker: Mesh;
  mapRoot: TransformNode;
  buildPads: Array<{ id: number; position: Vector3 }>;
} {
  const engine = new Engine(canvas, true, { stencil: true, preserveDrawingBuffer: true });
  const scene = new Scene(engine);
  const instrumentation = new SceneInstrumentation(scene);
  instrumentation.captureFrameTime = true;
  instrumentation.captureRenderTime = true;
  instrumentation.captureActiveMeshesEvaluationTime = true;
  window.__TD_PERF__ = () => ({
    fps: Math.round(1000 / Math.max(0.1, instrumentation.frameTimeCounter.lastSecAverage)),
    frameMs: Number(instrumentation.frameTimeCounter.lastSecAverage.toFixed(2)),
    renderMs: Number(instrumentation.renderTimeCounter.lastSecAverage.toFixed(2)),
    activeMeshesMs: Number(instrumentation.activeMeshesEvaluationTimeCounter.lastSecAverage.toFixed(2)),
    activeMeshes: scene.getActiveMeshes().length,
  });
  const groundColor = Color3.FromHexString(config.colors.ground);
  scene.clearColor = groundColor.scale(0.35).toColor4();
  scene.fogMode = Scene.FOGMODE_EXP2;
  scene.fogDensity = 0.004;
  scene.fogColor = Color3.FromHexString(config.colors.ground);
  scene.imageProcessingConfiguration.contrast = 1.08;
  scene.imageProcessingConfiguration.exposure = 1.2;

  const cameraSettings = getCameraSettings(config);
  const camera = new ArcRotateCamera(
    'strategic-camera',
    cameraSettings.alpha,
    cameraSettings.beta,
    cameraSettings.radius,
    new Vector3(0, 0, 0),
    scene,
  );
  camera.lowerBetaLimit = cameraSettings.lowerBetaLimit;
  camera.upperBetaLimit = cameraSettings.upperBetaLimit;
  camera.lowerRadiusLimit = cameraSettings.lowerRadiusLimit;
  camera.upperRadiusLimit = cameraSettings.upperRadiusLimit;
  camera.wheelPrecision = 32;
  camera.panningSensibility = 0;
  camera.attachControl(canvas, true);
  const pointerInput = camera.inputs.attached.pointers as ArcRotateCameraPointersInput | undefined;
  if (pointerInput) {
    pointerInput.buttons = [0, 1];
  }

  const ambient = new HemisphericLight('ambient-light', new Vector3(0, 1, 0), scene);
  ambient.intensity = 1.0;
  ambient.diffuse = Color3.FromHexString(config.colors.brass);
  ambient.groundColor = Color3.FromHexString(config.colors.stone);

  const sun = new DirectionalLight('sun-light', new Vector3(-0.45, -1, 0.35), scene);
  sun.position = new Vector3(-16, 24, -18);
  sun.intensity = 1.8;
  sun.diffuse = Color3.FromHexString(config.colors.brass);

  const shadows = new ShadowGenerator(1024, sun);
  shadows.useBlurExponentialShadowMap = true;
  shadows.blurKernel = 32;
  shadows.setDarkness(0.32);
  shadows.bias = 0.02;
  shadows.normalBias = 0.02;

  void import('@babylonjs/core/PostProcesses/RenderPipeline/Pipelines/ssao2RenderingPipeline')
    .then(({ SSAO2RenderingPipeline }) => {
      const ambientOcclusion = new SSAO2RenderingPipeline('greenward-ambient-occlusion', scene, {
        ssaoRatio: 0.7,
        blurRatio: 0.7,
      });
      ambientOcclusion.radius = 2.2;
      ambientOcclusion.totalStrength = 0.55;
      ambientOcclusion.base = 0.5;
      scene.postProcessRenderPipelineManager.attachCamerasToRenderPipeline(
        'greenward-ambient-occlusion',
        camera,
      );
    })
    .catch(() => {
      // AO is an optional polish pass; the scene remains playable if its chunk fails to load.
    });

  const ground = createMaterial(scene, 'ground-material', config.colors.ground, 0.9);
  const path = createMaterial(scene, 'path-material', config.colors.path, 0.86);
  const stone = createMaterial(scene, 'stone-material', config.colors.stone, 0.82);
  const wood = createMaterial(scene, 'wood-material', config.colors.wood, 0.86);
  const brass = createMaterial(scene, 'brass-material', config.colors.brass, 0.5);
  const magic = createMaterial(scene, 'magic-material', config.colors.magic, 0.25);
  magic.emissiveColor = Color3.FromHexString(config.colors.magic).scale(0.3);
  const magicGlow = new GlowLayer('greenward-magic-glow', scene);
  magicGlow.intensity = 0.45;

  const battlefield = MeshBuilder.CreateGround('battlefield-ground', { width: 42, height: 30, subdivisions: 2 }, scene);
  battlefield.material = ground;
  battlefield.metadata = { interaction: 'map' };

  GREENWARD_PATH.slice(0, -1).forEach((start, index) => {
    const end = GREENWARD_PATH[index + 1];
    const deltaX = end.x - start.x;
    const deltaZ = end.z - start.z;
    const length = Math.hypot(deltaX, deltaZ) + 1.8;
    const pathSegment = addBlock(scene, path, `enemy-path-${index}`, new Vector3(
      (start.x + end.x) / 2,
      0.04,
      (start.z + end.z) / 2,
    ), {
      width: 4.1,
      height: 0.08,
      depth: length,
    }, Math.atan2(deltaX, deltaZ));
    pathSegment.metadata = { interaction: 'map' };
  });

  const buildPadPositions = [new Vector3(-8, 0.12, 1.9), new Vector3(3.8, 0.12, 5.2), new Vector3(6.8, 0.12, 0.3), new Vector3(-3.7, 0.12, -4.1)];
  buildPadPositions.forEach((position, index) => {
    addBuildPad(scene, stone, brass, magic, position, index);
  });

  const centralShrine = MeshBuilder.CreateCylinder('central-shrine', { diameter: 2.1, height: 2.7, tessellation: 8 }, scene);
  centralShrine.position = new Vector3(0, 1.4, -3.1);
  centralShrine.material = stone;
  const shrineGlow = MeshBuilder.CreateSphere('central-shrine-glow', { diameter: 0.75, segments: 12 }, scene);
  shrineGlow.position = new Vector3(0, 3.1, -3.1);
  shrineGlow.material = magic;

  addCastle(scene, stone, wood, brass);
  const heroRoot = addHeroMarker(scene, wood, brass);
  const destinationMarker = MeshBuilder.CreateTorus('hero-destination-marker', {
    diameter: 1.5,
    thickness: 0.06,
    tessellation: 24,
  }, scene);
  destinationMarker.material = magic;
  destinationMarker.isVisible = false;
  addBarricade(scene, wood, new Vector3(-8.7, 0, 7.2), 0.15);
  addBarricade(scene, wood, new Vector3(7.7, 0, -1.6), -0.65);
  addBarricade(scene, wood, new Vector3(2.2, 0, 8.1), 0.05);

  [
    [new Vector3(-18, 0, -5), 1.1],
    [new Vector3(-18, 0, 8), 0.8],
    [new Vector3(17, 0, 7), 1.2],
    [new Vector3(18, 0, -3), 0.9],
    [new Vector3(-14, 0, 12), 0.75],
    [new Vector3(14, 0, 12), 0.85],
  ].forEach(([position, scale]) => addTree(scene, wood, ground, position as Vector3, scale as number));

  addLantern(scene, wood, brass, magic, new Vector3(-3.7, 2.4, -10.4));
  addLantern(scene, wood, brass, magic, new Vector3(3.7, 2.4, -10.4));

  const mapRoot = new TransformNode('greenward-map-root', scene);
  mapRoot.scaling = new Vector3(1.5, 1.5, 1.5);
  heroRoot.parent = mapRoot;
  destinationMarker.parent = mapRoot;
  scene.meshes.forEach((mesh) => {
    if (!mesh.parent) {
      mesh.parent = mapRoot;
    }
    if (mesh instanceof Mesh) {
      addSubtleVertexVariation(mesh);
    }
    mesh.receiveShadows = true;
    if (mesh !== battlefield) {
      shadows.addShadowCaster(mesh, true);
    }
  });

  return {
    engine,
    scene,
    heroRoot,
    destinationMarker,
    mapRoot,
    buildPads: buildPadPositions.map((position, id) => ({ id, position })),
  };
}
