using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Login;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation
{
    public abstract class DependencyContainer
    {
        private readonly UserAgent userAgent;
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private Lazy<ITogglApi> api;

        // Non lazy
        public ApiEnvironment ApiEnvironment { get; }
        public UserAccessManager UserAccessManager { get; }

        // Require recreation during login/logout
        public Lazy<ISyncManager> SyncManager { get; private set; }
        public Lazy<IInteractorFactory> InteractorFactory { get; private set; }

        // Normal dependencies
        public Lazy<IApiFactory> ApiFactory { get; }
        public Lazy<ITogglDatabase> Database { get; }
        public Lazy<ITimeService> TimeService { get; }
        public Lazy<IPlatformInfo> PlatformInfo { get; }
        public Lazy<ITogglDataSource> DataSource { get; }
        public Lazy<IGoogleService> GoogleService { get; }
        public Lazy<IRatingService> RatingService { get; }
        public Lazy<ICalendarService> CalendarService { get; }
        public Lazy<ILicenseProvider> LicenseProvider { get; }
        public Lazy<IUserPreferences> UserPreferences { get; }
        public Lazy<IRxActionFactory> RxActionFactory { get; }
        public Lazy<IAnalyticsService> AnalyticsService { get; }
        public Lazy<IStopwatchProvider> StopwatchProvider { get; }
        public Lazy<IBackgroundService> BackgroundService { get; }
        public Lazy<ISchedulerProvider> SchedulerProvider { get; }
        public Lazy<INotificationService> NotificationService { get; }
        public Lazy<IRemoteConfigService> RemoteConfigService { get; }
        public Lazy<IErrorHandlingService> ErrorHandlingService { get; }
        public Lazy<ILastTimeUsageStorage> LastTimeUsageStorage { get; }
        public Lazy<IApplicationShortcutCreator> ShortcutCreator { get; }
        public Lazy<IBackgroundSyncService> BackgroundSyncService { get; }
        public Lazy<IIntentDonationService> IntentDonationService { get; }
        public Lazy<IAutomaticSyncingService> AutomaticSyncingService { get; }
        public Lazy<ISyncErrorHandlingService> SyncErrorHandlingService { get; }
        public Lazy<IPrivateSharedStorageService> PrivateSharedStorageService { get; }
        public Lazy<ISuggestionProviderContainer> SuggestionProviderContainer { get; }

        protected DependencyContainer(ApiEnvironment apiEnvironment, UserAgent userAgent)
        {
            this.userAgent = userAgent;

            ApiEnvironment = apiEnvironment;

            SyncManager = new Lazy<ISyncManager>(unusableDependency<ISyncManager>);
            InteractorFactory = new Lazy<IInteractorFactory>(unusableDependency<IInteractorFactory>);

            Database = new Lazy<ITogglDatabase>(CreateDatabase);
            ApiFactory = new Lazy<IApiFactory>(CreateApiFactory);
            TimeService = new Lazy<ITimeService>(CreateTimeService);
            DataSource = new Lazy<ITogglDataSource>(CreateDataSource);
            PlatformInfo = new Lazy<IPlatformInfo>(CreatePlatformInfo);
            GoogleService = new Lazy<IGoogleService>(CreateGoogleService);
            RatingService = new Lazy<IRatingService>(CreateRatingService);
            CalendarService = new Lazy<ICalendarService>(CreateCalendarService);
            LicenseProvider = new Lazy<ILicenseProvider>(CreateLicenseProvider);
            RxActionFactory = new Lazy<IRxActionFactory>(CreateRxActionFactory);
            UserPreferences = new Lazy<IUserPreferences>(CreateUserPreferences);
            AnalyticsService = new Lazy<IAnalyticsService>(CreateAnalyticsService);
            StopwatchProvider = new Lazy<IStopwatchProvider>(CreateStopwatchProvider);
            BackgroundService = new Lazy<IBackgroundService>(CreateBackgroundService);
            SchedulerProvider = new Lazy<ISchedulerProvider>(CreateSchedulerProvider);
            ShortcutCreator = new Lazy<IApplicationShortcutCreator>(CreateShortcutCreator);
            NotificationService = new Lazy<INotificationService>(CreateNotificationService);
            RemoteConfigService = new Lazy<IRemoteConfigService>(CreateRemoteConfigService);
            ErrorHandlingService = new Lazy<IErrorHandlingService>(CreateErrorHandlingService);
            LastTimeUsageStorage = new Lazy<ILastTimeUsageStorage>(CreateLastTimeUsageStorage);
            BackgroundSyncService = new Lazy<IBackgroundSyncService>(CreateBackgroundSyncService);
            IntentDonationService = new Lazy<IIntentDonationService>(CreateIntentDonationService);
            AutomaticSyncingService = new Lazy<IAutomaticSyncingService>(CreateAutomaticSyncingService);
            SyncErrorHandlingService = new Lazy<ISyncErrorHandlingService>(CreateSyncErrorHandlingService);
            PrivateSharedStorageService = new Lazy<IPrivateSharedStorageService>(CreatePrivateSharedStorageService);
            SuggestionProviderContainer = new Lazy<ISuggestionProviderContainer>(CreateSuggestionProviderContainer);

            api = ApiFactory.Select(factory => factory.CreateApiWith(Credentials.None));
            UserAccessManager = new UserAccessManager(
                ApiFactory,
                Database,
                GoogleService,
                PrivateSharedStorageService);

            UserAccessManager
                .UserLoggedIn
                .Subscribe(recreateLazyDependenciesForLogin)
                .DisposedBy(disposeBag);

            UserAccessManager
                .UserLoggedOut
                .Subscribe(_ => recreateLazyDependenciesForLogout())
                .DisposedBy(disposeBag);
        }
        
        protected abstract ITogglDatabase CreateDatabase();
        protected abstract IPlatformInfo CreatePlatformInfo();
        protected abstract IGoogleService CreateGoogleService();
        protected abstract IRatingService CreateRatingService();
        protected abstract ICalendarService CreateCalendarService();
        protected abstract ILicenseProvider CreateLicenseProvider();
        protected abstract IUserPreferences CreateUserPreferences();
        protected abstract IAnalyticsService CreateAnalyticsService();
        protected abstract IStopwatchProvider CreateStopwatchProvider();
        protected abstract ISchedulerProvider CreateSchedulerProvider();
        protected abstract INotificationService CreateNotificationService();
        protected abstract IRemoteConfigService CreateRemoteConfigService();
        protected abstract IErrorHandlingService CreateErrorHandlingService();
        protected abstract ILastTimeUsageStorage CreateLastTimeUsageStorage();
        protected abstract IApplicationShortcutCreator CreateShortcutCreator();
        protected abstract IBackgroundSyncService CreateBackgroundSyncService();
        protected abstract IIntentDonationService CreateIntentDonationService();
        protected abstract IPrivateSharedStorageService CreatePrivateSharedStorageService();
        protected abstract ISuggestionProviderContainer CreateSuggestionProviderContainer();

        protected virtual ITimeService CreateTimeService()
            => new TimeService(SchedulerProvider.Value.DefaultScheduler);

        protected virtual IBackgroundService CreateBackgroundService()
            => new BackgroundService(TimeService.Value, AnalyticsService.Value);

        protected virtual IAutomaticSyncingService CreateAutomaticSyncingService()
            => new AutomaticSyncingService(BackgroundService.Value, TimeService.Value);

        protected virtual ISyncErrorHandlingService CreateSyncErrorHandlingService()
            => new SyncErrorHandlingService(ErrorHandlingService.Value);

        protected virtual ITogglDataSource CreateDataSource()
            => new TogglDataSource(Database.Value, TimeService.Value, AnalyticsService.Value);

        protected virtual IRxActionFactory CreateRxActionFactory()
            => new RxActionFactory(SchedulerProvider.Value);

        protected virtual IApiFactory CreateApiFactory()
            => new ApiFactory(ApiEnvironment, userAgent);

        protected virtual IInteractorFactory CreateInteractorFactory() => new InteractorFactory(
            api.Value,
            UserAccessManager,
            Database.Select(database => database.IdProvider),
            Database,
            TimeService,
            SyncManager,
            PlatformInfo,
            DataSource,
            CalendarService,
            UserPreferences,
            AnalyticsService,
            StopwatchProvider,
            NotificationService,
            LastTimeUsageStorage,
            ShortcutCreator,
            IntentDonationService,
            PrivateSharedStorageService
        );

        private void recreateLazyDependenciesForLogin(ITogglApi api)
        {
            //TODO: Make the interactor factor take lazies for easier recreation
            this.api = new Lazy<ITogglApi>(() => api);
            InteractorFactory = new Lazy<IInteractorFactory>(CreateInteractorFactory);
            SyncManager = new Lazy<ISyncManager>(() =>
                TogglSyncManager.CreateSyncManager(
                    Database.Value,
                    api,
                    DataSource.Value,
                    TimeService.Value,
                    AnalyticsService.Value,
                    LastTimeUsageStorage.Value,
                    SchedulerProvider.Value.DefaultScheduler,
                    StopwatchProvider.Value,
                    AutomaticSyncingService.Value
                )
            );
        }

        private void recreateLazyDependenciesForLogout()
        {
            api = ApiFactory.Select(factory => factory.CreateApiWith(Credentials.None));
            SyncManager = new Lazy<ISyncManager>(unusableDependency<ISyncManager>);
            InteractorFactory = new Lazy<IInteractorFactory>(CreateInteractorFactory);
        }

        private T unusableDependency<T>()
            => throw new InvalidOperationException("You can't use the this dependency before logging in");
    }
}
