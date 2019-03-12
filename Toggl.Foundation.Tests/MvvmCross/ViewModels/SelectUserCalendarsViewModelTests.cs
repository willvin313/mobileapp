using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.Tests.Generators;
using Xunit;

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

    }
}
