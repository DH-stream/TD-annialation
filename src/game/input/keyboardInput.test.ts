import { describe, expect, it } from 'vitest';
import { createKeyboardInputState, type KeyboardBindings } from './keyboardInput';

describe('keyboard input', () => {
  it('maps WASD to a normalized movement vector', () => {
    const input = createKeyboardInputState();

    input.press('w');
    input.press('d');

    expect(input.read('player-1', 100)).toMatchObject({
      playerId: 'player-1',
      moveX: expect.closeTo(Math.SQRT1_2),
      moveZ: expect.closeTo(-Math.SQRT1_2),
      basicAttack: false,
      specialAttack: false,
      issuedAt: 100,
    });
  });

  it('supports remappable movement keys and releases input cleanly', () => {
    const bindings: KeyboardBindings = { up: 'i', down: 'k', left: 'j', right: 'l' };
    const input = createKeyboardInputState(bindings);

    input.press('l');
    expect(input.read('player-1', 100).moveX).toBe(1);

    input.release('l');
    expect(input.read('player-1', 200)).toMatchObject({ moveX: 0, moveZ: 0 });
  });
});
