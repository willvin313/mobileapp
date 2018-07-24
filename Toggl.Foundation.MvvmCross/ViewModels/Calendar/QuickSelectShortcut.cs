using System;
using MvvmCross.UI;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class QuickSelectShortcut
    {
        private readonly ITimeService timeService;
        private readonly Func<DateTimeOffset, DateRangeParameter> getDateRange;

        public string Title { get; }

        public bool Selected { get; private set; }

        public MvxColor TitleColor { get; private set; }

        public MvxColor BackgroundColor { get; private set;  }

        public DateRangeParameter DateRange => getDateRange(timeService.CurrentDateTime);

        private QuickSelectShortcut(ITimeService timeService, string title, Func<DateTimeOffset, DateRangeParameter> getDateRange)
        {
            Ensure.Argument.IsNotNull(title, nameof(title));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(getDateRange, nameof(getDateRange));

            this.timeService = timeService;
            this.getDateRange = getDateRange;

            Title = title;
            TitleColor = calculateTitleColor(false);
            BackgroundColor = calculateBackgroundColor(false);
        }

        public void OnDateRangeChanged(DateRangeParameter dateRange)
        {
            var internalDateRange = DateRange;

            Selected =
                dateRange.StartDate.Date == internalDateRange.StartDate.Date
                && dateRange.EndDate.Date == internalDateRange.EndDate.Date;

            TitleColor = calculateTitleColor(Selected);
            BackgroundColor = calculateBackgroundColor(Selected);
        }

        private MvxColor calculateTitleColor(bool isSelected)
            => isSelected
            ? Color.Calendar.QuickSelect.SelectedTitle
            : Color.Calendar.QuickSelect.UnselectedTitle;

        private MvxColor calculateBackgroundColor(bool isSelected)
            => isSelected
            ? Color.Calendar.QuickSelect.SelectedBackground
            : Color.Calendar.QuickSelect.UnselectedBackground;

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
            return new QuickSelectShortcut(timeService, Resources.LastWeek, currentDate =>
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
            return new QuickSelectShortcut(timeService, Resources.ThisMonth, currentDate =>
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
            return new QuickSelectShortcut(timeService, Resources.ThisWeek, currentDate =>
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
            return new QuickSelectShortcut(timeService, Resources.ThisYear, now =>
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
            return new QuickSelectShortcut(timeService, Resources.Today, now =>
            {
                var today = now.Date;
                return DateRangeParameter
                    .WithDates(today, today)
                    .WithSource(ReportsSource.ShortcutToday);
            });
        }

        internal static QuickSelectShortcut ForYesterday(ITimeService timeService)
        {
            return new QuickSelectShortcut(timeService, Resources.Yesterday, now =>
            {
                var yesterday = now.Date.AddDays(-1);
                return DateRangeParameter
                    .WithDates(yesterday, yesterday)
                    .WithSource(ReportsSource.ShortcutYesterday);
            });
        }
    }
}
