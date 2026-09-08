import { describe, expect, it } from 'vitest';
import {
  advanceStage,
  createStageState,
  GREENWARD_PATH,
  placeTower,
  startNextWave,
  collectCoins,
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

  it('drops a coin on a tower kill and rewards the hero on proximity pickup', () => {
    const wave = startNextWave(createStageState('endless'));
    const placed = placeTower(wave, { x: -11, z: 8.7 });
    const enemy: EnemyState = {
      id: 'target',
      x: GREENWARD_PATH[0].x,
      z: GREENWARD_PATH[0].z,
      health: 8,
      maxHealth: 8,
      speed: 0,
      waypointIndex: 0,
    };
    const combatState = { ...placed.state, enemies: [enemy], remainingToSpawn: 0 };
    const afterAttack = advanceStage(combatState, 0.5);

    expect(placed.tower).toBeDefined();
    expect(afterAttack.gold).toBe(combatState.gold);
    expect(afterAttack.enemies).toHaveLength(0);
    expect(afterAttack.coins).toHaveLength(1);
    const collected = collectCoins(afterAttack, { x: GREENWARD_PATH[0].x, z: GREENWARD_PATH[0].z });
    expect(collected.collected).toBe(10);
    expect(collected.state.gold).toBe(combatState.gold + 10);
    expect(collected.state.coins).toHaveLength(0);
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
      x: GREENWARD_PATH.at(-1)!.x,
      z: GREENWARD_PATH.at(-1)!.z,
      health: 10,
      maxHealth: 10,
      speed: 8,
      waypointIndex: GREENWARD_PATH.length - 1,
    };
    const afterGate = advanceStage({ ...wave, enemies: [enemy], remainingToSpawn: 0 }, 1);

    expect(afterGate.baseHealth).toBe(19);
  });
});
