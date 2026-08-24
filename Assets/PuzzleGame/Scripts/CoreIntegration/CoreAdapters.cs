using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Economy;
using PuzzleGame.Core.Gacha;
using PuzzleGame.Core.Progression;
using PuzzleGame.Core.Scheduling;
using PuzzleGame.Presentation.Content;

namespace PuzzleGame.Presentation.CoreIntegration
{
    /// <summary>
    /// Thin adapters translating core state into the presentation seams
    /// (IEconomySource, IRosterSource, ISummonSource, IScheduleSource,
    /// IContentLibrary). Display strings still come from the authored
    /// ContentDb; every number and every transaction comes from core.
    /// </summary>
    public sealed class CoreEconomyAdapter : IEconomySource
    {
        readonly GameServices services;
        public event Action BalancesChanged;

        public CoreEconomyAdapter(GameServices services)
        {
            this.services = services;
            services.StateChanged += () => { var handler = BalancesChanged; if (handler != null) handler(); };
        }

        public long GetBalance(CurrencyId currency)
        {
            switch (currency)
            {
                case CurrencyId.Gold: return services.Wallet.GetBalance(WalletCurrencies.Gold);
                case CurrencyId.Gems: return services.Wallet.GetBalance(WalletCurrencies.Gems);
                case CurrencyId.Ticket: return services.Wallet.GetBalance(WalletCurrencies.Tickets);
                default: return services.Wallet.GetBalance(WalletCurrencies.UniversalDuplicateResource);
            }
        }
    }

    public sealed class CoreRosterAdapter : IRosterSource
    {
        readonly GameServices services;
        public event Action RosterChanged;

        public CoreRosterAdapter(GameServices services)
        {
            this.services = services;
            services.StateChanged += () => { var handler = RosterChanged; if (handler != null) handler(); };
        }

        public IReadOnlyList<CharacterView> GetOwned()
        {
            return services.OwnedCharacterIds
                .Select(BuildView)
                .Where(view => view != null)
                .OrderByDescending(view => view.CurrentRarity)
                .ThenBy(view => view.Element)
                .ThenBy(view => view.DisplayName, StringComparer.Ordinal)
                .ToList();
        }

        public CharacterView GetOwned(string characterId)
        {
            return services.IsOwned(characterId) ? BuildView(characterId) : null;
        }

        /// <summary>Authored display text from ContentDb, live numbers from core progress.</summary>
        public CharacterView BuildView(string characterId)
        {
            CharacterDto dto;
            if (!services.Db.Characters.TryGetValue(characterId, out dto)) return null;
            var progress = services.GetProgress(characterId);
            if (progress == null) return null;
            var view = services.Db.ToView(dto, progress.Level, progress.Ascension, progress.IsAwakened);
            var stats = progress.CurrentStats;
            view.Hp = stats.Hp;
            view.Atk = stats.Attack;
            view.Rec = stats.Recovery;
            if (view.ActiveSkill != null)
            {
                view.ActiveSkill.ChargeMax = progress.EffectiveActiveSkill.ChargeRequired;
            }
            return view;
        }

        public bool TryLevelUp(string characterId, int levels)
        {
            return services.TrySpendGoldForLevels(characterId, levels);
        }

        public bool TryAwaken(string characterId)
        {
            return services.TryAwaken(characterId).Succeeded;
        }

