using System;
using System.Collections.Generic;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Progression;

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
                ValidateSkill(skill.ChargeElement, skill.ChargeRequired, skill.Effects, errors);
                return errors;
            }

            var leaderSkill = data as LeaderSkillData;
            if (leaderSkill != null)
            {
                ValidateSkill(ElementType.Fire, 0, leaderSkill.Effects, errors);
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

            var eventData = data as EventData;
            if (eventData != null)
            {
                ValidateEvent(eventData, errors);
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
            errors.AddRange(ProgressionValidation.Validate(data));
        }

        private static void ValidateSkill(ElementType chargeElement, int chargeRequired, SkillEffectData[] effects, List<string> errors)
        {
            if (!Enum.IsDefined(typeof(ElementType), chargeElement)) errors.Add("Skill charge element is invalid.");
            if (chargeRequired < 0)
            {
                errors.Add("Skill charge requirement cannot be negative.");
            }

            if (effects == null || effects.Length == 0)
            {
                errors.Add("Skill must define at least one effect.");
            }
            else for (var index = 0; index < effects.Length; index++) ValidateSkillEffect(effects[index], errors);
        }

        private static void ValidateEnemy(EnemyData data, List<string> errors)
        {
            try { EnemyDataValidation.Validate(data); }
            catch (ArgumentException exception) { errors.Add(exception.Message); }
        }

        private static void ValidateStage(StageData data, List<string> errors)
        {
            if (data.Waves == null || data.Waves.Length == 0)
            {
                errors.Add("Stage must define at least one wave.");
            }
            else
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < data.Waves.Length; index++)
                {
                    var wave = data.Waves[index];
                    if (wave == null || string.IsNullOrWhiteSpace(wave.Id))
                    {
                        errors.Add("Wave ID is required.");
                    }
                    else if (!ids.Add(wave.Id)) errors.Add("Wave IDs must be unique within a stage.");
                    if (wave != null && (wave.EnemyIds == null || wave.EnemyIds.Length == 0)) errors.Add("Wave must define at least one enemy.");
                    else if (wave != null) for (var enemy = 0; enemy < wave.EnemyIds.Length; enemy++) if (string.IsNullOrWhiteSpace(wave.EnemyIds[enemy])) errors.Add("Wave enemy ID is required.");
                }
            }
            if (data.StarObjectives == null || data.StarObjectives.Length != 3)
            {
                errors.Add("Stage must define exactly three star objectives.");
            }
            else for (var index = 0; index < data.StarObjectives.Length; index++) ValidateObjective(data.StarObjectives[index], errors);
            ValidateRewards(data.ClearRewards, errors);
        }

        private static void ValidateBanner(BannerData data, List<string> errors)
        {
            if (!Enum.IsDefined(typeof(BannerType), data.Type)) errors.Add("Banner type is invalid.");
            var requiresSteps = data.Type == BannerType.GatherIn || data.Type == BannerType.StepUp;

            if (data.Characters == null || data.Characters.Length == 0)
            {
                errors.Add("Banner must define at least one weighted character.");
            }
            else
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
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
                    else if (!ids.Add(character.CharacterId)) errors.Add("Banner character IDs must be unique.");
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

            if (!Enum.IsDefined(typeof(ContentType), data.ContentType))
            {
                errors.Add("Schedule content type is invalid.");
            }

            if (data.Priority < 0)
            {
                errors.Add("Schedule priority cannot be negative.");
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

            var weekdays = new HashSet<int>();
            for (var index = 0; index < data.RecurringWeekdays.Length; index++)
            {
                if (data.RecurringWeekdays[index] < 0 || data.RecurringWeekdays[index] > 6)
                {
                    errors.Add("Schedule weekday must be between 0 and 6.");
                    break;
                }

                if (!weekdays.Add(data.RecurringWeekdays[index]))
                {
                    errors.Add("Schedule recurring weekdays cannot contain duplicates.");
                    break;
                }
            }
        }

        private static void ValidateReward(RewardData data, List<string> errors)
        {
            if (!Enum.IsDefined(typeof(RewardType), data.Type)) errors.Add("Reward type is invalid.");
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

        private static void ValidateEvent(EventData data, List<string> errors)
        {
            if (data.StageIds == null || data.StageIds.Length == 0) errors.Add("Event must define stage IDs.");
            else
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < data.StageIds.Length; index++)
                    if (string.IsNullOrWhiteSpace(data.StageIds[index])) errors.Add("Event stage ID is required.");
                    else if (!ids.Add(data.StageIds[index])) errors.Add("Event stage IDs must be unique.");
            }
            ValidateRewards(data.MilestoneRewards, errors);
        }

        private static void ValidateRewards(RewardData[] rewards, List<string> errors)
        {
            if (rewards == null) { errors.Add("Rewards cannot be null."); return; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < rewards.Length; index++)
            {
                var reward = rewards[index];
                if (reward == null || string.IsNullOrWhiteSpace(reward.Id)) errors.Add("Reward ID is required.");
                else if (!ids.Add(reward.Id)) errors.Add("Reward IDs must be unique.");
                if (reward != null) ValidateReward(reward, errors);
            }
        }

        private static void ValidateObjective(StarObjectiveData objective, List<string> errors)
        {
            if (objective == null) { errors.Add("Star objectives cannot be null."); return; }
            if (!Enum.IsDefined(typeof(StarObjectiveType), objective.Type)) errors.Add("Star objective type is invalid.");
            if (objective.Type == StarObjectiveType.FinishAboveHpThreshold && (float.IsNaN(objective.HpThresholdPercent) || float.IsInfinity(objective.HpThresholdPercent) || objective.HpThresholdPercent < 0f || objective.HpThresholdPercent > 1f)) errors.Add("HP objective threshold must be between zero and one.");
            if (objective.Type == StarObjectiveType.ClearWithinBoardResolutionCount && objective.MaximumBoardResolutionCount < 0) errors.Add("Board resolution objective limit cannot be negative.");
        }

        private static void ValidateSkillEffect(SkillEffectData effect, List<string> errors)
        {
            if (effect == null || effect.Payload == null) { errors.Add("Skill effects and payloads cannot be null."); return; }
            if (!Enum.IsDefined(typeof(SkillEffectType), effect.Type)) { errors.Add("Skill effect type is invalid."); return; }
            var payload = effect.Payload;
            if (payload.TurnCount < 0) errors.Add("Skill effect turn count cannot be negative.");
            if (float.IsNaN(payload.Multiplier) || float.IsInfinity(payload.Multiplier) || float.IsNaN(payload.DurationSeconds) || float.IsInfinity(payload.DurationSeconds)) errors.Add("Skill effect numeric payloads must be finite.");
            if ((effect.Type == SkillEffectType.ConvertOrbs || effect.Type == SkillEffectType.RemoveOrbs) && (!Enum.IsDefined(typeof(OrbType), payload.SourceOrb) || !Enum.IsDefined(typeof(OrbType), payload.TargetOrb))) errors.Add("Skill effect orb type is invalid.");
            if (effect.Type == SkillEffectType.CreateOrbs && (!Enum.IsDefined(typeof(OrbType), payload.TargetOrb) || payload.Amount < 0)) errors.Add("Create orb effect is invalid.");
            if ((effect.Type == SkillEffectType.Heal || effect.Type == SkillEffectType.DirectDamage) && payload.Amount < 0) errors.Add("Skill effect amount cannot be negative.");
            if ((effect.Type == SkillEffectType.AttackBoost || effect.Type == SkillEffectType.Shield) && (payload.Multiplier < 0f || (effect.Type == SkillEffectType.Shield && payload.Multiplier > 1f))) errors.Add("Skill effect multiplier is invalid.");
        }
    }
}
