using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.ViewModels;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Login;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross
{
    public sealed class AppStartTests
    {
        public abstract class AppStartTest : BaseMvvmCrossTests
        {
            protected AppStart<OnboardingViewModel> AppStart { get; }
            protected IMvxApplication App { get; } = Substitute.For<IMvxApplication>();
            protected IUserAccessManager UserAccessManager { get; } = Substitute.For<IUserAccessManager>();
            protected IOnboardingStorage OnboardingStorage { get; } = Substitute.For<IOnboardingStorage>();
            protected IAccessRestrictionStorage AccessRestrictionStorage { get; } =
                Substitute.For<IAccessRestrictionStorage>();

            protected AppStartTest()
            {
                var api = Substitute.For<ITogglApi>();
                UserAccessManager.UserLoggedIn.Returns(Observable.Return(api));

                var dependencyContainer = new TestDependencyContainer
                {
                    MockTimeService = TimeService,
                    MockUserAccessManager = UserAccessManager,
                    MockNavigationService = NavigationService,
                    MockOnboardingStorage = OnboardingStorage,
                    MockAccessRestrictionStorage = AccessRestrictionStorage,
                    MockSyncManager = Substitute.For<ISyncManager>(),
                    MockInteractorFactory = Substitute.For<IInteractorFactory>(),
                    MockBackgroundSyncService = Substitute.For<IBackgroundSyncService>()
                };
                UserAccessManager.CheckIfLoggedIn().Returns(true);

                AppStart = new AppStart<OnboardingViewModel>(App, dependencyContainer);
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
                UserAccessManager.DidNotReceive().CheckIfLoggedIn();
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheOutdatedViewIfTheVersionOfTheCurrentlyUsedApiIsOutdated()
            {
                AccessRestrictionStorage.IsApiOutdated().Returns(true);

                AppStart.Start();

                await NavigationService.Received().Navigate<OutdatedAppViewModel>();
                UserAccessManager.DidNotReceive().CheckIfLoggedIn();
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheReLoginViewIfTheUserRevokedTheApiToken()
            {
                AccessRestrictionStorage.IsUnauthorized(Arg.Any<string>()).Returns(true);

                AppStart.Start();

                await NavigationService.Received().Navigate<TokenResetViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheOutdatedViewIfTheTokenWasRevokedAndTheAppIsOutdated()
            {
                AccessRestrictionStorage.IsUnauthorized(Arg.Any<string>()).Returns(true);
                AccessRestrictionStorage.IsClientOutdated().Returns(true);

                AppStart.Start();

                await NavigationService.Received().Navigate<OutdatedAppViewModel>();
                UserAccessManager.DidNotReceive().CheckIfLoggedIn();
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheOutdatedViewIfTheTokenWasRevokedAndTheApiIsOutdated()
            {
                AccessRestrictionStorage.IsUnauthorized(Arg.Any<string>()).Returns(true);
                AccessRestrictionStorage.IsApiOutdated().Returns(true);

                AppStart.Start();

                await NavigationService.Received().Navigate<OutdatedAppViewModel>();
                await NavigationService.DidNotReceive().Navigate<TokenResetViewModel>();
                UserAccessManager.DidNotReceive().CheckIfLoggedIn();
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
                UserAccessManager.CheckIfLoggedIn().Returns(false);

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

            [Fact, LogIfTooSlow]
            public void MarksTheUserAsNotNewWhenUsingTheAppForTheFirstTimeAfterSixtyDays()
            {
                var now = DateTimeOffset.Now;

                TimeService.CurrentDateTime.Returns(now);
                OnboardingStorage.GetLastOpened().Returns(now.AddDays(-60));

                AppStart.Start();

                OnboardingStorage.Received().SetLastOpened(now);
                OnboardingStorage.Received().SetIsNewUser(false);
            }
        }
    }
}
