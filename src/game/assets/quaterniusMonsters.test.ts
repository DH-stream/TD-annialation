import { describe, expect, it } from 'vitest';
import { monsterAnimationName } from './quaterniusMonsters';

describe('Quaternius monster animation mapping', () => {
  it('maps each gameplay phase to an animation that exists on its model', () => {
    expect(monsterAnimationName('skitter', 'spawning')).toBe('Jump');
    expect(monsterAnimationName('skitter', 'walking')).toBe('Walk');
    expect(monsterAnimationName('skitter', 'hit')).toBe('HitRecieve');
    expect(monsterAnimationName('skitter', 'attacking')).toBe('Bite_Front');
    expect(monsterAnimationName('skitter', 'dying')).toBe('Death');

    expect(monsterAnimationName('raider', 'spawning')).toBe('Jump');
    expect(monsterAnimationName('raider', 'walking')).toBe('Run');
    expect(monsterAnimationName('raider', 'hit')).toBe('HitReact');
    expect(monsterAnimationName('raider', 'attacking')).toBe('Punch');
    expect(monsterAnimationName('raider', 'dying')).toBe('Death');

    expect(monsterAnimationName('brute', 'spawning')).toBe('Jump');
    expect(monsterAnimationName('brute', 'walking')).toBe('Run');
    expect(monsterAnimationName('brute', 'hit')).toBe('HitReact');
    expect(monsterAnimationName('brute', 'attacking')).toBe('Punch');
    expect(monsterAnimationName('brute', 'dying')).toBe('Death');
  });
});
