using System;
using System.Collections.Generic;
using UnityEngine;

namespace TDAnnihilation
{
    public enum TDMenuScreen
    {
        MainMenu,
        StageSelect,
        SkillTree,
        Settings,
        Gameplay,
        Result
    }

    public sealed class TDStageDefinition
    {
        public TDStageDefinition(string id, string displayName, string description, bool isSelectable)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            IsSelectable = isSelectable;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public bool IsSelectable { get; }
    }

    public static class TDStageCatalog
    {
        public static IReadOnlyList<TDStageDefinition> CreateDefault() => new[]
        {
            new TDStageDefinition("greenward", "GREENWARD", "Hold the valley against the first invasion.", true),
            new TDStageDefinition("stage-two", "THE ASHEN PASS", "A future battlefield.", false),
            new TDStageDefinition("stage-three", "THE SUNKEN KEEP", "A future battlefield.", false)
        };
    }

    public enum TDSkillBranch
    {
        Archer,
        Attack,
        Mage,
        Economy,
        Utility,
        Support,
        Crit,
        Cooldown,
        Range,
        Elemental,
        Defense,
        Warden = Defense,
        Arcana = Mage,
        Bastion = Archer
    }

    public enum TDSkillEffect
    {
        None,
        ArcherDamage,
        ArcherRange,
        HeroDamage,
        ArcaneDamage,
        ArcaneFireInterval,
        StartingGold,
        TowerCost,
        StartingLives,
        HeroCriticalChance,
        HeroCooldown,
        HeroRange,
        MegaRadius,
        HeavyDamage
    }

    public sealed class TDSkillNodeDefinition
    {
        public TDSkillNodeDefinition(string id, TDSkillBranch branch, string displayName, string description,
            string prerequisiteId, int cost = 1, TDSkillEffect effect = TDSkillEffect.None, float effectValue = 0f)
        {
            Id = id;
            Branch = branch;
            DisplayName = displayName;
            Description = description;
            PrerequisiteId = prerequisiteId;
            Cost = cost;
            Effect = effect;
            EffectValue = effectValue;
        }

        public string Id { get; }
        public TDSkillBranch Branch { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string PrerequisiteId { get; }
        public int Cost { get; }
        public TDSkillEffect Effect { get; }
        public float EffectValue { get; }
    }

