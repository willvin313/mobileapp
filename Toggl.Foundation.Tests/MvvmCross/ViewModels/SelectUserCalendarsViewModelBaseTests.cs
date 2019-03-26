using System.Collections.Generic;
using Toggl.Foundation.Exceptions;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Xunit;
using static Toggl.Multivac.Extensions.FunctionalExtensions;
using Toggl.PrimeRadiant.Settings;
using MvvmCross.Navigation;
using Toggl.Foundation.MvvmCross.ViewModels.Selectable;
using System.Reactive;
using System;
using Toggl.Foundation.Tests.TestExtensions;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectUserCalendarsViewModelBaseTests
    {
        public sealed class MockSelectUserCalendarsViewModel : SelectUserCalendarsViewModelBase
        {
            public MockSelectUserCalendarsViewModel(
                IUserPreferences userPreferences,
                IInteractorFactory interactorFactory,
                IMvxNavigationService navigationService,
                IRxActionFactory rxActionFactory)
                : base(userPreferences, interactorFactory, navigationService, rxActionFactory)
            {
            }
        }

        public abstract class SelectUserCalendarsViewModelBaseTest : BaseViewModelTests<MockSelectUserCalendarsViewModel>
        {
            protected SelectUserCalendarsViewModelBaseTest()
            {
                UserPreferences.EnabledCalendarIds().Returns(new List<string>());
            }

            protected override MockSelectUserCalendarsViewModel CreateViewModel()
                => new MockSelectUserCalendarsViewModel(UserPreferences, InteractorFactory, NavigationService, RxActionFactory);
        }

        public sealed class TheConstructor : SelectUserCalendarsViewModelBaseTest
        {
            [Fact, LogIfTooSlow]
            public async Task FillsTheCalendarList()
            {
                var userCalendarsObservable = Enumerable
                    .Range(0, 9)
                    .Select(id => new UserCalendar(
                        id.ToString(),
                        $"Calendar #{id}",
                        $"Source #{id % 3}",
                        false))
                    .Apply(Observable.Return);
                InteractorFactory.GetUserCalendars().Execute().Returns(userCalendarsObservable);

                var viewModel = new MockSelectUserCalendarsViewModel(UserPreferences, InteractorFactory, NavigationService, RxActionFactory);

                await viewModel.Initialize();
                var calendars = await viewModel.Calendars.FirstAsync();

                calendars.Should().HaveCount(3);
                calendars.ForEach(group => group.Items.Should().HaveCount(3));
            }
        }

        public sealed class TheInitializeMethod : SelectUserCalendarsViewModelBaseTest
        {
            [Fact, LogIfTooSlow]
            public async Task HandlesNotAuthorizedException()
            {
                InteractorFactory
                    .GetUserCalendars()
                    .Execute()
                    .Returns(Observable.Throw<IEnumerable<UserCalendar>>(new NotAuthorizedException("")));

                await ViewModel.Initialize();
                var calendars = await ViewModel.Calendars.FirstAsync();

                calendars.Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public async Task MarksAllCalendarsAsNotSelected()
            {
                var userCalendarsObservable = Enumerable
                    .Range(0, 9)
                    .Select(id => new UserCalendar(
                        id.ToString(),
                        $"Calendar #{id}",
                        $"Source #{id % 3}",
                        false))
                    .Apply(Observable.Return);
                InteractorFactory.GetUserCalendars().Execute().Returns(userCalendarsObservable);

                await ViewModel.Initialize();
                var calendars = await ViewModel.Calendars.FirstAsync();

                foreach (var calendarGroup in calendars)
                {
                    calendarGroup.Items.None(calendar => calendar.Selected).Should().BeTrue();
                }
            }
        }

        public sealed class TheCloseAction : SelectUserCalendarsViewModelBaseTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModelAndReturnsTheInitialCalendarIds()
            {
                var initialSelectedIds = new List<string> { "0", "1", "2", "3" };
                UserPreferences.EnabledCalendarIds().Returns(initialSelectedIds);

                var userCalendars = Enumerable
                    .Range(0, 9)
                    .Select(id => new UserCalendar(
                        id.ToString(),
                        $"Calendar #{id}",
                        $"Source #{id % 3}",
                        false));

                InteractorFactory
                    .GetUserCalendars()
                    .Execute()
                    .Returns(Observable.Return(userCalendars));
                await ViewModel.Initialize();
                var selectedIds = new[] { "0", "2", "4", "7" };

                var calendars = userCalendars
                    .Where(calendar => selectedIds.Contains(calendar.Id))
                    .Select(calendar => new SelectableUserCalendarViewModel(calendar, false));

                ViewModel.SelectCalendar.ExecuteSequentally(calendars)
                    .PrependAction(ViewModel.Close)
                    .Subscribe();

                TestScheduler.Start();

                await NavigationService.Received().Close(ViewModel, Arg.Is<string[]>(ids => ids.SequenceEqual(initialSelectedIds)));
            }
        }

        public sealed class TheDoneAction : SelectUserCalendarsViewModelBaseTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModelAndReturnsSelectedCalendarIds()
            {
                var userCalendars = Enumerable
                    .Range(0, 9)
                    .Select(id => new UserCalendar(
                        id.ToString(),
                        $"Calendar #{id}",
                        $"Source #{id % 3}",
                        false));

                InteractorFactory
                    .GetUserCalendars()
                    .Execute()
                    .Returns(Observable.Return(userCalendars));

                await ViewModel.Initialize();
                var selectedIds = new[] { "0", "2", "4", "7" };

                var calendars = userCalendars
                    .Where(calendar => selectedIds.Contains(calendar.Id))
                    .Select(calendar => new SelectableUserCalendarViewModel(calendar, false));

                ViewModel.SelectCalendar.ExecuteSequentally(calendars)
                    .PrependAction(ViewModel.Done)
                    .Subscribe();

                TestScheduler.Start();

                await NavigationService.Received().Close(ViewModel, Arg.Is<string[]>(ids => ids.SequenceEqual(selectedIds)));
            }
        }

        public abstract class TheDoneActionEnabledProperty
        {
            public class WhenYouDoNotForceItemSelection : SelectUserCalendarsViewModelBaseTest
            {
                public WhenYouDoNotForceItemSelection()
                {
                    ViewModel.Prepare(false);
                    ViewModel.Initialize().Wait();
                }

                [Fact, LogIfTooSlow]
                public void ReturnsTrue()
                {
                    var observer = Substitute.For<IObserver<bool>>();

                    ViewModel.Done.Enabled.Subscribe(observer);
                    SchedulerProvider.TestScheduler.AdvanceBy(1);

                    observer.Received().OnNext(true);
                }
            }

            public class WhenYouForceItemSelection : SelectUserCalendarsViewModelBaseTest
            {
                public WhenYouForceItemSelection()
                {
                    ViewModel.Prepare(true);
                    ViewModel.Initialize().Wait();
                }

                [Fact, LogIfTooSlow]
                public void StartsWithFalse()
                {
                    var observer = Substitute.For<IObserver<bool>>();

                    ViewModel.Done.Enabled.Subscribe(observer);
                    SchedulerProvider.TestScheduler.AdvanceBy(1);

                    observer.Received().OnNext(false);
                }

                [Fact, LogIfTooSlow]
                public async Task EmitsTrueAfterOneCalendarHasBeenSelected()
                {
                    var observer = Substitute.For<IObserver<bool>>();
                    ViewModel.Done.Enabled.Subscribe(observer);
                    var selectableUserCalendar = new SelectableUserCalendarViewModel(
                        new UserCalendar(),
                        false
                    );

                    ViewModel.SelectCalendar.Execute(selectableUserCalendar);
                    TestScheduler.Start();

                    Received.InOrder(() =>
                    {
                        observer.OnNext(false);
                        observer.OnNext(true);
                    });
                }

                [Fact, LogIfTooSlow]
                public void DoesNotEmitAnythingWhenSelectingAdditionalCalendars()
                {
                    var observer = Substitute.For<IObserver<bool>>();
                    ViewModel.Done.Enabled.Subscribe(observer);
                    var selectedableUserCalendars = Enumerable
                        .Range(0, 10)
                        .Select(id =>
                        {
                            var userCalendar = new UserCalendar(id.ToString(), id.ToString(), "Doenst matter");
                            return new SelectableUserCalendarViewModel(userCalendar, false);
                        });

                    var auxObserver = TestScheduler.CreateObserver<Unit>();
                    ViewModel.SelectCalendar.ExecuteSequentally(selectedableUserCalendars)
                        .Subscribe(auxObserver);

                    TestScheduler.Start();

                    Received.InOrder(() =>
                    {
                        observer.OnNext(false);
                        observer.OnNext(true);
                    });
                }

                [Fact, LogIfTooSlow]
                public void EmitsFalseAfterAllTheCalendarsHaveBeenDeselected()
                {
                    var observer = Substitute.For<IObserver<bool>>();
                    ViewModel.Done.Enabled.Subscribe(observer);

                    var userCalendars = Enumerable
                        .Range(0, 3)
                        .Select(id => new UserCalendar(id.ToString(), id.ToString(), "Doesn't matter"));

                    var selectedableUserCalendars = userCalendars
                        .Select(userCalendar => new SelectableUserCalendarViewModel(userCalendar, false));

                    InteractorFactory
                        .GetUserCalendars()
                        .Execute()
                        .Returns(Observable.Return(userCalendars));


                    var auxObserver = TestScheduler.CreateObserver<Unit>();
                    ViewModel.SelectCalendar.ExecuteSequentally(
                            selectedableUserCalendars
                                .Concat(selectedableUserCalendars)
                        )
                        .Subscribe(auxObserver);

                    TestScheduler.Start();

                    Received.InOrder(() =>
                    {
                        observer.OnNext(false);
                        observer.OnNext(true);
                        observer.OnNext(false);
                    });
                }
            }
        }

        public sealed class TheSelectCalendarAction : SelectUserCalendarsViewModelBaseTest
        {
            [Fact, LogIfTooSlow]
            public async Task MarksTheCalendarAsSelectedIfItIsNotSelected()
            {
                var userCalendars = Enumerable
                    .Range(0, 9)
                    .Select(id => new UserCalendar(
                        id.ToString(),
                        $"Calendar #{id}",
                        $"Source #{id % 3}",
                        false));
                var userCalendarsObservable = Observable.Return(userCalendars);
                InteractorFactory.GetUserCalendars().Execute().Returns(userCalendarsObservable);
                await ViewModel.Initialize();
                var viewModelCalendars = await ViewModel.Calendars.FirstAsync();
                var calendarToBeSelected = viewModelCalendars.First().Items.First();

                ViewModel.SelectCalendar.Execute(calendarToBeSelected);
                TestScheduler.Start();

                calendarToBeSelected.Selected.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task MarksTheCalendarAsNotSelectedIfItIsSelected()
            {
                var userCalendars = Enumerable
                    .Range(0, 9)
                    .Select(id => new UserCalendar(
                        id.ToString(),
                        $"Calendar #{id}",
                        $"Source #{id % 3}",
                        true));
                var userCalendarsObservable = Observable.Return(userCalendars);
                InteractorFactory.GetUserCalendars().Execute().Returns(userCalendarsObservable);
                await ViewModel.Initialize();
                var viewModelCalendars = await ViewModel.Calendars.FirstAsync();
                var calendarToBeSelected = viewModelCalendars.First().Items.First();

                ViewModel.SelectCalendar.Execute(calendarToBeSelected); //Select the calendar
                TestScheduler.Start();
                ViewModel.SelectCalendar.Execute(calendarToBeSelected); //Deselect the calendar
                TestScheduler.Start();

                calendarToBeSelected.Selected.Should().BeFalse();
            }
        }
    }
}
