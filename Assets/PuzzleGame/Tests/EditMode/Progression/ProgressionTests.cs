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
            var progress = ProgressionFixtures.CreateProgress();

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
            var progress = ProgressionFixtures.CreateProgress();

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

            Assert.That(() => new CharacterProgress(data, ProgressionFixtures.ActiveSkill()), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Descending_experience_thresholds_are_rejected()
        {
            var data = ProgressionFixtures.ValidCharacter();
            data.LevelCurve.ExperienceRequiredByLevel = new[] { 250, 100 };

            Assert.That(() => new CharacterProgress(data, ProgressionFixtures.ActiveSkill()), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Authored_ascension_ranks_apply_their_explicit_effect_bands()
        {
            var progress = ProgressionFixtures.CreateProgress();
            var wallet = new Wallet();

            var first = ProgressionService.ApplyDuplicate(progress, wallet);
            var second = ProgressionService.ApplyDuplicate(progress, wallet);
            var third = ProgressionService.ApplyDuplicate(progress, wallet);
            var fourth = ProgressionService.ApplyDuplicate(progress, wallet);
            var fifth = ProgressionService.ApplyDuplicate(progress, wallet);

            Assert.That(first.NewAscension, Is.EqualTo(1));
            Assert.That(first.Effects.ActiveSkillChargeReduction, Is.EqualTo(2));
            Assert.That(second.Effects.ActiveSkillChargeReduction, Is.EqualTo(5));
            Assert.That(third.Effects.StatBonus.Hp, Is.EqualTo(10));
            Assert.That(fourth.Effects.StatBonus.Hp, Is.EqualTo(30));
            Assert.That(fifth.Effects.StatBonus.Attack, Is.EqualTo(5));
            Assert.That(progress.CurrentStats.Hp, Is.EqualTo(130));
        }

        [Test]
        public void Early_ascension_reduces_effective_active_skill_charge_and_persists_cumulative_effects()
        {
            var progress = ProgressionFixtures.CreateProgress();
            var wallet = new Wallet();

            ProgressionService.ApplyDuplicate(progress, wallet);
            Assert.That(progress.EffectiveActiveSkill.ChargeRequired, Is.EqualTo(8));
            Assert.That(progress.CurrentAscensionEffects.ActiveSkillChargeReduction, Is.EqualTo(2));

            ProgressionService.ApplyDuplicate(progress, wallet);
            Assert.That(progress.EffectiveActiveSkill.ChargeRequired, Is.EqualTo(5));
            Assert.That(progress.CurrentAscensionEffects.ActiveSkillChargeReduction, Is.EqualTo(5));
        }

        [Test]
        public void Effective_active_skill_charge_has_a_minimum_of_one()
        {
            var data = ProgressionFixtures.ValidCharacter();
            var skill = ProgressionFixtures.ActiveSkill();
            skill.ChargeRequired = 3;
            var progress = new CharacterProgress(data, skill);
            var wallet = new Wallet();

            ProgressionService.ApplyDuplicate(progress, wallet);
            ProgressionService.ApplyDuplicate(progress, wallet);

            Assert.That(progress.EffectiveActiveSkill.ChargeRequired, Is.EqualTo(1));
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
            var progress = new CharacterProgress(data, ProgressionFixtures.ActiveSkill());
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
            var progress = ProgressionFixtures.CreateProgress();
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.Gold, 40);
            var materials = new MaterialInventory();
            materials.Add("crystal", 3);

            Assert.That(ProgressionService.CanAwaken(progress, wallet, materials), Is.False);
        }

        [Test]
        public void Insufficient_gold_blocks_awakening_without_mutating_materials_or_the_unit()
        {
            var progress = ProgressionFixtures.AwakeningReady();
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.Gold, 39);
            var materials = new MaterialInventory();
            materials.Add("crystal", 3);

            var result = ProgressionService.Awaken(progress, wallet, materials);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureCode, Is.EqualTo("InsufficientGold"));
            Assert.That(progress.EffectiveRarity, Is.EqualTo(5));
            Assert.That(wallet.GetBalance(WalletCurrencies.Gold), Is.EqualTo(39));
            Assert.That(materials.GetBalance("crystal"), Is.EqualTo(3));
        }

        [Test]
        public void Experience_threshold_boundaries_and_incremental_gains_choose_the_authored_level()
        {
            var progress = ProgressionFixtures.CreateProgress();

            Assert.That(ProgressionService.ApplyExperience(progress, 99).NewLevel, Is.EqualTo(1));
            Assert.That(ProgressionService.ApplyExperience(progress, 1).NewLevel, Is.EqualTo(2));
            Assert.That(ProgressionService.ApplyExperience(progress, 149).NewLevel, Is.EqualTo(2));
            Assert.That(ProgressionService.ApplyExperience(progress, 1).NewLevel, Is.EqualTo(3));
            Assert.That(ProgressionService.ApplyExperience(progress, 1).AppliedExperience, Is.EqualTo(0));
        }

        [Test]
        public void Wrong_band_ascension_ranks_and_configured_zero_gold_are_rejected()
        {
            var earlyWrongBand = ProgressionFixtures.ValidCharacter();
            earlyWrongBand.Ascension.Ranks[0].StatBonus = new StatBlock { Hp = 1 };
            Assert.That(() => new CharacterProgress(earlyWrongBand, ProgressionFixtures.ActiveSkill()), Throws.TypeOf<ArgumentException>());

            var lateWrongBand = ProgressionFixtures.ValidCharacter();
            lateWrongBand.Ascension.Ranks[2].StatBonus = null;
            Assert.That(() => new CharacterProgress(lateWrongBand, ProgressionFixtures.ActiveSkill()), Throws.TypeOf<ArgumentException>());

            var zeroGold = ProgressionFixtures.ValidCharacter();
            zeroGold.Awakening.GoldCost = 0;
            Assert.That(() => new CharacterProgress(zeroGold, ProgressionFixtures.ActiveSkill()), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Overflowing_derived_stats_or_cumulative_charge_reduction_are_rejected_before_runtime_state_exists()
        {
            var statsOverflow = ProgressionFixtures.ValidCharacter();
            statsOverflow.LevelCurve.StatsByLevel[2].Hp = int.MaxValue;
            statsOverflow.Ascension.Ranks[2].StatBonus = new StatBlock { Hp = 1 };
            Assert.That(() => new CharacterProgress(statsOverflow, ProgressionFixtures.ActiveSkill()), Throws.TypeOf<ArgumentException>());

            var chargeOverflow = ProgressionFixtures.ValidCharacter();
            chargeOverflow.Ascension.Ranks[0].ActiveSkillChargeReduction = int.MaxValue;
            chargeOverflow.Ascension.Ranks[1].ActiveSkillChargeReduction = int.MaxValue;
            Assert.That(() => new CharacterProgress(chargeOverflow, ProgressionFixtures.ActiveSkill()), Throws.TypeOf<ArgumentException>());
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
                ActiveSkillId = "skill-1",
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
                        new AscensionRankData { Rank = 1, ActiveSkillChargeReduction = 2 },
                        new AscensionRankData { Rank = 2, ActiveSkillChargeReduction = 3 },
                        new AscensionRankData { Rank = 3, StatBonus = new StatBlock { Hp = 10, Attack = 0, Recovery = 0 } },
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
            var progress = CreateProgress();
            ProgressionService.ApplyExperience(progress, 250);
            return progress;
        }

        internal static CharacterProgress MaxAscended()
        {
            var progress = CreateProgress();
            var wallet = new Wallet();
            for (var index = 0; index < 5; index++) ProgressionService.ApplyDuplicate(progress, wallet);
            return progress;
        }

        internal static CharacterProgress CreateProgress()
        {
            return new CharacterProgress(ValidCharacter(), ActiveSkill());
        }

        internal static SkillData ActiveSkill()
        {
            return new SkillData
            {
                Id = "skill-1",
                ChargeElement = ElementType.Fire,
                ChargeRequired = 10,
                Effects = new[] { new SkillEffectData { Type = SkillEffectType.Heal, Payload = new EffectPayloadData() } }
            };
        }
    }
}
