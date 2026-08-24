using System;
using System.Collections.Generic;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Board;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Economy;
using PuzzleGame.Core.Gacha;
using PuzzleGame.Core.Persistence;
using PuzzleGame.Core.Progression;
using PuzzleGame.Core.Scheduling;
using PuzzleGame.Core.Stages;

namespace PuzzleGame.Core.Sample
{
    /// <summary>Builds the small deterministic content/runtime graph used by the playable sample.</summary>
    public static class VerticalSliceFactory
    {
        public const int DefaultSeed = 24082026;
        public const string FeaturedCharacterId = "hero-ember";
        public const string StageId = "stage-vertical-slice";
        public const string EnemyId = "boss-prism-warden";
        public static readonly DateTimeOffset SampleScheduleTime =
            new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.FromHours(-5));

        public static VerticalSliceSample Create(int seed = DefaultSeed)
        {
            var skills = CreateSkills();
            var characters = CreateCharacters(skills);
            var leader = CreateLeaderSkill();
            var passive = new PassiveData
            {
                Id = "passive-ember-focus",
                Effects = new[]
                {
                    new SkillEffectData
                    {
                        Type = SkillEffectType.AttackBoost,
                        Payload = new EffectPayloadData { Multiplier = 1.25f }
                    }
                }
            };
            var enemyData = CreateEnemy();
            var stageData = CreateStage();
            var standardBanner = CreateStandardBanner();
            var gatherOne = CreateGatherInBanner("banner-gather-one", "rotation-one", true);
            var gatherTwo = CreateGatherInBanner("banner-gather-two", "rotation-two", false);
            var eventData = CreateEvent();
            var schedules = CreateSchedules();

            ValidateAll(characters, skills, leader, passive, enemyData, stageData, standardBanner, gatherOne, gatherTwo,
                eventData, schedules);
            var stageCatalog = new StageCatalog(new[] { stageData }, new[] { enemyData });
            var ownedProgress = new CharacterProgress[characters.Length];
            for (var index = 0; index < ownedProgress.Length; index++) ownedProgress[index] = new CharacterProgress(characters[index], skills[index]);
            var ownedCollection = new SummonCollection(ownedProgress);
            var ownedHero = ownedCollection.GetProgress(FeaturedCharacterId);
            ProgressionService.ApplyExperience(ownedHero, 100);
            ProgressionService.ApplyDuplicate(ownedHero, new Wallet());

            var generatedBoard = new BoardGenerator(new DeterministicRandom(seed ^ 0x243f6a88)).Generate();

            var board = CreateOpeningBoard();
            var party = CreateParty(characters, skills, leader, passive, ownedCollection);
            party.ApplyDamage(100);
            var stageSession = stageCatalog.CreateSession(stageData.Id);
            var enemy = stageSession.CurrentEnemies[0];
            var battleContext = new BattleContext(board, party, enemy);
            var activeSkillExample = ActivateLeaderSkill(party, battleContext);
            var orbSource = new SampleOrbSource(seed ^ 0x13198a2e);

            var previewParty = CreateParty(characters, skills, leader, passive, ownedCollection);
            previewParty.ApplyDamage(100);
            var previewSession = stageCatalog.CreateSession(stageData.Id);
            var previewEnemy = previewSession.CurrentEnemies[0];
            previewEnemy.ApplyDamage(previewEnemy.CurrentHp - (previewEnemy.MaxHp / 2 + 1));
            var previewContext = new BattleContext(CreateOpeningBoard(), previewParty, previewEnemy);
            ActivateLeaderSkill(previewParty, previewContext);
            var openingResolution = new BoardResolver().Resolve(previewContext.Board, new SampleOrbSource(seed ^ 0x13198a2e));
            var openingTurn = previewSession.CompleteBoardResolution(new BattleEngine(), openingResolution, previewContext);

            var bossMechanicExample = openingTurn.Mechanics;
            var objectiveExample = StageObjectiveEvaluator.Evaluate(stageData, new StageResult(true, .75f, 2));

            var ascensionWallet = new Wallet();
            var ascensionProgress = new CharacterProgress(characters[0], skills[0]);
            var ascensionExample = ProgressionService.ApplyDuplicate(ascensionProgress, ascensionWallet);

            var awakeningProgress = new CharacterProgress(characters[0], skills[0]);
            ProgressionService.ApplyExperience(awakeningProgress, 250);
            var awakeningWallet = new Wallet();
            awakeningWallet.Add(WalletCurrencies.Gold, 500);
            var awakeningMaterials = new MaterialInventory();
            awakeningMaterials.Add("material-prism-core", 5);
            var awakeningExample = ProgressionService.Awaken(awakeningProgress, awakeningWallet, awakeningMaterials);

            var definitions = CreateSummonDefinitions(characters, skills);
            var standardWallet = CreateGemWallet(5000);
            var standardState = new BannerRuntimeState(standardBanner.Id, standardBanner.RotationId, 0);
            var standardService = new SummonService(standardBanner, definitions, standardWallet,
                new DeterministicRandom(seed ^ unchecked((int)0xa4093822u)), null, standardState);
            var standardSummon = standardService.PurchaseAndRoll(1);

            var gatherOneWallet = CreateGemWallet(5000);
            var gatherOneState = new BannerRuntimeState(gatherOne.Id, gatherOne.RotationId, gatherOne.Steps.Length, gatherOne.Steps.Length - 1);
            var gatherOneService = new SummonService(gatherOne, definitions, gatherOneWallet,
                new DeterministicRandom(seed ^ 0x299f31d0), null, gatherOneState);
            var gatherOneSummon = gatherOneService.PurchaseAndRoll();

            var gatherTwoWallet = CreateGemWallet(5000);
            var gatherTwoState = new BannerRuntimeState(gatherTwo.Id, gatherTwo.RotationId, gatherTwo.Steps.Length, gatherTwo.Steps.Length - 1);
            var gatherTwoService = new SummonService(gatherTwo, definitions, gatherTwoWallet,
                new DeterministicRandom(seed ^ 0x082efa98), null, gatherTwoState);
            var gatherTwoSummon = gatherTwoService.PurchaseAndRoll();
            var packFlow = PackSummonFlow.Create(gatherTwoSummon);

            var saveProfile = CreateSaveProfile(characters, standardState, gatherOneState, gatherTwoState);
            var memoryStorage = new SampleMemorySaveStorage();
            var saveService = new SaveService(memoryStorage);
            saveService.Save(saveProfile);
            var serializedSave = memoryStorage.Content;
            var saveRoundTrip = saveService.LoadOrCreate();

            var scheduler = new RotationScheduler(new FixedClock(SampleScheduleTime));
            var activeContent = scheduler.GetActive(schedules);

            return new VerticalSliceSample(seed, characters, skills, leader, passive, ownedCollection, enemyData, stageData, stageCatalog,
                standardBanner, gatherOne, gatherTwo, eventData, schedules, new BoardSnapshot(generatedBoard),
                board, orbSource, party, enemy, stageSession, battleContext, activeSkillExample,
                openingResolution.CascadeLayers, openingTurn, ascensionProgress, ascensionExample,
                awakeningProgress, awakeningExample, bossMechanicExample, objectiveExample,
                standardState, standardSummon, gatherOneState, gatherOneSummon, gatherTwoState,
                gatherTwoSummon, packFlow, saveRoundTrip, serializedSave, activeContent);
        }

