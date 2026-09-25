using System.Linq;
using NUnit.Framework;

namespace TDAnnihilation.Tests
{
    public sealed class TDMenuDefinitionsTests
    {
        [Test]
        public void DefaultStageCatalogHasOneSelectableGreenwardAndTwoLockedStages()
        {
            var stages = TDStageCatalog.CreateDefault();

            Assert.That(stages.Count, Is.EqualTo(3));
            Assert.That(stages[0].Id, Is.EqualTo("greenward"));
            Assert.That(stages[0].IsSelectable, Is.True);
            Assert.That(stages.Skip(1).All(stage => !stage.IsSelectable), Is.True);
        }

        [Test]
        public void SkillTreeRequiresPrerequisiteBeforeUnlock()
        {
            var nodes = TDSkillTreeCatalog.CreateDefault();
            var state = new TDSkillTreeState();

            Assert.That(state.TryUnlock("warden-edge", nodes), Is.False);
            Assert.That(state.TryUnlock("warden-grit", nodes), Is.True);
            Assert.That(state.TryUnlock("warden-edge", nodes), Is.True);
            Assert.That(state.IsUnlocked("warden-edge"), Is.True);
        }

        [Test]
        public void DefaultSkillTreeContainsWardenArcanaAndBastionBranches()
        {
            var branches = TDSkillTreeCatalog.CreateDefault()
                .Select(node => node.Branch)
                .Distinct()
                .ToArray();

            Assert.That(branches, Does.Contain(TDSkillBranch.Warden));
            Assert.That(branches, Does.Contain(TDSkillBranch.Arcana));
            Assert.That(branches, Does.Contain(TDSkillBranch.Bastion));
        }
    }
}
