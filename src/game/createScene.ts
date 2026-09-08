/// <reference types="vite/client" />

import { ArcRotateCamera } from '@babylonjs/core/Cameras/arcRotateCamera';
import '@babylonjs/core/Culling/ray';
import '@babylonjs/core/Shaders/glowBlurPostProcess.fragment';
import '@babylonjs/core/Shaders/glowMapGeneration.fragment';
import '@babylonjs/core/Shaders/glowMapGeneration.vertex';
import '@babylonjs/core/Shaders/glowMapMerge.fragment';
import '@babylonjs/core/Shaders/glowMapMerge.vertex';
import '@babylonjs/core/Shaders/kernelBlur.fragment';
import '@babylonjs/core/Shaders/kernelBlur.vertex';
import '@babylonjs/core/Shaders/pbr.fragment';
import '@babylonjs/core/Shaders/pbr.vertex';
import '@babylonjs/core/Shaders/postprocess.vertex';
import '@babylonjs/core/Shaders/rgbdDecode.fragment';
import '@babylonjs/core/Shaders/shadowMap.fragment';
import '@babylonjs/core/Shaders/shadowMap.vertex';
import '@babylonjs/core/Shaders/ssao2.fragment';
import '@babylonjs/core/Shaders/ssaoCombine.fragment';
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
import { ImageProcessingConfiguration } from '@babylonjs/core/Materials/imageProcessingConfiguration';
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
      instancedMeshes: number;
    };
  }
}

type WindNode = { node: TransformNode; phase: number; strength: number };
type AmbientFlame = { flame: Mesh; light: PointLight; phase: number; baseIntensity: number };
type AmbientMote = { mesh: Mesh; origin: Vector3; phase: number; drift: Vector3 };
type SmokePuff = { mesh: Mesh; origin: Vector3; phase: number };

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

function createHorizonDome(scene: Scene, horizonColor: Color3, upperColor: Color3): Mesh {
  const dome = MeshBuilder.CreateSphere('greenward-horizon-dome', {
    diameter: 360,
    segments: 32,
  }, scene);
  const material = new StandardMaterial('greenward-horizon-material', scene);
  material.disableLighting = true;
  material.backFaceCulling = false;
  material.disableDepthWrite = true;
  material.diffuseColor = Color3.Black();
  material.emissiveColor = Color3.Lerp(horizonColor, upperColor, 0.28);
  material.fogEnabled = false;
  dome.material = material;
  dome.infiniteDistance = true;
  dome.isPickable = false;
  dome.renderingGroupId = 0;
  return dome;
}

function createAmbientMotes(scene: Scene, mapRoot: TransformNode, color: Color3): AmbientMote[] {
  const material = new StandardMaterial('greenward-mote-material', scene);
  material.disableLighting = true;
  material.diffuseColor = color;
  material.emissiveColor = color.scale(0.35);
  material.alpha = 1;
  material.disableDepthWrite = true;
  const positions = [
    [-13, 2.5, 7], [-9, 1.8, 1], [-4, 2.2, 8], [1, 2.9, 6], [6, 2.1, 8],
    [12, 2.8, 4], [-15, 3.1, -1], [-8, 2.4, -7], [5, 2.8, -6], [13, 2.2, -8],
    [-1, 3.4, 10], [9, 3.6, 1],
  ];
  return positions.map(([x, y, z], index) => {
    const mesh = MeshBuilder.CreateSphere(`greenward-mote-${index}`, { diameter: 0.05, segments: 4 }, scene);
    mesh.material = material;
    mesh.parent = mapRoot;
    mesh.position.set(x, y, z);
    mesh.isPickable = false;
    return {
      mesh,
      origin: new Vector3(x, y, z),
      phase: index * 0.83,
      drift: new Vector3(0.35 + (index % 3) * 0.08, 0.18 + (index % 2) * 0.06, 0.25),
    };
  });
}

