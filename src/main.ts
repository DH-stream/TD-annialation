import { PointerEventTypes } from '@babylonjs/core/Events/pointerEvents';
import { Color3 } from '@babylonjs/core/Maths/math.color';
import { Vector3 } from '@babylonjs/core/Maths/math.vector';
import { StandardMaterial } from '@babylonjs/core/Materials/standardMaterial';
import { MeshBuilder } from '@babylonjs/core/Meshes/meshBuilder';
import { TransformNode } from '@babylonjs/core/Meshes/transformNode';
import type { AssetContainer } from '@babylonjs/core/assetContainer';
import './styles.css';
import { createCoinVisual, createEnemyVisual, createGameScene, createHeroAttackEffect, createHeroCrown } from './game/createScene';
import { DEFAULT_SCENE_CONFIG } from './game/config/sceneConfig';
import { createKeyboardInputSource } from './game/input/keyboardInput';
import { createAppShell, loadStoredKeyboardBindings } from './ui/appShell';
import { advanceHeroPosition, type HeroPosition } from './game/sim/heroMovement';
import {
  advanceStage,
  applyHeroAttack,
  collectCoins,
  createStageState,
  placeTower,
  startNextWave,
  validateTowerPlacement,
  type TowerPlacement,
  type StageState,
} from './game/sim/stageSimulation';
import type { GameSelection } from './ui/appShell';
import type { GameState, NearbyPeer, NetworkTransport } from './game/sim/types';
import { createSupabaseLobbyPresence, createSupabaseRealtimeBridge } from './game/network/supabaseRealtimeBridge';
import {
  instantiateKenneyCharacter,
  loadKenneyCharacter,
  type CharacterInstance,
} from './game/assets/kenneyCharacters';
import {
  instantiateFantasyTownAsset,
  loadKenneyFantasyTownAsset,
} from './game/assets/kenneyFantasyTown';

const canvas = document.querySelector<HTMLCanvasElement>('#game-canvas');
const app = document.querySelector<HTMLElement>('#app');
const gameHud = document.querySelector<HTMLElement>('#game-hud');
const gameplayPanel = document.querySelector<HTMLElement>('#gameplay-panel');
const gameModeValue = document.querySelector<HTMLElement>('#game-mode-value');
const waveValue = document.querySelector<HTMLElement>('#wave-value');
const goldValue = document.querySelector<HTMLElement>('#gold-value');
const baseHealthValue = document.querySelector<HTMLElement>('#base-health-value');
const basicAttackValue = document.querySelector<HTMLElement>('#basic-attack-value');
const specialAttackValue = document.querySelector<HTMLElement>('#special-attack-value');
const gameplayMessage = document.querySelector<HTMLElement>('#gameplay-message');
const startWaveButton = document.querySelector<HTMLButtonElement>('#start-wave-button');
const nearbyPlayers = document.querySelector<HTMLElement>('#nearby-players');

if (!canvas || !app || !gameHud || !gameplayPanel || !gameModeValue || !waveValue || !goldValue || !baseHealthValue || !basicAttackValue || !specialAttackValue || !gameplayMessage || !startWaveButton || !nearbyPlayers) {
  throw new Error('The game shell is missing a required root element.');
}

