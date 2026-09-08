import 'babylonjs';
import './styles.css';
import { createGameScene } from './game/createScene';

const canvas = document.querySelector<HTMLCanvasElement>('#game-canvas');

if (!canvas) {
  throw new Error('Game canvas was not found.');
}

const { engine, scene } = createGameScene(canvas);

const resize = (): void => engine.resize();
window.addEventListener('resize', resize);
engine.runRenderLoop(() => scene.render());
