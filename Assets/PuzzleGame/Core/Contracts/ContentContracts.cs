using System;

namespace PuzzleGame.Core.Contracts
{
    public interface IIdentifiedData
    {
        string Id { get; }
    }

    [Serializable]
    public sealed class CharacterData : IIdentifiedData
    {
        public string Id;
        public string DisplayName;
        public ElementType Element;
        public int BaseRarity;
        public StatBlock BaseStats = new StatBlock();
        public string ActiveSkillId;
        public string LeaderSkillId;
        public string PassiveId;
        public string[] Tags = Array.Empty<string>();
        public ProgressionCurveData LevelCurve = new ProgressionCurveData();
        public AscensionConfigurationData Ascension = new AscensionConfigurationData();
        public AwakeningRequirementData Awakening = new AwakeningRequirementData();
        public VisualReferenceSet BaseVisuals = new VisualReferenceSet();
        public VisualReferenceSet AwakenedVisuals = new VisualReferenceSet();

        string IIdentifiedData.Id => Id;

        public static CharacterData CreateForTests(string id, ElementType element, int baseRarity)
        {
            return new CharacterData
            {
                Id = id,
                Element = element,
                BaseRarity = baseRarity,
                BaseStats = new StatBlock(),
                ActiveSkillId = "test-active-skill",
                LevelCurve = new ProgressionCurveData
                {
                    MaxLevel = 1,
                    ExperienceRequiredByLevel = Array.Empty<int>(),
                    StatsByLevel = new[] { new StatBlock() }
                },
                Ascension = new AscensionConfigurationData
                {
                    OverflowUniversalResourceAmount = 1,
                    Ranks = new[]
                    {
                        new AscensionRankData { Rank = 1, ActiveSkillChargeReduction = 1 },
                        new AscensionRankData { Rank = 2, ActiveSkillChargeReduction = 1 },
                        new AscensionRankData { Rank = 3, StatBonus = new StatBlock { Hp = 1 } },
                        new AscensionRankData { Rank = 4, StatBonus = new StatBlock { Attack = 1 } },
                        new AscensionRankData { Rank = 5, StatBonus = new StatBlock { Recovery = 1 } }
                    }
                }
            };
        }
    }

    [Serializable]
    public sealed class ProgressionCurveData
    {
        public int MaxLevel = 1;
        public int[] ExperienceRequiredByLevel = Array.Empty<int>();
        public StatBlock[] StatsByLevel = Array.Empty<StatBlock>();
    }

    [Serializable]
    public sealed class AscensionConfigurationData
    {
        public int OverflowUniversalResourceAmount;
        public AscensionRankData[] Ranks = Array.Empty<AscensionRankData>();
    }

    [Serializable]
    public sealed class AscensionRankData
    {
        public int Rank;
        public int ActiveSkillChargeReduction;
        public StatBlock StatBonus;
    }

    [Serializable]
    public sealed class AwakeningRequirementData
    {
        public int RequiredLevel;
        public int GoldCost;
        public MaterialRequirementData[] Materials = Array.Empty<MaterialRequirementData>();
    }

    [Serializable]
    public sealed class MaterialRequirementData
    {
        public string MaterialId;
        public int Amount;
    }

    [Serializable]
    public sealed class VisualReferenceSet
    {
        public string PortraitKey;
        public string CardArtKey;
        public string ModelKey;
        public string VfxKey;
    }

    [Serializable]
    public sealed class SkillData : IIdentifiedData
    {
        public string Id;
        public string DisplayName;
        public ElementType ChargeElement;
        public int ChargeRequired;
        public SkillEffectData[] Effects = Array.Empty<SkillEffectData>();

        string IIdentifiedData.Id => Id;
    }

    [Serializable]
    public sealed class LeaderSkillData : IIdentifiedData
    {
        public string Id;
        public string DisplayName;
        public ElementType[] RequiredElements = Array.Empty<ElementType>();
        public string[] RequiredTags = Array.Empty<string>();
        public int MinimumComboCount;
        public float MinimumHpPercent;
        public SkillEffectData[] Effects = Array.Empty<SkillEffectData>();

