﻿using System;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.Services;

namespace Toggl.Foundation.UI.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarLastMonthQuickSelectShortcut
        : ReportsCalendarBaseQuickSelectShortcut
    {
        public ReportsCalendarLastMonthQuickSelectShortcut(ITimeService timeService)
            : base(timeService, Resources.LastMonth, ReportPeriod.LastMonth)
        {
        }

        public override ReportsDateRangeParameter GetDateRange()
        {
            var lastMonth = TimeService.CurrentDateTime.Date.AddMonths(-1);
            var start = new DateTimeOffset(lastMonth.Year, lastMonth.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddMonths(1).AddDays(-1);
            return ReportsDateRangeParameter
                .WithDates(start, end)
                .WithSource(ReportsSource.ShortcutLastMonth);
        }
    }
}
