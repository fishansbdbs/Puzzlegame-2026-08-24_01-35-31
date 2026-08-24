using System;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Battle
{
    public sealed class EnemyRuntime
    {
        private readonly EnemyData data;
        private int nextActionIndex;

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
        }

        public void SetComboShield(int minimumComboCount, int turns)
        {
            if (minimumComboCount < 0) throw new ArgumentOutOfRangeException("minimumComboCount");
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            if (turns == 0) return;
            ComboShieldMinimum = minimumComboCount;
            ComboShieldTurns = turns;
        }

        public void SetEnrage(float multiplier, int turns)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 0f)
                throw new ArgumentOutOfRangeException("multiplier");
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            if (turns == 0) return;
            AttackMultiplier = multiplier;
            EnrageTurns = turns;
        }

        public void TickTimedEffects()
        {
            if (AbsorbTurns > 0 && --AbsorbTurns == 0) { AbsorbedElement = null; }
            if (ComboShieldTurns > 0 && --ComboShieldTurns == 0) { ComboShieldMinimum = 0; }
            if (EnrageTurns > 0 && --EnrageTurns == 0) { AttackMultiplier = 1f; }
        }

        public void ClearTimedEffects()
        {
            AbsorbedElement = null;
            AbsorbTurns = 0;
            ComboShieldMinimum = 0;
            ComboShieldTurns = 0;
            AttackMultiplier = 1f;
            EnrageTurns = 0;
        }

        private static void Validate(EnemyData data)
        {
            if (string.IsNullOrWhiteSpace(data.Id)) throw new ArgumentException("Enemy ID is required.", "data");
            ValidateCombatElement(data.Element, "data");
            if (data.BaseStats == null || data.BaseStats.Hp < 0 || data.BaseStats.Attack < 0 || data.BaseStats.Recovery < 0)
                throw new ArgumentException("Enemy stats cannot be null or negative.", "data");
            if (data.InitialCountdown < 1) throw new ArgumentException("Enemy countdown must be at least one.", "data");
            if (data.Actions == null || data.Actions.Length == 0) throw new ArgumentException("Enemy must define actions.", "data");
            for (var actionIndex = 0; actionIndex < data.Actions.Length; actionIndex++)
            {
                var action = data.Actions[actionIndex];
                if (action == null || string.IsNullOrWhiteSpace(action.Id) || action.ResetCountdown < 1 || action.Effects == null)
                    throw new ArgumentException("Enemy actions must be identified, reset to at least one, and define effects.", "data");
                for (var effectIndex = 0; effectIndex < action.Effects.Length; effectIndex++)
                {
                    var effect = action.Effects[effectIndex];
                    if (effect == null || effect.Payload == null || effect.Type < EnemyEffectType.Damage || effect.Type > EnemyEffectType.ManipulateCountdown)
                        throw new ArgumentException("Enemy effects and payloads must be valid.", "data");
                }
            }
        }

        internal static void ValidateCombatElement(ElementType element, string name)
        {
            if (element < ElementType.Fire || element > ElementType.Dark)
                throw new ArgumentException("Element must be a defined non-Heart element.", name);
        }
    }
}
