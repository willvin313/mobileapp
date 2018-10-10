using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Toggl.Foundation.Services;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class ReportsCalendarViewModelTests
    {
        public abstract class ReportsCalendarViewModelTest
            : BaseViewModelTests<ReportsCalendarViewModel>
        {
            protected override ReportsCalendarViewModel CreateViewModel()
                => new ReportsCalendarViewModel(TimeService, DialogService, DataSource, IntentDonationService);
        }

        public sealed class TheConstructor : ReportsCalendarViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useTimeService,
                bool useDialogService,
                bool useDataSource,
                bool useIntentDonationService
                )
            {
                var timeService = useTimeService ? TimeService : null;
                var dialogService = useDialogService ? DialogService : null;
                var dataSource = useDataSource ? DataSource : null;
                var intentDonationService = useIntentDonationService ? IntentDonationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new ReportsCalendarViewModel(timeService, dialogService, dataSource, intentDonationService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod
            : ReportsCalendarViewModelTest
        {
            [Property]
            public void InitializesCurrentMonthPropertyToCurrentDateTimeOfTimeService(
                DateTimeOffset now)
            {
                TimeService.CurrentDateTime.Returns(now);

                ViewModel.Prepare();

                ViewModel.CurrentMonth.Year.Should().Be(now.Year);
                ViewModel.CurrentMonth.Month.Should().Be(now.Month);
            }
        }

        public sealed class TheInitializeMethod : ReportsCalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task InitializesTheMonthsPropertyToTheMonthsToShow()
            {
                var now = new DateTimeOffset(2020, 4, 2, 1, 1, 1, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);
                ViewModel.Prepare();

                await ViewModel.Initialize();

                ViewModel.Months.Should().HaveCount(ReportsCalendarViewModel.MonthsToShow);
                var firstDateTime = now.AddMonths(-(ReportsCalendarViewModel.MonthsToShow - 1));
                var month = new CalendarMonth(
                    firstDateTime.Year, firstDateTime.Month);
                for (int i = 0; i < (ReportsCalendarViewModel.MonthsToShow - 1); i++, month = month.Next())
                {
                    ViewModel.Months[i].CalendarMonth.Should().Be(month);
                }
            }

            [Fact, LogIfTooSlow]
            public async Task FillsQuickSelectShortcutlist()
            {
                var expectedShortCuts = new List<Type>
                {
                    typeof(ReportsCalendarTodayQuickSelectShortcut),
                    typeof(ReportsCalendarYesterdayQuickSelectShortcut),
                    typeof(ReportsCalendarThisWeekQuickSelectShortcut),
                    typeof(ReportsCalendarLastWeekQuickSelectShortcut),
                    typeof(ReportsCalendarThisMonthQuickSelectShortcut),
                    typeof(ReportsCalendarLastMonthQuickSelectShortcut),
                    typeof(ReportsCalendarThisYearQuickSelectShortcut)
                };
                var now = new DateTimeOffset(2020, 4, 2, 1, 1, 1, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);
                ViewModel.Prepare();

                await ViewModel.Initialize();

                for (int i = 0; i < ViewModel.QuickSelectShortcuts.Count; i++)
                    ViewModel.QuickSelectShortcuts[i].GetType().Should().Be(expectedShortCuts[i]);
            }

            [Property]
            public void InitializesTheBeginningOfWeekProperty(BeginningOfWeek beginningOfWeek)
            {
                var user = Substitute.For<IThreadSafeUser>();
                user.BeginningOfWeek.Returns(beginningOfWeek);
                DataSource.User.Current.Returns(Observable.Return(user));
                TimeService.CurrentDateTime.Returns(new DateTimeOffset(2018, 4, 20, 16, 20, 0, TimeSpan.Zero));
                ViewModel.Prepare();
                ViewModel.Initialize().Wait();

                ViewModel.BeginningOfWeek.Should().Be(beginningOfWeek);
            }

            [Fact, LogIfTooSlow]
            public void InitializesTheDateRangeWithTheCurrentWeek()
            {
                var user = Substitute.For<IThreadSafeUser>();
                user.BeginningOfWeek.Returns(BeginningOfWeek.Sunday);
                DataSource.User.Current.Returns(Observable.Return(user));
                var now = new DateTimeOffset(2018, 7, 1, 1, 1, 1, TimeSpan.Zero);
                var observer = Substitute.For<IObserver<ReportsDateRangeParameter>>();
                TimeService.CurrentDateTime.Returns(now);
                ViewModel.SelectedDateRangeObservable.Subscribe(observer);
                ViewModel.Prepare();
                ViewModel.Initialize().Wait();

                observer.Received().OnNext(Arg.Is<ReportsDateRangeParameter>(
                    dateRange => ensureDateRangeIsCorrect(
                        dateRange,
                        ViewModel.Months[ReportsCalendarViewModel.MonthsToShow - 1].Days[0],
                        ViewModel.Months[ReportsCalendarViewModel.MonthsToShow - 1].Days[6]
                    )));
            }
        }

        public sealed class TheCurrentMonthProperty : ReportsCalendarViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(2017, 12, 12, 2017, 12)]
            [InlineData(2017, 5, 0, 2016, 5)]
            [InlineData(2017, 5, 12, 2017, 5)]
            [InlineData(2017, 5, 7, 2016, 12)]
            [InlineData(2017, 5, 8, 2017, 1)]
            public void RepresentsTheCurrentPage(
                int currentYear,
                int currentMonth,
                int currentPage,
                int expectedYear,
                int expectedMonth)
            {
                var now = new DateTimeOffset(currentYear, currentMonth, 1, 0, 0, 0, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);
                ViewModel.Prepare();
                ViewModel.CurrentPage = currentPage;

                ViewModel.CurrentMonth.Year.Should().Be(expectedYear);
                ViewModel.CurrentMonth.Month.Should().Be(expectedMonth);
            }
        }

        public sealed class TheCurrentPageProperty : ReportsCalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsInitializedToTheMonthsToShowConstantMinusOne()
            {
                ViewModel.CurrentPage.Should().Be(ReportsCalendarViewModel.MonthsToShow - 1);
            }
        }

        public sealed class TheRowsInCurrentMonthProperty : ReportsCalendarViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(2017, 12, 12, BeginningOfWeek.Monday, 5)]
            [InlineData(2017, 12, 10, BeginningOfWeek.Monday, 6)]
            [InlineData(2017, 2, 12, BeginningOfWeek.Wednesday, 4)]
            public async Task ReturnsTheRowCountOfCurrentlyShownMonth(
                int currentYear,
                int currentMonth,
                int currentPage,
                BeginningOfWeek beginningOfWeek,
                int expectedRowCount)
            {
                var now = new DateTimeOffset(currentYear, currentMonth, 1, 0, 0, 0, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);
                var user = Substitute.For<IThreadSafeUser>();
                user.BeginningOfWeek.Returns(beginningOfWeek);
                DataSource.User.Current.Returns(Observable.Return(user));
                ViewModel.Prepare();
                await ViewModel.Initialize();
                ViewModel.CurrentPage = currentPage;

                ViewModel.RowsInCurrentMonth.Should().Be(expectedRowCount);
            }
        }

        public sealed class TheSelectedDateRangeObservableProperty : ReportsCalendarViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(0, 0, 3, 3)]
            [InlineData(5, 23, 9, 0)]
            public void EmitsNewElementWheneverDateRangeIsChangedByTappingTwoCells(
                int startPageIndex,
                int startCellIndex,
                int endPageIndex,
                int endCellIndex)
            {
                var now = new DateTimeOffset(2017, 12, 19, 1, 2, 3, TimeSpan.Zero);
                var observer = Substitute.For<IObserver<ReportsDateRangeParameter>>();
                TimeService.CurrentDateTime.Returns(now);
                ViewModel.SelectedDateRangeObservable.Subscribe(observer);
                ViewModel.Prepare();
                ViewModel.Initialize().Wait();
                var startMonth = ViewModel.Months[startPageIndex];
                var firstTappedCellViewModel = startMonth.Days[startCellIndex];
                var endMonth = ViewModel.Months[endPageIndex];
                var secondTappedCellViewModel = endMonth.Days[endCellIndex];
                ViewModel.CalendarDayTappedCommand.Execute(firstTappedCellViewModel);
                ViewModel.CalendarDayTappedCommand.Execute(secondTappedCellViewModel);

                observer.Received().OnNext(Arg.Is<ReportsDateRangeParameter>(
                    dateRange => ensureDateRangeIsCorrect(
                        dateRange,
                        firstTappedCellViewModel,
                        secondTappedCellViewModel)));
            }
        }

        private static bool ensureDateRangeIsCorrect(
            ReportsDateRangeParameter dateRange,
            ReportsCalendarDayViewModel expectedStart,
            ReportsCalendarDayViewModel expectedEnd)
            => dateRange.StartDate.Year == expectedStart.CalendarMonth.Year
               && dateRange.StartDate.Month == expectedStart.CalendarMonth.Month
               && dateRange.StartDate.Day == expectedStart.Day
               && dateRange.EndDate.Year == expectedEnd.CalendarMonth.Year
               && dateRange.EndDate.Month == expectedEnd.CalendarMonth.Month
               && dateRange.EndDate.Day == expectedEnd.Day;

        public abstract class TheCalendarDayTappedCommand : ReportsCalendarViewModelTest
        {
            public TheCalendarDayTappedCommand()
            {
                var now = new DateTimeOffset(2017, 12, 19, 1, 2, 3, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);
                ViewModel.Prepare();
                ViewModel.Initialize().Wait();
            }

            protected ReportsCalendarDayViewModel FindDayViewModel(int monthIndex, int dayIndex)
                => ViewModel.Months[monthIndex].Days[dayIndex];

            public sealed class AfterTappingOneCell : TheCalendarDayTappedCommand
            {
                [Theory, LogIfTooSlow]
                [InlineData(5, 8)]
                public void MarksTheFirstTappedCellAsSelected(
                int monthIndex, int dayIndex)
                {
                    var dayViewModel = FindDayViewModel(monthIndex, dayIndex);

                    ViewModel.CalendarDayTappedCommand.Execute(dayViewModel);

                    dayViewModel.Selected.Should().BeTrue();
                }

                [Theory, LogIfTooSlow]
                [InlineData(11, 0)]
                public void MarksTheFirstTappedCellAsStartOfSelection(
                    int monthIndex, int dayIndex)
                {
                    var dayViewModel = FindDayViewModel(monthIndex, dayIndex);

                    ViewModel.CalendarDayTappedCommand.Execute(dayViewModel);

                    dayViewModel.IsStartOfSelectedPeriod.Should().BeTrue();
                }

                [Theory, LogIfTooSlow]
                [InlineData(3, 20)]
                public void MarksTheFirstTappedCellAsEndOfSelection(
                    int monthIndex, int dayIndex)
                {
                    var dayViewModel = FindDayViewModel(monthIndex, dayIndex);

                    ViewModel.CalendarDayTappedCommand.Execute(dayViewModel);

                    dayViewModel.IsEndOfSelectedPeriod.Should().BeTrue();
                }
            }

            public sealed class AfterTappingTwoCells : TheCalendarDayTappedCommand
            {
                [Theory, LogIfTooSlow]
                [InlineData(0, 0, 5, 8)]
                public void MarksTheFirstTappedCellAsNotEndOfSelection(
                    int firstMonthIndex,
                    int firstDayindex,
                    int secondMonthIndex,
                    int secondDayIndex)
                {
                    var firstDayViewModel = FindDayViewModel(firstMonthIndex, firstDayindex);
                    var secondDayViewModel = FindDayViewModel(secondMonthIndex, secondDayIndex);

                    ViewModel.CalendarDayTappedCommand.Execute(firstDayViewModel);
                    ViewModel.CalendarDayTappedCommand.Execute(secondDayViewModel);

                    firstDayViewModel.IsEndOfSelectedPeriod.Should().BeFalse();
                }

                [Theory, LogIfTooSlow]
                [InlineData(1, 1, 9, 9)]
                public void MarksTheSecondTappedCellAsEndOfSelection(
                    int firstMonthIndex,
                    int firstDayindex,
                    int secondMonthIndex,
                    int secondDayIndex)
                {
                    var firstDayViewModel = FindDayViewModel(firstMonthIndex, firstDayindex);
                    var secondDayViewModel = FindDayViewModel(secondMonthIndex, secondDayIndex);

                    ViewModel.CalendarDayTappedCommand.Execute(firstDayViewModel);
                    ViewModel.CalendarDayTappedCommand.Execute(secondDayViewModel);

                    secondDayViewModel.IsEndOfSelectedPeriod.Should().BeTrue();
                }

                [Theory, LogIfTooSlow]
                [InlineData(1, 2, 3, 4)]
                public void MarksTheSecondTappedCellAsNotStartOfSelection(
                    int firstMonthIndex,
                    int firstDayindex,
                    int secondMonthIndex,
                    int secondDayIndex)
                {
                    var firstDayViewModel = FindDayViewModel(firstMonthIndex, firstDayindex);
                    var secondDayViewModel = FindDayViewModel(secondMonthIndex, secondDayIndex);

                    ViewModel.CalendarDayTappedCommand.Execute(firstDayViewModel);
                    ViewModel.CalendarDayTappedCommand.Execute(secondDayViewModel);

                    secondDayViewModel.IsStartOfSelectedPeriod.Should().BeFalse();
                }

                [Theory, LogIfTooSlow]
                [InlineData(2, 15, 7, 20)]
                public void MarksTheWholeIntervalAsSelected(
                    int firstMonthIndex,
                    int firstDayindex,
                    int secondMonthIndex,
                    int secondDayIndex)
                {
                    var firstDayViewModel = FindDayViewModel(firstMonthIndex, firstDayindex);
                    var secondDayViewModel = FindDayViewModel(secondMonthIndex, secondDayIndex);

                    ViewModel.CalendarDayTappedCommand.Execute(firstDayViewModel);
                    ViewModel.CalendarDayTappedCommand.Execute(secondDayViewModel);

                    for (int monthIndex = firstMonthIndex; monthIndex <= secondMonthIndex; monthIndex++)
                    {
                        var month = ViewModel.Months[monthIndex];
                        var startIndex = monthIndex == firstMonthIndex
                            ? firstDayindex
                            : 0;
                        var endIndex = monthIndex == secondMonthIndex
                            ? secondDayIndex
                            : month.Days.Count - 1;
                        assertDaysInMonthSelected(month, startIndex, endIndex);
                    }
                }

                private void assertDaysInMonthSelected(
                    ReportsCalendarPageViewModel calendarPage, int startindex, int endIndex)
                {
                    for (int i = startindex; i <= endIndex; i++)
                        calendarPage.Days[i].Selected.Should().BeTrue();
                }
            }
        }

        public sealed class TheQuickSelectCommand : ReportsCalendarViewModelTest
        {
            [Property]
            public void UsingAnyOfTheShortcutsDoesNotThrowAnyTimeOfTheYear(DateTimeOffset now)
            {
                TimeService.CurrentDateTime.Returns(now);
                // in this property test it is not possible to use the default ViewModel,
                // because we have to reset it in each iteration of the test
                var viewModel = CreateViewModel();
                viewModel.Prepare();
                viewModel.Initialize().Wait();

                foreach (var shortcut in viewModel.QuickSelectShortcuts)
                {
                    Action usingShortcut = () => viewModel.QuickSelectCommand.Execute(shortcut);
                    usingShortcut.Should().NotThrow();
                }
            }

            [Property]
            public void SelectingAnyDateRangeDoesNotMakeTheAppCrash(DateTimeOffset a, DateTimeOffset b, DateTimeOffset c)
            {
                var dates = new[] { a, b, c };
                Array.Sort(dates);
                var start = dates[0];
                var now = dates[1];
                var end = dates[2];
                TimeService.CurrentDateTime.Returns(now);
                var selectedRange = ReportsDateRangeParameter.WithDates(start, end).WithSource(ReportsSource.Calendar);
                var customShortcut = new CustomShortcut(selectedRange, TimeService);

                // in this property test it is not possible to use the default ViewModel,
                // because we have to reset it in each iteration of the test
                var viewModel = CreateViewModel();
                viewModel.Prepare();
                viewModel.Initialize().Wait();

                Action usingShortcut = () => viewModel.QuickSelectCommand.Execute(customShortcut);

                usingShortcut.Should().NotThrow();
            }

            private sealed class CustomShortcut : ReportsCalendarBaseQuickSelectShortcut
            {
                private ReportsDateRangeParameter range;

                public CustomShortcut(ReportsDateRangeParameter range, ITimeService timeService) : base(timeService, "", ReportPeriod.Unknown)
                {
                    this.range = range;
                }

                public override ReportsDateRangeParameter GetDateRange()
                    => range;
            }
        }
    }
}
