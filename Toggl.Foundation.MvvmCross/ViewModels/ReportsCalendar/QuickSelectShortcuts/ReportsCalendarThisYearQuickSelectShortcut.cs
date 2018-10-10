using System;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.Services;

namespace Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarThisYearQuickSelectShortcut
        : ReportsCalendarBaseQuickSelectShortcut
    {
        public ReportsCalendarThisYearQuickSelectShortcut(ITimeService timeService)
            : base(timeService, Resources.ThisYear, ReportPeriod.ThisYear)
        {
        }

        public override ReportsDateRangeParameter GetDateRange()
        {
            var thisYear = TimeService.CurrentDateTime.Year;
            var start = new DateTimeOffset(thisYear, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddYears(1).AddDays(-1);
            return ReportsDateRangeParameter
                .WithDates(start, end)
                .WithSource(ReportsSource.ShortcutThisYear);
        }
    }
}
