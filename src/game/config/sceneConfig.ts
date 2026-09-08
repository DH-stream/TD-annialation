export type SceneConfig = {
  camera: {
    alpha: number;
    beta: number;
    radius: number;
    lowerBetaLimit: number;
    upperBetaLimit: number;
    lowerRadiusLimit: number;
    upperRadiusLimit: number;
  };
  colors: {
    ground: string;
    path: string;
    stone: string;
    wood: string;
    brass: string;
    magic: string;
    blood: string;
  };
};

export const DEFAULT_SCENE_CONFIG: SceneConfig = {
  camera: {
    alpha: -Math.PI / 2,
    beta: Math.PI / 3,
    radius: 28,
    lowerBetaLimit: Math.PI * 0.24,
    upperBetaLimit: Math.PI * 0.44,
    lowerRadiusLimit: 14,
    upperRadiusLimit: 44,
  },
  colors: {
    ground: '#304936',
    path: '#ad8a5b',
    stone: '#5c6870',
    wood: '#6d432c',
    brass: '#c59b52',
    magic: '#5ec7e8',
    blood: '#6b2422',
  },
};

export function clampCameraPitch(pitchRadians: number): number {
  return Math.min(
    DEFAULT_SCENE_CONFIG.camera.upperBetaLimit,
    Math.max(DEFAULT_SCENE_CONFIG.camera.lowerBetaLimit, pitchRadians),
  );
}
