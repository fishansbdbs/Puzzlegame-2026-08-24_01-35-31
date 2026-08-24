using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Economy;
using PuzzleGame.Core.Progression;

namespace PuzzleGame.Core.Gacha
{
    public enum SummonOwnership { New, Duplicate }

    public sealed class SummonCharacterDefinition
    {
        public SummonCharacterDefinition(CharacterData character, SkillData activeSkill)
        {
            if (character == null) throw new ArgumentNullException("character");
            if (activeSkill == null) throw new ArgumentNullException("activeSkill");
            Character = character; ActiveSkill = activeSkill;
        }
        public CharacterData Character { get; private set; }
        public SkillData ActiveSkill { get; private set; }
    }

    public sealed class SummonCollection
    {
        private readonly Dictionary<string, CharacterProgress> progressById = new Dictionary<string, CharacterProgress>(StringComparer.Ordinal);
        public SummonCollection(IEnumerable<CharacterProgress> initialProgress = null)
        {
            if (initialProgress == null) return;
            foreach (var progress in initialProgress)
            {
                if (progress == null || progressById.ContainsKey(progress.CharacterId)) throw new ArgumentException("Collection progress must be non-null and have unique IDs.", "initialProgress");
                progressById.Add(progress.CharacterId, progress);
            }
        }
        public bool Contains(string characterId) { return !string.IsNullOrWhiteSpace(characterId) && progressById.ContainsKey(characterId); }
        public CharacterProgress GetProgress(string characterId)
        {
            CharacterProgress progress;
            if (!progressById.TryGetValue(characterId, out progress)) throw new KeyNotFoundException("Character is not owned: " + characterId);
            return progress;
        }
        internal Dictionary<string, CharacterProgress> CreateTransactionSnapshots()
        {
            var snapshots = new Dictionary<string, CharacterProgress>(StringComparer.Ordinal);
            foreach (var pair in progressById) snapshots.Add(pair.Key, pair.Value.CreateTransactionSnapshot());
            return snapshots;
        }
        internal void CommitValidatedNew(CharacterProgress progress) { progressById.Add(progress.CharacterId, progress); }
    }

    public sealed class SummonQuote
    {
        internal SummonQuote(bool available, int pullCount, int gems, int step, string failure) { IsAvailable = available; PullCount = pullCount; GemCost = gems; StepIndex = step; FailureCode = failure; }
        public bool IsAvailable { get; private set; }
        public int PullCount { get; private set; }
        public int GemCost { get; private set; }
        public int StepIndex { get; private set; }
        public string FailureCode { get; private set; }
    }

    public sealed class DuplicateSummonDetail
    {
        internal DuplicateSummonDetail(DuplicateApplicationResult result) { PreviousAscension = result.PreviousAscension; NewAscension = result.NewAscension; UniversalResourceGranted = result.UniversalResourceGranted; }
        public int PreviousAscension { get; private set; }
        public int NewAscension { get; private set; }
        public int UniversalResourceGranted { get; private set; }
    }

    public sealed class SummonResult
    {
        internal SummonResult(string id, int rarity, ElementType element, SummonOwnership ownership, DuplicateSummonDetail duplicate)
        { CharacterId = id; Rarity = rarity; Element = element; Ownership = ownership; Duplicate = duplicate; }
        public string CharacterId { get; private set; }
        public int Rarity { get; private set; }
        public ElementType Element { get; private set; }
        public SummonOwnership Ownership { get; private set; }
        public DuplicateSummonDetail Duplicate { get; private set; }
    }

    public sealed class SummonBatch
    {
        internal SummonBatch(string bannerId, string presentationKey, int gems, IList<SummonResult> results)
        {
            BannerId = bannerId; PresentationKey = presentationKey; GemCost = gems;
            var copy = new SummonResult[results.Count]; results.CopyTo(copy, 0); Results = Array.AsReadOnly(copy);
        }
        public string BannerId { get; private set; }
        public string PresentationKey { get; private set; }
        public int GemCost { get; private set; }
        public IReadOnlyList<SummonResult> Results { get; private set; }
        public string PackThickness { get { return Results.Count == 1 ? "Single" : "Multi"; } }
        public bool HasFiveStar { get { for (var i = 0; i < Results.Count; i++) if (Results[i].Rarity == 5) return true; return false; } }
    }

    public sealed class SummonService
    {
        public const int StandardSinglePullGemCost = 150;
        public const int StandardTenPullGemCost = 1500;
        public const int FeaturedGuaranteeWeightMultiplier = 2;
        private readonly BannerSnapshot banner;
        private readonly Dictionary<string, Candidate> catalog;
        private readonly Wallet wallet;
        private readonly IRandomSource random;
        private readonly SummonCollection collection;
        private readonly BannerRuntimeState state;

