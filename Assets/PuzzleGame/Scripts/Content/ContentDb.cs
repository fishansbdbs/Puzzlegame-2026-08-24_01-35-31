using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PuzzleGame.Presentation.Content
{
    /// <summary>
    /// Loads all JSON content under Resources/Content/* and exposes it to
    /// presentation code. Discovery is folder-based (Resources.LoadAll), so
    /// new content files require no code changes.
    /// </summary>
    public class ContentDb : IContentLibrary
    {
        static ContentDb _instance;
        public static ContentDb Instance => _instance ?? (_instance = Load());

        public readonly Dictionary<string, CharacterDto> Characters = new Dictionary<string, CharacterDto>();
        public readonly Dictionary<string, EnemyDto> Enemies = new Dictionary<string, EnemyDto>();
        public readonly Dictionary<int, ChapterDto> Chapters = new Dictionary<int, ChapterDto>();
        public readonly Dictionary<string, DialogueSceneDto> DialogueScenes = new Dictionary<string, DialogueSceneDto>();
        public readonly Dictionary<string, BannerDto> Banners = new Dictionary<string, BannerDto>();
        public readonly Dictionary<string, EventDto> Events = new Dictionary<string, EventDto>();
        public readonly Dictionary<string, RewardTableDto> RewardTables = new Dictionary<string, RewardTableDto>();
        public readonly List<ScheduleEntryDto> Schedule = new List<ScheduleEntryDto>();
        public readonly Dictionary<string, ItemDto> Items = new Dictionary<string, ItemDto>();
        public readonly Dictionary<string, PackThemeDto> PackThemes = new Dictionary<string, PackThemeDto>();

        /// <summary>Load anomalies (duplicate ids across files, etc.) for the validator.</summary>
        public readonly List<string> LoadIssues = new List<string>();

        void Put<T>(Dictionary<string, T> map, string id, T value, string what)
        {
            if (string.IsNullOrEmpty(id))
            {
                LoadIssues.Add(what + " entry with empty id");
                return;
            }
            if (map.ContainsKey(id))
            {
                LoadIssues.Add("duplicate " + what + " id '" + id + "' across content files");
            }
            map[id] = value;
        }

        public static ContentDb Load()
        {
            var db = new ContentDb();
            foreach (var file in Resources.LoadAll<TextAsset>("Content/characters"))
            {
                var parsed = JsonUtility.FromJson<CharacterFileDto>(file.text);
                if (parsed?.characters == null) { Warn(file); continue; }
                foreach (var c in parsed.characters) db.Put(db.Characters, c.id, c, "character");
            }
            foreach (var file in Resources.LoadAll<TextAsset>("Content/enemies"))
            {
                var parsed = JsonUtility.FromJson<EnemyFileDto>(file.text);
                if (parsed?.enemies == null) { Warn(file); continue; }
                foreach (var e in parsed.enemies) db.Put(db.Enemies, e.id, e, "enemy");
            }
            foreach (var file in Resources.LoadAll<TextAsset>("Content/stages"))
            {
                var parsed = JsonUtility.FromJson<ChapterFileDto>(file.text);
                if (parsed?.chapter == null) { Warn(file); continue; }
                if (db.Chapters.ContainsKey(parsed.chapter.chapterNumber))
                {
                    db.LoadIssues.Add("duplicate chapter number " + parsed.chapter.chapterNumber + " across content files");
                }
                db.Chapters[parsed.chapter.chapterNumber] = parsed.chapter;
            }
            foreach (var file in Resources.LoadAll<TextAsset>("Content/dialogue"))
            {
                var parsed = JsonUtility.FromJson<DialogueFileDto>(file.text);
                if (parsed?.scenes == null) { Warn(file); continue; }
                foreach (var s in parsed.scenes) db.Put(db.DialogueScenes, s.id, s, "dialogue scene");
            }
            foreach (var file in Resources.LoadAll<TextAsset>("Content/banners"))
            {
                var parsed = JsonUtility.FromJson<BannerFileDto>(file.text);
                if (parsed?.banners == null) { Warn(file); continue; }
                foreach (var b in parsed.banners) db.Put(db.Banners, b.id, b, "banner");
            }
            foreach (var file in Resources.LoadAll<TextAsset>("Content/events"))
            {
                var parsed = JsonUtility.FromJson<EventFileDto>(file.text);
                if (parsed?.events == null) { Warn(file); continue; }
                foreach (var e in parsed.events) db.Put(db.Events, e.id, e, "event");
            }
            foreach (var file in Resources.LoadAll<TextAsset>("Content/rewards"))
            {
                var parsed = JsonUtility.FromJson<RewardFileDto>(file.text);
                if (parsed?.tables == null) { Warn(file); continue; }
                foreach (var t in parsed.tables) db.Put(db.RewardTables, t.id, t, "reward table");
            }
            foreach (var file in Resources.LoadAll<TextAsset>("Content/schedule"))
            {
                var parsed = JsonUtility.FromJson<ScheduleFileDto>(file.text);
                if (parsed?.entries == null) { Warn(file); continue; }
                db.Schedule.AddRange(parsed.entries);
            }
            foreach (var file in Resources.LoadAll<TextAsset>("Content/items"))
            {
                var parsed = JsonUtility.FromJson<ItemFileDto>(file.text);
                if (parsed?.items == null) { Warn(file); continue; }
                foreach (var i in parsed.items) db.Put(db.Items, i.id, i, "item");
            }
            foreach (var file in Resources.LoadAll<TextAsset>("Content/packs"))
            {
                var parsed = JsonUtility.FromJson<PackThemeFileDto>(file.text);
                if (parsed?.themes == null) { Warn(file); continue; }
                foreach (var t in parsed.themes) db.Put(db.PackThemes, t.id, t, "pack theme");
            }
            return db;
        }

        static void Warn(TextAsset file)
        {
            Debug.LogWarning("[ContentDb] Could not parse content file: " + file.name);
        }

        // ------------------------- Conversion ----------------------------

        public static ElementId ParseElement(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "fire": return ElementId.Fire;
                case "water": return ElementId.Water;
                case "nature": return ElementId.Nature;
                case "light": return ElementId.Light;
                case "dark": return ElementId.Dark;
                default: return ElementId.Fire;
            }
        }

        public static BannerKind ParseBannerKind(string value)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "featured": return BannerKind.Featured;
                case "gatherin": return BannerKind.GatherIn;
                case "stepup": return BannerKind.StepUp;
                default: return BannerKind.Standard;
            }
        }

        /// <summary>Build a display view for a character at a given progression state.</summary>
        public CharacterView ToView(CharacterDto dto, int level = -1, int ascension = 0, bool awakened = false)
        {
            if (dto == null) return null;
            int lvl = level < 0 ? dto.maxLevel : Mathf.Clamp(level, 1, dto.maxLevel);
            float t = dto.maxLevel <= 1 ? 1f : (lvl - 1f) / (dto.maxLevel - 1f);
            // Simple display curve: 40% of max stats at level 1.
            int hp = Mathf.RoundToInt(Mathf.Lerp(dto.hp * 0.4f, dto.hp, t));
            int atk = Mathf.RoundToInt(Mathf.Lerp(dto.atk * 0.4f, dto.atk, t));
            int rec = Mathf.RoundToInt(Mathf.Lerp(dto.rec * 0.4f, dto.rec, t));
            if (awakened && dto.HasAwakenedForm)
            {
                hp += dto.awakened.hpBonus;
                atk += dto.awakened.atkBonus;
                rec += dto.awakened.recBonus;
            }
            var view = new CharacterView
            {
                Id = dto.id,
                DisplayName = dto.name,
                Epithet = awakened && dto.HasAwakenedForm && !string.IsNullOrEmpty(dto.awakened.epithet)
                    ? dto.awakened.epithet : dto.epithet,
                Element = ParseElement(dto.element),
                BaseRarity = dto.rarity,
                Awakened = awakened,
                Level = lvl,
                LevelMax = dto.maxLevel,
                Hp = hp,
                Atk = atk,
                Rec = rec,
                Ascension = ascension,
                AscensionCap = 5,
                ActiveSkill = dto.activeSkill == null ? null : new SkillView
                {
                    Name = dto.activeSkill.name,
                    Description = dto.activeSkill.desc,
                    Charge = 0,
                    ChargeMax = dto.activeSkill.chargeMax
                },
                LeaderSkill = dto.leaderSkill == null ? null : new LeaderSkillView
                {
                    Name = dto.leaderSkill.name,
                    Description = dto.leaderSkill.desc
                },
                PassiveName = dto.passive != null ? dto.passive.name : "",
                PassiveDescription = dto.passive != null ? dto.passive.desc : "",
                Tags = new List<string>(dto.tags),
                ArtRef = dto.artRef,
                AwakenedArtRef = dto.HasAwakenedForm ? dto.awakened.artRef : "",
                VfxRef = dto.vfxRef,
                AwakenedVfxRef = dto.HasAwakenedForm ? dto.awakened.vfxRef : "",
                Flavor = dto.flavor
            };
            return view;
        }

        // ------------------------ IContentLibrary ------------------------

        public CharacterView GetCharacter(string id)
        {
            return Characters.TryGetValue(id ?? "", out var dto) ? ToView(dto) : null;
        }

        public IReadOnlyList<CharacterView> GetAllCharacters()
        {
            return Characters.Values.Select(c => ToView(c)).ToList();
        }

        public IReadOnlyList<StageSummaryVm> GetChapterStages(int chapterNumber)
        {
            if (!Chapters.TryGetValue(chapterNumber, out var chapter)) return new List<StageSummaryVm>();
            return chapter.stages.Select(s => new StageSummaryVm
            {
                Id = s.id,
                DisplayName = s.name,
                ChapterNumber = chapterNumber,
                StageNumber = s.stageNumber,
                Unlocked = true,
                BossPreview = s.kind == "boss" || s.kind == "miniboss"
                    ? DescribeBoss(s) : ""
            }).ToList();
        }

        string DescribeBoss(StageDto stage)
        {
            foreach (var wave in Enumerable.Reverse(stage.waves))
            {
                foreach (var we in wave.enemies)
                {
                    if (Enemies.TryGetValue(we.enemyId, out var enemy) && enemy.boss)
                    {
                        return enemy.name;
                    }
                }
            }
            return "";
        }

        public int GetChapterCount()
        {
            return Chapters.Count == 0 ? 0 : Chapters.Keys.Max();
        }

        public string GetChapterTitle(int chapterNumber)
        {
            return Chapters.TryGetValue(chapterNumber, out var chapter) ? chapter.title : "???";
        }
    }
}
