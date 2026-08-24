using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Presentation.Content;

namespace PuzzleGame.Presentation.CoreIntegration
{
    /// <summary>
    /// Translates the authored JSON content bank (Resources/Content, parsed
    /// by ContentDb) into Codex's authoritative core contracts, and runs
    /// ContractValidation over every produced record.
    ///
    /// The JSON bank predates the final contracts, so this is the single
    /// canonical mapping: display strings stay presentation-side in
    /// ContentDb, while everything gameplay-relevant (stats, curves, skill
    /// effects, enemy actions, banner weights, guarantees, schedules) is
    /// materialized here as core data. No gameplay rules are implemented in
    /// this class — it only builds data for core engines to execute.
    /// </summary>
    public sealed class TranslatedContent
    {
        public readonly Dictionary<string, CharacterData> Characters = new Dictionary<string, CharacterData>(StringComparer.Ordinal);
        public readonly Dictionary<string, SkillData> ActiveSkills = new Dictionary<string, SkillData>(StringComparer.Ordinal);
        public readonly Dictionary<string, LeaderSkillData> LeaderSkills = new Dictionary<string, LeaderSkillData>(StringComparer.Ordinal);
        public readonly Dictionary<string, PassiveData> Passives = new Dictionary<string, PassiveData>(StringComparer.Ordinal);
        /// <summary>All enemies including per-stage scaled variants.</summary>
        public readonly Dictionary<string, EnemyData> Enemies = new Dictionary<string, EnemyData>(StringComparer.Ordinal);
        /// <summary>Variant enemy id → authored base enemy id (for display lookups).</summary>
        public readonly Dictionary<string, string> EnemyBaseIds = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly Dictionary<string, StageData> Stages = new Dictionary<string, StageData>(StringComparer.Ordinal);
        public readonly Dictionary<string, BannerData> Banners = new Dictionary<string, BannerData>(StringComparer.Ordinal);
        public readonly Dictionary<string, EventData> Events = new Dictionary<string, EventData>(StringComparer.Ordinal);
        public readonly List<RotationScheduleData> Schedules = new List<RotationScheduleData>();
        /// <summary>Presentation stage id → authored stage modifier strings.</summary>
        public readonly Dictionary<string, List<string>> StageModifiers = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        public SkillData GetActiveSkill(string characterId) { return ActiveSkills[characterId + "-active"]; }
        public LeaderSkillData GetLeaderSkill(string characterId)
        {
            LeaderSkillData skill;
            return LeaderSkills.TryGetValue(characterId + "-leader", out skill) ? skill : null;
        }
        public PassiveData GetPassive(string characterId)
        {
            PassiveData passive;
            return Passives.TryGetValue(characterId + "-passive", out passive) ? passive : null;
        }
    }

