using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MvvmCross.ViewModels;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Login;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Settings;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross
{
    public sealed class AppStartTests
    {
        public abstract class AppStartTest : BaseMvvmCrossTests
        {
            protected AppStart<OnboardingViewModel> AppStart { get; }
            protected IMvxApplication App { get; } = Substitute.For<IMvxApplication>();
            protected ISyncManager SyncManager { get; } = Substitute.For<ISyncManager>();
            protected IUserAccessManager UserAccessManager { get; } = Substitute.For<IUserAccessManager>();
            protected IOnboardingStorage OnboardingStorage { get; } = Substitute.For<IOnboardingStorage>();
            protected IAccessRestrictionStorage AccessRestrictionStorage { get; } =
                Substitute.For<IAccessRestrictionStorage>();

            protected AppStartTest()
            {
                AppStart = new AppStart<OnboardingViewModel>(App, TimeService, UserAccessManager, OnboardingStorage, NavigationService, AccessRestrictionStorage);
                UserAccessManager.TryInitializingAccessToUserData(out _, out _).Returns(x =>
                {
                    x[0] = SyncManager;
                    x[1] = InteractorFactory;
                    return true;
                });
            }
        }

        public sealed class TheConstructor : AppStartTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useTimeService,
                bool useUserAccessManager,
                bool useOnboardingStorage,
                bool userNavigationService,
                bool useAccessRestrictionStorage)
            {
                var timeService = useTimeService ? TimeService : null;
                var userAccessManager = useUserAccessManager ? UserAccessManager : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = userNavigationService ? NavigationService : null;
                var accessRestrictionStorage = useAccessRestrictionStorage ? AccessRestrictionStorage : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new AppStart<OnboardingViewModel>(
                        App,
                        timeService,
                        userAccessManager,
                        onboardingStorage,
                        navigationService,
                        accessRestrictionStorage);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheStartMethod : AppStartTest
        {
            [Fact, LogIfTooSlow]
            public async Task ShowsTheOutdatedViewIfTheCurrentVersionOfTheAppIsOutdated()
            {
                AccessRestrictionStorage.IsClientOutdated().Returns(true);

                AppStart.Start();

                await NavigationService.Received().Navigate<OutdatedAppViewModel>();
                UserAccessManager.DidNotReceive().TryInitializingAccessToUserData(out _, out _);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheOutdatedViewIfTheVersionOfTheCurrentlyUsedApiIsOutdated()
            {
                AccessRestrictionStorage.IsApiOutdated().Returns(true);

                AppStart.Start();

                await NavigationService.Received().Navigate<OutdatedAppViewModel>();
                UserAccessManager.DidNotReceive().TryInitializingAccessToUserData(out _, out _);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheReLoginViewIfTheUserRevokedTheApiToken()
            {
                AccessRestrictionStorage.IsUnauthorized(Arg.Any<string>()).Returns(true);

                AppStart.Start();

                await SyncManager.DidNotReceive().ForceFullSync();
                await NavigationService.Received().Navigate<TokenResetViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheOutdatedViewIfTheTokenWasRevokedAndTheAppIsOutdated()
            {
                AccessRestrictionStorage.IsUnauthorized(Arg.Any<string>()).Returns(true);
                AccessRestrictionStorage.IsClientOutdated().Returns(true);

                AppStart.Start();

                await NavigationService.Received().Navigate<OutdatedAppViewModel>();
                UserAccessManager.DidNotReceive().TryInitializingAccessToUserData(out _, out _);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheOutdatedViewIfTheTokenWasRevokedAndTheApiIsOutdated()
            {
                AccessRestrictionStorage.IsUnauthorized(Arg.Any<string>()).Returns(true);
                AccessRestrictionStorage.IsApiOutdated().Returns(true);

                AppStart.Start();

                await NavigationService.Received().Navigate<OutdatedAppViewModel>();
                await NavigationService.DidNotReceive().Navigate<TokenResetViewModel>();
                UserAccessManager.DidNotReceive().TryInitializingAccessToUserData(out _, out _);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotShowTheUnauthorizedAccessViewIfUsersApiTokenChanged()
            {
                var oldApiToken = Guid.NewGuid().ToString();
                var newApiToken = Guid.NewGuid().ToString();
                var user = Substitute.For<IThreadSafeUser>();
                var interactorFactory = Substitute.For<IInteractorFactory>();
                user.ApiToken.Returns(newApiToken);
                interactorFactory.GetCurrentUser().Execute().Returns(Observable.Return(user));
                AccessRestrictionStorage.IsUnauthorized(Arg.Is(oldApiToken)).Returns(true);
                AccessRestrictionStorage.IsApiOutdated().Returns(false);
                AccessRestrictionStorage.IsClientOutdated().Returns(false);

                AppStart.Start();

                await NavigationService.Received().Navigate<MainTabBarViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheOnboardingViewModelIfTheUserHasNotLoggedInPreviously()
            {
                UserAccessManager.TryInitializingAccessToUserData(out _, out _).Returns(false);

                AppStart.Start();

                await NavigationService.Received().Navigate<OnboardingViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsNavigateToMainTabBarViewModelIfTheUserHasLoggedInPreviously()
            {
                AppStart.Start();

                await NavigationService.Received().Navigate<MainTabBarViewModel>();
            }

            [Fact, LogIfTooSlow]
            public void SetsFirstOpenedTime()
            {
                TimeService.CurrentDateTime.Returns(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero));

                AppStart.Start();

                OnboardingStorage.Received().SetFirstOpened(TimeService.CurrentDateTime);
            }
        }
    }
}
