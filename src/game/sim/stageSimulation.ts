export type PlayMode = 'stages' | 'endless';
export type StageStatus = 'build' | 'wave' | 'won' | 'lost';
export type EnemyKind = 'skitter' | 'raider' | 'brute';
export type EnemyPhase = 'spawning' | 'walking' | 'hit' | 'dying' | 'attacking';

export type PathPoint = { x: number; z: number };

export type EnemyState = {
  id: string;
  kind: EnemyKind;
  phase: EnemyPhase;
  phaseTimer: number;
  x: number;
  z: number;
  health: number;
  maxHealth: number;
  speed: number;
  waypointIndex: number;
};

export type TowerState = {
  id: string;
  x: number;
  z: number;
  range: number;
  damage: number;
  attackCooldown: number;
  attackTimer: number;
};

export type CoinState = {
  id: string;
  x: number;
  z: number;
  value: number;
};

export type StageState = {
  playMode: PlayMode;
  status: StageStatus;
  wave: number;
  maxWaves: number | null;
  baseHealth: number;
  gold: number;
  enemies: EnemyState[];
  towers: TowerState[];
  coins: CoinState[];
  remainingToSpawn: number;
  spawnTimer: number;
  nextTowerNumber: number;
};

export type TowerPlacement = { x: number; z: number };

export const GREENWARD_PATH: PathPoint[] = [
  { x: -21, z: 14 },
  { x: -13.2, z: 14 },
  { x: -9.9, z: 5.8 },
  { x: -2, z: 5.8 },
  { x: 0.3, z: -2.8 },
  { x: 8.9, z: -2.8 },
  { x: 10.5, z: -10 },
  { x: 0, z: -13.7 },
];

const ENEMY_SPAWN_INTERVAL = 0.7;
const ENEMY_SPAWN_DURATION = 0.35;
const ENEMY_HIT_DURATION = 0.18;
const ENEMY_DEATH_DURATION = 0.65;
const ENEMY_GATE_ATTACK_DURATION = 0.6;
const STARTING_GOLD = 200;
export const TOWER_COST = 50;
export const TOWER_MIN_SPACING = 3.2;
export const TOWER_MIN_PATH_CLEARANCE = 3.4;
export const TOWER_PLACEMENT_BOUNDS = { minX: -30, maxX: 30, minZ: -22, maxZ: 22 };

const ENEMY_ARCHETYPES: Record<EnemyKind, { health: number; speed: number; reward: number; gateDamage: number }> = {
  skitter: { health: 7, speed: 4.25, reward: 8, gateDamage: 1 },
  raider: { health: 10, speed: 2.95, reward: 10, gateDamage: 1 },
  brute: { health: 26, speed: 1.7, reward: 18, gateDamage: 2 },
};

const WAVE_PATTERNS: EnemyKind[][] = [
  ['raider', 'skitter', 'raider', 'skitter', 'raider'],
  ['skitter', 'raider', 'brute', 'skitter', 'raider', 'brute'],
  ['brute', 'skitter', 'raider', 'brute', 'raider', 'skitter', 'brute'],
];

export type TowerPlacementReason = 'ok' | 'phase' | 'insufficient-gold' | 'out-of-bounds' | 'path-clearance' | 'tower-spacing';

export type TowerPlacementValidation = {
  valid: boolean;
  reason: TowerPlacementReason;
};

function distanceToSegment(point: TowerPlacement, start: PathPoint, end: PathPoint): number {
  const deltaX = end.x - start.x;
  const deltaZ = end.z - start.z;
  const lengthSquared = deltaX * deltaX + deltaZ * deltaZ;
  if (lengthSquared === 0) return Math.hypot(point.x - start.x, point.z - start.z);
  const projection = Math.max(0, Math.min(1, ((point.x - start.x) * deltaX + (point.z - start.z) * deltaZ) / lengthSquared));
  return Math.hypot(point.x - (start.x + projection * deltaX), point.z - (start.z + projection * deltaZ));
}

function distanceToPath(point: TowerPlacement): number {
  return Math.min(...GREENWARD_PATH.slice(0, -1).map((start, index) => distanceToSegment(point, start, GREENWARD_PATH[index + 1])));
}

export function validateTowerPlacement(state: StageState, position: TowerPlacement): TowerPlacementValidation {
  if (state.status === 'won' || state.status === 'lost') return { valid: false, reason: 'phase' };
  if (state.gold < TOWER_COST) return { valid: false, reason: 'insufficient-gold' };
  if (
    position.x < TOWER_PLACEMENT_BOUNDS.minX
    || position.x > TOWER_PLACEMENT_BOUNDS.maxX
    || position.z < TOWER_PLACEMENT_BOUNDS.minZ
    || position.z > TOWER_PLACEMENT_BOUNDS.maxZ
  ) {
    return { valid: false, reason: 'out-of-bounds' };
  }
  if (distanceToPath(position) < TOWER_MIN_PATH_CLEARANCE) return { valid: false, reason: 'path-clearance' };
  if (state.towers.some((tower) => Math.hypot(tower.x - position.x, tower.z - position.z) < TOWER_MIN_SPACING)) {
    return { valid: false, reason: 'tower-spacing' };
  }
  return { valid: true, reason: 'ok' };
}

