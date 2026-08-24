using System;
using System.Collections.Generic;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Stages
{
    public sealed class StageCatalog
    {
        private readonly Dictionary<string, StageData> stages;
        private readonly Dictionary<string, EnemyData> enemies;

        public StageCatalog(IEnumerable<StageData> stageRecords, IEnumerable<EnemyData> enemyRecords)
        {
            if (stageRecords == null) throw new ArgumentNullException("stageRecords");
            if (enemyRecords == null) throw new ArgumentNullException("enemyRecords");

            enemies = new Dictionary<string, EnemyData>(StringComparer.Ordinal);
            foreach (var enemy in enemyRecords)
            {
                StageValidation.ValidateEnemy(enemy);
                if (enemies.ContainsKey(enemy.Id)) throw new ArgumentException("Enemy IDs must be unique.", "enemyRecords");
                enemies.Add(enemy.Id, StageDataSnapshot.Clone(enemy));
            }

            stages = new Dictionary<string, StageData>(StringComparer.Ordinal);
            foreach (var stage in stageRecords)
            {
                StageValidation.ValidateStage(stage, enemies);
                if (stages.ContainsKey(stage.Id)) throw new ArgumentException("Stage IDs must be unique.", "stageRecords");
                stages.Add(stage.Id, StageDataSnapshot.Clone(stage));
            }
        }

        public StageData GetRequired(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Stage ID is required.", "id");
            StageData stage;
            if (!stages.TryGetValue(id, out stage)) throw new KeyNotFoundException("No stage exists with ID '" + id + "'.");
            return StageDataSnapshot.Clone(stage);
        }

        public StageSession CreateSession(string id)
        {
            var stage = GetRequired(id);
            var sessionEnemies = new List<EnemyData>(enemies.Count);
            foreach (var enemy in enemies.Values) sessionEnemies.Add(StageDataSnapshot.Clone(enemy));
            return new StageSession(stage, sessionEnemies);
        }
    }

    internal static class StageValidation
    {
        internal static void ValidateStage(StageData stage, IDictionary<string, EnemyData> knownEnemies)
        {
            if (stage == null) throw new ArgumentNullException("stage");
            if (string.IsNullOrWhiteSpace(stage.Id)) throw new ArgumentException("Stage ID is required.", "stage");
            if (stage.Waves == null || stage.Waves.Length == 0) throw new ArgumentException("Stage must define at least one wave.", "stage");
            if (stage.StarObjectives == null || stage.StarObjectives.Length != 3) throw new ArgumentException("Stage must define exactly three star objectives.", "stage");

            var waveIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < stage.Waves.Length; index++)
            {
                var wave = stage.Waves[index];
                if (wave == null || string.IsNullOrWhiteSpace(wave.Id)) throw new ArgumentException("Wave ID is required.", "stage");
                if (!waveIds.Add(wave.Id)) throw new ArgumentException("Wave IDs must be unique within a stage.", "stage");
                if (wave.EnemyIds == null || wave.EnemyIds.Length == 0) throw new ArgumentException("Wave must define at least one enemy.", "stage");
                for (var enemyIndex = 0; enemyIndex < wave.EnemyIds.Length; enemyIndex++)
                {
                    var enemyId = wave.EnemyIds[enemyIndex];
                    if (string.IsNullOrWhiteSpace(enemyId) || knownEnemies == null || !knownEnemies.ContainsKey(enemyId))
                        throw new ArgumentException("Wave references an unknown enemy.", "stage");
                }
            }

            for (var index = 0; index < stage.StarObjectives.Length; index++) ValidateObjective(stage.StarObjectives[index]);
        }

        internal static void ValidateObjective(StarObjectiveData objective)
        {
            if (objective == null) throw new ArgumentException("Star objectives cannot be null.", "objective");
            if (objective.Type < StarObjectiveType.Clear || objective.Type > StarObjectiveType.ClearWithinBoardResolutionCount)
                throw new ArgumentException("Star objective type is not defined.", "objective");
            if (objective.Type == StarObjectiveType.FinishAboveHpThreshold &&
                (float.IsNaN(objective.HpThresholdPercent) || float.IsInfinity(objective.HpThresholdPercent) || objective.HpThresholdPercent < 0f || objective.HpThresholdPercent > 1f))
                throw new ArgumentException("HP objective threshold must be between zero and one.", "objective");
            if (objective.Type == StarObjectiveType.ClearWithinBoardResolutionCount && objective.MaximumBoardResolutionCount < 0)
                throw new ArgumentException("Board resolution objective limit cannot be negative.", "objective");
        }

        internal static void ValidateEnemy(EnemyData enemy)
        {
            EnemyDataValidation.Validate(enemy);
        }
    }

    internal static class StageDataSnapshot
    {
        internal static StageData Clone(StageData source)
        {
            if (source == null) return null;
            var waves = source.Waves == null ? null : new WaveData[source.Waves.Length];
            if (waves != null) for (var index = 0; index < waves.Length; index++) waves[index] = Clone(source.Waves[index]);
            var objectives = source.StarObjectives == null ? null : new StarObjectiveData[source.StarObjectives.Length];
            if (objectives != null) for (var index = 0; index < objectives.Length; index++) objectives[index] = Clone(source.StarObjectives[index]);
            return new StageData { Id = source.Id, ChapterId = source.ChapterId, DisplayName = source.DisplayName, IsPermanentStory = source.IsPermanentStory,
                Waves = waves, StarObjectives = objectives, ClearRewards = Clone(source.ClearRewards) };
        }

        internal static EnemyData Clone(EnemyData source)
        {
            if (source == null) return null;
            var actions = source.Actions == null ? null : new EnemyActionData[source.Actions.Length];
            if (actions != null) for (var index = 0; index < actions.Length; index++) actions[index] = Clone(source.Actions[index]);
            var triggers = source.ThresholdTriggers == null ? null : new EnemyThresholdTriggerData[source.ThresholdTriggers.Length];
            if (triggers != null) for (var index = 0; index < triggers.Length; index++) triggers[index] = Clone(source.ThresholdTriggers[index]);
            return new EnemyData { Id = source.Id, DisplayName = source.DisplayName, Element = source.Element,
                BaseStats = source.BaseStats == null ? null : new StatBlock { Hp = source.BaseStats.Hp, Attack = source.BaseStats.Attack, Recovery = source.BaseStats.Recovery },
                InitialCountdown = source.InitialCountdown, Actions = actions, ThresholdTriggers = triggers, Visuals = source.Visuals == null ? null : new VisualReferenceSet { PortraitKey = source.Visuals.PortraitKey, CardArtKey = source.Visuals.CardArtKey, ModelKey = source.Visuals.ModelKey, VfxKey = source.Visuals.VfxKey } };
        }

        internal static WaveData Clone(WaveData source)
        {
            if (source == null) return null;
            return new WaveData { Id = source.Id, EnemyIds = source.EnemyIds == null ? null : (string[])source.EnemyIds.Clone(), Rewards = Clone(source.Rewards) };
        }

        internal static StarObjectiveData Clone(StarObjectiveData source)
        {
            return source == null ? null : new StarObjectiveData { Type = source.Type, HpThresholdPercent = source.HpThresholdPercent, MaximumBoardResolutionCount = source.MaximumBoardResolutionCount };
        }

        private static EnemyActionData Clone(EnemyActionData source)
        {
            if (source == null) return null;
            var effects = source.Effects == null ? null : new EnemyEffectData[source.Effects.Length];
            if (effects != null) for (var index = 0; index < effects.Length; index++) effects[index] = Clone(source.Effects[index]);
            return new EnemyActionData { Id = source.Id, ResetCountdown = source.ResetCountdown, Effects = effects };
        }

        private static EnemyEffectData Clone(EnemyEffectData source)
        {
            return source == null ? null : new EnemyEffectData { Type = source.Type, Payload = Clone(source.Payload) };
        }

        private static EnemyThresholdTriggerData Clone(EnemyThresholdTriggerData source)
        {
            return source == null ? null : new EnemyThresholdTriggerData { Id = source.Id, HpThresholdPercent = source.HpThresholdPercent, Effect = Clone(source.Effect) };
        }

        private static EffectPayloadData Clone(EffectPayloadData source)
        {
            return source == null ? null : new EffectPayloadData { Amount = source.Amount, Multiplier = source.Multiplier, DurationSeconds = source.DurationSeconds,
                TurnCount = source.TurnCount, ComboCount = source.ComboCount, SourceOrb = source.SourceOrb, TargetOrb = source.TargetOrb, TargetId = source.TargetId,
                Tags = source.Tags == null ? null : (string[])source.Tags.Clone() };
        }

        private static RewardData[] Clone(RewardData[] source)
        {
            if (source == null) return null;
            var results = new RewardData[source.Length];
            for (var index = 0; index < results.Length; index++)
            {
                var reward = source[index];
                results[index] = reward == null ? null : new RewardData { Id = reward.Id, Type = reward.Type, ItemId = reward.ItemId, Amount = reward.Amount };
            }
            return results;
        }
    }
}
