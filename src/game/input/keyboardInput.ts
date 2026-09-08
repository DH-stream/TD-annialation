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
  setBindings(bindings: KeyboardBindings): void;
  getBindings(): KeyboardBindings;
  read(playerId: string, issuedAt: number): PlayerInput;
};

export const BASIC_ATTACK_KEY = 'j';
export const SPECIAL_ATTACK_KEY = 'k';

function normalizeKey(key: string): string {
  return key.trim().toLowerCase();
}

function axis(pressedKeys: Set<string>, positiveKey: string, negativeKey: string): number {
  return Number(pressedKeys.has(normalizeKey(positiveKey))) - Number(pressedKeys.has(normalizeKey(negativeKey)));
}

export function createKeyboardInputState(bindings: KeyboardBindings = DEFAULT_KEYBOARD_BINDINGS): KeyboardInputState {
  const pressedKeys = new Set<string>();
  let basicAttackQueued = false;
  let specialAttackQueued = false;
  let activeBindings = { ...bindings };

  return {
    press: (key) => {
      const normalizedKey = normalizeKey(key);
      pressedKeys.add(normalizedKey);
      if (normalizedKey === BASIC_ATTACK_KEY) basicAttackQueued = true;
      if (normalizedKey === SPECIAL_ATTACK_KEY) specialAttackQueued = true;
    },
    release: (key) => pressedKeys.delete(normalizeKey(key)),
    setBindings: (nextBindings) => {
      activeBindings = { ...nextBindings };
      pressedKeys.clear();
      basicAttackQueued = false;
      specialAttackQueued = false;
    },
    getBindings: () => ({ ...activeBindings }),
    read: (playerId, issuedAt) => {
      const rawMoveX = axis(pressedKeys, activeBindings.right, activeBindings.left);
      const rawMoveZ = axis(pressedKeys, activeBindings.down, activeBindings.up);
      const length = Math.hypot(rawMoveX, rawMoveZ);
      const scale = length > 1 ? 1 / length : 1;

      const input = {
        playerId,
        moveX: rawMoveX * scale,
        moveZ: rawMoveZ * scale,
        basicAttack: basicAttackQueued,
        specialAttack: specialAttackQueued,
        issuedAt,
      };
      basicAttackQueued = false;
      specialAttackQueued = false;
      return input;
    },
  };
}

export function createKeyboardInputSource(
  target: Window,
  bindings: KeyboardBindings = DEFAULT_KEYBOARD_BINDINGS,
): KeyboardInputState & { dispose(): void } {
  const state = createKeyboardInputState(bindings);
  let movementKeys = new Set([...Object.values(bindings), BASIC_ATTACK_KEY, SPECIAL_ATTACK_KEY].map(normalizeKey));
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
    setBindings: (nextBindings) => {
      state.setBindings(nextBindings);
      movementKeys = new Set([...Object.values(nextBindings), BASIC_ATTACK_KEY, SPECIAL_ATTACK_KEY].map(normalizeKey));
    },
    dispose: () => {
      target.removeEventListener('keydown', onKeyDown);
      target.removeEventListener('keyup', onKeyUp);
    },
  };
}
