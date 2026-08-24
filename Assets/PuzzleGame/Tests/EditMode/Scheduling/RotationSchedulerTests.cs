using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PuzzleGame.Core.Contracts;
using PuzzleGame.Core.Scheduling;

namespace PuzzleGame.Tests.EditMode.Scheduling
{
    public sealed class RotationSchedulerTests
    {
        [Test]
        public void Content_is_active_at_inclusive_start_and_inactive_at_exclusive_end()
        {
            var schedule = Absolute("event", ContentType.EventChapter, 9, 10);
            var clock = new FakeClock(schedule.Start);
            var scheduler = new RotationScheduler(clock);

            Assert.That(scheduler.GetActive(new[] { schedule }), Has.Count.EqualTo(1));

            clock.Now = schedule.End;
            Assert.That(scheduler.GetActive(new[] { schedule }), Is.Empty);
        }

        [Test]
        public void Scheduler_reads_the_injected_clock_for_each_query()
        {
            var schedule = Absolute("event", ContentType.EventChapter, 9, 10);
            var clock = new FakeClock(schedule.Start.AddMinutes(-1));
            var scheduler = new RotationScheduler(clock);

            Assert.That(scheduler.GetActive(new[] { schedule }), Is.Empty);
            clock.Now = schedule.Start;
            Assert.That(scheduler.GetActive(new[] { schedule }), Has.Count.EqualTo(1));
        }

        [Test]
        public void Disabled_content_is_not_returned()
        {
            var schedule = Absolute("disabled", ContentType.EventChapter, 9, 10);
            schedule.IsEnabled = false;

            Assert.That(new RotationScheduler(new FakeClock(schedule.Start)).GetActive(new[] { schedule }), Is.Empty);
        }

        [TestCase(ContentType.StandardBanner)]
        [TestCase(ContentType.FeaturedBanner)]
        [TestCase(ContentType.GatherInBanner)]
        [TestCase(ContentType.StepUpBanner)]
        [TestCase(ContentType.EventChapter)]
        [TestCase(ContentType.DailyDungeon)]
        [TestCase(ContentType.WeeklyDungeon)]
        [TestCase(ContentType.AwakeningMaterialStage)]
        [TestCase(ContentType.Tower)]
        [TestCase(ContentType.BossRush)]
        public void Every_defined_content_category_can_be_scheduled(ContentType contentType)
        {
            var schedule = Absolute("category-" + (int)contentType, contentType, 9, 10);

            var active = new RotationScheduler(new FakeClock(schedule.Start)).GetActive(new[] { schedule });

            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(active[0].ContentType, Is.EqualTo(contentType));
        }

        [Test]
        public void Active_content_is_sorted_by_descending_priority_then_ordinal_schedule_id()
        {
            var first = Absolute("zeta", ContentType.EventChapter, 9, 10);
            first.Priority = 2;
            var second = Absolute("alpha", ContentType.EventChapter, 9, 10);
            second.Priority = 2;
            var third = Absolute("first", ContentType.EventChapter, 9, 10);
            third.Priority = 3;

            var active = new RotationScheduler(new FakeClock(first.Start)).GetActive(new[] { first, second, third });

            CollectionAssert.AreEqual(new[] { "first", "alpha", "zeta" }, ScheduleIds(active));
        }

        [Test]
        public void Recurrence_uses_the_schedule_authored_offset_and_minute_boundaries()
        {
            var schedule = Recurring("offset", DayOfWeek.Monday, 9 * 60, 10 * 60, TimeSpan.FromHours(2));
            var clock = new FakeClock(new DateTimeOffset(2026, 8, 24, 7, 0, 0, TimeSpan.Zero));
            var scheduler = new RotationScheduler(clock);

            var active = scheduler.GetActive(new[] { schedule });
            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(active[0].ActiveStart, Is.EqualTo(new DateTimeOffset(2026, 8, 24, 9, 0, 0, TimeSpan.FromHours(2))));
            Assert.That(active[0].ActiveEnd, Is.EqualTo(new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.FromHours(2))));

            clock.Now = new DateTimeOffset(2026, 8, 24, 8, 0, 0, TimeSpan.Zero);
            Assert.That(scheduler.GetActive(new[] { schedule }), Is.Empty);
        }

        [Test]
        public void Recurring_window_remains_bounded_by_its_absolute_window()
        {
            var schedule = Recurring("outer", DayOfWeek.Monday, 0, 1439, TimeSpan.Zero);
            schedule.Start = new DateTimeOffset(2026, 8, 24, 9, 0, 0, TimeSpan.Zero);
            schedule.End = new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

            var active = new RotationScheduler(new FakeClock(schedule.Start)).GetActive(new[] { schedule });

            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(active[0].ActiveStart, Is.EqualTo(schedule.Start));
            Assert.That(active[0].ActiveEnd, Is.EqualTo(schedule.End));
        }

