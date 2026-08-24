using NUnit.Framework;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Economy;
using PuzzleGame.Core.Progression;

namespace PuzzleGame.Tests.EditMode.Battle
{
    public sealed class ProgressionBattleTests
    {
        [Test]
        public void Factory_materializes_level_and_duplicate_stats_with_effective_skill_charge()
        {
            var data = ProgressionBattleFixtures.Character();
            var skill = ProgressionBattleFixtures.Skill();
            var progress = new CharacterProgress(data, skill);
            ProgressionService.ApplyExperience(progress, 100);
            var wallet = new Wallet();
            ProgressionService.ApplyDuplicate(progress, wallet);
            ProgressionService.ApplyDuplicate(progress, wallet);
            ProgressionService.ApplyDuplicate(progress, wallet);

            var runtime = CharacterBattleFactory.Create(progress, skill);

            Assert.That(runtime.Data.BaseStats.Hp, Is.EqualTo(160));
            Assert.That(runtime.Data.BaseStats.Attack, Is.EqualTo(30));
            Assert.That(runtime.ActiveSkill.ChargeRequired, Is.EqualTo(5));
            Assert.That(runtime.EffectiveRarity, Is.EqualTo(5));
        }

        [Test]
        public void Factory_materializes_awakened_rarity_and_visuals_without_duplicates()
        {
            var data = ProgressionBattleFixtures.Character();
            var skill = ProgressionBattleFixtures.Skill();
            var progress = new CharacterProgress(data, skill);
            ProgressionService.ApplyExperience(progress, 100);
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.Gold, 10);
            var materials = new MaterialInventory();
            materials.Add("core", 1);
            Assert.That(ProgressionService.Awaken(progress, wallet, materials).Succeeded, Is.True);

            var runtime = CharacterBattleFactory.Create(progress, skill);

            Assert.That(runtime.EffectiveRarity, Is.EqualTo(6));
            Assert.That(runtime.Visuals.PortraitKey, Is.EqualTo("awakened"));
        }

        [Test]
        public void Runtime_rejects_active_and_leader_ids_that_do_not_match_the_character()
        {
            var data = ProgressionBattleFixtures.Character();
            var wrongSkill = ProgressionBattleFixtures.Skill();
            wrongSkill.Id = "wrong";
            Assert.That(() => new CharacterRuntime(data, wrongSkill), Throws.TypeOf<System.ArgumentException>());

            var leaderData = ProgressionBattleFixtures.Character();
            leaderData.LeaderSkillId = "leader";
            Assert.That(() => new CharacterRuntime(leaderData, ProgressionBattleFixtures.Skill()), Throws.TypeOf<System.ArgumentException>());
        }
    }

    internal static class ProgressionBattleFixtures
    {
        internal static CharacterData Character()
        {
            return new CharacterData
            {
                Id = "fighter", Element = ElementType.Fire, BaseRarity = 5,
                BaseStats = new StatBlock { Hp = 100, Attack = 20, Recovery = 5 }, ActiveSkillId = "active",
                LevelCurve = new ProgressionCurveData { MaxLevel = 2, ExperienceRequiredByLevel = new[] { 100 }, StatsByLevel = new[] { new StatBlock { Hp = 100, Attack = 20, Recovery = 5 }, new StatBlock { Hp = 150, Attack = 30, Recovery = 8 } } },
                Ascension = new AscensionConfigurationData { OverflowUniversalResourceAmount = 1, Ranks = new[] { new AscensionRankData { Rank = 1, ActiveSkillChargeReduction = 2 }, new AscensionRankData { Rank = 2, ActiveSkillChargeReduction = 3 }, new AscensionRankData { Rank = 3, StatBonus = new StatBlock { Hp = 10 } }, new AscensionRankData { Rank = 4, StatBonus = new StatBlock { Attack = 2 } }, new AscensionRankData { Rank = 5, StatBonus = new StatBlock { Recovery = 1 } } } },
                Awakening = new AwakeningRequirementData { RequiredLevel = 2, GoldCost = 10, Materials = new[] { new MaterialRequirementData { MaterialId = "core", Amount = 1 } } },
                BaseVisuals = new VisualReferenceSet { PortraitKey = "base" }, AwakenedVisuals = new VisualReferenceSet { PortraitKey = "awakened" }
            };
        }
        internal static SkillData Skill() { return new SkillData { Id = "active", ChargeElement = ElementType.Fire, ChargeRequired = 10, Effects = new[] { new SkillEffectData { Type = SkillEffectType.Heal, Payload = new EffectPayloadData { Amount = 1 } } } }; }
    }
}
