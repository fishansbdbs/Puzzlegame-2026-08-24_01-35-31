using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Battle
{
    public sealed class CharacterRuntime
    {
        private readonly IReadOnlyList<SkillEffectData> passiveEffects;

        public CharacterRuntime(CharacterData data, SkillData activeSkill = null, LeaderSkillData leaderSkill = null,
            SkillEffectData[] passiveEffects = null)
        {
            if (data == null) throw new ArgumentNullException("data");
            ValidateCharacterData(data);
            if (activeSkill != null) ValidateSkill(activeSkill);
            if (leaderSkill != null) ValidateEffects(leaderSkill.Effects, "leaderSkill");
            ValidateEffects(passiveEffects ?? new SkillEffectData[0], "passiveEffects");

            Data = data;
            ActiveSkill = activeSkill;
            LeaderSkill = leaderSkill;
            this.passiveEffects = Array.AsReadOnly((SkillEffectData[])(passiveEffects ?? new SkillEffectData[0]).Clone());
        }

        public CharacterData Data { get; private set; }
        public SkillData ActiveSkill { get; private set; }
        public LeaderSkillData LeaderSkill { get; private set; }
        public IReadOnlyList<SkillEffectData> PassiveEffects { get { return passiveEffects; } }
        public int CurrentCharge { get; private set; }
        public int BindTurns { get; private set; }
        public bool IsBound { get { return BindTurns > 0; } }

        public int AddCharge(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount");
            if (ActiveSkill == null || amount == 0) return 0;
            var before = CurrentCharge;
            var maximum = ActiveSkill.ChargeRequired;
            var total = (long)CurrentCharge + amount;
            CurrentCharge = (int)Math.Min(maximum, total);
            return CurrentCharge - before;
        }

        public void ConsumeCharge()
        {
            if (ActiveSkill == null) throw new InvalidOperationException("Character has no active skill.");
            if (CurrentCharge < ActiveSkill.ChargeRequired) throw new InvalidOperationException("Active skill is not charged.");
            CurrentCharge = 0;
        }

        public void Bind(int turns)
        {
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            BindTurns = Math.Max(BindTurns, turns);
        }

        public void AdvanceBindTurn()
        {
            if (BindTurns > 0) BindTurns--;
        }

        private static void ValidateCharacterData(CharacterData data)
        {
            if (string.IsNullOrWhiteSpace(data.Id)) throw new ArgumentException("Character ID is required.", "data");
            if (data.Element < ElementType.Fire || data.Element > ElementType.Dark)
                throw new ArgumentException("Character element must be a non-Heart defined element.", "data");
            if (data.BaseStats == null || data.BaseStats.Hp < 0 || data.BaseStats.Attack < 0 || data.BaseStats.Recovery < 0)
                throw new ArgumentException("Character stats cannot be null or negative.", "data");
        }

        private static void ValidateSkill(SkillData skill)
        {
            if (string.IsNullOrWhiteSpace(skill.Id)) throw new ArgumentException("Skill ID is required.", "activeSkill");
            if (skill.ChargeRequired < 0) throw new ArgumentException("Skill charge cannot be negative.", "activeSkill");
            if (skill.ChargeElement < ElementType.Fire || skill.ChargeElement > ElementType.Heart)
                throw new ArgumentException("Skill charge element is not defined.", "activeSkill");
            ValidateEffects(skill.Effects, "activeSkill");
            if (skill.Effects.Length == 0) throw new ArgumentException("Skill must have at least one effect.", "activeSkill");
        }

        internal static void ValidateEffects(SkillEffectData[] effects, string parameterName)
        {
            if (effects == null) throw new ArgumentException("Effects cannot be null.", parameterName);
            for (var index = 0; index < effects.Length; index++)
            {
                if (effects[index] == null || effects[index].Payload == null)
                    throw new ArgumentException("Effects and payloads cannot be null.", parameterName);
                if (effects[index].Type < SkillEffectType.ConvertOrbs || effects[index].Type > SkillEffectType.DirectDamage)
                    throw new ArgumentException("Skill effect type is not defined.", parameterName);
            }
        }
    }
}
