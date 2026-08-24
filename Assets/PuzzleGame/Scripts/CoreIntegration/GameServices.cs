using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PuzzleGame.Core.Battle;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Economy;
using PuzzleGame.Core.Gacha;
using PuzzleGame.Core.Persistence;
using PuzzleGame.Core.Progression;
using PuzzleGame.Core.Scheduling;
using PuzzleGame.Core.Stages;
using PuzzleGame.Presentation.Content;
using PuzzleGame.Presentation.UI;
using PuzzleGame.Unity.Persistence;
using UnityEngine;

namespace PuzzleGame.Presentation.CoreIntegration
{
    /// <summary>
    /// Composition root for the real game: loads the authored content bank,
    /// translates it onto core contracts, restores the player profile
    /// through core's save system, and owns the single instances of the
    /// core services (wallet, materials, collection, catalogs, scheduler,
    /// per-banner summon services). All state mutation flows through core
    /// public APIs; this class adds no gameplay rules of its own.
    /// </summary>
    public sealed class GameServices
    {
        static GameServices _instance;
        public static GameServices Instance
        {
            get
            {
                if (_instance == null) _instance = Create(null, null);
                return _instance;
            }
        }

        public static void ResetForTests() { _instance = null; }
        public static void InstallForTests(GameServices services) { _instance = services; }

        public ContentDb Db { get; private set; }
        public TranslatedContent Content { get; private set; }
        public Wallet Wallet { get; private set; }
        public MaterialInventory Materials { get; private set; }
        public SummonCollection Collection { get; private set; }
        public StageCatalog StageCatalog { get; private set; }
        public RotationScheduler Scheduler { get; private set; }
        public IClock Clock { get; private set; }
        public SettingsSaveData Settings { get; private set; }

        readonly SaveService saveService;
        readonly System.Random seedSource;
        readonly HashSet<string> ownedIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> knownMaterialIds = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, BannerRuntimeState> bannerStates = new Dictionary<string, BannerRuntimeState>(StringComparer.Ordinal);
        readonly Dictionary<string, SummonService> summonServices = new Dictionary<string, SummonService>(StringComparer.Ordinal);
        readonly Dictionary<string, StageProgressSaveData> stageProgress = new Dictionary<string, StageProgressSaveData>(StringComparer.Ordinal);
        List<SummonCharacterDefinition> summonCatalog;
        string[] partyIds = { "", "", "", "", "" };

        /// <summary>Raised whenever wallet/collection/progress state changed.</summary>
        public event Action StateChanged;

        GameServices(ISaveStorage storage, IClock clock, int seed)
        {
            Db = ContentDb.Instance;
            Content = ContentTranslator.Translate(Db);
            Clock = clock ?? new SystemLocalClock();
            Scheduler = new RotationScheduler(Clock);
            Wallet = new Wallet();
            Materials = new MaterialInventory();
            seedSource = new System.Random(seed);
            StageCatalog = new StageCatalog(Content.Stages.Values, Content.Enemies.Values);

            if (storage == null)
            {
                var path = Path.Combine(Application.persistentDataPath, "puzzlegame_save.json");
                storage = new FileSaveStorage(path);
            }
            saveService = new SaveService(storage);
            var load = saveService.LoadOrCreate();
            var initialProgress = RestoreFromSave(load.Data);
            bool seeded = false;
            if (initialProgress.Count == 0)
            {
                SeedNewProfile(initialProgress);
                seeded = true;
            }
            // The collection's public add path is the summon transaction, so
            // restored and seeded characters enter through its constructor.
            Collection = new SummonCollection(initialProgress);
            foreach (var progress in initialProgress) ownedIds.Add(progress.CharacterId);
            if (seeded) SaveNow();
            SyncSettingsToPresentation();
        }

        public static GameServices Create(ISaveStorage storage, IClock clock, int seed = 0)
        {
            return new GameServices(storage, clock, seed == 0 ? Environment.TickCount : seed);
        }

        // ------------------------------ profile ------------------------------

