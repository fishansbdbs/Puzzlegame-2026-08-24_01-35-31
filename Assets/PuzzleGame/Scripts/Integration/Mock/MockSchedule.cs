using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PuzzleGame.Presentation.Content;

namespace PuzzleGame.Presentation.Mock
{
    /// <summary>
    /// DEMO ONLY. Emulates Codex's calendar/rotation scheduler by reading
    /// the schedule content files. Presentation reads active content through
    /// IScheduleSource and never does its own date logic; the date math here
    /// stands in for the core scheduler until it is merged.
    /// </summary>
    public class MockSchedule : IScheduleSource
    {
        readonly ContentDb _db;

        public MockSchedule(ContentDb db)
        {
            _db = db;
        }

        public DateTime NowUtc => DateTime.UtcNow;

        public IReadOnlyList<ScheduledContentVm> GetActiveContent()
        {
            var now = NowUtc;
            return _db.Schedule
                .Select(e => ToVm(e, now))
                .Where(v => v != null && IsActive(v, now))
                .ToList();
        }

        public IReadOnlyList<ScheduledContentVm> GetUpcomingContent(int maxCount)
        {
            var now = NowUtc;
            return _db.Schedule
                .Select(e => ToVm(e, now))
                .Where(v => v != null && !v.Permanent && v.StartUtc > now)
                .OrderBy(v => v.StartUtc)
                .Take(maxCount)
                .ToList();
        }

        static bool IsActive(ScheduledContentVm vm, DateTime now)
        {
            if (vm.Permanent) return true;
            return vm.StartUtc <= now && now < vm.EndUtc;
        }

        ScheduledContentVm ToVm(ScheduleEntryDto dto, DateTime now)
        {
            var vm = new ScheduledContentVm
            {
                Id = dto.id,
                Kind = ParseKind(dto.kind),
                DisplayName = dto.name,
                Description = dto.desc,
                Permanent = dto.permanent,
                TargetId = dto.targetId
            };
            if (dto.permanent)
            {
                vm.StartUtc = DateTime.MinValue;
                vm.EndUtc = DateTime.MaxValue;
                return vm;
            }
            switch ((dto.recurrence ?? "none").ToLowerInvariant())
            {
                case "daily":
                    vm.StartUtc = now.Date;
                    vm.EndUtc = now.Date.AddDays(1);
                    return vm;
                case "weekly":
                    int diff = ((int)now.DayOfWeek - dto.weekday + 7) % 7;
                    var windowStart = now.Date.AddDays(-diff);
                    vm.StartUtc = windowStart;
                    vm.EndUtc = windowStart.AddDays(1);
                    if (!(vm.StartUtc <= now && now < vm.EndUtc))
                    {
                        // Not today: surface as upcoming next occurrence.
                        vm.StartUtc = windowStart.AddDays(7);
                        vm.EndUtc = vm.StartUtc.AddDays(1);
                    }
                    return vm;
                default:
                    if (!TryParseUtc(dto.startUtc, out var start) || !TryParseUtc(dto.endUtc, out var end))
                    {
                        return null;
                    }
                    vm.StartUtc = start;
                    vm.EndUtc = end;
                    return vm;
            }
        }

        static bool TryParseUtc(string value, out DateTime result)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out result);
        }

        static ScheduledContentKind ParseKind(string kind)
        {
            switch ((kind ?? "").ToLowerInvariant())
            {
                case "eventchapter": return ScheduledContentKind.EventChapter;
                case "materialdungeon": return ScheduledContentKind.MaterialDungeon;
                case "awakeningstage": return ScheduledContentKind.AwakeningStage;
                case "challengetower": return ScheduledContentKind.ChallengeTower;
                case "bossrush": return ScheduledContentKind.BossRush;
                default: return ScheduledContentKind.Banner;
            }
        }
    }
}
