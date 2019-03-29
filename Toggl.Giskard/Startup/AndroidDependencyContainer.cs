using System;
using Toggl.Giskard.Services;
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
using Android.Content;
using Android.App;
using MvvmCross.Navigation;

namespace Toggl.Giskard
{
    public sealed class AndroidDependencyContainer : UIDependencyContainer
    {
        private const int numberOfSuggestions = 5;

        private readonly Lazy<SettingsStorage> settingsStorage;

        public IMvxNavigationService MvxNavigationService { get; internal set; }

        public new static AndroidDependencyContainer Instance { get; private set; }

        public static void EnsureInitialized(ApiEnvironment environment, Platform platform, string version)
        {
            if (Instance != null)
                return;

            Instance = new AndroidDependencyContainer(environment, platform, version);
            UIDependencyContainer.Instance = Instance;
        }

        private AndroidDependencyContainer(ApiEnvironment environment, Platform platform, string version)
            : base(environment, new UserAgent(platform.ToString(), version))
        {
            var appVersion = Version.Parse(version);
            
            settingsStorage = new Lazy<SettingsStorage>(() => new SettingsStorage(appVersion, KeyValueStorage));
        }

        protected override IAnalyticsService CreateAnalyticsService()
            => new AnalyticsServiceAndroid();

        protected override IBackgroundSyncService CreateBackgroundSyncService()
            => new BackgroundSyncServiceAndroid();

        protected override IBrowserService CreateBrowserService()
            => new BrowserServiceAndroid();

        protected override ICalendarService CreateCalendarService()
            => new CalendarServiceAndroid(PermissionsService);

        protected override ITogglDatabase CreateDatabase()
            => new Database();

        protected override IDialogService CreateDialogService()
            => new DialogServiceAndroid();

        protected override IGoogleService CreateGoogleService()
            => new GoogleServiceAndroid();

        protected override IIntentDonationService CreateIntentDonationService()
            => new NoopIntentDonationServiceAndroid();

        protected override IKeyValueStorage CreateKeyValueStorage()
        {
            var sharedPreferences = Application.Context.GetSharedPreferences(Platform.Giskard.ToString(), FileCreationMode.Private);
            return new SharedPreferencesStorageAndroid(sharedPreferences);
        }

        protected override ILicenseProvider CreateLicenseProvider()
            => new LicenseProviderAndroid();

        protected override INotificationService CreateNotificationService()
            => new NotificationServiceAndroid();

        protected override IPasswordManagerService CreatePasswordManagerService()
            => new StubPasswordManagerService();

        protected override IPermissionsService CreatePermissionsService()
            => new PermissionsServiceAndroid();

        protected override IPlatformInfo CreatePlatformInfo()
            => new PlatformInfoAndroid();

        protected override IPrivateSharedStorageService CreatePrivateSharedStorageService()
            => new NoopPrivateSharedStorageServiceAndroid();

        protected override IRatingService CreateRatingService()
            => new RatingServiceAndroid(Application.Context);

        protected override IRemoteConfigService CreateRemoteConfigService()
            => new RemoteConfigServiceAndroid();

        protected override ISchedulerProvider CreateSchedulerProvider()
            => new AndroidSchedulerProvider();

        protected override IApplicationShortcutCreator CreateShortcutCreator()
            => new ApplicationShortcutCreator(Application.Context);

        protected override IStopwatchProvider CreateStopwatchProvider()
            => new FirebaseStopwatchProviderAndroid();

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
