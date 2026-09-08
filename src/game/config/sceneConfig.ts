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
    alpha: Math.PI / 2,
    beta: Math.PI / 3,
    radius: 44,
    lowerBetaLimit: Math.PI * 0.24,
    upperBetaLimit: Math.PI * 0.44,
    lowerRadiusLimit: 22,
    upperRadiusLimit: 68,
  },
  colors: {
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
