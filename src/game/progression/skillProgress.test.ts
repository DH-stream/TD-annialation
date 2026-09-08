import { describe, expect, it } from 'vitest';
import { loadStoredSkillProgress, saveSkillProgress } from './skillProgress';

function createStorage(): Storage {
  const values = new Map<string, string>();
  return {
    getItem: (key) => values.get(key) ?? null,
    setItem: (key, value) => { values.set(key, value); },
    removeItem: (key) => { values.delete(key); },
    clear: () => values.clear(),
    key: () => null,
    get length() { return values.size; },
  };
}

describe('Greenward skill progress storage', () => {
  it('restores purchased skills and remaining points after a reload', () => {
    const storage = createStorage();

    saveSkillProgress({ purchased: ['royal-oath'], remainingPoints: 4 }, storage);

    expect(loadStoredSkillProgress(storage)).toEqual({
      purchased: ['royal-oath'],
      remainingPoints: 4,
    });
  });
});
