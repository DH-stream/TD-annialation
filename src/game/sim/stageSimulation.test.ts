import { describe, expect, it } from 'vitest';
import {
  advanceStage,
  createStageState,
  placeTower,
  startNextWave,
  type EnemyState,
} from './stageSimulation';

describe('Greenward stage simulation', () => {
  it('starts in build phase and spawns the first wave deterministically', () => {
    const initial = createStageState('stages');
    const wave = startNextWave(initial);
    const afterSpawn = advanceStage(wave, 1);

    expect(initial.status).toBe('build');
    expect(wave.wave).toBe(1);
    expect(wave.status).toBe('wave');
    expect(afterSpawn.enemies.length).toBeGreaterThan(0);
    expect(afterSpawn.enemies[0].id).toBe('greenward-wave-1-enemy-1');
  });

  it('lets a tower damage an enemy and rewards a kill', () => {
    const wave = startNextWave(createStageState('endless'));
    const placed = placeTower(wave, { x: -11, z: 8.7 });
    const enemy: EnemyState = {
      id: 'target',
      x: -11,
      z: 8.7,
      health: 8,
      maxHealth: 8,
      speed: 0,
      waypointIndex: 0,
    };
    const combatState = { ...placed.state, enemies: [enemy], remainingToSpawn: 0 };
    const afterAttack = advanceStage(combatState, 0.5);

    expect(placed.tower).toBeDefined();
    expect(afterAttack.gold).toBeGreaterThan(combatState.gold);
    expect(afterAttack.enemies).toHaveLength(0);
  });

  it('does not stack two towers on the same build pad', () => {
    const initial = createStageState('stages');
    const first = placeTower(initial, { x: 3.8, z: 5.2 });
    const second = placeTower(first.state, { x: 3.8, z: 5.2 });

    expect(first.tower).toBeDefined();
    expect(second.tower).toBeUndefined();
    expect(second.state.gold).toBe(150);
  });

  it('loses base health when an enemy reaches the gate', () => {
    const wave = startNextWave(createStageState('endless'));
    const enemy: EnemyState = {
      id: 'gate-runner',
      x: 10.8,
      z: -7.7,
      health: 10,
      maxHealth: 10,
      speed: 8,
      waypointIndex: 4,
    };
    const afterGate = advanceStage({ ...wave, enemies: [enemy], remainingToSpawn: 0 }, 1);

    expect(afterGate.baseHealth).toBe(19);
  });
});
