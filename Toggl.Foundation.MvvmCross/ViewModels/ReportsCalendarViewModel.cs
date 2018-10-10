using System;
using System.Collections.Generic;
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
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class ReportsCalendarViewModel : MvxViewModel
    {
        public const int MonthsToShow = 13;

        //Fields
        private readonly ITimeService timeService;
        private readonly IDialogService dialogService;
        private readonly ITogglDataSource dataSource;
        private readonly IIntentDonationService intentDonationService;
        private readonly ISubject<ReportsDateRangeParameter> selectedDateRangeSubject = new Subject<ReportsDateRangeParameter>();
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

        private bool isInitialized;
        private CalendarMonth initialMonth;
        private CompositeDisposable disposableBag;
        private ReportsCalendarDayViewModel startOfSelection;
        private ReportPeriod reportPeriod = ReportPeriod.ThisWeek;

        public BeginningOfWeek BeginningOfWeek { get; private set; }

        //Properties
        [DependsOn(nameof(CurrentPage))]
        public CalendarMonth CurrentMonth => convertPageIndexTocalendarMonth(CurrentPage);

        public int CurrentPage { get; set; } = MonthsToShow - 1;

        [DependsOn(nameof(Months), nameof(CurrentPage))]
        public int RowsInCurrentMonth => Months[CurrentPage].RowCount;

        public List<ReportsCalendarPageViewModel> Months { get; } = new List<ReportsCalendarPageViewModel>();

        public IObservable<ReportsDateRangeParameter> SelectedDateRangeObservable
            => selectedDateRangeSubject.AsObservable();

        public List<ReportsCalendarBaseQuickSelectShortcut> QuickSelectShortcuts { get; private set; }

        public IMvxAsyncCommand<ReportsCalendarDayViewModel> CalendarDayTappedCommand { get; }

        public IMvxCommand<ReportsCalendarBaseQuickSelectShortcut> QuickSelectCommand { get; }

        public ReportsCalendarViewModel(
            ITimeService timeService,
            IDialogService dialogService,
            ITogglDataSource dataSource,
            IIntentDonationService intentDonationService)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(intentDonationService, nameof(intentDonationService));

            this.timeService = timeService;
            this.dialogService = dialogService;
            this.dataSource = dataSource;
            this.intentDonationService = intentDonationService;

            CalendarDayTappedCommand = new MvxAsyncCommand<ReportsCalendarDayViewModel>(calendarDayTapped);
            QuickSelectCommand = new MvxCommand<ReportsCalendarBaseQuickSelectShortcut>(quickSelect);

            disposableBag = new CompositeDisposable();
        }

        private async Task calendarDayTapped(ReportsCalendarDayViewModel tappedDay)
        {
            if (startOfSelection == null)
            {
                var date = tappedDay.DateTimeOffset;

                var dateRange = ReportsDateRangeParameter
                    .WithDates(date, date)
                    .WithSource(ReportsSource.Calendar);
                startOfSelection = tappedDay;
                highlightDateRange(dateRange);
            }
            else
            {
                var startDate = startOfSelection.DateTimeOffset;
                var endDate = tappedDay.DateTimeOffset;

                if (System.Math.Abs((endDate - startDate).Days) > 365)
                {
                    await dialogService.Alert(
                        Resources.ReportTooLongTitle,
                        Resources.ReportTooLongDescription,
                        Resources.Ok
                    );
                }
                else
                {
                    var dateRange = ReportsDateRangeParameter
                        .WithDates(startDate, endDate)
                        .WithSource(ReportsSource.Calendar);
                    startOfSelection = null;
                    changeDateRange(dateRange);
                }
            }
        }

        public override void Prepare()
        {
            base.Prepare();

            var now = timeService.CurrentDateTime;
            initialMonth = new CalendarMonth(now.Year, now.Month).AddMonths(-(MonthsToShow - 1));
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            BeginningOfWeek = (await dataSource.User.Current.FirstAsync()).BeginningOfWeek;
            fillMonthArray();
            RaisePropertyChanged(nameof(CurrentMonth));

            QuickSelectShortcuts = createQuickSelectShortcuts();

            QuickSelectShortcuts
                .Select(quickSelectShortcut => SelectedDateRangeObservable.Subscribe(
                    quickSelectShortcut.OnDateRangeChanged))
                .ForEach(disposableBag.Add);

            var initialShortcut = QuickSelectShortcuts.Single(shortcut => shortcut.Period == reportPeriod);
            changeDateRange(initialShortcut.GetDateRange().WithSource(ReportsSource.Initial));
            isInitialized = true;
        }

        public void OnToggleCalendar() => selectStartOfSelectionIfNeeded();

        public void OnHideCalendar() => selectStartOfSelectionIfNeeded();

        public string DayHeaderFor(int index)
            => dayHeaders[(index + (int)BeginningOfWeek + 7) % 7];

        public void SelectPeriod(ReportPeriod period)
        {
            reportPeriod = period;

            if (isInitialized)
            {
                var initialShortcut = QuickSelectShortcuts.Single(shortcut => shortcut.Period == period);
                changeDateRange(initialShortcut.GetDateRange().WithSource(ReportsSource.Initial));
            }
        }

        private void selectStartOfSelectionIfNeeded()
        {
            if (startOfSelection == null) return;

            var date = startOfSelection.DateTimeOffset;
            var dateRange = ReportsDateRangeParameter
                .WithDates(date, date)
                .WithSource(ReportsSource.Calendar);
            changeDateRange(dateRange);
        }

        private void fillMonthArray()
        {
            var monthIterator = initialMonth;
            for (int i = 0; i < MonthsToShow; i++, monthIterator = monthIterator.Next())
                Months.Add(new ReportsCalendarPageViewModel(monthIterator, BeginningOfWeek, timeService.CurrentDateTime));
        }

        private List<ReportsCalendarBaseQuickSelectShortcut> createQuickSelectShortcuts()
            => new List<ReportsCalendarBaseQuickSelectShortcut>
            {
                new ReportsCalendarTodayQuickSelectShortcut(timeService),
                new ReportsCalendarYesterdayQuickSelectShortcut(timeService),
                new ReportsCalendarThisWeekQuickSelectShortcut(timeService, BeginningOfWeek),
                new ReportsCalendarLastWeekQuickSelectShortcut(timeService, BeginningOfWeek),
                new ReportsCalendarThisMonthQuickSelectShortcut(timeService),
                new ReportsCalendarLastMonthQuickSelectShortcut(timeService),
                new ReportsCalendarThisYearQuickSelectShortcut(timeService)
            };

        private CalendarMonth convertPageIndexTocalendarMonth(int pageIndex)
            => initialMonth.AddMonths(pageIndex);

        private void changeDateRange(ReportsDateRangeParameter newDateRange)
        {
            startOfSelection = null;

            highlightDateRange(newDateRange);

            selectedDateRangeSubject.OnNext(newDateRange);
        }

        private void quickSelect(ReportsCalendarBaseQuickSelectShortcut quickSelectShortCut)
        {
            intentDonationService.DonateShowReport(quickSelectShortCut.Period);
            changeDateRange(quickSelectShortCut.GetDateRange());
        }

        private void highlightDateRange(ReportsDateRangeParameter dateRange)
        {
            Months.ForEach(month => month.Days.ForEach(day => day.OnSelectedRangeChanged(dateRange)));
        }
    }
}