        private static CharacterData[] CreateCharacters(SkillData[] skills)
        {
            var ids = new[] { FeaturedCharacterId, "hero-tide", "hero-grove", "hero-dawn", "hero-void" };
            var names = new[] { "Ember Vanguard", "Tide Keeper", "Grove Sentinel", "Dawn Seer", "Void Ranger" };
            var elements = new[] { ElementType.Fire, ElementType.Water, ElementType.Nature, ElementType.Light, ElementType.Dark };
            var rarities = new[] { 5, 4, 3, 4, 5 };
            var result = new CharacterData[ids.Length];
            for (var index = 0; index < result.Length; index++)
            {
                var baseHp = 1000 + index * 100;
                var baseAttack = 100 + index * 10;
                var baseRecovery = 50 + index * 5;
                result[index] = new CharacterData
                {
                    Id = ids[index],
                    DisplayName = names[index],
                    Element = elements[index],
                    BaseRarity = rarities[index],
                    BaseStats = new StatBlock { Hp = baseHp, Attack = baseAttack, Recovery = baseRecovery },
                    ActiveSkillId = skills[index].Id,
                    LeaderSkillId = index == 0 ? "leader-prismatic-charge" : string.Empty,
                    PassiveId = index == 0 ? "passive-ember-focus" : string.Empty,
                    Tags = new[] { "vertical-slice", index == 0 ? "vanguard" : "ally" },
                    LevelCurve = new ProgressionCurveData
                    {
                        MaxLevel = 3,
                        ExperienceRequiredByLevel = new[] { 100, 250 },
                        StatsByLevel = new[]
                        {
                            new StatBlock { Hp = baseHp, Attack = baseAttack, Recovery = baseRecovery },
                            new StatBlock { Hp = baseHp + 100, Attack = baseAttack + 15, Recovery = baseRecovery + 10 },
                            new StatBlock { Hp = baseHp + 250, Attack = baseAttack + 35, Recovery = baseRecovery + 20 }
                        }
                    },
                    Ascension = CreateAscension(),
                    Awakening = index == 0
                        ? new AwakeningRequirementData
                        {
                            RequiredLevel = 3,
                            GoldCost = 300,
                            Materials = new[] { new MaterialRequirementData { MaterialId = "material-prism-core", Amount = 2 } }
                        }
                        : new AwakeningRequirementData(),
                    BaseVisuals = new VisualReferenceSet
                    {
                        PortraitKey = ids[index] + "/portrait/base",
                        CardArtKey = ids[index] + "/card/base",
                        ModelKey = ids[index] + "/model/base",
                        VfxKey = ids[index] + "/vfx/base"
                    },
                    AwakenedVisuals = new VisualReferenceSet
                    {
                        PortraitKey = ids[index] + "/portrait/awakened",
                        CardArtKey = ids[index] + "/card/awakened",
                        ModelKey = ids[index] + "/model/awakened",
                        VfxKey = ids[index] + "/vfx/awakened"
                    }
                };
            }
            return result;
        }

        private static AscensionConfigurationData CreateAscension()
        {
            return new AscensionConfigurationData
            {
                OverflowUniversalResourceAmount = 10,
                Ranks = new[]
                {
                    new AscensionRankData { Rank = 1, ActiveSkillChargeReduction = 1 },
                    new AscensionRankData { Rank = 2, ActiveSkillChargeReduction = 1 },
                    new AscensionRankData { Rank = 3, StatBonus = new StatBlock { Hp = 50 } },
                    new AscensionRankData { Rank = 4, StatBonus = new StatBlock { Attack = 10 } },
                    new AscensionRankData { Rank = 5, StatBonus = new StatBlock { Recovery = 5 } }
                }
            };
        }

