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
