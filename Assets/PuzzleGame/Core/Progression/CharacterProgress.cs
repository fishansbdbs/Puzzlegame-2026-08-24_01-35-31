using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Progression
{
    public sealed class CharacterProgress
    {
        private readonly CharacterData data;
        private readonly SkillSnapshot activeSkill;
        private int level;
        private int totalExperience;
        private int ascension;
        private bool awakened;

        public CharacterProgress(CharacterData data, SkillData activeSkill)
        {
            if (data == null) throw new ArgumentNullException("data");
            var errors = ContractValidation.Validate(data);
            if (errors.Count > 0) throw new ArgumentException(errors[0], "data");
            ValidateActiveSkill(data, activeSkill);
            this.data = ProgressionDataSnapshot.Clone(data);
            this.activeSkill = new SkillSnapshot(activeSkill.Id, activeSkill.DisplayName, activeSkill.ChargeElement, activeSkill.ChargeRequired);
            level = 1;
        }

        public string CharacterId { get { return data.Id; } }
        public int Level { get { return level; } }
        public int TotalExperience { get { return totalExperience; } }
        public int Ascension { get { return ascension; } }
        public bool IsAwakened { get { return awakened; } }
        public int EffectiveRarity { get { return awakened ? (int)Rarity.Awakened : data.BaseRarity; } }
        public StatSnapshot CurrentStats { get { return ProgressionCalculation.DeriveStats(data, level, ascension); } }
        public AscensionEffectSnapshot CurrentAscensionEffects { get { return ProgressionCalculation.DeriveEffects(data, ascension); } }
        public SkillSnapshot EffectiveActiveSkill
        {
            get
            {
                var reduction = CurrentAscensionEffects.ActiveSkillChargeReduction;
                var charge = Math.Max(1L, (long)activeSkill.ChargeRequired - reduction);
                return new SkillSnapshot(activeSkill.Id, activeSkill.DisplayName, activeSkill.ChargeElement, (int)charge);
            }
        }
        public VisualSnapshot CurrentVisuals { get { return ProgressionDataSnapshot.Snapshot(awakened ? data.AwakenedVisuals : data.BaseVisuals); } }

        internal CharacterData Data { get { return data; } }
        internal CharacterProgress CreateTransactionSnapshot()
        {
            var snapshot = new CharacterProgress(ProgressionDataSnapshot.Clone(data), new SkillData
            {
                Id = activeSkill.Id,
                DisplayName = activeSkill.DisplayName,
                ChargeElement = activeSkill.ChargeElement,
                ChargeRequired = activeSkill.ChargeRequired
            });
            snapshot.SetExperience(totalExperience, level);
            snapshot.SetAscension(ascension);
            if (awakened) snapshot.SetAwakened();
            return snapshot;
        }
        internal void SetExperience(int value, int newLevel) { totalExperience = value; level = newLevel; }
        internal void SetAscension(int value) { ascension = value; }
        internal void SetAwakened() { awakened = true; }

        private static void ValidateActiveSkill(CharacterData data, SkillData activeSkill)
        {
            if (activeSkill == null) throw new ArgumentNullException("activeSkill");
            if (!string.Equals(data.ActiveSkillId, activeSkill.Id, StringComparison.Ordinal))
                throw new ArgumentException("Active skill ID must match the character active skill ID.", "activeSkill");
            if (activeSkill.ChargeRequired < 1) throw new ArgumentException("Active skill charge requirement must be at least one.", "activeSkill");
            if (activeSkill.ChargeElement < ElementType.Fire || activeSkill.ChargeElement > ElementType.Heart)
                throw new ArgumentException("Active skill charge element is not defined.", "activeSkill");
        }
    }

    public sealed class StatSnapshot
    {
        public StatSnapshot(int hp, int attack, int recovery) { Hp = hp; Attack = attack; Recovery = recovery; }
        public int Hp { get; private set; }
        public int Attack { get; private set; }
        public int Recovery { get; private set; }
    }

    public sealed class SkillSnapshot
    {
        public SkillSnapshot(string id, string displayName, ElementType chargeElement, int chargeRequired)
        {
            Id = id; DisplayName = displayName; ChargeElement = chargeElement; ChargeRequired = chargeRequired;
        }

        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public ElementType ChargeElement { get; private set; }
        public int ChargeRequired { get; private set; }
    }

    public sealed class VisualSnapshot
    {
        public VisualSnapshot(string portraitKey, string cardArtKey, string modelKey, string vfxKey)
        {
            PortraitKey = portraitKey; CardArtKey = cardArtKey; ModelKey = modelKey; VfxKey = vfxKey;
        }

        public string PortraitKey { get; private set; }
        public string CardArtKey { get; private set; }
        public string ModelKey { get; private set; }
        public string VfxKey { get; private set; }
    }

    public sealed class AscensionEffectSnapshot
    {
        public AscensionEffectSnapshot(int activeSkillChargeReduction, StatSnapshot statBonus)
        {
            ActiveSkillChargeReduction = activeSkillChargeReduction; StatBonus = statBonus;
        }

        public int ActiveSkillChargeReduction { get; private set; }
        public StatSnapshot StatBonus { get; private set; }
    }

    internal static class ProgressionDataSnapshot
    {
        internal static CharacterData Clone(CharacterData source)
        {
            var ranks = new AscensionRankData[source.Ascension.Ranks.Length];
            for (var index = 0; index < ranks.Length; index++)
            {
                var rank = source.Ascension.Ranks[index];
                ranks[index] = new AscensionRankData { Rank = rank.Rank, ActiveSkillChargeReduction = rank.ActiveSkillChargeReduction, StatBonus = Clone(rank.StatBonus) };
            }
            var stats = new StatBlock[source.LevelCurve.StatsByLevel.Length];
            for (var index = 0; index < stats.Length; index++) stats[index] = Clone(source.LevelCurve.StatsByLevel[index]);
            var materials = new MaterialRequirementData[source.Awakening.Materials.Length];
            for (var index = 0; index < materials.Length; index++) materials[index] = new MaterialRequirementData { MaterialId = source.Awakening.Materials[index].MaterialId, Amount = source.Awakening.Materials[index].Amount };
            return new CharacterData
            {
                Id = source.Id, DisplayName = source.DisplayName, Element = source.Element, BaseRarity = source.BaseRarity,
                BaseStats = Clone(source.BaseStats), ActiveSkillId = source.ActiveSkillId, LeaderSkillId = source.LeaderSkillId,
                PassiveId = source.PassiveId, Tags = source.Tags == null ? null : (string[])source.Tags.Clone(),
                LevelCurve = new ProgressionCurveData { MaxLevel = source.LevelCurve.MaxLevel, ExperienceRequiredByLevel = (int[])source.LevelCurve.ExperienceRequiredByLevel.Clone(), StatsByLevel = stats },
                Ascension = new AscensionConfigurationData { OverflowUniversalResourceAmount = source.Ascension.OverflowUniversalResourceAmount, Ranks = ranks },
                Awakening = new AwakeningRequirementData { RequiredLevel = source.Awakening.RequiredLevel, GoldCost = source.Awakening.GoldCost, Materials = materials },
                BaseVisuals = Clone(source.BaseVisuals), AwakenedVisuals = Clone(source.AwakenedVisuals)
            };
        }

        internal static VisualSnapshot Snapshot(VisualReferenceSet source) { return new VisualSnapshot(source.PortraitKey, source.CardArtKey, source.ModelKey, source.VfxKey); }
        private static StatBlock Clone(StatBlock source) { return source == null ? null : new StatBlock { Hp = source.Hp, Attack = source.Attack, Recovery = source.Recovery }; }
        private static VisualReferenceSet Clone(VisualReferenceSet source) { return new VisualReferenceSet { PortraitKey = source.PortraitKey, CardArtKey = source.CardArtKey, ModelKey = source.ModelKey, VfxKey = source.VfxKey }; }
    }

    internal static class ProgressionCalculation
    {
        internal static StatSnapshot DeriveStats(CharacterData data, int level, int ascension)
        {
            var baseStats = data.LevelCurve.StatsByLevel[level - 1];
            var effects = DeriveEffects(data, ascension).StatBonus;
            return new StatSnapshot(CheckedAdd(baseStats.Hp, effects.Hp), CheckedAdd(baseStats.Attack, effects.Attack), CheckedAdd(baseStats.Recovery, effects.Recovery));
        }

        internal static AscensionEffectSnapshot DeriveEffects(CharacterData data, int ascension)
        {
            long reduction = 0; long hp = 0; long attack = 0; long recovery = 0;
            for (var rankNumber = 1; rankNumber <= ascension; rankNumber++)
            {
                var rank = FindRank(data.Ascension.Ranks, rankNumber);
                reduction += rank.ActiveSkillChargeReduction;
                if (rank.StatBonus == null) continue;
                hp += rank.StatBonus.Hp; attack += rank.StatBonus.Attack; recovery += rank.StatBonus.Recovery;
            }
            return new AscensionEffectSnapshot(ToInt(reduction), new StatSnapshot(ToInt(hp), ToInt(attack), ToInt(recovery)));
        }

        internal static AscensionRankData FindRank(AscensionRankData[] ranks, int rankNumber)
        {
            for (var index = 0; index < ranks.Length; index++) if (ranks[index].Rank == rankNumber) return ranks[index];
            throw new InvalidOperationException("Validated ascension rank is missing.");
        }

        private static int CheckedAdd(int left, int right)
        {
            try { return checked(left + right); }
            catch (OverflowException) { throw new InvalidOperationException("Authored progression values overflow integer stats."); }
        }
        private static int ToInt(long value)
        {
            if (value < 0 || value > int.MaxValue) throw new InvalidOperationException("Authored progression values overflow integer stats.");
            return (int)value;
        }
    }

    public static class ProgressionValidation
    {
        public static IReadOnlyList<string> Validate(CharacterData data)
        {
            var errors = new List<string>();
            if (data == null) { errors.Add("Data cannot be null."); return errors.AsReadOnly(); }
            if (data.Element == ElementType.Heart) errors.Add("Character element cannot be Heart.");
            if (data.BaseRarity < (int)Rarity.One || data.BaseRarity > (int)Rarity.Five) errors.Add("Character base rarity must be between 1 and 5.");
            if (string.IsNullOrWhiteSpace(data.ActiveSkillId)) errors.Add("Character active skill ID is required.");
            var curveValid = ValidateCurve(data.LevelCurve, errors);
            var ascensionValid = ValidateAscension(data.Ascension, errors);
            ValidateAwakening(data.Awakening, curveValid ? data.LevelCurve.MaxLevel : 0, errors);
            if (data.BaseVisuals == null || data.AwakenedVisuals == null) errors.Add("Character visuals cannot be null.");
            if (curveValid && ascensionValid) ValidateEffectiveValueRanges(data.LevelCurve, data.Ascension, errors);
            return errors.AsReadOnly();
        }

        private static bool ValidateCurve(ProgressionCurveData curve, List<string> errors)
        {
            if (curve == null || curve.MaxLevel < 1 || curve.ExperienceRequiredByLevel == null || curve.StatsByLevel == null)
            { errors.Add("Progression curve is required and must define a positive max level."); return false; }
            var valid = true;
            if (curve.ExperienceRequiredByLevel.Length != curve.MaxLevel - 1 || curve.StatsByLevel.Length != curve.MaxLevel)
            { errors.Add("Progression curve must define one stat block per level and one cumulative threshold per level after one."); valid = false; }
            var previous = 0;
            for (var index = 0; index < curve.ExperienceRequiredByLevel.Length; index++)
            {
                var threshold = curve.ExperienceRequiredByLevel[index];
                if (threshold <= previous) { errors.Add("Progression experience thresholds must be strictly increasing positive totals."); valid = false; break; }
                previous = threshold;
            }
            for (var index = 0; index < curve.StatsByLevel.Length; index++)
            {
                var stats = curve.StatsByLevel[index];
                if (stats == null || stats.Hp < 0 || stats.Attack < 0 || stats.Recovery < 0) { errors.Add("Progression stats cannot be null or negative."); valid = false; break; }
            }
            return valid;
        }

        private static bool ValidateAscension(AscensionConfigurationData ascension, List<string> errors)
        {
            if (ascension == null || ascension.Ranks == null || ascension.Ranks.Length != 5 || ascension.OverflowUniversalResourceAmount <= 0)
            { errors.Add("Ascension must define five ranks and a positive overflow conversion."); return false; }
            var valid = true; var seen = new bool[6];
            for (var index = 0; index < ascension.Ranks.Length; index++)
            {
                var rank = ascension.Ranks[index];
                if (rank == null || rank.Rank < 1 || rank.Rank > 5 || seen[rank.Rank]) { errors.Add("Ascension ranks must contain each authored rank from one through five exactly once."); valid = false; continue; }
                seen[rank.Rank] = true;
                if (rank.ActiveSkillChargeReduction < 0 || (rank.StatBonus != null && (rank.StatBonus.Hp < 0 || rank.StatBonus.Attack < 0 || rank.StatBonus.Recovery < 0))) { errors.Add("Ascension effects cannot be negative."); valid = false; }
                if (rank.Rank <= 2)
                {
                    if (rank.ActiveSkillChargeReduction <= 0) { errors.Add("Ascension ranks 1 and 2 must each reduce active skill charge by a positive amount."); valid = false; }
                    if (HasPositiveStat(rank.StatBonus)) { errors.Add("Ascension ranks 1 and 2 cannot grant stat bonuses."); valid = false; }
                }
                else
                {
                    if (rank.ActiveSkillChargeReduction != 0) { errors.Add("Ascension ranks 3 through 5 cannot reduce active skill charge."); valid = false; }
                    if (!HasPositiveStat(rank.StatBonus)) { errors.Add("Ascension ranks 3 through 5 must each grant a positive stat bonus."); valid = false; }
                }
            }
            return valid;
        }

        private static void ValidateAwakening(AwakeningRequirementData awakening, int maxLevel, List<string> errors)
        {
            if (awakening == null || awakening.Materials == null) { errors.Add("Awakening configuration cannot be null."); return; }
            if (awakening.RequiredLevel == 0 && awakening.GoldCost == 0 && awakening.Materials.Length == 0) return;
            if (awakening.RequiredLevel < 1 || awakening.RequiredLevel > maxLevel || awakening.GoldCost <= 0 || awakening.Materials.Length == 0) errors.Add("Awakening requirements are malformed.");
            for (var index = 0; index < awakening.Materials.Length; index++)
            {
                var material = awakening.Materials[index];
                if (material == null || string.IsNullOrWhiteSpace(material.MaterialId) || material.Amount <= 0) errors.Add("Awakening materials must have a nonblank ID and positive amount.");
                else for (var other = 0; other < index; other++) if (awakening.Materials[other] != null && string.Equals(awakening.Materials[other].MaterialId, material.MaterialId, StringComparison.Ordinal)) errors.Add("Awakening material IDs must be unique.");
            }
        }

        private static void ValidateEffectiveValueRanges(ProgressionCurveData curve, AscensionConfigurationData ascension, List<string> errors)
        {
            long reduction = 0; var statDeltas = new long[3];
            for (var rankNumber = 1; rankNumber <= 5; rankNumber++)
            {
                var rank = ProgressionCalculation.FindRank(ascension.Ranks, rankNumber);
                reduction += rank.ActiveSkillChargeReduction;
                if (reduction > int.MaxValue) { errors.Add("Cumulative ascension charge reduction overflows Int32."); break; }
                if (rank.StatBonus != null) { statDeltas[0] += rank.StatBonus.Hp; statDeltas[1] += rank.StatBonus.Attack; statDeltas[2] += rank.StatBonus.Recovery; }
                for (var level = 0; level < curve.StatsByLevel.Length; level++)
                {
                    var stats = curve.StatsByLevel[level];
                    if ((long)stats.Hp + statDeltas[0] > int.MaxValue || (long)stats.Attack + statDeltas[1] > int.MaxValue || (long)stats.Recovery + statDeltas[2] > int.MaxValue)
                    { errors.Add("Level stats plus ascension bonuses overflow Int32."); return; }
                }
            }
        }

        private static bool HasPositiveStat(StatBlock stats) { return stats != null && (stats.Hp > 0 || stats.Attack > 0 || stats.Recovery > 0); }
    }
}
