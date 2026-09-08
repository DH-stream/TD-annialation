export type GameMode = 'solo' | 'friend';
export type PlayMode = 'stages' | 'endless';
export type MenuScreen = 'main' | 'play-mode' | 'settings' | 'skill-tree';

export type MenuState = {
  screen: MenuScreen;
  gameMode: GameMode | null;
  playMode: PlayMode | null;
};

export function createInitialMenuState(): MenuState {
  return { screen: 'main', gameMode: null, playMode: null };
}

export function selectGameMode(state: MenuState, gameMode: GameMode): MenuState {
  return { ...state, screen: 'play-mode', gameMode };
}

export function selectPlayMode(state: MenuState, playMode: PlayMode): MenuState {
  return { ...state, screen: 'play-mode', playMode };
}