const {
  engine,
  scene,
  shadows,
  heroRoot,
  destinationMarker,
  mapRoot,
  registerWindNode,
  updateAmbient,
} = createGameScene(canvas);
const keyboard = createKeyboardInputSource(window, loadStoredKeyboardBindings());
let stageState: StageState | null = null;
const enemyVisuals = new Map<string, TransformNode>();
const enemyCharacterInstances = new Map<string, CharacterInstance>();
// ponytail: pools retain the peak active visual count; cap and evict if endless scale makes that material.
const fallbackEnemyVisualPool: TransformNode[] = [];
const enemyCharacterPool: CharacterInstance[] = [];
const remotePlayerVisuals = new Map<string, CharacterInstance>();
const remotePlayerStates = new Map<string, { x: number; y: number; z: number; receivedAt: number }>();
const towerVisuals = new Map<string, TransformNode>();
const coinVisuals = new Map<string, TransformNode>();
const coinVisualPool: TransformNode[] = [];
const attackEffects: Array<{ root: TransformNode; expiresAt: number }> = [];
let enemyCharacterContainer: AssetContainer | null = null;
let heroCharacter: CharacterInstance | null = null;
let towerAssets: { base: AssetContainer; roof: AssetContainer; banner: AssetContainer } | null = null;
let createTowerVisual: ((tower: { id: string; x: number; z: number }) => void) | null = null;
let towerPreviewRoot: TransformNode | null = null;
let towerPreviewMaterial: StandardMaterial | null = null;
let towerPreviewIndicator: TransformNode | null = null;
let networkTransport: NetworkTransport | null = null;
const localPlayerId = `player-${globalThis.crypto?.randomUUID?.().slice(0, 8) ?? Math.random().toString(36).slice(2, 10)}`;
let networkStateTimer = 0;
let heroActionUntil = 0;
let basicAttackCooldown = 0;
let specialAttackCooldown = 0;
let networkStatusMessage: string | null = null;

const prepareCharacterAssets = async (): Promise<void> => {
  try {
    const [enemyContainer, heroContainer] = await Promise.all([
      loadKenneyCharacter(scene, 'a'),
      loadKenneyCharacter(scene, 'd'),
    ]);
    enemyCharacterContainer = enemyContainer;
    heroCharacter = instantiateKenneyCharacter(heroContainer, 'hero-king', 1.25);
    heroCharacter.root.parent = mapRoot;
    heroCharacter.root.position.copyFrom(heroRoot.position);
    createHeroCrown(scene, DEFAULT_SCENE_CONFIG).parent = heroCharacter.root;
    heroCharacter.play('idle');
    heroRoot.setEnabled(false);

    enemyVisuals.forEach((fallback, enemyId) => {
      const position = fallback.position.clone();
      fallback.dispose(false, true);
      const instance = instantiateKenneyCharacter(enemyCharacterContainer!, enemyId, 0.9);
      instance.root.parent = mapRoot;
      instance.root.position.copyFrom(position);
      instance.play('walk');
      enemyVisuals.set(enemyId, instance.root);
      enemyCharacterInstances.set(enemyId, instance);
    });
  } catch (error) {
    console.warn('Kenney characters could not be loaded; using fallback silhouettes.', error);
    // The fallback silhouettes keep the game playable when an optional asset fails to load.
  }
};

void prepareCharacterAssets();

const toLocalPlacement = (worldPoint: Vector3): TowerPlacement => {
  const mapScale = mapRoot.scaling.x;
  return { x: worldPoint.x / mapScale, z: worldPoint.z / mapScale };
};

type PlacementMesh = { metadata?: { placementSurface?: string }; parent?: PlacementMesh | null };

const placementSurfaceFor = (mesh: PlacementMesh | null | undefined): string | undefined => {
  let current = mesh;
  while (current) {
    const surface = current.metadata?.placementSurface;
    if (surface) return surface;
    current = current.parent;
  }
  return undefined;
};

const setTowerPreview = (position: TowerPlacement, valid: boolean): void => {
  if (!towerPreviewRoot || !towerPreviewMaterial) return;
  towerPreviewRoot.position.set(position.x, 0, position.z);
  const color = Color3.FromHexString(valid ? '#71C982' : '#B94A4A');
  towerPreviewMaterial.diffuseColor = color;
  towerPreviewMaterial.emissiveColor = towerPreviewMaterial.diffuseColor.scale(valid ? 0.35 : 0.2);
  towerPreviewRoot.getChildMeshes().forEach((mesh) => {
    mesh.renderOverlay = true;
    mesh.overlayColor = color;
    mesh.overlayAlpha = 0.72;
  });
  if (towerPreviewIndicator) {
    towerPreviewIndicator.getChildMeshes().forEach((mesh) => {
      mesh.material = towerPreviewMaterial;
    });
  }
  towerPreviewRoot.setEnabled(true);
};

