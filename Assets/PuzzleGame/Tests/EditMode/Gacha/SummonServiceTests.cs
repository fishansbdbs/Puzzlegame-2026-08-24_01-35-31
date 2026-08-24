using System;
using System.Collections.Generic;
using NUnit.Framework;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Economy;
using PuzzleGame.Core.Gacha;
using PuzzleGame.Core.Progression;

namespace PuzzleGame.Tests.EditMode.Gacha
{
    public sealed class SummonServiceTests
    {
        [TestCase(1, 150)]
        [TestCase(10, 1500)]
        public void Standard_banner_uses_fixed_gem_prices(int count, int expectedCost)
        {
            var service = SummonFixtures.CreateService(BannerType.Standard, new[] { SummonFixtures.Entry("one", 1) });

            Assert.That(service.Quote(count).GemCost, Is.EqualTo(expectedCost));
        }

        [Test]
        public void Standard_banner_rejects_unsupported_pull_counts()
        {
            var service = SummonFixtures.CreateService(BannerType.Standard, new[] { SummonFixtures.Entry("one", 1) });

            Assert.That(() => service.Quote(2), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Weighted_roll_uses_exclusive_upper_bound_boundaries_deterministically()
        {
            var service = SummonFixtures.CreateService(BannerType.Featured, new[]
            {
                SummonFixtures.Entry("one", 2), SummonFixtures.Entry("two", 3)
            }, random: new ScriptedRandom(0, 1, 2, 4));
            service.Wallet.Add(WalletCurrencies.Gems, 600);

            Assert.That(service.PurchaseAndRoll(1).Results[0].CharacterId, Is.EqualTo("one"));
            Assert.That(service.PurchaseAndRoll(1).Results[0].CharacterId, Is.EqualTo("one"));
            Assert.That(service.PurchaseAndRoll(1).Results[0].CharacterId, Is.EqualTo("two"));
            Assert.That(service.PurchaseAndRoll(1).Results[0].CharacterId, Is.EqualTo("two"));
        }

        [Test]
        public void Invalid_weights_and_unknown_catalog_references_are_rejected_before_purchase()
        {
            Assert.That(() => SummonFixtures.CreateService(BannerType.Featured, new[] { SummonFixtures.Entry("one", 0) }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => SummonFixtures.CreateService(BannerType.Featured, new[] { SummonFixtures.Entry("missing", 1) }), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Final_boost_guarantee_replaces_last_result_with_a_five_star_and_doubles_featured_weight()
        {
            var service = SummonFixtures.CreateStepService(new BannerStepData
            {
                PullCount = 2, GemCost = 50, GuaranteedFiveStarFeaturedBoost = true
            }, new[]
            {
                SummonFixtures.Entry("one", 1), SummonFixtures.Entry("five-normal", 1), SummonFixtures.Entry("five-featured", 1, true)
            }, new ScriptedRandom(0, 0, 2));
            service.Wallet.Add(WalletCurrencies.Gems, 50);

            var batch = service.PurchaseAndRoll();

            Assert.That(batch.Results, Has.Count.EqualTo(2));
            Assert.That(batch.Results[0].CharacterId, Is.EqualTo("one"));
            Assert.That(batch.Results[1].Rarity, Is.EqualTo(5));
            Assert.That(batch.Results[1].CharacterId, Is.EqualTo("five-featured"));
            Assert.That(SummonService.FeaturedGuaranteeWeightMultiplier, Is.EqualTo(2));
        }

        [Test]
        public void Final_featured_guarantee_replaces_last_result_with_featured_five_star()
        {
            var service = SummonFixtures.CreateStepService(new BannerStepData
            {
                PullCount = 2, GemCost = 50, GuaranteedFeaturedFiveStar = true
            }, new[] { SummonFixtures.Entry("one", 1), SummonFixtures.Entry("five-featured", 1, true) }, new ScriptedRandom(0, 0, 0));
            service.Wallet.Add(WalletCurrencies.Gems, 50);

            var batch = service.PurchaseAndRoll();

            Assert.That(batch.Results[1].CharacterId, Is.EqualTo("five-featured"));
            Assert.That(batch.Results[1].Rarity, Is.EqualTo(5));
        }

        [Test]
        public void Steps_quote_current_step_and_advance_only_after_successful_purchase()
        {
            var banner = SummonFixtures.Banner(BannerType.GatherIn, new[] { SummonFixtures.Entry("one", 1), SummonFixtures.Entry("five-featured", 1, true) });
            banner.Steps = new[]
            {
                new BannerStepData { PullCount = 1, GemCost = 50 },
                new BannerStepData { PullCount = 2, GemCost = 100, GuaranteedFiveStarFeaturedBoost = true }
            };
            var service = SummonFixtures.CreateService(banner, new ScriptedRandom(0, 0, 0, 0));

            Assert.That(service.Quote().PullCount, Is.EqualTo(1));
            Assert.That(service.CanSummon(), Is.False);
            Assert.That(service.State.NextStepIndex, Is.EqualTo(0));
            service.Wallet.Add(WalletCurrencies.Gems, 150);
            service.PurchaseAndRoll();
            Assert.That(service.Quote().PullCount, Is.EqualTo(2));
            service.PurchaseAndRoll();
            Assert.That(service.Quote().IsAvailable, Is.False);
            Assert.That(service.State.IsComplete, Is.True);
            Assert.That(() => service.PurchaseAndRoll(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Random_failure_insufficient_funds_and_duplicate_overflow_leave_all_transaction_state_unchanged()
        {
            var randomFailure = SummonFixtures.CreateService(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) }, new ThrowingRandom());
            randomFailure.Wallet.Add(WalletCurrencies.Gems, 150);
            Assert.That(() => randomFailure.PurchaseAndRoll(1), Throws.TypeOf<InvalidOperationException>());
            Assert.That(randomFailure.Wallet.GetBalance(WalletCurrencies.Gems), Is.EqualTo(150));

            var insufficientBanner = SummonFixtures.Banner(BannerType.GatherIn, new[] { SummonFixtures.Entry("one", 1), SummonFixtures.Entry("five-featured", 1, true) });
            insufficientBanner.Steps = new[] { new BannerStepData { PullCount = 1, GemCost = 50, GuaranteedFiveStarFeaturedBoost = true } };
            var insufficient = SummonFixtures.CreateService(insufficientBanner);
            Assert.That(insufficient.CanSummon(), Is.False);
            Assert.That(insufficient.State.NextStepIndex, Is.EqualTo(0));

            var progress = SummonFixtures.MaxProgress("one");
            var wallet = new Wallet(); wallet.Add(WalletCurrencies.Gems, 150); wallet.Add(WalletCurrencies.UniversalDuplicateResource, int.MaxValue);
            var overflow = SummonFixtures.CreateService(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) }, new ScriptedRandom(0), wallet, new[] { progress });
            Assert.That(() => overflow.PurchaseAndRoll(1), Throws.TypeOf<OverflowException>());
            Assert.That(wallet.GetBalance(WalletCurrencies.Gems), Is.EqualTo(150));
            Assert.That(progress.Ascension, Is.EqualTo(5));
        }

        [Test]
        public void Results_snapshot_new_or_duplicate_and_apply_progression_before_returning()
        {
            var service = SummonFixtures.CreateService(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) }, new ScriptedRandom(0, 0));
            service.Wallet.Add(WalletCurrencies.Gems, 300);

            var first = service.PurchaseAndRoll(1).Results[0];
            var second = service.PurchaseAndRoll(1).Results[0];

            Assert.That(first.Ownership, Is.EqualTo(SummonOwnership.New));
            Assert.That(second.Ownership, Is.EqualTo(SummonOwnership.Duplicate));
            Assert.That(second.Duplicate.PreviousAscension, Is.EqualTo(0));
            Assert.That(second.Duplicate.NewAscension, Is.EqualTo(1));
            Assert.That(service.Collection.GetProgress("one").Ascension, Is.EqualTo(1));
        }

        [Test]
        public void Batch_and_results_do_not_alias_mutable_authored_data_or_collections()
        {
            var banner = SummonFixtures.Banner(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) });
            var service = SummonFixtures.CreateService(banner, new ScriptedRandom(0));
            service.Wallet.Add(WalletCurrencies.Gems, 150);
            var batch = service.PurchaseAndRoll(1);
            banner.PresentationKey = "changed";
            banner.Characters[0].CharacterId = "missing";

            Assert.That(batch.PresentationKey, Is.EqualTo("pack"));
            Assert.That(batch.Results[0].CharacterId, Is.EqualTo("one"));
            Assert.That(batch.Results, Is.Not.AssignableTo<List<SummonResult>>());
        }