        List<CharacterProgress> RestoreFromSave(SaveData data)
        {
            var initialProgress = new List<CharacterProgress>();
            if (data == null) data = SaveData.CreateDefault();
            Settings = data.Settings ?? new SettingsSaveData();

            if (data.Wallet != null)
            {
                if (data.Wallet.Gold > 0) Wallet.Add(WalletCurrencies.Gold, data.Wallet.Gold);
                if (data.Wallet.Gems > 0) Wallet.Add(WalletCurrencies.Gems, data.Wallet.Gems);
                if (data.Wallet.Tickets > 0) Wallet.Add(WalletCurrencies.Tickets, data.Wallet.Tickets);
                if (data.Wallet.UniversalDuplicateResource > 0) Wallet.Add(WalletCurrencies.UniversalDuplicateResource, data.Wallet.UniversalDuplicateResource);
            }
            foreach (var material in data.Materials ?? Array.Empty<MaterialBalanceSaveData>())
            {
                knownMaterialIds.Add(material.MaterialId);
                if (material.Balance > 0) Materials.Add(material.MaterialId, material.Balance);
            }

            foreach (var character in data.Characters ?? Array.Empty<CharacterProgressSaveData>())
            {
                CharacterData authored;
                if (!Content.Characters.TryGetValue(character.CharacterId, out authored)) continue;
                var progress = CreateProgress(character.CharacterId);
                // Restore exclusively through public core transitions.
                if (character.TotalExperience > 0) ProgressionService.ApplyExperience(progress, character.TotalExperience);
                for (var rank = 0; rank < Math.Min(5, character.Ascension); rank++) ProgressionService.ApplyDuplicate(progress, Wallet);
                if (character.Awakened) RestoreAwakened(progress);
                initialProgress.Add(progress);
            }

            partyIds = (string[])(data.PartyCharacterIds ?? new[] { "", "", "", "", "" }).Clone();
            if (partyIds.Length != 5) partyIds = new[] { "", "", "", "", "" };

            foreach (var banner in data.Banners ?? Array.Empty<BannerRuntimeSaveData>())
            {
                BannerData authored;
                if (!Content.Banners.TryGetValue(banner.BannerId, out authored)) continue;
                if (authored.Steps.Length != banner.AuthoredStepCount) continue;   // authored shape changed: reset
                bannerStates[banner.BannerId] = new BannerRuntimeState(banner.BannerId, banner.RotationId, banner.AuthoredStepCount, banner.NextStepIndex);
            }

            foreach (var stage in data.Stages ?? Array.Empty<StageProgressSaveData>())
            {
                stageProgress[stage.StageId] = stage;
            }
            return initialProgress;
        }

        void RestoreAwakened(CharacterProgress progress)
        {
            var requirements = progress.AuthoredData.Awakening;
            if (requirements.RequiredLevel == 0) return;
            if (progress.Level < requirements.RequiredLevel)
            {
                var curve = progress.AuthoredData.LevelCurve;
                var cap = curve.MaxLevel == 1 ? 0 : curve.ExperienceRequiredByLevel[curve.ExperienceRequiredByLevel.Length - 1];
                ProgressionService.ApplyExperience(progress, cap);
            }
            // Grant exactly the requirement, then run the real transaction; net zero.
            Wallet.Add(WalletCurrencies.Gold, requirements.GoldCost);
            foreach (var material in requirements.Materials)
            {
                knownMaterialIds.Add(material.MaterialId);
                Materials.Add(material.MaterialId, material.Amount);
            }
            ProgressionService.Awaken(progress, Wallet, Materials);
        }

        void SeedNewProfile(List<CharacterProgress> initialProgress)
        {
            Wallet.Add(WalletCurrencies.Gems, 3000);
            Wallet.Add(WalletCurrencies.Gold, 50000);
            Wallet.Add(WalletCurrencies.Tickets, 2);
            GrantMaterial("radiant_core", 3);
            var starters = new[] { "fire_sparky", "water_mildred", "nature_randy", "light_gus", "dark_pete" };
            for (var index = 0; index < starters.Length; index++)
            {
                if (!Content.Characters.ContainsKey(starters[index])) continue;
                if (initialProgress.Any(p => p.CharacterId == starters[index])) continue;
                var progress = CreateProgress(starters[index]);
                // Starters arrive combat-ready at level 10 so the first hour
                // isn't a wall (2250 = the authored curve's level-10 total).
                ProgressionService.ApplyExperience(progress, 2250);
                initialProgress.Add(progress);
                partyIds[index] = starters[index];
            }
        }

        public CharacterProgress CreateProgress(string characterId)
        {
            var authored = Content.Characters[characterId];
            return new CharacterProgress(authored, Content.GetActiveSkill(characterId));
        }

        // ------------------------------ roster -------------------------------

        public IReadOnlyCollection<string> OwnedCharacterIds { get { return ownedIds; } }
        public bool IsOwned(string characterId) { return ownedIds.Contains(characterId); }

        public CharacterProgress GetProgress(string characterId)
        {
            return Collection.Contains(characterId) ? Collection.GetProgress(characterId) : null;
        }

        public IReadOnlyList<string> PartyIds { get { return Array.AsReadOnly(partyIds); } }