const hideTowerPreview = (): void => {
  towerPreviewRoot?.setEnabled(false);
};

const prepareFantasyEnvironment = async (): Promise<void> => {
  try {
    const [wall, wallArchTop, doorwayBase, doorwaySquare, door, roof, roofGable, tree, treeHigh, rock, fence, banner, lantern, fountain, chimney] = await Promise.all([
      loadKenneyFantasyTownAsset(scene, 'wall'),
      loadKenneyFantasyTownAsset(scene, 'wall-arch-top'),
      loadKenneyFantasyTownAsset(scene, 'wall-doorway-base'),
      loadKenneyFantasyTownAsset(scene, 'wall-doorway-square'),
      loadKenneyFantasyTownAsset(scene, 'wall-door'),
      loadKenneyFantasyTownAsset(scene, 'roof-high'),
      loadKenneyFantasyTownAsset(scene, 'roof-high-gable'),
      loadKenneyFantasyTownAsset(scene, 'tree'),
      loadKenneyFantasyTownAsset(scene, 'tree-high'),
      loadKenneyFantasyTownAsset(scene, 'rock-large'),
      loadKenneyFantasyTownAsset(scene, 'fence'),
      loadKenneyFantasyTownAsset(scene, 'banner-green'),
      loadKenneyFantasyTownAsset(scene, 'lantern'),
      loadKenneyFantasyTownAsset(scene, 'fountain-round'),
      loadKenneyFantasyTownAsset(scene, 'chimney'),
    ]);

    scene.meshes
      .filter((mesh) => (
        /^(castle-|tree-|barricade-|lantern-|central-shrine)/.test(mesh.name)
        && !mesh.name.startsWith('lantern-flame-')
        && !mesh.name.startsWith('castle-window-')
        && mesh.name !== 'central-shrine-glow'
      ))
      .forEach((mesh) => mesh.dispose(false, false));

    const addAsset = (
      container: Parameters<typeof instantiateFantasyTownAsset>[0],
      id: string,
      position: { x: number; y: number; z: number },
      scale: number,
      rotationY = 0,
    ): void => {
      const root = instantiateFantasyTownAsset(container, id, position, scale, rotationY);
      root.parent = mapRoot;
      root.metadata = { placementSurface: 'blocked' };
      root.getChildMeshes().forEach((mesh) => {
        if (!mesh.isAnInstance) mesh.receiveShadows = true;
        mesh.metadata = { ...(mesh.metadata ?? {}), placementSurface: 'blocked' };
        shadows.addShadowCaster(mesh, true);
      });
      if (id.startsWith('greenward-tree')) {
        registerWindNode(root);
      }
    };

    const createTower = (tower: { id: string; x: number; z: number }): void => {
      if (!towerAssets || towerVisuals.has(tower.id)) return;
      const root = new TransformNode(`tower-root-${tower.id}`, scene);
      root.metadata = { placementSurface: 'blocked' };
      const addPart = (container: AssetContainer, part: string, y: number, rotationY = 0): void => {
        const asset = instantiateFantasyTownAsset(container, `${tower.id}-${part}`, { x: 0, y, z: 0 }, 1, rotationY);
        asset.parent = root;
        asset.getChildMeshes().forEach((mesh) => {
          if (!mesh.isAnInstance) mesh.receiveShadows = true;
          mesh.metadata = { ...(mesh.metadata ?? {}), placementSurface: 'blocked' };
          shadows.addShadowCaster(mesh, true);
        });
      };
      addPart(towerAssets.base, 'base', 0, Math.PI / 2);
      addPart(towerAssets.roof, 'roof', 0.9, Math.PI / 2);
      addPart(towerAssets.banner, 'banner', 0.75);
      root.parent = mapRoot;
      root.position.set(tower.x, 0, tower.z);
      towerVisuals.set(tower.id, root);
    };

    towerAssets = { base: wall, roof, banner };
    towerPreviewRoot = new TransformNode('tower-placement-preview', scene);
    towerPreviewRoot.parent = mapRoot;
    towerPreviewMaterial = new StandardMaterial('tower-placement-preview-material', scene);
    towerPreviewMaterial.diffuseColor = Color3.FromHexString('#71C982');
    towerPreviewMaterial.emissiveColor = Color3.FromHexString('#71C982').scale(0.35);
    towerPreviewMaterial.alpha = 0.58;
    towerPreviewMaterial.backFaceCulling = false;
    towerPreviewIndicator = new TransformNode('tower-placement-indicator', scene);
    towerPreviewIndicator.parent = towerPreviewRoot;
    const indicator = MeshBuilder.CreateTorus('tower-placement-indicator-ring', {
      diameter: 2.4,
      thickness: 0.12,
      tessellation: 24,
    }, scene);
    indicator.position.y = 0.08;
    indicator.material = towerPreviewMaterial;
    indicator.isPickable = false;
    indicator.parent = towerPreviewIndicator;
    const ghostTint = MeshBuilder.CreateCylinder('tower-placement-ghost-tint', {
      diameterTop: 0.16,
      diameterBottom: 1.5,
      height: 2.2,
      tessellation: 4,
    }, scene);
    ghostTint.position.y = 1.1;
    ghostTint.material = towerPreviewMaterial;
    ghostTint.isPickable = false;
    ghostTint.parent = towerPreviewRoot;
    const addPreviewPart = (container: AssetContainer, part: string, y: number, rotationY = 0): void => {
      const asset = instantiateFantasyTownAsset(container, `tower-preview-${part}`, { x: 0, y, z: 0 }, 1, rotationY);
      asset.parent = towerPreviewRoot;
      asset.getChildMeshes().forEach((mesh) => {
        mesh.isPickable = false;
      });
    };
    addPreviewPart(wall, 'base', 0, Math.PI / 2);
    addPreviewPart(roof, 'roof', 0.9, Math.PI / 2);
    addPreviewPart(banner, 'banner', 0.75);
    towerPreviewRoot.setEnabled(false);

    const castleZ = -15;
    [-9, -6, -3, 3, 6, 9].forEach((x) => addAsset(wall, `castle-wall-${x}`, { x, y: 0, z: castleZ }, 3.2, Math.PI / 2));
    addAsset(doorwayBase, 'castle-gatehouse-base', { x: 0, y: 0, z: castleZ + 0.05 }, 2.6, Math.PI / 2);
    addAsset(doorwaySquare, 'castle-gatehouse-arch', { x: 0, y: 1.35, z: castleZ + 0.05 }, 2.6, Math.PI / 2);
    addAsset(door, 'castle-gatehouse-door', { x: 0, y: 0, z: castleZ + 1.15 }, 1.7, Math.PI / 2);
    addAsset(wallArchTop, 'castle-gatehouse-top', { x: 0, y: 4, z: castleZ }, 2.45, Math.PI / 2);
    addAsset(roofGable, 'castle-gatehouse-roof', { x: 0, y: 4.55, z: castleZ }, 2.45, Math.PI / 2);
    [-11.5, 11.5].forEach((x) => {
      addAsset(wall, `castle-tower-base-${x}`, { x, y: 0, z: castleZ }, 2, Math.PI / 2);
      addAsset(doorwaySquare, `castle-tower-arch-${x}`, { x, y: 1.05, z: castleZ }, 1.5, Math.PI / 2);
      addAsset(roofGable, `castle-tower-roof-${x}`, { x, y: 2.85, z: castleZ }, 1.9, Math.PI / 2);
    });
    addAsset(banner, 'castle-banner', { x: 0, y: 3.5, z: castleZ - 0.75 }, 1.9);
    addAsset(chimney, 'castle-chimney-left', { x: -11.5, y: 3.1, z: castleZ }, 1.2, Math.PI / 2);
    addAsset(chimney, 'castle-chimney-right', { x: 11.5, y: 3.1, z: castleZ }, 1.2, Math.PI / 2);

    [
      [-27, -7, 1.9], [-29, 12, 1.55], [27, 11, 2.05], [29, -5, 1.7],
      [-22, 20, 1.45], [22, 20, 1.6],
    ].forEach(([x, z, scale], index) => addAsset(index % 2 === 0 ? treeHigh : tree, `greenward-tree-${index}`, { x, y: 0, z }, scale));
    [
      [-25, 10, 1.5], [-26, 15, 1.1], [25, 14, 1.4], [27, 8, 1.0],
      [-20, 18, 1.1], [21, 17, 1.15],
    ].forEach(([x, z, scale], index) => addAsset(rock, `greenward-rock-${index}`, { x, y: 0, z }, scale, index * 0.6));
    addAsset(fence, 'greenward-fence-left', { x: -18, y: 0, z: -10.5 }, 2, Math.PI / 2);
    addAsset(fence, 'greenward-fence-right', { x: 18, y: 0, z: -10.5 }, 2, Math.PI / 2);
    addAsset(fountain, 'greenward-fountain', { x: 0, y: 0, z: -4.2 }, 1.8);
    addAsset(lantern, 'greenward-lantern-left', { x: -4.8, y: 0, z: -13.8 }, 1.4);
    addAsset(lantern, 'greenward-lantern-right', { x: 4.8, y: 0, z: -13.8 }, 1.4);
    stageState?.towers.forEach(createTower);

    createTowerVisual = createTower;
  } catch (error) {
    console.warn('Kenney environment could not be loaded; using authored fallback geometry.', error);
    // Keep the authored foundation proxies if an optional environment asset fails to load.
  }
};

