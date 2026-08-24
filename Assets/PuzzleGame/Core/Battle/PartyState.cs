using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Battle
{
    public sealed class PartyState
    {
        public const int RequiredMemberCount = 5;
        private readonly IReadOnlyList<CharacterRuntime> members;
        private object attackBoostIdentity = new object();
        private object shieldIdentity = new object();

        public PartyState(IEnumerable<CharacterRuntime> members)
        {
            if (members == null) throw new ArgumentNullException("members");
            var copy = new List<CharacterRuntime>(members);
            if (copy.Count != RequiredMemberCount) throw new ArgumentException("Party must contain exactly five characters.", "members");
            for (var index = 0; index < copy.Count; index++)
                if (copy[index] == null) throw new ArgumentException("Party characters cannot be null.", "members");

            this.members = copy.AsReadOnly();
            long hp = 0;
            long recovery = 0;
            for (var index = 0; index < copy.Count; index++)
            {
                hp += copy[index].MaxHp;
                recovery += copy[index].Recovery;
            }

            MaxHp = Saturate(hp);
            TotalRecovery = Saturate(recovery);
            CurrentHp = MaxHp;
            AttackMultiplier = 1f;
            DamageTakenMultiplier = 1f;
        }

        public IReadOnlyList<CharacterRuntime> Members { get { return members; } }
        public CharacterRuntime Leader { get { return members[0]; } }
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int TotalRecovery { get; private set; }
        public float HpPercent { get { return MaxHp == 0 ? 0f : CurrentHp / (float)MaxHp; } }
        public float AttackMultiplier { get; private set; }
        public int AttackBoostTurns { get; private set; }
        public string AttackBoostSourceId { get; private set; }
        public float DamageTakenMultiplier { get; private set; }
        public int ShieldTurns { get; private set; }

        public int ApplyDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount");
            var scaled = CombatCalculator.RoundToStateInt((double)amount * DamageTakenMultiplier);
            var applied = Math.Min(CurrentHp, scaled);
            CurrentHp -= applied;
            return applied;
        }

        public int Heal(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount");
            var applied = (int)Math.Min((long)amount, (long)MaxHp - CurrentHp);
            CurrentHp += applied;
            return applied;
        }

        public void ApplyAttackBoost(float multiplier, int turns, string sourceId)
        {
            ValidateMultiplier(multiplier, "multiplier");
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            if (turns == 0) return;
            AttackMultiplier = multiplier;
            AttackBoostTurns = turns;
            AttackBoostSourceId = sourceId ?? string.Empty;
            attackBoostIdentity = new object();
        }

        public void ApplyShield(float damageTakenMultiplier, int turns)
        {
            ValidateMultiplier(damageTakenMultiplier, "damageTakenMultiplier");
            if (damageTakenMultiplier > 1f) throw new ArgumentOutOfRangeException("damageTakenMultiplier");
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            if (turns == 0) return;
            DamageTakenMultiplier = damageTakenMultiplier;
            ShieldTurns = turns;
            shieldIdentity = new object();
        }

        public int BindLowestSlots(int count, int turns)
        {
            if (count < 0) throw new ArgumentOutOfRangeException("count");
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            var affected = Math.Min(count, members.Count);
            for (var index = 0; index < affected; index++) members[index].Bind(turns);
            return affected;
        }

        public void TickTimedEffects()
        {
            TickAttackBoost();
            TickShield();
        }

        public void ClearTimedEffects()
        {
            AttackMultiplier = 1f;
            AttackBoostTurns = 0;
            AttackBoostSourceId = string.Empty;
            DamageTakenMultiplier = 1f;
            ShieldTurns = 0;
            attackBoostIdentity = new object();
            shieldIdentity = new object();
        }

        internal PartyTimedEffectCapture CaptureTimedEffects()
        {
            return new PartyTimedEffectCapture(attackBoostIdentity, shieldIdentity);
        }

        internal void TickCapturedTimedEffects(PartyTimedEffectCapture capture)
        {
            if (object.ReferenceEquals(attackBoostIdentity, capture.AttackBoostIdentity)) TickAttackBoost();
            if (object.ReferenceEquals(shieldIdentity, capture.ShieldIdentity)) TickShield();
        }

        private void TickAttackBoost()
        {
            if (AttackBoostTurns > 0 && --AttackBoostTurns == 0)
            {
                AttackMultiplier = 1f;
                AttackBoostSourceId = string.Empty;
                attackBoostIdentity = new object();
            }
        }

        private void TickShield()
        {
            if (ShieldTurns > 0 && --ShieldTurns == 0)
            {
                DamageTakenMultiplier = 1f;
                shieldIdentity = new object();
            }
        }

        private static int Saturate(long value)
        {
            return value >= int.MaxValue ? int.MaxValue : (int)value;
        }

        private static void ValidateMultiplier(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    internal struct PartyTimedEffectCapture
    {
        internal PartyTimedEffectCapture(object attackBoostIdentity, object shieldIdentity)
        {
            AttackBoostIdentity = attackBoostIdentity;
            ShieldIdentity = shieldIdentity;
        }

        internal readonly object AttackBoostIdentity;
        internal readonly object ShieldIdentity;
    }
}