        public List<CharacterProgress> ResolveParty()
        {
            var result = new List<CharacterProgress>();
            foreach (var id in partyIds)
            {
                if (string.IsNullOrEmpty(id) || !Collection.Contains(id)) continue;
                if (result.Any(p => p.CharacterId == id)) continue;
                result.Add(Collection.GetProgress(id));
            }
            if (result.Count < PartyState.RequiredMemberCount)
            {
                foreach (var id in ownedIds.OrderByDescending(o => GetProgress(o).EffectiveRarity).ThenBy(o => o, StringComparer.Ordinal))
                {
                    if (result.Count >= PartyState.RequiredMemberCount) break;
                    if (result.Any(p => p.CharacterId == id)) continue;
                    result.Add(Collection.GetProgress(id));
                }
                for (var index = 0; index < partyIds.Length; index++)
                    partyIds[index] = index < result.Count ? result[index].CharacterId : "";
            }
            return result;
        }

        public bool TrySpendGoldForLevels(string characterId, int levels)
        {
            var progress = GetProgress(characterId);
            if (progress == null || levels <= 0) return false;
            var curve = progress.AuthoredData.LevelCurve;
            int targetLevel = Math.Min(curve.MaxLevel, progress.Level + levels);
            int gained = targetLevel - progress.Level;
            if (gained <= 0) return false;
            int targetExperience = targetLevel <= 1 ? 0 : curve.ExperienceRequiredByLevel[targetLevel - 2];
            int neededExperience = Math.Max(0, targetExperience - progress.TotalExperience);
            long cost = (long)gained * 400 * Math.Max(1, progress.AuthoredData.BaseRarity);
            if (cost > int.MaxValue || !Wallet.TrySpend(WalletCurrencies.Gold, (int)cost)) return false;
            ProgressionService.ApplyExperience(progress, neededExperience);
            SaveNow();
            RaiseStateChanged();
            return true;
        }

        public AwakeningResult TryAwaken(string characterId)
        {
            var progress = GetProgress(characterId);
            if (progress == null) return new AwakeningResult(false, "NotOwned", null);
            var result = ProgressionService.Awaken(progress, Wallet, Materials);
            if (result.Succeeded)
            {
                SaveNow();
                RaiseStateChanged();
            }
            return result;
        }

        public void GrantMaterial(string materialId, int amount)
        {
            knownMaterialIds.Add(materialId);
            Materials.Add(materialId, amount);
        }

        // ------------------------------ summons ------------------------------

        public SummonService GetSummonService(string bannerId)
        {
            SummonService service;
            if (summonServices.TryGetValue(bannerId, out service)) return service;
            BannerData banner;
            if (!Content.Banners.TryGetValue(bannerId, out banner)) return null;
            if (summonCatalog == null)
            {
                summonCatalog = new List<SummonCharacterDefinition>();
                foreach (var character in Content.Characters.Values)
                    summonCatalog.Add(new SummonCharacterDefinition(character, Content.GetActiveSkill(character.Id)));
            }
            BannerRuntimeState state;
            if (!bannerStates.TryGetValue(bannerId, out state))
            {
                state = new BannerRuntimeState(banner.Id, banner.Id, banner.Steps.Length);
                bannerStates[bannerId] = state;
            }
            service = new SummonService(banner, summonCatalog, Wallet, new SystemRandomSource(seedSource.Next()), Collection, state);
            summonServices[bannerId] = service;
            return service;
        }

        /// <summary>Track newly owned characters from a completed core summon batch.</summary>
        public void RegisterSummonBatch(SummonBatch batch)
        {
            foreach (var result in batch.Results)
            {
                if (result.Ownership == SummonOwnership.New) ownedIds.Add(result.CharacterId);
            }
            SaveNow();
            RaiseStateChanged();
        }

        // ------------------------------ schedule -----------------------------

        public IReadOnlyList<ActiveContent> GetActiveContent()
        {
            return Scheduler.GetActive(Content.Schedules);
        }

        // ------------------------------ stages -------------------------------

        public StageProgressSaveData GetStageProgress(string stageId)
        {
            StageProgressSaveData progress;
            return stageProgress.TryGetValue(stageId, out progress) ? progress : null;
        }