        private static SkillData[] CreateSkills()
        {
            var elements = new[] { ElementType.Fire, ElementType.Water, ElementType.Nature, ElementType.Light, ElementType.Dark };
            var result = new SkillData[elements.Length];
            for (var index = 0; index < result.Length; index++)
            {
                result[index] = new SkillData
                {
                    Id = "active-" + elements[index].ToString().ToLowerInvariant(),
                    DisplayName = index == 0 ? "Ember Rally" : elements[index] + " Pulse",
                    ChargeElement = elements[index],
                    ChargeRequired = 4 + index,
                    Effects = index == 0
                        ? new[]
                        {
                            new SkillEffectData
                            {
                                Type = SkillEffectType.AttackBoost,
                                Payload = new EffectPayloadData { Multiplier = 1.2f, TurnCount = 3 }
                            }
                        }
                        : new[]
                        {
                            new SkillEffectData
                            {
                                Type = SkillEffectType.Heal,
                                Payload = new EffectPayloadData { Amount = 25 + index }
                            }
                        }
                };
            }
            return result;
        }

        private static LeaderSkillData CreateLeaderSkill()
        {
            return new LeaderSkillData
            {
                Id = "leader-prismatic-charge",
                DisplayName = "Prismatic Charge",
                RequiredElements = new[] { ElementType.Fire, ElementType.Water },
                RequiredTags = new[] { "vertical-slice" },
                Effects = new[]
                {
                    new SkillEffectData
                    {
                        Type = SkillEffectType.AttackBoost,
                        Payload = new EffectPayloadData { Multiplier = 1.5f }
                    }
                }
            };
        }

        private static EnemyData CreateEnemy()
        {
            return new EnemyData
            {
                Id = EnemyId,
                DisplayName = "Prism Warden",
                Element = ElementType.Nature,
                BaseStats = new StatBlock { Hp = 5000, Attack = 100, Recovery = 0 },
                InitialCountdown = 1,
                Actions = new[]
                {
                    new EnemyActionData
                    {
                        Id = "boss-strike",
                        ResetCountdown = 2,
                        Effects = new[]
                        {
                            new EnemyEffectData
                            {
                                Type = EnemyEffectType.Damage,
                                Payload = new EffectPayloadData { Amount = 25 }
                            }
                        }
                    }
                },
                ThresholdTriggers = new[]
                {
                    new EnemyThresholdTriggerData
                    {
                        Id = "boss-enrage-half",
                        HpThresholdPercent = 50f,
                        Effect = new EnemyEffectData
                        {
                            Type = EnemyEffectType.Enrage,
                            Payload = new EffectPayloadData { Multiplier = 1.75f, TurnCount = 2 }
                        }
                    }
                },
                Visuals = new VisualReferenceSet
                {
                    PortraitKey = "boss-prism-warden/portrait",
                    CardArtKey = "boss-prism-warden/card",
                    ModelKey = "boss-prism-warden/model",
                    VfxKey = "boss-prism-warden/vfx"
                }
            };
        }

        private static StageData CreateStage()
        {
            return new StageData
            {
                Id = StageId,
                ChapterId = "chapter-sample",
                DisplayName = "Prism Warden Trial",
                IsPermanentStory = true,
                Waves = new[] { new WaveData { Id = "wave-prism-warden", EnemyIds = new[] { EnemyId } } },
                StarObjectives = new[]
                {
                    new StarObjectiveData { Type = StarObjectiveType.Clear },
                    new StarObjectiveData { Type = StarObjectiveType.FinishAboveHpThreshold, HpThresholdPercent = .5f },
                    new StarObjectiveData { Type = StarObjectiveType.ClearWithinBoardResolutionCount, MaximumBoardResolutionCount = 3 }
                },
                ClearRewards = new[]
                {
                    new RewardData { Id = "reward-stage-gold", Type = RewardType.Gold, Amount = 500 },
                    new RewardData { Id = "reward-stage-core", Type = RewardType.Material, ItemId = "material-prism-core", Amount = 1 }
                }
            };
        }

        private static BannerData CreateStandardBanner()
        {
            return new BannerData
            {
                Id = "banner-standard",
                DisplayName = "Standard Pack",
                Type = BannerType.Standard,
                RotationId = "evergreen",
                PresentationKey = "pack/standard",
                Characters = CreatePool(),
                Steps = Array.Empty<BannerStepData>()
            };
        }

        private static BannerData CreateGatherInBanner(string id, string rotationId, bool boostedFinal)
        {
            var steps = new BannerStepData[10];
            for (var index = 0; index < steps.Length; index++)
            {
                steps[index] = new BannerStepData { PullCount = index + 1, GemCost = (index + 1) * 50 };
            }
            steps[steps.Length - 1].GuaranteedFiveStarFeaturedBoost = boostedFinal;
            steps[steps.Length - 1].GuaranteedFeaturedFiveStar = !boostedFinal;
            return new BannerData
            {
                Id = id,
                DisplayName = boostedFinal ? "Gather-In Rotation One" : "Gather-In Rotation Two",
                Type = BannerType.GatherIn,
                RotationId = rotationId,
                PresentationKey = boostedFinal ? "pack/gather-one" : "pack/gather-two",
                Characters = CreatePool(),
                Steps = steps
            };
        }

