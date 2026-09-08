export type SkillNode = {
  id: string;
  title: string;
  description: string;
  cost: number;
  depth: number;
  angleDegrees: number;
  prerequisites: string[];
};

export type SkillPurchaseResult = {
  purchased: Set<string>;
  remainingPoints: number;
};

export const GREENWARD_SKILL_TREE: SkillNode[] = [
  {
    id: 'royal-oath',
    title: 'Royal Oath',
    description: 'The hero enters the battlefield with resolve.',
    cost: 1,
    depth: 0,
    angleDegrees: 0,
    prerequisites: [],
  },
  {
    id: 'iron-skin',
    title: 'Iron Skin',
    description: 'Increase the hero\'s survivability.',
    cost: 1,
    depth: 1,
    angleDegrees: -140,
    prerequisites: ['royal-oath'],
  },
  {
    id: 'battle-command',
    title: 'Battle Command',
    description: 'Nearby defenses gain a stronger opening volley.',
    cost: 1,
    depth: 1,
    angleDegrees: -40,
    prerequisites: ['royal-oath'],
  },
  {
    id: 'arcane-aegis',
    title: 'Arcane Aegis',
    description: 'The shrine projects a protective magic pulse.',
    cost: 2,
    depth: 1,
    angleDegrees: 55,
    prerequisites: ['royal-oath'],
  },
  {
    id: 'unyielding-banner',
    title: 'Unyielding Banner',
    description: 'Unlock a future rally aura for co-op defenders.',
    cost: 2,
    depth: 2,
    angleDegrees: -88,
    prerequisites: ['royal-oath'],
  },
  {
    id: 'spell-forge',
    title: 'Spell Forge',
    description: 'Future towers can inherit the Greenward magic language.',
    cost: 2,
    depth: 2,
    angleDegrees: 8,
    prerequisites: ['arcane-aegis'],
  },
  {
    id: 'last-stand',
    title: 'Last Stand',
    description: 'A future clutch upgrade for the final wave.',
    cost: 3,
    depth: 2,
    angleDegrees: 108,
    prerequisites: ['battle-command'],
  },
];

function findSkillNode(id: string): SkillNode | undefined {
  return GREENWARD_SKILL_TREE.find((node) => node.id === id);
}

export function canPurchaseSkill(node: SkillNode, purchased: Set<string>, availablePoints: number): boolean {
  return availablePoints >= node.cost
    && !purchased.has(node.id)
    && node.prerequisites.every((prerequisite) => purchased.has(prerequisite));
}

export function purchaseSkill(id: string, purchased: Set<string>, availablePoints: number): SkillPurchaseResult {
  const node = findSkillNode(id);
  if (!node || !canPurchaseSkill(node, purchased, availablePoints)) {
    return { purchased: new Set(purchased), remainingPoints: availablePoints };
  }

  const nextPurchased = new Set(purchased);
  nextPurchased.add(node.id);
  return { purchased: nextPurchased, remainingPoints: availablePoints - node.cost };
}
