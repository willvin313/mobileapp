using System;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.Services;

namespace Toggl.Foundation.UI.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarThisMonthQuickSelectShortcut
        : ReportsCalendarBaseQuickSelectShortcut
    {
        public ReportsCalendarThisMonthQuickSelectShortcut(ITimeService timeService)
            : base(timeService, Resources.ThisMonth, ReportPeriod.ThisMonth)
        {
        }

        public override ReportsDateRangeParameter GetDateRange()
        {
            var now = TimeService.CurrentDateTime.Date;
            var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddMonths(1).AddDays(-1);
            return ReportsDateRangeParameter
                .WithDates(start, end)
                .WithSource(ReportsSource.ShortcutThisMonth);
        }
    }
}