void prepareFantasyEnvironment();

const updateGameplayHud = (): void => {
  if (!stageState) return;
  gameModeValue.textContent = gameHud.dataset.gameMode === 'friend' ? 'FRIEND' : 'SOLO';
  waveValue.textContent = String(stageState.wave);
  goldValue.textContent = String(stageState.gold);
  baseHealthValue.textContent = String(stageState.baseHealth);
  basicAttackValue.textContent = basicAttackCooldown <= 0 ? 'READY' : `${basicAttackCooldown.toFixed(1)}s`;
  specialAttackValue.textContent = specialAttackCooldown <= 0 ? 'READY' : `${specialAttackCooldown.toFixed(1)}s`;
  startWaveButton.disabled = stageState.status !== 'build';
  if (stageState.status !== 'build') hideTowerPreview();
  if (networkStatusMessage) {
    gameplayMessage.textContent = networkStatusMessage;
  } else if (stageState.status === 'won') {
    gameplayMessage.textContent = 'GREENWARD SECURED · FIRST STAGE COMPLETE.';
  } else if (stageState.status === 'lost') {
    gameplayMessage.textContent = 'THE HEART HAS FALLEN · RESTART TO TRY AGAIN.';
  } else if (stageState.status === 'wave') {
    gameplayMessage.textContent = `WAVE ${stageState.wave} INCOMING · DEFEND THE ROYAL HEART.`;
  } else {
    gameplayMessage.textContent = 'Place on open ground, keep clear of the route, then start the next wave.';
  }
};

