using System;
using System.Collections.Generic;
using System.Globalization;
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

        [Test]
        public void Recurrence_at_year_one_does_not_evaluate_an_unrepresentable_previous_day()
        {
            var date = new DateTime(1, 1, 1);
            var schedule = ConfiguredRecurring(
                "year-one",
                new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(1, 1, 1, 1, 0, 0, TimeSpan.Zero),
                date.DayOfWeek,
                0,
                60);

            var active = new RotationScheduler(new FakeClock(new DateTimeOffset(1, 1, 1, 0, 30, 0, TimeSpan.Zero)))
                .GetActive(new[] { schedule });

            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(active[0].ActiveStart, Is.EqualTo(schedule.Start));
            Assert.That(active[0].ActiveEnd, Is.EqualTo(schedule.End));
        }

        [Test]
        public void Equal_minute_recurrence_at_year_9999_clips_its_unrepresentable_end_to_the_outer_window()
        {
            var date = new DateTime(9999, 12, 31);
            var schedule = ConfiguredRecurring(
                "year-max",
                new DateTimeOffset(9999, 12, 31, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(9999, 12, 31, 23, 0, 0, TimeSpan.Zero),
                date.DayOfWeek,
                12 * 60,
                12 * 60);

            var active = new RotationScheduler(new FakeClock(new DateTimeOffset(9999, 12, 31, 18, 0, 0, TimeSpan.Zero)))
                .GetActive(new[] { schedule });

            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(active[0].ActiveStart, Is.EqualTo(schedule.Start));
            Assert.That(active[0].ActiveEnd, Is.EqualTo(schedule.End));
        }

        [Test]
        public void Year_one_positive_offset_recurrence_uses_a_representable_local_anchor()
        {
            var date = new DateTime(1, 1, 1);
            var offset = TimeSpan.FromHours(14);
            var schedule = ConfiguredRecurring(
                "year-one-positive-offset",
                new DateTimeOffset(1, 1, 1, 14, 0, 0, offset),
                new DateTimeOffset(1, 1, 1, 15, 0, 0, offset),
                date.DayOfWeek,
                14 * 60,
                15 * 60);

            var active = new RotationScheduler(new FakeClock(new DateTimeOffset(1, 1, 1, 14, 30, 0, offset)))
                .GetActive(new[] { schedule });

            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(active[0].ActiveStart, Is.EqualTo(schedule.Start));
            Assert.That(active[0].ActiveEnd, Is.EqualTo(schedule.End));
        }

        [Test]
        public void Year_9999_negative_offset_recurrence_clips_an_unrepresentable_local_end()
        {
            var date = new DateTime(9999, 12, 31);
            var offset = TimeSpan.FromHours(-14);
            var schedule = ConfiguredRecurring(
                "year-max-negative-offset",
                new DateTimeOffset(9999, 12, 31, 0, 0, 0, offset),
                new DateTimeOffset(9999, 12, 31, 9, 0, 0, offset),
                date.DayOfWeek,
                0,
                10 * 60);

            var active = new RotationScheduler(new FakeClock(new DateTimeOffset(9999, 12, 31, 8, 0, 0, offset)))
                .GetActive(new[] { schedule });

            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(active[0].ActiveStart, Is.EqualTo(schedule.Start));
            Assert.That(active[0].ActiveEnd, Is.EqualTo(schedule.End));
        }

        [TestCase(2026, 1, 31, 23 * 60, 60, 2026, 2, 1, 0, 30)]
        [TestCase(2026, 12, 31, 23 * 60, 60, 2027, 1, 1, 0, 30)]
        [TestCase(2028, 2, 29, 23 * 60, 60, 2028, 3, 1, 0, 30)]
        public void Overnight_recurrence_handles_month_year_and_leap_date_transitions(
            int startYear,
            int startMonth,
            int startDay,
            int startMinute,
            int endMinute,
            int nowYear,
            int nowMonth,
            int nowDay,
            int nowHour,
            int nowMinute)
        {
            var startDate = new DateTime(startYear, startMonth, startDay);
            var now = new DateTimeOffset(nowYear, nowMonth, nowDay, nowHour, nowMinute, 0, TimeSpan.Zero);
            var schedule = ConfiguredRecurring(
                "transition-" + startDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                new DateTimeOffset(startDate, TimeSpan.Zero),
                now.AddDays(1),
                startDate.DayOfWeek,
                startMinute,
                endMinute);

            var active = new RotationScheduler(new FakeClock(now)).GetActive(new[] { schedule });

            Assert.That(active, Has.Count.EqualTo(1));
            Assert.That(active[0].ActiveStart, Is.EqualTo(new DateTimeOffset(startDate, TimeSpan.Zero).AddMinutes(startMinute)));
            Assert.That(active[0].ActiveEnd, Is.EqualTo(new DateTimeOffset(startDate, TimeSpan.Zero).AddDays(1).AddMinutes(endMinute)));
        }

        [TestCase(9, 0, true)]
        [TestCase(9, 59, true)]
        [TestCase(10, 0, false)]
        public void Non_overnight_recurrence_has_an_inclusive_start_and_exclusive_end(int hour, int minute, bool expectedActive)
        {
            var day = new DateTime(2026, 8, 24);
            var schedule = ConfiguredRecurring(
                "non-overnight",
                new DateTimeOffset(day, TimeSpan.Zero),
                new DateTimeOffset(day.AddDays(1), TimeSpan.Zero),
                day.DayOfWeek,
                9 * 60,
                10 * 60);

            var active = new RotationScheduler(new FakeClock(new DateTimeOffset(2026, 8, 24, hour, minute, 0, TimeSpan.Zero)))
                .GetActive(new[] { schedule });

            Assert.That(active.Count == 1, Is.EqualTo(expectedActive));
        }

        [TestCase(2026, 8, 23, 23, 0, true)]
        [TestCase(2026, 8, 24, 0, 59, true)]
        [TestCase(2026, 8, 24, 1, 0, false)]
        public void Overnight_recurrence_has_an_inclusive_start_and_exclusive_end(
            int year,
            int month,
            int day,
            int hour,
            int minute,
            bool expectedActive)
        {
            var startDate = new DateTime(2026, 8, 23);
            var schedule = ConfiguredRecurring(
                "overnight-boundary",
                new DateTimeOffset(startDate, TimeSpan.Zero),
                new DateTimeOffset(startDate.AddDays(2), TimeSpan.Zero),
                startDate.DayOfWeek,
                23 * 60,
                60);

            var active = new RotationScheduler(new FakeClock(new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero)))
                .GetActive(new[] { schedule });

            Assert.That(active.Count == 1, Is.EqualTo(expectedActive));
        }

        [Test]
        public void Equivalent_instants_with_different_offsets_produce_the_same_authored_offset_interval()
        {
            var schedule = Recurring("equivalent-instant", DayOfWeek.Monday, 9 * 60, 10 * 60, TimeSpan.FromHours(2));
            var scheduler = new RotationScheduler(new FakeClock(new DateTimeOffset(2026, 8, 24, 7, 30, 0, TimeSpan.Zero)));
            var first = scheduler.GetActive(new[] { schedule }).Single();
            var second = new RotationScheduler(new FakeClock(new DateTimeOffset(2026, 8, 24, 2, 30, 0, TimeSpan.FromHours(-5))))
                .GetActive(new[] { schedule }).Single();

            Assert.That(second.ActiveStart, Is.EqualTo(first.ActiveStart));
            Assert.That(second.ActiveEnd, Is.EqualTo(first.ActiveEnd));
        }

        [Test]
        public void Recurring_window_clips_its_start_to_the_absolute_window_exactly()
        {
            var day = new DateTime(2026, 8, 24);
            var schedule = ConfiguredRecurring(
                "clip-start",
                new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 24, 17, 0, 0, TimeSpan.Zero),
                day.DayOfWeek,
                9 * 60,
                17 * 60);

            var active = new RotationScheduler(new FakeClock(schedule.Start)).GetActive(new[] { schedule }).Single();

            Assert.That(active.ActiveStart, Is.EqualTo(schedule.Start));
            Assert.That(active.ActiveEnd, Is.EqualTo(schedule.End));
        }

        [Test]
        public void Recurring_window_clips_its_end_to_the_absolute_window_exactly()
        {
            var day = new DateTime(2026, 8, 24);
            var schedule = ConfiguredRecurring(
                "clip-end",
                new DateTimeOffset(2026, 8, 24, 9, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 24, 16, 0, 0, TimeSpan.Zero),
                day.DayOfWeek,
                9 * 60,
                17 * 60);

            var active = new RotationScheduler(new FakeClock(schedule.Start)).GetActive(new[] { schedule }).Single();

            Assert.That(active.ActiveStart, Is.EqualTo(schedule.Start));
            Assert.That(active.ActiveEnd, Is.EqualTo(schedule.End));
        }

        [Test]
        public void Scheduler_reads_the_clock_once_after_all_validation_has_succeeded()
        {
            var schedule = Absolute("count-clock", ContentType.EventChapter, 9, 10);
            var clock = new CountingClock(schedule.Start);

            Assert.That(new RotationScheduler(clock).GetActive(new[] { schedule }), Has.Count.EqualTo(1));
            Assert.That(clock.ReadCount, Is.EqualTo(1));
        }

        [Test]
        public void Scheduler_does_not_read_the_clock_when_validation_fails()
        {
            var invalid = Absolute("invalid-before-clock", ContentType.EventChapter, 9, 10);
            invalid.ContentId = string.Empty;
            var clock = new ThrowingClock();

            Assert.That(() => new RotationScheduler(clock).GetActive(new[] { invalid }), Throws.TypeOf<ArgumentException>());
            Assert.That(clock.ReadCount, Is.EqualTo(0));
        }

        [Test]
        public void Scheduler_rejects_each_invalid_schedule_shape_and_disabled_invalid_records_atomically()
        {
            var clock = new ThrowingClock();

            var missingId = Absolute("missing-id", ContentType.EventChapter, 9, 10);
            missingId.Id = string.Empty;
            AssertInvalid(missingId, "ID is required.", clock);

            var missingContent = Absolute("missing-content", ContentType.EventChapter, 9, 10);
            missingContent.ContentId = string.Empty;
            AssertInvalid(missingContent, "Schedule content ID is required.", clock);

            var invalidType = Absolute("invalid-type", ContentType.EventChapter, 9, 10);
            invalidType.ContentType = (ContentType)99;
            AssertInvalid(invalidType, "Schedule content type is invalid.", clock);

            var invalidPriority = Absolute("invalid-priority", ContentType.EventChapter, 9, 10);
            invalidPriority.Priority = -1;
            AssertInvalid(invalidPriority, "Schedule priority cannot be negative.", clock);

            var invalidWindow = Absolute("invalid-window", ContentType.EventChapter, 9, 10);
            invalidWindow.End = invalidWindow.Start;
            AssertInvalid(invalidWindow, "Schedule end must be after schedule start.", clock);

            var invalidMinute = Absolute("invalid-minute", ContentType.EventChapter, 9, 10);
            invalidMinute.StartMinuteOfDay = 1440;
            AssertInvalid(invalidMinute, "Schedule recurrence minutes must be between 0 and 1439.", clock);

            var invalidWeekday = Absolute("invalid-weekday", ContentType.EventChapter, 9, 10);
            invalidWeekday.RecurringWeekdays = new[] { 7 };
            AssertInvalid(invalidWeekday, "Schedule weekday must be between 0 and 6.", clock);

            var duplicateWeekday = Absolute("duplicate-weekday", ContentType.EventChapter, 9, 10);
            duplicateWeekday.RecurringWeekdays = new[] { 1, 1 };
            AssertInvalid(duplicateWeekday, "Schedule recurring weekdays cannot contain duplicates.", clock);

            var nullWeekdays = Absolute("null-weekdays", ContentType.EventChapter, 9, 10);
            nullWeekdays.RecurringWeekdays = null;
            AssertInvalid(nullWeekdays, "Schedule recurring weekdays cannot be null.", clock);

            var disabledInvalid = Absolute("disabled-invalid", ContentType.EventChapter, 9, 10);
            disabledInvalid.IsEnabled = false;
            disabledInvalid.Priority = -1;
            AssertInvalid(disabledInvalid, "Schedule priority cannot be negative.", clock);

            Assert.That(clock.ReadCount, Is.EqualTo(0));
        }

        [Test]
        public void Scheduler_rejects_null_and_duplicate_records_without_reading_the_clock()
        {
            var first = Absolute("duplicate-id", ContentType.EventChapter, 9, 10);
            var duplicate = Absolute("duplicate-id", ContentType.EventChapter, 9, 10);
            var clock = new ThrowingClock();

            var exception = Assert.Throws<ArgumentException>(() =>
                new RotationScheduler(clock).GetActive(new RotationScheduleData[] { first, null, duplicate }));

            StringAssert.Contains("Schedule at index 1 cannot be null.", exception.Message);
            StringAssert.Contains("Duplicate schedule ID 'duplicate-id'.", exception.Message);
            Assert.That(clock.ReadCount, Is.EqualTo(0));
        }

        [Test]
        public void Returned_active_list_and_content_snapshot_are_immutable()
        {
            var schedule = Absolute("immutable-list", ContentType.EventChapter, 9, 10);
            var active = new RotationScheduler(new FakeClock(schedule.Start)).GetActive(new[] { schedule });
            var mutableView = active as IList<ActiveContent>;

            Assert.That(mutableView, Is.Not.Null);
            Assert.That(() => mutableView.Add(active[0]), Throws.TypeOf<NotSupportedException>());
            Assert.That(typeof(ActiveContent).GetProperty("ContentId").CanWrite, Is.False);
            Assert.That(active[0].ContentId, Is.EqualTo("content-immutable-list"));
        }

        [Test]
        public void Priority_ties_use_ordinal_order_even_under_turkish_culture_and_mixed_case_ids()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var schedules = new[]
                {
                    Absolute("i", ContentType.EventChapter, 9, 10),
                    Absolute("Z", ContentType.EventChapter, 9, 10),
                    Absolute("a", ContentType.EventChapter, 9, 10),
                    Absolute("I", ContentType.EventChapter, 9, 10)
                };

                var active = new RotationScheduler(new FakeClock(schedules[0].Start)).GetActive(schedules);

                CollectionAssert.AreEqual(new[] { "I", "Z", "a", "i" }, ScheduleIds(active));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
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

        private static RotationScheduleData ConfiguredRecurring(
            string id,
            DateTimeOffset start,
            DateTimeOffset end,
            DayOfWeek weekday,
            int startMinute,
            int endMinute)
        {
            return new RotationScheduleData
            {
                Id = id,
                ContentId = "content-" + id,
                ContentType = ContentType.EventChapter,
                Start = start,
                End = end,
                RecurringWeekdays = new[] { (int)weekday },
                StartMinuteOfDay = startMinute,
                EndMinuteOfDay = endMinute
            };
        }

        private static void AssertInvalid(RotationScheduleData schedule, string expectedError, ThrowingClock clock)
        {
            var exception = Assert.Throws<ArgumentException>(() => new RotationScheduler(clock).GetActive(new[] { schedule }));
            StringAssert.Contains(expectedError, exception.Message);
        }

        private sealed class FakeClock : IClock
        {
            public FakeClock(DateTimeOffset now)
            {
                Now = now;
            }

            public DateTimeOffset Now { get; set; }
        }

        private sealed class CountingClock : IClock
        {
            private readonly DateTimeOffset _now;

            public CountingClock(DateTimeOffset now)
            {
                _now = now;
            }

            public int ReadCount { get; private set; }

            public DateTimeOffset Now
            {
                get
                {
                    ReadCount++;
                    return _now;
                }
            }
        }

        private sealed class ThrowingClock : IClock
        {
            public int ReadCount { get; private set; }

            public DateTimeOffset Now
            {
                get
                {
                    ReadCount++;
                    throw new AssertionException("Clock must not be read when validation fails.");
                }
            }
        }
    }
}
