export type SceneConfig = {
  battlefield: {
    width: number;
    height: number;
  };
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
    horizon: string;
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
  battlefield: {
    width: 72,
    height: 52,
  },
  camera: {
    alpha: Math.PI / 2,
    beta: 0.72,
    radius: 52,
    lowerBetaLimit: 0.55,
    upperBetaLimit: 1.05,
    lowerRadiusLimit: 30,
    upperRadiusLimit: 90,
  },
  colors: {
    horizon: '#142523',
    ground: '#3F5D47',
    path: '#B98A57',
    stone: '#59676D',
    wood: '#7B4B34',
    brass: '#D2A85B',
    magic: '#D2A85B',
    blood: '#8E2F2B',
  },
};

export function clampCameraPitch(pitchRadians: number): number {
  return Math.min(
    DEFAULT_SCENE_CONFIG.camera.upperBetaLimit,
    Math.max(DEFAULT_SCENE_CONFIG.camera.lowerBetaLimit, pitchRadians),
  );
}
