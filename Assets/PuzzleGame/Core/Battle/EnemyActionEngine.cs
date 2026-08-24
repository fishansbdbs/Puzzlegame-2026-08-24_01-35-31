using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Battle
{
    public sealed class EnemyEffectSnapshot
    {
        internal EnemyEffectSnapshot(EnemyEffectData effect)
        {
            Type = effect.Type;
            var payload = effect.Payload;
            Amount = payload.Amount;
            Multiplier = payload.Multiplier;
            DurationSeconds = payload.DurationSeconds;
            TurnCount = payload.TurnCount;
            ComboCount = payload.ComboCount;
            SourceOrb = payload.SourceOrb;
            TargetOrb = payload.TargetOrb;
            TargetId = payload.TargetId;
            Tags = Array.AsReadOnly(payload.Tags == null ? new string[0] : (string[])payload.Tags.Clone());
        }

        public EnemyEffectType Type { get; private set; }
        public int Amount { get; private set; }
        public float Multiplier { get; private set; }
        public float DurationSeconds { get; private set; }
        public int TurnCount { get; private set; }
        public int ComboCount { get; private set; }
        public OrbType SourceOrb { get; private set; }
        public OrbType TargetOrb { get; private set; }
        public string TargetId { get; private set; }
        public IReadOnlyList<string> Tags { get; private set; }
    }

    public sealed class EnemyActionSnapshot
    {
        internal EnemyActionSnapshot(EnemyActionData action)
        {
            Id = action.Id;
            ResetCountdown = action.ResetCountdown;
            var effects = new List<EnemyEffectSnapshot>(action.Effects.Length);
            for (var index = 0; index < action.Effects.Length; index++) effects.Add(new EnemyEffectSnapshot(action.Effects[index]));
            Effects = effects.AsReadOnly();
        }

        public string Id { get; private set; }
        public int ResetCountdown { get; private set; }
        public IReadOnlyList<EnemyEffectSnapshot> Effects { get; private set; }
    }

    public sealed class EnemyTurnResolution
    {
        internal EnemyTurnResolution(List<EnemyActionSnapshot> actions, List<BattleEffectEvent> events)
        {
            ExecutedActions = actions.AsReadOnly();
            Events = events.AsReadOnly();
        }

        public IReadOnlyList<EnemyActionSnapshot> ExecutedActions { get; private set; }
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
            var actions = new List<EnemyActionSnapshot>();
            var events = new List<BattleEffectEvent>();
            if (enemy.IsDefeated) return new EnemyTurnResolution(actions, events);

            var pendingAction = enemy.PeekNextAction();
            ValidateAction(pendingAction);
            var nextCountdown = enemy.Countdown > 0 ? enemy.Countdown - 1 : 0;
            if (nextCountdown > 0)
            {
                enemy.ModifyCountdown(-1);
                return new EnemyTurnResolution(actions, events);
            }

            var action = enemy.CommitNextAction();
            var actionSnapshot = new EnemyActionSnapshot(action);
            actions.Add(actionSnapshot);
            enemy.ResetCountdown(action.ResetCountdown);
            for (var index = 0; index < action.Effects.Length; index++)
                events.Add(Apply(action.Effects[index], action.Id, enemy, context));
            return new EnemyTurnResolution(actions, events);
        }

        private static void ValidateAction(EnemyActionData action)
        {
            for (var index = 0; index < action.Effects.Length; index++) ValidateEffect(action.Effects[index]);
        }

        private static void ValidateEffect(EnemyEffectData effect)
        {
            var payload = effect.Payload;
            SkillEngine.NonNegative(payload.TurnCount);
            switch (effect.Type)
            {
                case EnemyEffectType.Damage:
                case EnemyEffectType.Poison:
                case EnemyEffectType.Hazard:
                case EnemyEffectType.Blocker:
                case EnemyEffectType.Bind:
                    SkillEngine.NonNegative(payload.Amount);
                    break;
                case EnemyEffectType.ConvertOrbs:
                    SkillEngine.ValidateOrb(payload.SourceOrb);
                    SkillEngine.ValidateOrb(payload.TargetOrb);
                    break;
                case EnemyEffectType.LockOrbs:
                    SkillEngine.ValidateOrb(payload.SourceOrb);
                    break;
                case EnemyEffectType.ReduceMoveTime:
                    SkillEngine.Seconds(payload);
                    break;
                case EnemyEffectType.DamageAbsorb:
                    SkillEngine.ValidateOrb(payload.SourceOrb);
                    if (payload.SourceOrb == OrbType.Heart) throw new InvalidOperationException("Heart cannot be a damage absorb element.");
                    break;
                case EnemyEffectType.ComboShield:
                    SkillEngine.NonNegative(payload.Amount != 0 ? payload.Amount : payload.ComboCount);
                    break;
                case EnemyEffectType.Enrage:
                    SkillEngine.PositiveMultiplier(payload.Multiplier);
                    break;
                case EnemyEffectType.ManipulateCountdown:
                    break;
                default:
                    throw new InvalidOperationException("Unsupported enemy effect type.");
            }
        }

        private static BattleEffectEvent Apply(EnemyEffectData effect, string sourceId, EnemyRuntime enemy, BattleContext context)
        {
            var payload = effect.Payload;
            switch (effect.Type)
            {
                case EnemyEffectType.Damage:
                    var raw = CombatCalculator.RoundToStateInt((double)payload.Amount * enemy.AttackMultiplier);
                    return new BattleEffectEvent(BattleEffectKind.Damage, sourceId, context.Party.ApplyDamage(raw), enemy.AttackMultiplier, payload.TurnCount, 0);
                case EnemyEffectType.ConvertOrbs:
                    return Event(BattleEffectKind.ConvertOrbs, sourceId, payload, SkillEngine.ConvertAll(context.Board, payload.SourceOrb, payload.TargetOrb));
                case EnemyEffectType.LockOrbs:
                    context.BoardEffects.Lock(payload.SourceOrb, payload.TurnCount);
                    return Event(BattleEffectKind.LockOrbs, sourceId, payload, 0);
                case EnemyEffectType.Poison:
                    context.BoardEffects.AddPoison(payload.Amount, payload.TurnCount);
                    return Event(BattleEffectKind.Poison, sourceId, payload, payload.Amount);
                case EnemyEffectType.Hazard:
                    context.BoardEffects.AddHazard(payload.Amount, payload.TurnCount);
                    return Event(BattleEffectKind.Hazard, sourceId, payload, payload.Amount);
                case EnemyEffectType.Blocker:
                    context.BoardEffects.AddBlocker(payload.Amount, payload.TurnCount);
                    return Event(BattleEffectKind.Blocker, sourceId, payload, payload.Amount);
                case EnemyEffectType.Bind:
                    return Event(BattleEffectKind.Bind, sourceId, payload, context.Party.BindLowestSlots(payload.Amount, payload.TurnCount));
                case EnemyEffectType.ReduceMoveTime:
                    context.ModifyMoveTime(-SkillEngine.Seconds(payload), payload.TurnCount);
                    return Event(BattleEffectKind.ReduceMoveTime, sourceId, payload, 0);
                case EnemyEffectType.DamageAbsorb:
                    enemy.SetDamageAbsorb((ElementType)payload.SourceOrb, payload.TurnCount);
                    return Event(BattleEffectKind.DamageAbsorb, sourceId, payload, 0);
                case EnemyEffectType.ComboShield:
                    enemy.SetComboShield(payload.Amount != 0 ? payload.Amount : payload.ComboCount, payload.TurnCount);
                    return Event(BattleEffectKind.ComboShield, sourceId, payload, 0);
                case EnemyEffectType.Enrage:
                    enemy.SetEnrage(payload.Multiplier, payload.TurnCount);
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
