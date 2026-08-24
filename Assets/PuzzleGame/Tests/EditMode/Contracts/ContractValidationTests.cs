using System;
using NUnit.Framework;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Tests.EditMode.Contracts
{
    public sealed class ContractValidationTests
    {
        [Test]
        public void Character_rejects_heart_as_a_playable_element()
        {
            var data = CharacterData.CreateForTests("healer", ElementType.Heart, 3);

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Character element cannot be Heart.");
        }

        [Test]
        public void Stage_requires_at_least_one_wave_and_three_objectives()
        {
            var data = new StageData
            {
                Id = "stage-1",
                Waves = Array.Empty<WaveData>(),
                StarObjectives = Array.Empty<StarObjectiveData>()
            };

            var errors = ContractValidation.Validate(data);

            Assert.That(errors, Has.Count.GreaterThanOrEqualTo(2));
            CollectionAssert.Contains(errors, "Stage must define at least one wave.");
            CollectionAssert.Contains(errors, "Stage must define exactly three star objectives.");
        }

        [Test]
        public void Identified_contract_rejects_a_missing_id()
        {
            var data = CharacterData.CreateForTests(string.Empty, ElementType.Fire, 3);

            CollectionAssert.Contains(ContractValidation.Validate(data), "ID is required.");
        }

        [Test]
        public void Character_rejects_a_rarity_outside_the_pullable_range()
        {
            var data = CharacterData.CreateForTests("invalid-rarity", ElementType.Fire, 6);

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Character base rarity must be between 1 and 5.");
        }

        [Test]
        public void Character_rejects_a_rarity_below_the_pullable_range()
        {
            var data = CharacterData.CreateForTests("zero-rarity", ElementType.Fire, 0);

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Character base rarity must be between 1 and 5.");
        }

        [Test]
        public void Banner_rejects_a_non_positive_character_weight()
        {
            var data = new BannerData
            {
                Id = "standard",
                Type = BannerType.Standard,
                Characters = new[] { new WeightedCharacterData { CharacterId = "unit-1", Weight = 0 } }
            };

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Banner character weight must be positive.");
        }

        [Test]
        public void Banner_rejects_a_negative_character_weight()
        {
            var data = new BannerData
            {
                Id = "standard-negative-weight",
                Type = BannerType.Standard,
                Characters = new[] { new WeightedCharacterData { CharacterId = "unit-1", Weight = -1 } }
            };

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Banner character weight must be positive.");
        }

        [Test]
        public void Stage_rejects_a_wave_with_a_missing_stable_id()
        {
            var data = ValidStage();
            data.Waves[0].Id = string.Empty;

            CollectionAssert.Contains(ContractValidation.Validate(data), "Wave ID is required.");
        }

        [Test]
        public void Wave_exposes_its_stable_id_through_the_shared_contract()
        {
            IIdentifiedData data = new WaveData { Id = "wave-1" };

            Assert.That(data.Id, Is.EqualTo("wave-1"));
        }

        [Test]
        public void Stage_with_one_identified_wave_and_exactly_three_objectives_is_valid()
        {
            var errors = ContractValidation.Validate(ValidStage());

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void Stage_rejects_two_star_objectives()
        {
            var data = ValidStage();
            data.StarObjectives = new[]
            {
                new StarObjectiveData { Type = StarObjectiveType.Clear },
                new StarObjectiveData { Type = StarObjectiveType.FinishAboveHpThreshold, HpThresholdPercent = 50f }
            };

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Stage must define exactly three star objectives.");
        }

        [Test]
        public void Stage_rejects_four_star_objectives()
        {
            var data = ValidStage();
            data.StarObjectives = new[]
            {
                new StarObjectiveData { Type = StarObjectiveType.Clear },
                new StarObjectiveData { Type = StarObjectiveType.FinishAboveHpThreshold, HpThresholdPercent = 50f },
                new StarObjectiveData { Type = StarObjectiveType.ClearWithinBoardResolutionCount, MaximumBoardResolutionCount = 5 },
                new StarObjectiveData { Type = StarObjectiveType.Clear }
            };

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Stage must define exactly three star objectives.");
        }

        [Test]
        public void Schedule_rejects_an_end_that_is_not_after_its_start()
        {
            var start = new DateTimeOffset(2026, 8, 24, 9, 0, 0, TimeSpan.Zero);
            var data = new RotationScheduleData
            {
                Id = "invalid-window",
                ContentId = "event-1",
                Start = start,
                End = start
            };

            CollectionAssert.Contains(ContractValidation.Validate(data), "Schedule end must be after schedule start.");
        }

        [Test]
        public void Schedule_rejects_recurrence_minutes_below_zero()
        {
            var data = ValidSchedule();
            data.StartMinuteOfDay = -1;

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Schedule recurrence minutes must be between 0 and 1439.");
        }

        [Test]
        public void Schedule_rejects_recurrence_minutes_above_1439()
        {
            var data = ValidSchedule();
            data.EndMinuteOfDay = 1440;

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Schedule recurrence minutes must be between 0 and 1439.");
        }

        [Test]
        public void Schedule_rejects_a_recurrence_weekday_below_zero()
        {
            var data = ValidSchedule();
            data.RecurringWeekdays = new[] { -1 };

            CollectionAssert.Contains(ContractValidation.Validate(data), "Schedule weekday must be between 0 and 6.");
        }

        [Test]
        public void Schedule_rejects_a_recurrence_weekday_above_six()
        {
            var data = ValidSchedule();
            data.RecurringWeekdays = new[] { 7 };

            CollectionAssert.Contains(ContractValidation.Validate(data), "Schedule weekday must be between 0 and 6.");
        }

        [Test]
        public void Gather_in_banner_requires_at_least_one_step()
        {
            var data = ValidBanner(BannerType.GatherIn);

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Gather-In banner must define at least one step.");
        }

        [Test]
        public void Stage_rejects_a_null_nested_wave()
        {
            var data = ValidStage();
            data.Waves = new WaveData[] { null };

            CollectionAssert.Contains(ContractValidation.Validate(data), "Wave ID is required.");
        }

        [Test]
        public void Step_up_banner_requires_at_least_one_step()
        {
            var data = ValidBanner(BannerType.StepUp);

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Step-Up banner must define at least one step.");
        }

        [Test]
        public void Gather_in_banner_rejects_a_step_without_a_pull()
        {
            var data = ValidBanner(BannerType.GatherIn);
            data.Steps = new[] { new BannerStepData { PullCount = 0, GemCost = 50 } };

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Banner step pull count must be at least 1.");
        }

        [Test]
        public void Step_up_banner_rejects_a_step_without_a_pull()
        {
            var data = ValidBanner(BannerType.StepUp);
            data.Steps = new[] { new BannerStepData { PullCount = 0, GemCost = 50 } };

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Banner step pull count must be at least 1.");
        }

        [Test]
        public void Gather_in_banner_requires_a_final_guarantee()
        {
            var data = ValidBanner(BannerType.GatherIn);
            data.Steps = new[] { new BannerStepData { PullCount = 10, GemCost = 1500 } };

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Gather-In final step must define a guarantee.");
        }

        [Test]
        public void Gather_in_banner_rejects_conflicting_final_guarantees()
        {
            var data = ValidBanner(BannerType.GatherIn);
            data.Steps = new[]
            {
                new BannerStepData
                {
                    PullCount = 10,
                    GemCost = 1500,
                    GuaranteedFiveStarFeaturedBoost = true,
                    GuaranteedFeaturedFiveStar = true
                }
            };

            CollectionAssert.Contains(
                ContractValidation.Validate(data),
                "Gather-In final step can define only one guarantee.");
        }

        [Test]
        public void Gather_in_banner_with_one_final_guarantee_is_valid()
        {
            var data = ValidBanner(BannerType.GatherIn);
            data.Steps = new[]
            {
                new BannerStepData
                {
                    PullCount = 10,
                    GemCost = 1500,
                    GuaranteedFeaturedFiveStar = true
                }
            };

            Assert.That(ContractValidation.Validate(data), Is.Empty);
        }

        private static StageData ValidStage()
        {
            return new StageData
            {
                Id = "stage-1",
                Waves = new[] { new WaveData { Id = "wave-1" } },
                StarObjectives = new[]
                {
                    new StarObjectiveData { Type = StarObjectiveType.Clear },
                    new StarObjectiveData { Type = StarObjectiveType.FinishAboveHpThreshold, HpThresholdPercent = 50f },
                    new StarObjectiveData { Type = StarObjectiveType.ClearWithinBoardResolutionCount, MaximumBoardResolutionCount = 5 }
                }
            };
        }

        private static BannerData ValidBanner(BannerType type)
        {
            return new BannerData
            {
                Id = "banner-1",
                Type = type,
                Characters = new[] { new WeightedCharacterData { CharacterId = "unit-1", Weight = 1 } }
            };
        }

        private static RotationScheduleData ValidSchedule()
        {
            return new RotationScheduleData
            {
                Id = "schedule-1",
                ContentId = "event-1",
                Start = new DateTimeOffset(2026, 8, 24, 9, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.Zero),
                StartMinuteOfDay = 0,
                EndMinuteOfDay = 1439,
                RecurringWeekdays = new[] { 0 }
            };
        }
    }
}
