using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PuzzleGame.Presentation.Content;

namespace PuzzleGame.Presentation.EditorTools
{
    /// <summary>
    /// In-engine content validation. Run from the CLI:
    ///   Unity -batchmode -quit -projectPath . -executeMethod
    ///     PuzzleGame.Presentation.EditorTools.ContentValidator.Run
    /// Throws (non-zero exit) when any content file is malformed or any
    /// cross-reference (enemy ids, dialogue ids, banner units, schedule
    /// targets) is broken.
    /// </summary>
    public static class ContentValidator
    {
        static readonly List<string> Errors = new List<string>();
        static readonly List<string> Warnings = new List<string>();

        public static void Run()
        {
            Errors.Clear();
            Warnings.Clear();
            var db = ContentDb.Load();

            Errors.AddRange(db.LoadIssues);
            ValidateCharacters(db);
            ValidateEnemies(db);
            ValidateChapters(db);
            ValidateBanners(db);
            ValidateEvents(db);
            ValidateSchedule(db);
            ValidateItems(db);
            ValidatePackThemes(db);

            Debug.Log("[ContentValidator] characters=" + db.Characters.Count +
                      " enemies=" + db.Enemies.Count +
                      " chapters=" + db.Chapters.Count +
                      " stages=" + db.Chapters.Values.Sum(c => c.stages.Count) +
                      " dialogueScenes=" + db.DialogueScenes.Count +
                      " banners=" + db.Banners.Count +
                      " events=" + db.Events.Count +
                      " rewardTables=" + db.RewardTables.Count +
                      " scheduleEntries=" + db.Schedule.Count +
                      " items=" + db.Items.Count +
                      " packThemes=" + db.PackThemes.Count);
            foreach (var warning in Warnings)
            {
                Debug.LogWarning("[ContentValidator] " + warning);
            }
            if (Errors.Count > 0)
            {
                foreach (var error in Errors)
                {
                    Debug.LogError("[ContentValidator] " + error);
                }
                throw new Exception("Content validation failed with " + Errors.Count + " error(s).");
            }
            Debug.Log("[ContentValidator] OK — all content valid.");
        }

        static readonly HashSet<string> ValidElements = new HashSet<string> { "fire", "water", "nature", "light", "dark" };
        static readonly HashSet<string> ValidStageKinds = new HashSet<string> { "normal", "miniboss", "boss", "story" };
        static readonly HashSet<string> ValidActionTypes = new HashSet<string>
        {
            "damage", "bigdamage", "convert", "lock", "poison", "block", "bind",
            "timerdown", "absorb", "comboshield", "enrage", "heal", "summon", "taunt"
        };

        static void ValidateCharacters(ContentDb db)
        {
            foreach (var c in db.Characters.Values)
            {
                string where = "character '" + c.id + "'";
                if (string.IsNullOrEmpty(c.id)) Errors.Add("character with empty id");
                if (string.IsNullOrEmpty(c.name)) Errors.Add(where + ": empty name");
                if (!ValidElements.Contains((c.element ?? "").ToLowerInvariant()))
                    Errors.Add(where + ": invalid element '" + c.element + "'");
                if (c.rarity < 1 || c.rarity > 5)
                    Errors.Add(where + ": base rarity must be 1..5 (got " + c.rarity + ")");
                if (c.hp <= 0 || c.atk <= 0 || c.rec < 0)
                    Errors.Add(where + ": non-positive stats");
                if (c.leaderSkill == null || string.IsNullOrEmpty(c.leaderSkill.name))
                    Errors.Add(where + ": missing leader skill");
                if (c.activeSkill == null || string.IsNullOrEmpty(c.activeSkill.name))
                    Errors.Add(where + ": missing active skill");
                if (c.activeSkill != null && c.activeSkill.chargeMax <= 0)
                    Errors.Add(where + ": active skill chargeMax must be positive");
                if (c.rarity == 5 && !c.HasAwakenedForm)
                    Errors.Add(where + ": every 5★ must define a 6★ awakened form");
                if (c.HasAwakenedForm && string.IsNullOrEmpty(c.awakened.artRef))
                    Errors.Add(where + ": awakened form missing artRef (visible art change is required)");
                if (c.tags == null || c.tags.Count == 0)
                    Warnings.Add(where + ": no team-composition tags");
            }
        }

