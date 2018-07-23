using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    public sealed class CalendarPageViewModel
    {
        private readonly DateTimeOffset today;
        private readonly BeginningOfWeek beginningOfWeek;

        public IImmutableList<CalendarDayViewModel> Days { get; }

        public CalendarMonth CalendarMonth { get; }

        public int RowCount { get; }

        public CalendarPageViewModel(
            CalendarMonth calendarMonth, BeginningOfWeek beginningOfWeek, DateTimeOffset today)
        {
            this.beginningOfWeek = beginningOfWeek;
            this.today = today;

            CalendarMonth = calendarMonth;

            Days = calculateDays().ToImmutableList();

            RowCount = Days.Count / 7;
        }

        private IEnumerable<CalendarDayViewModel> calculateDays()
        {
            foreach (var day in calculateDaysFromPreviousMonth())
            {
                yield return day;
            }

            foreach (var day in calculateDaysFromCurrentMonth())
            {
                yield return day;
            }

            foreach (var day in calculateDaysFromNextMonth())
            {
                yield return day;
            }
        }

        private IEnumerable<CalendarDayViewModel> calculateDaysFromPreviousMonth()
        {
            var firstDayOfMonth = CalendarMonth.DayOfWeek(1);
            if (firstDayOfMonth == beginningOfWeek.ToDayOfWeekEnum())
                yield break;

            var previousMonth = CalendarMonth.Previous();
            var daysInPreviousMonth = previousMonth.DaysInMonth;
            var daysToAdd = ((int)firstDayOfMonth - (int)beginningOfWeek.ToDayOfWeekEnum() + 7) % 7;

            for (int i = daysToAdd - 1; i >= 0; i--)
                yield return createDay(daysInPreviousMonth - i, previousMonth, false);
        }

        private IEnumerable<CalendarDayViewModel> calculateDaysFromCurrentMonth()
        {
            var daysInMonth = CalendarMonth.DaysInMonth;
            for (int i = 0; i < daysInMonth; i++)
                yield return createDay(i + 1, CalendarMonth, true);
        }

        private IEnumerable<CalendarDayViewModel> calculateDaysFromNextMonth()
        {
            var lastDayOfWeekInTargetMonth = (int)CalendarMonth
                .DayOfWeek(CalendarMonth.DaysInMonth);

            var nextMonth = CalendarMonth.AddMonths(1);
            var lastDayOfWeek = ((int)beginningOfWeek + 6) % 7;
            var daysToAdd = (lastDayOfWeek - lastDayOfWeekInTargetMonth + 7) % 7;

            for (int i = 0; i < daysToAdd; i++)
                yield return createDay(i + 1, nextMonth, false);
        }

        private CalendarDayViewModel createDay(int day, CalendarMonth month, bool isCurrentMonth)
            => new CalendarDayViewModel(day, month, isCurrentMonth, today);
    }
}