        [Test]
        public void Overnight_window_belongs_to_its_configured_start_day_across_a_sunday_to_monday_wrap()
        {
            var schedule = Recurring("overnight", DayOfWeek.Sunday, 23 * 60, 60, TimeSpan.Zero);
            var clock = new FakeClock(new DateTimeOffset(2026, 8, 24, 0, 30, 0, TimeSpan.Zero));
            var scheduler = new RotationScheduler(clock);

            var active = scheduler.GetActive(new[] { schedule });
            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(active[0].ActiveStart, Is.EqualTo(new DateTimeOffset(2026, 8, 23, 23, 0, 0, TimeSpan.Zero)));
            Assert.That(active[0].ActiveEnd, Is.EqualTo(new DateTimeOffset(2026, 8, 24, 1, 0, 0, TimeSpan.Zero)));

            clock.Now = new DateTimeOffset(2026, 8, 25, 0, 30, 0, TimeSpan.Zero);
            Assert.That(scheduler.GetActive(new[] { schedule }), Is.Empty);
        }

        [Test]
        public void Equal_recurrence_minutes_define_a_full_day_owned_by_the_start_day()
        {
            var schedule = Recurring("full-day", DayOfWeek.Monday, 12 * 60, 12 * 60, TimeSpan.Zero);
            var clock = new FakeClock(new DateTimeOffset(2026, 8, 24, 18, 0, 0, TimeSpan.Zero));
            var scheduler = new RotationScheduler(clock);

            Assert.That(scheduler.GetActive(new[] { schedule }), Has.Count.EqualTo(1));
            clock.Now = new DateTimeOffset(2026, 8, 25, 11, 59, 0, TimeSpan.Zero);
            Assert.That(scheduler.GetActive(new[] { schedule }), Has.Count.EqualTo(1));
            clock.Now = new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);
            Assert.That(scheduler.GetActive(new[] { schedule }), Is.Empty);
        }

        [Test]
        public void Invalid_or_duplicate_records_reject_the_whole_query_before_any_result_is_returned()
        {
            var valid = Absolute("same", ContentType.EventChapter, 9, 10);
            var duplicate = Absolute("same", ContentType.EventChapter, 9, 10);
            var invalid = Absolute("invalid", ContentType.EventChapter, 9, 10);
            invalid.ContentId = " ";

            var exception = Assert.Throws<ArgumentException>(() =>
                new RotationScheduler(new FakeClock(valid.Start)).GetActive(new RotationScheduleData[] { valid, null, duplicate, invalid }));

            StringAssert.Contains("Schedule at index 1", exception.Message);
            StringAssert.Contains("Duplicate schedule ID 'same'", exception.Message);
            StringAssert.Contains("Schedule content ID is required.", exception.Message);
        }

        [Test]
        public void Contract_validation_rejects_undefined_content_negative_priority_and_duplicate_weekdays()
        {
            var schedule = Absolute("invalid-contract", ContentType.EventChapter, 9, 10);
            schedule.ContentType = (ContentType)99;
            schedule.Priority = -1;
            schedule.RecurringWeekdays = new[] { (int)DayOfWeek.Monday, (int)DayOfWeek.Monday };

            var errors = ContractValidation.Validate(schedule);

            CollectionAssert.Contains(errors, "Schedule content type is invalid.");
            CollectionAssert.Contains(errors, "Schedule priority cannot be negative.");
            CollectionAssert.Contains(errors, "Schedule recurring weekdays cannot contain duplicates.");
        }

        [Test]
        public void Active_content_is_an_immutable_snapshot_not_an_alias_of_authored_data()
        {
            var schedule = Absolute("immutable", ContentType.EventChapter, 9, 10);
            var active = new RotationScheduler(new FakeClock(schedule.Start)).GetActive(new[] { schedule }).Single();

            schedule.Id = "changed";
            schedule.ContentId = "changed-content";
            schedule.Priority = 99;
            schedule.End = schedule.Start;

            Assert.That(active.ScheduleId, Is.EqualTo("immutable"));
            Assert.That(active.ContentId, Is.EqualTo("content-immutable"));
            Assert.That(active.Priority, Is.EqualTo(0));
            Assert.That(typeof(ActiveContent).GetProperty("ScheduleId").CanWrite, Is.False);
        }

        private static string[] ScheduleIds(IReadOnlyList<ActiveContent> active)
        {
            var ids = new string[active.Count];
            for (var index = 0; index < active.Count; index++)
            {
                ids[index] = active[index].ScheduleId;
            }

            return ids;
        }

        private static RotationScheduleData Absolute(string id, ContentType type, int startHour, int endHour)
        {
            return new RotationScheduleData
            {
                Id = id,
                ContentId = "content-" + id,
                ContentType = type,
                Start = new DateTimeOffset(2026, 8, 24, startHour, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 8, 24, endHour, 0, 0, TimeSpan.Zero),
                RecurringWeekdays = new int[0],
                StartMinuteOfDay = 0,
                EndMinuteOfDay = 0
            };
        }

        private static RotationScheduleData Recurring(string id, DayOfWeek weekday, int startMinute, int endMinute, TimeSpan offset)
        {
            return new RotationScheduleData
            {
                Id = id,
                ContentId = "content-" + id,
                ContentType = ContentType.EventChapter,
                Start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, offset),
                End = new DateTimeOffset(2027, 1, 1, 0, 0, 0, offset),
                RecurringWeekdays = new[] { (int)weekday },
                StartMinuteOfDay = startMinute,
                EndMinuteOfDay = endMinute
            };
        }

        private sealed class FakeClock : IClock
        {
            public FakeClock(DateTimeOffset now)
            {
                Now = now;
            }

            public DateTimeOffset Now { get; set; }
        }
    }
}
