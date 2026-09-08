export type PlayMode = 'stages' | 'endless';
export type StageStatus = 'build' | 'wave' | 'won' | 'lost';

export type PathPoint = { x: number; z: number };

export type EnemyState = {
  id: string;
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

export type StageState = {
  playMode: PlayMode;
  status: StageStatus;
  wave: number;
  maxWaves: number | null;
  baseHealth: number;
  gold: number;
  enemies: EnemyState[];
  towers: TowerState[];
  remainingToSpawn: number;
  spawnTimer: number;
  nextTowerNumber: number;
};

export type TowerPlacement = { x: number; z: number };

export const GREENWARD_PATH: PathPoint[] = [
  { x: -11, z: 8.7 },
  { x: -5.2, z: 4.2 },
  { x: 0.5, z: 0.7 },
  { x: 6.1, z: -3.4 },
  { x: 10.8, z: -7.7 },
];

const ENEMY_SPAWN_INTERVAL = 0.7;
const STARTING_GOLD = 200;
const TOWER_COST = 50;
const ENEMY_REWARD = 10;

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
  const occupied = state.towers.some((tower) => Math.hypot(tower.x - position.x, tower.z - position.z) < 0.5);
  if (state.status === 'won' || state.status === 'lost' || state.gold < TOWER_COST || occupied) {
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

function spawnEnemy(state: StageState): EnemyState {
  const enemyNumber = 5 + state.wave - state.remainingToSpawn;
  const maxHealth = 8 + state.wave * 2;
  return {
    id: `greenward-wave-${state.wave}-enemy-${enemyNumber}`,
    x: GREENWARD_PATH[0].x,
    z: GREENWARD_PATH[0].z,
    health: maxHealth,
    maxHealth,
    speed: 2.8 + state.wave * 0.15,
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

  return { ...enemy, x, z, waypointIndex };
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

  let spawnTimer = state.spawnTimer + deltaSeconds;
  let remainingToSpawn = state.remainingToSpawn;
  const spawnedEnemies = [...state.enemies];
  while (spawnTimer >= ENEMY_SPAWN_INTERVAL && remainingToSpawn > 0) {
    spawnTimer -= ENEMY_SPAWN_INTERVAL;
    spawnedEnemies.push(spawnEnemy({ ...state, remainingToSpawn }));
    remainingToSpawn -= 1;
  }

  let baseHealth = state.baseHealth;
  const movedEnemies = spawnedEnemies
    .map((enemy) => moveEnemy(enemy, deltaSeconds))
    .filter((enemy) => {
      if (enemy.waypointIndex < GREENWARD_PATH.length - 1) {
        return true;
      }
      baseHealth -= 1;
      return false;
    });

  let gold = state.gold;
  let enemies = movedEnemies;
  const towers = state.towers.map((tower) => {
    let attackTimer = Math.max(0, tower.attackTimer - deltaSeconds);
    if (attackTimer > 0) {
      return { ...tower, attackTimer };
    }

    const target = enemies
      .map((enemy) => ({ enemy, distance: Math.hypot(enemy.x - tower.x, enemy.z - tower.z) }))
      .filter(({ distance }) => distance <= tower.range)
      .sort((left, right) => left.distance - right.distance)[0]?.enemy;
    if (!target) {
      return { ...tower, attackTimer: 0 };
    }

    enemies = enemies.flatMap((enemy) => {
      if (enemy.id !== target.id) {
        return [enemy];
      }
      const health = enemy.health - tower.damage;
      if (health <= 0) {
        gold += ENEMY_REWARD;
        return [];
      }
      return [{ ...enemy, health }];
    });
    attackTimer = tower.attackCooldown;
    return { ...tower, attackTimer };
  });

  const nextState: StageState = {
    ...state,
    baseHealth,
    gold,
    enemies,
    towers,
    remainingToSpawn,
    spawnTimer,
    status: baseHealth <= 0 ? 'lost' : 'wave',
  };
  return finishWave(nextState);
}