        public bool GetAwakenRequirements(string characterId, List<string> outLines)
        {
            outLines.Clear();
            var progress = services.GetProgress(characterId);
            if (progress == null) { outLines.Add("Not owned."); return false; }
            if (progress.IsAwakened) { outLines.Add("Already awakened."); return false; }
            var requirements = progress.AuthoredData.Awakening;
            if (requirements.RequiredLevel == 0)
            {
                outLines.Add("This character has no 6★ Awakened form yet.");
                return false;
            }
            bool levelOk = progress.Level >= requirements.RequiredLevel;
            outLines.Add((levelOk ? "✓" : "✗") + " Reach level " + requirements.RequiredLevel + " (now " + progress.Level + ")");
            long gold = services.Wallet.GetBalance(WalletCurrencies.Gold);
            bool goldOk = gold >= requirements.GoldCost;
            outLines.Add((goldOk ? "✓" : "✗") + " " + UI.UiKit.FormatNumber(requirements.GoldCost) + " Gold (have " + UI.UiKit.FormatNumber(gold) + ")");
            bool materialsOk = true;
            foreach (var material in requirements.Materials)
            {
                var balance = services.Materials.GetBalance(material.MaterialId);
                bool ok = balance >= material.Amount;
                materialsOk &= ok;
                ItemDto item;
                var name = services.Db.Items.TryGetValue(material.MaterialId, out item) ? item.name : material.MaterialId;
                outLines.Add((ok ? "✓" : "✗") + " " + material.Amount + "× " + name + " (have " + balance + ")");
            }
            outLines.Add("Duplicates are NOT required for Awakening.");
            return levelOk && goldOk && materialsOk;
        }
    }

    public sealed class CoreScheduleAdapter : IScheduleSource
    {
        readonly GameServices services;

        public CoreScheduleAdapter(GameServices services) { this.services = services; }

        public DateTime NowUtc { get { return services.Clock.Now.UtcDateTime; } }

        public IReadOnlyList<ScheduledContentVm> GetActiveContent()
        {
            return services.GetActiveContent().Select(ToVm).Where(vm => vm != null).ToList();
        }

        public IReadOnlyList<ScheduledContentVm> GetUpcomingContent(int maxCount)
        {
            var now = services.Clock.Now;
            return services.Content.Schedules
                .Where(schedule => schedule.RecurringWeekdays.Length == 0 && schedule.Start > now)
                .OrderBy(schedule => schedule.Start)
                .Take(maxCount)
                .Select(schedule => ToVm(schedule.Id, schedule.ContentId, schedule.ContentType, schedule.Start, schedule.End))
                .Where(vm => vm != null)
                .ToList();
        }

        ScheduledContentVm ToVm(ActiveContent content)
        {
            return ToVm(content.ScheduleId, content.ContentId, content.ContentType, content.ActiveStart, content.ActiveEnd);
        }

        ScheduledContentVm ToVm(string scheduleId, string contentId, ContentType type, DateTimeOffset start, DateTimeOffset end)
        {
            var vm = new ScheduledContentVm
            {
                Id = scheduleId,
                TargetId = contentId,
                Kind = ToKind(type),
                StartUtc = start.UtcDateTime,
                EndUtc = end.UtcDateTime,
                Permanent = end.Year >= 2099
            };
            BannerDto banner;
            EventDto eventDto;
            if (services.Db.Banners.TryGetValue(contentId, out banner))
            {
                vm.DisplayName = banner.name;
                vm.Description = banner.desc;
            }
            else if (services.Db.Events.TryGetValue(contentId, out eventDto))
            {
                vm.DisplayName = eventDto.name;
                vm.Description = eventDto.desc;
                vm.ThemeRef = eventDto.theme;
            }
            else
            {
                return null;
            }
            return vm;
        }

        static ScheduledContentKind ToKind(ContentType type)
        {
            switch (type)
            {
                case ContentType.StandardBanner:
                case ContentType.FeaturedBanner:
                case ContentType.GatherInBanner:
                case ContentType.StepUpBanner:
                    return ScheduledContentKind.Banner;
                case ContentType.EventChapter: return ScheduledContentKind.EventChapter;
                case ContentType.DailyDungeon:
                case ContentType.WeeklyDungeon:
                    return ScheduledContentKind.MaterialDungeon;
                case ContentType.AwakeningMaterialStage: return ScheduledContentKind.AwakeningStage;
                case ContentType.Tower: return ScheduledContentKind.ChallengeTower;
                default: return ScheduledContentKind.BossRush;
            }
        }
    }