        public SummonService(BannerData bannerData, IEnumerable<SummonCharacterDefinition> characterCatalog, Wallet wallet, IRandomSource random, SummonCollection collection = null, BannerRuntimeState state = null)
        {
            if (bannerData == null) throw new ArgumentNullException("bannerData");
            if (wallet == null) throw new ArgumentNullException("wallet");
            if (random == null) throw new ArgumentNullException("random");
            ValidateBannerType(bannerData);
            var errors = ContractValidation.Validate(bannerData);
            if (errors.Count > 0) throw new ArgumentException(errors[0], "bannerData");
            banner = new BannerSnapshot(bannerData);
            ValidateBannerShape();
            catalog = BuildCatalog(characterCatalog);
            ValidatePoolAndGuarantees();
            this.wallet = wallet; this.random = random; this.collection = collection ?? new SummonCollection();
            this.state = state ?? new BannerRuntimeState(banner.Id, banner.RotationId, banner.Steps.Length);
            this.state.AssertMatches(banner.Id, banner.RotationId, banner.Steps.Length);
        }

        public Wallet Wallet { get { return wallet; } }
        public SummonCollection Collection { get { return collection; } }
        public BannerRuntimeState State { get { return state; } }
        private bool UsesSteps { get { return banner.Type == BannerType.GatherIn || banner.Type == BannerType.StepUp || (banner.Type == BannerType.Featured && banner.Steps.Length > 0); } }

        public SummonQuote Quote() { return QuoteInternal(null); }
        public SummonQuote Quote(int pullCount) { return QuoteInternal(pullCount); }
        public bool CanSummon() { var quote = Quote(); return quote.IsAvailable && wallet.CanAfford(WalletCurrencies.Gems, quote.GemCost); }
        public bool CanSummon(int pullCount) { var quote = Quote(pullCount); return quote.IsAvailable && wallet.CanAfford(WalletCurrencies.Gems, quote.GemCost); }
        public SummonBatch PurchaseAndRoll() { return PurchaseAndRollInternal(null); }
        public SummonBatch PurchaseAndRoll(int pullCount) { return PurchaseAndRollInternal(pullCount); }

        private SummonQuote QuoteInternal(int? requested)
        {
            if (UsesSteps)
            {
                if (state.IsComplete) return new SummonQuote(false, 0, 0, state.NextStepIndex, "RotationComplete");
                var step = banner.Steps[state.NextStepIndex];
                if (requested.HasValue && requested.Value != step.PullCount) throw new ArgumentOutOfRangeException("pullCount", "Pull count must match the current banner step.");
                return new SummonQuote(true, step.PullCount, step.GemCost, state.NextStepIndex, null);
            }
            if (!requested.HasValue) throw new ArgumentException("Pull count is required for this banner.", "pullCount");
            if (requested.Value != 1 && requested.Value != 10) throw new ArgumentOutOfRangeException("pullCount", "Only one and ten pulls are supported.");
            var cost = banner.Type == BannerType.Standard ? (requested.Value == 1 ? StandardSinglePullGemCost : StandardTenPullGemCost) : (requested.Value == 1 ? banner.SingleCost : banner.TenCost);
            return new SummonQuote(true, requested.Value, cost, -1, null);
        }

        private SummonBatch PurchaseAndRollInternal(int? requested)
        {
            var quote = QuoteInternal(requested);
            if (!quote.IsAvailable) throw new InvalidOperationException(quote.FailureCode);
            if (!wallet.CanAfford(WalletCurrencies.Gems, quote.GemCost)) throw new InvalidOperationException("InsufficientGems");
            var picks = new Candidate[quote.PullCount];
            for (var index = 0; index < picks.Length; index++) picks[index] = Roll(banner.Entries);
            if (UsesSteps) ApplyGuarantee(banner.Steps[state.NextStepIndex], picks);
            var plan = BuildTransactionPlan(picks, quote);

            if (!wallet.TrySpend(WalletCurrencies.Gems, plan.GemCost)) throw new InvalidOperationException("Wallet changed during summon transaction.");
            for (var index = 0; index < plan.Duplicates.Count; index++)
            {
                var duplicate = plan.Duplicates[index];
                if (duplicate.ActualProgress != null) ProgressionService.CommitDuplicatePlan(duplicate.ProgressionPlan, duplicate.ActualProgress, wallet);
                else ProgressionService.CommitDuplicateOverflow(duplicate.ProgressionPlan, wallet);
            }
            for (var index = 0; index < plan.NewProgress.Count; index++) collection.CommitValidatedNew(plan.NewProgress[index]);
            if (plan.AdvancesStep) state.Advance();
            return new SummonBatch(banner.Id, banner.PresentationKey, plan.GemCost, plan.Results);
        }

