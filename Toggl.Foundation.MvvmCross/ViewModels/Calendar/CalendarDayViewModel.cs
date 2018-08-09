using System;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarDayViewModel
    {
        public int Day { get; }

        public bool IsToday { get; }

        public bool IsInCurrentMonth { get; }

        public CalendarMonth CalendarMonth { get; }

        public DateTimeOffset DateTime { get; }
        
        public bool Selected { get; private set; }
        
        public bool IsEndOfSelectedPeriod { get; private set; }
        
        public bool IsStartOfSelectedPeriod { get; private set; }

        public CalendarDayViewModel(int day, CalendarMonth month, bool isInCurrentMonth, DateTimeOffset today)
        {
            Day = day;
            CalendarMonth = month;
            IsInCurrentMonth = isInCurrentMonth;
            DateTime = new DateTimeOffset(month.Year, month.Month, Day, 0, 0, 0, TimeSpan.Zero);
            IsToday = today.Date == DateTime.Date;

            Selected = false;
            IsEndOfSelectedPeriod = false;
            IsStartOfSelectedPeriod = false;
        }

        public void OnSelectedRangeChanged(ReportsDateRangeParameter selectedRange)
        {
            var isSelected = selectedRange != null
                && selectedRange.StartDate.Date <= DateTime.Date
                && selectedRange.EndDate.Date >= DateTime.Date;

            Selected = isSelected;
            IsEndOfSelectedPeriod = isSelected && selectedRange.EndDate.Date == DateTime.Date;
            IsStartOfSelectedPeriod = isSelected && selectedRange.StartDate.Date == DateTime.Date;
        }
    }
}
