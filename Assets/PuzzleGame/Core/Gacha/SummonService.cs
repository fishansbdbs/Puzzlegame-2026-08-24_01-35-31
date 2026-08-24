using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            Character = character;
            ActiveSkill = activeSkill;
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
                if (progress == null) throw new ArgumentException("Collection cannot contain null progress.", "initialProgress");
                if (progressById.ContainsKey(progress.CharacterId)) throw new ArgumentException("Collection character IDs must be unique.", "initialProgress");
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
        internal void Add(CharacterProgress progress)
        {
            if (progress == null) throw new ArgumentNullException("progress");
            progressById.Add(progress.CharacterId, progress);
        }
    }

    public sealed class SummonQuote
    {
        internal SummonQuote(bool isAvailable, int pullCount, int gemCost, int stepIndex, string failureCode)
        { IsAvailable = isAvailable; PullCount = pullCount; GemCost = gemCost; StepIndex = stepIndex; FailureCode = failureCode; }
        public bool IsAvailable { get; private set; }
        public int PullCount { get; private set; }
        public int GemCost { get; private set; }
        public int StepIndex { get; private set; }
        public string FailureCode { get; private set; }
    }

    public sealed class DuplicateSummonDetail
    {
        internal DuplicateSummonDetail(DuplicateApplicationResult result)
        {
            PreviousAscension = result.PreviousAscension; NewAscension = result.NewAscension; UniversalResourceGranted = result.UniversalResourceGranted;
        }
        public int PreviousAscension { get; private set; }
        public int NewAscension { get; private set; }
        public int UniversalResourceGranted { get; private set; }
    }

    public sealed class SummonResult
    {
        internal SummonResult(string characterId, int rarity, ElementType element, SummonOwnership ownership, DuplicateSummonDetail duplicate)
        { CharacterId = characterId; Rarity = rarity; Element = element; Ownership = ownership; Duplicate = duplicate; }
        public string CharacterId { get; private set; }
        public int Rarity { get; private set; }
        public ElementType Element { get; private set; }
        public SummonOwnership Ownership { get; private set; }
        public DuplicateSummonDetail Duplicate { get; private set; }
    }

    public sealed class SummonBatch
    {
        internal SummonBatch(string bannerId, string presentationKey, int gemCost, IList<SummonResult> results)
        {
            BannerId = bannerId; PresentationKey = presentationKey; GemCost = gemCost;
            var copied = new SummonResult[results.Count]; results.CopyTo(copied, 0); Results = Array.AsReadOnly(copied);
        }
        public string BannerId { get; private set; }
        public string PresentationKey { get; private set; }
        public int GemCost { get; private set; }
        public IReadOnlyList<SummonResult> Results { get; private set; }
        public string PackThickness { get { return Results.Count == 1 ? "Single" : "Multi"; } }
        public bool HasFiveStar { get { for (var index = 0; index < Results.Count; index++) if (Results[index].Rarity == 5) return true; return false; } }
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
            var errors = ContractValidation.Validate(bannerData);
            if (errors.Count > 0) throw new ArgumentException(errors[0], "bannerData");
            banner = new BannerSnapshot(bannerData);
            catalog = BuildCatalog(characterCatalog);
            ValidateEntriesAndGuarantees();
            this.wallet = wallet; this.random = random; this.collection = collection ?? new SummonCollection();
            this.state = state ?? new BannerRuntimeState(banner.Id, banner.Steps.Length);
            this.state.AssertMatches(banner.Id);
            if (!UsesSteps && this.state.NextStepIndex != 0) throw new ArgumentException("Non-step banners cannot have advanced state.", "state");
        }

        public Wallet Wallet { get { return wallet; } }
        public SummonCollection Collection { get { return collection; } }
        public BannerRuntimeState State { get { return state; } }

        public SummonQuote Quote() { return QuoteInternal(null); }
        public SummonQuote Quote(int pullCount) { return QuoteInternal(pullCount); }
        public bool CanSummon() { var quote = Quote(); return quote.IsAvailable && wallet.CanAfford(WalletCurrencies.Gems, quote.GemCost); }
        public bool CanSummon(int pullCount) { var quote = Quote(pullCount); return quote.IsAvailable && wallet.CanAfford(WalletCurrencies.Gems, quote.GemCost); }
        public SummonBatch PurchaseAndRoll() { return PurchaseAndRollInternal(null); }
        public SummonBatch PurchaseAndRoll(int pullCount) { return PurchaseAndRollInternal(pullCount); }

        private bool UsesSteps { get { return banner.Type == BannerType.GatherIn || banner.Type == BannerType.StepUp; } }

        private SummonQuote QuoteInternal(int? requestedPullCount)
        {
            if (UsesSteps)
            {
                if (state.IsComplete) return new SummonQuote(false, 0, 0, state.NextStepIndex, "RotationComplete");
                var step = banner.Steps[state.NextStepIndex];
                if (requestedPullCount.HasValue && requestedPullCount.Value != step.PullCount) throw new ArgumentOutOfRangeException("pullCount", "Pull count must match the current banner step.");
                return new SummonQuote(true, step.PullCount, step.GemCost, state.NextStepIndex, null);
            }
            if (!requestedPullCount.HasValue) throw new ArgumentException("Pull count is required for this banner.", "pullCount");
            var count = requestedPullCount.Value;
            if (count != 1 && count != 10) throw new ArgumentOutOfRangeException("pullCount", "Only one and ten pulls are supported.");
            var cost = banner.Type == BannerType.Standard ? (count == 1 ? StandardSinglePullGemCost : StandardTenPullGemCost) : (count == 1 ? banner.SinglePullGemCost : banner.TenPullGemCost);
            return new SummonQuote(true, count, cost, -1, null);
        }

        private SummonBatch PurchaseAndRollInternal(int? requestedPullCount)
        {
            var quote = QuoteInternal(requestedPullCount);
            if (!quote.IsAvailable) throw new InvalidOperationException(quote.FailureCode);
            if (!wallet.CanAfford(WalletCurrencies.Gems, quote.GemCost)) throw new InvalidOperationException("InsufficientGems");

            var picks = new Candidate[quote.PullCount];
            for (var index = 0; index < picks.Length; index++) picks[index] = Roll(banner.Entries);
            if (UsesSteps) ApplyGuarantee(banner.Steps[state.NextStepIndex], picks);
            var ownership = PlanOwnershipAndPreflight(picks);

            if (!wallet.TrySpend(WalletCurrencies.Gems, quote.GemCost)) throw new InvalidOperationException("Wallet changed during summon transaction.");
            var results = new List<SummonResult>(picks.Length);
            for (var index = 0; index < picks.Length; index++)
            {
                var candidate = picks[index];
                if (ownership[index] == SummonOwnership.New)
                {
                    collection.Add(new CharacterProgress(ProgressionDataSnapshot.Clone(candidate.Character), candidate.ActiveSkill));
                    results.Add(new SummonResult(candidate.Id, candidate.Rarity, candidate.Element, SummonOwnership.New, null));
                }
                else
                {
                    var applied = ProgressionService.ApplyDuplicate(collection.GetProgress(candidate.Id), wallet);
                    results.Add(new SummonResult(candidate.Id, candidate.Rarity, candidate.Element, SummonOwnership.Duplicate, new DuplicateSummonDetail(applied)));
                }
            }
            if (UsesSteps) state.Advance();
            return new SummonBatch(banner.Id, banner.PresentationKey, quote.GemCost, results);
        }

        private Candidate Roll(IReadOnlyList<WeightedEntry> entries)
        {
            var weights = new long[entries.Count];
            for (var index = 0; index < entries.Count; index++) weights[index] = entries[index].Weight;
            return catalog[entries[GachaRandom.SelectIndex(weights, random)].Id];
        }

        private void ApplyGuarantee(StepSnapshot step, Candidate[] picks)
        {
            if (!step.BoostedFiveStar && !step.FeaturedFiveStar) return;
            var candidates = new List<Candidate>();
            for (var index = 0; index < banner.Entries.Count; index++)
            {
                var entry = banner.Entries[index];
                var candidate = catalog[entry.Id];
                if (candidate.Rarity == 5 && (!step.FeaturedFiveStar || entry.IsFeatured)) candidates.Add(candidate);
            }
            var weights = new long[candidates.Count];
            for (var index = 0; index < candidates.Count; index++)
            {
                var entry = FindEntry(candidates[index].Id);
                weights[index] = step.BoostedFiveStar && entry.IsFeatured ? checked((long)entry.Weight * FeaturedGuaranteeWeightMultiplier) : entry.Weight;
            }
            picks[picks.Length - 1] = candidates[GachaRandom.SelectIndex(weights, random)];
        }

        private SummonOwnership[] PlanOwnershipAndPreflight(Candidate[] picks)
        {
            var result = new SummonOwnership[picks.Length];
            var ascensionById = new Dictionary<string, int>(StringComparer.Ordinal);
            long overflow = 0;
            for (var index = 0; index < picks.Length; index++)
            {
                var id = picks[index].Id;
                int ascension;
                if (!ascensionById.TryGetValue(id, out ascension))
                {
                    if (collection.Contains(id)) { result[index] = SummonOwnership.Duplicate; ascension = collection.GetProgress(id).Ascension; }
                    else { result[index] = SummonOwnership.New; ascensionById.Add(id, 0); continue; }
                }
                else result[index] = SummonOwnership.Duplicate;
                if (ascension >= ProgressionService.MaximumAscension) overflow = checked(overflow + picks[index].Character.Ascension.OverflowUniversalResourceAmount);
                else ascension++;
                ascensionById[id] = ascension;
            }
            if (overflow > 0 && (long)wallet.GetBalance(WalletCurrencies.UniversalDuplicateResource) + overflow > int.MaxValue)
                throw new OverflowException("Duplicate conversion would overflow the universal resource balance.");
            return result;
        }

        private Dictionary<string, Candidate> BuildCatalog(IEnumerable<SummonCharacterDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException("characterCatalog");
            var result = new Dictionary<string, Candidate>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null || definition.Character == null || definition.ActiveSkill == null) throw new ArgumentException("Catalog definitions must be complete.", "characterCatalog");
                var errors = ContractValidation.Validate(definition.Character);
                if (errors.Count > 0) throw new ArgumentException(errors[0], "characterCatalog");
                var character = ProgressionDataSnapshot.Clone(definition.Character);
                if (!string.Equals(character.ActiveSkillId, definition.ActiveSkill.Id, StringComparison.Ordinal)) throw new ArgumentException("Catalog skill must match its character.", "characterCatalog");
                if (result.ContainsKey(character.Id)) throw new ArgumentException("Catalog character IDs must be unique.", "characterCatalog");
                result.Add(character.Id, new Candidate(character, CloneSkill(definition.ActiveSkill)));
            }
            return result;
        }

        private void ValidateEntriesAndGuarantees()
        {
            for (var index = 0; index < banner.Entries.Count; index++)
            {
                var entry = banner.Entries[index];
                if (!catalog.ContainsKey(entry.Id)) throw new ArgumentException("Banner references an unknown catalog character: " + entry.Id, "bannerData");
            }
            for (var index = 0; index < banner.Steps.Length; index++)
            {
                var step = banner.Steps[index];
                if (!step.BoostedFiveStar && !step.FeaturedFiveStar) continue;
                var hasFive = false; var hasFeaturedFive = false;
                for (var entryIndex = 0; entryIndex < banner.Entries.Count; entryIndex++)
                {
                    var entry = banner.Entries[entryIndex];
                    if (catalog[entry.Id].Rarity != 5) continue;
                    hasFive = true; if (entry.IsFeatured) hasFeaturedFive = true;
                }
                if (!hasFive || !hasFeaturedFive) throw new ArgumentException("Guaranteed steps require featured five-star catalog candidates.", "bannerData");
            }
        }

        private static SkillData CloneSkill(SkillData source)
        {
            return new SkillData { Id = source.Id, DisplayName = source.DisplayName, ChargeElement = source.ChargeElement, ChargeRequired = source.ChargeRequired, Effects = source.Effects == null ? null : (SkillEffectData[])source.Effects.Clone() };
        }

        private WeightedEntry FindEntry(string id)
        {
            for (var index = 0; index < banner.Entries.Count; index++) if (string.Equals(banner.Entries[index].Id, id, StringComparison.Ordinal)) return banner.Entries[index];
            throw new InvalidOperationException("Guaranteed candidate was not in the banner.");
        }

        private sealed class Candidate
        {
            internal Candidate(CharacterData character, SkillData activeSkill) { Character = character; ActiveSkill = activeSkill; Id = character.Id; Rarity = character.BaseRarity; Element = character.Element; }
            internal string Id { get; private set; }
            internal int Rarity { get; private set; } internal ElementType Element { get; private set; }
            internal CharacterData Character { get; private set; } internal SkillData ActiveSkill { get; private set; }
        }

        private sealed class BannerSnapshot
        {
            internal BannerSnapshot(BannerData source)
            {
                Id = source.Id; Type = source.Type; SinglePullGemCost = source.SinglePullGemCost; TenPullGemCost = source.TenPullGemCost; PresentationKey = source.PresentationKey;
                var entries = new List<WeightedEntry>();
                for (var index = 0; index < source.Characters.Length; index++) entries.Add(new WeightedEntry(source.Characters[index]));
                Entries = entries;
                Steps = new StepSnapshot[source.Steps.Length]; for (var index = 0; index < Steps.Length; index++) Steps[index] = new StepSnapshot(source.Steps[index]);
            }
            internal string Id; internal BannerType Type; internal int SinglePullGemCost; internal int TenPullGemCost; internal string PresentationKey;
            internal List<WeightedEntry> Entries; internal StepSnapshot[] Steps;
        }

        private sealed class WeightedEntry
        {
            internal WeightedEntry(WeightedCharacterData source) { Id = source.CharacterId; Weight = source.Weight; IsFeatured = source.IsFeatured; }
            internal string Id; internal int Weight; internal bool IsFeatured;
        }

        private sealed class StepSnapshot
        {
            internal StepSnapshot(BannerStepData source) { PullCount = source.PullCount; GemCost = source.GemCost; BoostedFiveStar = source.GuaranteedFiveStarFeaturedBoost; FeaturedFiveStar = source.GuaranteedFeaturedFiveStar; }
            internal int PullCount; internal int GemCost; internal bool BoostedFiveStar; internal bool FeaturedFiveStar;
        }
    }
}
