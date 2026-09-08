import { describe, expect, it } from 'vitest';
import { advanceHeroPosition, type HeroPosition } from './heroMovement';
import type { PlayerInput } from './types';

const position: HeroPosition = { x: 0, y: 0, z: 0 };
const idleInput: PlayerInput = {
  playerId: 'player-1',
  moveX: 0,
  moveZ: 0,
  basicAttack: false,
  specialAttack: false,
  issuedAt: 0,
};

describe('hero movement', () => {
  it('lets keyboard movement override a click destination', () => {
    const result = advanceHeroPosition(position, { ...idleInput, moveX: 1 }, { x: -10, y: 0, z: 0 }, 0.5);

    expect(result.destination).toBeNull();
    expect(result.position.x).toBeCloseTo(3);
    expect(result.position.z).toBeCloseTo(0);
  });

  it('moves toward a clicked destination and stops on arrival', () => {
    const result = advanceHeroPosition(position, idleInput, { x: 2, y: 0, z: 0 }, 1);

    expect(result.position).toEqual({ x: 2, y: 0, z: 0 });
    expect(result.destination).toBeNull();
  });

  it('keeps direct movement inside the enlarged local map bounds', () => {
    const result = advanceHeroPosition(position, { ...idleInput, moveX: 1 }, null, 10);

    expect(result.position.x).toBe(28);
  });
});
