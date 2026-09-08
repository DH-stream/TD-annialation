import type { PlayerInput } from './types';

export type HeroPosition = {
  x: number;
  y: number;
  z: number;
};

export const DEFAULT_HERO_SPEED = 6;
export const HERO_ARRIVAL_DISTANCE = 0.12;

export function advanceHeroPosition(
  position: HeroPosition,
  input: PlayerInput,
  destination: HeroPosition | null,
  deltaSeconds: number,
  speed = DEFAULT_HERO_SPEED,
): { position: HeroPosition; destination: HeroPosition | null } {
  const moveLength = Math.hypot(input.moveX, input.moveZ);
  if (moveLength > 0.001) {
    const scale = (speed * Math.max(0, deltaSeconds)) / moveLength;
    return {
      position: {
        x: position.x + input.moveX * scale,
        y: position.y,
        z: position.z + input.moveZ * scale,
      },
      destination: null,
    };
  }

  if (!destination || deltaSeconds <= 0) {
    return { position, destination };
  }

  const directionX = destination.x - position.x;
  const directionZ = destination.z - position.z;
  const distance = Math.hypot(directionX, directionZ);
  const travelDistance = speed * deltaSeconds;

  if (distance <= HERO_ARRIVAL_DISTANCE || distance <= travelDistance) {
    return {
      position: { x: destination.x, y: position.y, z: destination.z },
      destination: null,
    };
  }

  const scale = travelDistance / distance;
  return {
    position: {
      x: position.x + directionX * scale,
      y: position.y,
      z: position.z + directionZ * scale,
    },
    destination,
  };
}