        private TransactionPlan BuildTransactionPlan(Candidate[] picks, SummonQuote quote)
        {
            var snapshots = collection.CreateTransactionSnapshots();
            var plan = new TransactionPlan(quote.GemCost, UsesSteps);
            long universalResource = 0;
            for (var index = 0; index < picks.Length; index++)
            {
                var candidate = picks[index];
                CharacterProgress working;
                if (!snapshots.TryGetValue(candidate.Id, out working))
                {
                    working = candidate.CreateStagedProgress(); snapshots.Add(candidate.Id, working); plan.NewProgress.Add(working);
                    plan.Results.Add(new SummonResult(candidate.Id, candidate.Rarity, candidate.Element, SummonOwnership.New, null));
                    continue;
                }
                var duplicatePlan = ProgressionService.PlanDuplicate(working);
                ProgressionService.ApplyDuplicatePlanToSnapshot(duplicatePlan, working);
                universalResource = checked(universalResource + duplicatePlan.Result.UniversalResourceGranted);
                CharacterProgress actual = collection.Contains(candidate.Id) ? collection.GetProgress(candidate.Id) : null;
                plan.Duplicates.Add(new PlannedDuplicate(duplicatePlan, actual));
                plan.Results.Add(new SummonResult(candidate.Id, candidate.Rarity, candidate.Element, SummonOwnership.Duplicate, new DuplicateSummonDetail(duplicatePlan.Result)));
            }
            if ((long)wallet.GetBalance(WalletCurrencies.UniversalDuplicateResource) + universalResource > int.MaxValue)
                throw new OverflowException("Duplicate conversion would overflow the universal resource balance.");
            return plan;
        }

        private Candidate Roll(IReadOnlyList<WeightedEntry> entries)
        {
            var weights = new long[entries.Count]; for (var index = 0; index < entries.Count; index++) weights[index] = entries[index].Weight;
            return catalog[entries[GachaRandom.SelectIndex(weights, random)].Id];
        }

        private void ApplyGuarantee(StepSnapshot step, Candidate[] picks)
        {
            if (!step.BoostedFiveStar && !step.FeaturedFiveStar) return;
            var eligible = new List<WeightedEntry>();
            for (var index = 0; index < banner.Entries.Count; index++)
            {
                var entry = banner.Entries[index];
                if (catalog[entry.Id].Rarity == 5 && (!step.FeaturedFiveStar || entry.IsFeatured)) eligible.Add(entry);
            }
            var weights = new long[eligible.Count];
            for (var index = 0; index < eligible.Count; index++) weights[index] = step.BoostedFiveStar && eligible[index].IsFeatured ? checked((long)eligible[index].Weight * FeaturedGuaranteeWeightMultiplier) : eligible[index].Weight;
            picks[picks.Length - 1] = catalog[eligible[GachaRandom.SelectIndex(weights, random)].Id];
        }

