using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class CalendarPermissionDeniedViewModelTests
    {
        public abstract class CalendarPermissionDeniedViewModelTest
            : BaseViewModelTests<CalendarPermissionDeniedViewModel>
        {
            protected override CalendarPermissionDeniedViewModel CreateViewModel()
                => new CalendarPermissionDeniedViewModel(NavigationService, PermissionsService, RxActionFactory);
        }

        public sealed class TheConstructor : CalendarPermissionDeniedViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService,
                bool usePermissionsService,
                bool useRxActionFactory)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarPermissionDeniedViewModel(
                        useNavigationService ? NavigationService : null,
                        usePermissionsService ? PermissionsService : null,
                        useRxActionFactory ? RxActionFactory : null
                    );

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheEnableAccessAction : CalendarPermissionDeniedViewModelTest
        {
            [Fact]
            public async Task OpensAppSettings()
            {
                ViewModel.EnableAccess.Execute();

                PermissionsService.Received().OpenAppSettings();
            }
        }

        public sealed class TheViewAppearedMethod : CalendarPermissionDeniedViewModelTest
        {
            [Fact]
            public async Task ClosesWhenPermissionWasGranted()
            {
                PermissionsService.CalendarPermissionGranted.Returns(Observable.Return(true));
                ViewModel.ViewAppeared();

                TestScheduler.Start();

                await NavigationService.Received().Close(ViewModel, Unit.Default);
            }

            [Fact]
            public async Task DoesNothingWhenPermissionWasNotGranted()
            {
                PermissionsService.CalendarPermissionGranted.Returns(Observable.Return(false));
                ViewModel.ViewAppeared();

                TestScheduler.Start();

                await NavigationService.DidNotReceive().Close(Arg.Any<CalendarPermissionDeniedViewModel>(), Unit.Default);
            }
        }
    }
}
