import {
  ArcRotateCamera,
  ArcRotateCameraPointersInput,
  Color3,
  Color4,
  DirectionalLight,
  Engine,
  HemisphericLight,
  Mesh,
  MeshBuilder,
  PBRMaterial,
  PointLight,
  Scene,
  TransformNode,
  Vector3,
} from 'babylonjs';
import { DEFAULT_SCENE_CONFIG, type SceneConfig } from './config/sceneConfig';

export function getCameraSettings(config: SceneConfig): SceneConfig['camera'] {
  return config.camera;
}

function createMaterial(scene: Scene, name: string, color: string, roughness = 0.82): PBRMaterial {
  const material = new PBRMaterial(name, scene);
  material.albedoColor = Color3.FromHexString(color);
  material.roughness = roughness;
  return material;
}

function addBlock(
  scene: Scene,
  material: PBRMaterial,
  name: string,
  position: Vector3,
  dimensions: { width: number; height: number; depth: number },
  rotationY = 0,
): Mesh {
  const block = MeshBuilder.CreateBox(name, dimensions, scene);
  block.position = position;
  block.rotation.y = rotationY;
  block.material = material;
  return block;
}

function addTree(scene: Scene, wood: PBRMaterial, ground: PBRMaterial, position: Vector3, scale = 1): void {
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

function addBuildPad(scene: Scene, stone: PBRMaterial, brass: PBRMaterial, position: Vector3, index: number): void {
  const base = MeshBuilder.CreateCylinder(`build-pad-${index}`, {
    diameter: 3.2,
    height: 0.18,
    tessellation: 12,
  }, scene);
  base.position = position;
  base.material = stone;
  base.metadata = { interaction: 'map' };

  const ring = MeshBuilder.CreateTorus(`build-pad-ring-${index}`, {
    diameter: 2.75,
    thickness: 0.09,
    tessellation: 24,
  }, scene);
  ring.position = position.add(new Vector3(0, 0.16, 0));
  ring.material = brass;
  ring.metadata = { interaction: 'map' };
}

function addBarricade(scene: Scene, wood: PBRMaterial, position: Vector3, rotationY: number): void {
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

function addCastle(scene: Scene, stone: PBRMaterial, wood: PBRMaterial, brass: PBRMaterial): void {
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

function addHeroMarker(scene: Scene, wood: PBRMaterial, brass: PBRMaterial): TransformNode {
  const heroRoot = new TransformNode('hero-root', scene);
  heroRoot.position = new Vector3(0, 0, 4.2);

  const horse = MeshBuilder.CreateBox('hero-horse-preview', { width: 1.1, height: 0.9, depth: 1.8 }, scene);
  horse.position = new Vector3(0, 0.7, 0);
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

function addLantern(scene: Scene, brass: PBRMaterial, position: Vector3): void {
  const lantern = MeshBuilder.CreateSphere(`lantern-${position.x}-${position.z}`, { diameter: 0.35, segments: 8 }, scene);
  lantern.position = position;
  lantern.material = brass;
  const light = new PointLight(`lantern-light-${position.x}-${position.z}`, position, scene);
  light.diffuse = new Color3(1, 0.62, 0.2);
  light.intensity = 0.8;
  light.range = 7;
}

export function createGameScene(
  canvas: HTMLCanvasElement,
  config: SceneConfig = DEFAULT_SCENE_CONFIG,
): { engine: Engine; scene: Scene; heroRoot: TransformNode; destinationMarker: Mesh } {
  const engine = new Engine(canvas, true, { stencil: true, preserveDrawingBuffer: true });
  const scene = new Scene(engine);
  scene.clearColor = new Color4(0.055, 0.09, 0.1, 1);
  scene.fogMode = Scene.FOGMODE_EXP2;
  scene.fogDensity = 0.008;
  scene.fogColor = Color3.FromHexString('#1b2d2a');
  scene.imageProcessingConfiguration.contrast = 1.08;
  scene.imageProcessingConfiguration.exposure = 1.05;

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
  ambient.intensity = 0.72;
  ambient.diffuse = Color3.FromHexString('#f0d3a0');
  ambient.groundColor = Color3.FromHexString('#172522');

  const sun = new DirectionalLight('sun-light', new Vector3(-0.45, -1, 0.35), scene);
  sun.position = new Vector3(-16, 24, -18);
  sun.intensity = 2.1;
  sun.diffuse = Color3.FromHexString('#ffd7a0');

  const ground = createMaterial(scene, 'ground-material', config.colors.ground);
  const path = createMaterial(scene, 'path-material', config.colors.path);
  const stone = createMaterial(scene, 'stone-material', config.colors.stone);
  const wood = createMaterial(scene, 'wood-material', config.colors.wood);
  const brass = createMaterial(scene, 'brass-material', config.colors.brass, 0.5);
  const magic = createMaterial(scene, 'magic-material', config.colors.magic, 0.25);

  const battlefield = MeshBuilder.CreateGround('battlefield-ground', { width: 42, height: 30, subdivisions: 2 }, scene);
  battlefield.material = ground;
  battlefield.metadata = { interaction: 'map' };

  const pathSegments = [
    { position: new Vector3(-11, 0.04, 8.7), rotation: 0.12, width: 4.1, depth: 8 },
    { position: new Vector3(-5.2, 0.04, 4.2), rotation: -0.75, width: 4.1, depth: 7.5 },
    { position: new Vector3(0.5, 0.04, 0.7), rotation: 0.12, width: 4.1, depth: 9 },
    { position: new Vector3(6.1, 0.04, -3.4), rotation: 0.78, width: 4.1, depth: 7.2 },
    { position: new Vector3(10.8, 0.04, -7.7), rotation: 0.04, width: 4.1, depth: 7.5 },
  ];
  pathSegments.forEach((segment, index) => {
    const pathSegment = addBlock(scene, path, `enemy-path-${index}`, segment.position, {
      width: segment.width,
      height: 0.08,
      depth: segment.depth,
    }, segment.rotation);
    pathSegment.metadata = { interaction: 'map' };
  });

  [new Vector3(-8, 0.12, 1.9), new Vector3(3.8, 0.12, 5.2), new Vector3(6.8, 0.12, 0.3), new Vector3(-3.7, 0.12, -4.1)].forEach((position, index) => {
    addBuildPad(scene, stone, brass, position, index);
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

  addLantern(scene, brass, new Vector3(-3.7, 2.4, -10.4));
  addLantern(scene, brass, new Vector3(3.7, 2.4, -10.4));

  return { engine, scene, heroRoot, destinationMarker };
}
