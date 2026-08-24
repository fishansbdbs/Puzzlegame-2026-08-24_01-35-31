using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Stages
{
    public sealed class StageResult
    {
        public StageResult(bool isCleared, float finalHpRatio, int boardResolutionCount)
        {
            if (float.IsNaN(finalHpRatio) || float.IsInfinity(finalHpRatio) || finalHpRatio < 0f || finalHpRatio > 1f)
                throw new ArgumentOutOfRangeException("finalHpRatio");
            if (boardResolutionCount < 0) throw new ArgumentOutOfRangeException("boardResolutionCount");
            IsCleared = isCleared;
            FinalHpRatio = finalHpRatio;
            BoardResolutionCount = boardResolutionCount;
        }

        public bool IsCleared { get; private set; }
        public float FinalHpRatio { get; private set; }
        public int BoardResolutionCount { get; private set; }
    }

    public sealed class StarResult
    {
        internal StarResult(StarObjectiveData objective, bool earned)
        {
            Objective = StageDataSnapshot.Clone(objective);
            Earned = earned;
        }

        public StarObjectiveData Objective { get { return StageDataSnapshot.Clone(ObjectiveData); } private set { ObjectiveData = value; } }
        public bool Earned { get; private set; }
        private StarObjectiveData ObjectiveData { get; set; }
    }

    public static class StageObjectiveEvaluator
    {
        public static IReadOnlyList<StarResult> Evaluate(StageData stage, StageResult result)
        {
            if (stage == null) throw new ArgumentNullException("stage");
            if (result == null) throw new ArgumentNullException("result");
            if (stage.StarObjectives == null || stage.StarObjectives.Length != 3) throw new ArgumentException("Stage must define exactly three star objectives.", "stage");
            var stars = new List<StarResult>(stage.StarObjectives.Length);
            for (var index = 0; index < stage.StarObjectives.Length; index++)
            {
                var objective = stage.StarObjectives[index];
                StageValidation.ValidateObjective(objective);
                stars.Add(new StarResult(objective, IsEarned(objective, result)));
            }
            return stars.AsReadOnly();
        }

        private static bool IsEarned(StarObjectiveData objective, StageResult result)
        {
            if (!result.IsCleared) return false;
            switch (objective.Type)
            {
                case StarObjectiveType.Clear: return true;
                case StarObjectiveType.FinishAboveHpThreshold: return result.FinalHpRatio >= objective.HpThresholdPercent;
                case StarObjectiveType.ClearWithinBoardResolutionCount: return result.BoardResolutionCount <= objective.MaximumBoardResolutionCount;
                default: throw new InvalidOperationException("Unsupported star objective type.");
            }
        }
    }
}
