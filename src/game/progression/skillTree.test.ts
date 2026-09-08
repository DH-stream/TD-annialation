import { describe, expect, it } from 'vitest';
import { canPurchaseSkill, GREENWARD_SKILL_TREE, purchaseSkill } from './skillTree';

describe('Greenward skill tree', () => {
  it('opens from the inner node outward through prerequisites', () => {
    const root = GREENWARD_SKILL_TREE.find((node) => node.id === 'royal-oath');
    const outer = GREENWARD_SKILL_TREE.find((node) => node.id === 'unyielding-banner');

    expect(root).toBeDefined();
    expect(outer).toBeDefined();
    expect(canPurchaseSkill(outer!, new Set(), 5)).toBe(false);
    expect(canPurchaseSkill(outer!, new Set(['royal-oath']), 5)).toBe(true);
  });

  it('spends points and returns the newly purchased node', () => {
    const result = purchaseSkill('royal-oath', new Set(), 2);

    expect(result).toEqual({ purchased: new Set(['royal-oath']), remainingPoints: 1 });
  });
});
