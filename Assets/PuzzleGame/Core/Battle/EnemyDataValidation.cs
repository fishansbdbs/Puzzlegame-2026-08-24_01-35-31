using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Battle
{
    internal static class EnemyDataValidation
    {
        internal static void Validate(EnemyData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrWhiteSpace(data.Id)) throw new ArgumentException("Enemy ID is required.", "data");
            EnemyRuntime.ValidateCombatElement(data.Element, "data");
            if (data.BaseStats == null || data.BaseStats.Hp < 0 || data.BaseStats.Attack < 0 || data.BaseStats.Recovery < 0)
                throw new ArgumentException("Enemy stats cannot be null or negative.", "data");
            if (data.InitialCountdown < 1) throw new ArgumentException("Enemy countdown must be at least one.", "data");
            if (data.Actions == null || data.Actions.Length == 0) throw new ArgumentException("Enemy must define actions.", "data");
            for (var index = 0; index < data.Actions.Length; index++) ValidateAction(data.Actions[index]);

            if (data.ThresholdTriggers == null) throw new ArgumentException("Enemy threshold triggers cannot be null.", "data");
            var triggerIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < data.ThresholdTriggers.Length; index++)
            {
                var trigger = data.ThresholdTriggers[index];
                ValidateThresholdTrigger(trigger);
                if (!triggerIds.Add(trigger.Id)) throw new ArgumentException("Enemy threshold trigger IDs must be unique.", "data");
            }
        }

        internal static void ValidateAction(EnemyActionData action)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.Id) || action.ResetCountdown < 1 || action.Effects == null)
                throw new ArgumentException("Enemy actions must be identified, reset to at least one, and define effects.", "action");
            for (var index = 0; index < action.Effects.Length; index++) ValidateEffect(action.Effects[index]);
        }

        internal static void ValidateThresholdTrigger(EnemyThresholdTriggerData trigger)
        {
            if (trigger == null || string.IsNullOrWhiteSpace(trigger.Id)) throw new ArgumentException("Enemy threshold trigger ID is required.", "trigger");
            if (float.IsNaN(trigger.HpThresholdPercent) || float.IsInfinity(trigger.HpThresholdPercent) ||
                trigger.HpThresholdPercent < 1f || trigger.HpThresholdPercent > 100f)
                throw new ArgumentException("Enemy HP threshold must be between one and one hundred percent.", "trigger");
            ValidateEffect(trigger.Effect);
            if (trigger.Effect.Type != EnemyEffectType.Enrage)
                throw new ArgumentException("Enemy threshold triggers must define an enrage effect.", "trigger");
            if (trigger.Effect.Payload.TurnCount < 1)
                throw new ArgumentException("Enemy threshold enrage duration must be at least one turn.", "trigger");
        }

        internal static void ValidateEffect(EnemyEffectData effect)
        {
            if (effect == null || effect.Payload == null || effect.Type < EnemyEffectType.Damage || effect.Type > EnemyEffectType.ManipulateCountdown)
                throw new ArgumentException("Enemy effects and payloads must be valid.", "effect");
            var payload = effect.Payload;
            NonNegative(payload.TurnCount, "Enemy effect turn count cannot be negative.");
            switch (effect.Type)
            {
                case EnemyEffectType.Damage:
                case EnemyEffectType.Poison:
                case EnemyEffectType.Hazard:
                case EnemyEffectType.Blocker:
                case EnemyEffectType.Bind:
                    NonNegative(payload.Amount, "Enemy effect amount cannot be negative.");
                    break;
                case EnemyEffectType.ConvertOrbs:
                    ValidateOrb(payload.SourceOrb);
                    ValidateOrb(payload.TargetOrb);
                    break;
                case EnemyEffectType.LockOrbs:
                    ValidateOrb(payload.SourceOrb);
                    break;
                case EnemyEffectType.ReduceMoveTime:
                    NonNegative(payload.Amount, "Enemy move-time amount cannot be negative.");
                    FiniteNonNegative(payload.DurationSeconds, "Enemy move-time duration must be finite and non-negative.");
                    break;
                case EnemyEffectType.DamageAbsorb:
                    ValidateOrb(payload.SourceOrb);
                    if (payload.SourceOrb == OrbType.Heart) throw new ArgumentException("Heart cannot be a damage absorb element.", "effect");
                    break;
                case EnemyEffectType.ComboShield:
                    NonNegative(payload.Amount, "Enemy combo shield amount cannot be negative.");
                    NonNegative(payload.ComboCount, "Enemy combo shield combo count cannot be negative.");
                    break;
                case EnemyEffectType.Enrage:
                    FiniteNonNegative(payload.Multiplier, "Enemy enrage multiplier must be finite and non-negative.");
                    break;
                case EnemyEffectType.ManipulateCountdown:
                    break;
                default:
                    throw new ArgumentException("Enemy effect type is not defined.", "effect");
            }
        }

        private static void ValidateOrb(OrbType orb)
        {
            if (orb < OrbType.Fire || orb > OrbType.Heart) throw new ArgumentException("Enemy effect orb type is not defined.", "effect");
        }

        private static void NonNegative(int value, string message)
        {
            if (value < 0) throw new ArgumentException(message, "effect");
        }

        private static void FiniteNonNegative(float value, string message)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f) throw new ArgumentException(message, "effect");
        }
    }
}
