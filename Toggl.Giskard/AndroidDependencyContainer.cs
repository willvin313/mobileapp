using System;
using Android.App;
using Android.Content;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Giskard.Services;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Realm;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;

namespace Toggl.Giskard
{
    public sealed class AndroidDependencyContainer : FoundationUiDependencyContainer
    {
        private readonly Lazy<SettingsStorage> settingsStorage;
        private readonly IForkingNavigationService navigationService;
        private readonly SharedPreferencesStorageAndroid sharedPreferencesStorage;

        public AndroidDependencyContainer(IForkingNavigationService navigationService, IPlatformInfo platformInfo, ISchedulerProvider schedulerProvider) 
            : base(platformInfo, schedulerProvider)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.navigationService = navigationService;

            sharedPreferencesStorage = new SharedPreferencesStorageAndroid(
                Application.Context.GetSharedPreferences(Platform.Giskard.ToString(), FileCreationMode.Private)
            );

            settingsStorage = new Lazy<SettingsStorage>(
                () => new SettingsStorage(PlatformInfo.Version, sharedPreferencesStorage)
            );
        }

        public override Lazy<IAccessRestrictionStorage> InitializeAccessRestrictionStorage()
            => new Lazy<IAccessRestrictionStorage>(() => settingsStorage.Value);

        public override Lazy<IKeyValueStorage> InitializeKeyValueStorage()
            => new Lazy<IKeyValueStorage>(() => sharedPreferencesStorage);

        public override Lazy<ILastTimeUsageStorage> InitializeLastTimeUsageStorage()
            => new Lazy<ILastTimeUsageStorage>(() => settingsStorage.Value);

        public override Lazy<IOnboardingStorage> InitializeOnboardingStorage()
            => new Lazy<IOnboardingStorage>(() => settingsStorage.Value);

        public override Lazy<IUserPreferences> InitializeUserPreferences()
            => new Lazy<IUserPreferences>(() => settingsStorage.Value);

        public override Lazy<IAnalyticsService> InitializeAnalyticsService()
            => new Lazy<IAnalyticsService>(() => new AnalyticsServiceAndroid());

        public override Lazy<ITogglApi> InitializeApi()
            => new Lazy<ITogglApi>(() => ApiFactory.Value.CreateApiWith(Credentials.None));

        public override Lazy<IBrowserService> InitializeBrowserService()
            => new Lazy<IBrowserService>(() => new BrowserServiceAndroid());

        public override Lazy<ICalendarService> InitializeCalendarService()
            => new Lazy<ICalendarService>(() => new CalendarServiceAndroid(PermissionsService.Value));

        public override Lazy<ITogglDatabase> InitializeDatabase()
            => new Lazy<ITogglDatabase>(() => new Database());

        public override Lazy<IDialogService> InitializeDialogService()
            => new Lazy<IDialogService>(() => new DialogServiceAndroid());

        public override Lazy<IGoogleService> InitializeGoogleService()
            => new Lazy<IGoogleService>(() => new GoogleServiceAndroid());

        public override Lazy<IIntentDonationService> InitializeIntentDonationService()
            => new Lazy<IIntentDonationService>(() => new NoopIntentDonationServiceAndroid());

        public override Lazy<ILicenseProvider> InitializeLicenseProvider()
            => new Lazy<ILicenseProvider>(() => new LicenseProviderAndroid());

        public override Lazy<IMailService> InitializeMailService()
            => new Lazy<IMailService>(() => new MailServiceAndroid(Application.Context));

        public override Lazy<IForkingNavigationService> InitializeNavigationService()
            => new Lazy<IForkingNavigationService>(() => navigationService);

        public override Lazy<INotificationService> InitializeNotificationService()
            => new Lazy<INotificationService>(() => new NotificationServiceAndroid());

        public override Lazy<IPermissionsService> InitializePermissionsService()
            => new Lazy<IPermissionsService>(() => new PermissionsServiceAndroid());

        public override Lazy<IPrivateSharedStorageService> InitializePrivateSharedStorageService()
            => new Lazy<IPrivateSharedStorageService>(() => new NoopPrivateSharedStorageServiceAndroid());

        public override Lazy<IRatingService> InitializeRatingService()
            => new Lazy<IRatingService>(() => new RatingServiceAndroid(Application.Context));

        public override Lazy<IRemoteConfigService> InitializeRemoteConfigService()
            => new Lazy<IRemoteConfigService>(() => new RemoteConfigServiceAndroid());

        public override Lazy<IApplicationShortcutCreator> InitializeShortcutCreator()
            => new Lazy<IApplicationShortcutCreator>(() => new ApplicationShortcutCreator(Application.Context));

        public override Lazy<IStopwatchProvider> InitializeStopwatchProvider()
            => new Lazy<IStopwatchProvider>(() => new FirebaseStopwatchProviderAndroid());
    }
}
