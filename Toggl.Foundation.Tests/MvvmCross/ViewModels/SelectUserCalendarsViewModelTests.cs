using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.MvvmCross.ViewModels.Selectable;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac;
using Xunit;
using static Toggl.Multivac.Extensions.FunctionalExtensions;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectUserCalendarsViewModelTests
    {
        public abstract class SelectUserCalendarsViewModelTest : BaseViewModelTests<SelectUserCalendarsViewModel>
        {
            protected SelectUserCalendarsViewModelTest()
            {
                UserPreferences.EnabledCalendarIds().Returns(new List<string>());
            }

            protected override SelectUserCalendarsViewModel CreateViewModel()
                => new SelectUserCalendarsViewModel(UserPreferences, InteractorFactory, NavigationService, RxActionFactory);
        }

        public sealed class TheConstructor : SelectUserCalendarsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useUserPreferences,
                bool useInteractorFactory,
                bool useNavigationService,
                bool useRxActionFactory)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new SelectUserCalendarsViewModel(
                        useUserPreferences ? UserPreferences : null,
                        useInteractorFactory ? InteractorFactory : null,
                        useNavigationService ? NavigationService : null,
                        useRxActionFactory ? RxActionFactory : null
                    );

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheDoneAction : SelectUserCalendarsViewModelTest
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

                var selectCalendars = userCalendars
                    .Where(calendar => selectedIds.Contains(calendar.Id))
                    .Select(calendar => new SelectableUserCalendarViewModel(calendar, false))
                    .Select(ViewModel.SelectCalendar.DeferredExecute)
                    .Aggregate(Observable.Concat);

                RxActionHelper.RunSequentially(
                    selectCalendars,
                    ViewModel.Done.DeferredExecute()
                );
                TestScheduler.Start();

                await NavigationService.Received().Close(ViewModel, Arg.Is<string[]>(ids => ids.SequenceEqual(selectedIds)));
            }
        }

        public abstract class TheDoneActionEnabledProperty
        {
            public class WhenYouDoNotForceItemSelection : SelectUserCalendarsViewModelTest
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

            public class WhenYouForceItemSelection : SelectUserCalendarsViewModelTest
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
                    RxActionHelper.RunSequentially(
                        selectedableUserCalendars
                            .Select(calendar => Observable.Defer(() => ViewModel.SelectCalendar.Execute(calendar)))
                            .ToArray()
                    );
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
                    var selectedableUserCalendars = Enumerable
                        .Range(0, 10)
                        .Select(id =>
                        {
                            var userCalendar = new UserCalendar(id.ToString(), id.ToString(), "Doenst matter");
                            return new SelectableUserCalendarViewModel(userCalendar, false);
                        });

                    var selectAll = selectedableUserCalendars
                        .Select(calendar => ViewModel.SelectCalendar.DeferredExecute(calendar))
                        .Aggregate(Observable.Concat);

                    RxActionHelper.RunSequentially(selectAll, selectAll);
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
    }
}
