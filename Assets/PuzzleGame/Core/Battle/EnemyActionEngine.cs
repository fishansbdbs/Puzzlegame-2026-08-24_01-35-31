using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Battle
{
    public sealed class EnemyTurnResolution
    {
        internal EnemyTurnResolution(List<EnemyActionData> actions, List<BattleEffectEvent> events)
        {
            ExecutedActions = actions.AsReadOnly();
            Events = events.AsReadOnly();
        }

        public IReadOnlyList<EnemyActionData> ExecutedActions { get; private set; }
        public IReadOnlyList<BattleEffectEvent> Events { get; private set; }
    }

    public sealed class EnemyActionEngine
    {
        public EnemyTurnResolution AdvanceAfterBoardResolution(EnemyRuntime enemy, BattleContext context)
        {
            if (enemy == null) throw new ArgumentNullException("enemy");
            if (context == null) throw new ArgumentNullException("context");
            if (!object.ReferenceEquals(enemy, context.Enemy))
                throw new ArgumentException("Battle context must reference the enemy being advanced.", "context");
            var actions = new List<EnemyActionData>();
            var events = new List<BattleEffectEvent>();
            if (enemy.IsDefeated) return new EnemyTurnResolution(actions, events);

            if (enemy.Countdown > 0) enemy.ModifyCountdown(-1);
            if (enemy.Countdown > 0) return new EnemyTurnResolution(actions, events);

            var action = enemy.TakeNextAction();
            actions.Add(action);
            // Reset before effects so an action's own countdown manipulation has a lasting, bounded result.
            enemy.ResetCountdown(action.ResetCountdown);
            for (var index = 0; index < action.Effects.Length; index++)
                events.Add(Apply(action.Effects[index], action.Id, enemy, context));
            return new EnemyTurnResolution(actions, events);
        }

        private static BattleEffectEvent Apply(EnemyEffectData effect, string sourceId, EnemyRuntime enemy, BattleContext context)
        {
            var payload = effect.Payload;
            switch (effect.Type)
            {
                case EnemyEffectType.Damage:
                    var raw = CombatCalculator.RoundToStateInt(SkillEngine.NonNegative(payload.Amount) * enemy.AttackMultiplier);
                    return new BattleEffectEvent(BattleEffectKind.Damage, sourceId, context.Party.ApplyDamage(raw), enemy.AttackMultiplier, payload.TurnCount, 0);
                case EnemyEffectType.ConvertOrbs:
                    return Event(BattleEffectKind.ConvertOrbs, sourceId, payload,
                        SkillEngine.ConvertAll(context.Board, payload.SourceOrb, payload.TargetOrb));
                case EnemyEffectType.LockOrbs:
                    SkillEngine.ValidateOrb(payload.SourceOrb);
                    context.BoardEffects.Lock(payload.SourceOrb, SkillEngine.NonNegative(payload.TurnCount));
                    return Event(BattleEffectKind.LockOrbs, sourceId, payload, 0);
                case EnemyEffectType.Poison:
                    context.BoardEffects.AddPoison(SkillEngine.NonNegative(payload.Amount), SkillEngine.NonNegative(payload.TurnCount));
                    return Event(BattleEffectKind.Poison, sourceId, payload, payload.Amount);
                case EnemyEffectType.Hazard:
                    context.BoardEffects.AddHazard(SkillEngine.NonNegative(payload.Amount), SkillEngine.NonNegative(payload.TurnCount));
                    return Event(BattleEffectKind.Hazard, sourceId, payload, payload.Amount);
                case EnemyEffectType.Blocker:
                    context.BoardEffects.AddBlocker(SkillEngine.NonNegative(payload.Amount), SkillEngine.NonNegative(payload.TurnCount));
                    return Event(BattleEffectKind.Blocker, sourceId, payload, payload.Amount);
                case EnemyEffectType.Bind:
                    return Event(BattleEffectKind.Bind, sourceId, payload,
                        context.Party.BindLowestSlots(SkillEngine.NonNegative(payload.Amount), SkillEngine.NonNegative(payload.TurnCount)));
                case EnemyEffectType.ReduceMoveTime:
                    context.ModifyMoveTime(-SkillEngine.Seconds(payload), SkillEngine.NonNegative(payload.TurnCount));
                    return Event(BattleEffectKind.ReduceMoveTime, sourceId, payload, 0);
                case EnemyEffectType.DamageAbsorb:
                    SkillEngine.ValidateOrb(payload.SourceOrb);
                    if (payload.SourceOrb == OrbType.Heart) throw new InvalidOperationException("Heart cannot be a damage absorb element.");
                    enemy.SetDamageAbsorb((ElementType)payload.SourceOrb, SkillEngine.NonNegative(payload.TurnCount));
                    return Event(BattleEffectKind.DamageAbsorb, sourceId, payload, 0);
                case EnemyEffectType.ComboShield:
                    enemy.SetComboShield(SkillEngine.NonNegative(payload.Amount != 0 ? payload.Amount : payload.ComboCount),
                        SkillEngine.NonNegative(payload.TurnCount));
                    return Event(BattleEffectKind.ComboShield, sourceId, payload, 0);
                case EnemyEffectType.Enrage:
                    enemy.SetEnrage(SkillEngine.PositiveMultiplier(payload.Multiplier), SkillEngine.NonNegative(payload.TurnCount));
                    return Event(BattleEffectKind.Enrage, sourceId, payload, 0);
                case EnemyEffectType.ManipulateCountdown:
                    enemy.ModifyCountdown(payload.Amount);
                    return Event(BattleEffectKind.ManipulateCountdown, sourceId, payload, 0);
                default:
                    throw new InvalidOperationException("Unsupported enemy effect type.");
            }
        }

        private static BattleEffectEvent Event(BattleEffectKind kind, string sourceId, EffectPayloadData payload, int affected)
        {
            return new BattleEffectEvent(kind, sourceId, payload.Amount, payload.Multiplier, payload.TurnCount, affected);
        }
    }
}
