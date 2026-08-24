using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Battle
{
    public sealed class CharacterRuntime
    {
        private readonly CharacterData data;
        private readonly SkillData activeSkill;
        private readonly LeaderSkillData leaderSkill;
        private readonly PassiveData passive;
        private readonly int effectiveRarity;
        private readonly VisualReferenceSet visuals;
        private object bindIdentity = new object();

        public CharacterRuntime(CharacterData data, SkillData activeSkill = null, LeaderSkillData leaderSkill = null,
            PassiveData passive = null, int effectiveRarity = 0, VisualReferenceSet visuals = null)
        {
            if (data == null) throw new ArgumentNullException("data");
            ValidateCharacterData(data);
            ValidateBound(data, "data");
            ValidateStableIds(data, activeSkill, leaderSkill, passive);
            ValidateBound(activeSkill, "activeSkill");
            ValidateBound(leaderSkill, "leaderSkill");
            ValidateBound(passive, "passive");
            this.data = AuthoredDataSnapshot.Clone(data);
            this.activeSkill = AuthoredDataSnapshot.Clone(activeSkill);
            this.leaderSkill = AuthoredDataSnapshot.Clone(leaderSkill);
            this.passive = AuthoredDataSnapshot.Clone(passive);
            this.effectiveRarity = effectiveRarity == 0 ? data.BaseRarity : effectiveRarity;
            if (this.effectiveRarity < (int)Rarity.One || this.effectiveRarity > (int)Rarity.Awakened)
                throw new ArgumentOutOfRangeException("effectiveRarity");
            this.visuals = AuthoredDataSnapshot.Clone(visuals ?? data.BaseVisuals);
        }

        public CharacterData Data { get { return AuthoredDataSnapshot.Clone(data); } }
        public SkillData ActiveSkill { get { return AuthoredDataSnapshot.Clone(activeSkill); } }
        public LeaderSkillData LeaderSkill { get { return AuthoredDataSnapshot.Clone(leaderSkill); } }
        public PassiveData Passive { get { return AuthoredDataSnapshot.Clone(passive); } }
        public IReadOnlyList<SkillEffectData> PassiveEffects { get { return Array.AsReadOnly(AuthoredDataSnapshot.Clone(passive == null ? new SkillEffectData[0] : passive.Effects)); } }
        public int EffectiveRarity { get { return effectiveRarity; } }
        public VisualReferenceSet Visuals { get { return AuthoredDataSnapshot.Clone(visuals); } }
        public int CurrentCharge { get; private set; }
        public int BindTurns { get; private set; }
        public bool IsBound { get { return BindTurns > 0; } }

        internal string Id { get { return data.Id; } }
        internal ElementType Element { get { return data.Element; } }
        internal int MaxHp { get { return data.BaseStats.Hp; } }
        internal int Attack { get { return data.BaseStats.Attack; } }
        internal int Recovery { get { return data.BaseStats.Recovery; } }
        internal string PassiveId { get { return data.PassiveId; } }
        internal SkillData ActiveSkillState { get { return activeSkill; } }
        internal LeaderSkillData LeaderSkillState { get { return leaderSkill; } }
        internal SkillEffectData[] PassiveEffectState { get { return passive == null ? new SkillEffectData[0] : passive.Effects; } }

        internal bool HasTag(string tag)
        {
            var tags = data.Tags ?? new string[0];
            for (var index = 0; index < tags.Length; index++)
                if (string.Equals(tags[index], tag, StringComparison.Ordinal)) return true;
            return false;
        }

        public int AddCharge(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount");
            if (activeSkill == null || amount == 0) return 0;
            var before = CurrentCharge;
            CurrentCharge = (int)Math.Min(activeSkill.ChargeRequired, (long)CurrentCharge + amount);
            return CurrentCharge - before;
        }

        public void ConsumeCharge()
        {
            if (activeSkill == null) throw new InvalidOperationException("Character has no active skill.");
            if (CurrentCharge < activeSkill.ChargeRequired) throw new InvalidOperationException("Active skill is not charged.");
            CurrentCharge = 0;
        }

        public void Bind(int turns)
        {
            if (turns < 0) throw new ArgumentOutOfRangeException("turns");
            if (turns > 0)
            {
                BindTurns = Math.Max(BindTurns, turns);
                bindIdentity = new object();
            }
        }

        public void TickBind()
        {
            if (BindTurns > 0 && --BindTurns == 0) bindIdentity = new object();
        }

        public void ClearBind()
        {
            BindTurns = 0;
            bindIdentity = new object();
        }

        public void AdvanceBindTurn() { TickBind(); }
        internal object CaptureBindIdentity() { return bindIdentity; }
        internal void TickCapturedBind(object capturedIdentity) { if (object.ReferenceEquals(bindIdentity, capturedIdentity)) TickBind(); }

        private static void ValidateCharacterData(CharacterData data)
        {
            if (string.IsNullOrWhiteSpace(data.Id)) throw new ArgumentException("Character ID is required.", "data");
            if (data.Element < ElementType.Fire || data.Element > ElementType.Dark)
                throw new ArgumentException("Character element must be a non-Heart defined element.", "data");
            if (data.BaseStats == null || data.BaseStats.Hp < 0 || data.BaseStats.Attack < 0 || data.BaseStats.Recovery < 0)
                throw new ArgumentException("Character stats cannot be null or negative.", "data");
        }

        private static void ValidateStableIds(CharacterData data, SkillData activeSkill, LeaderSkillData leaderSkill, PassiveData passive)
        {
            if (string.IsNullOrWhiteSpace(data.ActiveSkillId) != (activeSkill == null))
                throw new ArgumentException("Character active skill ID and active skill must either both be present or both be absent.", "activeSkill");
            if (activeSkill != null && !string.Equals(data.ActiveSkillId, activeSkill.Id, StringComparison.Ordinal))
                throw new ArgumentException("Active skill ID must match the character active skill ID.", "activeSkill");
            if (string.IsNullOrWhiteSpace(data.LeaderSkillId) != (leaderSkill == null))
                throw new ArgumentException("Character leader skill ID and leader skill must either both be present or both be absent.", "leaderSkill");
            if (leaderSkill != null && !string.Equals(data.LeaderSkillId, leaderSkill.Id, StringComparison.Ordinal))
                throw new ArgumentException("Leader skill ID must match the character leader skill ID.", "leaderSkill");
            if (string.IsNullOrWhiteSpace(data.PassiveId) != (passive == null))
                throw new ArgumentException("Character passive ID and passive must either both be present or both be absent.", "passive");
            if (passive != null && !string.Equals(data.PassiveId, passive.Id, StringComparison.Ordinal))
                throw new ArgumentException("Passive ID must match the character passive ID.", "passive");
        }

        private static void ValidateBound(IIdentifiedData data, string parameterName)
        {
            if (data == null) return;
            var errors = ContractValidation.Validate(data);
            if (errors.Count > 0) throw new ArgumentException(errors[0], parameterName);
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

    internal static class AuthoredDataSnapshot
    {
        internal static CharacterData Clone(CharacterData source)
        {
            if (source == null) return null;
            return new CharacterData
            {
                Id = source.Id, DisplayName = source.DisplayName, Element = source.Element, BaseRarity = source.BaseRarity,
                BaseStats = Clone(source.BaseStats), ActiveSkillId = source.ActiveSkillId, LeaderSkillId = source.LeaderSkillId,
                PassiveId = source.PassiveId, Tags = Clone(source.Tags), LevelCurve = Clone(source.LevelCurve),
                Ascension = Clone(source.Ascension),
                Awakening = Clone(source.Awakening), BaseVisuals = Clone(source.BaseVisuals), AwakenedVisuals = Clone(source.AwakenedVisuals)
            };
        }

        internal static SkillData Clone(SkillData source)
        {
            if (source == null) return null;
            return new SkillData { Id = source.Id, DisplayName = source.DisplayName, ChargeElement = source.ChargeElement, ChargeRequired = source.ChargeRequired, Effects = Clone(source.Effects) };
        }

        internal static LeaderSkillData Clone(LeaderSkillData source)
        {
            if (source == null) return null;
            return new LeaderSkillData
            {
                Id = source.Id, DisplayName = source.DisplayName,
                RequiredElements = source.RequiredElements == null ? null : (ElementType[])source.RequiredElements.Clone(),
                RequiredTags = Clone(source.RequiredTags), MinimumComboCount = source.MinimumComboCount,
                MinimumHpPercent = source.MinimumHpPercent, Effects = Clone(source.Effects)
            };
        }

        internal static PassiveData Clone(PassiveData source)
        {
            return source == null ? null : new PassiveData { Id = source.Id, Effects = Clone(source.Effects) };
        }

        internal static SkillEffectData[] Clone(SkillEffectData[] source)
        {
            if (source == null) return null;
            var result = new SkillEffectData[source.Length];
            for (var index = 0; index < source.Length; index++) result[index] = new SkillEffectData { Type = source[index].Type, Payload = Clone(source[index].Payload) };
            return result;
        }

        internal static EnemyData Clone(EnemyData source)
        {
            if (source == null) return null;
            var actions = new EnemyActionData[source.Actions.Length];
            for (var index = 0; index < actions.Length; index++) actions[index] = Clone(source.Actions[index]);
            var triggers = source.ThresholdTriggers == null ? null : new EnemyThresholdTriggerData[source.ThresholdTriggers.Length];
            if (triggers != null) for (var index = 0; index < triggers.Length; index++) triggers[index] = Clone(source.ThresholdTriggers[index]);
            return new EnemyData
            {
                Id = source.Id, DisplayName = source.DisplayName, Element = source.Element, BaseStats = Clone(source.BaseStats),
                InitialCountdown = source.InitialCountdown, Actions = actions, ThresholdTriggers = triggers, Visuals = Clone(source.Visuals)
            };
        }

        internal static EnemyThresholdTriggerData Clone(EnemyThresholdTriggerData source)
        {
            return source == null ? null : new EnemyThresholdTriggerData { Id = source.Id, HpThresholdPercent = source.HpThresholdPercent, Effect = Clone(source.Effect) };
        }

        internal static EnemyActionData Clone(EnemyActionData source)
        {
            if (source == null) return null;
            var effects = new EnemyEffectData[source.Effects.Length];
            for (var index = 0; index < effects.Length; index++) effects[index] = new EnemyEffectData { Type = source.Effects[index].Type, Payload = Clone(source.Effects[index].Payload) };
            return new EnemyActionData { Id = source.Id, ResetCountdown = source.ResetCountdown, Effects = effects };
        }

        internal static EnemyEffectData Clone(EnemyEffectData source)
        {
            return source == null ? null : new EnemyEffectData { Type = source.Type, Payload = Clone(source.Payload) };
        }

        internal static EffectPayloadData Clone(EffectPayloadData source)
        {
            if (source == null) return null;
            return new EffectPayloadData
            {
                Amount = source.Amount, Multiplier = source.Multiplier, DurationSeconds = source.DurationSeconds,
                TurnCount = source.TurnCount, ComboCount = source.ComboCount, SourceOrb = source.SourceOrb,
                TargetOrb = source.TargetOrb, TargetId = source.TargetId, Tags = Clone(source.Tags)
            };
        }

        private static string[] Clone(string[] source) { return source == null ? null : (string[])source.Clone(); }
        private static StatBlock Clone(StatBlock source) { return source == null ? null : new StatBlock { Hp = source.Hp, Attack = source.Attack, Recovery = source.Recovery }; }
        internal static VisualReferenceSet Clone(VisualReferenceSet source) { return source == null ? null : new VisualReferenceSet { PortraitKey = source.PortraitKey, CardArtKey = source.CardArtKey, ModelKey = source.ModelKey, VfxKey = source.VfxKey }; }

        private static ProgressionCurveData Clone(ProgressionCurveData source)
        {
            if (source == null) return null;
            var stats = source.StatsByLevel == null ? null : new StatBlock[source.StatsByLevel.Length];
            if (stats != null) for (var index = 0; index < stats.Length; index++) stats[index] = Clone(source.StatsByLevel[index]);
            return new ProgressionCurveData { MaxLevel = source.MaxLevel, ExperienceRequiredByLevel = source.ExperienceRequiredByLevel == null ? null : (int[])source.ExperienceRequiredByLevel.Clone(), StatsByLevel = stats };
        }

        private static AscensionConfigurationData Clone(AscensionConfigurationData source)
        {
            if (source == null) return null;
            var ranks = source.Ranks == null ? null : new AscensionRankData[source.Ranks.Length];
            if (ranks != null)
            {
                for (var index = 0; index < ranks.Length; index++)
                {
                    var rank = source.Ranks[index];
                    ranks[index] = rank == null ? null : new AscensionRankData
                    {
                        Rank = rank.Rank,
                        ActiveSkillChargeReduction = rank.ActiveSkillChargeReduction,
                        StatBonus = Clone(rank.StatBonus)
                    };
                }
            }

            return new AscensionConfigurationData
            {
                OverflowUniversalResourceAmount = source.OverflowUniversalResourceAmount,
                Ranks = ranks
            };
        }

        private static AwakeningRequirementData Clone(AwakeningRequirementData source)
        {
            if (source == null) return null;
            var materials = source.Materials == null ? null : new MaterialRequirementData[source.Materials.Length];
            if (materials != null) for (var index = 0; index < materials.Length; index++)
                materials[index] = source.Materials[index] == null ? null : new MaterialRequirementData { MaterialId = source.Materials[index].MaterialId, Amount = source.Materials[index].Amount };
            return new AwakeningRequirementData { RequiredLevel = source.RequiredLevel, GoldCost = source.GoldCost, Materials = materials };
        }
    }
}
