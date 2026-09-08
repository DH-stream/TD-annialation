import { PointerEventTypes } from 'babylonjs';
import './styles.css';
import { createGameScene } from './game/createScene';
import { createKeyboardInputSource } from './game/input/keyboardInput';
import { advanceHeroPosition, type HeroPosition } from './game/sim/heroMovement';

const canvas = document.querySelector<HTMLCanvasElement>('#game-canvas');

if (!canvas) {
  throw new Error('Game canvas was not found.');
}

const { engine, scene, heroRoot, destinationMarker } = createGameScene(canvas);
const keyboard = createKeyboardInputSource(window);
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

  const pick = scene.pick(
    scene.pointerX,
    scene.pointerY,
    (mesh) => mesh.metadata?.interaction === 'map',
  );
  if (!pick?.hit || !pick.pickedPoint) {
    return;
  }

  destination = { x: pick.pickedPoint.x, y: 0, z: pick.pickedPoint.z };
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

  scene.render();
});