    public static class ContentTranslator
    {
        public static TranslatedContent Translate(ContentDb db)
        {
            if (db == null) throw new ArgumentNullException("db");
            var result = new TranslatedContent();
            var errors = new List<string>();

            foreach (var dto in db.Characters.Values) TranslateCharacter(dto, result, errors);
            foreach (var dto in db.Enemies.Values) TranslateBaseEnemy(dto, result, errors);
            foreach (var chapter in db.Chapters.Values)
            {
                foreach (var stage in chapter.stages)
                    TranslateStage(stage, "chapter_" + chapter.chapterNumber.ToString("00"), db, result, errors);
            }
            foreach (var ev in db.Events.Values) TranslateEvent(ev, db, result, errors);
            foreach (var banner in db.Banners.Values) TranslateBanner(banner, db, result, errors);
            foreach (var entry in db.Schedule) TranslateSchedule(entry, db, result, errors);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Content translation produced " + errors.Count + " invalid record(s):\n" +
                    string.Join("\n", errors.ToArray()));
            }
            return result;
        }

        // ------------------------------ characters ------------------------------

        static void TranslateCharacter(CharacterDto dto, TranslatedContent result, List<string> errors)
        {
            var element = ToElement(dto.element);
            var activeSkill = BuildActiveSkill(dto, element);
            var leaderSkill = BuildLeaderSkill(dto);
            var passive = BuildPassive(dto);

            var data = new CharacterData
            {
                Id = dto.id,
                DisplayName = dto.name,
                Element = element,
                BaseRarity = dto.rarity,
                BaseStats = new StatBlock { Hp = dto.hp, Attack = dto.atk, Recovery = dto.rec },
                ActiveSkillId = activeSkill.Id,
                LeaderSkillId = leaderSkill != null ? leaderSkill.Id : null,
                PassiveId = passive != null ? passive.Id : null,
                Tags = dto.tags != null ? dto.tags.ToArray() : Array.Empty<string>(),
                LevelCurve = BuildLevelCurve(dto),
                Ascension = BuildAscension(dto),
                Awakening = BuildAwakening(dto),
                BaseVisuals = new VisualReferenceSet
                {
                    PortraitKey = dto.artRef,
                    CardArtKey = dto.artRef,
                    ModelKey = dto.artRef,
                    VfxKey = dto.vfxRef
                },
                AwakenedVisuals = new VisualReferenceSet
                {
                    PortraitKey = dto.HasAwakenedForm ? dto.awakened.artRef : dto.artRef,
                    CardArtKey = dto.HasAwakenedForm ? dto.awakened.artRef : dto.artRef,
                    ModelKey = dto.HasAwakenedForm ? dto.awakened.artRef : dto.artRef,
                    VfxKey = dto.HasAwakenedForm ? dto.awakened.vfxRef : dto.vfxRef
                }
            };

            Check(result.Characters, data, errors);
            Check(result.ActiveSkills, activeSkill, errors);
            if (leaderSkill != null) Check(result.LeaderSkills, leaderSkill, errors);
            if (passive != null) Check(result.Passives, passive, errors);
        }

        static ProgressionCurveData BuildLevelCurve(CharacterDto dto)
        {
            int maxLevel = Math.Max(1, dto.maxLevel);
            var stats = new StatBlock[maxLevel];
            for (var level = 1; level <= maxLevel; level++)
            {
                // Same display curve the roster UI always used: 40% → 100%.
                float t = maxLevel <= 1 ? 1f : (level - 1f) / (maxLevel - 1f);
                stats[level - 1] = new StatBlock
                {
                    Hp = LerpStat(dto.hp, t),
                    Attack = LerpStat(dto.atk, t),
                    Recovery = LerpStat(dto.rec, t)
                };
            }
            var thresholds = new int[maxLevel - 1];
            for (var index = 0; index < thresholds.Length; index++)
            {
                // Cumulative quadratic curve: strictly increasing totals.
                thresholds[index] = 25 * (index + 1) * (index + 2);
            }
            return new ProgressionCurveData { MaxLevel = maxLevel, ExperienceRequiredByLevel = thresholds, StatsByLevel = stats };
        }

        static int LerpStat(int max, float t)
        {
            return Math.Max(1, (int)Math.Round(max * (0.4f + 0.6f * t)));
        }

        static AscensionConfigurationData BuildAscension(CharacterDto dto)
        {
            // Spec: early ranks improve the Active Skill, later ranks add stats.
            int reduction = Math.Max(1, (dto.activeSkill != null ? dto.activeSkill.chargeMax : 20) / 10);
            return new AscensionConfigurationData
            {
                OverflowUniversalResourceAmount = 50,   // "+50 Spark Essence"
                Ranks = new[]
                {
                    new AscensionRankData { Rank = 1, ActiveSkillChargeReduction = reduction },
                    new AscensionRankData { Rank = 2, ActiveSkillChargeReduction = reduction },
                    new AscensionRankData { Rank = 3, StatBonus = new StatBlock { Hp = Math.Max(1, dto.hp / 20) } },
                    new AscensionRankData { Rank = 4, StatBonus = new StatBlock { Attack = Math.Max(1, dto.atk / 20) } },
                    new AscensionRankData { Rank = 5, StatBonus = new StatBlock { Recovery = Math.Max(1, dto.rec / 20) } }
                }
            };
        }

        static AwakeningRequirementData BuildAwakening(CharacterDto dto)
        {
            if (!dto.HasAwakenedForm)
            {
                // All-zero shape = "not configured", accepted by validation.
                return new AwakeningRequirementData();
            }
            return new AwakeningRequirementData
            {
                RequiredLevel = Math.Max(1, dto.maxLevel),
                GoldCost = 100000,
                Materials = new[]
                {
                    new MaterialRequirementData { MaterialId = "radiant_core", Amount = 10 },
                    new MaterialRequirementData { MaterialId = ElementCoreId(ToElement(dto.element)), Amount = 5 }
                }
            };
        }

        public static string ElementCoreId(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire: return "ember_core";
                case ElementType.Water: return "tide_core";
                case ElementType.Nature: return "bloom_core";
                case ElementType.Light: return "dawn_core";
                default: return "dusk_core";
            }
        }

        // ------------------------------- skills ---------------------------------

        static SkillData BuildActiveSkill(CharacterDto dto, ElementType element)
        {
            var skill = dto.activeSkill ?? new SkillDto { name = dto.name + "'s Resolve", desc = "", chargeMax = 20 };
            return new SkillData
            {
                Id = dto.id + "-active",
                DisplayName = skill.name,
                ChargeElement = element,
                ChargeRequired = Math.Max(3, skill.chargeMax),
                Effects = SynthesizeSkillEffects(skill.desc ?? "", dto, element)
            };
        }

        /// <summary>
        /// Maps authored skill descriptions onto typed core effects. The
        /// description keywords were the authoring vocabulary all along
        /// (convert/heal/delay/extend/shield/damage); this makes that
        /// mapping canonical instead of leaving it inside a demo mock.
        /// </summary>
        static SkillEffectData[] SynthesizeSkillEffects(string desc, CharacterDto dto, ElementType element)
        {
            var lower = desc.ToLowerInvariant();
            var effects = new List<SkillEffectData>();

            var convert = Regex.Match(lower, @"convert (?:all )?(fire|water|nature|light|dark|heart) orbs to (fire|water|nature|light|dark|heart)");
            if (convert.Success)
            {
                effects.Add(new SkillEffectData
                {
                    Type = SkillEffectType.ConvertOrbs,
                    Payload = new EffectPayloadData
                    {
                        SourceOrb = ToOrb(convert.Groups[1].Value),
                        TargetOrb = ToOrb(convert.Groups[2].Value)
                    }
                });
            }
            else if (lower.Contains("convert") || lower.Contains("change"))
            {
                // Non-specific conversions ("bottom row", "corners"): create
                // orbs of the character's element instead.
                effects.Add(new SkillEffectData
                {
                    Type = SkillEffectType.CreateOrbs,
                    Payload = new EffectPayloadData { TargetOrb = (OrbType)element, Amount = 6 }
                });
            }

            if (lower.Contains("heal") || lower.Contains("recover"))
            {
                effects.Add(new SkillEffectData
                {
                    Type = SkillEffectType.Heal,
                    Payload = new EffectPayloadData { Amount = Math.Max(100, dto.rec * 6) }
                });
            }

            var extend = Regex.Match(lower, @"extend(?:s)? (?:the )?move(?:ment)? time by (\d+)");
            if (extend.Success || lower.Contains("extend move"))
            {
                int seconds = extend.Success ? int.Parse(extend.Groups[1].Value, CultureInfo.InvariantCulture) : 3;
                effects.Add(new SkillEffectData
                {
                    Type = SkillEffectType.ExtendMoveTime,
                    Payload = new EffectPayloadData { DurationSeconds = seconds, TurnCount = 1 }
                });
            }

            if (lower.Contains("delay"))
            {
                effects.Add(new SkillEffectData
                {
                    Type = SkillEffectType.DelayEnemies,
                    Payload = new EffectPayloadData { Amount = 2 }
                });
            }

            if (lower.Contains("shield") || lower.Contains("reduce damage") || lower.Contains("reducing damage"))
            {
                effects.Add(new SkillEffectData
                {
                    Type = SkillEffectType.Shield,
                    Payload = new EffectPayloadData { Multiplier = 0.5f, TurnCount = 1 }
                });
            }

            if (lower.Contains("increase attack") || lower.Contains("attack boost"))
            {
                effects.Add(new SkillEffectData
                {
                    Type = SkillEffectType.AttackBoost,
                    Payload = new EffectPayloadData { Multiplier = 1.5f, TurnCount = 1 }
                });
            }

            if (effects.Count == 0 || lower.Contains("damage"))
            {
                effects.Add(new SkillEffectData
                {
                    Type = SkillEffectType.DirectDamage,
                    Payload = new EffectPayloadData { Amount = Math.Max(100, dto.atk * 5) }
                });
            }
            return effects.ToArray();
        }

        static LeaderSkillData BuildLeaderSkill(CharacterDto dto)
        {
            if (dto.leaderSkill == null || string.IsNullOrEmpty(dto.leaderSkill.name)) return null;
            var desc = (dto.leaderSkill.desc ?? "").ToLowerInvariant();

            // Multiplier: first "xN" or "xN.N" in the authored text; capped so
            // party-wide application of element-scoped authored text stays sane.
            float multiplier = 1.5f;
            var match = Regex.Match(desc, @"x(\d+(?:\.\d+)?)");
            if (match.Success)
            {
                multiplier = Math.Min(3f, float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
            }

            int minimumCombo = 0;
            var combo = Regex.Match(desc, @"(?:at |combos? )(\d+)\+? ?combo");
            if (combo.Success) minimumCombo = int.Parse(combo.Groups[1].Value, CultureInfo.InvariantCulture);

            float minimumHp = 0f;
            var hpAbove = Regex.Match(desc, @"above (\d+)% hp");
            if (hpAbove.Success) minimumHp = int.Parse(hpAbove.Groups[1].Value, CultureInfo.InvariantCulture) / 100f;

            var requiredTags = new List<string>();
            foreach (var tag in dto.tags ?? new List<string>())
            {
                if (desc.Contains(tag.ToLowerInvariant())) { requiredTags.Add(tag); break; }
            }

            return new LeaderSkillData
            {
                Id = dto.id + "-leader",
                DisplayName = dto.leaderSkill.name,
                RequiredElements = Array.Empty<ElementType>(),
                RequiredTags = requiredTags.ToArray(),
                MinimumComboCount = minimumCombo,
                MinimumHpPercent = minimumHp,
                Effects = new[]
                {
                    new SkillEffectData { Type = SkillEffectType.AttackBoost, Payload = new EffectPayloadData { Multiplier = multiplier } }
                }
            };
        }

        static PassiveData BuildPassive(CharacterDto dto)
        {
            if (dto.passive == null || string.IsNullOrEmpty(dto.passive.name)) return null;
            var desc = (dto.passive.desc ?? "").ToLowerInvariant();
            float multiplier = 1.05f;
            var percent = Regex.Match(desc, @"\+(\d+)% (?:atk|damage)");
            if (percent.Success)
            {
                multiplier = 1f + Math.Min(50, int.Parse(percent.Groups[1].Value, CultureInfo.InvariantCulture)) / 100f;
            }
            return new PassiveData
            {
                Id = dto.id + "-passive",
                Effects = new[]
                {
                    new SkillEffectData { Type = SkillEffectType.AttackBoost, Payload = new EffectPayloadData { Multiplier = multiplier } }
                }
            };
        }

        // -------------------------------- enemies -------------------------------

        static void TranslateBaseEnemy(EnemyDto dto, TranslatedContent result, List<string> errors)
        {
            var data = BuildEnemy(dto, dto.id, 1f, 1f);
            result.EnemyBaseIds[dto.id] = dto.id;
            Check(result.Enemies, data, errors);
        }

        static EnemyData BuildEnemy(EnemyDto dto, string id, float hpMult, float atkMult)
        {
            int hp = Math.Max(1, (int)Math.Round(dto.hp * hpMult));
            int atk = Math.Max(0, (int)Math.Round(dto.atk * atkMult));
            var actions = new List<EnemyActionData>();
            var triggers = new List<EnemyThresholdTriggerData>();
            foreach (var action in dto.actions ?? new List<EnemyActionDto>())
            {
                var type = (action.type ?? "damage").ToLowerInvariant();
                if (type == "enrage")
                {
                    triggers.Add(new EnemyThresholdTriggerData
                    {
                        Id = id + "-t" + triggers.Count,
                        HpThresholdPercent = 50f,
                        Effect = new EnemyEffectData
                        {
                            Type = EnemyEffectType.Enrage,
                            Payload = new EffectPayloadData { Multiplier = 1.5f, TurnCount = 3 }
                        }
                    });
                    continue;
                }
                var effect = TranslateEnemyEffect(type, action, dto, atk);
                if (effect == null) continue;   // heal/summon/taunt: display-only flavor, no core equivalent
                actions.Add(new EnemyActionData
                {
                    Id = action.name,
                    ResetCountdown = Math.Max(1, dto.countdown),
                    Effects = new[] { effect }
                });
            }
            if (actions.Count == 0)
            {
                actions.Add(new EnemyActionData
                {
                    Id = "Attack",
                    ResetCountdown = Math.Max(1, dto.countdown),
                    Effects = new[] { new EnemyEffectData { Type = EnemyEffectType.Damage, Payload = new EffectPayloadData { Amount = Math.Max(1, atk) } } }
                });
            }
            return new EnemyData
            {
                Id = id,
                DisplayName = dto.name,
                Element = ToElement(dto.element),
                BaseStats = new StatBlock { Hp = hp, Attack = atk, Recovery = 0 },
                InitialCountdown = Math.Max(1, dto.countdown),
                Actions = actions.ToArray(),
                ThresholdTriggers = triggers.ToArray(),
                Visuals = new VisualReferenceSet { PortraitKey = dto.artRef, CardArtKey = dto.artRef, ModelKey = dto.artRef, VfxKey = "" }
            };
        }

        static EnemyEffectData TranslateEnemyEffect(string type, EnemyActionDto action, EnemyDto dto, int scaledAtk)
        {
            var element = ToElement(dto.element);
            switch (type)
            {
                case "damage":
                    return Effect(EnemyEffectType.Damage, p => p.Amount = DamageAmount(action, scaledAtk, 1f));
                case "bigdamage":
                    return Effect(EnemyEffectType.Damage, p => p.Amount = DamageAmount(action, scaledAtk, 1.8f));
                case "convert":
                {
                    var target = ToOrbOrDefault(action.color, (OrbType)element);
                    return Effect(EnemyEffectType.ConvertOrbs, p => { p.SourceOrb = NextOrbExcept(target); p.TargetOrb = target; });
                }
                case "lock":
                    // Core locks a whole orb type; use the enemy's own element.
                    return Effect(EnemyEffectType.LockOrbs, p => { p.SourceOrb = (OrbType)element; p.TurnCount = 2; });
                case "poison":
                    return Effect(EnemyEffectType.Poison, p => { p.Amount = Math.Max(1, action.count); p.TurnCount = 2; });
                case "block":
                    return Effect(EnemyEffectType.Blocker, p => { p.Amount = Math.Max(1, action.count); p.TurnCount = 2; });
                case "bind":
                    return Effect(EnemyEffectType.Bind, p => { p.Amount = Math.Max(1, action.count); p.TurnCount = 2; });
                case "timerdown":
                    return Effect(EnemyEffectType.ReduceMoveTime, p => { p.DurationSeconds = Math.Max(1, action.amount); p.TurnCount = 2; });
                case "absorb":
                    return Effect(EnemyEffectType.DamageAbsorb, p =>
                    {
                        var orb = ToOrbOrDefault(action.color, (OrbType)element);
                        p.SourceOrb = orb == OrbType.Heart ? (OrbType)element : orb;
                        p.TurnCount = 2;
                    });
                case "comboshield":
                    return Effect(EnemyEffectType.ComboShield, p => { p.Amount = action.count > 0 ? action.count : 3; p.TurnCount = 2; });
                default:
                    return null;
            }
        }

        static int DamageAmount(EnemyActionDto action, int scaledAtk, float factor)
        {
            // Hand-authored actions carry absolute damage; generated content
            // carried a placeholder 1 and expects attack-derived damage.
            return action.amount > 10 ? action.amount : Math.Max(1, (int)Math.Round(scaledAtk * factor));
        }

        static EnemyEffectData Effect(EnemyEffectType type, Action<EffectPayloadData> configure)
        {
            var payload = new EffectPayloadData();
            configure(payload);
            return new EnemyEffectData { Type = type, Payload = payload };
        }

        static OrbType NextOrbExcept(OrbType target)
        {
            var next = (OrbType)(((int)target + 1) % 5);
            return next == target ? OrbType.Heart : next;
        }

        // -------------------------------- stages --------------------------------

        static void TranslateStage(StageDto stage, string chapterId, ContentDb db, TranslatedContent result, List<string> errors)
        {
            var waves = new WaveData[stage.waves.Count];
            for (var waveIndex = 0; waveIndex < stage.waves.Count; waveIndex++)
            {
                var wave = stage.waves[waveIndex];
                var usedIds = new HashSet<string>(StringComparer.Ordinal);
                var enemyIds = new List<string>();
                foreach (var entry in wave.enemies)
                {
                    EnemyDto baseDto;
                    if (!db.Enemies.TryGetValue(entry.enemyId ?? "", out baseDto))
                    {
                        errors.Add("stage " + stage.id + ": unknown enemy " + entry.enemyId);
                        continue;
                    }
                    var variantId = EnsureEnemyVariant(baseDto, entry.hpMult, entry.atkMult, entry.countdownOverride, usedIds, result, errors);
                    usedIds.Add(variantId);
                    enemyIds.Add(variantId);
                }
                waves[waveIndex] = new WaveData
                {
                    Id = stage.id + "-w" + waveIndex,
                    EnemyIds = enemyIds.ToArray(),
                    Rewards = Array.Empty<RewardData>()
                };
            }

            var rewards = new List<RewardData>();
            if (stage.rewards != null)
            {
                if (stage.rewards.gold > 0) rewards.Add(new RewardData { Id = stage.id + "-gold", Type = RewardType.Gold, Amount = stage.rewards.gold });
                if (stage.rewards.gems > 0) rewards.Add(new RewardData { Id = stage.id + "-gems", Type = RewardType.Gems, Amount = stage.rewards.gems });
                for (var index = 0; index < stage.rewards.items.Count; index++)
                {
                    var item = stage.rewards.items[index];
                    rewards.Add(new RewardData { Id = stage.id + "-item" + index, Type = RewardType.Material, ItemId = item.id, Amount = Math.Max(1, item.count) });
                }
            }

            var data = new StageData
            {
                Id = stage.id,
                ChapterId = chapterId,
                DisplayName = stage.name,
                IsPermanentStory = stage.staminaCost == 0,
                Waves = waves,
                StarObjectives = new[]
                {
                    new StarObjectiveData { Type = StarObjectiveType.Clear },
                    new StarObjectiveData { Type = StarObjectiveType.FinishAboveHpThreshold, HpThresholdPercent = Clamp01(stage.hpThresholdStar) },
                    new StarObjectiveData { Type = StarObjectiveType.ClearWithinBoardResolutionCount, MaximumBoardResolutionCount = Math.Max(1, stage.resolutionsStar) }
                },
                ClearRewards = rewards.ToArray()
            };
            result.StageModifiers[stage.id] = new List<string>(stage.modifiers ?? new List<string>());
            Check(result.Stages, data, errors);
        }

        static string EnsureEnemyVariant(EnemyDto baseDto, float hpMult, float atkMult, int countdownOverride,
            HashSet<string> usedInWave, TranslatedContent result, List<string> errors)
        {
            bool scaled = Math.Abs(hpMult - 1f) > 0.001f || Math.Abs(atkMult - 1f) > 0.001f || countdownOverride > 0;
            var id = scaled
                ? baseDto.id + "~h" + hpMult.ToString("0.##", CultureInfo.InvariantCulture) +
                  "a" + atkMult.ToString("0.##", CultureInfo.InvariantCulture) +
                  (countdownOverride > 0 ? "c" + countdownOverride : "")
                : baseDto.id;
            // Core waves require unique enemy ids; duplicates in one authored
            // wave become distinct clone variants.
            var candidate = id;
            var copy = 2;
            while (usedInWave.Contains(candidate)) candidate = id + "#" + copy++;
            id = candidate;

            if (!result.Enemies.ContainsKey(id))
            {
                var dto = countdownOverride > 0 ? CloneWithCountdown(baseDto, countdownOverride) : baseDto;
                var data = BuildEnemy(dto, id, hpMult, atkMult);
                result.EnemyBaseIds[id] = baseDto.id;
                Check(result.Enemies, data, errors);
            }
            return id;
        }

        static EnemyDto CloneWithCountdown(EnemyDto source, int countdown)
        {
            return new EnemyDto
            {
                id = source.id, name = source.name, element = source.element, boss = source.boss,
                hp = source.hp, atk = source.atk, countdown = countdown, artRef = source.artRef,
                flavor = source.flavor, actions = source.actions
            };
        }

        // -------------------------------- events --------------------------------

        static void TranslateEvent(EventDto ev, ContentDb db, TranslatedContent result, List<string> errors)
        {
            var stageIds = new List<string>();
            foreach (var stage in ev.stages)
            {
                TranslateStage(stage, ev.id, db, result, errors);
                stageIds.Add(stage.id);
            }
            var data = new EventData
            {
                Id = ev.id,
                DisplayName = ev.name,
                StageIds = stageIds.ToArray(),
                MilestoneRewards = Array.Empty<RewardData>(),
                PresentationKey = ev.theme
            };
            Check(result.Events, data, errors);
        }

        // -------------------------------- banners -------------------------------

        static void TranslateBanner(BannerDto dto, ContentDb db, TranslatedContent result, List<string> errors)
        {
            var pool = BuildBannerPool(dto, db);
            if (pool.Count == 0)
            {
                errors.Add("banner " + dto.id + ": empty character pool after translation");
                return;
            }
            var steps = new BannerStepData[dto.steps.Count];
            for (var index = 0; index < dto.steps.Count; index++)
            {
                var step = dto.steps[index];
                bool isFinal = index == dto.steps.Count - 1;
                var guarantee = (step.guarantee ?? "").ToUpperInvariant();
                steps[index] = new BannerStepData
                {
                    PullCount = step.pullCount,
                    GemCost = step.gemCost,
                    // Only final-step 5★ guarantees exist in core; the two
                    // authored strings map to the two guarantee kinds.
                    GuaranteedFeaturedFiveStar = isFinal && guarantee.Contains("FEATURED 5★"),
                    GuaranteedFiveStarFeaturedBoost = isFinal && !guarantee.Contains("FEATURED 5★") && guarantee.Contains("5★ GUARANTEED")
                };
            }

            var type = ToBannerType(dto.kind);
            // Gather-In final steps must define a guarantee in core.
            if (type == BannerType.GatherIn && steps.Length > 0 &&
                !steps[steps.Length - 1].GuaranteedFeaturedFiveStar && !steps[steps.Length - 1].GuaranteedFiveStarFeaturedBoost)
            {
                steps[steps.Length - 1].GuaranteedFiveStarFeaturedBoost = true;
            }

            var data = new BannerData
            {
                Id = dto.id,
                DisplayName = dto.name,
                Type = type,
                SinglePullGemCost = dto.singleCost,
                TenPullGemCost = dto.multiCost,
                Characters = pool.ToArray(),
                Steps = steps,
                PresentationKey = string.IsNullOrEmpty(dto.packArt) ? dto.id : dto.packArt,
                RotationId = dto.id
            };
            Check(result.Banners, data, errors);
        }

        /// <summary>
        /// Converts authored display rates (percent per rarity + featured
        /// share of five-stars) into the core's weighted character pool.
        /// </summary>
        static List<WeightedCharacterData> BuildBannerPool(BannerDto dto, ContentDb db)
        {
            var featured = new HashSet<string>(dto.featured ?? new List<string>(), StringComparer.Ordinal);
            var byRarity = new Dictionary<int, List<CharacterDto>>();
            foreach (var character in db.Characters.Values)
            {
                if (character.rarity < dto.minPoolRarity) continue;
                if (dto.poolTags != null && dto.poolTags.Count > 0)
                {
                    bool tagged = featured.Contains(character.id);
                    foreach (var tag in character.tags) if (dto.poolTags.Contains(tag)) tagged = true;
                    if (!tagged) continue;
                }
                List<CharacterDto> list;
                if (!byRarity.TryGetValue(character.rarity, out list)) byRarity[character.rarity] = list = new List<CharacterDto>();
                list.Add(character);
            }
            // Featured units are always in the pool even outside tag filters.
            foreach (var id in featured)
            {
                CharacterDto character;
                if (!db.Characters.TryGetValue(id, out character)) continue;
                List<CharacterDto> list;
                if (!byRarity.TryGetValue(character.rarity, out list)) byRarity[character.rarity] = list = new List<CharacterDto>();
                if (!list.Contains(character)) list.Add(character);
            }

            var rates = dto.rates ?? new BannerRatesDto();
            var rarityRate = new Dictionary<int, float>
            {
                { 1, rates.oneStar }, { 2, rates.twoStar }, { 3, rates.threeStar }, { 4, rates.fourStar }, { 5, rates.fiveStar }
            };
            var pool = new List<WeightedCharacterData>();
            foreach (var pair in byRarity)
            {
                var rarity = pair.Key;
                var members = pair.Value;
                float totalRate;
                if (!rarityRate.TryGetValue(rarity, out totalRate) || totalRate <= 0f) continue;
                if (rarity == 5 && featured.Count > 0)
                {
                    var featuredMembers = members.FindAll(m => featured.Contains(m.id));
                    var normalMembers = members.FindAll(m => !featured.Contains(m.id));
                    float share = Clamp01(rates.featuredShareOfFiveStar / 100f);
                    if (normalMembers.Count == 0) share = 1f;
                    if (featuredMembers.Count == 0) share = 0f;
                    AddWeighted(pool, featuredMembers, totalRate * share, featured);
                    AddWeighted(pool, normalMembers, totalRate * (1f - share), featured);
                }
                else
                {
                    AddWeighted(pool, members, totalRate, featured);
                }
            }
            return pool;
        }

        static void AddWeighted(List<WeightedCharacterData> pool, List<CharacterDto> members, float groupRate, HashSet<string> featured)
        {
            if (members.Count == 0 || groupRate <= 0f) return;
            var each = groupRate / members.Count;
            foreach (var member in members)
            {
                pool.Add(new WeightedCharacterData
                {
                    CharacterId = member.id,
                    Weight = Math.Max(1, (int)Math.Round(each * 10000f)),
                    IsFeatured = featured.Contains(member.id)
                });
            }
        }

        // ------------------------------- schedules ------------------------------

        static void TranslateSchedule(ScheduleEntryDto dto, ContentDb db, TranslatedContent result, List<string> errors)
        {
            var data = new RotationScheduleData
            {
                Id = dto.id,
                ContentId = dto.targetId,
                ContentType = ResolveContentType(dto, db),
                IsEnabled = true,
                Priority = 0
            };
            var recurrence = (dto.recurrence ?? "none").ToLowerInvariant();
            if (dto.permanent || recurrence == "daily" || recurrence == "weekly")
            {
                data.Start = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
                data.End = new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);
                if (recurrence == "daily")
                {
                    data.RecurringWeekdays = new[] { 0, 1, 2, 3, 4, 5, 6 };
                    data.StartMinuteOfDay = 0;
                    data.EndMinuteOfDay = 1439;
                }
                else if (recurrence == "weekly")
                {
                    data.RecurringWeekdays = new[] { Math.Max(0, Math.Min(6, dto.weekday)) };
                    data.StartMinuteOfDay = 0;
                    data.EndMinuteOfDay = 1439;
                }
            }
            else
            {
                DateTimeOffset start, end;
                if (!DateTimeOffset.TryParse(dto.startUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out start) ||
                    !DateTimeOffset.TryParse(dto.endUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out end))
                {
                    errors.Add("schedule " + dto.id + ": invalid dates");
                    return;
                }
                data.Start = start;
                data.End = end;
            }

            var validation = ContractValidation.Validate(data);
            if (validation.Count > 0)
            {
                errors.Add("schedule " + dto.id + ": " + validation[0]);
                return;
            }
            result.Schedules.Add(data);
        }

        static ContentType ResolveContentType(ScheduleEntryDto dto, ContentDb db)
        {
            var kind = (dto.kind ?? "").ToLowerInvariant();
            if (kind == "banner")
            {
                BannerDto banner;
                if (db.Banners.TryGetValue(dto.targetId ?? "", out banner))
                {
                    switch ((banner.kind ?? "standard").ToLowerInvariant())
                    {
                        case "featured": return ContentType.FeaturedBanner;
                        case "gatherin": return ContentType.GatherInBanner;
                        case "stepup": return ContentType.StepUpBanner;
                        default: return ContentType.StandardBanner;
                    }
                }
                return ContentType.StandardBanner;
            }
            switch (kind)
            {
                case "eventchapter": return ContentType.EventChapter;
                case "materialdungeon":
                    return (dto.recurrence ?? "none").ToLowerInvariant() == "daily" ? ContentType.DailyDungeon : ContentType.WeeklyDungeon;
                case "awakeningstage": return ContentType.AwakeningMaterialStage;
                case "challengetower": return ContentType.Tower;
                case "bossrush": return ContentType.BossRush;
                default: return ContentType.EventChapter;
            }
        }

        // -------------------------------- helpers -------------------------------

        static void Check<T>(Dictionary<string, T> target, T data, List<string> errors) where T : IIdentifiedData
        {
            var validation = ContractValidation.Validate(data);
            if (validation.Count > 0)
            {
                errors.Add(typeof(T).Name + " '" + data.Id + "': " + string.Join("; ", CopyStrings(validation)));
                return;
            }
            if (target.ContainsKey(data.Id))
            {
                errors.Add(typeof(T).Name + " '" + data.Id + "': duplicate id after translation");
                return;
            }
            target.Add(data.Id, data);
        }

        static string[] CopyStrings(IReadOnlyList<string> source)
        {
            var copy = new string[source.Count];
            for (var index = 0; index < source.Count; index++) copy[index] = source[index];
            return copy;
        }

        public static ElementType ToElement(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "water": return ElementType.Water;
                case "nature": return ElementType.Nature;
                case "light": return ElementType.Light;
                case "dark": return ElementType.Dark;
                default: return ElementType.Fire;
            }
        }

        static OrbType ToOrb(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "water": return OrbType.Water;
                case "nature": return OrbType.Nature;
                case "light": return OrbType.Light;
                case "dark": return OrbType.Dark;
                case "heart": return OrbType.Heart;
                default: return OrbType.Fire;
            }
        }

        static OrbType ToOrbOrDefault(string value, OrbType fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : ToOrb(value);
        }

        static BannerType ToBannerType(string kind)
        {
            switch ((kind ?? "standard").Trim().ToLowerInvariant())
            {
                case "featured": return BannerType.Featured;
                case "gatherin": return BannerType.GatherIn;
                case "stepup": return BannerType.StepUp;
                default: return BannerType.Standard;
            }
        }

        static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }
    }
}
