using System;
using System.Collections.Generic;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Battle
{
    public enum BattleEffectKind
    {
        ConvertOrbs,
        CreateOrbs,
        RemoveOrbs,
        AttackBoost,
        Heal,
        ExtendMoveTime,
        DelayEnemies,
        ManipulateCountdown,
        Shield,
        DirectDamage,
        Damage,
        LockOrbs,
        Poison,
        Hazard,
        Blocker,
        Bind,
        ReduceMoveTime,
        DamageAbsorb,
        ComboShield,
        Enrage
    }

    public sealed class BattleEffectEvent
    {
        internal BattleEffectEvent(BattleEffectKind kind, string sourceId, int amount, float multiplier, int turns, int affectedCount)
        {
            Kind = kind;
            SourceId = sourceId ?? string.Empty;
            Amount = amount;
            Multiplier = multiplier;
            Turns = turns;
            AffectedCount = affectedCount;
        }

        public BattleEffectKind Kind { get; private set; }
        public string SourceId { get; private set; }
        public int Amount { get; private set; }
        public float Multiplier { get; private set; }
        public int Turns { get; private set; }
        public int AffectedCount { get; private set; }
    }

    public enum SkillActivationFailure
    {
        None,
        NoSkill,
        NotCharged,
        Bound
    }

    public sealed class SkillResolution
    {
        internal SkillResolution(bool succeeded, SkillActivationFailure failureReason, List<BattleEffectEvent> events)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Events = events.AsReadOnly();
        }

        public bool Succeeded { get; private set; }
        public SkillActivationFailure FailureReason { get; private set; }
        public IReadOnlyList<BattleEffectEvent> Events { get; private set; }
    }

    public sealed class SkillChargeEvent
    {
        internal SkillChargeEvent(int characterSlot, string characterId, int amount)
        {
            CharacterSlot = characterSlot;
            CharacterId = characterId;
            Amount = amount;
        }

        public int CharacterSlot { get; private set; }
        public string CharacterId { get; private set; }
        public int Amount { get; private set; }
    }

    public sealed class SkillChargeResolution
    {
        internal SkillChargeResolution(List<SkillChargeEvent> events)
        {
            Events = events.AsReadOnly();
        }

        public IReadOnlyList<SkillChargeEvent> Events { get; private set; }
    }

    public sealed class SkillEngine
    {
        public bool CanActivate(CharacterRuntime character)
        {
            if (character == null) throw new ArgumentNullException("character");
            return character.ActiveSkillState != null && !character.IsBound && character.CurrentCharge >= character.ActiveSkillState.ChargeRequired;
        }

        public SkillResolution Activate(CharacterRuntime character, BattleContext context)
        {
            if (character == null) throw new ArgumentNullException("character");
            if (context == null) throw new ArgumentNullException("context");
            if (character.ActiveSkillState == null) return Failed(SkillActivationFailure.NoSkill);
            if (character.IsBound) return Failed(SkillActivationFailure.Bound);
            if (character.CurrentCharge < character.ActiveSkillState.ChargeRequired) return Failed(SkillActivationFailure.NotCharged);

            var events = new List<BattleEffectEvent>();
            var effects = character.ActiveSkillState.Effects;
            for (var index = 0; index < effects.Length; index++) ValidateEffect(effects[index]);
            for (var index = 0; index < effects.Length; index++)
                events.Add(Apply(effects[index], character.ActiveSkillState.Id, context));
            character.ConsumeCharge();
            return new SkillResolution(true, SkillActivationFailure.None, events);
        }

        public SkillChargeResolution ChargeFrom(BoardResolution resolution, PartyState party)
        {
            if (resolution == null) throw new ArgumentNullException("resolution");
            if (party == null) throw new ArgumentNullException("party");
            var counts = new int[6];
            for (var layerIndex = 0; layerIndex < resolution.CascadeLayers.Count; layerIndex++)
            {
                var groups = resolution.CascadeLayers[layerIndex].Groups;
                for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++) counts[(int)groups[groupIndex].OrbType]++;
            }
            var events = new List<SkillChargeEvent>();
            for (var slot = 0; slot < party.Members.Count; slot++)
            {
                var character = party.Members[slot];
                if (character.ActiveSkillState == null || character.IsBound) continue;
                var amount = character.AddCharge(counts[(int)character.ActiveSkillState.ChargeElement]);
                if (amount > 0) events.Add(new SkillChargeEvent(slot, character.Id, amount));
            }
            return new SkillChargeResolution(events);
        }

        private static SkillResolution Failed(SkillActivationFailure reason)
        {
            return new SkillResolution(false, reason, new List<BattleEffectEvent>());
        }

        private static void ValidateEffect(SkillEffectData effect)
        {
            var payload = effect.Payload;
            NonNegative(payload.TurnCount);
            switch (effect.Type)
            {
                case SkillEffectType.ConvertOrbs:
                case SkillEffectType.RemoveOrbs:
                    ValidateOrb(payload.SourceOrb);
                    ValidateOrb(payload.TargetOrb);
                    break;
                case SkillEffectType.CreateOrbs:
                    ValidateOrb(payload.TargetOrb);
                    NonNegative(payload.Amount);
                    break;
                case SkillEffectType.AttackBoost:
                    PositiveMultiplier(payload.Multiplier);
                    break;
                case SkillEffectType.Heal:
                case SkillEffectType.DirectDamage:
                    NonNegative(payload.Amount);
                    break;
                case SkillEffectType.ExtendMoveTime:
                    Seconds(payload);
                    break;
                case SkillEffectType.DelayEnemies:
                    NonNegative(payload.Amount != 0 ? payload.Amount : payload.TurnCount);
                    break;
                case SkillEffectType.ManipulateCountdown:
                    break;
                case SkillEffectType.Shield:
                    var multiplier = PositiveMultiplier(payload.Multiplier);
                    if (multiplier > 1f) throw new InvalidOperationException("Shield damage multiplier cannot exceed one.");
                    break;
                default:
                    throw new InvalidOperationException("Unsupported skill effect type.");
            }
        }

        private static BattleEffectEvent Apply(SkillEffectData effect, string sourceId, BattleContext context)
        {
            var payload = effect.Payload;
            switch (effect.Type)
            {
                case SkillEffectType.ConvertOrbs:
                    return OrbEvent(BattleEffectKind.ConvertOrbs, sourceId, payload, ConvertAll(context.Board, payload.SourceOrb, payload.TargetOrb));
                case SkillEffectType.CreateOrbs:
                    return OrbEvent(BattleEffectKind.CreateOrbs, sourceId, payload, Create(context.Board, payload.TargetOrb, payload.Amount));
                case SkillEffectType.RemoveOrbs:
                    return OrbEvent(BattleEffectKind.RemoveOrbs, sourceId, payload, ConvertAll(context.Board, payload.SourceOrb, payload.TargetOrb));
                case SkillEffectType.AttackBoost:
                    context.Party.ApplyAttackBoost(PositiveMultiplier(payload.Multiplier), NonNegative(payload.TurnCount), sourceId);
                    return Event(BattleEffectKind.AttackBoost, sourceId, payload, 0);
                case SkillEffectType.Heal:
                    return new BattleEffectEvent(BattleEffectKind.Heal, sourceId, context.Party.Heal(NonNegative(payload.Amount)), payload.Multiplier, payload.TurnCount, 0);
                case SkillEffectType.ExtendMoveTime:
                    context.ModifyMoveTime(Seconds(payload), NonNegative(payload.TurnCount));
                    return Event(BattleEffectKind.ExtendMoveTime, sourceId, payload, 0);
                case SkillEffectType.DelayEnemies:
                    context.Enemy.ModifyCountdown(NonNegative(payload.Amount != 0 ? payload.Amount : payload.TurnCount));
                    return Event(BattleEffectKind.DelayEnemies, sourceId, payload, 0);
                case SkillEffectType.ManipulateCountdown:
                    context.Enemy.ModifyCountdown(payload.Amount);
                    return Event(BattleEffectKind.ManipulateCountdown, sourceId, payload, 0);
                case SkillEffectType.Shield:
                    context.Party.ApplyShield(PositiveMultiplier(payload.Multiplier), NonNegative(payload.TurnCount));
                    return Event(BattleEffectKind.Shield, sourceId, payload, 0);
                case SkillEffectType.DirectDamage:
                    return new BattleEffectEvent(BattleEffectKind.DirectDamage, sourceId, context.Enemy.ApplyDamage(NonNegative(payload.Amount)), payload.Multiplier, payload.TurnCount, 0);
                default:
                    throw new InvalidOperationException("Unsupported skill effect type.");
            }
        }

        internal static int ConvertAll(BoardState board, OrbType source, OrbType target)
        {
            ValidateOrb(source);
            ValidateOrb(target);
            var affected = 0;
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
                if (board.Get(x, y) == source && source != target) { board.Set(x, y, target); affected++; }
            return affected;
        }

        private static int Create(BoardState board, OrbType target, int amount)
        {
            ValidateOrb(target);
            amount = NonNegative(amount);
            var affected = 0;
            for (var y = 0; y < BoardState.Rows && affected < amount; y++)
            for (var x = 0; x < BoardState.Columns && affected < amount; x++)
                if (board.Get(x, y) != target) { board.Set(x, y, target); affected++; }
            return affected;
        }

        private static BattleEffectEvent OrbEvent(BattleEffectKind kind, string sourceId, EffectPayloadData payload, int count)
        {
            return new BattleEffectEvent(kind, sourceId, payload.Amount, payload.Multiplier, payload.TurnCount, count);
        }

        private static BattleEffectEvent Event(BattleEffectKind kind, string sourceId, EffectPayloadData payload, int count)
        {
            return new BattleEffectEvent(kind, sourceId, payload.Amount, payload.Multiplier, payload.TurnCount, count);
        }

        internal static int NonNegative(int value)
        {
            if (value < 0) throw new InvalidOperationException("Effect amount and duration cannot be negative for this effect.");
            return value;
        }

        internal static float PositiveMultiplier(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new InvalidOperationException("Effect multiplier must be finite and non-negative.");
            return value;
        }

        internal static float Seconds(EffectPayloadData payload)
        {
            var value = payload.DurationSeconds != 0f ? payload.DurationSeconds : payload.Amount;
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new InvalidOperationException("Effect duration must be finite and non-negative.");
            return value;
        }

        internal static void ValidateOrb(OrbType orb)
        {
            if (orb < OrbType.Fire || orb > OrbType.Heart) throw new InvalidOperationException("Effect orb type is not defined.");
        }
    }
}
