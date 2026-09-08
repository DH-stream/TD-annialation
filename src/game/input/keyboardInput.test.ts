import { describe, expect, it } from 'vitest';
import { createKeyboardInputState, type KeyboardBindings } from './keyboardInput';

describe('keyboard input', () => {
  it('maps WASD to a normalized movement vector', () => {
    const input = createKeyboardInputState();

    input.press('w');
    input.press('a');

    expect(input.read('player-1', 100)).toMatchObject({
      playerId: 'player-1',
      moveX: expect.closeTo(Math.SQRT1_2),
      moveZ: expect.closeTo(-Math.SQRT1_2),
      basicAttack: false,
      specialAttack: false,
      issuedAt: 100,
    });
  });

  it('maps D to the negative horizontal axis after the A/D correction', () => {
    const input = createKeyboardInputState();

    input.press('d');

    expect(input.read('player-1', 100).moveX).toBe(-1);
  });

  it('supports remappable movement keys and releases input cleanly', () => {
    const bindings: KeyboardBindings = { up: 'i', down: 'k', left: 'j', right: 'l' };
    const input = createKeyboardInputState(bindings);

    input.press('l');
    expect(input.read('player-1', 100).moveX).toBe(1);

    input.release('l');
    expect(input.read('player-1', 200)).toMatchObject({ moveX: 0, moveZ: 0 });
  });

  it('updates the live bindings used by the input source', () => {
    const input = createKeyboardInputState();

    input.setBindings({ up: 'i', down: 'k', left: 'j', right: 'l' });
    input.press('l');

    expect(input.read('player-1', 300).moveX).toBe(1);
    expect(input.getBindings()).toEqual({ up: 'i', down: 'k', left: 'j', right: 'l' });
  });

  it('queues one basic or special attack per key press', () => {
    const input = createKeyboardInputState();

    input.press('j');
    expect(input.read('player-1', 100).basicAttack).toBe(true);
    expect(input.read('player-1', 116).basicAttack).toBe(false);

    input.press('k');
    expect(input.read('player-1', 132).specialAttack).toBe(true);
  });
});
