using System;

namespace PuzzleGame.Core.Contracts
{
    public enum OrbType
    {
        Fire,
        Water,
        Nature,
        Light,
        Dark,
        Heart
    }

    public enum ElementType
    {
        Fire,
        Water,
        Nature,
        Light,
        Dark,
        Heart
    }

    public enum Rarity
    {
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Awakened = 6
    }

    public enum BannerType
    {
        Standard,
        Featured,
        StepUp,
        GatherIn
    }

    public enum ContentType
    {
        StandardBanner,
        FeaturedBanner,
        GatherInBanner,
        StepUpBanner,
        EventChapter,
        DailyDungeon,
        WeeklyDungeon,
        AwakeningMaterialStage,
        Tower,
        BossRush
    }

    public enum SkillEffectType
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
        DirectDamage
    }

    public enum EnemyEffectType
    {
        Damage,
        ConvertOrbs,
        LockOrbs,
        Poison,
        Hazard,
        Blocker,
        Bind,
        ReduceMoveTime,
        DamageAbsorb,
        ComboShield,
        Enrage,
        ManipulateCountdown
    }

    public enum RewardType
    {
        Gold,
        Gems,
        Tickets,
        Character,
        Material,
        UniversalDuplicateResource,
        EventCurrency
    }

    public enum StarObjectiveType
    {
        Clear,
        FinishAboveHpThreshold,
        ClearWithinBoardResolutionCount
    }

    [Serializable]
    public sealed class StatBlock
    {
        public int Hp;
        public int Attack;
        public int Recovery;
    }

    [Serializable]
    public sealed class EffectPayloadData
    {
        public int Amount;
        public float Multiplier;
        public float DurationSeconds;
        public int TurnCount;
        public int ComboCount;
        public OrbType SourceOrb;
        public OrbType TargetOrb;
        public string TargetId;
        public string[] Tags = Array.Empty<string>();
    }

    [Serializable]
    public sealed class SkillEffectData
    {
        public SkillEffectType Type;
        public EffectPayloadData Payload = new EffectPayloadData();
    }

    [Serializable]
    public sealed class EnemyEffectData
    {
        public EnemyEffectType Type;
        public EffectPayloadData Payload = new EffectPayloadData();
    }
}