export function createStageState(playMode: PlayMode): StageState {
  return {
    playMode,
    status: 'build',
    wave: 0,
    maxWaves: playMode === 'stages' ? 3 : null,
    baseHealth: 20,
    gold: STARTING_GOLD,
    enemies: [],
    towers: [],
    coins: [],
    remainingToSpawn: 0,
    spawnTimer: 0,
    nextTowerNumber: 1,
  };
}

export function startNextWave(state: StageState): StageState {
  if (state.status === 'won' || state.status === 'lost' || (state.maxWaves !== null && state.wave >= state.maxWaves)) {
    return state;
  }

  const wave = state.wave + 1;
  return {
    ...state,
    status: 'wave',
    wave,
    remainingToSpawn: 4 + wave,
    spawnTimer: 0,
  };
}

export function placeTower(state: StageState, position: TowerPlacement): { state: StageState; tower?: TowerState } {
  if (!validateTowerPlacement(state, position).valid) {
    return { state };
  }

  const tower: TowerState = {
    id: `greenward-tower-${state.nextTowerNumber}`,
    x: position.x,
    z: position.z,
    range: 5.2,
    damage: 8,
    attackCooldown: 0.8,
    attackTimer: 0,
  };
  return {
    tower,
    state: {
      ...state,
      gold: state.gold - TOWER_COST,
      towers: [...state.towers, tower],
      nextTowerNumber: state.nextTowerNumber + 1,
    },
  };
}

function canBeDamaged(enemy: EnemyState): boolean {
  return enemy.phase !== 'spawning' && enemy.phase !== 'dying';
}

function damageEnemy(enemy: EnemyState, damage: number, coins: CoinState[]): EnemyState {
  const health = enemy.health - damage;
  if (health > 0) {
    return { ...enemy, health, phase: 'hit', phaseTimer: ENEMY_HIT_DURATION };
  }
  coins.push({
    id: `coin-${enemy.id}`,
    x: enemy.x,
    z: enemy.z,
    value: ENEMY_ARCHETYPES[enemy.kind].reward,
  });
  return { ...enemy, health: 0, phase: 'dying', phaseTimer: ENEMY_DEATH_DURATION };
}

function applyDamageToTargets(state: StageState, targetIds: Set<string>, damage: number): StageState {
  const coins = [...state.coins];
  const enemies = state.enemies.map((enemy) => (
    targetIds.has(enemy.id) && canBeDamaged(enemy)
      ? damageEnemy(enemy, damage, coins)
      : enemy
  ));
  return { ...state, enemies, coins };
}

export function applyHeroAttack(
  state: StageState,
  position: { x: number; z: number },
  attack: 'basic' | 'special',
): StageState {
  if (state.status !== 'wave') return state;

  const range = attack === 'special' ? 5.8 : 3.4;
  const damage = attack === 'special' ? 18 : 8;
  const targets = state.enemies
    .filter(canBeDamaged)
    .map((enemy) => ({ enemy, distance: Math.hypot(enemy.x - position.x, enemy.z - position.z) }))
    .filter(({ distance }) => distance <= range)
    .sort((left, right) => left.distance - right.distance)
    .slice(0, attack === 'special' ? undefined : 1)
    .map(({ enemy }) => enemy.id);
  return targets.length === 0 ? state : applyDamageToTargets(state, new Set(targets), damage);
}

function spawnEnemy(state: StageState): EnemyState {
  const enemyNumber = 5 + state.wave - state.remainingToSpawn;
  const pattern = WAVE_PATTERNS[Math.min(state.wave, WAVE_PATTERNS.length) - 1];
  const kind = pattern[(enemyNumber - 1) % pattern.length];
  const archetype = ENEMY_ARCHETYPES[kind];
  const health = archetype.health + (state.wave - 1) * 2;
  return {
    id: `greenward-wave-${state.wave}-enemy-${enemyNumber}`,
    kind,
    phase: 'spawning',
    phaseTimer: ENEMY_SPAWN_DURATION,
    x: GREENWARD_PATH[0].x,
    z: GREENWARD_PATH[0].z,
    health,
    maxHealth: health,
    speed: archetype.speed,
    waypointIndex: 0,
  };
}

