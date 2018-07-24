using System;
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
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Xunit;
using System.Globalization;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class ReportsCalendarViewModelTests
    {
        public abstract class ReportsCalendarViewModelTest : BaseViewModelTests<ReportsCalendarViewModel>
        {
            protected override ReportsCalendarViewModel CreateViewModel()
                => new ReportsCalendarViewModel(TimeService, DataSource);
        }

        public sealed class TheConstructor : ReportsCalendarViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ClassData(typeof(TwoParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useTimeService, bool useDataSource)
            {
                var timeService = useTimeService ? TimeService : null;
                var dataSource = useDataSource ? DataSource : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new ReportsCalendarViewModel(timeService, dataSource);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }

            [Property]
            public void InitializesCurrentMonthPropertyToCurrentDateTimeOfTimeService(DateTimeOffset now)
            {
                TimeService.CurrentDateTime.Returns(now);

                var viewModel = CreateViewModel();

                var currentYear = viewModel.CurrentYear.FirstAsync().GetAwaiter().GetResult();
                var currentMonth = viewModel.CurrentMonthName.FirstAsync().GetAwaiter().GetResult();

                currentYear.Should().Be(now.Year.ToString());
                currentMonth.Should().Be(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(now.Month));
            }

            [Fact, LogIfTooSlow]
            public async Task InitializesTheMonthsPropertyToLast12Months()
            {
                var now = new DateTimeOffset(2020, 4, 2, 1, 1, 1, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);

                var months = await ViewModel.Months.FirstAsync();

                months.Should().HaveCount(12);
                var firstDateTime = now.AddMonths(-11);
                var month = new CalendarMonth(firstDateTime.Year, firstDateTime.Month);

                for (int i = 0; i < 12; i++, month = month.Next())
                {
                    months[i].CalendarMonth.Should().Be(month);
                }
            }

            [Fact, LogIfTooSlow]
            public async Task FillsQuickSelectShortcutlist()
            {
                var now = new DateTimeOffset(2020, 4, 2, 1, 1, 1, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);

                var shortcuts = await ViewModel.QuickSelectShortcuts.FirstAsync();
                shortcuts.Should().HaveCount(7);
            }

            [Fact, LogIfTooSlow]
            public async Task InitializesTheDateRangeWithTheCurrentWeek()
            {
                var user = Substitute.For<IThreadSafeUser>();
                user.BeginningOfWeek.Returns(BeginningOfWeek.Sunday);
                DataSource.User.Current.Returns(Observable.Return(user));
                var now = new DateTimeOffset(2018, 7, 1, 1, 1, 1, TimeSpan.Zero);
                var observer = Substitute.For<IObserver<DateRangeParameter>>();
                TimeService.CurrentDateTime.Returns(now);
                ViewModel.SelectedDateRangeObservable.Subscribe(observer);

                var months = await ViewModel.Months.FirstAsync();

                observer.Received().OnNext(Arg.Is<DateRangeParameter>(
                    dateRange => ensureDateRangeIsCorrect(
                        dateRange,
                        months[11].Days[0],
                        months[11].Days[6]
                    )));
            }
        }

        public sealed class TheCurrentMonthProperty : ReportsCalendarViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(2017, 12, 11, 2017, 12)]
            [InlineData(2017, 5, 0, 2016, 6)]
            [InlineData(2017, 5, 11, 2017, 5)]
            [InlineData(2017, 5, 6, 2016, 12)]
            [InlineData(2017, 5, 7, 2017, 1)]
            public async Task RepresentsTheCurrentPage(
                int currentYear,
                int currentMonth,
                int currentPage,
                int expectedYear,
                int expectedMonth)
            {
                var now = new DateTimeOffset(currentYear, currentMonth, 1, 0, 0, 0, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);
                ViewModel.Prepare();
                ViewModel.OnCurrentPageChanged(currentPage);

                var year = await ViewModel.CurrentYear.FirstAsync();
                var month = await ViewModel.CurrentMonthName.FirstAsync();

                year.Should().Be(expectedYear.ToString());
                month.Should().Be(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(expectedMonth));
            }
        }

        public sealed class TheCurrentPageProperty : ReportsCalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsInitializedTo11()
            {
                ViewModel.CurrentPage.Should().Be(11);
            }
        }

        public sealed class TheRowsInCurrentMonthProperty : ReportsCalendarViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(2017, 12, 11, BeginningOfWeek.Monday, 5)]
            [InlineData(2017, 12, 9, BeginningOfWeek.Monday, 6)]
            [InlineData(2017, 2, 11, BeginningOfWeek.Wednesday, 4)]
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
                ViewModel.OnCurrentPageChanged(currentPage);

                var rowsInCurrentMonth = await ViewModel.RowsInCurrentMonth;

                rowsInCurrentMonth.Should().Be(expectedRowCount);
            }
        }

        public sealed class TheSelectedDateRangeObservableProperty : ReportsCalendarViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(0, 0, 3, 3)]
            [InlineData(5, 23, 9, 0)]
            public async Task EmitsNewElementWheneverDateRangeIsChangedByTappingTwoCells(
                int startPageIndex,
                int startCellIndex,
                int endPageIndex,
                int endCellIndex)
            {
                var now = new DateTimeOffset(2017, 12, 19, 1, 2, 3, TimeSpan.Zero);
                var observer = Substitute.For<IObserver<DateRangeParameter>>();
                TimeService.CurrentDateTime.Returns(now);
                ViewModel.SelectedDateRangeObservable.Subscribe(observer);

                var months = await ViewModel.Months;
                var startMonth = months[startPageIndex];
                var firstTappedCellViewModel = startMonth.Days[startCellIndex];
                var endMonth = months[endPageIndex];
                var secondTappedCellViewModel = endMonth.Days[endCellIndex];
                ViewModel.CalendarDayTapped(firstTappedCellViewModel);
                ViewModel.CalendarDayTapped(secondTappedCellViewModel);

                observer.Received().OnNext(Arg.Is<DateRangeParameter>(
                    dateRange => ensureDateRangeIsCorrect(
                        dateRange,
                        firstTappedCellViewModel,
                        secondTappedCellViewModel)));
            }
        }

        private static bool ensureDateRangeIsCorrect(
            DateRangeParameter dateRange,
            CalendarDayViewModel expectedStart,
            CalendarDayViewModel expectedEnd)
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

            protected async Task<CalendarDayViewModel> FindDayViewModel(int monthIndex, int dayIndex)
                => (await ViewModel.Months)[monthIndex].Days[dayIndex];

            public sealed class AfterTappingOneCell : TheCalendarDayTappedCommand
            {
                [Theory, LogIfTooSlow]
                [InlineData(5, 8)]
                public async Task MarksTheFirstTappedCellAsSelected(
                int monthIndex, int dayIndex)
                {
                    var dayViewModel = await FindDayViewModel(monthIndex, dayIndex);

                    ViewModel.CalendarDayTapped(dayViewModel);

                    dayViewModel.Selected.Should().BeTrue();
                }

                [Theory, LogIfTooSlow]
                [InlineData(11, 0)]
                public async Task MarksTheFirstTappedCellAsStartOfSelection(
                    int monthIndex, int dayIndex)
                {
                    var dayViewModel = await FindDayViewModel(monthIndex, dayIndex);

                    ViewModel.CalendarDayTapped(dayViewModel);

                    dayViewModel.IsStartOfSelectedPeriod.Should().BeTrue();
                }

                [Theory, LogIfTooSlow]
                [InlineData(3, 20)]
                public async Task MarksTheFirstTappedCellAsEndOfSelection(
                    int monthIndex, int dayIndex)
                {
                    var dayViewModel = await FindDayViewModel(monthIndex, dayIndex);

                    ViewModel.CalendarDayTapped(dayViewModel);

                    dayViewModel.IsEndOfSelectedPeriod.Should().BeTrue();
                }
            }

            public sealed class AfterTappingTwoCells : TheCalendarDayTappedCommand
            {
                [Theory, LogIfTooSlow]
                [InlineData(0, 0, 5, 8)]
                public async Task MarksTheFirstTappedCellAsNotEndOfSelection(
                    int firstMonthIndex,
                    int firstDayindex,
                    int secondMonthIndex,
                    int secondDayIndex)
                {
                    var firstDayViewModel = await FindDayViewModel(firstMonthIndex, firstDayindex);
                    var secondDayViewModel = await FindDayViewModel(secondMonthIndex, secondDayIndex);

                    ViewModel.CalendarDayTapped(firstDayViewModel);
                    ViewModel.CalendarDayTapped(secondDayViewModel);

                    firstDayViewModel.IsEndOfSelectedPeriod.Should().BeFalse();
                }

                [Theory, LogIfTooSlow]
                [InlineData(1, 1, 9, 9)]
                public async Task MarksTheSecondTappedCellAsEndOfSelection(
                    int firstMonthIndex,
                    int firstDayindex,
                    int secondMonthIndex,
                    int secondDayIndex)
                {
                    var firstDayViewModel = await FindDayViewModel(firstMonthIndex, firstDayindex);
                    var secondDayViewModel = await FindDayViewModel(secondMonthIndex, secondDayIndex);

                    ViewModel.CalendarDayTapped(firstDayViewModel);
                    ViewModel.CalendarDayTapped(secondDayViewModel);

                    secondDayViewModel.IsEndOfSelectedPeriod.Should().BeTrue();
                }

                [Theory, LogIfTooSlow]
                [InlineData(1, 2, 3, 4)]
                public async Task MarksTheSecondTappedCellAsNotStartOfSelection(
                    int firstMonthIndex,
                    int firstDayindex,
                    int secondMonthIndex,
                    int secondDayIndex)
                {
                    var firstDayViewModel = await FindDayViewModel(firstMonthIndex, firstDayindex);
                    var secondDayViewModel = await FindDayViewModel(secondMonthIndex, secondDayIndex);

                    ViewModel.CalendarDayTapped(firstDayViewModel);
                    ViewModel.CalendarDayTapped(secondDayViewModel);

                    secondDayViewModel.IsStartOfSelectedPeriod.Should().BeFalse();
                }

                [Theory, LogIfTooSlow]
                [InlineData(2, 15, 7, 20)]
                public async Task MarksTheWholeIntervalAsSelected(
                    int firstMonthIndex,
                    int firstDayindex,
                    int secondMonthIndex,
                    int secondDayIndex)
                {
                    var firstDayViewModel = await FindDayViewModel(firstMonthIndex, firstDayindex);
                    var secondDayViewModel = await FindDayViewModel(secondMonthIndex, secondDayIndex);

                    ViewModel.CalendarDayTapped(firstDayViewModel);
                    ViewModel.CalendarDayTapped(secondDayViewModel);

                    var months = await ViewModel.Months;

                    for (int monthIndex = firstMonthIndex; monthIndex <= secondMonthIndex; monthIndex++)
                    {
                        var month = months[monthIndex];
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
                    CalendarPageViewModel calendarPage, int startindex, int endIndex)
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

                foreach (var shortcut in viewModel.QuickSelectShortcuts.GetAwaiter().GetResult())
                {
                    Action usingShortcut = () => viewModel.QuickSelect(shortcut);
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
                var selectedRange = DateRangeParameter.WithDates(start, end).WithSource(ReportsSource.Calendar);
                var customShortcut = Substitute.ForPartsOf<QuickSelectShortcut>();
                customShortcut.DateRange.Returns(selectedRange);

                // in this property test it is not possible to use the default ViewModel,
                // because we have to reset it in each iteration of the test
                var viewModel = CreateViewModel();
                viewModel.Prepare();
                viewModel.Initialize().Wait();

                Action usingShortcut = () => viewModel.QuickSelect(customShortcut);

                usingShortcut.Should().NotThrow();
            }
        }
    }
}
