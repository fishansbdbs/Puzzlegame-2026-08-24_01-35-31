using System;
using System.Collections.Generic;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Stages
{
    public sealed class BossMechanicEngine
    {
        public IReadOnlyList<EnemyEffectSnapshot> Evaluate(EnemyRuntime enemy)
        {
            if (enemy == null) throw new ArgumentNullException("enemy");
            var triggered = new List<EnemyEffectSnapshot>();
            if (enemy.MaxHp <= 0) return triggered.AsReadOnly();
            var data = enemy.Data;
            for (var index = 0; index < data.ThresholdTriggers.Length; index++)
            {
                var trigger = data.ThresholdTriggers[index];
                if ((double)enemy.CurrentHp * 100d > (double)enemy.MaxHp * trigger.HpThresholdPercent) continue;
                if (!enemy.TryConsumeThresholdTrigger(trigger.Id)) continue;
                var effect = trigger.Effect;
                var multiplier = Math.Max(enemy.EnrageTurns > 0 ? enemy.AttackMultiplier : 1f, effect.Payload.Multiplier);
                var turns = Math.Max(enemy.EnrageTurns, effect.Payload.TurnCount);
                enemy.SetEnrage(multiplier, turns);
                triggered.Add(new EnemyEffectSnapshot(effect));
            }
            return triggered.AsReadOnly();
        }
    }
}