function moveEnemy(enemy: EnemyState, deltaSeconds: number): EnemyState {
  let x = enemy.x;
  let z = enemy.z;
  let waypointIndex = enemy.waypointIndex;
  let remainingDistance = enemy.speed * deltaSeconds;

  while (remainingDistance > 0 && waypointIndex < GREENWARD_PATH.length - 1) {
    const target = GREENWARD_PATH[waypointIndex + 1];
    const directionX = target.x - x;
    const directionZ = target.z - z;
    const distance = Math.hypot(directionX, directionZ);
    if (distance <= remainingDistance) {
      x = target.x;
      z = target.z;
      waypointIndex += 1;
      remainingDistance -= distance;
      continue;
    }
    const scale = remainingDistance / distance;
    x += directionX * scale;
    z += directionZ * scale;
    remainingDistance = 0;
  }

  if (waypointIndex === GREENWARD_PATH.length - 1) {
    return { ...enemy, x, z, waypointIndex, phase: 'attacking', phaseTimer: ENEMY_GATE_ATTACK_DURATION };
  }
  return { ...enemy, x, z, waypointIndex };
}

function advanceEnemyPhase(enemy: EnemyState, deltaSeconds: number): { enemy?: EnemyState; baseDamage: number } {
  if (enemy.phase === 'walking') return { enemy, baseDamage: 0 };
  const phaseTimer = enemy.phaseTimer - deltaSeconds;
  if (phaseTimer > 0) return { enemy: { ...enemy, phaseTimer }, baseDamage: 0 };
  if (enemy.phase === 'dying') return { baseDamage: 0 };
  if (enemy.phase === 'attacking') return { baseDamage: ENEMY_ARCHETYPES[enemy.kind].gateDamage };
  return { enemy: { ...enemy, phase: 'walking', phaseTimer: 0 }, baseDamage: 0 };
}

function finishWave(state: StageState): StageState {
  if (state.enemies.length > 0 || state.remainingToSpawn > 0) {
    return state;
  }
  if (state.maxWaves !== null && state.wave >= state.maxWaves) {
    return { ...state, status: 'won' };
  }
  return { ...state, status: 'build' };
}

export function advanceStage(state: StageState, deltaSeconds: number): StageState {
  if (state.status !== 'wave' || deltaSeconds <= 0) {
    return state;
  }

  let baseHealth = state.baseHealth;
  const maturedEnemies = state.enemies.flatMap((enemy) => {
    const result = advanceEnemyPhase(enemy, deltaSeconds);
    baseHealth -= result.baseDamage;
    return result.enemy ? [result.enemy] : [];
  });

  let spawnTimer = state.spawnTimer + deltaSeconds;
  let remainingToSpawn = state.remainingToSpawn;
  const spawnedEnemies = [...maturedEnemies];
  while (spawnTimer >= ENEMY_SPAWN_INTERVAL && remainingToSpawn > 0) {
    spawnTimer -= ENEMY_SPAWN_INTERVAL;
    spawnedEnemies.push(spawnEnemy({ ...state, remainingToSpawn }));
    remainingToSpawn -= 1;
  }

  let enemies = spawnedEnemies.map((enemy) => (
    enemy.phase === 'walking' ? moveEnemy(enemy, deltaSeconds) : enemy
  ));
  const coins = [...state.coins];
  const towers = state.towers.map((tower) => {
    let attackTimer = Math.max(0, tower.attackTimer - deltaSeconds);
    if (attackTimer > 0) return { ...tower, attackTimer };

    const target = enemies
      .filter(canBeDamaged)
      .map((enemy) => ({ enemy, distance: Math.hypot(enemy.x - tower.x, enemy.z - tower.z) }))
      .filter(({ distance }) => distance <= tower.range)
      .sort((left, right) => left.distance - right.distance)[0]?.enemy;
    if (!target) return { ...tower, attackTimer: 0 };

    enemies = enemies.map((enemy) => (
      enemy.id === target.id ? damageEnemy(enemy, tower.damage, coins) : enemy
    ));
    attackTimer = tower.attackCooldown;
    return { ...tower, attackTimer };
  });

  const nextState: StageState = {
    ...state,
    baseHealth,
    enemies,
    towers,
    coins,
    remainingToSpawn,
    spawnTimer,
    status: baseHealth <= 0 ? 'lost' : 'wave',
  };
  return finishWave(nextState);
}

export function collectCoins(
  state: StageState,
  position: { x: number; z: number },
  radius = 1.6,
): { state: StageState; collected: number } {
  const collectedCoins = state.coins.filter((coin) => Math.hypot(coin.x - position.x, coin.z - position.z) <= radius);
  if (collectedCoins.length === 0) {
    return { state, collected: 0 };
  }
  const collectedIds = new Set(collectedCoins.map((coin) => coin.id));
  const collected = collectedCoins.reduce((sum, coin) => sum + coin.value, 0);
  return {
    collected,
    state: {
      ...state,
      gold: state.gold + collected,
      coins: state.coins.filter((coin) => !collectedIds.has(coin.id)),
    },
  };
}
