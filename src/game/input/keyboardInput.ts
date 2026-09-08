import type { PlayerInput } from '../sim/types';

export type KeyboardBindings = {
  up: string;
  down: string;
  left: string;
  right: string;
};

export const DEFAULT_KEYBOARD_BINDINGS: KeyboardBindings = {
  up: 'w',
  down: 's',
  left: 'd',
  right: 'a',
};

export type KeyboardInputState = {
  press(key: string): void;
  release(key: string): void;
  read(playerId: string, issuedAt: number): PlayerInput;
};

function normalizeKey(key: string): string {
  return key.trim().toLowerCase();
}

function axis(pressedKeys: Set<string>, positiveKey: string, negativeKey: string): number {
  return Number(pressedKeys.has(normalizeKey(positiveKey))) - Number(pressedKeys.has(normalizeKey(negativeKey)));
}

export function createKeyboardInputState(bindings: KeyboardBindings = DEFAULT_KEYBOARD_BINDINGS): KeyboardInputState {
  const pressedKeys = new Set<string>();

  return {
    press: (key) => pressedKeys.add(normalizeKey(key)),
    release: (key) => pressedKeys.delete(normalizeKey(key)),
    read: (playerId, issuedAt) => {
      const rawMoveX = axis(pressedKeys, bindings.right, bindings.left);
      const rawMoveZ = axis(pressedKeys, bindings.down, bindings.up);
      const length = Math.hypot(rawMoveX, rawMoveZ);
      const scale = length > 1 ? 1 / length : 1;

      return {
        playerId,
        moveX: rawMoveX * scale,
        moveZ: rawMoveZ * scale,
        basicAttack: false,
        specialAttack: false,
        issuedAt,
      };
    },
  };
}

export function createKeyboardInputSource(
  target: Window,
  bindings: KeyboardBindings = DEFAULT_KEYBOARD_BINDINGS,
): KeyboardInputState & { dispose(): void } {
  const state = createKeyboardInputState(bindings);
  const movementKeys = new Set(Object.values(bindings).map(normalizeKey));
  const onKeyDown = (event: KeyboardEvent): void => {
    if (movementKeys.has(normalizeKey(event.key))) {
      event.preventDefault();
      state.press(event.key);
    }
  };
  const onKeyUp = (event: KeyboardEvent): void => state.release(event.key);

  target.addEventListener('keydown', onKeyDown);
  target.addEventListener('keyup', onKeyUp);

  return {
    ...state,
    dispose: () => {
      target.removeEventListener('keydown', onKeyDown);
      target.removeEventListener('keyup', onKeyUp);
    },
  };
}