        static void ValidateEnemies(ContentDb db)
        {
            foreach (var e in db.Enemies.Values)
            {
                string where = "enemy '" + e.id + "'";
                if (string.IsNullOrEmpty(e.name)) Errors.Add(where + ": empty name");
                if (!ValidElements.Contains((e.element ?? "").ToLowerInvariant()))
                    Errors.Add(where + ": invalid element '" + e.element + "'");
                if (e.hp <= 0 || e.atk < 0) Errors.Add(where + ": non-positive hp/atk");
                if (e.countdown < 1) Errors.Add(where + ": countdown must be >= 1");
                foreach (var action in e.actions)
                {
                    if (!ValidActionTypes.Contains((action.type ?? "").ToLowerInvariant()))
                        Errors.Add(where + ": unknown action type '" + action.type + "'");
                    if (string.IsNullOrEmpty(action.name))
                        Errors.Add(where + ": action with empty name");
                }
                if (e.boss && e.actions.Count < 2)
                    Warnings.Add(where + ": boss has fewer than 2 actions");
            }
        }

        static readonly HashSet<string> AllStageIds = new HashSet<string>();

        static void ValidateChapters(ContentDb db)
        {
            AllStageIds.Clear();
            foreach (var chapter in db.Chapters.Values)
            {
                string cwhere = "chapter " + chapter.chapterNumber;
                if (string.IsNullOrEmpty(chapter.title)) Errors.Add(cwhere + ": empty title");
                if (chapter.stages.Count == 0) Errors.Add(cwhere + ": no stages");
                int maxStars = chapter.stages.Count * 3;
                foreach (var milestone in chapter.starMilestones)
                {
                    if (milestone.stars < 1 || milestone.stars > maxStars)
                        Errors.Add(cwhere + ": milestone at " + milestone.stars + " stars is unreachable (max " + maxStars + ")");
                    foreach (var item in milestone.items)
                    {
                        if (!db.Items.ContainsKey(item.id ?? ""))
                            Errors.Add(cwhere + ": milestone references unknown item '" + item.id + "'");
                    }
                }
                if (chapter.stages.Count >= 20 && chapter.starMilestones.Count == 0)
                    Warnings.Add(cwhere + ": no star milestones configured");
                var numbers = new HashSet<int>();
                foreach (var stage in chapter.stages)
                {
                    string where = cwhere + " stage " + stage.stageNumber + " ('" + stage.id + "')";
                    if (!numbers.Add(stage.stageNumber)) Errors.Add(where + ": duplicate stage number");
                    if (string.IsNullOrEmpty(stage.id)) Errors.Add(where + ": empty id");
                    else if (!AllStageIds.Add(stage.id)) Errors.Add(where + ": duplicate stage id across content");
                    CheckModifiers(where, stage.modifiers);
                    CheckRewardItems(db, where, stage.rewards);
                    if (!ValidStageKinds.Contains(stage.kind ?? ""))
                        Errors.Add(where + ": invalid kind '" + stage.kind + "'");
                    if (stage.waves.Count == 0) Errors.Add(where + ": no waves");
                    if (stage.hpThresholdStar <= 0f || stage.hpThresholdStar > 1f)
                        Errors.Add(where + ": hpThresholdStar must be in (0,1]");
                    if (stage.resolutionsStar < 1) Errors.Add(where + ": resolutionsStar must be >= 1");
                    foreach (var wave in stage.waves)
                    {
                        if (wave.enemies.Count == 0) Errors.Add(where + ": wave with no enemies");
                        foreach (var we in wave.enemies)
                        {
                            if (!db.Enemies.ContainsKey(we.enemyId ?? ""))
                                Errors.Add(where + ": unknown enemy '" + we.enemyId + "'");
                            if (we.hpMult <= 0f || we.atkMult <= 0f)
                                Errors.Add(where + ": non-positive multiplier");
                        }
                    }
                    CheckDialogueRef(db, where, stage.dialogueBefore);
                    CheckDialogueRef(db, where, stage.dialogueAfter);
                }
                // Chapter-shape guidance from the design spec.
                int minibosses = chapter.stages.Count(s => s.kind == "miniboss");
                bool hasBoss = chapter.stages.Any(s => s.kind == "boss");
                if (chapter.stages.Count >= 20)
                {
                    if (minibosses < 2) Warnings.Add(cwhere + ": fewer than 2 minibosses");
                    if (!hasBoss) Errors.Add(cwhere + ": full chapter missing a boss stage");
                }
            }
        }