        /// <summary>
        /// Commits a finished stage run: persists clear/star bests and grants
        /// authored rewards (repeat clears re-grant gold; first clear grants
        /// the full reward list).
        /// </summary>
        public void RecordStageCompletion(string stageId, IReadOnlyList<StarResult> stars, float finalHpRatio, int boardResolutions)
        {
            StageProgressSaveData progress;
            if (!stageProgress.TryGetValue(stageId, out progress))
            {
                progress = new StageProgressSaveData { StageId = stageId };
                stageProgress[stageId] = progress;
            }
            bool firstClear = !progress.IsCleared;
            progress.IsCleared = true;
            for (var index = 0; index < Math.Min(3, stars.Count); index++)
                progress.Stars[index] |= stars[index].Earned;
            progress.BestFinalHpBasisPoints = Math.Max(progress.BestFinalHpBasisPoints, (int)Math.Round(finalHpRatio * 10000f));
            progress.BestBoardResolutionCount = progress.BestBoardResolutionCount == 0
                ? boardResolutions
                : Math.Min(progress.BestBoardResolutionCount, boardResolutions);

            StageData stage;
            if (Content.Stages.TryGetValue(stageId, out stage))
            {
                foreach (var reward in stage.ClearRewards)
                {
                    if (!firstClear && reward.Type != RewardType.Gold) continue;
                    GrantReward(reward);
                }
            }
            SaveNow();
            RaiseStateChanged();
        }

        void GrantReward(RewardData reward)
        {
            switch (reward.Type)
            {
                case RewardType.Gold: Wallet.Add(WalletCurrencies.Gold, reward.Amount); break;
                case RewardType.Gems: Wallet.Add(WalletCurrencies.Gems, reward.Amount); break;
                case RewardType.Tickets: Wallet.Add(WalletCurrencies.Tickets, reward.Amount); break;
                case RewardType.UniversalDuplicateResource: Wallet.Add(WalletCurrencies.UniversalDuplicateResource, reward.Amount); break;
                case RewardType.Material:
                case RewardType.EventCurrency:
                    GrantMaterial(reward.ItemId, reward.Amount);
                    break;
            }
        }

        /// <summary>Total earned stars for a chapter (for milestone display).</summary>
        public int GetChapterStars(IEnumerable<string> stageIds)
        {
            var total = 0;
            foreach (var id in stageIds)
            {
                var progress = GetStageProgress(id);
                if (progress == null) continue;
                for (var index = 0; index < progress.Stars.Length; index++) if (progress.Stars[index]) total++;
            }
            return total;
        }

        // ------------------------------- save --------------------------------

        public void SaveNow()
        {
            var data = new SaveData();
            data.Wallet = new WalletSaveData
            {
                Gold = Wallet.GetBalance(WalletCurrencies.Gold),
                Gems = Wallet.GetBalance(WalletCurrencies.Gems),
                Tickets = Wallet.GetBalance(WalletCurrencies.Tickets),
                UniversalDuplicateResource = Wallet.GetBalance(WalletCurrencies.UniversalDuplicateResource)
            };
            data.Materials = knownMaterialIds
                .Select(id => new MaterialBalanceSaveData { MaterialId = id, Balance = Materials.GetBalance(id) })
                .ToArray();
            data.Characters = ownedIds
                .Where(id => Collection.Contains(id))
                .Select(id =>
                {
                    var progress = Collection.GetProgress(id);
                    return new CharacterProgressSaveData
                    {
                        CharacterId = id,
                        TotalExperience = progress.TotalExperience,
                        Ascension = progress.Ascension,
                        Awakened = progress.IsAwakened
                    };
                })
                .ToArray();
            data.PartyCharacterIds = (string[])partyIds.Clone();
            data.Banners = bannerStates.Values
                .Select(state => new BannerRuntimeSaveData
                {
                    BannerId = state.BannerId,
                    RotationId = state.RotationId,
                    AuthoredStepCount = state.StepCount,
                    NextStepIndex = state.NextStepIndex
                })
                .ToArray();
            data.Stages = stageProgress.Values.ToArray();
            data.Settings = PullSettingsFromPresentation();
            saveService.Save(data);
        }

        public void SetParty(IReadOnlyList<string> ids)
        {
            for (var index = 0; index < partyIds.Length; index++)
                partyIds[index] = ids != null && index < ids.Count ? ids[index] ?? "" : "";
            SaveNow();
            RaiseStateChanged();
        }

        void SyncSettingsToPresentation()
        {
            MotionSettings.ReducedMotion = Settings.ReducedMotion;
        }

        SettingsSaveData PullSettingsFromPresentation()
        {
            Settings.ReducedMotion = MotionSettings.ReducedMotion;
            return Settings;
        }

        public void RaiseStateChanged()
        {
            var handler = StateChanged;
            if (handler != null) handler();
        }
    }

    /// <summary>System.Random adapter for both core random seams.</summary>
    public sealed class SystemRandomSource : Core.Board.IRandomSource, Core.Gacha.IRandomSource
    {
        readonly System.Random random;
        public SystemRandomSource(int seed) { random = new System.Random(seed); }
        public int Next(int exclusiveMaximum) { return random.Next(exclusiveMaximum); }
        public int NextInt(int exclusiveUpperBound) { return random.Next(exclusiveUpperBound); }
    }

}
