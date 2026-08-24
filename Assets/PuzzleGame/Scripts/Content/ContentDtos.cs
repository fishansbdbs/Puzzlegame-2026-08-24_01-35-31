using System;
using System.Collections.Generic;

namespace PuzzleGame.Presentation.Content
{
    // ---------------------------------------------------------------------
    // DRAFT content schemas (JSON, parsed with JsonUtility).
    //
    // Field sets follow the master design spec's data-contract list
    // (CharacterData, EnemyData, StageData, WaveData, BannerData, EventData,
    // RotationScheduleData, RewardData). Codex owns the authoritative
    // schemas; when they land, migrate these files/loaders instead of
    // duplicating rules. Rates and costs in banner files are DISPLAY data
    // and must be kept in sync with core gacha configuration.
    //
    // JSON files live under Assets/PuzzleGame/Resources/Content/<category>/
    // and are discovered with Resources.LoadAll, so adding content never
    // requires code changes.
    // ---------------------------------------------------------------------

    [Serializable]
    public class SkillDto
    {
        public string name;
        public string desc;
        public int chargeMax = 20;
    }

    [Serializable]
    public class NamedEffectDto
    {
        public string name;
        public string desc;
    }

    [Serializable]
    public class AwakenedFormDto
    {
        public string epithet;          // optional awakened flavor title
        public string abilityName;
        public string abilityDesc;
        public int hpBonus;
        public int atkBonus;
        public int recBonus;
        public string artRef;
        public string vfxRef;
    }

    [Serializable]
    public class CharacterDto
    {
        public string id;
        public string name;
        public string epithet;
        public string element;          // fire | water | nature | light | dark
        public int rarity;              // base rarity 1..5
        public List<string> tags = new List<string>();
        public int maxLevel = 50;
        public int hp;                  // stats at max level, pre-awakening
        public int atk;
        public int rec;
        public NamedEffectDto leaderSkill;
        public SkillDto activeSkill;
        public NamedEffectDto passive;
        public AwakenedFormDto awakened;
        public string artRef;
        public string vfxRef;
        public string flavor;
    }

    [Serializable]
    public class CharacterFileDto
    {
        public List<CharacterDto> characters = new List<CharacterDto>();
    }

    // ------------------------------ Enemies -------------------------------

    [Serializable]
    public class EnemyActionDto
    {
        public string name;
        public string desc;             // player-facing telegraph text
        /// <summary>
        /// damage | bigDamage | convert | lock | poison | block | bind |
        /// timerDown | absorb | comboShield | enrage | heal | summon | taunt
        /// </summary>
        public string type = "damage";
        public int amount;              // damage / heal magnitude hint
        public int count;               // orbs affected / turns bound etc.
        public string color;            // orb color parameter where relevant
    }

    [Serializable]
    public class EnemyDto
    {
        public string id;
        public string name;
        public string element;
        public bool boss;
        public int hp;                  // baseline; stages scale via multipliers
        public int atk;
        public int countdown = 3;
        public string artRef;
        public string flavor;
        public List<EnemyActionDto> actions = new List<EnemyActionDto>();
    }

    [Serializable]
    public class EnemyFileDto
    {
        public List<EnemyDto> enemies = new List<EnemyDto>();
    }

    // ------------------------------ Stages --------------------------------

    [Serializable]
    public class WaveEnemyDto
    {
        public string enemyId;
        public float hpMult = 1f;
        public float atkMult = 1f;
        public int countdownOverride = -1;
    }

    [Serializable]
    public class WaveDto
    {
        public List<WaveEnemyDto> enemies = new List<WaveEnemyDto>();
    }

    [Serializable]
    public class RewardItemDto
    {
        public string id;
        public int count = 1;
    }

    [Serializable]
    public class StageRewardDto
    {
        public int gold;
        public int gems;
        public List<RewardItemDto> items = new List<RewardItemDto>();
        public string firstClearBonus;
    }

    [Serializable]
    public class StageDto
    {
        public string id;
        public int stageNumber;
        public string name;
        /// <summary>normal | miniboss | boss | story</summary>
        public string kind = "normal";
        public string background;
        public List<WaveDto> waves = new List<WaveDto>();
        public List<string> modifiers = new List<string>();
        /// <summary>Finish above this fraction of party HP for star 2.</summary>
        public float hpThresholdStar = 0.5f;
        /// <summary>Clear within this many board resolutions for star 3.</summary>
        public int resolutionsStar = 15;
        public string dialogueBefore;   // dialogue scene id or empty
        public string dialogueAfter;
        public StageRewardDto rewards = new StageRewardDto();
        public int staminaCost;         // 0 for permanent story stages
    }

