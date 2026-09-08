const SKILL_PROGRESS_STORAGE_KEY = 'td-annihilation.skill-progress.v1';

export type SkillProgress = {
  purchased: string[];
  remainingPoints: number;
};

const INITIAL_SKILL_PROGRESS: SkillProgress = { purchased: [], remainingPoints: 5 };

function isSkillProgress(value: unknown): value is SkillProgress {
  if (!value || typeof value !== 'object') return false;
  const candidate = value as Record<string, unknown>;
  return Array.isArray(candidate.purchased)
    && candidate.purchased.every((id) => typeof id === 'string')
    && typeof candidate.remainingPoints === 'number'
    && Number.isFinite(candidate.remainingPoints)
    && candidate.remainingPoints >= 0;
}

export function loadStoredSkillProgress(storage: Storage | undefined = window.localStorage): SkillProgress {
  try {
    const saved = storage.getItem(SKILL_PROGRESS_STORAGE_KEY);
    const parsed: unknown = saved ? JSON.parse(saved) : null;
    return isSkillProgress(parsed)
      ? { purchased: [...new Set(parsed.purchased)], remainingPoints: parsed.remainingPoints }
      : { ...INITIAL_SKILL_PROGRESS };
  } catch {
    return { ...INITIAL_SKILL_PROGRESS };
  }
}

export function saveSkillProgress(progress: SkillProgress, storage: Storage | undefined = window.localStorage): void {
  try {
    storage.setItem(SKILL_PROGRESS_STORAGE_KEY, JSON.stringify(progress));
  } catch {
    // Skill purchases remain usable when localStorage is unavailable.
  }
}
