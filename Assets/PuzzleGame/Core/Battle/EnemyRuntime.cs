using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Battle
{
    public sealed class EnemyRuntime
    {
        private readonly EnemyData data;
        private int nextActionIndex;
        private object absorbIdentity = new object();
        private object comboShieldIdentity = new object();
        private object enrageIdentity = new object();
        private readonly HashSet<string> consumedThresholdTriggerIds = new HashSet<string>(StringComparer.Ordinal);

        public EnemyRuntime(EnemyData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            Validate(data);
            this.data = AuthoredDataSnapshot.Clone(data);
            MaxHp = data.BaseStats.Hp;
            CurrentHp = MaxHp;
            Countdown = data.InitialCountdown;
            AttackMultiplier = 1f;
        }

        public EnemyData Data { get { return AuthoredDataSnapshot.Clone(data); } }
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int Countdown { get; private set; }
        public int CurrentActionIndex { get { return nextActionIndex; } }
        public ElementType? AbsorbedElement { get; private set; }
        public int AbsorbTurns { get; private set; }
        public int ComboShieldMinimum { get; private set; }
        public int ComboShieldTurns { get; private set; }
        public float AttackMultiplier { get; private set; }
        public int EnrageTurns { get; private set; }
        public bool IsDefeated { get { return CurrentHp == 0; } }
        internal ElementType Element { get { return data.Element; } }

        public int ApplyDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount");
            var applied = Math.Min(CurrentHp, amount);
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

        public int ModifyCountdown(int delta)
        {
            var value = (long)Countdown + delta;
            Countdown = value <= 0 ? 0 : value >= int.MaxValue ? int.MaxValue : (int)value;
            return Countdown;
        }

        public void ResetCountdown(int value)
        {
            if (value < 1) throw new ArgumentOutOfRangeException("value");
            Countdown = value;
        }

        internal EnemyActionData PeekNextAction() { return data.Actions[nextActionIndex]; }

        internal bool TryConsumeThresholdTrigger(string triggerId)
        {
            if (string.IsNullOrWhiteSpace(triggerId)) throw new ArgumentException("Threshold trigger ID is required.", "triggerId");
            return consumedThresholdTriggerIds.Add(triggerId);
        }

        internal EnemyActionData CommitNextAction()
        {
            var action = data.Actions[nextActionIndex];
            nextActionIndex = (nextActionIndex + 1) % data.Actions.Length;
            return action;
        }

        public void SetDamageAbsorb(ElementType element, int turns)
        {
            ValidateCombatElement(element, "element");
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            if (turns == 0) return;
            AbsorbedElement = element;
            AbsorbTurns = turns;
            absorbIdentity = new object();
        }

        public void SetComboShield(int minimumComboCount, int turns)
        {
            if (minimumComboCount < 0) throw new ArgumentOutOfRangeException("minimumComboCount");
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            if (turns == 0) return;
            ComboShieldMinimum = minimumComboCount;
            ComboShieldTurns = turns;
            comboShieldIdentity = new object();
        }

        public void SetEnrage(float multiplier, int turns)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0f)
                throw new ArgumentOutOfRangeException("multiplier");
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            if (turns == 0) return;
            AttackMultiplier = multiplier;
            EnrageTurns = turns;
            enrageIdentity = new object();
        }

        public void TickTimedEffects()
        {
            TickAbsorb();
            TickComboShield();
            TickEnrage();
        }

        public void ClearTimedEffects()
        {
            AbsorbedElement = null;
            AbsorbTurns = 0;
            ComboShieldMinimum = 0;
            ComboShieldTurns = 0;
            AttackMultiplier = 1f;
            EnrageTurns = 0;
            absorbIdentity = new object();
            comboShieldIdentity = new object();
            enrageIdentity = new object();
        }

        internal EnemyTimedEffectCapture CaptureTimedEffects()
        {
            return new EnemyTimedEffectCapture(absorbIdentity, comboShieldIdentity, enrageIdentity);
        }

        internal void TickCapturedTimedEffects(EnemyTimedEffectCapture capture)
        {
            if (object.ReferenceEquals(absorbIdentity, capture.AbsorbIdentity)) TickAbsorb();
            if (object.ReferenceEquals(comboShieldIdentity, capture.ComboShieldIdentity)) TickComboShield();
            if (object.ReferenceEquals(enrageIdentity, capture.EnrageIdentity)) TickEnrage();
        }

        private void TickAbsorb()
        {
            if (AbsorbTurns > 0 && --AbsorbTurns == 0)
            {
                AbsorbedElement = null;
                absorbIdentity = new object();
            }
        }

        private void TickComboShield()
        {
            if (ComboShieldTurns > 0 && --ComboShieldTurns == 0)
            {
                ComboShieldMinimum = 0;
                comboShieldIdentity = new object();
            }
        }

        private void TickEnrage()
        {
            if (EnrageTurns > 0 && --EnrageTurns == 0)
            {
                AttackMultiplier = 1f;
                enrageIdentity = new object();
            }
        }

        private static void Validate(EnemyData data)
        {
            EnemyDataValidation.Validate(data);
        }

        internal static void ValidateCombatElement(ElementType element, string name)
        {
            if (element < ElementType.Fire || element > ElementType.Dark)
                throw new ArgumentException("Element must be a defined non-Heart element.", name);
        }
    }

    internal struct EnemyTimedEffectCapture
    {
        internal EnemyTimedEffectCapture(object absorbIdentity, object comboShieldIdentity, object enrageIdentity)
        {
            AbsorbIdentity = absorbIdentity;
            ComboShieldIdentity = comboShieldIdentity;
            EnrageIdentity = enrageIdentity;
        }

        internal readonly object AbsorbIdentity;
        internal readonly object ComboShieldIdentity;
        internal readonly object EnrageIdentity;
    }
}
