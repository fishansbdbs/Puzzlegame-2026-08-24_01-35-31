using System;
using NUnit.Framework;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Economy;
using PuzzleGame.Core.Progression;

namespace PuzzleGame.Tests.EditMode.Progression
{
    public sealed class ProgressionTests
    {
        [Test]
        public void Experience_uses_authored_cumulative_thresholds_and_returns_level_stats()
        {
            var progress = new CharacterProgress(ProgressionFixtures.ValidCharacter());

            var result = ProgressionService.ApplyExperience(progress, 100);

            Assert.That(result.PreviousLevel, Is.EqualTo(1));
            Assert.That(result.NewLevel, Is.EqualTo(2));
            Assert.That(result.TotalExperience, Is.EqualTo(100));
            Assert.That(result.Stats.Hp, Is.EqualTo(150));
            Assert.That(result.Stats.Attack, Is.EqualTo(30));
            Assert.That(result.Stats.Recovery, Is.EqualTo(8));
        }

        [Test]
        public void Experience_at_max_level_is_clamped_to_the_authored_cap()
        {
            var progress = new CharacterProgress(ProgressionFixtures.ValidCharacter());

            var result = ProgressionService.ApplyExperience(progress, 1000);

            Assert.That(result.NewLevel, Is.EqualTo(3));
            Assert.That(result.TotalExperience, Is.EqualTo(250));
            Assert.That(result.AppliedExperience, Is.EqualTo(250));
            Assert.That(result.IsAtMaxLevel, Is.True);
        }

