using Toggl.Foundation.Analytics;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.Services;
using Toggl.Multivac;

namespace Toggl.Foundation.UI.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarLastWeekQuickSelectShortcut
        : ReportsCalendarBaseQuickSelectShortcut
    {
        private readonly BeginningOfWeek beginningOfWeek;

        public ReportsCalendarLastWeekQuickSelectShortcut(
            ITimeService timeService, BeginningOfWeek beginningOfWeek)
            : base(timeService, Resources.LastWeek, ReportPeriod.LastWeek)
        {
            this.beginningOfWeek = beginningOfWeek;
        }

        public override ReportsDateRangeParameter GetDateRange()
        {
            var now = TimeService.CurrentDateTime.Date;
            var difference = (now.DayOfWeek - beginningOfWeek.ToDayOfWeekEnum() + 7) % 7;
            var start = now.AddDays(-(difference + 7));
            var end = start.AddDays(6);
            return ReportsDateRangeParameter
                .WithDates(start, end)
                .WithSource(ReportsSource.ShortcutLastWeek);
        }
    }
}
