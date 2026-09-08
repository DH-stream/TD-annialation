import { PointerEventTypes, TransformNode } from 'babylonjs';
import './styles.css';
import { createEnemyVisual, createGameScene, createTowerVisual } from './game/createScene';
import { DEFAULT_SCENE_CONFIG } from './game/config/sceneConfig';
import { createKeyboardInputSource } from './game/input/keyboardInput';
import { createAppShell, loadStoredKeyboardBindings } from './ui/appShell';
import { advanceHeroPosition, type HeroPosition } from './game/sim/heroMovement';
import {
  advanceStage,
  createStageState,
  placeTower,
  startNextWave,
  type StageState,
} from './game/sim/stageSimulation';
import type { GameSelection } from './ui/appShell';

const canvas = document.querySelector<HTMLCanvasElement>('#game-canvas');
const app = document.querySelector<HTMLElement>('#app');
const gameHud = document.querySelector<HTMLElement>('#game-hud');
const gameplayPanel = document.querySelector<HTMLElement>('#gameplay-panel');
const gameModeValue = document.querySelector<HTMLElement>('#game-mode-value');
const waveValue = document.querySelector<HTMLElement>('#wave-value');
const goldValue = document.querySelector<HTMLElement>('#gold-value');
const baseHealthValue = document.querySelector<HTMLElement>('#base-health-value');
const gameplayMessage = document.querySelector<HTMLElement>('#gameplay-message');
const startWaveButton = document.querySelector<HTMLButtonElement>('#start-wave-button');

if (!canvas || !app || !gameHud || !gameplayPanel || !gameModeValue || !waveValue || !goldValue || !baseHealthValue || !gameplayMessage || !startWaveButton) {
  throw new Error('The game shell is missing a required root element.');
}

const { engine, scene, heroRoot, destinationMarker, mapRoot, buildPads } = createGameScene(canvas);
const keyboard = createKeyboardInputSource(window, loadStoredKeyboardBindings());
let stageState: StageState | null = null;
const enemyVisuals = new Map<string, TransformNode>();
const towerVisuals = new Map<string, TransformNode>();

const updateGameplayHud = (): void => {
  if (!stageState) return;
  gameModeValue.textContent = gameHud.dataset.gameMode === 'friend' ? 'FRIEND' : 'SOLO';
  waveValue.textContent = String(stageState.wave);
  goldValue.textContent = String(stageState.gold);
  baseHealthValue.textContent = String(stageState.baseHealth);
  startWaveButton.disabled = stageState.status !== 'build';
  if (stageState.status === 'won') {
    gameplayMessage.textContent = 'GREENWARD SECURED · FIRST STAGE COMPLETE.';
  } else if (stageState.status === 'lost') {
    gameplayMessage.textContent = 'THE HEART HAS FALLEN · RESTART TO TRY AGAIN.';
  } else if (stageState.status === 'wave') {
    gameplayMessage.textContent = `WAVE ${stageState.wave} INCOMING · DEFEND THE ROYAL HEART.`;
  } else {
    gameplayMessage.textContent = 'Build on a rune pad, then start the next wave.';
  }
};

const startSelection = ({ gameMode, playMode }: GameSelection): void => {
  gameHud.classList.remove('is-hidden');
  gameplayPanel.classList.remove('is-hidden');
  gameHud.dataset.gameMode = gameMode;
  gameHud.dataset.playMode = playMode;
  stageState = createStageState(playMode);
  updateGameplayHud();
};

createAppShell(app, keyboard, { onStartGame: startSelection });

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

  const input = keyboard.read('player-1', now);
  const movement = advanceHeroPosition(
    { x: heroRoot.position.x, y: heroRoot.position.y, z: heroRoot.position.z },
    input,
    destination,
    deltaSeconds,
  );
  destination = movement.destination;
  heroRoot.position.set(movement.position.x, movement.position.y, movement.position.z);
  destinationMarker.isVisible = destination !== null;

  if (stageState) {
    stageState = advanceStage(stageState, deltaSeconds);
    const activeEnemyIds = new Set(stageState.enemies.map((enemy) => enemy.id));
    stageState.enemies.forEach((enemy) => {
      let enemyVisual = enemyVisuals.get(enemy.id);
      if (!enemyVisual) {
        enemyVisual = createEnemyVisual(scene, DEFAULT_SCENE_CONFIG, enemy.id);
        enemyVisual.parent = mapRoot;
        enemyVisuals.set(enemy.id, enemyVisual);
      }
      enemyVisual.position.set(enemy.x, 0, enemy.z);
    });
    enemyVisuals.forEach((enemyVisual, enemyId) => {
      if (!activeEnemyIds.has(enemyId)) {
        enemyVisual.dispose(false, true);
        enemyVisuals.delete(enemyId);
      }
    });
    updateGameplayHud();
  }

  scene.render();
});
