using System;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModel
{
    public abstract class BaseCalendarQuickSelectShortcutTests : BaseMvvmCrossTests
    {
        protected BaseCalendarQuickSelectShortcutTests()
        {
            TimeService.CurrentDateTime.Returns(CurrentTime);
        }

        protected abstract QuickSelectShortcut CreateQuickSelectShortcut();
        protected abstract QuickSelectShortcut TryToCreateQuickSelectShortcutWithNull();

        protected abstract DateTimeOffset CurrentTime { get; }
        protected abstract DateTime ExpectedStart { get; }
        protected abstract DateTime ExpectedEnd { get; }

        [Fact, LogIfTooSlow]
        public void SetsSelectedToTrueWhenReceivesOnDateRangeChangedWithOwnDateRange()
        {
            var quickSelectShortCut = CreateQuickSelectShortcut();
            var dateRange = quickSelectShortCut.DateRange;

            quickSelectShortCut.OnDateRangeChanged(dateRange);
        }

        [Fact, LogIfTooSlow]
        public void TheGetDateRangeReturnsExpectedDateRange()
        {
            var dateRange = CreateQuickSelectShortcut().DateRange;

            dateRange.StartDate.Date.Should().Be(ExpectedStart);
            dateRange.EndDate.Date.Should().Be(ExpectedEnd);
        }

        [Fact, LogIfTooSlow]
        public void ConstructorThrowsWhenTryingToConstructWithNull()
        {
            Action tryingToConstructWithNull =
                () => TryToCreateQuickSelectShortcutWithNull();

            tryingToConstructWithNull.Should().Throw<ArgumentNullException>();
        }
    }

    public sealed class CalendarLastMonthQuickSelectShortcutTests : BaseCalendarQuickSelectShortcutTests
    {
        protected override DateTimeOffset CurrentTime => new DateTimeOffset(2016, 4, 4, 1, 2, 3, TimeSpan.Zero);
        protected override DateTime ExpectedStart => new DateTime(2016, 3, 1);
        protected override DateTime ExpectedEnd => new DateTime(2016, 3, 31);

        protected override QuickSelectShortcut CreateQuickSelectShortcut()
            => QuickSelectShortcut.ForLastMonth(TimeService);

        protected override QuickSelectShortcut TryToCreateQuickSelectShortcutWithNull()
            => QuickSelectShortcut.ForLastMonth(null);
    }

    public abstract class CalendarLastWeekQuickSelectShortcutTests : BaseCalendarQuickSelectShortcutTests
    {
        protected abstract BeginningOfWeek BeginningOfWeek { get; }

        protected sealed override QuickSelectShortcut CreateQuickSelectShortcut()
            => QuickSelectShortcut.ForLastWeek(TimeService, BeginningOfWeek);

        protected sealed override QuickSelectShortcut TryToCreateQuickSelectShortcutWithNull()
            => QuickSelectShortcut.ForLastWeek(null, BeginningOfWeek);

        public sealed class WhenBeginningOfWeekIsMonday : CalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Monday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 18);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 24);
        }

        public sealed class WhenBeginningOfWeekIsTuesday : CalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Tuesday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 19);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 25);
        }

        public sealed class WhenBeginningOfWeekIsWednesday : CalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Wednesday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 13);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 19);
        }

        public sealed class WhenBeginningOfWeekIsThursday : CalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Thursday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 14);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 20);
        }

        public sealed class WhenBeginningOfWeekIsFriday : CalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Friday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 15);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 21);
        }

        public sealed class WhenBeginningOfWeekIsSaturday : CalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Saturday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 16);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 22);
        }

        public sealed class WhenBeginningOfWeekIsSunday : CalendarLastWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Sunday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 17);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 23);
        }
    }

    public sealed class CalendarThisMonthQuickSelectShortcutTests : BaseCalendarQuickSelectShortcutTests
    {
        protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 11, 28, 0, 0, 0, TimeSpan.Zero);
        protected override DateTime ExpectedStart => new DateTime(2017, 11, 1);
        protected override DateTime ExpectedEnd => new DateTime(2017, 11, 30);

        protected override QuickSelectShortcut CreateQuickSelectShortcut()
            => QuickSelectShortcut.ForThisMonth(TimeService);

        protected override QuickSelectShortcut TryToCreateQuickSelectShortcutWithNull()
            => QuickSelectShortcut.ForThisMonth(null);
    }

    public abstract class CalendarThisWeekQuickSelectShortcutTests : BaseCalendarQuickSelectShortcutTests
    {
        protected abstract BeginningOfWeek BeginningOfWeek { get; }

        protected sealed override QuickSelectShortcut CreateQuickSelectShortcut()
            => QuickSelectShortcut.ForThisWeek(TimeService, BeginningOfWeek);

        protected sealed override QuickSelectShortcut TryToCreateQuickSelectShortcutWithNull()
            => QuickSelectShortcut.ForThisWeek(null, BeginningOfWeek);

        public sealed class WhenBeginningOfWeekIsMonday : CalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Monday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 25);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 31);
        }

        public sealed class WhenBeginningOfWeekIsTuesday : CalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Tuesday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 26);
            protected override DateTime ExpectedEnd => new DateTime(2018, 1, 1);
        }

        public sealed class WhenBeginningOfWeekIsWednesday : CalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Wednesday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 20);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 26);
        }

        public sealed class WhenBeginningOfWeekIsThursday : CalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Thursday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 21);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 27);
        }

        public sealed class WhenBeginningOfWeekIsFriday : CalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Friday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 22);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 28);
        }

        public sealed class WhenBeginningOfWeekIsSaturday : CalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Saturday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 23);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 29);
        }

        public sealed class WhenBeginningOfWeekIsSunday : CalendarThisWeekQuickSelectShortcutTests
        {
            protected override BeginningOfWeek BeginningOfWeek => BeginningOfWeek.Sunday;
            protected override DateTimeOffset CurrentTime => new DateTimeOffset(2017, 12, 26, 0, 0, 0, TimeSpan.Zero);
            protected override DateTime ExpectedStart => new DateTime(2017, 12, 24);
            protected override DateTime ExpectedEnd => new DateTime(2017, 12, 30);
        }
    }

    public sealed class CalendarThisYearQuickSelectShortcutTests : BaseCalendarQuickSelectShortcutTests
    {
        protected override DateTimeOffset CurrentTime => new DateTimeOffset(1984, 4, 5, 6, 7, 8, TimeSpan.Zero);
        protected override DateTime ExpectedStart => new DateTime(1984, 1, 1);
        protected override DateTime ExpectedEnd => new DateTime(1984, 12, 31);

        protected override QuickSelectShortcut CreateQuickSelectShortcut()
            => QuickSelectShortcut.ForThisYear(TimeService);

        protected override QuickSelectShortcut TryToCreateQuickSelectShortcutWithNull()
            => QuickSelectShortcut.ForThisYear(null);
    }

    public sealed class CalendarTodayQuickSelectShortcutTests : BaseCalendarQuickSelectShortcutTests
    {
        protected override DateTimeOffset CurrentTime => new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
        protected override DateTime ExpectedStart => new DateTime(2020, 1, 2);
        protected override DateTime ExpectedEnd => new DateTime(2020, 1, 2);

        protected override QuickSelectShortcut CreateQuickSelectShortcut()
            => QuickSelectShortcut.ForToday(TimeService);

        protected override QuickSelectShortcut TryToCreateQuickSelectShortcutWithNull()
            => QuickSelectShortcut.ForToday(null);
    }

    public sealed class CalendarYesterdayQuickSelectShortcutTests : BaseCalendarQuickSelectShortcutTests
    {
        protected override DateTimeOffset CurrentTime => new DateTimeOffset(1998, 4, 5, 6, 4, 2, TimeSpan.Zero);
        protected override DateTime ExpectedStart => new DateTime(1998, 4, 4);
        protected override DateTime ExpectedEnd => new DateTime(1998, 4, 4);

        protected override QuickSelectShortcut CreateQuickSelectShortcut()
            => QuickSelectShortcut.ForYesterday(TimeService);

        protected override QuickSelectShortcut TryToCreateQuickSelectShortcutWithNull()
            => QuickSelectShortcut.ForYesterday(null);
    }
}
