import type { AnimationGroup } from '@babylonjs/core/Animations/animationGroup';
import type { AssetContainer } from '@babylonjs/core/assetContainer';
import { LoadAssetContainerAsync } from '@babylonjs/core/Loading/sceneLoader';
import { TransformNode } from '@babylonjs/core/Meshes/transformNode';
import type { Scene } from '@babylonjs/core/scene';

export type KenneyCharacterVariant = 'a' | 'b' | 'd';

export type CharacterInstance = {
  root: TransformNode;
  animations: Map<string, AnimationGroup>;
  play(name: string, loop?: boolean): void;
  dispose(): void;
};

const containerCache = new Map<string, Promise<AssetContainer>>();

export async function loadKenneyCharacter(
  scene: Scene,
  variant: KenneyCharacterVariant,
): Promise<AssetContainer> {
  const key = `kenney-blocky-character-${variant}`;
  const cached = containerCache.get(key);
  if (cached) return cached;

  const loading = (async () => {
    await import('@babylonjs/loaders/glTF');
    return LoadAssetContainerAsync(
      `/assets/vendor/kenney/blocky-characters/character-${variant}.glb`,
      scene,
    );
  })();
  containerCache.set(key, loading);
  return loading;
}

export function instantiateKenneyCharacter(
  container: AssetContainer,
  id: string,
  scale = 1,
): CharacterInstance {
  const instance = container.instantiateModelsToScene(
    (sourceName) => `${id}-${sourceName}`,
    false,
  );
  const root = instance.rootNodes.find((node): node is TransformNode => node instanceof TransformNode)
    ?? new TransformNode(`${id}-root`, container.scene);
  root.scaling.setAll(scale);

  const animations = new Map<string, AnimationGroup>();
  instance.animationGroups.forEach((group) => animations.set(group.name, group));
  let activeAnimation: string | null = null;

  return {
    root,
    animations,
    play: (name, loop = true) => {
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