        [Test]
        public void Featured_uses_authored_prices_without_steps_and_current_step_when_steps_are_authored()
        {
            var fixedBanner = SummonFixtures.Banner(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) });
            fixedBanner.SinglePullGemCost = 77; fixedBanner.TenPullGemCost = 701;
            var fixedService = SummonFixtures.CreateService(fixedBanner);
            Assert.That(fixedService.Quote(1).GemCost, Is.EqualTo(77));
            Assert.That(fixedService.Quote(10).GemCost, Is.EqualTo(701));

            var steppedBanner = SummonFixtures.Banner(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) });
            steppedBanner.Steps = new[] { new BannerStepData { PullCount = 2, GemCost = 33 } };
            var steppedService = SummonFixtures.CreateService(steppedBanner);
            Assert.That(steppedService.Quote().PullCount, Is.EqualTo(2));
            Assert.That(steppedService.Quote().GemCost, Is.EqualTo(33));
        }

        [Test]
        public void Step_up_uses_steps_and_binds_runtime_state_to_banner_rotation_and_shape()
        {
            var banner = SummonFixtures.Banner(BannerType.StepUp, new[] { SummonFixtures.Entry("one", 1) });
            banner.RotationId = "rotation-a";
            banner.Steps = new[] { new BannerStepData { PullCount = 1, GemCost = 12 } };
            var state = new BannerRuntimeState("banner", "rotation-a", 1);
            var service = new SummonService(banner, SummonFixtures.Catalog(), new Wallet(), new ScriptedRandom(0), new SummonCollection(), state);
            Assert.That(service.Quote().GemCost, Is.EqualTo(12));

            Assert.That(() => new BannerRuntimeState("banner", "rotation-a", 1, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new SummonService(banner, SummonFixtures.Catalog(), new Wallet(), new ScriptedRandom(0), new SummonCollection(), new BannerRuntimeState("banner", "other", 1)), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new SummonService(banner, SummonFixtures.Catalog(), new Wallet(), new ScriptedRandom(0), new SummonCollection(), new BannerRuntimeState("banner", "rotation-a", 0)), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Invalid_banner_pool_enum_random_and_skill_content_are_rejected_before_mutation()
        {
            Assert.That(() => SummonFixtures.CreateService(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1), SummonFixtures.Entry("one", 2) }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => SummonFixtures.CreateService((BannerType)999, new[] { SummonFixtures.Entry("one", 1) }), Throws.TypeOf<ArgumentException>());

            var invalidElement = SummonFixtures.Definition("one", 1); invalidElement.Character.Element = (ElementType)999;
            Assert.That(() => new SummonService(SummonFixtures.Banner(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) }), new[] { invalidElement }, new Wallet(), new ScriptedRandom(0)), Throws.TypeOf<ArgumentException>());

            var invalidSkill = SummonFixtures.Definition("one", 1); invalidSkill.ActiveSkill.ChargeRequired = 0;
            Assert.That(() => new SummonService(SummonFixtures.Banner(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) }), new[] { invalidSkill }, new Wallet(), new ScriptedRandom(0)), Throws.TypeOf<ArgumentException>());

            var outOfRange = SummonFixtures.CreateService(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) }, new ScriptedRandom(1));
            outOfRange.Wallet.Add(WalletCurrencies.Gems, 150);
            Assert.That(() => outOfRange.PurchaseAndRoll(1), Throws.TypeOf<InvalidOperationException>());
            Assert.That(outOfRange.Wallet.GetBalance(WalletCurrencies.Gems), Is.EqualTo(150));

            Assert.That(() => SummonFixtures.CreateService(BannerType.Featured, new[] { SummonFixtures.Entry("one", int.MaxValue), SummonFixtures.Entry("two", int.MaxValue) }), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Same_id_multi_pull_plans_exact_sequential_duplicate_results_before_commit()
        {
            var progress = SummonFixtures.MaxProgress("one");
            var wallet = new Wallet(); wallet.Add(WalletCurrencies.Gems, 1500);
            var service = SummonFixtures.CreateService(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) }, new ScriptedRandom(new int[10]), wallet, new[] { progress });

            var batch = service.PurchaseAndRoll(10);

            for (var index = 0; index < 10; index++)
            {
                Assert.That(batch.Results[index].Ownership, Is.EqualTo(SummonOwnership.Duplicate));
                Assert.That(batch.Results[index].Duplicate.PreviousAscension, Is.EqualTo(5));
                Assert.That(batch.Results[index].Duplicate.NewAscension, Is.EqualTo(5));
                Assert.That(batch.Results[index].Duplicate.UniversalResourceGranted, Is.EqualTo(1));
            }
            Assert.That(wallet.GetBalance(WalletCurrencies.UniversalDuplicateResource), Is.EqualTo(10));
        }

        [Test]
        public void Late_mixed_batch_failure_rolls_back_new_records_existing_progress_currency_and_rotation()
        {
            var banner = SummonFixtures.Banner(BannerType.StepUp, new[] { SummonFixtures.Entry("two", 1), SummonFixtures.Entry("one", 1) });
            banner.Steps = new[] { new BannerStepData { PullCount = 2, GemCost = 50, GuaranteedFiveStarFeaturedBoost = false } };
            var existing = SummonFixtures.MaxProgress("one");
            var wallet = new Wallet(); wallet.Add(WalletCurrencies.Gems, 50); wallet.Add(WalletCurrencies.UniversalDuplicateResource, int.MaxValue);
            var service = SummonFixtures.CreateService(banner, new ScriptedRandom(0, 1), wallet, new[] { existing });

            Assert.That(() => service.PurchaseAndRoll(), Throws.TypeOf<OverflowException>());
            Assert.That(wallet.GetBalance(WalletCurrencies.Gems), Is.EqualTo(50));
            Assert.That(wallet.GetBalance(WalletCurrencies.UniversalDuplicateResource), Is.EqualTo(int.MaxValue));
            Assert.That(service.Collection.Contains("two"), Is.False);
            Assert.That(existing.Ascension, Is.EqualTo(5));
            Assert.That(service.State.NextStepIndex, Is.EqualTo(0));
        }

        [Test]
        public void Configured_featured_steps_purchase_authored_counts_costs_and_complete_the_rotation()
        {
            var banner = SummonFixtures.Banner(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) });
            banner.Steps = new[] { new BannerStepData { PullCount = 1, GemCost = 17 }, new BannerStepData { PullCount = 2, GemCost = 29 } };
            var service = SummonFixtures.CreateService(banner, new ScriptedRandom(0, 0, 0));
            service.Wallet.Add(WalletCurrencies.Gems, 46);

            var first = service.PurchaseAndRoll();
            var second = service.PurchaseAndRoll();

            Assert.That(first.GemCost, Is.EqualTo(17)); Assert.That(first.Results, Has.Count.EqualTo(1));
            Assert.That(second.GemCost, Is.EqualTo(29)); Assert.That(second.Results, Has.Count.EqualTo(2));
            Assert.That(service.State.NextStepIndex, Is.EqualTo(2)); Assert.That(service.State.IsComplete, Is.True);
            Assert.That(service.Wallet.GetBalance(WalletCurrencies.Gems), Is.EqualTo(0));
            Assert.That(() => service.PurchaseAndRoll(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Step_up_purchase_advances_each_authored_step_then_rejects_completed_rotation()
        {
            var banner = SummonFixtures.Banner(BannerType.StepUp, new[] { SummonFixtures.Entry("one", 1) });
            banner.Steps = new[] { new BannerStepData { PullCount = 1, GemCost = 5 }, new BannerStepData { PullCount = 2, GemCost = 9 } };
            var service = SummonFixtures.CreateService(banner, new ScriptedRandom(0, 0, 0));
            service.Wallet.Add(WalletCurrencies.Gems, 14);

            Assert.That(service.PurchaseAndRoll().Results, Has.Count.EqualTo(1));
            Assert.That(service.State.NextStepIndex, Is.EqualTo(1));
            Assert.That(service.PurchaseAndRoll().Results, Has.Count.EqualTo(2));
            Assert.That(service.State.IsComplete, Is.True);
            Assert.That(() => service.PurchaseAndRoll(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Owned_overflow_configuration_not_catalog_configuration_controls_atomic_rollback()
        {
            var ownedDefinition = SummonFixtures.Definition("one", 1);
            ownedDefinition.Character.Ascension.OverflowUniversalResourceAmount = 2;
            var owned = new CharacterProgress(ownedDefinition.Character, ownedDefinition.ActiveSkill);
            var setupWallet = new Wallet(); for (var index = 0; index < 5; index++) ProgressionService.ApplyDuplicate(owned, setupWallet);
            var wallet = new Wallet(); wallet.Add(WalletCurrencies.Gems, 150); wallet.Add(WalletCurrencies.UniversalDuplicateResource, int.MaxValue - 1);
            var service = SummonFixtures.CreateService(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) }, new ScriptedRandom(0), wallet, new[] { owned });

            Assert.That(() => service.PurchaseAndRoll(1), Throws.TypeOf<OverflowException>());
            Assert.That(wallet.GetBalance(WalletCurrencies.Gems), Is.EqualTo(150));
            Assert.That(wallet.GetBalance(WalletCurrencies.UniversalDuplicateResource), Is.EqualTo(int.MaxValue - 1));
            Assert.That(owned.Ascension, Is.EqualTo(5));
        }

        [Test]
        public void Negative_rng_invalid_guarantee_candidates_and_guarantee_overflow_do_not_mutate_a_purchase()
        {
            var negative = SummonFixtures.CreateService(BannerType.Featured, new[] { SummonFixtures.Entry("one", 1) }, new ScriptedRandom(-1));
            negative.Wallet.Add(WalletCurrencies.Gems, 150);
            Assert.That(() => negative.PurchaseAndRoll(1), Throws.TypeOf<InvalidOperationException>());
            Assert.That(negative.Wallet.GetBalance(WalletCurrencies.Gems), Is.EqualTo(150));

            var noFive = SummonFixtures.Banner(BannerType.GatherIn, new[] { SummonFixtures.Entry("one", 1) });
            noFive.Steps = new[] { new BannerStepData { PullCount = 1, GemCost = 1, GuaranteedFiveStarFeaturedBoost = true } };
            Assert.That(() => SummonFixtures.CreateService(noFive), Throws.TypeOf<ArgumentException>());
            var noFeaturedFive = SummonFixtures.Banner(BannerType.GatherIn, new[] { SummonFixtures.Entry("five-normal", 1) });
            noFeaturedFive.Steps = new[] { new BannerStepData { PullCount = 1, GemCost = 1, GuaranteedFeaturedFiveStar = true } };
            Assert.That(() => SummonFixtures.CreateService(noFeaturedFive), Throws.TypeOf<ArgumentException>());

            var overflowGuarantee = SummonFixtures.Banner(BannerType.StepUp, new[] { SummonFixtures.Entry("five-featured", int.MaxValue, true) });
            overflowGuarantee.Steps = new[] { new BannerStepData { PullCount = 1, GemCost = 10, GuaranteedFiveStarFeaturedBoost = true } };
            var service = SummonFixtures.CreateService(overflowGuarantee, new ScriptedRandom(0, 0));
            service.Wallet.Add(WalletCurrencies.Gems, 10);
            Assert.That(() => service.PurchaseAndRoll(), Throws.TypeOf<ArgumentException>());
            Assert.That(service.Wallet.GetBalance(WalletCurrencies.Gems), Is.EqualTo(10));
            Assert.That(service.State.NextStepIndex, Is.EqualTo(0));
        }
    }

    internal static class SummonFixtures
    {
        internal static SummonService CreateService(BannerType type, WeightedCharacterData[] entries, IRandomSource random = null, Wallet wallet = null, IEnumerable<CharacterProgress> owned = null)
        {
            return CreateService(Banner(type, entries), random, wallet, owned);
        }

        internal static SummonService CreateService(BannerData banner, IRandomSource random = null, Wallet wallet = null, IEnumerable<CharacterProgress> owned = null)
        {
            return new SummonService(banner, Catalog(), wallet ?? new Wallet(), random ?? new ScriptedRandom(0), new SummonCollection(owned));
        }

        internal static SummonService CreateStepService(BannerStepData step, WeightedCharacterData[] entries, IRandomSource random)
        {
            var banner = Banner(BannerType.GatherIn, entries); banner.Steps = new[] { step };
            return CreateService(banner, random);
        }

        internal static BannerData Banner(BannerType type, WeightedCharacterData[] entries)
        {
            return new BannerData { Id = "banner", Type = type, Characters = entries, PresentationKey = "pack", SinglePullGemCost = 150, TenPullGemCost = 1500, Steps = Array.Empty<BannerStepData>() };
        }

        internal static WeightedCharacterData Entry(string id, int weight, bool featured = false)
        {
            return new WeightedCharacterData { CharacterId = id, Weight = weight, IsFeatured = featured };
        }

        internal static IEnumerable<SummonCharacterDefinition> Catalog()
        {
            return new[] { Definition("one", 1), Definition("two", 2), Definition("five-normal", 5), Definition("five-featured", 5) };
        }

        internal static CharacterProgress MaxProgress(string id)
        {
            var definition = Definition(id, id == "one" ? 1 : 5); var progress = new CharacterProgress(definition.Character, definition.ActiveSkill); var wallet = new Wallet();
            for (var index = 0; index < 5; index++) ProgressionService.ApplyDuplicate(progress, wallet);
            return progress;
        }

        internal static SummonCharacterDefinition Definition(string id, int rarity)
        {
            var character = CharacterData.CreateForTests(id, ElementType.Fire, rarity); character.ActiveSkillId = id + "-skill";
            var skill = new SkillData { Id = character.ActiveSkillId, ChargeElement = ElementType.Fire, ChargeRequired = 1, Effects = new[] { new SkillEffectData { Payload = new EffectPayloadData() } } };
            return new SummonCharacterDefinition(character, skill);
        }
    }

    internal sealed class ScriptedRandom : IRandomSource
    {
        private readonly Queue<int> values;
        internal ScriptedRandom(params int[] values) { this.values = new Queue<int>(values); }
        public int NextInt(int exclusiveUpperBound) { return values.Dequeue(); }
    }

    internal sealed class ThrowingRandom : IRandomSource
    {
        public int NextInt(int exclusiveUpperBound) { throw new InvalidOperationException("random failed"); }
    }
}
