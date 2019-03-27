using System;
using MvvmCross;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Sync;
using Toggl.Multivac.Extensions;
using Toggl.Ultrawave;

namespace Toggl.Foundation.MvvmCross
{
    public static class FoundationExtensions
    {
        private const int newUserThreshold = 60;
        private static IDisposable loginDisposable;
        private static readonly TimeSpan retryDelayLimit = TimeSpan.FromSeconds(60);

        public static MvvmCrossFoundation.Builder StartRegisteringPlatformServices(this TogglFoundation.Builder builder)
            => builder.Build().StartRegisteringPlatformServices();

        public static MvvmCrossFoundation.Builder StartRegisteringPlatformServices(this TogglFoundation foundation)
            => new MvvmCrossFoundation.Builder(foundation);

        public static MvvmCrossFoundation RevokeNewUserIfNeeded(this MvvmCrossFoundation foundation)
        {
            var now = foundation.TimeService.CurrentDateTime;
            var lastUsed = foundation.OnboardingStorage.GetLastOpened();
            foundation.OnboardingStorage.SetLastOpened(now);
            if (!lastUsed.HasValue) return foundation;

            var offset = now - lastUsed;
            if (offset < TimeSpan.FromDays(newUserThreshold)) return foundation;

            foundation.OnboardingStorage.SetIsNewUser(false);
            return foundation;
        }

        public static void Initialize(this MvvmCrossFoundation foundation)
        {
            initializeInversionOfControl(foundation);

            (ISyncManager, IInteractorFactory) initializeAfterLogin(ITogglApi api)
            {
                var syncManager = TogglSyncManager.CreateSyncManager(
                    foundation.Database,
                    api,
                    foundation.DataSource,
                    foundation.TimeService,
                    foundation.AnalyticsService,
                    foundation.LastTimeUsageStorage,
                    foundation.Scheduler,
                    foundation.StopwatchProvider,
                    foundation.AutomaticSyncingService);

                Mvx.RegisterSingleton(api);
                Mvx.RegisterSingleton(syncManager);
                Mvx.ConstructAndRegisterSingleton<IInteractorFactory, InteractorFactory>();

                foundation.SyncErrorHandlingService.HandleErrorsOf(syncManager);

                var interactorFactory = Mvx.Resolve<IInteractorFactory>();

                return (syncManager, interactorFactory);
            }

            var userAccessManager =
                new UserAccessManager(foundation.ApiFactory, foundation.Database, foundation.GoogleService, foundation.PrivateSharedStorageService, initializeAfterLogin);

            Mvx.RegisterSingleton<IUserAccessManager>(userAccessManager);

            foundation.BackgroundSyncService.SetupBackgroundSync(userAccessManager);
        }

        private static void initializeInversionOfControl(MvvmCrossFoundation foundation)
        {
            Mvx.RegisterSingleton(foundation.DataSource);
            Mvx.RegisterSingleton(foundation.DataSource.Tags);
            Mvx.RegisterSingleton(foundation.DataSource.User);
            Mvx.RegisterSingleton(foundation.DataSource.Tasks);
            Mvx.RegisterSingleton(foundation.DataSource.Clients);
            Mvx.RegisterSingleton(foundation.DataSource.Projects);
            Mvx.RegisterSingleton(foundation.DataSource.TimeEntries);
            Mvx.RegisterSingleton(foundation.StopwatchProvider);
            Mvx.RegisterSingleton(foundation.BackgroundService);
            Mvx.RegisterSingleton(foundation.AutomaticSyncingService);
            Mvx.RegisterSingleton(foundation.BackgroundSyncService);
            Mvx.RegisterSingleton(foundation.DialogService);
            Mvx.RegisterSingleton(foundation.Database);
            Mvx.RegisterSingleton(foundation.BrowserService);
            Mvx.RegisterSingleton(foundation.UserAgent);
            Mvx.RegisterSingleton(foundation.Scheduler);
            Mvx.RegisterSingleton(foundation.ApiFactory);
            Mvx.RegisterSingleton(foundation.TimeService);
            Mvx.RegisterSingleton(foundation.RatingService);
            Mvx.RegisterSingleton(foundation.ShortcutCreator);
            Mvx.RegisterSingleton(foundation.LicenseProvider);
            Mvx.RegisterSingleton(foundation.ShortcutCreator);
            Mvx.RegisterSingleton(foundation.AnalyticsService);
            Mvx.RegisterSingleton(foundation.PlatformInfo);
            Mvx.RegisterSingleton(foundation.NotificationService);
            Mvx.RegisterSingleton(foundation.Database.IdProvider);
            Mvx.RegisterSingleton(foundation.RemoteConfigService);
            Mvx.RegisterSingleton(foundation.SuggestionProviderContainer);
            Mvx.RegisterSingleton(foundation.UserPreferences);
            Mvx.RegisterSingleton(foundation.OnboardingStorage);
            Mvx.RegisterSingleton(foundation.AccessRestrictionStorage);
            Mvx.RegisterSingleton(foundation.LastTimeUsageStorage);
            Mvx.RegisterSingleton(foundation.ErrorHandlingService);
            Mvx.RegisterSingleton(foundation.SyncErrorHandlingService);
            Mvx.RegisterSingleton(foundation.PermissionsService);
            Mvx.RegisterSingleton(foundation.CalendarService);
            Mvx.RegisterSingleton(foundation.SchedulerProvider);
            Mvx.RegisterSingleton(foundation.PlatformInfo);
            Mvx.RegisterSingleton(foundation.IntentDonationService);
            Mvx.RegisterSingleton(foundation.PrivateSharedStorageService);
            Mvx.RegisterSingleton(foundation.PasswordManagerService ?? new StubPasswordManagerService());
            Mvx.RegisterSingleton(foundation.RxActionFactory);
        }
    }
}
