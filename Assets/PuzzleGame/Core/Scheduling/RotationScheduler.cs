using System;
using System.Collections.Generic;
using PuzzleGame.Core.Contracts;

namespace PuzzleGame.Core.Scheduling
{
    public sealed class ActiveContent
    {
        private readonly string _scheduleId;
        private readonly string _contentId;
        private readonly ContentType _contentType;
        private readonly int _priority;
        private readonly DateTimeOffset _activeStart;
        private readonly DateTimeOffset _activeEnd;

        public ActiveContent(
            string scheduleId,
            string contentId,
            ContentType contentType,
            int priority,
            DateTimeOffset activeStart,
            DateTimeOffset activeEnd)
        {
            _scheduleId = scheduleId;
            _contentId = contentId;
            _contentType = contentType;
            _priority = priority;
            _activeStart = activeStart;
            _activeEnd = activeEnd;
        }

        public string ScheduleId { get { return _scheduleId; } }
        public string ContentId { get { return _contentId; } }
        public ContentType ContentType { get { return _contentType; } }
        public int Priority { get { return _priority; } }
        public DateTimeOffset ActiveStart { get { return _activeStart; } }
        public DateTimeOffset ActiveEnd { get { return _activeEnd; } }
    }

    public sealed class RotationScheduler
    {
        private readonly IClock _clock;

        public RotationScheduler(IClock clock)
        {
            if (clock == null)
            {
                throw new ArgumentNullException("clock");
            }

            _clock = clock;
        }

        public IReadOnlyList<ActiveContent> GetActive(IEnumerable<RotationScheduleData> schedules)
        {
            if (schedules == null)
            {
                throw new ArgumentNullException("schedules");
            }

            var snapshots = SnapshotAndValidate(schedules);
            var now = _clock.Now;
            var active = new List<ActiveContent>();

            for (var index = 0; index < snapshots.Count; index++)
            {
                ActiveContent content;
                if (TryGetActive(snapshots[index], now, out content))
                {
                    active.Add(content);
                }
            }

            active.Sort(CompareActiveContent);
            return active.AsReadOnly();
        }

        private static List<ScheduleSnapshot> SnapshotAndValidate(IEnumerable<RotationScheduleData> schedules)
        {
            var snapshots = new List<ScheduleSnapshot>();
            var errors = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var index = 0;

            foreach (var schedule in schedules)
            {
                if (schedule == null)
                {
                    errors.Add("Schedule at index " + index + " cannot be null.");
                    index++;
                    continue;
                }

                var snapshot = new ScheduleSnapshot(schedule);
                var validation = ContractValidation.Validate(snapshot.ToContract());
                for (var errorIndex = 0; errorIndex < validation.Count; errorIndex++)
                {
                    errors.Add("Schedule at index " + index + ": " + validation[errorIndex]);
                }

                if (!string.IsNullOrWhiteSpace(snapshot.Id) && !ids.Add(snapshot.Id))
                {
                    errors.Add("Duplicate schedule ID '" + snapshot.Id + "'.");
                }

                snapshots.Add(snapshot);
                index++;
            }

            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join(Environment.NewLine, errors.ToArray()), "schedules");
            }

            return snapshots;
        }

        private static bool TryGetActive(ScheduleSnapshot schedule, DateTimeOffset now, out ActiveContent active)
        {
            active = null;
            if (!schedule.IsEnabled || now < schedule.Start || now >= schedule.End)
            {
                return false;
            }

            if (schedule.RecurringWeekdays.Length == 0)
            {
                active = CreateActiveContent(schedule, schedule.Start, schedule.End);
                return true;
            }

            var localNow = now.ToOffset(schedule.Start.Offset);
            for (var dayOffset = -1; dayOffset <= 0; dayOffset++)
            {
                var startDate = localNow.Date.AddDays(dayOffset);
                if (!ContainsWeekday(schedule.RecurringWeekdays, (int)startDate.DayOfWeek))
                {
                    continue;
                }

                var candidateStart = AtMinute(startDate, schedule.StartMinuteOfDay, schedule.Start.Offset);
                var candidateEnd = CandidateEnd(startDate, schedule.StartMinuteOfDay, schedule.EndMinuteOfDay, schedule.Start.Offset);
                if (now < candidateStart || now >= candidateEnd)
                {
                    continue;
                }

                var activeStart = candidateStart > schedule.Start ? candidateStart : schedule.Start;
                var activeEnd = candidateEnd < schedule.End ? candidateEnd : schedule.End;
                if (activeStart >= activeEnd)
                {
                    return false;
                }

                active = CreateActiveContent(schedule, activeStart, activeEnd);
                return true;
            }

            return false;
        }

        private static ActiveContent CreateActiveContent(ScheduleSnapshot schedule, DateTimeOffset activeStart, DateTimeOffset activeEnd)
        {
            return new ActiveContent(
                schedule.Id,
                schedule.ContentId,
                schedule.ContentType,
                schedule.Priority,
                activeStart,
                activeEnd);
        }

        private static bool ContainsWeekday(int[] weekdays, int weekday)
        {
            for (var index = 0; index < weekdays.Length; index++)
            {
                if (weekdays[index] == weekday)
                {
                    return true;
                }
            }

            return false;
        }

        private static DateTimeOffset AtMinute(DateTime date, int minuteOfDay, TimeSpan offset)
        {
            return new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, offset).AddMinutes(minuteOfDay);
        }

        private static DateTimeOffset CandidateEnd(DateTime startDate, int startMinute, int endMinute, TimeSpan offset)
        {
            if (startMinute >= endMinute)
            {
                return AtMinute(startDate.AddDays(1), endMinute, offset);
            }

            return AtMinute(startDate, endMinute, offset);
        }

        private static int CompareActiveContent(ActiveContent left, ActiveContent right)
        {
            var priority = right.Priority.CompareTo(left.Priority);
            return priority != 0 ? priority : StringComparer.Ordinal.Compare(left.ScheduleId, right.ScheduleId);
        }

        private sealed class ScheduleSnapshot
        {
            public ScheduleSnapshot(RotationScheduleData schedule)
            {
                Id = schedule.Id;
                ContentId = schedule.ContentId;
                ContentType = schedule.ContentType;
                IsEnabled = schedule.IsEnabled;
                Priority = schedule.Priority;
                Start = schedule.Start;
                End = schedule.End;
                StartMinuteOfDay = schedule.StartMinuteOfDay;
                EndMinuteOfDay = schedule.EndMinuteOfDay;
                RecurringWeekdays = schedule.RecurringWeekdays == null
                    ? null
                    : (int[])schedule.RecurringWeekdays.Clone();
            }

            public string Id { get; private set; }
            public string ContentId { get; private set; }
            public ContentType ContentType { get; private set; }
            public bool IsEnabled { get; private set; }
            public int Priority { get; private set; }
            public DateTimeOffset Start { get; private set; }
            public DateTimeOffset End { get; private set; }
            public int[] RecurringWeekdays { get; private set; }
            public int StartMinuteOfDay { get; private set; }
            public int EndMinuteOfDay { get; private set; }

            public RotationScheduleData ToContract()
            {
                return new RotationScheduleData
                {
                    Id = Id,
                    ContentId = ContentId,
                    ContentType = ContentType,
                    IsEnabled = IsEnabled,
                    Priority = Priority,
                    Start = Start,
                    End = End,
                    RecurringWeekdays = RecurringWeekdays == null ? null : (int[])RecurringWeekdays.Clone(),
                    StartMinuteOfDay = StartMinuteOfDay,
                    EndMinuteOfDay = EndMinuteOfDay
                };
            }
        }
    }
}
