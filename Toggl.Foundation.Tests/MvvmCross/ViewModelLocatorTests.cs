using FluentAssertions;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using NSubstitute;
using System;
using System.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross
{
    public class ViewModelLocatorTests : BaseMvvmCrossTests
    {
        [Fact(Skip = "Fixing this while MvvmCross is still there is too complex"), LogIfTooSlow]
        public void IsAbleToCreateEveryViewModel()
        {
            var locator = new TogglViewModelLocator(new TestDependencyContainer
            {
                MockUserAccessManager = Substitute.For<IUserAccessManager>(),
                MockAccessRestrictionStorage = Substitute.For<IAccessRestrictionStorage>(),
                MockAnalyticsService = Substitute.For<IAnalyticsService>(),
                MockBackgroundSyncService = Substitute.For<IBackgroundSyncService>(),
                MockBrowserService = Substitute.For<IBrowserService>(),
                MockCalendarService = Substitute.For<ICalendarService>(),
                MockDatabase = Substitute.For<ITogglDatabase>(),
                MockDialogService = Substitute.For<IDialogService>(),
                MockGoogleService = Substitute.For<IGoogleService>(),
                MockIntentDonationService = Substitute.For<IIntentDonationService>(),
                MockKeyValueStorage = Substitute.For<IKeyValueStorage>(),
                MockLastTimeUsageStorage = Substitute.For<ILastTimeUsageStorage>(),
                MockLicenseProvider = Substitute.For<ILicenseProvider>(),
                MockNavigationService = Substitute.For<IMvxNavigationService>(),
                MockNotificationService = Substitute.For<INotificationService>(),
                MockOnboardingStorage = Substitute.For<IOnboardingStorage>(),
                MockPasswordManagerService = Substitute.For<IPasswordManagerService>(),
                MockPermissionsService = Substitute.For<IPermissionsService>(),
                MockPlatformInfo = Substitute.For<IPlatformInfo>(),
                MockPrivateSharedStorageService = Substitute.For<IPrivateSharedStorageService>(),
                MockRatingService = Substitute.For<IRatingService>(),
                MockRemoteConfigService = Substitute.For<IRemoteConfigService>(),
                MockSchedulerProvider = Substitute.For<ISchedulerProvider>(),
                MockShortcutCreator = Substitute.For<IApplicationShortcutCreator>(),
                MockStopwatchProvider = Substitute.For<IStopwatchProvider>(),
                MockSuggestionProviderContainer = Substitute.For<ISuggestionProviderContainer>(),
                MockUserPreferences = Substitute.For<IUserPreferences>(),
                MockInteractorFactory = Substitute.For<IInteractorFactory>(),
                MockTimeService = Substitute.For<ITimeService>(),
                MockSyncManager = Substitute.For<ISyncManager>(),
            });
            
            var viewModelTypes = typeof(MainViewModel).Assembly
                .GetTypes()
                .Where(isViewModel);

            foreach (var viewModelType in viewModelTypes)
            {
                Action tryingToFindAViewModel = () => locator.Load(viewModelType, null, null);
                tryingToFindAViewModel.Should().NotThrow();
            }

            bool isViewModel(Type type)
                => type.ImplementsOrDerivesFrom<IMvxViewModel>();
        }

    }
}