    public sealed class CoreSummonAdapter : ISummonSource
    {
        readonly GameServices services;
        readonly CoreRosterAdapter roster;

        public CoreSummonAdapter(GameServices services, CoreRosterAdapter roster)
        {
            this.services = services;
            this.roster = roster;
        }

        public IReadOnlyList<BannerVm> GetBanners()
        {
            var active = services.GetActiveContent();
            var result = new List<BannerVm>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var content in active)
            {
                if (!IsBanner(content.ContentType)) continue;
                if (!seen.Add(content.ContentId)) continue;
                var vm = BuildVm(content.ContentId, content.ActiveEnd.Year >= 2099 ? (DateTime?)null : content.ActiveEnd.UtcDateTime);
                if (vm != null) result.Add(vm);
            }
            return result
                .OrderBy(vm => vm.Kind == BannerKind.Standard ? 1 : 0)
                .ThenBy(vm => vm.DisplayName, StringComparer.Ordinal)
                .ToList();
        }

        static bool IsBanner(ContentType type)
        {
            return type == ContentType.StandardBanner || type == ContentType.FeaturedBanner ||
                   type == ContentType.GatherInBanner || type == ContentType.StepUpBanner;
        }

        public BannerVm GetBanner(string bannerId)
        {
            var active = services.GetActiveContent().FirstOrDefault(content => content.ContentId == bannerId && IsBanner(content.ContentType));
            var endsAt = active != null && active.ActiveEnd.Year < 2099 ? active.ActiveEnd.UtcDateTime : (DateTime?)null;
            return BuildVm(bannerId, endsAt);
        }

        BannerVm BuildVm(string bannerId, DateTime? endsAtUtc)
        {
            BannerDto dto;
            if (!services.Db.Banners.TryGetValue(bannerId, out dto)) return null;
            var service = services.GetSummonService(bannerId);
            if (service == null) return null;
            var vm = new BannerVm
            {
                Id = dto.id,
                DisplayName = dto.name,
                Kind = ContentDb.ParseBannerKind(dto.kind),
                Description = dto.desc,
                SingleCost = dto.singleCost,
                MultiCost = dto.multiCost,
                PackArtRef = string.IsNullOrEmpty(dto.packArt) ? dto.id : dto.packArt,
                GuaranteeText = dto.guarantee,
                RotationIndex = dto.rotationIndex,
                Active = true,
                EndsAtUtc = endsAtUtc,
                RatesText = FormatRates(dto)
            };
            foreach (var id in dto.featured)
            {
                var featured = services.IsOwned(id) ? roster.BuildView(id) : services.Db.GetCharacter(id);
                if (featured != null) vm.FeaturedUnits.Add(featured);
            }
            var state = service.State;
            for (var index = 0; index < dto.steps.Count; index++)
            {
                vm.Steps.Add(new BannerStepVm
                {
                    Index = index,
                    GemCost = dto.steps[index].gemCost,
                    PullCount = dto.steps[index].pullCount,
                    Consumed = index < state.NextStepIndex,
                    GuaranteeText = dto.steps[index].guarantee
                });
            }
            vm.CurrentStep = Math.Min(state.NextStepIndex, Math.Max(0, dto.steps.Count - 1));
            return vm;
        }

        static string FormatRates(BannerDto dto)
        {
            var rates = dto.rates ?? new BannerRatesDto();
            var lines = new List<string>
            {
                "5★  " + rates.fiveStar.ToString("0.##") + "%",
                "4★  " + rates.fourStar.ToString("0.##") + "%",
                "3★  " + rates.threeStar.ToString("0.##") + "%",
                "2★  " + rates.twoStar.ToString("0.##") + "%",
                "1★  " + rates.oneStar.ToString("0.##") + "%"
            };
            if (dto.featured.Count > 0)
            {
                lines.Add("Featured units take " + rates.featuredShareOfFiveStar.ToString("0.#") + "% of 5★ results.");
            }
            return string.Join("\n", lines.ToArray());
        }

