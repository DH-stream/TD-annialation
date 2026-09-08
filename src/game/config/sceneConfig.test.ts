import { describe, expect, it } from 'vitest';
import { DEFAULT_SCENE_CONFIG, clampCameraPitch } from './sceneConfig';

describe('scene configuration', () => {
  it('uses a strategic top-down default camera angle', () => {
    expect(DEFAULT_SCENE_CONFIG.camera.alpha).toBeCloseTo(Math.PI / 2);
    expect(DEFAULT_SCENE_CONFIG.camera.beta).toBeCloseTo(0.72);
    expect(DEFAULT_SCENE_CONFIG.camera.radius).toBe(62);
    expect(DEFAULT_SCENE_CONFIG.camera.lowerRadiusLimit).toBeLessThan(
      DEFAULT_SCENE_CONFIG.camera.upperRadiusLimit,
    );
  });

  it('clamps camera pitch to the readable battlefield range', () => {
    expect(clampCameraPitch(0)).toBeCloseTo(DEFAULT_SCENE_CONFIG.camera.lowerBetaLimit);
    expect(clampCameraPitch(Math.PI)).toBeCloseTo(DEFAULT_SCENE_CONFIG.camera.upperBetaLimit);
  });

  it('uses the locked Greenward ground anchor', () => {
    expect(DEFAULT_SCENE_CONFIG.colors.ground).toBe('#3F5D47');
  });

  it('uses the locked Greenward horizon palette value', () => {
    expect(DEFAULT_SCENE_CONFIG.colors.horizon).toBe('#142523');
  });

  it('defines a genuinely expanded Greenward battlefield', () => {
    const config = DEFAULT_SCENE_CONFIG as typeof DEFAULT_SCENE_CONFIG & {
      battlefield?: { width: number; height: number };
    };

    expect(config.battlefield).toEqual({ width: 72, height: 52 });
  });
});
