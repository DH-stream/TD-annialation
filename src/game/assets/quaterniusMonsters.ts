import type { AnimationGroup } from '@babylonjs/core/Animations/animationGroup';
import type { AssetContainer } from '@babylonjs/core/assetContainer';
import { LoadAssetContainerAsync } from '@babylonjs/core/Loading/sceneLoader';
import { TransformNode } from '@babylonjs/core/Meshes/transformNode';
import type { Scene } from '@babylonjs/core/scene';
import type { EnemyKind, EnemyPhase } from '../sim/stageSimulation';

export type MonsterInstance = {
  root: TransformNode;
  animations: Map<string, AnimationGroup>;
  play(phase: EnemyPhase, loop?: boolean): void;
  dispose(): void;
};

const assetPaths: Record<EnemyKind, string> = {
  skitter: '/assets/vendor/quaternius/ultimate-monsters/green-spiky-blob.gltf',
  raider: '/assets/vendor/quaternius/ultimate-monsters/orc.gltf',
  brute: '/assets/vendor/quaternius/ultimate-monsters/yeti.gltf',
};

const animationNames: Record<EnemyKind, Record<EnemyPhase, string>> = {
  skitter: {
    spawning: 'Jump',
    walking: 'Walk',
    hit: 'HitRecieve',
    attacking: 'Bite_Front',
    dying: 'Death',
  },
  raider: {
    spawning: 'Jump',
    walking: 'Run',
    hit: 'HitReact',
    attacking: 'Punch',
    dying: 'Death',
  },
  brute: {
    spawning: 'Jump',
    walking: 'Run',
    hit: 'HitReact',
    attacking: 'Punch',
    dying: 'Death',
  },
};

const containerCache = new Map<EnemyKind, Promise<AssetContainer>>();

export function monsterAnimationName(kind: EnemyKind, phase: EnemyPhase): string {
  return animationNames[kind][phase];
}

export async function loadQuaterniusMonster(scene: Scene, kind: EnemyKind): Promise<AssetContainer> {
  const cached = containerCache.get(kind);
  if (cached) return cached;

  const loading = (async () => {
    await import('@babylonjs/loaders/glTF');
    return LoadAssetContainerAsync(assetPaths[kind], scene);
  })();
  containerCache.set(kind, loading);
  return loading;
}

export function instantiateQuaterniusMonster(
  container: AssetContainer,
  kind: EnemyKind,
  id: string,
  scale = 1,
): MonsterInstance {
  const instance = container.instantiateModelsToScene((sourceName) => `${id}-${sourceName}`, false);
  const root = instance.rootNodes.find((node): node is TransformNode => node instanceof TransformNode)
    ?? new TransformNode(`${id}-root`, container.scene);
  root.scaling.setAll(scale);

  const animations = new Map<string, AnimationGroup>();
  instance.animationGroups.forEach((group) => animations.set(group.name, group));
  let activeAnimation: string | null = null;

  return {
    root,
    animations,
    play: (phase, loop = phase === 'walking') => {
      const name = monsterAnimationName(kind, phase);
      const animation = animations.get(name);
      if (!animation || activeAnimation === name) return;
      animations.forEach((candidate) => {
        if (candidate !== animation) candidate.stop();
      });
      animation.start(loop, 1);
      activeAnimation = name;
      if (!loop) {
        animation.onAnimationEndObservable.addOnce(() => {
          if (activeAnimation === name) activeAnimation = null;
        });
      }
    },
    dispose: () => {
      animations.forEach((animation) => animation.dispose());
      root.dispose(false, true);
    },
  };
}
