import type { AssetContainer } from '@babylonjs/core/assetContainer';
import { LoadAssetContainerAsync } from '@babylonjs/core/Loading/sceneLoader';
import { TransformNode } from '@babylonjs/core/Meshes/transformNode';
import type { Scene } from '@babylonjs/core/scene';

export type FantasyTownAsset =
  | 'banner-green'
  | 'chimney'
  | 'fence'
  | 'fountain-round'
  | 'lantern'
  | 'rock-large'
  | 'roof-high'
  | 'roof-high-gable'
  | 'tree'
  | 'tree-high'
  | 'wall'
  | 'wall-arch'
  | 'wall-arch-top'
  | 'wall-door'
  | 'wall-doorway-base'
  | 'wall-doorway-square'
  | 'wall-doorway-square-wide';

const containerCache = new Map<string, Promise<AssetContainer>>();

export async function loadKenneyFantasyTownAsset(scene: Scene, asset: FantasyTownAsset): Promise<AssetContainer> {
  const key = `kenney-fantasy-town-${asset}`;
  const cached = containerCache.get(key);
  if (cached) return cached;

  const loading = (async () => {
    await import('@babylonjs/loaders/glTF');
    return LoadAssetContainerAsync(
      `/assets/vendor/kenney/fantasy-town-kit/${asset}.glb`,
      scene,
    );
  })();
  containerCache.set(key, loading);
  return loading;
}

export function instantiateFantasyTownAsset(
  container: AssetContainer,
  id: string,
  position: { x: number; y: number; z: number },
  scale = 1,
  rotationY = 0,
): TransformNode {
  const instance = container.instantiateModelsToScene(
    (sourceName) => `${id}-${sourceName}`,
    false,
    { doNotInstantiate: false },
  );
  const root = new TransformNode(`${id}-root`, container.scene);
  instance.rootNodes.forEach((node) => {
    node.parent = root;
  });
  root.position.set(position.x, position.y, position.z);
  root.scaling.setAll(scale);
  root.rotation.y = rotationY;
  return root;
}
