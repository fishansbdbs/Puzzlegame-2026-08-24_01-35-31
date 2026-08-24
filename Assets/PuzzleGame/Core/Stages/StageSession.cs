using System;
using System.Collections.Generic;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Stages
{
    public sealed class StageStructure
    {
        private readonly string[][] waveEnemyIds;

        internal StageStructure(StageData stage)
        {
            waveEnemyIds = new string[stage.Waves.Length][];
            for (var waveIndex = 0; waveIndex < stage.Waves.Length; waveIndex++)
                waveEnemyIds[waveIndex] = (string[])stage.Waves[waveIndex].EnemyIds.Clone();
        }

        public int WaveCount { get { return waveEnemyIds.Length; } }

        public int GetEnemyCount(int waveIndex)
        {
            ValidateWaveIndex(waveIndex);
            return waveEnemyIds[waveIndex].Length;
        }

        public string GetEnemyId(int waveIndex, int enemyIndex)
        {
            ValidateWaveIndex(waveIndex);
            if (enemyIndex < 0 || enemyIndex >= waveEnemyIds[waveIndex].Length)
                throw new ArgumentOutOfRangeException("enemyIndex");
            return waveEnemyIds[waveIndex][enemyIndex];
        }

        private void ValidateWaveIndex(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= waveEnemyIds.Length)
                throw new ArgumentOutOfRangeException("waveIndex");
        }
    }

    public sealed class StageSession
    {
        private readonly StageData stage;
        private readonly Dictionary<string, EnemyData> enemyTemplates;
        private readonly HashSet<BattleTurnResolution> recordedResolutions = new HashSet<BattleTurnResolution>();
        private List<EnemyRuntime> currentEnemies;

        public StageSession(StageData stageData, IEnumerable<EnemyData> enemies)
        {
            if (stageData == null) throw new ArgumentNullException("stageData");
            if (enemies == null) throw new ArgumentNullException("enemies");
            enemyTemplates = new Dictionary<string, EnemyData>(StringComparer.Ordinal);
            foreach (var enemy in enemies)
            {
                StageValidation.ValidateEnemy(enemy);
                if (enemyTemplates.ContainsKey(enemy.Id)) throw new ArgumentException("Enemy IDs must be unique.", "enemies");
                enemyTemplates.Add(enemy.Id, StageDataSnapshot.Clone(enemy));
            }
            StageValidation.ValidateStage(stageData, enemyTemplates);
            stage = StageDataSnapshot.Clone(stageData);
            AuthoredStructure = new StageStructure(stage);
        }

        public int CurrentWaveIndex { get; private set; }
        public int BoardResolutionCount { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool HasLoadedCurrentWave { get { return currentEnemies != null; } }
        public bool ConsumesStamina { get { return !stage.IsPermanentStory; } }
        public StageStructure AuthoredStructure { get; private set; }

        public WaveData CurrentWave
        {
            get
            {
                EnsureCurrentWaveLoaded();
                return StageDataSnapshot.Clone(stage.Waves[CurrentWaveIndex]);
            }
        }

        public IReadOnlyList<EnemyRuntime> CurrentEnemies
        {
            get
            {
                EnsureCurrentWaveLoaded();
                return currentEnemies.AsReadOnly();
            }
        }

        public bool TryAdvanceWave()
        {
            EnsureActive();
            EnsureCurrentWaveLoaded();
            for (var index = 0; index < currentEnemies.Count; index++) if (!currentEnemies[index].IsDefeated) return false;
            if (CurrentWaveIndex == stage.Waves.Length - 1)
            {
                IsCompleted = true;
                currentEnemies = null;
                return true;
            }
            CurrentWaveIndex++;
            currentEnemies = null;
            return true;
        }

        public void AdvanceWave()
        {
            if (!TryAdvanceWave()) throw new InvalidOperationException("The current wave must be defeated before advancing.");
        }

        public BattleTurnResolution CompleteBoardResolution(BattleEngine engine, BoardResolution resolution, BattleContext context)
        {
            EnsureActive();
            if (engine == null) throw new ArgumentNullException("engine");
            if (resolution == null) throw new ArgumentNullException("resolution");
            if (context == null) throw new ArgumentNullException("context");
            EnsureCurrentWaveLoaded();
            var found = false;
            for (var index = 0; index < currentEnemies.Count; index++) if (object.ReferenceEquals(currentEnemies[index], context.Enemy)) found = true;
            if (!found) throw new ArgumentException("Battle context must use an enemy in the current wave.", "context");
            var result = engine.CompleteBoardResolution(resolution, context);
            RecordBoardResolution(result);
            return result;
        }

        public void RecordBoardResolution(BattleTurnResolution resolution)
        {
            EnsureActive();
            if (resolution == null) throw new ArgumentNullException("resolution");
            if (recordedResolutions.Add(resolution) && !resolution.WasSkippedBecausePartyDefeated)
                BoardResolutionCount = BoardResolutionCount == int.MaxValue ? int.MaxValue : BoardResolutionCount + 1;
        }

        private void EnsureCurrentWaveLoaded()
        {
            EnsureActive();
            if (currentEnemies != null) return;
            var wave = stage.Waves[CurrentWaveIndex];
            currentEnemies = new List<EnemyRuntime>(wave.EnemyIds.Length);
            for (var index = 0; index < wave.EnemyIds.Length; index++) currentEnemies.Add(new EnemyRuntime(StageDataSnapshot.Clone(enemyTemplates[wave.EnemyIds[index]])));
        }

        private void EnsureActive()
        {
            if (IsCompleted) throw new InvalidOperationException("The stage session is complete.");
        }
    }
}