    public static class TDSkillTreeCatalog
    {
        public static IReadOnlyList<TDSkillNodeDefinition> CreateDefault() => new[]
        {
            new TDSkillNodeDefinition("bastion-foundation", TDSkillBranch.Archer, "FOUNDATION", "Archer tower damage +10%.", null, 1, TDSkillEffect.ArcherDamage, 0.10f),
            new TDSkillNodeDefinition("bastion-focus", TDSkillBranch.Archer, "FOCUS", "Archer tower range +10%.", "bastion-foundation", 2, TDSkillEffect.ArcherRange, 0.10f),
            new TDSkillNodeDefinition("attack-force", TDSkillBranch.Attack, "ATTACK", "Hero attack damage +10%.", null, 1, TDSkillEffect.HeroDamage, 0.10f),
            new TDSkillNodeDefinition("arcana-spark", TDSkillBranch.Mage, "SPARK", "Arcane tower damage +10%.", null, 1, TDSkillEffect.ArcaneDamage, 0.10f),
            new TDSkillNodeDefinition("arcana-echo", TDSkillBranch.Mage, "ECHO", "Arcane tower firing interval -10%.", "arcana-spark", 2, TDSkillEffect.ArcaneFireInterval, 0.10f),
            new TDSkillNodeDefinition("economy-fortune", TDSkillBranch.Economy, "FORTUNE", "Start each run with 20 more gold.", null, 1, TDSkillEffect.StartingGold, 20f),
            new TDSkillNodeDefinition("utility-craft", TDSkillBranch.Utility, "UTILITY", "Tower build cost -5 gold.", null, 1, TDSkillEffect.TowerCost, 5f),
            new TDSkillNodeDefinition("support-rally", TDSkillBranch.Support, "SUPPORT", "Start each run with 2 more lives.", null, 1, TDSkillEffect.StartingLives, 2f),
            new TDSkillNodeDefinition("crit-opportunity", TDSkillBranch.Crit, "CRITICAL", "Hero hits have a 10% chance to deal double damage.", null, 1, TDSkillEffect.HeroCriticalChance, 0.10f),
            new TDSkillNodeDefinition("cooldown-tempo", TDSkillBranch.Cooldown, "TEMPO", "Hero attack cooldowns -10%.", null, 1, TDSkillEffect.HeroCooldown, 0.10f),
            new TDSkillNodeDefinition("range-sight", TDSkillBranch.Range, "SIGHT", "Hero light and heavy attack range +10%.", null, 1, TDSkillEffect.HeroRange, 0.10f),
            new TDSkillNodeDefinition("elemental-ember", TDSkillBranch.Elemental, "EMBER", "Mega attack radius +10%.", null, 1, TDSkillEffect.MegaRadius, 0.10f),
            new TDSkillNodeDefinition("warden-grit", TDSkillBranch.Defense, "GRIT", "Start each run with 2 more lives.", null, 1, TDSkillEffect.StartingLives, 2f),
            new TDSkillNodeDefinition("warden-edge", TDSkillBranch.Defense, "EDGE", "Heavy attack damage multiplier +0.2.", "warden-grit", 2, TDSkillEffect.HeavyDamage, 0.2f),

            new TDSkillNodeDefinition("archer-fletching", TDSkillBranch.Archer, "FLETCHING", "Archer tower damage +5%.", "bastion-foundation", 2, TDSkillEffect.ArcherDamage, 0.05f),
            new TDSkillNodeDefinition("archer-precision", TDSkillBranch.Archer, "PRECISION", "Archer tower damage +7%.", "bastion-focus", 3, TDSkillEffect.ArcherDamage, 0.07f),
            new TDSkillNodeDefinition("archer-longshot", TDSkillBranch.Archer, "LONGSHOT", "Archer tower range +7%.", "archer-fletching", 3, TDSkillEffect.ArcherRange, 0.07f),
            new TDSkillNodeDefinition("archer-volley", TDSkillBranch.Archer, "VOLLEY", "Archer tower damage +8%.", "archer-precision", 5, TDSkillEffect.ArcherDamage, 0.08f),

            new TDSkillNodeDefinition("attack-discipline", TDSkillBranch.Attack, "DISCIPLINE", "Hero attack damage +5%.", "attack-force", 2, TDSkillEffect.HeroDamage, 0.05f),
            new TDSkillNodeDefinition("attack-fury", TDSkillBranch.Attack, "FURY", "Heavy attack damage multiplier +0.1.", "attack-force", 2, TDSkillEffect.HeavyDamage, 0.1f),
            new TDSkillNodeDefinition("attack-rhythm", TDSkillBranch.Attack, "RHYTHM", "Hero attack cooldowns -4%.", "attack-discipline", 3, TDSkillEffect.HeroCooldown, 0.04f),
            new TDSkillNodeDefinition("attack-impact", TDSkillBranch.Attack, "IMPACT", "Hero attack damage +7%.", "attack-fury", 3, TDSkillEffect.HeroDamage, 0.07f),
            new TDSkillNodeDefinition("attack-mastery", TDSkillBranch.Attack, "MASTERY", "Hero attack damage +8%.", "attack-rhythm", 5, TDSkillEffect.HeroDamage, 0.08f),

            new TDSkillNodeDefinition("mage-focus", TDSkillBranch.Mage, "FOCUS", "Arcane tower damage +5%.", "arcana-spark", 2, TDSkillEffect.ArcaneDamage, 0.05f),
            new TDSkillNodeDefinition("mage-current", TDSkillBranch.Mage, "CURRENT", "Arcane tower firing interval -4%.", "arcana-echo", 3, TDSkillEffect.ArcaneFireInterval, 0.04f),
            new TDSkillNodeDefinition("mage-resonance", TDSkillBranch.Mage, "RESONANCE", "Arcane tower damage +7%.", "mage-focus", 3, TDSkillEffect.ArcaneDamage, 0.07f),
            new TDSkillNodeDefinition("mage-conduit", TDSkillBranch.Mage, "CONDUIT", "Arcane tower firing interval -5%.", "mage-current", 5, TDSkillEffect.ArcaneFireInterval, 0.05f),

            new TDSkillNodeDefinition("economy-cache", TDSkillBranch.Economy, "CACHE", "Start each run with 10 more gold.", "economy-fortune", 2, TDSkillEffect.StartingGold, 10f),
            new TDSkillNodeDefinition("economy-thrift", TDSkillBranch.Economy, "THRIFT", "Tower build cost -2 gold.", "economy-fortune", 2, TDSkillEffect.TowerCost, 2f),
            new TDSkillNodeDefinition("economy-reserve", TDSkillBranch.Economy, "RESERVE", "Start each run with 15 more gold.", "economy-cache", 3, TDSkillEffect.StartingGold, 15f),
            new TDSkillNodeDefinition("economy-ledger", TDSkillBranch.Economy, "LEDGER", "Tower build cost -2 gold.", "economy-thrift", 3, TDSkillEffect.TowerCost, 2f),
            new TDSkillNodeDefinition("economy-treasury", TDSkillBranch.Economy, "TREASURY", "Start each run with 20 more gold.", "economy-reserve", 5, TDSkillEffect.StartingGold, 20f),

            new TDSkillNodeDefinition("utility-tools", TDSkillBranch.Utility, "TOOLS", "Tower build cost -2 gold.", "utility-craft", 2, TDSkillEffect.TowerCost, 2f),
            new TDSkillNodeDefinition("utility-planning", TDSkillBranch.Utility, "PLANNING", "Start each run with 10 more gold.", "utility-craft", 2, TDSkillEffect.StartingGold, 10f),
            new TDSkillNodeDefinition("utility-forge", TDSkillBranch.Utility, "FORGE", "Tower build cost -2 gold.", "utility-tools", 3, TDSkillEffect.TowerCost, 2f),
            new TDSkillNodeDefinition("utility-supply", TDSkillBranch.Utility, "SUPPLY", "Start each run with 15 more gold.", "utility-planning", 3, TDSkillEffect.StartingGold, 15f),
            new TDSkillNodeDefinition("utility-engineer", TDSkillBranch.Utility, "ENGINEER", "Tower build cost -3 gold.", "utility-forge", 5, TDSkillEffect.TowerCost, 3f),

            new TDSkillNodeDefinition("support-guard", TDSkillBranch.Support, "GUARD", "Start each run with 1 more life.", "support-rally", 2, TDSkillEffect.StartingLives, 1f),
            new TDSkillNodeDefinition("support-resolve", TDSkillBranch.Support, "RESOLVE", "Hero attack damage +4%.", "support-rally", 2, TDSkillEffect.HeroDamage, 0.04f),
            new TDSkillNodeDefinition("support-banner", TDSkillBranch.Support, "BANNER", "Start each run with 1 more life.", "support-guard", 3, TDSkillEffect.StartingLives, 1f),
            new TDSkillNodeDefinition("support-courage", TDSkillBranch.Support, "COURAGE", "Hero attack damage +5%.", "support-resolve", 3, TDSkillEffect.HeroDamage, 0.05f),
            new TDSkillNodeDefinition("support-sanctuary", TDSkillBranch.Support, "SANCTUARY", "Start each run with 2 more lives.", "support-banner", 5, TDSkillEffect.StartingLives, 2f),

            new TDSkillNodeDefinition("crit-edge", TDSkillBranch.Crit, "KEEN EDGE", "Hero critical chance +3%.", "crit-opportunity", 2, TDSkillEffect.HeroCriticalChance, 0.03f),
            new TDSkillNodeDefinition("crit-force", TDSkillBranch.Crit, "FORCE", "Hero attack damage +5%.", "crit-opportunity", 2, TDSkillEffect.HeroDamage, 0.05f),
            new TDSkillNodeDefinition("crit-instinct", TDSkillBranch.Crit, "INSTINCT", "Hero critical chance +4%.", "crit-edge", 3, TDSkillEffect.HeroCriticalChance, 0.04f),
            new TDSkillNodeDefinition("crit-strike", TDSkillBranch.Crit, "STRIKE", "Hero attack damage +5%.", "crit-force", 3, TDSkillEffect.HeroDamage, 0.05f),
            new TDSkillNodeDefinition("crit-perfect", TDSkillBranch.Crit, "PERFECT HIT", "Hero critical chance +5%.", "crit-instinct", 5, TDSkillEffect.HeroCriticalChance, 0.05f),

            new TDSkillNodeDefinition("cooldown-flow", TDSkillBranch.Cooldown, "FLOW", "Hero attack cooldowns -3%.", "cooldown-tempo", 2, TDSkillEffect.HeroCooldown, 0.03f),
            new TDSkillNodeDefinition("cooldown-power", TDSkillBranch.Cooldown, "POWER", "Heavy attack damage multiplier +0.1.", "cooldown-tempo", 2, TDSkillEffect.HeavyDamage, 0.1f),
            new TDSkillNodeDefinition("cooldown-haste", TDSkillBranch.Cooldown, "HASTE", "Hero attack cooldowns -4%.", "cooldown-flow", 3, TDSkillEffect.HeroCooldown, 0.04f),
            new TDSkillNodeDefinition("cooldown-recovery", TDSkillBranch.Cooldown, "RECOVERY", "Hero attack cooldowns -3%.", "cooldown-power", 3, TDSkillEffect.HeroCooldown, 0.03f),
            new TDSkillNodeDefinition("cooldown-surge", TDSkillBranch.Cooldown, "SURGE", "Hero attack cooldowns -5%.", "cooldown-haste", 5, TDSkillEffect.HeroCooldown, 0.05f),

            new TDSkillNodeDefinition("range-reach", TDSkillBranch.Range, "REACH", "Hero attack range +5%.", "range-sight", 2, TDSkillEffect.HeroRange, 0.05f),
            new TDSkillNodeDefinition("range-aim", TDSkillBranch.Range, "AIM", "Hero critical chance +3%.", "range-sight", 2, TDSkillEffect.HeroCriticalChance, 0.03f),
            new TDSkillNodeDefinition("range-horizon", TDSkillBranch.Range, "HORIZON", "Hero attack range +5%.", "range-reach", 3, TDSkillEffect.HeroRange, 0.05f),
            new TDSkillNodeDefinition("range-target", TDSkillBranch.Range, "TARGET", "Hero critical chance +3%.", "range-aim", 3, TDSkillEffect.HeroCriticalChance, 0.03f),
            new TDSkillNodeDefinition("range-eagle", TDSkillBranch.Range, "EAGLE EYE", "Hero attack range +7%.", "range-horizon", 5, TDSkillEffect.HeroRange, 0.07f),

            new TDSkillNodeDefinition("elemental-radius", TDSkillBranch.Elemental, "RADIUS", "Mega attack radius +5%.", "elemental-ember", 2, TDSkillEffect.MegaRadius, 0.05f),
            new TDSkillNodeDefinition("elemental-flame", TDSkillBranch.Elemental, "FLAME", "Hero attack damage +4%.", "elemental-ember", 2, TDSkillEffect.HeroDamage, 0.04f),
            new TDSkillNodeDefinition("elemental-wave", TDSkillBranch.Elemental, "WAVE", "Mega attack radius +7%.", "elemental-radius", 3, TDSkillEffect.MegaRadius, 0.07f),
            new TDSkillNodeDefinition("elemental-spark", TDSkillBranch.Elemental, "SPARK", "Hero attack damage +5%.", "elemental-flame", 3, TDSkillEffect.HeroDamage, 0.05f),
            new TDSkillNodeDefinition("elemental-inferno", TDSkillBranch.Elemental, "INFERNO", "Mega attack radius +8%.", "elemental-wave", 5, TDSkillEffect.MegaRadius, 0.08f),

            new TDSkillNodeDefinition("defense-watch", TDSkillBranch.Defense, "WATCH", "Start each run with 1 more life.", "warden-grit", 2, TDSkillEffect.StartingLives, 1f),
            new TDSkillNodeDefinition("defense-bulwark", TDSkillBranch.Defense, "BULWARK", "Start each run with 2 more lives.", "warden-edge", 3, TDSkillEffect.StartingLives, 2f),
            new TDSkillNodeDefinition("defense-rampart", TDSkillBranch.Defense, "RAMPART", "Start each run with 1 more life.", "defense-watch", 3, TDSkillEffect.StartingLives, 1f),
            new TDSkillNodeDefinition("defense-citadel", TDSkillBranch.Defense, "CITADEL", "Start each run with 2 more lives.", "defense-bulwark", 5, TDSkillEffect.StartingLives, 2f)
        };
    }