        public SummonSession RequestSummon(string bannerId, bool multi)
        {
            var service = services.GetSummonService(bannerId);
            if (service == null) return null;
            int pullCount = multi ? 10 : 1;
            if (!service.CanSummon(pullCount)) return null;
            var batch = service.PurchaseAndRoll(pullCount);
            return BuildSession(bannerId, batch);
        }

        public SummonSession RequestStepSummon(string bannerId)
        {
            var service = services.GetSummonService(bannerId);
            if (service == null || !service.CanSummon()) return null;
            var batch = service.PurchaseAndRoll();
            return BuildSession(bannerId, batch);
        }

        SummonSession BuildSession(string bannerId, SummonBatch batch)
        {
            services.RegisterSummonBatch(batch);
            var session = new SummonSession
            {
                BannerId = bannerId,
                Banner = GetBanner(bannerId),
                GemCost = batch.GemCost,
                CorePackFlow = PackSummonFlow.Start(batch)
            };
            foreach (var result in batch.Results)
            {
                var card = new SummonCardResult
                {
                    CharacterId = result.CharacterId,
                    Character = roster.BuildView(result.CharacterId) ?? services.Db.GetCharacter(result.CharacterId),
                    IsNew = result.Ownership == SummonOwnership.New
                };
                if (result.Duplicate != null)
                {
                    card.AscensionAfter = result.Duplicate.NewAscension;
                    card.AscensionGained = result.Duplicate.NewAscension > result.Duplicate.PreviousAscension;
                    if (result.Duplicate.UniversalResourceGranted > 0)
                    {
                        card.OverflowReward = "+" + result.Duplicate.UniversalResourceGranted + " Spark Essence";
                    }
                }
                session.Cards.Add(card);
            }
            session.MaxRarity = session.Cards.Count == 0 ? 1 : session.Cards.Max(card => card.Character != null ? card.Character.BaseRarity : 1);
            return session;
        }
    }

    /// <summary>
    /// IContentLibrary backed by the authored ContentDb for display, with
    /// live stage progress (stars/cleared/unlocked) from the core save.
    /// Stage N unlocks when stage N-1 of its chapter is cleared.
    /// </summary>
    public sealed class CoreContentAdapter : IContentLibrary
    {
        readonly GameServices services;

        public CoreContentAdapter(GameServices services) { this.services = services; }

        public CharacterView GetCharacter(string id) { return services.Db.GetCharacter(id); }
        public IReadOnlyList<CharacterView> GetAllCharacters() { return services.Db.GetAllCharacters(); }
        public int GetChapterCount() { return services.Db.GetChapterCount(); }
        public string GetChapterTitle(int chapterNumber) { return services.Db.GetChapterTitle(chapterNumber); }

        public IReadOnlyList<StageSummaryVm> GetChapterStages(int chapterNumber)
        {
            var summaries = services.Db.GetChapterStages(chapterNumber);
            bool previousCleared = IsChapterUnlocked(chapterNumber);
            foreach (var summary in summaries.OrderBy(s => s.StageNumber))
            {
                var progress = services.GetStageProgress(summary.Id);
                summary.Cleared = progress != null && progress.IsCleared;
                summary.StarsEarned = progress == null ? 0 : progress.Stars.Count(star => star);
                summary.Unlocked = previousCleared;
                previousCleared = summary.Cleared;
            }
            return summaries;
        }

        public bool IsChapterUnlocked(int chapterNumber)
        {
            if (chapterNumber <= 1) return true;
            ChapterDto previous;
            if (!services.Db.Chapters.TryGetValue(chapterNumber - 1, out previous)) return true;
            var finalStage = previous.stages.OrderBy(stage => stage.stageNumber).LastOrDefault();
            if (finalStage == null) return true;
            var progress = services.GetStageProgress(finalStage.id);
            return progress != null && progress.IsCleared;
        }
    }
}