const startSelection = ({ gameMode, playMode, roomCode, password }: GameSelection): void => {
  gameHud.classList.remove('is-hidden');
  gameplayPanel.classList.remove('is-hidden');
  gameHud.dataset.gameMode = gameMode;
  gameHud.dataset.playMode = playMode;
  stageState = createStageState(playMode);
  updateGameplayHud();
  if (gameMode === 'friend' && roomCode) {
    void connectFriendTransport(roomCode, password ?? '');
  }
};

const renderNearbyPeers = (element: HTMLElement, peers: NearbyPeer[]): void => {
  element.replaceChildren();
  if (peers.length === 0) {
    element.textContent = 'NO OTHER TD PLAYERS FOUND YET · KEEP THIS VIEW OPEN';
    return;
  }
  const label = document.createElement('span');
  label.textContent = `${peers.length} TD PLAYER${peers.length === 1 ? '' : 'S'} ONLINE · SELECT TO USE ROOM`;
  element.append(label);
  peers.forEach((peer) => {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'friend-peer-button';
    button.dataset.nearbyRoom = peer.roomCode;
    const name = document.createElement('span');
    name.textContent = peer.displayName;
    const room = document.createElement('span');
    room.className = 'friend-peer-room';
    room.textContent = peer.passwordProtected ? 'LOCKED SESSION' : `ROOM ${peer.roomCode}`;
    button.append(name, room);
    element.append(button);
  });
};

