using System.Collections.Generic;

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
        Warden,
        Arcana,
        Bastion
    }

    public sealed class TDSkillNodeDefinition
    {
        public TDSkillNodeDefinition(string id, TDSkillBranch branch, string displayName, string description, string prerequisiteId)
        {
            Id = id;
            Branch = branch;
            DisplayName = displayName;
            Description = description;
            PrerequisiteId = prerequisiteId;
        }

        public string Id { get; }
        public TDSkillBranch Branch { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string PrerequisiteId { get; }
    }

    public static class TDSkillTreeCatalog
    {
        public static IReadOnlyList<TDSkillNodeDefinition> CreateDefault() => new[]
        {
            new TDSkillNodeDefinition("warden-grit", TDSkillBranch.Warden, "GRIT", "A future hero-survival node.", null),
            new TDSkillNodeDefinition("warden-edge", TDSkillBranch.Warden, "EDGE", "A future melee node.", "warden-grit"),
            new TDSkillNodeDefinition("arcana-spark", TDSkillBranch.Arcana, "SPARK", "A future energy-bolt node.", null),
            new TDSkillNodeDefinition("arcana-echo", TDSkillBranch.Arcana, "ECHO", "A future spell node.", "arcana-spark"),
            new TDSkillNodeDefinition("bastion-foundation", TDSkillBranch.Bastion, "FOUNDATION", "A future tower node.", null),
            new TDSkillNodeDefinition("bastion-focus", TDSkillBranch.Bastion, "FOCUS", "A future defense node.", "bastion-foundation")
        };
    }

    public sealed class TDSkillTreeState
    {
        private readonly HashSet<string> unlocked = new HashSet<string>();

        public bool IsUnlocked(string id) => !string.IsNullOrEmpty(id) && unlocked.Contains(id);

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
    }
}