    public sealed class TDSkillTreeState
    {
        private const string SaveKey = "TDAnnihilation.SkillTreeProgression.v1";
        private const int CurrentSaveVersion = 1;
        private readonly HashSet<string> unlocked = new HashSet<string>();
        private readonly HashSet<string> clearedMaps = new HashSet<string>();
        private readonly HashSet<string> endlessMaps = new HashSet<string>();

        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public int skullBalance;
            public string[] unlockedNodeIds;
            public string[] clearedMapIds;
            public string[] endlessMapIds;
        }

        public int SkullBalance { get; private set; }

        public bool IsUnlocked(string id) => !string.IsNullOrEmpty(id) && unlocked.Contains(id);
        public bool HasClearedMap(string mapId) => !string.IsNullOrEmpty(mapId) && clearedMaps.Contains(mapId);
        public bool IsEndlessUnlocked(string mapId) => !string.IsNullOrEmpty(mapId) && endlessMaps.Contains(mapId);

        public int AwardMapClear(string mapId, int firstClearReward, int repeatClearReward)
        {
            if (string.IsNullOrWhiteSpace(mapId)) return 0;
            bool firstClear = clearedMaps.Add(mapId);
            endlessMaps.Add(mapId);
            int reward = Mathf.Max(0, firstClear ? firstClearReward : repeatClearReward);
            SkullBalance = AddClamped(SkullBalance, reward);
            Save();
            return reward;
        }

