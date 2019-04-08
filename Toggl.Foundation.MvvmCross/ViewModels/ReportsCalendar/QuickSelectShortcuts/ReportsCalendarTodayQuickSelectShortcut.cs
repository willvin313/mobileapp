using Toggl.Foundation.Analytics;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.Services;

namespace Toggl.Foundation.UI.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarTodayQuickSelectShortcut : ReportsCalendarBaseQuickSelectShortcut
    {
        public ReportsCalendarTodayQuickSelectShortcut(ITimeService timeService)
            : base(timeService, Resources.Today, ReportPeriod.Today)
        {
        }

        public override ReportsDateRangeParameter GetDateRange()
        {
            var today = TimeService.CurrentDateTime.Date;
            return ReportsDateRangeParameter
                .WithDates(today, today)
                .WithSource(ReportsSource.ShortcutToday);
        }
    }
}
