using System;
using System.Collections.Generic;
using System.Linq;
using PuzzleGame.Presentation.Content;

namespace PuzzleGame.Presentation.Mock
{
    /// <summary>
    /// DEMO ONLY. Emulates Codex's gacha result generation so the pack-rip
    /// presentation can be exercised. The real game must take results from
    /// core gacha probability/guarantee logic — presentation never rolls.
    /// This mock's RNG exists solely because that system is not merged yet.
    /// </summary>
    public class MockSummons : ISummonSource
    {
        readonly ContentDb _db;
        readonly MockEconomy _economy;
        readonly MockRoster _roster;
        readonly MockSchedule _schedule;
        readonly System.Random _rng = new System.Random();
        readonly Dictionary<string, int> _stepProgress = new Dictionary<string, int>();

        public MockSummons(ContentDb db, MockEconomy economy, MockRoster roster, MockSchedule schedule)
        {
            _db = db;
            _economy = economy;
            _roster = roster;
            _schedule = schedule;
        }

        public IReadOnlyList<BannerVm> GetBanners()
        {
            var active = _schedule.GetActiveContent()
                .Where(c => c.Kind == ScheduledContentKind.Banner)
                .ToList();
            var result = new List<BannerVm>();
            foreach (var entry in active)
            {
                if (_db.Banners.TryGetValue(entry.TargetId, out var dto))
                {
                    result.Add(ToVm(dto, entry));
                }
            }
            // Standard banner is permanent and must always exist.
            if (result.All(b => b.Kind != BannerKind.Standard))
            {
                var standard = _db.Banners.Values.FirstOrDefault(b => b.kind == "standard");
                if (standard != null) result.Add(ToVm(standard, null));
            }
            return result
                .OrderBy(b => b.Kind == BannerKind.Standard ? 1 : 0)
                .ThenBy(b => b.DisplayName)
                .ToList();
        }

        public BannerVm GetBanner(string bannerId)
        {
            return GetBanners().FirstOrDefault(b => b.Id == bannerId)
                   ?? (_db.Banners.TryGetValue(bannerId ?? "", out var dto) ? ToVm(dto, null) : null);
        }

        BannerVm ToVm(BannerDto dto, ScheduledContentVm scheduleEntry)
        {
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
                EndsAtUtc = scheduleEntry != null && !scheduleEntry.Permanent ? scheduleEntry.EndUtc : (DateTime?)null,
                RatesText = FormatRates(dto)
            };
            foreach (var id in dto.featured)
            {
                var c = _db.GetCharacter(id);
                if (c != null) vm.FeaturedUnits.Add(c);
            }
            int progress = _stepProgress.TryGetValue(dto.id, out var p) ? p : 0;
            for (int i = 0; i < dto.steps.Count; i++)
            {
                vm.Steps.Add(new BannerStepVm
                {
                    Index = i,
                    GemCost = dto.steps[i].gemCost,
                    PullCount = dto.steps[i].pullCount,
                    Consumed = i < progress,
                    GuaranteeText = dto.steps[i].guarantee
                });
            }
            vm.CurrentStep = Math.Min(progress, Math.Max(0, dto.steps.Count - 1));
            return vm;
        }

        static string FormatRates(BannerDto dto)
        {
            var r = dto.rates;
            var lines = new List<string>
            {
                "5★  " + r.fiveStar.ToString("0.##") + "%",
                "4★  " + r.fourStar.ToString("0.##") + "%",
                "3★  " + r.threeStar.ToString("0.##") + "%",
                "2★  " + r.twoStar.ToString("0.##") + "%",
                "1★  " + r.oneStar.ToString("0.##") + "%"
            };
            if (dto.featured.Count > 0)
            {
                lines.Add("Featured units take " + r.featuredShareOfFiveStar.ToString("0.#") + "% of 5★ results.");
            }
            return string.Join("\n", lines);
        }

        public SummonSession RequestSummon(string bannerId, bool multi)
        {
            var vm = GetBanner(bannerId);
            if (vm == null || !_db.Banners.TryGetValue(bannerId, out var dto)) return null;
            int cost = multi ? vm.MultiCost : vm.SingleCost;
            int count = multi ? vm.MultiCount : 1;
            if (!_economy.TrySpend(CurrencyId.Gems, cost)) return null;

            var session = NewSession(vm, dto, count, cost);
            // Ten-pulls guarantee at least a 4★ in the standard/featured pattern.
            // Cards only carry ids until Finish() resolves them, so check DTOs.
            if (multi && session.Cards.All(c => RarityOf(c.CharacterId) < 4))
            {
                ReplaceLast(session, dto, 4);
            }
            Finish(session);
            return session;
        }

