using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels.Selectable;
using Toggl.Foundation.MvvmCross.ViewModels.Settings;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class CalendarSettingsViewModelTests
    {
        public abstract class CalendarSettingsViewModelTest : BaseViewModelTests<CalendarSettingsViewModel>
        {
            protected override CalendarSettingsViewModel CreateViewModel()
                => new CalendarSettingsViewModel(UserPreferences, InteractorFactory, NavigationService, RxActionFactory, PermissionsService);
        }

        public sealed class TheConstructor : CalendarSettingsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useUserPreferences,
                bool useInteractorFactory,
                bool useNavigationService,
                bool useRxActionFactory,
                bool usePermissionsService)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarSettingsViewModel(
                        useUserPreferences ? UserPreferences : null,
                        useInteractorFactory ? InteractorFactory : null,
                        useNavigationService ? NavigationService : null,
                        useRxActionFactory ? RxActionFactory : null,
                        usePermissionsService ? PermissionsService : null
                    );

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePermissionGrantedProperty : CalendarSettingsViewModelTest
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task GetsInitialisedToTheProperValue(bool permissionGranted)
            {
                UserPreferences.EnabledCalendarIds().Returns(new List<string>());
                PermissionsService.CalendarPermissionGranted.Returns(Observable.Return(permissionGranted));

                await ViewModel.Initialize();

                ViewModel.PermissionGranted.Should().Be(permissionGranted);
            }
        }

        public sealed class TheRequestAccessAction : CalendarSettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task OpensAppSettings()
            {
                ViewModel.RequestAccess.Execute();

                PermissionsService.Received().OpenAppSettings();
            }
        }

        public sealed class TheInitializeMethod : CalendarSettingsViewModelTest
        {
            [Property]
            public void SetsProperCalendarsAsSelected(
                NonEmptySet<NonEmptyString> strings0,
                NonEmptySet<NonEmptyString> strings1)
            {
                var enabledCalendarIds = strings0.Get.Select(str => str.Get).ToList();
                var unenabledCalendarIds = strings1.Get.Select(str => str.Get).ToList();
                var allCalendarIds = enabledCalendarIds.Concat(unenabledCalendarIds).ToList();
                UserPreferences.EnabledCalendarIds().Returns(enabledCalendarIds);
                var userCalendars = allCalendarIds
                    .Select(id => new UserCalendar(
                        id,
                        "Does not matter",
                        "Does not matter, pt.2"
                    ));
                InteractorFactory
                    .GetUserCalendars()
                    .Execute()
                    .Returns(Observable.Return(userCalendars));
                var viewModel = CreateViewModel();

                viewModel.Initialize().Wait();

                var calendars = viewModel.Calendars.FirstAsync().Wait();
                foreach (var calendarGroup in calendars)
                {
                    foreach (var calendar in calendarGroup.Items)
                    {
                        if (enabledCalendarIds.Contains(calendar.Id))
                            calendar.Selected.Should().BeTrue();
                    }
                }
            }

            [Fact]
            public void SetsTheEnabledCalendarsToNullWhenCalendarPermissionsWereNotGranted()
            {
                PermissionsService.CalendarPermissionGranted.Returns(Observable.Return(false));
                UserPreferences.EnabledCalendarIds().Returns(new List<string>());

                var viewModel = CreateViewModel();

                viewModel.Initialize().Wait();

                UserPreferences.Received().SetEnabledCalendars(Arg.Is<string[]>(strings => strings == null || strings.Length == 0));
            }

            [Fact]
            public void DoesNotSetTheEnabledCalendarsToNullWhenCalendarPermissionsWereGranted()
            {
                PermissionsService.CalendarPermissionGranted.Returns(Observable.Return(true));
                UserPreferences.EnabledCalendarIds().Returns(new List<string>());

                var viewModel = CreateViewModel();

                viewModel.Initialize().Wait();

                UserPreferences.DidNotReceive().SetEnabledCalendars(Arg.Is<string[]>(strings => strings == null || strings.Length == 0));
            }
        }

        public sealed class TheSelectCalendarAction : CalendarSettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task StoresTheEnabledCalendarsInUserPreferences()
            {
                var firstCalendar = new UserCalendar("1", "1", "1");
                var secondCalendar = new UserCalendar("2", "2", "2");

                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.SelectCalendar.ExecuteSequentally(
                        new SelectableUserCalendarViewModel(firstCalendar, false),
                        new SelectableUserCalendarViewModel(secondCalendar, false)
                    )
                    .Subscribe(observer);


                TestScheduler.Start();

                Received.InOrder(() =>
                {
                    UserPreferences.SetEnabledCalendars(new string[] { "1" });
                    UserPreferences.SetEnabledCalendars(new string[] { "1", "2" });
                });
            }
        }
    }
}