        [Test]
        public void Malformed_progression_curves_are_rejected_instead_of_falling_back_to_base_stats()
        {
            var data = ProgressionFixtures.ValidCharacter();
            data.LevelCurve.StatsByLevel = new[] { new StatBlock { Hp = 100, Attack = 20, Recovery = 5 } };

            Assert.That(() => new CharacterProgress(data), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Descending_experience_thresholds_are_rejected()
        {
            var data = ProgressionFixtures.ValidCharacter();
            data.LevelCurve.ExperienceRequiredByLevel = new[] { 250, 100 };

            Assert.That(() => new CharacterProgress(data), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Authored_ascension_ranks_apply_their_explicit_effect_bands()
        {
            var progress = new CharacterProgress(ProgressionFixtures.ValidCharacter());
            var wallet = new Wallet();

            var first = ProgressionService.ApplyDuplicate(progress, wallet);
            var second = ProgressionService.ApplyDuplicate(progress, wallet);
            var third = ProgressionService.ApplyDuplicate(progress, wallet);
            var fourth = ProgressionService.ApplyDuplicate(progress, wallet);
            var fifth = ProgressionService.ApplyDuplicate(progress, wallet);

            Assert.That(first.NewAscension, Is.EqualTo(1));
            Assert.That(first.Effects.ActiveSkillPerformanceBonus, Is.EqualTo(10));
            Assert.That(second.Effects.ActiveSkillPerformanceBonus, Is.EqualTo(25));
            Assert.That(third.Effects.PassivePerformanceBonus, Is.EqualTo(5));
            Assert.That(fourth.Effects.StatBonus.Hp, Is.EqualTo(20));
            Assert.That(fifth.Effects.StatBonus.Attack, Is.EqualTo(5));
        }

        [Test]
        public void Sixth_duplicate_after_ascension_five_becomes_universal_resource()
        {
            var progress = ProgressionFixtures.MaxAscended();
            var wallet = new Wallet();

            var result = ProgressionService.ApplyDuplicate(progress, wallet);

            Assert.That(result.NewAscension, Is.EqualTo(5));
            Assert.That(result.UniversalResourceGranted, Is.EqualTo(25));
            Assert.That(wallet.GetBalance(WalletCurrencies.UniversalDuplicateResource), Is.EqualTo(25));
        }

        [Test]
        public void Duplicate_conversion_overflow_leaves_the_unit_and_wallet_unchanged()
        {
            var progress = ProgressionFixtures.MaxAscended();
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.UniversalDuplicateResource, int.MaxValue);

            Assert.That(() => ProgressionService.ApplyDuplicate(progress, wallet), Throws.TypeOf<OverflowException>());
            Assert.That(progress.Ascension, Is.EqualTo(5));
            Assert.That(wallet.GetBalance(WalletCurrencies.UniversalDuplicateResource), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void Progression_snapshots_do_not_alias_mutable_authored_data()
        {
            var data = ProgressionFixtures.ValidCharacter();
            var progress = new CharacterProgress(data);
            data.LevelCurve.StatsByLevel[0].Hp = 999;
            data.AwakenedVisuals.PortraitKey = "altered";

            Assert.That(progress.CurrentStats.Hp, Is.EqualTo(100));
            Assert.That(progress.CurrentVisuals.PortraitKey, Is.EqualTo("base-portrait"));
        }

        [Test]
        public void Awakening_consumes_materials_and_gold_without_checking_duplicates()
        {
            var progress = ProgressionFixtures.AwakeningReady();
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.Gold, 40);
            var materials = new MaterialInventory();
            materials.Add("crystal", 3);

            var result = ProgressionService.Awaken(progress, wallet, materials);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(progress.Ascension, Is.EqualTo(0));
            Assert.That(progress.EffectiveRarity, Is.EqualTo(6));
            Assert.That(result.Visuals.PortraitKey, Is.EqualTo("awakened-portrait"));
            Assert.That(wallet.GetBalance(WalletCurrencies.Gold), Is.EqualTo(0));
            Assert.That(materials.GetBalance("crystal"), Is.EqualTo(0));
        }

        [Test]
        public void Failed_awakening_leaves_gold_materials_and_unit_state_unchanged()
        {
            var progress = ProgressionFixtures.AwakeningReady();
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.Gold, 40);
            var materials = new MaterialInventory();
            materials.Add("crystal", 2);

            var result = ProgressionService.Awaken(progress, wallet, materials);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(progress.EffectiveRarity, Is.EqualTo(5));
            Assert.That(wallet.GetBalance(WalletCurrencies.Gold), Is.EqualTo(40));
            Assert.That(materials.GetBalance("crystal"), Is.EqualTo(2));
        }

        [Test]
        public void Can_awaken_requires_the_authored_level()
        {
            var progress = new CharacterProgress(ProgressionFixtures.ValidCharacter());
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.Gold, 40);
            var materials = new MaterialInventory();
            materials.Add("crystal", 3);

            Assert.That(ProgressionService.CanAwaken(progress, wallet, materials), Is.False);
        }
    }

    internal static class ProgressionFixtures
    {
        internal static CharacterData ValidCharacter()
        {
            return new CharacterData
            {
                Id = "unit-1",
                Element = ElementType.Fire,
                BaseRarity = 5,
                BaseStats = new StatBlock { Hp = 100, Attack = 20, Recovery = 5 },
                LevelCurve = new ProgressionCurveData
                {
                    MaxLevel = 3,
                    ExperienceRequiredByLevel = new[] { 100, 250 },
                    StatsByLevel = new[]
                    {
                        new StatBlock { Hp = 100, Attack = 20, Recovery = 5 },
                        new StatBlock { Hp = 150, Attack = 30, Recovery = 8 },
                        new StatBlock { Hp = 220, Attack = 45, Recovery = 12 }
                    }
                },
                Ascension = new AscensionConfigurationData
                {
                    OverflowUniversalResourceAmount = 25,
                    Ranks = new[]
                    {
                        new AscensionRankData { Rank = 1, ActiveSkillPerformanceBonus = 10 },
                        new AscensionRankData { Rank = 2, ActiveSkillPerformanceBonus = 15 },
                        new AscensionRankData { Rank = 3, PassivePerformanceBonus = 5 },
                        new AscensionRankData { Rank = 4, StatBonus = new StatBlock { Hp = 20, Attack = 0, Recovery = 0 } },
                        new AscensionRankData { Rank = 5, StatBonus = new StatBlock { Hp = 0, Attack = 5, Recovery = 1 } }
                    }
                },
                Awakening = new AwakeningRequirementData
                {
                    RequiredLevel = 3,
                    GoldCost = 40,
                    Materials = new[] { new MaterialRequirementData { MaterialId = "crystal", Amount = 3 } }
                },
                BaseVisuals = new VisualReferenceSet { PortraitKey = "base-portrait" },
                AwakenedVisuals = new VisualReferenceSet { PortraitKey = "awakened-portrait" }
            };
        }

        internal static CharacterProgress AwakeningReady()
        {
            var progress = new CharacterProgress(ValidCharacter());
            ProgressionService.ApplyExperience(progress, 250);
            return progress;
        }

        internal static CharacterProgress MaxAscended()
        {
            var progress = new CharacterProgress(ValidCharacter());
            var wallet = new Wallet();
            for (var index = 0; index < 5; index++) ProgressionService.ApplyDuplicate(progress, wallet);
            return progress;
        }
    }
}
