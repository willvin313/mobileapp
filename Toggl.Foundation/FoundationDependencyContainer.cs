using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Login;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;

namespace Toggl.Foundation
{
    public abstract class FoundationDependencyContainer
    {
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        // Lightweight and crucial, no need to be lazy
        public IPlatformInfo PlatformInfo { get; }
        public ILoginManager LoginManager { get; }
        public ISchedulerProvider SchedulerProvider { get; }

        // Needs recreation every now and then 
        public Lazy<ITogglApi> Api { get; private set; }

        // Telemetry
        public Lazy<IAnalyticsService> AnalyticsService { get; private set; }
        public Lazy<IStopwatchProvider> StopwatchProvider { get; private set; }
        public Lazy<IRemoteConfigService> RemoteConfigService { get; private set; }

        // Dev dependencies
        public Lazy<IApiFactory> ApiFactory { get; private set; }
        public Lazy<ITimeService> TimeService { get; private set; }
        public Lazy<ITogglDataSource> DataSource { get; private set; }
        public Lazy<IBackgroundService> BackgroundService { get; private set; }
        public Lazy<IInteractorFactory> InteractorFactory { get; private set; }
        public Lazy<ISuggestionProviderContainer> SuggestionProviderContainer { get; private set; }

        // Platform specific IoC
        public Lazy<ITogglDatabase> Database { get; private set; }
        public Lazy<IMailService> MailService { get; private set; }
        public Lazy<IGoogleService> GoogleService { get; private set; }
        public Lazy<IRatingService> RatingService { get; private set; }
        public Lazy<ILicenseProvider> LicenseProvider { get; private set; }
        public Lazy<INotificationService> NotificationService { get; private set; }
        public Lazy<IApplicationShortcutCreator> ShortcutCreator { get; private set; }
        public Lazy<IIntentDonationService> IntentDonationService { get; private set; }
        public Lazy<IPrivateSharedStorageService> PrivateSharedStorageService { get; private set; }

        public Lazy<IKeyValueStorage> KeyValueStorage { get; private set; }
        public Lazy<IUserPreferences> UserPreferences { get; private set; }
        public Lazy<IOnboardingStorage> OnboardingStorage { get; private set; }
        public Lazy<IErrorHandlingService> ErrorHandlingService { get; private set; }
        public Lazy<IAccessRestrictionStorage> AccessRestrictionStorage { get; private set; }
        public Lazy<ILastTimeUsageStorage> LastTimeUsageStorage { get; private set; }
        public Lazy<ICalendarService> CalendarService { get; private set; }

        protected FoundationDependencyContainer(IPlatformInfo platformInfo, ISchedulerProvider schedulerProvider)
        {
            Ensure.Argument.IsNotNull(platformInfo, nameof(platformInfo));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            PlatformInfo = platformInfo;
            SchedulerProvider = schedulerProvider;

            initializeServices();

            LoginManager = new LoginManager(ApiFactory, Database, GoogleService, PrivateSharedStorageService);


            Observable.Merge(LoginManager.UserLoggedIn, LoginManager.UserLoggedOut)
                .Subscribe(recreateDependencies)
                .DisposedBy(disposeBag);

            DataSource = new Lazy<ITogglDataSource>(
                () => new TogglDataSource(
                    Api.Value,
                    Database.Value,
                    TimeService.Value,
                    ErrorHandlingService.Value,
                    BackgroundService.Value,
                    CreateSyncManager.Value,
                    MinimumTimeInBackgroundForFullSync.Value,
                    NotificationService.Value,
                    ShortcutCreator.Value,
                    AnalyticsService.Value,
                    LoginManager
                )
            );

            InteractorFactory = new Lazy<IInteractorFactory>(
                () => new InteractorFactory(
                    Database.Value.IdProvider, 
                    TimeService.Value,
                    PlatformInfo,
                    DataSource.Value,
                    UserPreferences.Value,
                    AnalyticsService.Value,
                    NotificationService.Value,
                    IntentDonationService.Value,
                    ShortcutCreator.Value,
                    LastTimeUsageStorage.Value,
                    CalendarService.Value
                )
            );
        }

