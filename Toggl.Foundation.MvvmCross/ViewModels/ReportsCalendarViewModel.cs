using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar.QuickSelectShortcuts;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class ReportsCalendarViewModel : MvxViewModel
    {
        private const int monthsToShow = 12;

        //Fields
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly ISubject<DateRangeParameter> selectedDateRangeSubject = new Subject<DateRangeParameter>();
        private readonly string[] dayHeaders =
        {
            Resources.SundayInitial,
            Resources.MondayInitial,
            Resources.TuesdayInitial,
            Resources.WednesdayInitial,
            Resources.ThursdayInitial,
            Resources.FridayInitial,
            Resources.SaturdayInitial
        };

        private CalendarMonth initialMonth;
        private CalendarDayViewModel startOfSelection;

        private CompositeDisposable disposeBag;

        //public BeginningOfWeek BeginningOfWeek { get; private set; }

        public IObservable<CalendarMonth> CurrentMonth { get; private set; }
        //=> convertPageIndexTocalendarMonth(CurrentPage);

        public IObservable<int> CurrentPage { get; }
        // = monthsToShow - 1;

        //[DependsOn(nameof(Months), nameof(CurrentPage))]
        public IObservable<int> RowsInCurrentMonth { get; private set; }
        // => Months[CurrentPage].RowCount;

        public IObservable<IImmutableList<CalendarPageViewModel>> Months { get; private set; }
        // = new List<CalendarPageViewModel>();

        public IObservable<DateRangeParameter> SelectedDateRangeObservable { get; private set; }
        // => selectedDateRangeSubject.AsObservable();

        public IObservable<IImmutableList<CalendarBaseQuickSelectShortcut>> QuickSelectShortcuts { get; private set; }

        public ReportsCalendarViewModel(ITimeService timeService, ITogglDataSource dataSource)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.timeService = timeService;
            this.dataSource = dataSource;
        }

        private void CalendarDayTapped(CalendarDayViewModel tappedDay)
        {
            if (startOfSelection == null)
            {
                var date = tappedDay.DateTimeOffset;

                var dateRange = DateRangeParameter
                    .WithDates(date, date)
                    .WithSource(ReportsSource.Calendar);
                startOfSelection = tappedDay;
                highlightDateRange(dateRange);
            }
            else
            {
                var startDate = startOfSelection.DateTimeOffset;
                var endDate = tappedDay.DateTimeOffset;

                var dateRange = DateRangeParameter
                    .WithDates(startDate, endDate)
                    .WithSource(ReportsSource.Calendar);
                startOfSelection = null;
                changeDateRange(dateRange);
            }
        }

        public override void Prepare()
        {
            base.Prepare();

            var now = timeService.CurrentDateTime;
            initialMonth = new CalendarMonth(now.Year, now.Month).AddMonths(-(monthsToShow - 1));
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            fillMonthArray();
            RaisePropertyChanged(nameof(CurrentMonth));

            SelectedDateRangeObservable = selectedDateRangeSubject.AsObservable();

            QuickSelectShortcuts =
                dataSource.User.Current
                    .Select(user => user.BeginningOfWeek)
                    .DistinctUntilChanged()
                    .Select(createQuickSelectShortcuts)
                    .Do(subscribeToSelectedDateRange);

            var initialShortcut = QuickSelectShortcuts.Single(shortcut => shortcut is CalendarThisWeekQuickSelectShortcut);
            changeDateRange(initialShortcut.GetDateRange().WithSource(ReportsSource.Initial));
        }

        private void subscribeToSelectedDateRange(IImmutableList<CalendarBaseQuickSelectShortcut> shortcuts)
        {
            disposeBag?.Dispose();
            disposeBag = new CompositeDisposable();

            foreach (var shortcut in shortcuts)
            {
                SelectedDateRangeObservable
                    .Subscribe(shortcut.OnDateRangeChanged)
                    .DisposedBy(disposeBag);
            }
        }

        public void OnToggleCalendar() => selectStartOfSelectionIfNeeded();

        public void OnHideCalendar() => selectStartOfSelectionIfNeeded();

        public string DayHeaderFor(int index)
            => dayHeaders[(index + (int)BeginningOfWeek + 7) % 7];

        private void selectStartOfSelectionIfNeeded()
        {
            if (startOfSelection == null) return;

            var date = startOfSelection.DateTimeOffset;
            var dateRange = DateRangeParameter
                .WithDates(date, date)
                .WithSource(ReportsSource.Calendar);
            changeDateRange(dateRange);
        }

        private void fillMonthArray()
        {
            var monthIterator = initialMonth;
            for (int i = 0; i < 12; i++, monthIterator = monthIterator.Next())
                Months.Add(new CalendarPageViewModel(monthIterator, BeginningOfWeek, timeService.CurrentDateTime));
        }

        private IImmutableList<CalendarBaseQuickSelectShortcut> createQuickSelectShortcuts(BeginningOfWeek beginningOfWeek)
            => ImmutableList.Create<CalendarBaseQuickSelectShortcut>(
                new CalendarTodayQuickSelectShortcut(timeService),
                new CalendarYesterdayQuickSelectShortcut(timeService),
                new CalendarThisWeekQuickSelectShortcut(timeService, beginningOfWeek),
                new CalendarLastWeekQuickSelectShortcut(timeService, beginningOfWeek),
                new CalendarThisMonthQuickSelectShortcut(timeService),
                new CalendarLastMonthQuickSelectShortcut(timeService),
                new CalendarThisYearQuickSelectShortcut(timeService)
            );

        private CalendarMonth convertPageIndexTocalendarMonth(int pageIndex)
            => initialMonth.AddMonths(pageIndex);

        private void changeDateRange(DateRangeParameter newDateRange)
        {
            startOfSelection = null;

            highlightDateRange(newDateRange);

            selectedDateRangeSubject.OnNext(newDateRange);
        }

        private void QuickSelect(CalendarBaseQuickSelectShortcut quickSelectShortCut)
            => changeDateRange(quickSelectShortCut.GetDateRange());

        private void highlightDateRange(DateRangeParameter dateRange)
        {
            Months.ForEach(month => month.Days.ForEach(day => day.OnSelectedRangeChanged(dateRange)));
        }
    }
}
