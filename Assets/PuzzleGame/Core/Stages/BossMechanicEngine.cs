using System;
using System.Collections.Generic;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Stages
{
    public sealed class BossMechanicEngine
    {
        private readonly Dictionary<EnemyRuntime, HashSet<string>> triggeredEffects = new Dictionary<EnemyRuntime, HashSet<string>>();

        public IReadOnlyList<EnemyEffectSnapshot> Evaluate(EnemyRuntime enemy)
        {
            if (enemy == null) throw new ArgumentNullException("enemy");
            var triggered = new List<EnemyEffectSnapshot>();
            if (enemy.MaxHp <= 0) return triggered.AsReadOnly();
            HashSet<string> seen;
            if (!triggeredEffects.TryGetValue(enemy, out seen))
            {
                seen = new HashSet<string>(StringComparer.Ordinal);
                triggeredEffects.Add(enemy, seen);
            }
            var data = enemy.Data;
            for (var actionIndex = 0; actionIndex < data.Actions.Length; actionIndex++)
            for (var effectIndex = 0; effectIndex < data.Actions[actionIndex].Effects.Length; effectIndex++)
            {
                var effect = data.Actions[actionIndex].Effects[effectIndex];
                if (effect.Type != EnemyEffectType.Enrage || effect.Payload.Amount < 1 || effect.Payload.Amount > 100) continue;
                var identity = actionIndex + ":" + effectIndex;
                if (seen.Contains(identity) || (long)enemy.CurrentHp * 100L > (long)enemy.MaxHp * effect.Payload.Amount) continue;
                seen.Add(identity);
                var multiplier = Math.Max(enemy.EnrageTurns > 0 ? enemy.AttackMultiplier : 1f, effect.Payload.Multiplier);
                var turns = Math.Max(enemy.EnrageTurns, effect.Payload.TurnCount);
                enemy.SetEnrage(multiplier, turns);
                triggered.Add(new EnemyEffectSnapshot(effect));
            }
            return triggered.AsReadOnly();
        }
    }
}
