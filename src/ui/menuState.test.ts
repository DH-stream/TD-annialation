import { describe, expect, it } from 'vitest';
import { createInitialMenuState, selectGameMode, selectPlayMode } from './menuState';

describe('menu flow state', () => {
  it('starts at the main menu and advances from game mode to play mode', () => {
    const initial = createInitialMenuState();
    const friendMode = selectGameMode(initial, 'friend');

    expect(initial.screen).toBe('main');
    expect(friendMode).toMatchObject({ screen: 'play-mode', gameMode: 'friend' });
  });

  it('keeps the selected game mode when a play mode is chosen', () => {
    const state = selectGameMode(createInitialMenuState(), 'solo');
    const selected = selectPlayMode(state, 'endless');

    expect(selected).toMatchObject({ screen: 'play-mode', gameMode: 'solo', playMode: 'endless' });
  });
});
