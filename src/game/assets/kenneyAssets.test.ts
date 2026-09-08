import { existsSync, readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

function readGlbJson(path: string): { images?: Array<{ uri?: string }> } {
  const file = readFileSync(path);
  const jsonLength = file.readUInt32LE(12);
  return JSON.parse(file.subarray(20, 20 + jsonLength).toString('utf8').replace(/\0/g, ''));
}

describe('Kenney character assets', () => {
  it('ships every external GLB texture at its authored relative path', () => {
    const assetRoot = resolve(process.cwd(), 'public/assets/vendor/kenney/blocky-characters');

    for (const variant of ['a', 'b', 'd']) {
      const glbPath = resolve(assetRoot, `character-${variant}.glb`);
      const glb = readGlbJson(glbPath);
      for (const image of glb.images ?? []) {
        if (image.uri) {
          expect(existsSync(resolve(dirname(glbPath), image.uri)), image.uri).toBe(true);
        }
      }
    }
  });
});