const startFriendLobbyDiscovery = (element: HTMLElement, getSession: () => { roomCode: string; password: string }): (() => void) => {
  const lobby = createSupabaseLobbyPresence({
    playerId: localPlayerId,
    displayName: 'Greenward Player',
    roomCode: getSession().roomCode,
    password: getSession().password,
  });
  const unsubscribePeers = lobby.onPeers((peers) => renderNearbyPeers(element, peers));
  const unsubscribeErrors = lobby.onError((error) => { element.textContent = `FRIEND SEARCH OFFLINE · ${error.message}`; });
  void lobby.connect().catch((error: unknown) => {
    // Friend mode remains playable locally when the optional Supabase bridge is not configured.
    element.textContent = `FRIEND SEARCH OFFLINE · ${error instanceof Error ? error.message : 'Supabase is not configured.'}`;
  });
  return () => {
    unsubscribePeers();
    unsubscribeErrors();
    void lobby.disconnect();
  };
};

const connectFriendTransport = async (roomCode: string, password: string): Promise<void> => {
  if (!roomCode) return;
  networkStatusMessage = `CONNECTING TO ROOM ${roomCode} · NO LOGIN`;
  nearbyPlayers.classList.remove('is-hidden');
  networkTransport = createSupabaseRealtimeBridge({ roomCode, password, playerId: localPlayerId, displayName: 'Greenward Player' });
  networkTransport.onError((error) => {
    networkStatusMessage = `FRIEND BRIDGE OFFLINE · ${error.message}`;
  });
  networkTransport.onState((state) => {
    Object.entries(state.players).forEach(([playerId, player]) => {
      if (playerId !== localPlayerId) {
        remotePlayerStates.set(playerId, { ...player, receivedAt: performance.now() });
      }
    });
  });
  try {
    await networkTransport.connect();
    networkStatusMessage = `FRIEND BRIDGE READY · ROOM ${roomCode}`;
  } catch {
    // The local game remains playable when the optional Friend-mode environment is not configured.
  }
};

createAppShell(app, keyboard, { onStartGame: startSelection, onFriendLobbyReady: startFriendLobbyDiscovery });

startWaveButton.addEventListener('click', () => {
  if (!stageState) return;
  stageState = startNextWave(stageState);
  updateGameplayHud();
});

let destination: HeroPosition | null = null;
let pointerDownPosition: { x: number; y: number } | null = null;

scene.onPointerObservable.add((pointerInfo) => {
  const event = pointerInfo.event as PointerEvent;

  if (pointerInfo.type === PointerEventTypes.POINTERMOVE) {
    if (!stageState || stageState.status !== 'build' || !towerPreviewRoot) return;
    const pick = scene.pick(scene.pointerX, scene.pointerY);
    if (!pick?.hit || !pick.pickedPoint) {
      hideTowerPreview();
      return;
    }
    const position = toLocalPlacement(pick.pickedPoint);
    const surface = placementSurfaceFor(pick.pickedMesh);
    const validation = surface === 'ground' ? validateTowerPlacement(stageState, position) : { valid: false };
    setTowerPreview(position, validation.valid);
    return;
  }

  if (pointerInfo.type === PointerEventTypes.POINTERDOWN && event.button === 0) {
    pointerDownPosition = { x: event.clientX, y: event.clientY };
    return;
  }

  if (pointerInfo.type !== PointerEventTypes.POINTERUP || event.button !== 0 || !pointerDownPosition) {
    return;
  }

  const pointerTravel = Math.hypot(event.clientX - pointerDownPosition.x, event.clientY - pointerDownPosition.y);
  pointerDownPosition = null;
  if (pointerTravel > 8) {
    return;
  }

  const pick = scene.pick(scene.pointerX, scene.pointerY);
  if (!pick?.hit || !pick.pickedPoint) {
    return;
  }

  const interaction = pick.pickedMesh?.metadata?.interaction;
  const surface = placementSurfaceFor(pick.pickedMesh);
  if (stageState?.status === 'build') {
    if (surface !== 'ground') return;
    const position = toLocalPlacement(pick.pickedPoint);
    const placement = placeTower(stageState, position);
    stageState = placement.state;
    if (placement.tower) {
      createTowerVisual?.(placement.tower);
      setTowerPreview(position, false);
    }
    updateGameplayHud();
    return;
  }
  if (interaction !== 'map') {
    return;
  }

  const mapScale = mapRoot.scaling.x;
  destination = { x: pick.pickedPoint.x / mapScale, y: 0, z: pick.pickedPoint.z / mapScale };
  destinationMarker.position.set(destination.x, 0.12, destination.z);
  destinationMarker.isVisible = true;
});

