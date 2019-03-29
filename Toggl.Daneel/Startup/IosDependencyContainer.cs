using Foundation;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Ios.Presenters;
using System;
using Toggl.Daneel.Presentation;
using Toggl.Daneel.Services;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Realm;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Daneel
{
    public sealed class IosDependencyContainer : UIDependencyContainer
    {
        private const int numberOfSuggestions = 3;

        private readonly Lazy<SettingsStorage> settingsStorage;

        public TogglPresenter ViewPresenter { get; }
        public IMvxNavigationService MvxNavigationService { get; internal set; }
        
        public new static IosDependencyContainer Instance { get; private set; }

        public static void EnsureInitialized(TogglPresenter viewPresenter, ApiEnvironment environment, Platform platform, string version)
        {
            if (Instance != null)
                return;

            Instance = new IosDependencyContainer(viewPresenter, environment, platform, version);
            UIDependencyContainer.Instance = Instance;
        }

        private IosDependencyContainer(TogglPresenter viewPresenter, ApiEnvironment environment, Platform platform, string version)
            : base(environment, new UserAgent(platform.ToString(), version))
        {
            ViewPresenter = viewPresenter;

            var appVersion = Version.Parse(version);

            settingsStorage = new Lazy<SettingsStorage>(() => new SettingsStorage(appVersion, KeyValueStorage));
        }

        protected override IAnalyticsService CreateAnalyticsService()
            => new AnalyticsServiceIos();

        protected override IBackgroundSyncService CreateBackgroundSyncService()
            => new BackgroundSyncServiceIos();

        protected override IBrowserService CreateBrowserService()
            => new BrowserServiceIos();

        protected override ICalendarService CreateCalendarService()
            => new CalendarServiceIos(PermissionsService);

        protected override ITogglDatabase CreateDatabase()
            => new Database();

        protected override IDialogService CreateDialogService()
            => new DialogServiceIos(ViewPresenter);

        protected override IGoogleService CreateGoogleService()
            => new GoogleServiceIos();

        protected override IIntentDonationService CreateIntentDonationService()
            => new IntentDonationServiceIos(AnalyticsService);

        protected override IKeyValueStorage CreateKeyValueStorage()
            => new UserDefaultsStorageIos();

        protected override ILicenseProvider CreateLicenseProvider()
            => new LicenseProviderIos();

        protected override INotificationService CreateNotificationService()
            => new NotificationServiceIos(PermissionsService, TimeService);

        protected override IPasswordManagerService CreatePasswordManagerService()
            => new OnePasswordServiceIos();

        protected override IPermissionsService CreatePermissionsService()
            => new PermissionsServiceIos();

        protected override IPlatformInfo CreatePlatformInfo()
            => new PlatformInfoIos();

        protected override IPrivateSharedStorageService CreatePrivateSharedStorageService()
            => new PrivateSharedStorageServiceIos();

        protected override IRatingService CreateRatingService()
            => new RatingServiceIos();

        protected override IRemoteConfigService CreateRemoteConfigService()
            => new RemoteConfigServiceIos();

        protected override ISchedulerProvider CreateSchedulerProvider()
            => new IOSSchedulerProvider();

        protected override IApplicationShortcutCreator CreateShortcutCreator()
            => new ApplicationShortcutCreator();

        protected override IStopwatchProvider CreateStopwatchProvider()
            => new FirebaseStopwatchProviderIos();

        protected override ISuggestionProviderContainer CreateSuggestionProviderContainer()
            => new SuggestionProviderContainer(
                new MostUsedTimeEntrySuggestionProvider(Database, TimeService, numberOfSuggestions)
            );

        protected override IMvxNavigationService CreateNavigationService()
            => MvxNavigationService;

        protected override ILastTimeUsageStorage CreateLastTimeUsageStorage()
            => settingsStorage.Value;

        protected override IOnboardingStorage CreateOnboardingStorage()
            => settingsStorage.Value;

        protected override IUserPreferences CreateUserPreferences()
            => settingsStorage.Value;

        protected override IAccessRestrictionStorage CreateAccessRestrictionStorage()
            => settingsStorage.Value;
    }
}
