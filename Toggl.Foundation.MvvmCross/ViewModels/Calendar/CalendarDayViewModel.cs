using System;
using MvvmCross.UI;
using Toggl.Foundation.MvvmCross.Helper;
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
        
        public MvxColor TextColor { get; private set; }

        public MvxColor BackgroundColor { get; private set; }
        
        public bool IsEndOfSelectedPeriod { get; private set; }
        
        public bool IsStartOfSelectedPeriod { get; private set; }

        public CalendarDayViewModel(int day, CalendarMonth month, bool isInCurrentMonth, DateTimeOffset today)
        {
            Day = day;
            CalendarMonth = month;
            IsInCurrentMonth = isInCurrentMonth;
            DateTime = new DateTimeOffset(month.Year, month.Month, Day, 0, 0, 0, TimeSpan.Zero);

            Selected = false;
            IsEndOfSelectedPeriod = false;
            IsStartOfSelectedPeriod = false;
            TextColor = calculateTextColor(Selected);
            BackgroundColor = calculateBackgroundColor(Selected);
        }

        public void OnSelectedRangeChanged(DateRangeParameter selectedRange)
        {
            var isSelected = selectedRange != null
                && selectedRange.StartDate.Date <= DateTime.Date
                && selectedRange.EndDate.Date >= DateTime.Date;

            Selected = isSelected;
            IsEndOfSelectedPeriod = isSelected && selectedRange.EndDate.Date == DateTime.Date;
            IsStartOfSelectedPeriod = isSelected && selectedRange.StartDate.Date == DateTime.Date;

            TextColor = calculateTextColor(Selected);
            BackgroundColor = calculateBackgroundColor(Selected);
        }

        private MvxColor calculateTextColor(bool selected)
        {
            if (selected)
                return Color.Calendar.CellTextColorSelected;

            return IsInCurrentMonth
                ? Color.Calendar.CellTextColorInCurrentMonth
                : Color.Calendar.CellTextColorOutOfCurrentMonth;
        }

        private MvxColor calculateBackgroundColor(bool selected)
            => selected
            ? Color.Calendar.SelectedDayBackgoundColor
            : Color.Common.Transparent;
    }
}