function createSmokePuffs(scene: Scene, mapRoot: TransformNode, color: Color3): SmokePuff[] {
  const material = new StandardMaterial('greenward-smoke-material', scene);
  material.disableLighting = true;
  material.diffuseColor = Color3.Black();
  material.emissiveColor = Color3.Lerp(color, Color3.White(), 0.28);
  material.alpha = 0.07;
  material.disableDepthWrite = true;
  const origins = [
    new Vector3(-8.4, 7.2, -12.7),
    new Vector3(-8.4, 7.8, -12.7),
    new Vector3(8.4, 7.2, -12.7),
    new Vector3(8.4, 7.8, -12.7),
  ];
  return origins.map((origin, index) => {
    const mesh = MeshBuilder.CreateSphere(`greenward-chimney-smoke-${index}`, { diameter: 0.24, segments: 6 }, scene);
    mesh.material = material;
    mesh.parent = mapRoot;
    mesh.position.copyFrom(origin);
    mesh.isPickable = false;
    return { mesh, origin, phase: index * 0.7 };
  });
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
  ambientFlames: AmbientFlame[],
  phase: number,
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
  const flame = MeshBuilder.CreateSphere(`lantern-flame-${position.x}-${position.z}`, { diameter: 0.28, segments: 8 }, scene);
  flame.position = position.add(new Vector3(0, 0.25, 0));
  flame.material = magic;
  const light = new PointLight(`lantern-light-${position.x}-${position.z}`, position, scene);
  light.diffuse = brass.diffuseColor.clone();
  light.intensity = 1.2;
  light.range = 10;
  ambientFlames.push({ flame, light, phase, baseIntensity: light.intensity });
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

export function createHeroCrown(scene: Scene, config: SceneConfig): TransformNode {
  const root = new TransformNode('hero-crown', scene);
  root.position.y = 2.5;
  const material = createMaterial(scene, 'hero-crown-material', config.colors.brass, 0.3);
  material.emissiveColor = Color3.FromHexString(config.colors.brass).scale(0.85);

  const band = MeshBuilder.CreateTorus('hero-crown-band', { diameter: 0.9, thickness: 0.1, tessellation: 12 }, scene);
  band.rotation.x = Math.PI / 2;
  band.material = material;
  band.parent = root;

  [-0.2, 0, 0.2].forEach((x, index) => {
    const point = MeshBuilder.CreateCylinder(`hero-crown-point-${index}`, { diameterTop: 0, diameterBottom: 0.22, height: 0.58, tessellation: 4 }, scene);
    point.position.set(x * 1.25, 0.29 + (index === 1 ? 0.12 : 0), 0);
    point.material = material;
    point.parent = root;
  });
  return root;
}

export function createHeroAttackEffect(
  scene: Scene,
  config: SceneConfig,
  id: string,
  attack: 'basic' | 'special',
): TransformNode {
  const root = new TransformNode(`hero-${attack}-effect-${id}`, scene);
  const material = createMaterial(scene, `hero-${attack}-effect-material-${id}`, attack === 'special' ? config.colors.magic : config.colors.brass, 0.18);
  material.emissiveColor = Color3.FromHexString(attack === 'special' ? config.colors.magic : config.colors.brass).scale(0.9);
  const ring = MeshBuilder.CreateTorus(`hero-${attack}-effect-ring-${id}`, {
    diameter: attack === 'special' ? 5.2 : 3.1,
    thickness: attack === 'special' ? 0.18 : 0.12,
    tessellation: 20,
  }, scene);
  ring.position.y = 0.2;
  ring.material = material;
  ring.parent = root;
  if (attack === 'special') {
    const shockwave = MeshBuilder.CreateCylinder(`hero-special-shockwave-${id}`, {
      diameter: 5.4,
      height: 0.06,
      tessellation: 20,
    }, scene);
    shockwave.position.y = 0.04;
    shockwave.material = material;
    shockwave.parent = root;
  }
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
  shadows: ShadowGenerator;
  heroRoot: TransformNode;
  destinationMarker: Mesh;
  mapRoot: TransformNode;
  buildPads: Array<{ id: number; position: Vector3 }>;
  registerWindNode: (node: TransformNode, phase?: number, strength?: number) => void;
  updateAmbient: (timeSeconds: number) => void;
} {
  const engine = new Engine(canvas, true, { stencil: true, preserveDrawingBuffer: true });
  const scene = new Scene(engine);
  const windNodes: WindNode[] = [];
  const ambientFlames: AmbientFlame[] = [];
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
    instancedMeshes: scene.meshes.filter((mesh) => mesh.getClassName() === 'InstancedMesh').length,
  });
  const groundColor = Color3.FromHexString(config.colors.ground);
  scene.clearColor = groundColor.scale(0.35).toColor4();
  scene.fogMode = Scene.FOGMODE_EXP2;
  scene.fogDensity = 0.004;
  scene.fogColor = Color3.FromHexString(config.colors.ground);
  scene.imageProcessingConfiguration.toneMappingEnabled = true;
  scene.imageProcessingConfiguration.toneMappingType = ImageProcessingConfiguration.TONEMAPPING_ACES;
  scene.imageProcessingConfiguration.contrast = 1;
  scene.imageProcessingConfiguration.exposure = 0.8;

  const cameraSettings = getCameraSettings(config);
  const camera = new ArcRotateCamera(
    'strategic-camera',
    cameraSettings.alpha,
    cameraSettings.beta,
    cameraSettings.radius,
    new Vector3(0, 0, -2.6),
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
  ambient.intensity = 0.6;
  ambient.diffuse = new Color3(0.58, 0.72, 0.68);
  ambient.groundColor = Color3.FromHexString(config.colors.stone);

  const sun = new DirectionalLight('sun-light', new Vector3(-0.45, -1, 0.35), scene);
  sun.position = new Vector3(-16, 24, -18);
  sun.intensity = 1;
  sun.diffuse = new Color3(1, 0.76, 0.5);

  const shadows = new ShadowGenerator(1024, sun);
  shadows.useBlurExponentialShadowMap = true;
  shadows.blurKernel = 32;
  shadows.setDarkness(0.32);
  shadows.bias = 0.02;
  shadows.normalBias = 0.02;

  void Promise.all([
    import('@babylonjs/core/Rendering/prePassRendererSceneComponent'),
    import('@babylonjs/core/PostProcesses/RenderPipeline/Pipelines/ssao2RenderingPipeline'),
  ])
    .then(([, { SSAO2RenderingPipeline }]) => {
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
  ground.emissiveColor = Color3.FromHexString(config.colors.ground).scale(0.34);
  const path = createMaterial(scene, 'path-material', config.colors.path, 0.86);
  const stone = createMaterial(scene, 'stone-material', config.colors.stone, 0.82);
  const wood = createMaterial(scene, 'wood-material', config.colors.wood, 0.86);
  const brass = createMaterial(scene, 'brass-material', config.colors.brass, 0.5);
  const magic = createMaterial(scene, 'magic-material', config.colors.magic, 0.25);
  magic.emissiveColor = Color3.FromHexString(config.colors.magic).scale(0.55);
  const magicGlow = new GlowLayer('greenward-magic-glow', scene);
  magicGlow.intensity = 0.65;

  const battlefield = MeshBuilder.CreateGround('battlefield-ground', {
    width: config.battlefield.width,
    height: config.battlefield.height,
    subdivisions: 2,
  }, scene);
  battlefield.material = ground;
  battlefield.metadata = { interaction: 'map' };
  const horizonGround = MeshBuilder.CreateGround('greenward-horizon-ground', {
    width: 360,
    height: 360,
  }, scene);
  horizonGround.position.y = -0.06;
  horizonGround.material = ground;
  horizonGround.isPickable = false;

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

  const buildPadPositions = [
    new Vector3(-10.8, 0.12, 2.6),
    new Vector3(5.1, 0.12, 7),
    new Vector3(9.2, 0.12, 0.4),
    new Vector3(-5, 0.12, -5.5),
  ];
  buildPadPositions.forEach((position, index) => {
    addBuildPad(scene, stone, brass, magic, position, index);
  });

  const centralShrine = MeshBuilder.CreateCylinder('central-shrine', { diameter: 2.1, height: 2.7, tessellation: 8 }, scene);
  centralShrine.position = new Vector3(0, 1.4, -4.2);
  centralShrine.material = stone;
  const shrineGlow = MeshBuilder.CreateSphere('central-shrine-glow', { diameter: 1.05, segments: 12 }, scene);
  shrineGlow.position = new Vector3(0, 3.1, -4.2);
  shrineGlow.material = magic;
  const windowMaterial = createMaterial(scene, 'castle-window-material', config.colors.brass, 0.25);
  windowMaterial.emissiveColor = Color3.FromHexString(config.colors.brass).scale(0.85);
  [-4.2, 4.2].forEach((x) => {
    const window = MeshBuilder.CreatePlane(`castle-window-${x}`, { width: 0.72, height: 1.05 }, scene);
    window.position.set(x, 2.9, -17.65);
    window.material = windowMaterial;
  });

  addCastle(scene, stone, wood, brass);
  const heroRoot = addHeroMarker(scene, wood, brass);
  const destinationMarker = MeshBuilder.CreateTorus('hero-destination-marker', {
    diameter: 1.5,
    thickness: 0.06,
    tessellation: 24,
  }, scene);
  destinationMarker.material = magic;
  destinationMarker.isVisible = false;
  addBarricade(scene, wood, new Vector3(-11.7, 0, 9.7), 0.15);
  addBarricade(scene, wood, new Vector3(10.4, 0, -2.2), -0.65);
  addBarricade(scene, wood, new Vector3(3, 0, 10.9), 0.05);

  [
    [new Vector3(-29, 0, -8), 1.1],
    [new Vector3(-30, 0, 12), 0.8],
    [new Vector3(28, 0, 11), 1.2],
    [new Vector3(30, 0, -6), 0.9],
    [new Vector3(-23, 0, 20), 0.75],
    [new Vector3(23, 0, 20), 0.85],
  ].forEach(([position, scale]) => addTree(scene, wood, ground, position as Vector3, scale as number));

  addLantern(scene, wood, brass, magic, new Vector3(-3.7, 2.4, -10.4), ambientFlames, 0.4);
  addLantern(scene, wood, brass, magic, new Vector3(3.7, 2.4, -10.4), ambientFlames, 2.1);

  const mapRoot = new TransformNode('greenward-map-root', scene);
  mapRoot.scaling = new Vector3(1.5, 1.5, 1.5);
  const shrineLight = new PointLight('central-shrine-light', new Vector3(0, 3.3, -4.2), scene);
  shrineLight.parent = mapRoot;
  shrineLight.diffuse = brass.diffuseColor.clone();
  shrineLight.intensity = 1.8;
  shrineLight.range = 14;
  ambientFlames.forEach(({ light }) => {
    light.parent = mapRoot;
  });
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
  battlefield.useVertexColors = true;

  const horizonDome = createHorizonDome(
    scene,
    Color3.FromHexString(config.colors.horizon),
    Color3.FromHexString(config.colors.ground),
  );
  const ambientMotes = createAmbientMotes(scene, mapRoot, Color3.FromHexString(config.colors.brass));
  const smokePuffs = createSmokePuffs(scene, mapRoot, Color3.FromHexString(config.colors.stone));
  const registerWindNode = (node: TransformNode, phase = windNodes.length * 0.7, strength = 0.018): void => {
    windNodes.push({ node, phase, strength });
  };
  const updateAmbient = (timeSeconds: number): void => {
    windNodes.forEach(({ node, phase, strength }) => {
      const gust = Math.sin(timeSeconds * 0.9 + phase) * 0.65 + Math.sin(timeSeconds * 1.7 + phase * 1.4) * 0.35;
      node.rotation.z = gust * strength;
      node.rotation.x = Math.sin(timeSeconds * 0.75 + phase) * strength * 0.45;
    });
    ambientFlames.forEach(({ flame, light, phase, baseIntensity }) => {
      const flicker = 1 + Math.sin(timeSeconds * 8 + phase) * 0.12 + Math.sin(timeSeconds * 13 + phase) * 0.06;
      flame.scaling.set(1 + flicker * 0.08, flicker, 1 + flicker * 0.08);
      light.intensity = baseIntensity * flicker;
    });
    ambientMotes.forEach(({ mesh, origin, phase, drift }) => {
      mesh.position.x = origin.x + Math.sin(timeSeconds * drift.x + phase) * 0.55;
      mesh.position.y = origin.y + Math.sin(timeSeconds * drift.y + phase) * 0.45;
      mesh.position.z = origin.z + Math.cos(timeSeconds * drift.z + phase) * 0.5;
    });
    smokePuffs.forEach(({ mesh, origin, phase }) => {
      const cycle = (timeSeconds * 0.12 + phase / (Math.PI * 2)) % 1;
      mesh.position.set(origin.x + Math.sin(timeSeconds * 0.7 + phase) * 0.12, origin.y + cycle * 2.2, origin.z);
      const size = 0.38 + cycle * 0.42;
      mesh.scaling.set(size, size, size);
      mesh.visibility = 0.55 + (1 - cycle) * 0.35;
    });
    horizonDome.rotation.y = timeSeconds * 0.002;
  };

  return {
    engine,
    scene,
    shadows,
    heroRoot,
    destinationMarker,
    mapRoot,
    buildPads: buildPadPositions.map((position, id) => ({ id, position })),
    registerWindNode,
    updateAmbient,
  };
}