        static void CheckDialogueRef(ContentDb db, string where, string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId)) return;
            if (!db.DialogueScenes.ContainsKey(sceneId))
                Errors.Add(where + ": unknown dialogue scene '" + sceneId + "'");
        }

        static readonly HashSet<string> ValidBannerKinds = new HashSet<string> { "standard", "featured", "gatherin", "stepup" };

        static void ValidateBanners(ContentDb db)
        {
            foreach (var b in db.Banners.Values)
            {
                string where = "banner '" + b.id + "'";
                if (string.IsNullOrEmpty(b.name)) Errors.Add(where + ": empty name");
                if (!ValidBannerKinds.Contains((b.kind ?? "").ToLowerInvariant()))
                    Errors.Add(where + ": invalid kind '" + b.kind + "'");
                foreach (var id in b.featured)
                {
                    if (!db.Characters.ContainsKey(id))
                        Errors.Add(where + ": unknown featured character '" + id + "'");
                }
                float total = b.rates.fiveStar + b.rates.fourStar + b.rates.threeStar + b.rates.twoStar + b.rates.oneStar;
                if (Math.Abs(total - 100f) > 0.01f)
                    Errors.Add(where + ": display rates sum to " + total + "%, expected 100%");
                string kind = (b.kind ?? "").ToLowerInvariant();
                if ((kind == "gatherin" || kind == "stepup") && b.steps.Count < 2)
                    Errors.Add(where + ": step banner needs at least 2 steps");
                if (kind == "gatherin")
                {
                    if (b.steps.Count > 0 && b.steps[b.steps.Count - 1].pullCount != 10)
                        Errors.Add(where + ": Gather-In final step must be a 10-character step");
                    if (b.rotationIndex < 1)
                        Errors.Add(where + ": Gather-In banner needs rotationIndex >= 1");
                    for (int i = 1; i < b.steps.Count; i++)
                    {
                        if (b.steps[i].gemCost < b.steps[i - 1].gemCost || b.steps[i].pullCount < b.steps[i - 1].pullCount)
                            Errors.Add(where + ": Gather-In steps must increase in cost and pull count");
                    }
                }
                foreach (var step in b.steps)
                {
                    if (step.gemCost <= 0 || step.pullCount <= 0)
                        Errors.Add(where + ": step with non-positive cost/count");
                }
                if (b.poolTags.Count == 0 && !db.Characters.Values.Any(c => c.rarity >= b.minPoolRarity))
                    Errors.Add(where + ": empty character pool");
            }
        }

        static readonly HashSet<string> ValidEventKinds = new HashSet<string>
        { "eventchapter", "materialdungeon", "awakeningstage", "challengetower", "bossrush" };

        static void ValidateEvents(ContentDb db)
        {
            foreach (var e in db.Events.Values)
            {
                string where = "event '" + e.id + "'";
                if (string.IsNullOrEmpty(e.name)) Errors.Add(where + ": empty name");
                if (!ValidEventKinds.Contains((e.kind ?? "").ToLowerInvariant()))
                    Errors.Add(where + ": invalid kind '" + e.kind + "'");
                if (e.stages.Count == 0) Errors.Add(where + ": no stages");
                foreach (var stage in e.stages)
                {
                    if (!string.IsNullOrEmpty(stage.id) && !AllStageIds.Add(stage.id))
                        Errors.Add(where + " stage '" + stage.id + "': duplicate stage id across content");
                    CheckModifiers(where + " stage '" + stage.id + "'", stage.modifiers);
                    CheckRewardItems(db, where + " stage '" + stage.id + "'", stage.rewards);
                    foreach (var wave in stage.waves)
                    {
                        foreach (var we in wave.enemies)
                        {
                            if (!db.Enemies.ContainsKey(we.enemyId ?? ""))
                                Errors.Add(where + " stage '" + stage.id + "': unknown enemy '" + we.enemyId + "'");
                        }
                    }
                    CheckDialogueRef(db, where + " stage '" + stage.id + "'", stage.dialogueBefore);
                    CheckDialogueRef(db, where + " stage '" + stage.id + "'", stage.dialogueAfter);
                }
                if (!string.IsNullOrEmpty(e.rewardTableId) && !db.RewardTables.ContainsKey(e.rewardTableId))
                    Errors.Add(where + ": unknown reward table '" + e.rewardTableId + "'");
            }
        }

        /// <summary>
        /// Stage modifier vocabulary. These are data-driven hooks for core
        /// (Codex) to interpret; presentation only displays them. Formats:
        /// prefix or prefix:number.
        /// </summary>
        static readonly HashSet<string> ValidModifierPrefixes = new HashSet<string>
        {
            "elite",              // elite encounter flag (better rewards, harder stats)
            "start_locks",        // N orbs start locked
            "start_poison",       // N orbs start poisoned
            "start_blockers",     // N blockers on the board at start
            "move_time_minus",    // movement timer reduced by N seconds
            "combo_shield",       // enemies only take damage at N+ combo
            "enemy_haste",        // enemy countdowns reduced by 1
            "no_heart_orbs",      // Heart orbs do not spawn
            "mono_element_only",  // party restricted to one element
            "element_bonus",      // stage grants a bonus to an element (display tag)
            "healing_reduced"     // healing halved
        };

        static void CheckModifiers(string where, List<string> modifiers)
        {
            foreach (var mod in modifiers)
            {
                var prefix = mod.Split(':')[0];
                if (!ValidModifierPrefixes.Contains(prefix))
                    Errors.Add(where + ": unknown stage modifier '" + mod + "'");
            }
        }

        static void CheckRewardItems(ContentDb db, string where, StageRewardDto rewards)
        {
            if (rewards == null) return;
            foreach (var item in rewards.items)
            {
                if (!db.Items.ContainsKey(item.id ?? ""))
                    Errors.Add(where + ": unknown reward item '" + item.id + "'");
                if (item.count < 1) Errors.Add(where + ": reward item count < 1");
            }
        }

        static void ValidateItems(ContentDb db)
        {
            var validCategories = new HashSet<string> { "material", "awakening", "token", "trophy", "enhance" };
            foreach (var item in db.Items.Values)
            {
                string where = "item '" + item.id + "'";
                if (string.IsNullOrEmpty(item.name)) Errors.Add(where + ": empty name");
                if (!validCategories.Contains(item.category ?? ""))
                    Errors.Add(where + ": invalid category '" + item.category + "'");
                if (item.rarity < 1 || item.rarity > 5) Errors.Add(where + ": rarity must be 1..5");
            }
            // Every reward-table item id must be declared.
            foreach (var table in db.RewardTables.Values)
            {
                foreach (var entry in table.entries)
                {
                    if (entry.itemId == "gold" || entry.itemId == "gems") continue;
                    if (!db.Items.ContainsKey(entry.itemId ?? ""))
                        Errors.Add("reward table '" + table.id + "': unknown item '" + entry.itemId + "'");
                }
            }
            // Event tokens must be declared.
            foreach (var ev in db.Events.Values)
            {
                if (!string.IsNullOrEmpty(ev.tokenId) && !db.Items.ContainsKey(ev.tokenId))
                    Errors.Add("event '" + ev.id + "': unknown token item '" + ev.tokenId + "'");
            }
        }

        static void ValidatePackThemes(ContentDb db)
        {
            var validFoils = new HashSet<string> { "none", "classic", "prismatic", "obsidian" };
            var validAccents = new HashSet<string> { "fire", "water", "nature", "light", "dark", "heart", "gold", "mint" };
            var validTease = new HashSet<string> { "subtle", "dramatic" };
            foreach (var theme in db.PackThemes.Values)
            {
                string where = "pack theme '" + theme.id + "'";
                if (!validFoils.Contains(theme.foilStyle ?? "")) Errors.Add(where + ": invalid foilStyle");
                if (!validAccents.Contains(theme.accent ?? "")) Errors.Add(where + ": invalid accent");
                if (!validTease.Contains(theme.teaseStyle ?? "")) Errors.Add(where + ": invalid teaseStyle");
                if (theme.baseTier < 0 || theme.baseTier > 2) Errors.Add(where + ": baseTier must be 0..2");
            }
            // Every banner must reference a defined pack theme.
            foreach (var banner in db.Banners.Values)
            {
                string art = string.IsNullOrEmpty(banner.packArt) ? banner.id : banner.packArt;
                if (!db.PackThemes.ContainsKey(art))
                    Errors.Add("banner '" + banner.id + "': no pack theme definition for '" + art + "'");
            }
        }

        static void ValidateSchedule(ContentDb db)
        {
            var ids = new HashSet<string>();
            foreach (var entry in db.Schedule)
            {
                string where = "schedule '" + entry.id + "'";
                if (!ids.Add(entry.id)) Errors.Add(where + ": duplicate schedule id");
                string kind = (entry.kind ?? "").ToLowerInvariant();
                if (kind == "banner")
                {
                    if (!db.Banners.ContainsKey(entry.targetId ?? ""))
                        Errors.Add(where + ": unknown banner target '" + entry.targetId + "'");
                }
                else
                {
                    if (!db.Events.ContainsKey(entry.targetId ?? ""))
                        Errors.Add(where + ": unknown event target '" + entry.targetId + "'");
                }
                if (!entry.permanent && entry.recurrence == "none")
                {
                    if (!DateTime.TryParse(entry.startUtc, out _) || !DateTime.TryParse(entry.endUtc, out _))
                        Errors.Add(where + ": invalid start/end dates");
                }
                if (entry.recurrence == "weekly" && (entry.weekday < 0 || entry.weekday > 6))
                    Errors.Add(where + ": weekday must be 0..6");
            }
        }
    }
}
