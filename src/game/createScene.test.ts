import { describe, expect, it } from 'vitest';
import { NullEngine } from '@babylonjs/core/Engines/nullEngine';
import { FreeCamera } from '@babylonjs/core/Cameras/freeCamera';
import { Matrix, Vector3 } from '@babylonjs/core/Maths/math.vector';
import { Scene } from '@babylonjs/core/scene';
import { DEFAULT_SCENE_CONFIG } from './config/sceneConfig';
import { getCameraSettings } from './createScene';

describe('Babylon scene camera bridge', () => {
  it('maps the strategic camera config without changing its limits', () => {
    expect(getCameraSettings(DEFAULT_SCENE_CONFIG)).toEqual(DEFAULT_SCENE_CONFIG.camera);
  });

  it('registers Babylon ray picking for map interactions', () => {
    const engine = new NullEngine();
    const scene = new Scene(engine);
    const camera = new FreeCamera('test-camera', new Vector3(0, 8, -8), scene);

    expect(() => scene.createPickingRay(10, 10, Matrix.Identity(), camera)).not.toThrow();

    scene.dispose();
    engine.dispose();
  });
});
