using System;
using System.Collections.Generic;
using PuzzleGame.Core.Battle;

namespace PuzzleGame.Core.Stages
{
    public sealed class BossMechanicEngine
    {
        public IReadOnlyList<EnemyEffectSnapshot> Evaluate(EnemyRuntime enemy)
        {
            if (enemy == null) throw new ArgumentNullException("enemy");
            return enemy.EvaluateThresholds();
        }
    }
}
