import { describe, expect, it } from 'vitest';
import { DEFAULT_SCENE_CONFIG } from './config/sceneConfig';
import { getCameraSettings } from './createScene';

describe('Babylon scene camera bridge', () => {
  it('maps the strategic camera config without changing its limits', () => {
    expect(getCameraSettings(DEFAULT_SCENE_CONFIG)).toEqual(DEFAULT_SCENE_CONFIG.camera);
  });
});