        private void recreateDependencies()
        {

        }

        private void initializeServices()
        {
            // Needs recreation every now and then 
            Api = InitializeApi();

            // Telemetry
            AnalyticsService = InitializeAnalyticsService();
            StopwatchProvider = InitializeStopwatchProvider();
            RemoteConfigService = InitializeRemoteConfigService();

            // Dev dependencies
            ApiFactory = InitializeApiFactory();
            TimeService = InitializeTimeService();
            BackgroundService = InitializeBackgroundService();
            SuggestionProviderContainer = InitializeSuggestionProviderContainer();

            // Platform specific IoC
            Database = InitializeDatabase();
            MailService = InitializeMailService();
            GoogleService = InitializeGoogleService();
            RatingService = InitializeRatingService();
            CalendarService = InitializeCalendarService();
            KeyValueStorage = InitializeKeyValueStorage();
            LicenseProvider = InitializeLicenseProvider();
            ShortcutCreator = InitializeShortcutCreator();
            UserPreferences = InitializeUserPreferences();
            OnboardingStorage = InitializeOnboardingStorage();
            NotificationService = InitializeNotificationService();
            ErrorHandlingService = InitializeErrorHandlingService();
            LastTimeUsageStorage = InitializeLastTimeUsageStorage();
            IntentDonationService = InitializeIntentDonationService();
            AccessRestrictionStorage = InitializeAccessRestrictionStorage();
            PrivateSharedStorageService = InitializePrivateSharedStorageService();
        }

        // Dev dependencies
        public virtual Lazy<IApiFactory> InitializeApiFactory()
            => new Lazy<IApiFactory>(() => new ApiFactory(PlatformInfo.ApiEnvironment, PlatformInfo.UserAgent));

        public virtual Lazy<ITimeService> InitializeTimeService()
            => new Lazy<ITimeService>(() => new TimeService(SchedulerProvider.DefaultScheduler));

        public virtual Lazy<IBackgroundService> InitializeBackgroundService()
            => new Lazy<IBackgroundService>(() => new BackgroundService(TimeService.Value));

        public virtual Lazy<ISuggestionProviderContainer> InitializeSuggestionProviderContainer()
            => new Lazy<ISuggestionProviderContainer>(() => new SuggestionProviderContainer());

        // Needs recreation every now and then 
        public abstract Lazy<ITogglApi> InitializeApi();

        // Telemetry
        public abstract Lazy<IAnalyticsService> InitializeAnalyticsService();
        public abstract Lazy<IStopwatchProvider> InitializeStopwatchProvider();
        public abstract Lazy<IRemoteConfigService> InitializeRemoteConfigService();

        // Platform specific IoC
        public abstract Lazy<ITogglDatabase> InitializeDatabase();
        public abstract Lazy<IMailService> InitializeMailService();
        public abstract Lazy<IGoogleService> InitializeGoogleService();
        public abstract Lazy<IRatingService> InitializeRatingService();
        public abstract Lazy<ILicenseProvider> InitializeLicenseProvider();
        public abstract Lazy<IKeyValueStorage> InitializeKeyValueStorage();
        public abstract Lazy<IUserPreferences> InitializeUserPreferences();
        public abstract Lazy<ICalendarService> InitializeCalendarService();
        public abstract Lazy<IOnboardingStorage> InitializeOnboardingStorage();
        public abstract Lazy<INotificationService> InitializeNotificationService();
        public abstract Lazy<IErrorHandlingService> InitializeErrorHandlingService();
        public abstract Lazy<ILastTimeUsageStorage> InitializeLastTimeUsageStorage();
        public abstract Lazy<IApplicationShortcutCreator> InitializeShortcutCreator();
        public abstract Lazy<IIntentDonationService> InitializeIntentDonationService();
        public abstract Lazy<IAccessRestrictionStorage> InitializeAccessRestrictionStorage();
        public abstract Lazy<IPrivateSharedStorageService> InitializePrivateSharedStorageService();
    }
}