    [Serializable]
    public class ChapterDto
    {
        public int chapterNumber;
        public string title;
        public string theme;            // background/enemy theme key
        public string blurb;            // silly chapter tagline
        public List<StageDto> stages = new List<StageDto>();
    }

    [Serializable]
    public class ChapterFileDto
    {
        public ChapterDto chapter;
    }

    // ------------------------------ Dialogue ------------------------------

    [Serializable]
    public class DialogueLineDto
    {
        public string speaker;
        public string portrait;         // art ref; empty = narrator
        public string text;
        /// <summary>left | right</summary>
        public string side = "left";
    }

    [Serializable]
    public class DialogueSceneDto
    {
        public string id;
        public List<DialogueLineDto> lines = new List<DialogueLineDto>();
    }

    [Serializable]
    public class DialogueFileDto
    {
        public List<DialogueSceneDto> scenes = new List<DialogueSceneDto>();
    }

    // ------------------------------ Banners -------------------------------

    [Serializable]
    public class BannerStepDto
    {
        public int gemCost;
        public int pullCount;
        public string guarantee;        // display text; logic lives in core
    }

    [Serializable]
    public class BannerRatesDto
    {
        // Display-only copies of core rate config, in percent.
        public float fiveStar = 4f;
        public float fourStar = 16f;
        public float threeStar = 40f;
        public float twoStar = 25f;
        public float oneStar = 15f;
        public float featuredShareOfFiveStar = 50f;
    }

    [Serializable]
    public class BannerDto
    {
        public string id;
        public string name;
        /// <summary>standard | featured | gatherIn | stepUp</summary>
        public string kind = "standard";
        public string desc;
        public string packArt;          // pack theme key for the rip presentation
        public List<string> featured = new List<string>();
        public int singleCost = 150;
        public int multiCost = 1500;
        public BannerRatesDto rates = new BannerRatesDto();
        public string guarantee;        // headline guarantee text
        public List<BannerStepDto> steps = new List<BannerStepDto>();
        public int rotationIndex;       // Gather-In rotation (1-based)
        /// <summary>Pool filter: only characters holding any of these tags. Empty = all.</summary>
        public List<string> poolTags = new List<string>();
        public int minPoolRarity = 1;
    }

    [Serializable]
    public class BannerFileDto
    {
        public List<BannerDto> banners = new List<BannerDto>();
    }

    // ------------------------------ Events --------------------------------

    [Serializable]
    public class EventDto
    {
        public string id;
        public string name;
        /// <summary>eventChapter | materialDungeon | awakeningStage | challengeTower | bossRush</summary>
        public string kind = "eventChapter";
        public string desc;
        public string theme;
        public string tokenId;          // event currency, if any
        /// <summary>Chapter file style stage list for the event.</summary>
        public List<StageDto> stages = new List<StageDto>();
        public string rewardTableId;
    }

    [Serializable]
    public class EventFileDto
    {
        public List<EventDto> events = new List<EventDto>();
    }

    // ------------------------------ Rewards -------------------------------

    [Serializable]
    public class RewardEntryDto
    {
        public string itemId;
        public int count = 1;
        public int weight = 1;
    }

    [Serializable]
    public class RewardTableDto
    {
        public string id;
        public List<RewardEntryDto> entries = new List<RewardEntryDto>();
    }

    [Serializable]
    public class RewardFileDto
    {
        public List<RewardTableDto> tables = new List<RewardTableDto>();
    }

    // ------------------------------ Schedule ------------------------------

    [Serializable]
    public class ScheduleEntryDto
    {
        public string id;
        /// <summary>banner | eventChapter | materialDungeon | awakeningStage | challengeTower | bossRush</summary>
        public string kind = "banner";
        public string targetId;         // banner/event id this entry activates
        public string name;
        public string desc;
        public bool permanent;
        public string startUtc;         // ISO-8601, empty when permanent
        public string endUtc;
        /// <summary>none | daily | weekly</summary>
        public string recurrence = "none";
        /// <summary>0=Sunday..6=Saturday for weekly recurrence.</summary>
        public int weekday;
    }

    [Serializable]
    public class ScheduleFileDto
    {
        public List<ScheduleEntryDto> entries = new List<ScheduleEntryDto>();
    }
}
