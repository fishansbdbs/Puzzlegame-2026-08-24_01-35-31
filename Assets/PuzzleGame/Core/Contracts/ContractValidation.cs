using System;
using System.Collections.Generic;

namespace PuzzleGame.Core.Contracts
{
    public static class ContractValidation
    {
        public static IReadOnlyList<string> Validate(IIdentifiedData data)
        {
            var errors = new List<string>();

            if (data == null)
            {
                errors.Add("Data cannot be null.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(data.Id))
            {
                errors.Add("ID is required.");
            }

            var character = data as CharacterData;
            if (character != null)
            {
                ValidateCharacter(character, errors);
                return errors;
            }

            var skill = data as SkillData;
            if (skill != null)
            {
                ValidateSkill(skill.ChargeRequired, skill.Effects, errors);
                return errors;
            }

            var leaderSkill = data as LeaderSkillData;
            if (leaderSkill != null)
            {
                ValidateSkill(0, leaderSkill.Effects, errors);
                return errors;
            }

            var enemy = data as EnemyData;
            if (enemy != null)
            {
                ValidateEnemy(enemy, errors);
                return errors;
            }

            var stage = data as StageData;
            if (stage != null)
            {
                ValidateStage(stage, errors);
                return errors;
            }

            var banner = data as BannerData;
            if (banner != null)
            {
                ValidateBanner(banner, errors);
                return errors;
            }

            var schedule = data as RotationScheduleData;
            if (schedule != null)
            {
                ValidateSchedule(schedule, errors);
                return errors;
            }

            var reward = data as RewardData;
            if (reward != null)
            {
                ValidateReward(reward, errors);
            }

            return errors;
        }

        private static void ValidateCharacter(CharacterData data, List<string> errors)
        {
            if (data.Element == ElementType.Heart)
            {
                errors.Add("Character element cannot be Heart.");
            }

            if (data.BaseRarity < (int)Rarity.One || data.BaseRarity > (int)Rarity.Five)
            {
                errors.Add("Character base rarity must be between 1 and 5.");
            }
        }

        private static void ValidateSkill(int chargeRequired, SkillEffectData[] effects, List<string> errors)
        {
            if (chargeRequired < 0)
            {
                errors.Add("Skill charge requirement cannot be negative.");
            }

            if (effects == null || effects.Length == 0)
            {
                errors.Add("Skill must define at least one effect.");
            }
        }

        private static void ValidateEnemy(EnemyData data, List<string> errors)
        {
            if (data.InitialCountdown < 1)
            {
                errors.Add("Enemy initial countdown must be at least 1.");
            }

            if (data.Actions == null || data.Actions.Length == 0)
            {
                errors.Add("Enemy must define at least one action.");
                return;
            }

            for (var index = 0; index < data.Actions.Length; index++)
            {
                var action = data.Actions[index];
                if (action == null || string.IsNullOrWhiteSpace(action.Id))
                {
                    errors.Add("Enemy action ID is required.");
                }
                else if (action.ResetCountdown < 1)
                {
                    errors.Add("Enemy action reset countdown must be at least 1.");
                }
            }
        }

        private static void ValidateStage(StageData data, List<string> errors)
        {
            if (data.Waves == null || data.Waves.Length == 0)
            {
                errors.Add("Stage must define at least one wave.");
            }
            else
            {
                for (var index = 0; index < data.Waves.Length; index++)
                {
                    var wave = data.Waves[index];
                    if (wave == null || string.IsNullOrWhiteSpace(wave.Id))
                    {
                        errors.Add("Wave ID is required.");
                    }
                }
            }

            if (data.StarObjectives == null || data.StarObjectives.Length != 3)
            {
                errors.Add("Stage must define exactly three star objectives.");
            }
        }

        private static void ValidateBanner(BannerData data, List<string> errors)
        {
            var requiresSteps = data.Type == BannerType.GatherIn || data.Type == BannerType.StepUp;

            if (data.Characters == null || data.Characters.Length == 0)
            {
                errors.Add("Banner must define at least one weighted character.");
            }
            else
            {
                for (var index = 0; index < data.Characters.Length; index++)
                {
                    var character = data.Characters[index];
                    if (character == null || string.IsNullOrWhiteSpace(character.CharacterId))
                    {
                        errors.Add("Banner character ID is required.");
                    }
                    else if (character.Weight <= 0)
                    {
                        errors.Add("Banner character weight must be positive.");
                    }
                }
            }

            if (data.SinglePullGemCost < 0 || data.TenPullGemCost < 0)
            {
                errors.Add("Banner gem costs cannot be negative.");
            }

            if (data.Steps == null)
            {
                errors.Add("Banner steps cannot be null.");
                return;
            }

            if (requiresSteps && data.Steps.Length == 0)
            {
                errors.Add(data.Type == BannerType.GatherIn
                    ? "Gather-In banner must define at least one step."
                    : "Step-Up banner must define at least one step.");
            }

            for (var index = 0; index < data.Steps.Length; index++)
            {
                var step = data.Steps[index];
                if (step == null || step.PullCount < 1)
                {
                    errors.Add("Banner step pull count must be at least 1.");
                }
                else if (step.GemCost < 0)
                {
                    errors.Add("Banner step gem cost cannot be negative.");
                }
            }

            if (data.Type != BannerType.GatherIn || data.Steps.Length == 0)
            {
                return;
            }

            var finalStep = data.Steps[data.Steps.Length - 1];
            if (finalStep == null)
            {
                return;
            }

            if (finalStep.GuaranteedFiveStarFeaturedBoost && finalStep.GuaranteedFeaturedFiveStar)
            {
                errors.Add("Gather-In final step can define only one guarantee.");
            }
            else if (!finalStep.GuaranteedFiveStarFeaturedBoost && !finalStep.GuaranteedFeaturedFiveStar)
            {
                errors.Add("Gather-In final step must define a guarantee.");
            }
        }

        private static void ValidateSchedule(RotationScheduleData data, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(data.ContentId))
            {
                errors.Add("Schedule content ID is required.");
            }

            if (data.End <= data.Start)
            {
                errors.Add("Schedule end must be after schedule start.");
            }

            if (data.StartMinuteOfDay < 0 || data.StartMinuteOfDay > 1439 ||
                data.EndMinuteOfDay < 0 || data.EndMinuteOfDay > 1439)
            {
                errors.Add("Schedule recurrence minutes must be between 0 and 1439.");
            }

            if (data.RecurringWeekdays == null)
            {
                errors.Add("Schedule recurring weekdays cannot be null.");
                return;
            }

            for (var index = 0; index < data.RecurringWeekdays.Length; index++)
            {
                if (data.RecurringWeekdays[index] < 0 || data.RecurringWeekdays[index] > 6)
                {
                    errors.Add("Schedule weekday must be between 0 and 6.");
                    break;
                }
            }
        }

        private static void ValidateReward(RewardData data, List<string> errors)
        {
            if (data.Amount <= 0)
            {
                errors.Add("Reward amount must be positive.");
            }

            if ((data.Type == RewardType.Character || data.Type == RewardType.Material || data.Type == RewardType.EventCurrency) &&
                string.IsNullOrWhiteSpace(data.ItemId))
            {
                errors.Add("Reward item ID is required for this reward type.");
            }
        }
    }
}