        public int AwardEndlessWave(int endlessWaveNumber, int baseReward, int increment)
        {
            if (endlessWaveNumber < 1) return 0;
            long scaledReward = Mathf.Max(0, baseReward) + (long)(endlessWaveNumber - 1) * Mathf.Max(0, increment);
            int reward = (int)Math.Min(scaledReward, int.MaxValue - (long)SkullBalance);
            SkullBalance += reward;
            Save();
            return reward;
        }

        public bool CanUnlock(string id, IReadOnlyList<TDSkillNodeDefinition> nodes)
        {
            if (string.IsNullOrEmpty(id) || nodes == null || IsUnlocked(id)) return false;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Id != id) continue;
                return string.IsNullOrEmpty(nodes[i].PrerequisiteId) || IsUnlocked(nodes[i].PrerequisiteId);
            }
            return false;
        }

        public bool TryUnlock(string id, IReadOnlyList<TDSkillNodeDefinition> nodes)
        {
            if (!CanUnlock(id, nodes)) return false;
            unlocked.Add(id);
            return true;
        }

        public bool TryPurchase(string id, IReadOnlyList<TDSkillNodeDefinition> nodes)
        {
            if (!CanUnlock(id, nodes)) return false;
            TDSkillNodeDefinition node = FindNode(id, nodes);
            if (node == null || node.Cost < 0 || SkullBalance < node.Cost) return false;
            unlocked.Add(id);
            SkullBalance -= node.Cost;
            Save();
            return true;
        }

        public TDSkillModifiers GetModifiers(IReadOnlyList<TDSkillNodeDefinition> nodes)
        {
            var modifiers = new TDSkillModifiers();
            if (nodes == null) return modifiers;
            for (int i = 0; i < nodes.Count; i++)
            {
                TDSkillNodeDefinition node = nodes[i];
                if (node == null || !IsUnlocked(node.Id)) continue;
                modifiers.Add(node.Effect, node.EffectValue);
            }
            return modifiers;
        }

        public void Save()
        {
            var data = new SaveData
            {
                version = CurrentSaveVersion,
                skullBalance = Mathf.Max(0, SkullBalance),
                unlockedNodeIds = ToArray(unlocked),
                clearedMapIds = ToArray(clearedMaps),
                endlessMapIds = ToArray(endlessMaps)
            };
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static TDSkillTreeState Load()
        {
            var state = new TDSkillTreeState();
            if (!PlayerPrefs.HasKey(SaveKey)) return state;
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                if (data == null || data.version != CurrentSaveVersion) return state;
                state.SkullBalance = Mathf.Max(0, data.skullBalance);
                var knownNodes = new HashSet<string>();
                IReadOnlyList<TDSkillNodeDefinition> nodes = TDSkillTreeCatalog.CreateDefault();
                for (int i = 0; i < nodes.Count; i++) knownNodes.Add(nodes[i].Id);
                AddValidIds(state.unlocked, data.unlockedNodeIds, knownNodes);
                AddValidIds(state.clearedMaps, data.clearedMapIds, null);
                AddValidIds(state.endlessMaps, data.endlessMapIds, null);
            }
            catch (Exception)
            {
                return new TDSkillTreeState();
            }
            return state;
        }

        private static TDSkillNodeDefinition FindNode(string id, IReadOnlyList<TDSkillNodeDefinition> nodes)
        {
            if (nodes == null) return null;
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i] != null && nodes[i].Id == id) return nodes[i];
            return null;
        }

        private static int AddClamped(int value, int amount) => amount > int.MaxValue - value ? int.MaxValue : value + amount;
        private static string[] ToArray(HashSet<string> values)
        {
            var result = new string[values.Count];
            values.CopyTo(result);
            return result;
        }

        private static void AddValidIds(HashSet<string> target, string[] values, HashSet<string> allowed)
        {
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
                if (!string.IsNullOrWhiteSpace(values[i]) && (allowed == null || allowed.Contains(values[i])))
                    target.Add(values[i]);
        }
    }

    public sealed class TDSkillModifiers
    {
        public float ArcherDamageBonus { get; private set; }
        public float ArcherRangeBonus { get; private set; }
        public float HeroDamageBonus { get; private set; }
        public float ArcaneDamageBonus { get; private set; }
        public float ArcaneFireIntervalReduction { get; private set; }
        public int StartingGoldBonus { get; private set; }
        public int TowerCostReduction { get; private set; }
        public int StartingLivesBonus { get; private set; }
        public float HeroCriticalChance { get; private set; }
        public float HeroCooldownReduction { get; private set; }
        public float HeroRangeBonus { get; private set; }
        public float MegaRadiusBonus { get; private set; }
        public float HeavyDamageBonus { get; private set; }

        internal void Add(TDSkillEffect effect, float value)
        {
            switch (effect)
            {
                case TDSkillEffect.ArcherDamage: ArcherDamageBonus += value; break;
                case TDSkillEffect.ArcherRange: ArcherRangeBonus += value; break;
                case TDSkillEffect.HeroDamage: HeroDamageBonus += value; break;
                case TDSkillEffect.ArcaneDamage: ArcaneDamageBonus += value; break;
                case TDSkillEffect.ArcaneFireInterval: ArcaneFireIntervalReduction += value; break;
                case TDSkillEffect.StartingGold: StartingGoldBonus += Mathf.RoundToInt(value); break;
                case TDSkillEffect.TowerCost: TowerCostReduction += Mathf.RoundToInt(value); break;
                case TDSkillEffect.StartingLives: StartingLivesBonus += Mathf.RoundToInt(value); break;
                case TDSkillEffect.HeroCriticalChance: HeroCriticalChance += value; break;
                case TDSkillEffect.HeroCooldown: HeroCooldownReduction += value; break;
                case TDSkillEffect.HeroRange: HeroRangeBonus += value; break;
                case TDSkillEffect.MegaRadius: MegaRadiusBonus += value; break;
                case TDSkillEffect.HeavyDamage: HeavyDamageBonus += value; break;
            }
        }
    }
}