        private static WeightedCharacterData[] CreatePool()
        {
            return new[]
            {
                new WeightedCharacterData { CharacterId = FeaturedCharacterId, Weight = 10, IsFeatured = true },
                new WeightedCharacterData { CharacterId = "hero-tide", Weight = 30 },
                new WeightedCharacterData { CharacterId = "hero-grove", Weight = 35 },
                new WeightedCharacterData { CharacterId = "hero-dawn", Weight = 20 },
                new WeightedCharacterData { CharacterId = "hero-void", Weight = 5 }
            };
        }

        private static EventData CreateEvent()
        {
            return new EventData
            {
                Id = "event-prism-week",
                DisplayName = "Prism Week",
                StageIds = new[] { StageId },
                PresentationKey = "event/prism-week",
                MilestoneRewards = new[] { new RewardData { Id = "reward-event-ticket", Type = RewardType.Tickets, Amount = 1 } }
            };
        }

        private static RotationScheduleData[] CreateSchedules()
        {
            var types = (ContentType[])Enum.GetValues(typeof(ContentType));
            var schedules = new RotationScheduleData[types.Length];
            for (var index = 0; index < types.Length; index++)
            {
                schedules[index] = new RotationScheduleData
                {
                    Id = "schedule-" + index.ToString("00") + "-" + types[index].ToString().ToLowerInvariant(),
                    ContentId = types[index] == ContentType.EventChapter ? "event-prism-week" : "sample-" + types[index].ToString().ToLowerInvariant(),
                    ContentType = types[index],
                    IsEnabled = true,
                    Priority = types.Length - index,
                    Start = SampleScheduleTime.AddDays(-7),
                    End = SampleScheduleTime.AddDays(7),
                    RecurringWeekdays = Array.Empty<int>(),
                    StartMinuteOfDay = 0,
                    EndMinuteOfDay = 0
                };
            }
            return schedules;
        }

        private static BoardState CreateOpeningBoard()
        {
            var board = new BoardState();
            for (var y = 0; y < BoardState.Rows; y++)
            for (var x = 0; x < BoardState.Columns; x++)
                board.Set(x, y, (OrbType)((x + y + 1) % 6));
            for (var x = 0; x < 3; x++)
            {
                board.Set(x, 0, OrbType.Fire);
                board.Set(x, 2, OrbType.Heart);
            }
            for (var x = 3; x < 6; x++) board.Set(x, 4, OrbType.Fire);
            return board;
        }

        private static PartyState CreateParty(CharacterData[] characters, SkillData[] skills,
            LeaderSkillData leader, PassiveData passive, SummonCollection ownedCollection)
        {
            var members = new CharacterRuntime[characters.Length];
            for (var index = 0; index < members.Length; index++)
                members[index] = CharacterBattleFactory.Create(ownedCollection.GetProgress(characters[index].Id), skills[index],
                    index == 0 ? leader : null, index == 0 ? passive : null);
            return new PartyState(members);
        }

        private static SkillResolution ActivateLeaderSkill(PartyState party, BattleContext context)
        {
            var leader = party.Leader;
            leader.AddCharge(leader.ActiveSkill.ChargeRequired);
            return new SkillEngine().Activate(leader, context);
        }

        private static SummonCharacterDefinition[] CreateSummonDefinitions(CharacterData[] characters, SkillData[] skills)
        {
            var definitions = new SummonCharacterDefinition[characters.Length];
            for (var index = 0; index < definitions.Length; index++)
                definitions[index] = new SummonCharacterDefinition(characters[index], skills[index]);
            return definitions;
        }

        private static Wallet CreateGemWallet(int gems)
        {
            var wallet = new Wallet();
            wallet.Add(WalletCurrencies.Gems, gems);
            return wallet;
        }

        private static SaveData CreateSaveProfile(CharacterData[] characters, BannerRuntimeState standard,
            BannerRuntimeState gatherOne, BannerRuntimeState gatherTwo)
        {
            var characterStates = new CharacterProgressSaveData[characters.Length];
            var party = new string[characters.Length];
            for (var index = 0; index < characters.Length; index++)
            {
                characterStates[index] = new CharacterProgressSaveData
                {
                    CharacterId = characters[index].Id,
                    TotalExperience = index == 0 ? 250 : index * 10,
                    Ascension = index == 1 ? 1 : 0,
                    Awakened = index == 0
                };
                party[index] = characters[index].Id;
            }
            return new SaveData
            {
                Version = SaveData.CurrentVersion,
                Wallet = new WalletSaveData
                {
                    Gold = 1200,
                    Gems = 3450,
                    Tickets = 7,
                    UniversalDuplicateResource = 25,
                    EventCurrencies = new[] { new CurrencyBalanceSaveData { CurrencyId = "currency-prism", Balance = 42 } }
                },
                Characters = characterStates,
                Materials = new[] { new MaterialBalanceSaveData { MaterialId = "material-prism-core", Balance = 3 } },
                PartyCharacterIds = party,
                Banners = new[]
                {
                    ToSave(standard), ToSave(gatherOne), ToSave(gatherTwo)
                },
                Stages = new[]
                {
                    new StageProgressSaveData
                    {
                        StageId = StageId,
                        IsCleared = true,
                        Stars = new[] { true, true, true },
                        BestFinalHpBasisPoints = 7500,
                        BestBoardResolutionCount = 2
                    }
                },
                Settings = new SettingsSaveData
                {
                    MusicVolumePercent = 80,
                    EffectsVolumePercent = 90,
                    ReducedMotion = false,
                    VibrationEnabled = true
                }
            };
        }

        private static BannerRuntimeSaveData ToSave(BannerRuntimeState state)
        {
            return new BannerRuntimeSaveData
            {
                BannerId = state.BannerId,
                RotationId = state.RotationId,
                AuthoredStepCount = state.StepCount,
                NextStepIndex = state.NextStepIndex
            };
        }

