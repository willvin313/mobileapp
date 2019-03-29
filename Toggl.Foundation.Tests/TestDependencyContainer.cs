using MvvmCross.Navigation;
using System;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.MvvmCross
{
    public class TestDependencyContainer : UIDependencyContainer
    {
        public static void Initialize(TestDependencyContainer container)
        {
            Instance = container;
        }

        public TestDependencyContainer()
            : base(ApiEnvironment.Staging, new UserAgent("Giskard", "999.99"))
        {
        }

        internal IUserAccessManager MockUserAccessManager { get; set; }
        public override IUserAccessManager UserAccessManager
            => MockUserAccessManager ?? base.UserAccessManager;

        internal IAccessRestrictionStorage MockAccessRestrictionStorage { get; set; }
        protected override IAccessRestrictionStorage CreateAccessRestrictionStorage()
            => MockAccessRestrictionStorage;

        internal IAnalyticsService MockAnalyticsService { get; set; }
        protected override IAnalyticsService CreateAnalyticsService()
            => MockAnalyticsService;

        internal IBackgroundSyncService MockBackgroundSyncService { get; set; }
        protected override IBackgroundSyncService CreateBackgroundSyncService()
            => MockBackgroundSyncService;

        internal IBrowserService MockBrowserService { get; set; }
        protected override IBrowserService CreateBrowserService()
            => MockBrowserService;

        internal ICalendarService MockCalendarService { get; set; }
        protected override ICalendarService CreateCalendarService()
            => MockCalendarService;

        internal ITogglDatabase MockDatabase { get; set; }
        protected override ITogglDatabase CreateDatabase()
            => MockDatabase;

        internal IDialogService MockDialogService { get; set; }
        protected override IDialogService CreateDialogService()
            => MockDialogService;

        internal IGoogleService MockGoogleService { get; set; }
        protected override IGoogleService CreateGoogleService()
            => MockGoogleService;

        internal IIntentDonationService MockIntentDonationService { get; set; }
        protected override IIntentDonationService CreateIntentDonationService()
            => MockIntentDonationService;

        internal IKeyValueStorage MockKeyValueStorage { get; set; }
        protected override IKeyValueStorage CreateKeyValueStorage()
            => MockKeyValueStorage;

        internal ILastTimeUsageStorage MockLastTimeUsageStorage { get; set; }
        protected override ILastTimeUsageStorage CreateLastTimeUsageStorage()
            => MockLastTimeUsageStorage;

        internal ILicenseProvider MockLicenseProvider { get; set; }
        protected override ILicenseProvider CreateLicenseProvider()
            => MockLicenseProvider;

        internal IMvxNavigationService MockNavigationService { get; set; }
        protected override IMvxNavigationService CreateNavigationService()
            => MockNavigationService;

        internal INotificationService MockNotificationService { get; set; }
        protected override INotificationService CreateNotificationService()
            => MockNotificationService;

        internal IOnboardingStorage MockOnboardingStorage { get; set; }
        protected override IOnboardingStorage CreateOnboardingStorage()
            => MockOnboardingStorage;

        internal IPasswordManagerService MockPasswordManagerService { get; set; }
        protected override IPasswordManagerService CreatePasswordManagerService()
            => MockPasswordManagerService;

        internal IPermissionsService MockPermissionsService { get; set; }
        protected override IPermissionsService CreatePermissionsService()
            => MockPermissionsService;

        internal IPlatformInfo MockPlatformInfo { get; set; }
        protected override IPlatformInfo CreatePlatformInfo()
            => MockPlatformInfo;

        internal IPrivateSharedStorageService MockPrivateSharedStorageService { get; set; }
        protected override IPrivateSharedStorageService CreatePrivateSharedStorageService()
            => MockPrivateSharedStorageService;

        internal IRatingService MockRatingService { get; set; }
        protected override IRatingService CreateRatingService()
            => MockRatingService;

        internal IRemoteConfigService MockRemoteConfigService { get; set; }
        protected override IRemoteConfigService CreateRemoteConfigService()
            => MockRemoteConfigService;

        internal ISchedulerProvider MockSchedulerProvider { get; set; }
        protected override ISchedulerProvider CreateSchedulerProvider()
            => MockSchedulerProvider;

        internal IApplicationShortcutCreator MockShortcutCreator { get; set; }
        protected override IApplicationShortcutCreator CreateShortcutCreator()
            => MockShortcutCreator;

        internal IStopwatchProvider MockStopwatchProvider { get; set; }
        protected override IStopwatchProvider CreateStopwatchProvider()
            => MockStopwatchProvider;

        internal ISuggestionProviderContainer MockSuggestionProviderContainer { get; set; }
        protected override ISuggestionProviderContainer CreateSuggestionProviderContainer()
            => MockSuggestionProviderContainer;

        internal IUserPreferences MockUserPreferences { get; set; }
        protected override IUserPreferences CreateUserPreferences()
            => MockUserPreferences;

        internal IInteractorFactory MockInteractorFactory { get; set; }
        protected override IInteractorFactory CreateInteractorFactory()
            => MockInteractorFactory;

        internal ITimeService MockTimeService { get; set; }
        protected override ITimeService CreateTimeService()
            => MockTimeService;

        internal ISyncManager MockSyncManager { get; set; }
        protected override ISyncManager CreateSyncManager()
            => MockSyncManager;
    }
}
