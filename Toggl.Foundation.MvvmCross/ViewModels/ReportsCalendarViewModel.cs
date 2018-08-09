using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class ReportsCalendarViewModel : MvxViewModel
    {
        public const int MonthsToShow = 12;

        private static readonly string[] dayHeaders =
        {
            Resources.SundayInitial,
            Resources.MondayInitial,
            Resources.TuesdayInitial,
            Resources.WednesdayInitial,
            Resources.ThursdayInitial,
            Resources.FridayInitial,
            Resources.SaturdayInitial
        };

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;

        private readonly CalendarMonth initialMonth;
        private readonly ISubject<int> currentPageSubject = new Subject<int>();
        private readonly ISubject<ReportsDateRangeParameter> highlitDateRangeSubject = new Subject<ReportsDateRangeParameter>();
        private readonly ISubject<ReportsDateRangeParameter> selectedDateRangeSubject = new Subject<ReportsDateRangeParameter>();

        private CalendarDayViewModel startOfSelection;
        private QuickSelectShortcut weeklyQuickSelectShortcut;
        private CompositeDisposable calendarDisposeBag;
        private CompositeDisposable shortcutDisposeBag;

        public IObservable<int> CurrentPage { get; }
        
        public IObservable<string> CurrentYear { get; }

        public IObservable<Unit> ReloadCalendar { get; }

        public IObservable<int> RowsInCurrentMonth { get; }

        public IObservable<string> CurrentMonthName { get; }

        public IObservable<IImmutableList<string>> DayHeaders { get; }

        public IObservable<IImmutableList<CalendarPageViewModel>> Months { get; }

        public IObservable<ReportsDateRangeParameter> SelectedDateRangeObservable { get; }

        public IObservable<IImmutableList<QuickSelectShortcut>> QuickSelectShortcuts { get; }

        public ReportsCalendarViewModel(ITimeService timeService, ITogglDataSource dataSource, ISchedulerProvider schedulerProvider)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.timeService = timeService;
            this.dataSource = dataSource;

            var currentDate = timeService.CurrentDateTime;
            initialMonth = new CalendarMonth(currentDate.Year, currentDate.Month).AddMonths(-MonthsToShow + 1);

            var beginningOfWeekObservable =
                dataSource.User.Current
                    .Select(user => user.BeginningOfWeek)
                    .DistinctUntilChanged()
                    .ConnectedReplay();

            DayHeaders = beginningOfWeekObservable
                .Select(headers)
                .AsDriver(schedulerProvider);

            SelectedDateRangeObservable = selectedDateRangeSubject
                .AsObservable()
                .AsDriver(schedulerProvider);

            CurrentPage = currentPageSubject
                .StartWith(MonthsToShow - 1)
                .AsObservable()
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            var currentMonthInfoObservable = CurrentPage
                .Select(initialMonth.AddMonths);

            ReloadCalendar = SelectedDateRangeObservable
                .Merge(highlitDateRangeSubject.AsObservable())
                .SelectUnit()
                .AsDriver(schedulerProvider);

            CurrentMonthName = currentMonthInfoObservable
                .Select(month => month.Month)
                .Select(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName)
                .AsDriver(schedulerProvider);

            CurrentYear = currentMonthInfoObservable
                .Select(month => month.Year.ToString())
                .AsDriver(schedulerProvider);

            QuickSelectShortcuts = beginningOfWeekObservable
                .Select(createQuickSelectShortcuts)
                .Do(subscribeToSelectedDateRange)
                .AsDriver(schedulerProvider);

            Months = beginningOfWeekObservable
                .Select(calendarPages)
                .Do(subscribeToSelectedDateRange)
                .AsDriver(schedulerProvider);

            RowsInCurrentMonth = CurrentPage
                .CombineLatest(Months, 
                    (currentPage, months) => months[currentPage].RowCount)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            IImmutableList<string> headers(BeginningOfWeek beginningOfWeek)
                => Enumerable.Range(0, 7)
                    .Select(index => dayHeaders[(index + (int)beginningOfWeek + 7) % 7])
                    .ToImmutableList();

            IImmutableList<CalendarPageViewModel> calendarPages(BeginningOfWeek beginningOfWeek)
            {
                var now = timeService.CurrentDateTime;

                return Enumerable.Range(0, MonthsToShow)
                    .Select(initialMonth.AddMonths)
                    .Select(calendarMonth => new CalendarPageViewModel(calendarMonth, beginningOfWeek, now))
                    .ToImmutableList();
            }

            IImmutableList<QuickSelectShortcut> createQuickSelectShortcuts(BeginningOfWeek beginningOfWeek) 
                => ImmutableList.Create<QuickSelectShortcut>(
                    QuickSelectShortcut.ForToday(timeService),
                    QuickSelectShortcut.ForYesterday(timeService),
                    weeklyQuickSelectShortcut = QuickSelectShortcut.ForThisWeek(timeService, beginningOfWeek),
                    QuickSelectShortcut.ForLastWeek(timeService, beginningOfWeek),
                    QuickSelectShortcut.ForThisMonth(timeService),
                    QuickSelectShortcut.ForLastMonth(timeService),
                    QuickSelectShortcut.ForThisYear(timeService)
                );
        }

        public void OnCurrentPageChanged(int currentPage)
        {
            currentPageSubject.OnNext(currentPage);
        }

        public void OnToggleCalendar()
        {
            if (startOfSelection == null) return;

            var date = startOfSelection.DateTime;
            var dateRange = ReportsDateRangeParameter
                .WithDates(date, date)
                .WithSource(ReportsSource.Calendar);
            
            changeDateRange(dateRange);
        }

        public void QuickSelect(QuickSelectShortcut quickSelectShortCut)
        {
            changeDateRange(quickSelectShortCut.DateRange);
            currentPageSubject.OnNext(MonthsToShow - 1 - quickSelectShortCut.PageOffset);
        }

        public void CalendarDayTapped(CalendarDayViewModel tappedDay)
        {
            if (startOfSelection == null)
            {
                var date = tappedDay.DateTime;

                var dateRange = ReportsDateRangeParameter
                    .WithDates(date, date)
                    .WithSource(ReportsSource.Calendar);
                startOfSelection = tappedDay;
                highlitDateRangeSubject.OnNext(dateRange);
            }
            else
            {
                var startDate = startOfSelection.DateTime;
                var endDate = tappedDay.DateTime;

                var dateRange = ReportsDateRangeParameter
                    .WithDates(startDate, endDate)
                    .WithSource(ReportsSource.Calendar);
                startOfSelection = null;
                changeDateRange(dateRange);
            }
        }

        private void subscribeToSelectedDateRange(IImmutableList<CalendarPageViewModel> months)
        {
            calendarDisposeBag?.Dispose();
            calendarDisposeBag = new CompositeDisposable();

            var highlightObservable = highlitDateRangeSubject.AsObservable();

            foreach (var month in months)
            {
                foreach (var day in month.Days)
                {
                    SelectedDateRangeObservable
                        .Subscribe(day.OnSelectedRangeChanged)
                        .DisposedBy(calendarDisposeBag);

                    highlightObservable
                        .Subscribe(day.OnSelectedRangeChanged)
                        .DisposedBy(calendarDisposeBag);
                }
            }
        }

        private void subscribeToSelectedDateRange(IImmutableList<QuickSelectShortcut> shortcuts)
        {
            shortcutDisposeBag?.Dispose();
            shortcutDisposeBag = new CompositeDisposable();

            foreach (var shortcut in shortcuts)
            {
                SelectedDateRangeObservable
                    .Subscribe(shortcut.OnDateRangeChanged)
                    .DisposedBy(shortcutDisposeBag);
            }

            changeDateRange(weeklyQuickSelectShortcut.DateRange);
        }

        private void changeDateRange(ReportsDateRangeParameter newDateRange)
        {
            startOfSelection = null;
            selectedDateRangeSubject.OnNext(newDateRange);
        }
    }
}