        private Dictionary<string, Candidate> BuildCatalog(IEnumerable<SummonCharacterDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException("characterCatalog");
            var result = new Dictionary<string, Candidate>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null || definition.Character == null || definition.ActiveSkill == null) throw new ArgumentException("Catalog definitions must be complete.", "characterCatalog");
                if (definition.Character.Element < ElementType.Fire || definition.Character.Element > ElementType.Dark) throw new ArgumentException("Catalog character element must be a defined playable element.", "characterCatalog");
                var errors = ContractValidation.Validate(definition.Character);
                if (errors.Count > 0) throw new ArgumentException(errors[0], "characterCatalog");
                var character = ProgressionDataSnapshot.Clone(definition.Character); var skill = CloneSkill(definition.ActiveSkill);
                var prototype = new CharacterProgress(character, skill);
                if (result.ContainsKey(character.Id)) throw new ArgumentException("Catalog character IDs must be unique.", "characterCatalog");
                result.Add(character.Id, new Candidate(character.Id, character.BaseRarity, character.Element, prototype));
            }
            return result;
        }

        private void ValidateBannerShape()
        {
            if (banner.Type == BannerType.Standard && banner.Steps.Length > 0) throw new ArgumentException("Standard banners cannot define steps.", "bannerData");
            if ((banner.Type == BannerType.GatherIn || banner.Type == BannerType.StepUp) && banner.Steps.Length == 0) throw new ArgumentException("Step banners must define steps.", "bannerData");
            for (var index = 0; index < banner.Steps.Length; index++) if (banner.Steps[index].BoostedFiveStar && banner.Steps[index].FeaturedFiveStar) throw new ArgumentException("A banner step can define only one guarantee.", "bannerData");
        }

        private void ValidatePoolAndGuarantees()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal); long total = 0;
            for (var index = 0; index < banner.Entries.Count; index++)
            {
                var entry = banner.Entries[index];
                if (!ids.Add(entry.Id)) throw new ArgumentException("Banner character IDs must be unique.", "bannerData");
                if (!catalog.ContainsKey(entry.Id)) throw new ArgumentException("Banner references an unknown catalog character: " + entry.Id, "bannerData");
                try { total = checked(total + entry.Weight); } catch (OverflowException) { throw new ArgumentException("Banner weight total overflows Int64.", "bannerData"); }
            }
            if (total > int.MaxValue) throw new ArgumentException("Banner weight total exceeds deterministic random range.", "bannerData");
            for (var index = 0; index < banner.Steps.Length; index++)
            {
                var step = banner.Steps[index]; if (!step.BoostedFiveStar && !step.FeaturedFiveStar) continue;
                var hasFive = false; var hasFeaturedFive = false;
                for (var entryIndex = 0; entryIndex < banner.Entries.Count; entryIndex++)
                {
                    var entry = banner.Entries[entryIndex]; if (catalog[entry.Id].Rarity != 5) continue;
                    hasFive = true; if (entry.IsFeatured) hasFeaturedFive = true;
                }
                if (!hasFive || !hasFeaturedFive) throw new ArgumentException("Guaranteed steps require featured five-star candidates.", "bannerData");
            }
        }

        private static void ValidateBannerType(BannerData data)
        {
            if (data.Type != BannerType.Standard && data.Type != BannerType.Featured && data.Type != BannerType.StepUp && data.Type != BannerType.GatherIn) throw new ArgumentException("Banner type is not defined.", "bannerData");
        }
        private static SkillData CloneSkill(SkillData source) { return new SkillData { Id = source.Id, DisplayName = source.DisplayName, ChargeElement = source.ChargeElement, ChargeRequired = source.ChargeRequired, Effects = source.Effects == null ? null : (SkillEffectData[])source.Effects.Clone() }; }

        private sealed class Candidate
        {
            internal Candidate(string id, int rarity, ElementType element, CharacterProgress prototype) { Id = id; Rarity = rarity; Element = element; this.prototype = prototype; }
            private readonly CharacterProgress prototype;
            internal string Id { get; private set; } internal int Rarity { get; private set; } internal ElementType Element { get; private set; }
            internal CharacterProgress CreateStagedProgress() { return prototype.CreateTransactionSnapshot(); }
        }
        private sealed class WeightedEntry
        {
            internal WeightedEntry(WeightedCharacterData source) { Id = source.CharacterId; Weight = source.Weight; IsFeatured = source.IsFeatured; }
            internal string Id; internal int Weight; internal bool IsFeatured;
        }
        private sealed class BannerSnapshot
        {
            internal BannerSnapshot(BannerData source)
            {
                Id = source.Id; Type = source.Type; SingleCost = source.SinglePullGemCost; TenCost = source.TenPullGemCost; PresentationKey = source.PresentationKey;
                RotationId = string.IsNullOrWhiteSpace(source.RotationId) ? source.Id : source.RotationId;
                Entries = new List<WeightedEntry>(); for (var index = 0; index < source.Characters.Length; index++) Entries.Add(new WeightedEntry(source.Characters[index]));
                Steps = new StepSnapshot[source.Steps.Length]; for (var index = 0; index < Steps.Length; index++) Steps[index] = new StepSnapshot(source.Steps[index]);
            }
            internal string Id; internal string RotationId; internal BannerType Type; internal int SingleCost; internal int TenCost; internal string PresentationKey; internal List<WeightedEntry> Entries; internal StepSnapshot[] Steps;
        }
        private sealed class StepSnapshot
        {
            internal StepSnapshot(BannerStepData source) { PullCount = source.PullCount; GemCost = source.GemCost; BoostedFiveStar = source.GuaranteedFiveStarFeaturedBoost; FeaturedFiveStar = source.GuaranteedFeaturedFiveStar; }
            internal int PullCount; internal int GemCost; internal bool BoostedFiveStar; internal bool FeaturedFiveStar;
        }
        private sealed class PlannedDuplicate
        {
            internal PlannedDuplicate(DuplicateTransactionPlan progressionPlan, CharacterProgress actualProgress) { ProgressionPlan = progressionPlan; ActualProgress = actualProgress; }
            internal DuplicateTransactionPlan ProgressionPlan; internal CharacterProgress ActualProgress;
        }
        private sealed class TransactionPlan
        {
            internal TransactionPlan(int gemCost, bool advancesStep) { GemCost = gemCost; AdvancesStep = advancesStep; }
            internal int GemCost; internal bool AdvancesStep; internal List<PlannedDuplicate> Duplicates = new List<PlannedDuplicate>(); internal List<CharacterProgress> NewProgress = new List<CharacterProgress>(); internal List<SummonResult> Results = new List<SummonResult>();
        }
    }
}
