using System;
using System.Reactive.Subjects;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class QuickSelectShortcut
    {
        private readonly ITimeService timeService;
        private readonly Func<DateTimeOffset, DateRangeParameter> getDateRange;
        private readonly ISubject<bool> isSelectedSubject = new Subject<bool>();

        public string Title { get; }

        public IObservable<bool> Selected { get; }

        public DateRangeParameter DateRange => getDateRange(timeService.CurrentDateTime);

        private QuickSelectShortcut(ITimeService timeService, string title, Func<DateTimeOffset, DateRangeParameter> getDateRange)
        {
            Ensure.Argument.IsNotNull(title, nameof(title));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(getDateRange, nameof(getDateRange));

            this.timeService = timeService;
            this.getDateRange = getDateRange;

            Title = title;
        }

        public void OnDateRangeChanged(DateRangeParameter dateRange)
        {
            var internalDateRange = DateRange;

            isSelectedSubject.OnNext(
                dateRange.StartDate.Date == internalDateRange.StartDate.Date
                && dateRange.EndDate.Date == internalDateRange.EndDate.Date
            );
        }

        internal static QuickSelectShortcut ForLastMonth(ITimeService timeService)
        {
            return new QuickSelectShortcut(timeService, Resources.LastMonth, now => 
            {
                var lastMonth = now.Date.AddMonths(-1);
                var start = new DateTimeOffset(lastMonth.Year, lastMonth.Month, 1, 0, 0, 0, TimeSpan.Zero);
                var end = start.AddMonths(1).AddDays(-1);
                return DateRangeParameter
                    .WithDates(start, end)
                    .WithSource(ReportsSource.ShortcutLastMonth);
            });
        }

        internal static QuickSelectShortcut ForLastWeek(ITimeService timeService, BeginningOfWeek beginningOfWeek)
        {
            return new QuickSelectShortcut(timeService, Resources.LastMonth, currentDate =>
            {
                var now = currentDate.Date;
                var difference = (now.DayOfWeek - beginningOfWeek.ToDayOfWeekEnum() + 7) % 7;
                var start = now.AddDays(-(difference + 7));
                var end = start.AddDays(6);
                return DateRangeParameter
                    .WithDates(start, end)
                    .WithSource(ReportsSource.ShortcutLastWeek);
            });
        }

        internal static QuickSelectShortcut ForThisMonth(ITimeService timeService)
        {
            return new QuickSelectShortcut(timeService, Resources.LastMonth, currentDate =>
            {
                var now = currentDate.Date;
                var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
                var end = start.AddMonths(1).AddDays(-1);
                return DateRangeParameter
                    .WithDates(start, end)
                    .WithSource(ReportsSource.ShortcutThisMonth);
            });
        }

        internal static QuickSelectShortcut ForThisWeek(ITimeService timeService, BeginningOfWeek beginningOfWeek)
        {
            return new QuickSelectShortcut(timeService, Resources.LastMonth, currentDate =>
            {
                var now = currentDate.Date;
                var difference = (now.DayOfWeek - beginningOfWeek.ToDayOfWeekEnum() + 7) % 7;
                var start = now.AddDays(-difference);
                var end = start.AddDays(6);
                return DateRangeParameter
                    .WithDates(start, end)
                    .WithSource(ReportsSource.ShortcutThisWeek);
            });
        }

        internal static QuickSelectShortcut ForThisYear(ITimeService timeService)
        {
            return new QuickSelectShortcut(timeService, Resources.LastMonth, now =>
            {
                var thisYear = now.Year;
                var start = new DateTimeOffset(thisYear, 1, 1, 0, 0, 0, TimeSpan.Zero);
                var end = start.AddYears(1).AddDays(-1);
                return DateRangeParameter
                    .WithDates(start, end)
                    .WithSource(ReportsSource.ShortcutThisYear);
            });
        }

        internal static QuickSelectShortcut ForToday(ITimeService timeService)
        {
            return new QuickSelectShortcut(timeService, Resources.LastMonth, now =>
            {
                var today = now.Date;
                return DateRangeParameter
                    .WithDates(today, today)
                    .WithSource(ReportsSource.ShortcutToday);
            });
        }

        internal static QuickSelectShortcut ForYesterday(ITimeService timeService)
        {
            return new QuickSelectShortcut(timeService, Resources.LastMonth, now =>
            {
                var yesterday = now.Date.AddDays(-1);
                return DateRangeParameter
                    .WithDates(yesterday, yesterday)
                    .WithSource(ReportsSource.ShortcutYesterday);
            });
        }
    }
}
