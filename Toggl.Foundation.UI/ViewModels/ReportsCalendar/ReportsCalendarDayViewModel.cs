﻿using System;
using Toggl.Foundation.UI.Interfaces;
using Toggl.Foundation.UI.Parameters;
using Toggl.Multivac;

namespace Toggl.Foundation.UI.ViewModels.ReportsCalendar
{
    [Preserve(AllMembers = true)]
    public sealed class ReportsCalendarDayViewModel : IDiffableByIdentifier<ReportsCalendarDayViewModel>
    {
        public int Day { get; }

        public CalendarMonth CalendarMonth { get; }

        public bool IsInCurrentMonth { get; }

        public bool IsToday { get; }

        public DateTimeOffset DateTimeOffset { get; }

        public ReportsCalendarDayViewModel(int day, CalendarMonth month, bool isInCurrentMonth, DateTimeOffset today)
        {
            Day = day;
            CalendarMonth = month;
            IsInCurrentMonth = isInCurrentMonth;
            DateTimeOffset = new DateTimeOffset(month.Year, month.Month, Day, 0, 0, 0, TimeSpan.Zero);
            IsToday = today.Date == DateTimeOffset.Date;
        }

        public bool IsSelected(ReportsDateRangeParameter selectedRange)
        {
            return selectedRange != null && selectedRange.StartDate.Date <= DateTimeOffset.Date && selectedRange.EndDate.Date >= DateTimeOffset.Date;
        }

        public bool IsStartOfSelectedPeriod(ReportsDateRangeParameter selectedRange)
        {
            return selectedRange != null && selectedRange.StartDate.Date == DateTimeOffset.Date;
        }

        public bool IsEndOfSelectedPeriod(ReportsDateRangeParameter selectedRange)
        {
            return selectedRange != null && selectedRange.EndDate.Date == DateTimeOffset.Date;
        }

        public bool Equals(ReportsCalendarDayViewModel other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(this, null)) return false;
            if (ReferenceEquals(other, null)) return false;
            return DateTimeOffset.Equals(other.DateTimeOffset)
                   && Day == other.Day
                   && CalendarMonth.Equals(other.CalendarMonth)
                   && IsInCurrentMonth == other.IsInCurrentMonth
                   && IsToday == other.IsToday;
        }

        public long Identifier => DateTimeOffset.Date.GetHashCode();
    }
}