const resize = (): void => engine.resize();
window.addEventListener('resize', resize);

let lastFrameTime = performance.now();
engine.runRenderLoop(() => {
  const now = performance.now();
  const deltaSeconds = Math.min((now - lastFrameTime) / 1000, 0.1);
  lastFrameTime = now;
  updateAmbient(now / 1000);

  const input = keyboard.read(localPlayerId, now);
  networkTransport?.sendInput(input);
  networkStateTimer -= deltaSeconds;
  if (networkTransport && networkStateTimer <= 0) {
    const phase: GameState['phase'] = !stageState
      ? 'foundation'
      : stageState.status === 'won'
        ? 'victory'
        : stageState.status === 'lost'
          ? 'defeat'
          : 'playing';
    networkTransport.sendState({
      phase,
      stageId: 'greenward-01',
      cameraTarget: { x: heroRoot.position.x, y: heroRoot.position.y, z: heroRoot.position.z },
      players: {
        [localPlayerId]: { x: heroRoot.position.x, y: heroRoot.position.y, z: heroRoot.position.z, health: 100 },
      },
    });
    networkStateTimer = 1 / 15;
  }
  basicAttackCooldown = Math.max(0, basicAttackCooldown - deltaSeconds);
  specialAttackCooldown = Math.max(0, specialAttackCooldown - deltaSeconds);
  const basicAttackReady = input.basicAttack && basicAttackCooldown <= 0;
  const specialAttackReady = input.specialAttack && specialAttackCooldown <= 0;
  if (stageState && (basicAttackReady || specialAttackReady)) {
    const attack = specialAttackReady ? 'special' : 'basic';
    stageState = applyHeroAttack(stageState, heroRoot.position, attack);
    const effect = createHeroAttackEffect(scene, DEFAULT_SCENE_CONFIG, String(now), attack);
    effect.parent = mapRoot;
    effect.position.copyFrom(heroRoot.position);
    attackEffects.push({ root: effect, expiresAt: now + (attack === 'special' ? 3200 : 1200) });
  }
  const movement = advanceHeroPosition(
    { x: heroRoot.position.x, y: heroRoot.position.y, z: heroRoot.position.z },
    input,
    destination,
    deltaSeconds,
  );
  destination = movement.destination;
  heroRoot.position.set(movement.position.x, movement.position.y, movement.position.z);
  if (heroCharacter) {
    heroCharacter.root.position.copyFrom(heroRoot.position);
    const isMoving = Math.hypot(input.moveX, input.moveZ) > 0 || movement.destination !== null;
    if (specialAttackReady) {
      heroCharacter.play('attack-kick-right', false);
      heroActionUntil = now + 650;
      specialAttackCooldown = 5;
    } else if (basicAttackReady) {
      heroCharacter.play('attack-melee-right', false);
      heroActionUntil = now + 520;
      basicAttackCooldown = 0.9;
    } else if (now >= heroActionUntil) {
      heroCharacter.play(isMoving ? 'walk' : 'idle');
    }
    if (isMoving) {
      heroCharacter.root.rotation.y = Math.atan2(input.moveX, input.moveZ);
    }
  }
  destinationMarker.isVisible = destination !== null;

  if (stageState) {
    stageState = advanceStage(stageState, deltaSeconds);
    stageState = collectCoins(stageState, { x: heroRoot.position.x, z: heroRoot.position.z }).state;
    const activeEnemyIds = new Set(stageState.enemies.map((enemy) => enemy.id));
    stageState.enemies.forEach((enemy) => {
      let enemyVisual = enemyVisuals.get(enemy.id);
      if (!enemyVisual) {
        if (enemyCharacterContainer) {
          const instance = enemyCharacterPool.pop() ?? instantiateKenneyCharacter(enemyCharacterContainer, enemy.id, 0.9);
          instance.root.setEnabled(true);
          instance.play('walk');
          enemyCharacterInstances.set(enemy.id, instance);
          enemyVisual = instance.root;
        } else {
          enemyVisual = fallbackEnemyVisualPool.pop() ?? createEnemyVisual(scene, DEFAULT_SCENE_CONFIG, enemy.id);
          enemyVisual.setEnabled(true);
        }
        enemyVisual.parent = mapRoot;
        enemyVisuals.set(enemy.id, enemyVisual);
      }
      enemyVisual.position.set(enemy.x, 0, enemy.z);
    });
    enemyVisuals.forEach((enemyVisual, enemyId) => {
      if (!activeEnemyIds.has(enemyId)) {
        const characterInstance = enemyCharacterInstances.get(enemyId);
        if (characterInstance) {
          characterInstance.root.setEnabled(false);
          enemyCharacterPool.push(characterInstance);
          enemyCharacterInstances.delete(enemyId);
        } else {
          enemyVisual.setEnabled(false);
          fallbackEnemyVisualPool.push(enemyVisual);
        }
        enemyVisuals.delete(enemyId);
      }
    });
    const activeCoinIds = new Set(stageState.coins.map((coin) => coin.id));
    stageState.coins.forEach((coin) => {
      let coinVisual = coinVisuals.get(coin.id);
      if (!coinVisual) {
        coinVisual = coinVisualPool.pop() ?? createCoinVisual(scene, DEFAULT_SCENE_CONFIG, coin.id);
        coinVisual.setEnabled(true);
        coinVisual.parent = mapRoot;
        coinVisuals.set(coin.id, coinVisual);
      }
      coinVisual.position.set(coin.x, 0.42, coin.z);
      coinVisual.rotation.y += deltaSeconds * 3;
    });
    coinVisuals.forEach((coinVisual, coinId) => {
      if (!activeCoinIds.has(coinId)) {
        coinVisual.setEnabled(false);
        coinVisualPool.push(coinVisual);
        coinVisuals.delete(coinId);
      }
    });
    if (enemyCharacterContainer) {
      remotePlayerStates.forEach((remote, playerId) => {
        let visual = remotePlayerVisuals.get(playerId);
        if (!visual) {
          visual = instantiateKenneyCharacter(enemyCharacterContainer!, `remote-${playerId}`, 1.05);
          visual.root.parent = mapRoot;
          visual.play('idle');
          remotePlayerVisuals.set(playerId, visual);
        }
        visual.root.position.set(remote.x, remote.y, remote.z);
      });
      remotePlayerVisuals.forEach((visual, playerId) => {
        const remote = remotePlayerStates.get(playerId);
        if (!remote || now - remote.receivedAt > 5000) {
          visual.dispose();
          remotePlayerVisuals.delete(playerId);
          remotePlayerStates.delete(playerId);
        }
      });
    }
    updateGameplayHud();
  }

  for (let index = attackEffects.length - 1; index >= 0; index -= 1) {
    const effect = attackEffects[index];
    effect.root.rotation.y += deltaSeconds * 6;
    if (now >= effect.expiresAt) {
      effect.root.dispose(false, true);
      attackEffects.splice(index, 1);
    }
  }

  scene.render();
});
