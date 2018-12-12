using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class CalendarPermissionDeniedViewModelTests
    {
        public abstract class CalendarPermissionDeniedViewModelTest
            : BaseViewModelTests<CalendarPermissionDeniedViewModel>
        {
            protected override CalendarPermissionDeniedViewModel CreateViewModel()
                => new CalendarPermissionDeniedViewModel(PermissionsService);
        }

        public sealed class TheConstructor : CalendarPermissionDeniedViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfAnyOfTheArgumentsIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarPermissionDeniedViewModel(null);

                tryingToConstructWithEmptyParameters
                    .Should()
                    .Throw<ArgumentNullException>();
            }
        }

        public sealed class TheEnableAccessAction : CalendarPermissionDeniedViewModelTest
        {
            [Fact]
            public async Task OpensAppSettings()
            {
                await ViewModel.EnableAccess.Execute(Unit.Default);

                PermissionsService.Received().OpenAppSettings();
            }
        }
    }
}
