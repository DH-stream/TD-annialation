import { PointerEventTypes } from '@babylonjs/core/Events/pointerEvents';
import { TransformNode } from '@babylonjs/core/Meshes/transformNode';
import type { AssetContainer } from '@babylonjs/core/assetContainer';
import './styles.css';
import { createCoinVisual, createEnemyVisual, createGameScene, createTowerVisual } from './game/createScene';
import { DEFAULT_SCENE_CONFIG } from './game/config/sceneConfig';
import { createKeyboardInputSource } from './game/input/keyboardInput';
import { createAppShell, loadStoredKeyboardBindings } from './ui/appShell';
import { advanceHeroPosition, type HeroPosition } from './game/sim/heroMovement';
import {
  advanceStage,
  collectCoins,
  createStageState,
  placeTower,
  startNextWave,
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

const { engine, scene, heroRoot, destinationMarker, mapRoot, buildPads } = createGameScene(canvas);
const keyboard = createKeyboardInputSource(window, loadStoredKeyboardBindings());
let stageState: StageState | null = null;
const enemyVisuals = new Map<string, TransformNode>();
const enemyCharacterInstances = new Map<string, CharacterInstance>();
const remotePlayerVisuals = new Map<string, CharacterInstance>();
const remotePlayerStates = new Map<string, { x: number; y: number; z: number; receivedAt: number }>();
const towerVisuals = new Map<string, TransformNode>();
const coinVisuals = new Map<string, TransformNode>();
let enemyCharacterContainer: AssetContainer | null = null;
let heroCharacter: CharacterInstance | null = null;
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
  } catch {
    // The fallback silhouettes keep the game playable when an optional asset fails to load.
  }
};

void prepareCharacterAssets();

const updateGameplayHud = (): void => {
  if (!stageState) return;
  gameModeValue.textContent = gameHud.dataset.gameMode === 'friend' ? 'FRIEND' : 'SOLO';
  waveValue.textContent = String(stageState.wave);
  goldValue.textContent = String(stageState.gold);
  baseHealthValue.textContent = String(stageState.baseHealth);
  basicAttackValue.textContent = basicAttackCooldown <= 0 ? 'READY' : `${basicAttackCooldown.toFixed(1)}s`;
  specialAttackValue.textContent = specialAttackCooldown <= 0 ? 'READY' : `${specialAttackCooldown.toFixed(1)}s`;
  startWaveButton.disabled = stageState.status !== 'build';
  if (networkStatusMessage) {
    gameplayMessage.textContent = networkStatusMessage;
  } else if (stageState.status === 'won') {
    gameplayMessage.textContent = 'GREENWARD SECURED · FIRST STAGE COMPLETE.';
  } else if (stageState.status === 'lost') {
    gameplayMessage.textContent = 'THE HEART HAS FALLEN · RESTART TO TRY AGAIN.';
  } else if (stageState.status === 'wave') {
    gameplayMessage.textContent = `WAVE ${stageState.wave} INCOMING · DEFEND THE ROYAL HEART.`;
  } else {
    gameplayMessage.textContent = 'Build on a rune pad, then start the next wave.';
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
  if (interaction === 'build-pad' && stageState) {
    const buildPadId = pick.pickedMesh?.metadata?.buildPadId as number | undefined;
    const pad = buildPads.find((candidate) => candidate.id === buildPadId);
    if (!pad) return;
    const placement = placeTower(stageState, { x: pad.position.x, z: pad.position.z });
    stageState = placement.state;
    if (placement.tower) {
      const towerVisual = createTowerVisual(scene, DEFAULT_SCENE_CONFIG, placement.tower.id);
      towerVisual.parent = mapRoot;
      towerVisual.position.set(placement.tower.x, 0, placement.tower.z);
      towerVisuals.set(placement.tower.id, towerVisual);
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
          const instance = instantiateKenneyCharacter(enemyCharacterContainer, enemy.id, 0.9);
          instance.play('walk');
          enemyCharacterInstances.set(enemy.id, instance);
          enemyVisual = instance.root;
        } else {
          enemyVisual = createEnemyVisual(scene, DEFAULT_SCENE_CONFIG, enemy.id);
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
          characterInstance.dispose();
          enemyCharacterInstances.delete(enemyId);
        } else {
          enemyVisual.dispose(false, true);
        }
        enemyVisuals.delete(enemyId);
      }
    });
    const activeCoinIds = new Set(stageState.coins.map((coin) => coin.id));
    stageState.coins.forEach((coin) => {
      let coinVisual = coinVisuals.get(coin.id);
      if (!coinVisual) {
        coinVisual = createCoinVisual(scene, DEFAULT_SCENE_CONFIG, coin.id);
        coinVisual.parent = mapRoot;
        coinVisuals.set(coin.id, coinVisual);
      }
      coinVisual.position.set(coin.x, 0.42, coin.z);
      coinVisual.rotation.y += deltaSeconds * 3;
    });
    coinVisuals.forEach((coinVisual, coinId) => {
      if (!activeCoinIds.has(coinId)) {
        coinVisual.dispose(false, true);
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

  scene.render();
});