        private static void ValidateAll(CharacterData[] characters, SkillData[] skills, LeaderSkillData leader, PassiveData passive,
            EnemyData enemy, StageData stage, BannerData standard, BannerData gatherOne, BannerData gatherTwo,
            EventData eventData, RotationScheduleData[] schedules)
        {
            for (var index = 0; index < characters.Length; index++) RequireValid(characters[index]);
            for (var index = 0; index < skills.Length; index++) RequireValid(skills[index]);
            RequireValid(leader);
            RequireValid(passive);
            RequireValid(enemy);
            RequireValid(stage);
            RequireValid(standard);
            RequireValid(gatherOne);
            RequireValid(gatherTwo);
            RequireValid(eventData);
            for (var index = 0; index < schedules.Length; index++) RequireValid(schedules[index]);
        }

        private static void RequireValid(IIdentifiedData data)
        {
            var errors = ContractValidation.Validate(data);
            if (errors.Count > 0) throw new InvalidOperationException(data.Id + ": " + errors[0]);
        }
    }

    /// <summary>
    /// Immutable authored/result snapshots plus the explicit mutable owners needed by the playable adapter.
    /// A sample instance never shares authored arrays or runtime state with another factory call.
    /// </summary>
    public sealed class VerticalSliceSample
    {
        private readonly CharacterData[] characters;
        private readonly SkillData[] skills;
        private readonly LeaderSkillData leader;
        private readonly PassiveData passive;
        private readonly EnemyData enemyDefinition;
        private readonly StageData stage;
        private readonly BannerData standardBanner;
        private readonly BannerData gatherOneBanner;
        private readonly BannerData gatherTwoBanner;
        private readonly EventData eventDefinition;
        private readonly RotationScheduleData[] schedules;
        private readonly IReadOnlyList<CascadeLayer> openingCascades;
        private readonly IReadOnlyList<EnemyEffectSnapshot> bossMechanics;
        private readonly IReadOnlyList<StarResult> objectiveResults;
        private readonly IReadOnlyList<ActiveContent> activeContent;

        internal VerticalSliceSample(int seed, CharacterData[] characters, SkillData[] skills,
            LeaderSkillData leader, PassiveData passive, SummonCollection ownedCharacters, EnemyData enemyDefinition, StageData stage, StageCatalog stageCatalog,
            BannerData standardBanner, BannerData gatherOneBanner, BannerData gatherTwoBanner,
            EventData eventDefinition, RotationScheduleData[] schedules, BoardSnapshot generatedBoard,
            BoardState board, IOrbSource orbSource, PartyState party, EnemyRuntime enemy,
            StageSession stageSession, BattleContext battleContext, SkillResolution activeSkillExample,
            IReadOnlyList<CascadeLayer> openingCascades, BattleTurnResolution openingTurn,
            CharacterProgress ascensionProgress, DuplicateApplicationResult ascensionExample,
            CharacterProgress awakeningProgress, AwakeningResult awakeningExample,
            IReadOnlyList<EnemyEffectSnapshot> bossMechanics, IReadOnlyList<StarResult> objectiveResults,
            BannerRuntimeState standardState, SummonBatch standardSummon,
            BannerRuntimeState gatherOneState, SummonBatch gatherOneSummon,
            BannerRuntimeState gatherTwoState, SummonBatch gatherTwoSummon, PackSummonFlow packFlow,
            SaveLoadResult saveRoundTrip, string serializedSave, IReadOnlyList<ActiveContent> activeContent)
        {
            Seed = seed;
            this.characters = SampleSnapshot.Clone(characters);
            this.skills = SampleSnapshot.Clone(skills);
            this.leader = SampleSnapshot.Clone(leader);
            this.passive = SampleSnapshot.Clone(passive);
            OwnedCharacters = ownedCharacters ?? throw new ArgumentNullException("ownedCharacters");
            this.enemyDefinition = SampleSnapshot.Clone(enemyDefinition);
            this.stage = SampleSnapshot.Clone(stage);
            StageCatalog = stageCatalog ?? throw new ArgumentNullException("stageCatalog");
            this.standardBanner = SampleSnapshot.Clone(standardBanner);
            this.gatherOneBanner = SampleSnapshot.Clone(gatherOneBanner);
            this.gatherTwoBanner = SampleSnapshot.Clone(gatherTwoBanner);
            this.eventDefinition = SampleSnapshot.Clone(eventDefinition);
            this.schedules = SampleSnapshot.Clone(schedules);
            GeneratedBoard = generatedBoard;
            OpeningBoard = new BoardSnapshot(board);
            OrbSource = orbSource;
            Party = party;
            Enemy = enemy;
            StageSession = stageSession;
            BattleContext = battleContext;
            ActiveSkillExample = activeSkillExample;
            this.openingCascades = Copy(openingCascades);
            OpeningTurn = openingTurn;
            AscensionProgress = ascensionProgress;
            AscensionExample = ascensionExample;
            AwakeningProgress = awakeningProgress;
            AwakeningExample = awakeningExample;
            this.bossMechanics = Copy(bossMechanics);
            this.objectiveResults = Copy(objectiveResults);
            StandardState = standardState;
            StandardSummonExample = standardSummon;
            GatherInRotationOneState = gatherOneState;
            GatherInRotationOneSummonExample = gatherOneSummon;
            GatherInRotationTwoState = gatherTwoState;
            GatherInRotationTwoSummonExample = gatherTwoSummon;
            PackFlow = packFlow;
            SaveRoundTrip = saveRoundTrip;
            SerializedSave = serializedSave;
            this.activeContent = Copy(activeContent);
        }

        public int Seed { get; private set; }
        public string FeaturedCharacterId { get { return VerticalSliceFactory.FeaturedCharacterId; } }
        public IReadOnlyList<CharacterData> CharacterCatalog { get { return Array.AsReadOnly(SampleSnapshot.Clone(characters)); } }
        public IReadOnlyList<SkillData> SkillCatalog { get { return Array.AsReadOnly(SampleSnapshot.Clone(skills)); } }
        public LeaderSkillData LeaderSkill { get { return SampleSnapshot.Clone(leader); } }
        public PassiveData Passive { get { return SampleSnapshot.Clone(passive); } }
        public IReadOnlyList<SkillEffectData> PassiveEffects { get { return Array.AsReadOnly(SampleSnapshot.Clone(passive.Effects)); } }
        public EnemyData EnemyDefinition { get { return SampleSnapshot.Clone(enemyDefinition); } }
        public StageData Stage { get { return SampleSnapshot.Clone(stage); } }
        public BannerData StandardBanner { get { return SampleSnapshot.Clone(standardBanner); } }
        public BannerData GatherInRotationOneBanner { get { return SampleSnapshot.Clone(gatherOneBanner); } }
        public BannerData GatherInRotationTwoBanner { get { return SampleSnapshot.Clone(gatherTwoBanner); } }
        public EventData EventDefinition { get { return SampleSnapshot.Clone(eventDefinition); } }
        public IReadOnlyList<RotationScheduleData> ScheduleDefinitions { get { return Array.AsReadOnly(SampleSnapshot.Clone(schedules)); } }
        public DateTimeOffset ScheduleNow { get { return VerticalSliceFactory.SampleScheduleTime; } }
        public BoardSnapshot GeneratedBoard { get; private set; }
        public BoardSnapshot OpeningBoard { get; private set; }
        public BoardState CurrentBoard { get { return BattleContext.Board; } }
        public IOrbSource OrbSource { get; private set; }
        public PartyState Party { get; private set; }
        public SummonCollection OwnedCharacters { get; private set; }
        public EnemyRuntime Enemy { get; private set; }
        public StageSession StageSession { get; private set; }
        public StageCatalog StageCatalog { get; private set; }
        public BattleContext BattleContext { get; private set; }
        public SkillResolution ActiveSkillExample { get; private set; }
        public IReadOnlyList<CascadeLayer> OpeningCascadeLayers { get { return openingCascades; } }
        public BattleTurnResolution OpeningTurn { get; private set; }
        public CharacterProgress AscensionProgress { get; private set; }
        public DuplicateApplicationResult AscensionExample { get; private set; }
        public CharacterProgress AwakeningProgress { get; private set; }
        public AwakeningResult AwakeningExample { get; private set; }
        public IReadOnlyList<EnemyEffectSnapshot> BossMechanicExample { get { return bossMechanics; } }
        public IReadOnlyList<StarResult> StageObjectiveExample { get { return objectiveResults; } }
        public BannerRuntimeState StandardState { get; private set; }
        public SummonBatch StandardSummonExample { get; private set; }
        public BannerRuntimeState GatherInRotationOneState { get; private set; }
        public SummonBatch GatherInRotationOneSummonExample { get; private set; }
        public BannerRuntimeState GatherInRotationTwoState { get; private set; }
        public SummonBatch GatherInRotationTwoSummonExample { get; private set; }
        public PackSummonFlow PackFlow { get; private set; }
        public SaveLoadResult SaveRoundTrip { get; private set; }
        public SaveData SaveProfile { get { return SaveRoundTrip.Data; } }
        public string SerializedSave { get; private set; }
        public IReadOnlyList<ActiveContent> ActiveScheduledContent { get { return activeContent; } }

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source)
        {
            var copy = new T[source.Count];
            for (var index = 0; index < copy.Length; index++) copy[index] = source[index];
            return Array.AsReadOnly(copy);
        }
    }

    internal sealed class DeterministicRandom : PuzzleGame.Core.Board.IRandomSource, PuzzleGame.Core.Gacha.IRandomSource
    {
        private uint state;
        internal DeterministicRandom(int seed)
        {
            state = unchecked((uint)seed) + 0x9e3779b9u;
            if (state == 0u) state = 0x6d2b79f5u;
        }
        public int Next(int exclusiveMaximum) { return NextValue(exclusiveMaximum); }
        public int NextInt(int exclusiveUpperBound) { return NextValue(exclusiveUpperBound); }
        private int NextValue(int maximum)
        {
            if (maximum <= 0) throw new ArgumentOutOfRangeException("maximum");
            var value = state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state = value;
            return (int)(value % (uint)maximum);
        }
    }

    internal sealed class SampleOrbSource : IOrbSource
    {
        private static readonly OrbType[] OpeningRefill =
        {
            OrbType.Water, OrbType.Fire,
            OrbType.Water, OrbType.Nature,
            OrbType.Water, OrbType.Light,
            OrbType.Dark, OrbType.Light, OrbType.Heart,
            OrbType.Heart, OrbType.Dark, OrbType.Nature, OrbType.Light
        };
        private readonly DeterministicRandom random;
        private int index;
        internal SampleOrbSource(int seed) { random = new DeterministicRandom(seed); }
        public OrbType NextOrb()
        {
            if (index < OpeningRefill.Length) return OpeningRefill[index++];
            return (OrbType)random.Next(6);
        }
    }

    internal sealed class FixedClock : IClock
    {
        internal FixedClock(DateTimeOffset now) { Now = now; }
        public DateTimeOffset Now { get; private set; }
    }

    internal sealed class SampleMemorySaveStorage : ISaveStorage
    {
        internal string Content { get; private set; }
        public SaveStorageReadResult Read()
        {
            return Content == null ? SaveStorageReadResult.Missing : SaveStorageReadResult.FromContent(Content);
        }
        public void WriteAtomic(string content) { Content = content; }
        public void BackupCorrupt() { throw new InvalidOperationException("The deterministic sample does not contain corrupt data."); }
    }

    internal static class SampleSnapshot
    {
        internal static CharacterData[] Clone(CharacterData[] values)
        {
            var result = new CharacterData[values.Length];
            for (var index = 0; index < values.Length; index++) result[index] = Clone(values[index]);
            return result;
        }
        internal static CharacterData Clone(CharacterData value)
        {
            if (value == null) return null;
            return new CharacterData
            {
                Id = value.Id, DisplayName = value.DisplayName, Element = value.Element, BaseRarity = value.BaseRarity,
                BaseStats = Clone(value.BaseStats), ActiveSkillId = value.ActiveSkillId, LeaderSkillId = value.LeaderSkillId,
                PassiveId = value.PassiveId, Tags = Clone(value.Tags), LevelCurve = Clone(value.LevelCurve),
                Ascension = Clone(value.Ascension), Awakening = Clone(value.Awakening),
                BaseVisuals = Clone(value.BaseVisuals), AwakenedVisuals = Clone(value.AwakenedVisuals)
            };
        }
        internal static SkillData[] Clone(SkillData[] values)
        {
            var result = new SkillData[values.Length];
            for (var index = 0; index < values.Length; index++) result[index] = Clone(values[index]);
            return result;
        }
        internal static SkillData Clone(SkillData value)
        {
            return value == null ? null : new SkillData
            {
                Id = value.Id, DisplayName = value.DisplayName, ChargeElement = value.ChargeElement,
                ChargeRequired = value.ChargeRequired, Effects = Clone(value.Effects)
            };
        }
        internal static LeaderSkillData Clone(LeaderSkillData value)
        {
            return value == null ? null : new LeaderSkillData
            {
                Id = value.Id, DisplayName = value.DisplayName,
                RequiredElements = value.RequiredElements == null ? null : (ElementType[])value.RequiredElements.Clone(),
                RequiredTags = Clone(value.RequiredTags), MinimumComboCount = value.MinimumComboCount,
                MinimumHpPercent = value.MinimumHpPercent, Effects = Clone(value.Effects)
            };
        }
        internal static PassiveData Clone(PassiveData value)
        {
            return value == null ? null : new PassiveData { Id = value.Id, Effects = Clone(value.Effects) };
        }
        internal static SkillEffectData[] Clone(SkillEffectData[] values)
        {
            if (values == null) return null;
            var result = new SkillEffectData[values.Length];
            for (var index = 0; index < values.Length; index++) result[index] = values[index] == null ? null : new SkillEffectData { Type = values[index].Type, Payload = Clone(values[index].Payload) };
            return result;
        }
        internal static EnemyData Clone(EnemyData value)
        {
            if (value == null) return null;
            var actions = new EnemyActionData[value.Actions.Length];
            for (var index = 0; index < actions.Length; index++)
            {
                var source = value.Actions[index];
                var effects = new EnemyEffectData[source.Effects.Length];
                for (var effect = 0; effect < effects.Length; effect++) effects[effect] = Clone(source.Effects[effect]);
                actions[index] = new EnemyActionData { Id = source.Id, ResetCountdown = source.ResetCountdown, Effects = effects };
            }
            var triggers = new EnemyThresholdTriggerData[value.ThresholdTriggers.Length];
            for (var index = 0; index < triggers.Length; index++) triggers[index] = new EnemyThresholdTriggerData
            {
                Id = value.ThresholdTriggers[index].Id,
                HpThresholdPercent = value.ThresholdTriggers[index].HpThresholdPercent,
                Effect = Clone(value.ThresholdTriggers[index].Effect)
            };
            return new EnemyData
            {
                Id = value.Id, DisplayName = value.DisplayName, Element = value.Element, BaseStats = Clone(value.BaseStats),
                InitialCountdown = value.InitialCountdown, Actions = actions, ThresholdTriggers = triggers, Visuals = Clone(value.Visuals)
            };
        }
        internal static StageData Clone(StageData value)
        {
            if (value == null) return null;
            var waves = new WaveData[value.Waves.Length];
            for (var index = 0; index < waves.Length; index++) waves[index] = new WaveData
            {
                Id = value.Waves[index].Id,
                EnemyIds = Clone(value.Waves[index].EnemyIds),
                Rewards = Clone(value.Waves[index].Rewards)
            };
            var objectives = new StarObjectiveData[value.StarObjectives.Length];
            for (var index = 0; index < objectives.Length; index++) objectives[index] = new StarObjectiveData
            {
                Type = value.StarObjectives[index].Type,
                HpThresholdPercent = value.StarObjectives[index].HpThresholdPercent,
                MaximumBoardResolutionCount = value.StarObjectives[index].MaximumBoardResolutionCount
            };
            return new StageData
            {
                Id = value.Id, ChapterId = value.ChapterId, DisplayName = value.DisplayName,
                IsPermanentStory = value.IsPermanentStory, Waves = waves, StarObjectives = objectives,
                ClearRewards = Clone(value.ClearRewards)
            };
        }
        internal static BannerData Clone(BannerData value)
        {
            if (value == null) return null;
            var entries = new WeightedCharacterData[value.Characters.Length];
            for (var index = 0; index < entries.Length; index++) entries[index] = new WeightedCharacterData
            {
                CharacterId = value.Characters[index].CharacterId,
                Weight = value.Characters[index].Weight,
                IsFeatured = value.Characters[index].IsFeatured
            };
            var steps = new BannerStepData[value.Steps.Length];
            for (var index = 0; index < steps.Length; index++) steps[index] = new BannerStepData
            {
                PullCount = value.Steps[index].PullCount, GemCost = value.Steps[index].GemCost,
                GuaranteedFiveStarFeaturedBoost = value.Steps[index].GuaranteedFiveStarFeaturedBoost,
                GuaranteedFeaturedFiveStar = value.Steps[index].GuaranteedFeaturedFiveStar
            };
            return new BannerData
            {
                Id = value.Id, DisplayName = value.DisplayName, Type = value.Type,
                SinglePullGemCost = value.SinglePullGemCost, TenPullGemCost = value.TenPullGemCost,
                Characters = entries, Steps = steps, PresentationKey = value.PresentationKey, RotationId = value.RotationId
            };
        }
        internal static EventData Clone(EventData value)
        {
            return value == null ? null : new EventData
            {
                Id = value.Id, DisplayName = value.DisplayName, StageIds = Clone(value.StageIds),
                MilestoneRewards = Clone(value.MilestoneRewards), PresentationKey = value.PresentationKey
            };
        }
        internal static RotationScheduleData[] Clone(RotationScheduleData[] values)
        {
            var result = new RotationScheduleData[values.Length];
            for (var index = 0; index < values.Length; index++) result[index] = new RotationScheduleData
            {
                Id = values[index].Id, ContentId = values[index].ContentId, ContentType = values[index].ContentType,
                IsEnabled = values[index].IsEnabled, Priority = values[index].Priority,
                Start = values[index].Start, End = values[index].End,
                RecurringWeekdays = values[index].RecurringWeekdays == null ? null : (int[])values[index].RecurringWeekdays.Clone(),
                StartMinuteOfDay = values[index].StartMinuteOfDay, EndMinuteOfDay = values[index].EndMinuteOfDay
            };
            return result;
        }
        private static RewardData[] Clone(RewardData[] values)
        {
            if (values == null) return null;
            var result = new RewardData[values.Length];
            for (var index = 0; index < values.Length; index++) result[index] = values[index] == null ? null : new RewardData
            {
                Id = values[index].Id, Type = values[index].Type, ItemId = values[index].ItemId, Amount = values[index].Amount
            };
            return result;
        }
        private static EnemyEffectData Clone(EnemyEffectData value)
        {
            return value == null ? null : new EnemyEffectData { Type = value.Type, Payload = Clone(value.Payload) };
        }
        private static EffectPayloadData Clone(EffectPayloadData value)
        {
            return value == null ? null : new EffectPayloadData
            {
                Amount = value.Amount, Multiplier = value.Multiplier, DurationSeconds = value.DurationSeconds,
                TurnCount = value.TurnCount, ComboCount = value.ComboCount, SourceOrb = value.SourceOrb,
                TargetOrb = value.TargetOrb, TargetId = value.TargetId, Tags = Clone(value.Tags)
            };
        }
        private static ProgressionCurveData Clone(ProgressionCurveData value)
        {
            var stats = new StatBlock[value.StatsByLevel.Length];
            for (var index = 0; index < stats.Length; index++) stats[index] = Clone(value.StatsByLevel[index]);
            return new ProgressionCurveData
            {
                MaxLevel = value.MaxLevel,
                ExperienceRequiredByLevel = (int[])value.ExperienceRequiredByLevel.Clone(),
                StatsByLevel = stats
            };
        }
        private static AscensionConfigurationData Clone(AscensionConfigurationData value)
        {
            var ranks = new AscensionRankData[value.Ranks.Length];
            for (var index = 0; index < ranks.Length; index++) ranks[index] = new AscensionRankData
            {
                Rank = value.Ranks[index].Rank,
                ActiveSkillChargeReduction = value.Ranks[index].ActiveSkillChargeReduction,
                StatBonus = Clone(value.Ranks[index].StatBonus)
            };
            return new AscensionConfigurationData { OverflowUniversalResourceAmount = value.OverflowUniversalResourceAmount, Ranks = ranks };
        }
        private static AwakeningRequirementData Clone(AwakeningRequirementData value)
        {
            var materials = new MaterialRequirementData[value.Materials.Length];
            for (var index = 0; index < materials.Length; index++) materials[index] = new MaterialRequirementData
            {
                MaterialId = value.Materials[index].MaterialId, Amount = value.Materials[index].Amount
            };
            return new AwakeningRequirementData { RequiredLevel = value.RequiredLevel, GoldCost = value.GoldCost, Materials = materials };
        }
        private static StatBlock Clone(StatBlock value)
        {
            return value == null ? null : new StatBlock { Hp = value.Hp, Attack = value.Attack, Recovery = value.Recovery };
        }
        private static VisualReferenceSet Clone(VisualReferenceSet value)
        {
            return value == null ? null : new VisualReferenceSet
            {
                PortraitKey = value.PortraitKey, CardArtKey = value.CardArtKey, ModelKey = value.ModelKey, VfxKey = value.VfxKey
            };
        }
        private static string[] Clone(string[] values) { return values == null ? null : (string[])values.Clone(); }
    }
}
