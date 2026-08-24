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
            Assert.That(runtime.Visuals.CardArtKey, Is.EqualTo("awakened-card"));
            Assert.That(runtime.Visuals.ModelKey, Is.EqualTo("awakened-model"));
            Assert.That(runtime.Visuals.VfxKey, Is.EqualTo("awakened-vfx"));
        }

        [Test]
        public void Factory_materializes_effective_charge_and_complete_multi_effect_skill_behavior()
        {
            var data = ProgressionBattleFixtures.Character();
            var skill = ProgressionBattleFixtures.Skill();
            skill.Effects = new[]
            {
                new SkillEffectData { Type = SkillEffectType.Heal, Payload = new EffectPayloadData { Amount = 1 } },
                new SkillEffectData { Type = SkillEffectType.AttackBoost, Payload = new EffectPayloadData { Multiplier = 1f, TurnCount = 1 } }
            };
            var progress = new CharacterProgress(data, skill);
            ProgressionService.ApplyDuplicate(progress, new Wallet());
            var runtime = CharacterBattleFactory.Create(progress, skill);
            runtime.AddCharge(7);

            Assert.That(new SkillEngine().CanActivate(runtime), Is.False);
            runtime.AddCharge(1);
            var members = BattleFixtures.StandardMembers();
            members[0] = runtime;
            var resolution = new SkillEngine().Activate(runtime,
                new BattleContext(BattleFixtures.StableBoard(), new PartyState(members), BattleFixtures.Enemy(ElementType.Fire)));

            Assert.That(runtime.ActiveSkill.ChargeRequired, Is.EqualTo(8));
            Assert.That(resolution.Succeeded, Is.True);
            Assert.That(resolution.Events, Has.Count.EqualTo(2));
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

        [Test]
        public void Runtime_requires_an_identified_passive_that_exactly_matches_the_character()
        {
            var data = ProgressionBattleFixtures.Character();
            data.PassiveId = "passive";
            var passive = ProgressionBattleFixtures.Passive();

            Assert.That(() => new CharacterRuntime(data, ProgressionBattleFixtures.Skill()), Throws.TypeOf<System.ArgumentException>());
            passive.Id = "wrong";
            Assert.That(() => new CharacterRuntime(data, ProgressionBattleFixtures.Skill(), null, passive), Throws.TypeOf<System.ArgumentException>());

            passive.Id = "passive";
            var runtime = new CharacterRuntime(data, ProgressionBattleFixtures.Skill(), null, passive);
            Assert.That(runtime.Passive.Id, Is.EqualTo("passive"));
            Assert.That(runtime.Passive.Effects, Has.Length.EqualTo(1));
        }

        [Test]
        public void Factory_does_not_mutate_progress_or_authored_data_when_a_bound_passive_is_invalid()
        {
            var data = ProgressionBattleFixtures.Character();
            data.PassiveId = "passive";
            var progress = new CharacterProgress(data, ProgressionBattleFixtures.Skill());
            ProgressionService.ApplyExperience(progress, 100);
            var beforeStats = progress.CurrentStats;
            var beforeData = progress.AuthoredData;

            var wrongPassive = ProgressionBattleFixtures.Passive();
            wrongPassive.Id = "wrong";

            Assert.That(() => CharacterBattleFactory.Create(progress, ProgressionBattleFixtures.Skill(), null, wrongPassive), Throws.TypeOf<System.ArgumentException>());
            Assert.That(progress.CurrentStats.Hp, Is.EqualTo(beforeStats.Hp));
            Assert.That(progress.AuthoredData.BaseStats.Hp, Is.EqualTo(beforeData.BaseStats.Hp));
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
                BaseVisuals = new VisualReferenceSet { PortraitKey = "base", CardArtKey = "base-card", ModelKey = "base-model", VfxKey = "base-vfx" }, AwakenedVisuals = new VisualReferenceSet { PortraitKey = "awakened", CardArtKey = "awakened-card", ModelKey = "awakened-model", VfxKey = "awakened-vfx" }
            };
        }
        internal static SkillData Skill() { return new SkillData { Id = "active", ChargeElement = ElementType.Fire, ChargeRequired = 10, Effects = new[] { new SkillEffectData { Type = SkillEffectType.Heal, Payload = new EffectPayloadData { Amount = 1 } } } }; }
        internal static PassiveData Passive() { return new PassiveData { Id = "passive", Effects = new[] { new SkillEffectData { Type = SkillEffectType.AttackBoost, Payload = new EffectPayloadData { Multiplier = 1f, TurnCount = 1 } } } }; }
    }
}
