using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.Services;

namespace Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarYesterdayQuickSelectShortcut : ReportsCalendarBaseQuickSelectShortcut
    {
         public ReportsCalendarYesterdayQuickSelectShortcut(ITimeService timeService)
            : base(timeService, Resources.Yesterday, ReportPeriod.Yesterday)
        {
        }

        public override ReportsDateRangeParameter GetDateRange()
        {
            var yesterday = TimeService.CurrentDateTime.Date.AddDays(-1);
            return ReportsDateRangeParameter
                .WithDates(yesterday, yesterday)
                .WithSource(ReportsSource.ShortcutYesterday);
        }
    }
}