        string IIdentifiedData.Id => Id;
    }

    [Serializable]
    public sealed class PassiveData : IIdentifiedData
    {
        public string Id;
        public SkillEffectData[] Effects = Array.Empty<SkillEffectData>();

        string IIdentifiedData.Id => Id;
    }

    [Serializable]
    public sealed class EnemyData : IIdentifiedData
    {
        public string Id;
        public string DisplayName;
        public ElementType Element;
        public StatBlock BaseStats = new StatBlock();
        public int InitialCountdown = 1;
        public EnemyActionData[] Actions = Array.Empty<EnemyActionData>();
        public EnemyThresholdTriggerData[] ThresholdTriggers = Array.Empty<EnemyThresholdTriggerData>();
        public VisualReferenceSet Visuals = new VisualReferenceSet();

        string IIdentifiedData.Id => Id;
    }

    [Serializable]
    public sealed class EnemyActionData
    {
        public string Id;
        public int ResetCountdown = 1;
        public EnemyEffectData[] Effects = Array.Empty<EnemyEffectData>();
    }

    [Serializable]
    public sealed class EnemyThresholdTriggerData
    {
        public string Id;
        public float HpThresholdPercent;
        public EnemyEffectData Effect = new EnemyEffectData();
    }

    [Serializable]
    public sealed class StageData : IIdentifiedData
    {
        public string Id;
        public string ChapterId;
        public string DisplayName;
        public bool IsPermanentStory;
        public WaveData[] Waves = Array.Empty<WaveData>();
        public StarObjectiveData[] StarObjectives = Array.Empty<StarObjectiveData>();
        public RewardData[] ClearRewards = Array.Empty<RewardData>();

        string IIdentifiedData.Id => Id;
    }

    [Serializable]
    public sealed class WaveData : IIdentifiedData
    {
        public string Id;
        public string[] EnemyIds = Array.Empty<string>();
        public RewardData[] Rewards = Array.Empty<RewardData>();

        string IIdentifiedData.Id => Id;
    }

    [Serializable]
    public sealed class StarObjectiveData
    {
        public StarObjectiveType Type;
        public float HpThresholdPercent;
        public int MaximumBoardResolutionCount;
    }

    [Serializable]
    public sealed class BannerData : IIdentifiedData
    {
        public string Id;
        public string DisplayName;
        public BannerType Type;
        public int SinglePullGemCost = 150;
        public int TenPullGemCost = 1500;
        public WeightedCharacterData[] Characters = Array.Empty<WeightedCharacterData>();
        public BannerStepData[] Steps = Array.Empty<BannerStepData>();
        public string PresentationKey;
        public string RotationId;

        string IIdentifiedData.Id => Id;
    }

    [Serializable]
    public sealed class WeightedCharacterData
    {
        public string CharacterId;
        public int Weight;
        public bool IsFeatured;
    }

    [Serializable]
    public sealed class BannerStepData
    {
        public int PullCount;
        public int GemCost;
        public bool GuaranteedFiveStarFeaturedBoost;
        public bool GuaranteedFeaturedFiveStar;
    }

    [Serializable]
    public sealed class EventData : IIdentifiedData
    {
        public string Id;
        public string DisplayName;
        public string[] StageIds = Array.Empty<string>();
        public RewardData[] MilestoneRewards = Array.Empty<RewardData>();
        public string PresentationKey;

        string IIdentifiedData.Id => Id;
    }

    [Serializable]
    public sealed class RotationScheduleData : IIdentifiedData
    {
        public string Id;
        public string ContentId;
        public ContentType ContentType;
        public bool IsEnabled = true;
        public int Priority;
        public DateTimeOffset Start;
        public DateTimeOffset End;
        public int[] RecurringWeekdays = Array.Empty<int>();
        public int StartMinuteOfDay;
        public int EndMinuteOfDay;

        string IIdentifiedData.Id => Id;
    }

    [Serializable]
    public sealed class RewardData : IIdentifiedData
    {
        public string Id;
        public RewardType Type;
        public string ItemId;
        public int Amount;

        string IIdentifiedData.Id => Id;
    }
}
