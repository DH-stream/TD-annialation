import { describe, expect, it } from 'vitest';
import {
  advanceStage,
  applyHeroAttack,
  collectCoins,
  createStageState,
  GREENWARD_PATH,
  placeTower,
  startNextWave,
  type EnemyState,
} from './stageSimulation';

const raider = (overrides: Partial<EnemyState> = {}): EnemyState => ({
  id: 'target',
  kind: 'raider',
  phase: 'walking',
  phaseTimer: 0,
  x: GREENWARD_PATH[0].x,
  z: GREENWARD_PATH[0].z,
  health: 8,
  maxHealth: 8,
  speed: 0,
  waypointIndex: 0,
  ...overrides,
});

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
    const placed = placeTower(wave, { x: -18, z: 10 });
    const combatState = { ...placed.state, enemies: [raider()], remainingToSpawn: 0 };
    const afterAttack = advanceStage(combatState, 0.5);

    expect(placed.tower).toBeDefined();
    expect(afterAttack.gold).toBe(combatState.gold);
    expect(afterAttack.enemies).toMatchObject([{ phase: 'dying' }]);
    expect(afterAttack.coins).toHaveLength(1);
    const afterDeath = advanceStage(afterAttack, 1);
    const collected = collectCoins(afterDeath, { x: GREENWARD_PATH[0].x, z: GREENWARD_PATH[0].z });
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

  it('allows freeform ground placement away from the route', () => {
    const placed = placeTower(createStageState('stages'), { x: 15, z: 8 });

    expect(placed.tower).toBeDefined();
    expect(placed.state.gold).toBe(150);
  });

  it('rejects tower placement on the route', () => {
    expect(placeTower(createStageState('stages'), { x: -16, z: 14 }).tower).toBeUndefined();
  });

  it('rejects tower placement inside another tower spacing radius', () => {
    const initial = placeTower(createStageState('stages'), { x: 15, z: 8 });

    expect(placeTower(initial.state, { x: 17, z: 8 }).tower).toBeUndefined();
  });

  it('rejects tower placement outside the playable ground bounds', () => {
    expect(placeTower(createStageState('stages'), { x: 31, z: 0 }).tower).toBeUndefined();
  });

  it('creates a coin when the hero basic attack defeats a nearby enemy', () => {
    const afterAttack = applyHeroAttack(
      { ...createStageState('endless'), status: 'wave', enemies: [raider({ id: 'hero-target', x: 0, z: 0 })] },
      { x: 0, z: 0 },
      'basic',
    );

    expect(afterAttack.enemies).toMatchObject([{ id: 'hero-target', phase: 'dying' }]);
    expect(afterAttack.coins).toEqual([{ id: 'coin-hero-target', x: 0, z: 0, value: 10 }]);
  });

  it('lets the hero special attack defeat every nearby enemy', () => {
    const enemies = [
      raider({ id: 'first-target', x: 1, z: 0, health: 12, maxHealth: 12 }),
      raider({ id: 'second-target', x: -1, z: 0, health: 12, maxHealth: 12 }),
    ];

    const afterAttack = applyHeroAttack(
      { ...createStageState('endless'), status: 'wave', enemies },
      { x: 0, z: 0 },
      'special',
    );

    expect(afterAttack.enemies).toMatchObject([{ phase: 'dying' }, { phase: 'dying' }]);
    expect(afterAttack.coins).toHaveLength(2);
  });

  it('loses base health when an enemy completes a gate attack', () => {
    const wave = startNextWave(createStageState('endless'));
    const afterGate = advanceStage({
      ...wave,
      enemies: [raider({
        id: 'gate-runner',
        x: GREENWARD_PATH.at(-1)!.x,
        z: GREENWARD_PATH.at(-1)!.z,
        speed: 8,
        waypointIndex: GREENWARD_PATH.length - 1,
      })],
      remainingToSpawn: 0,
    }, 1);

    expect(advanceStage(afterGate, 1).baseHealth).toBe(19);
  });

  it('spawns a deterministic mix of distinct enemy archetypes', () => {
    const afterSpawns = advanceStage(startNextWave(createStageState('endless')), 2.2);

    expect(afterSpawns.enemies.map((enemy) => enemy.kind)).toEqual(['raider', 'skitter', 'raider']);
    expect(afterSpawns.enemies.map((enemy) => enemy.phase)).toEqual(['spawning', 'spawning', 'spawning']);
  });

  it('keeps a killed enemy through its death state while dropping one coin', () => {
    const combat = {
      ...createStageState('endless'),
      status: 'wave' as const,
      enemies: [raider({ id: 'dying-target', x: 0, z: 0 })],
      remainingToSpawn: 0,
    };

    const afterKill = applyHeroAttack(combat, { x: 0, z: 0 }, 'basic');

    expect(afterKill.enemies).toMatchObject([{ id: 'dying-target', phase: 'dying' }]);
    expect(afterKill.coins).toEqual([{ id: 'coin-dying-target', x: 0, z: 0, value: 10 }]);
    expect(advanceStage(afterKill, 1).enemies).toHaveLength(0);
  });

  it('telegraphs a gate attack before damaging the base', () => {
    const wave = startNextWave(createStageState('endless'));
    const telegraph = advanceStage({
      ...wave,
      enemies: [raider({
        id: 'gate-telegraph',
        x: GREENWARD_PATH.at(-1)!.x,
        z: GREENWARD_PATH.at(-1)!.z,
        speed: 8,
        waypointIndex: GREENWARD_PATH.length - 1,
      })],
      remainingToSpawn: 0,
    }, 0.1);

    expect(telegraph.baseHealth).toBe(20);
    expect(telegraph.enemies).toMatchObject([{ id: 'gate-telegraph', phase: 'attacking' }]);
    expect(advanceStage(telegraph, 1).baseHealth).toBe(19);
  });
});
