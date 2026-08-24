using System;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Progression
{
    public sealed class CharacterProgress
    {
        private readonly CharacterData data;
        private int level;
        private int totalExperience;
        private int ascension;
        private bool awakened;

        public CharacterProgress(CharacterData data)
        {
            ProgressionValidation.Validate(data);
            this.data = ProgressionDataSnapshot.Clone(data);
            level = 1;
        }

        public string CharacterId { get { return data.Id; } }
        public int Level { get { return level; } }
        public int TotalExperience { get { return totalExperience; } }
        public int Ascension { get { return ascension; } }
        public bool IsAwakened { get { return awakened; } }
        public int EffectiveRarity { get { return awakened ? (int)Rarity.Awakened : data.BaseRarity; } }
        public StatSnapshot CurrentStats { get { return ProgressionCalculation.DeriveStats(data, level, ascension); } }
        public VisualSnapshot CurrentVisuals { get { return ProgressionDataSnapshot.Snapshot(awakened ? data.AwakenedVisuals : data.BaseVisuals); } }

        internal CharacterData Data { get { return data; } }
        internal void SetExperience(int value, int newLevel) { totalExperience = value; level = newLevel; }
        internal void SetAscension(int value) { ascension = value; }
        internal void SetAwakened() { awakened = true; }
    }

    public sealed class StatSnapshot
    {
        public StatSnapshot(int hp, int attack, int recovery)
        {
            Hp = hp;
            Attack = attack;
            Recovery = recovery;
        }

        public int Hp { get; private set; }
        public int Attack { get; private set; }
        public int Recovery { get; private set; }
    }

    public sealed class VisualSnapshot
    {
        public VisualSnapshot(string portraitKey, string cardArtKey, string modelKey, string vfxKey)
        {
            PortraitKey = portraitKey;
            CardArtKey = cardArtKey;
            ModelKey = modelKey;
            VfxKey = vfxKey;
        }

        public string PortraitKey { get; private set; }
        public string CardArtKey { get; private set; }
        public string ModelKey { get; private set; }
        public string VfxKey { get; private set; }
    }

    public sealed class AscensionEffectSnapshot
    {
        public AscensionEffectSnapshot(int activeSkillPerformanceBonus, int passivePerformanceBonus, StatSnapshot statBonus)
        {
            ActiveSkillPerformanceBonus = activeSkillPerformanceBonus;
            PassivePerformanceBonus = passivePerformanceBonus;
            StatBonus = statBonus;
        }

        public int ActiveSkillPerformanceBonus { get; private set; }
        public int PassivePerformanceBonus { get; private set; }
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
                ranks[index] = new AscensionRankData
                {
                    Rank = rank.Rank,
                    ActiveSkillPerformanceBonus = rank.ActiveSkillPerformanceBonus,
                    PassivePerformanceBonus = rank.PassivePerformanceBonus,
                    StatBonus = Clone(rank.StatBonus)
                };
            }

            var stats = new StatBlock[source.LevelCurve.StatsByLevel.Length];
            for (var index = 0; index < stats.Length; index++) stats[index] = Clone(source.LevelCurve.StatsByLevel[index]);
            var materials = new MaterialRequirementData[source.Awakening.Materials.Length];
            for (var index = 0; index < materials.Length; index++)
                materials[index] = new MaterialRequirementData { MaterialId = source.Awakening.Materials[index].MaterialId, Amount = source.Awakening.Materials[index].Amount };

            return new CharacterData
            {
                Id = source.Id,
                DisplayName = source.DisplayName,
                Element = source.Element,
                BaseRarity = source.BaseRarity,
                BaseStats = Clone(source.BaseStats),
                ActiveSkillId = source.ActiveSkillId,
                LeaderSkillId = source.LeaderSkillId,
                PassiveId = source.PassiveId,
                Tags = source.Tags == null ? null : (string[])source.Tags.Clone(),
                LevelCurve = new ProgressionCurveData
                {
                    MaxLevel = source.LevelCurve.MaxLevel,
                    ExperienceRequiredByLevel = (int[])source.LevelCurve.ExperienceRequiredByLevel.Clone(),
                    StatsByLevel = stats
                },
                Ascension = new AscensionConfigurationData
                {
                    OverflowUniversalResourceAmount = source.Ascension.OverflowUniversalResourceAmount,
                    Ranks = ranks
                },
                Awakening = new AwakeningRequirementData
                {
                    RequiredLevel = source.Awakening.RequiredLevel,
                    GoldCost = source.Awakening.GoldCost,
                    Materials = materials
                },
                BaseVisuals = Clone(source.BaseVisuals),
                AwakenedVisuals = Clone(source.AwakenedVisuals)
            };
        }

        internal static VisualSnapshot Snapshot(VisualReferenceSet source)
        {
            return new VisualSnapshot(source.PortraitKey, source.CardArtKey, source.ModelKey, source.VfxKey);
        }

        private static StatBlock Clone(StatBlock source)
        {
            if (source == null) return null;
            return new StatBlock { Hp = source.Hp, Attack = source.Attack, Recovery = source.Recovery };
        }

        private static VisualReferenceSet Clone(VisualReferenceSet source)
        {
            return new VisualReferenceSet { PortraitKey = source.PortraitKey, CardArtKey = source.CardArtKey, ModelKey = source.ModelKey, VfxKey = source.VfxKey };
        }
    }

    internal static class ProgressionCalculation
    {
        internal static StatSnapshot DeriveStats(CharacterData data, int level, int ascension)
        {
            var baseStats = data.LevelCurve.StatsByLevel[level - 1];
            long hp = baseStats.Hp;
            long attack = baseStats.Attack;
            long recovery = baseStats.Recovery;
            for (var index = 0; index < data.Ascension.Ranks.Length; index++)
            {
                var rank = data.Ascension.Ranks[index];
                if (rank.Rank > ascension || rank.StatBonus == null) continue;
                hp += rank.StatBonus.Hp;
                attack += rank.StatBonus.Attack;
                recovery += rank.StatBonus.Recovery;
            }

            return new StatSnapshot(ToInt(hp), ToInt(attack), ToInt(recovery));
        }

        internal static AscensionEffectSnapshot DeriveEffects(CharacterData data, int ascension)
        {
            long active = 0;
            long passive = 0;
            long hp = 0;
            long attack = 0;
            long recovery = 0;
            for (var index = 0; index < data.Ascension.Ranks.Length; index++)
            {
                var rank = data.Ascension.Ranks[index];
                if (rank.Rank > ascension) continue;
                active += rank.ActiveSkillPerformanceBonus;
                passive += rank.PassivePerformanceBonus;
                if (rank.StatBonus == null) continue;
                hp += rank.StatBonus.Hp;
                attack += rank.StatBonus.Attack;
                recovery += rank.StatBonus.Recovery;
            }

            return new AscensionEffectSnapshot(ToInt(active), ToInt(passive), new StatSnapshot(ToInt(hp), ToInt(attack), ToInt(recovery)));
        }

        private static int ToInt(long value)
        {
            if (value < 0 || value > int.MaxValue) throw new InvalidOperationException("Authored progression values overflow integer stats.");
            return (int)value;
        }
    }

    internal static class ProgressionValidation
    {
        internal static void Validate(CharacterData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (string.IsNullOrWhiteSpace(data.Id)) throw new ArgumentException("Character ID is required.", "data");
            if (data.BaseRarity < (int)Rarity.One || data.BaseRarity > (int)Rarity.Five)
                throw new ArgumentException("Character base rarity must be between one and five.", "data");
            ValidateCurve(data.LevelCurve);
            ValidateAscension(data.Ascension);
            ValidateAwakening(data.Awakening, data.LevelCurve.MaxLevel);
            if (data.BaseVisuals == null || data.AwakenedVisuals == null)
                throw new ArgumentException("Character visuals cannot be null.", "data");
        }

        private static void ValidateCurve(ProgressionCurveData curve)
        {
            if (curve == null || curve.MaxLevel < 1 || curve.ExperienceRequiredByLevel == null || curve.StatsByLevel == null)
                throw new ArgumentException("Progression curve is required and must define a positive max level.", "data");
            if (curve.ExperienceRequiredByLevel.Length != curve.MaxLevel - 1 || curve.StatsByLevel.Length != curve.MaxLevel)
                throw new ArgumentException("Progression curve must define one stat block per level and one cumulative threshold per level after one.", "data");

            var previous = 0;
            for (var index = 0; index < curve.ExperienceRequiredByLevel.Length; index++)
            {
                var threshold = curve.ExperienceRequiredByLevel[index];
                if (threshold <= previous) throw new ArgumentException("Progression experience thresholds must be strictly increasing positive totals.", "data");
                previous = threshold;
            }

            for (var index = 0; index < curve.StatsByLevel.Length; index++)
            {
                var stats = curve.StatsByLevel[index];
                if (stats == null || stats.Hp < 0 || stats.Attack < 0 || stats.Recovery < 0)
                    throw new ArgumentException("Progression stats cannot be null or negative.", "data");
            }
        }

        private static void ValidateAscension(AscensionConfigurationData ascension)
        {
            if (ascension == null || ascension.Ranks == null || ascension.Ranks.Length != 5 || ascension.OverflowUniversalResourceAmount <= 0)
                throw new ArgumentException("Ascension must define five ranks and a positive overflow conversion.", "data");
            var seen = new bool[6];
            for (var index = 0; index < ascension.Ranks.Length; index++)
            {
                var rank = ascension.Ranks[index];
                if (rank == null || rank.Rank < 1 || rank.Rank > 5 || seen[rank.Rank])
                    throw new ArgumentException("Ascension ranks must contain each authored rank from one through five exactly once.", "data");
                if (rank.ActiveSkillPerformanceBonus < 0 || rank.PassivePerformanceBonus < 0 ||
                    (rank.StatBonus != null && (rank.StatBonus.Hp < 0 || rank.StatBonus.Attack < 0 || rank.StatBonus.Recovery < 0)))
                    throw new ArgumentException("Ascension effects cannot be negative.", "data");
                seen[rank.Rank] = true;
            }
        }

        private static void ValidateAwakening(AwakeningRequirementData awakening, int maxLevel)
        {
            if (awakening == null || awakening.Materials == null) throw new ArgumentException("Awakening configuration cannot be null.", "data");
            if (awakening.RequiredLevel == 0 && awakening.GoldCost == 0 && awakening.Materials.Length == 0) return;
            if (awakening.RequiredLevel < 1 || awakening.RequiredLevel > maxLevel || awakening.GoldCost < 0 || awakening.Materials.Length == 0)
                throw new ArgumentException("Awakening requirements are malformed.", "data");
            for (var index = 0; index < awakening.Materials.Length; index++)
            {
                var material = awakening.Materials[index];
                if (material == null || string.IsNullOrWhiteSpace(material.MaterialId) || material.Amount <= 0)
                    throw new ArgumentException("Awakening materials must have a nonblank ID and positive amount.", "data");
                for (var other = 0; other < index; other++)
                    if (string.Equals(awakening.Materials[other].MaterialId, material.MaterialId, StringComparison.Ordinal))
                        throw new ArgumentException("Awakening material IDs must be unique.", "data");
            }
        }
    }
}