        public SummonSession RequestStepSummon(string bannerId)
        {
            var vm = GetBanner(bannerId);
            if (vm == null || vm.Steps.Count == 0 || !_db.Banners.TryGetValue(bannerId, out var dto)) return null;
            int progress = _stepProgress.TryGetValue(bannerId, out var p) ? p : 0;
            if (progress >= vm.Steps.Count) return null;   // steps are one-time per rotation
            var step = vm.Steps[progress];
            if (!_economy.TrySpend(CurrencyId.Gems, step.GemCost)) return null;

            var session = NewSession(vm, dto, step.PullCount, step.GemCost);
            session.StepIndex = progress;
            bool finalStep = progress == vm.Steps.Count - 1;
            if (finalStep)
            {
                // Rotation 1: guaranteed 5★ with featured odds up.
                // Rotation 2: guaranteed THE featured 5★.
                if (dto.rotationIndex >= 2 && dto.featured.Count > 0)
                {
                    ReplaceLastWith(session, dto.featured[0]);
                }
                else if (session.Cards.All(c => RarityOf(c.CharacterId) < 5))
                {
                    ReplaceLast(session, dto, 5);
                }
            }
            _stepProgress[bannerId] = progress + 1;
            Finish(session);
            return session;
        }

        SummonSession NewSession(BannerVm vm, BannerDto dto, int count, int cost)
        {
            var session = new SummonSession
            {
                BannerId = vm.Id,
                Banner = vm,
                GemCost = cost
            };
            for (int i = 0; i < count; i++)
            {
                var picked = RollCharacter(dto);
                session.Cards.Add(new SummonCardResult { CharacterId = picked.id });
            }
            return session;
        }

        int RarityOf(string characterId)
        {
            return _db.Characters.TryGetValue(characterId ?? "", out var c) ? c.rarity : 1;
        }

        void ReplaceLast(SummonSession session, BannerDto dto, int minRarity)
        {
            var pool = Pool(dto).Where(c => c.rarity >= minRarity).ToList();
            if (pool.Count == 0) return;
            var pick = pool[_rng.Next(pool.Count)];
            session.Cards[session.Cards.Count - 1] = new SummonCardResult { CharacterId = pick.id };
        }

        void ReplaceLastWith(SummonSession session, string characterId)
        {
            session.Cards[session.Cards.Count - 1] = new SummonCardResult { CharacterId = characterId };
        }

        void Finish(SummonSession session)
        {
            for (int i = 0; i < session.Cards.Count; i++)
            {
                var applied = _roster.ApplyPull(session.Cards[i].CharacterId);
                session.Cards[i] = applied;
                if (applied.Character == null)
                {
                    applied.Character = _db.GetCharacter(applied.CharacterId);
                }
            }
            session.MaxRarity = session.Cards.Max(c => c.Character != null ? c.Character.BaseRarity : 1);
        }

        List<CharacterDto> Pool(BannerDto dto)
        {
            IEnumerable<CharacterDto> pool = _db.Characters.Values;
            if (dto.poolTags.Count > 0)
            {
                pool = pool.Where(c => c.tags.Any(t => dto.poolTags.Contains(t)));
            }
            return pool.Where(c => c.rarity >= dto.minPoolRarity).ToList();
        }

        CharacterDto RollCharacter(BannerDto dto)
        {
            var r = dto.rates;
            float roll = (float)(_rng.NextDouble() * 100.0);
            int rarity;
            if (roll < r.fiveStar) rarity = 5;
            else if (roll < r.fiveStar + r.fourStar) rarity = 4;
            else if (roll < r.fiveStar + r.fourStar + r.threeStar) rarity = 3;
            else if (roll < r.fiveStar + r.fourStar + r.threeStar + r.twoStar) rarity = 2;
            else rarity = 1;

            // Featured share of 5★ results.
            if (rarity == 5 && dto.featured.Count > 0 && _rng.NextDouble() * 100.0 < r.featuredShareOfFiveStar)
            {
                var id = dto.featured[_rng.Next(dto.featured.Count)];
                if (_db.Characters.TryGetValue(id, out var featured)) return featured;
            }
            var pool = Pool(dto).Where(c => c.rarity == rarity).ToList();
            if (pool.Count == 0)
            {
                pool = Pool(dto);
                if (pool.Count == 0) pool = _db.Characters.Values.ToList();
            }
            return pool[_rng.Next(pool.Count)];
        }
    }
}
