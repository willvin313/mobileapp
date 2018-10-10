using System;
using System.Reactive.Concurrency;
using Foundation;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using MvvmCross;
using MvvmCross.Plugin;
using Toggl.Daneel.Presentation;
using Toggl.Daneel.Services;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.PrimeRadiant.Realm;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;
using MvvmCross.Platforms.Ios.Core;
using MvvmCross.Platforms.Ios.Presenters;
using System.Collections.Generic;
using System.Reflection;
using ColorPlugin = MvvmCross.Plugin.Color.Platforms.Ios.Plugin;
using VisibilityPlugin = MvvmCross.Plugin.Visibility.Platforms.Ios.Plugin;
using Toggl.Multivac.Extensions;

namespace Toggl.Daneel
{
    public partial class Setup : MvxIosSetup<App<OnboardingViewModel>>
    {
        private const int maxNumberOfSuggestions = 3;

        private IAnalyticsService analyticsService;
        private IForkingNavigationService navigationService;
        private PlatformInfo platformInfo;

#if USE_PRODUCTION_API
        private const ApiEnvironment environment = ApiEnvironment.Production;
#else
        private const ApiEnvironment environment = ApiEnvironment.Staging;
#endif

        protected override IMvxIosViewPresenter CreateViewPresenter()
            => new TogglPresenter(ApplicationDelegate, Window);

        protected override IMvxNavigationService InitializeNavigationService(IMvxViewModelLocatorCollection collection)
        {
            analyticsService = new AnalyticsService();
            platformInfo = new PlatformInfo { Platform = Platform.Daneel };

            var loader = CreateViewModelLoader(collection);
            Mvx.RegisterSingleton<IMvxViewModelLoader>(loader);

            navigationService = new NavigationService(null, loader, analyticsService, platformInfo);

            Mvx.RegisterSingleton<IForkingNavigationService>(navigationService);
            Mvx.RegisterSingleton<IMvxNavigationService>(navigationService);
            return navigationService;
        }

        public override IEnumerable<Assembly> GetPluginAssemblies()
        {
            yield return typeof(ColorPlugin).Assembly;
            yield return typeof(VisibilityPlugin).Assembly;
        }

        protected override void InitializeApp(IMvxPluginManager pluginManager, IMvxApplication app)
        {
#if !USE_PRODUCTION_API
            System.Net.ServicePointManager.ServerCertificateValidationCallback
                  += (sender, certificate, chain, sslPolicyErrors) => true;
#endif

            const string clientName = "Daneel";
            const string remoteConfigDefaultsFileName = "RemoteConfigDefaults";
            var version = NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();
            var database = new Database();
            var scheduler = Scheduler.Default;
            var timeService = new TimeService(scheduler);
            var topViewControllerProvider = (ITopViewControllerProvider)Presenter;
            var mailService = new MailService(topViewControllerProvider);
            var dialogService = new DialogService(topViewControllerProvider);
            var platformConstants = new PlatformConstants();
            var suggestionProviderContainer = new SuggestionProviderContainer(
                new MostUsedTimeEntrySuggestionProvider(database, timeService, maxNumberOfSuggestions)
            );
            var intentDonationService = new IntentDonationService();
            var privateSharedStorageService = new PrivateSharedStorageService();

            var appVersion = Version.Parse(version);
            var keyValueStorage = new UserDefaultsStorage();
            var permissionsService = new PermissionsService();
            var userAgent = new UserAgent(clientName, version);
            var settingsStorage = new SettingsStorage(Version.Parse(version), keyValueStorage);
            var remoteConfigService = new RemoteConfigService();
            remoteConfigService.SetupDefaults(remoteConfigDefaultsFileName);
            var schedulerProvider = new IOSSchedulerProvider();
            var calendarService = new CalendarService(permissionsService);
            var notificationService = new NotificationService(permissionsService, timeService);

            var foundation =
                TogglFoundation
                    .ForClient(userAgent, appVersion)
                    .WithDatabase(database)
                    .WithScheduler(scheduler)
                    .WithMailService(mailService)
                    .WithTimeService(timeService)
                    .WithApiEnvironment(environment)
                    .WithGoogleService<GoogleService>()
                    .WithRatingService<RatingService>()
                    .WithLicenseProvider<LicenseProvider>()
                    .WithAnalyticsService(analyticsService)
                    .WithSchedulerProvider(schedulerProvider)
                    .WithPlatformConstants(platformConstants)
                    .WithRemoteConfigService(remoteConfigService)
                    .WithNotificationService(notificationService)
                    .WithApiFactory(new ApiFactory(environment, userAgent))
                    .WithBackgroundService(new BackgroundService(timeService))
                    .WithApplicationShortcutCreator<ApplicationShortcutCreator>()
                    .WithSuggestionProviderContainer(suggestionProviderContainer)
                    .WithIntentDonationService(intentDonationService)
                    .WithPrivateSharedStorageService(privateSharedStorageService)
                    .WithPlatformInfo(platformInfo)

                    .StartRegisteringPlatformServices()
                    .WithDialogService(dialogService)
                    .WithLastTimeUsageStorage(settingsStorage)
                    .WithBrowserService<BrowserService>()
                    .WithKeyValueStorage(keyValueStorage)
                    .WithUserPreferences(settingsStorage)
                    .WithCalendarService(calendarService)
                    .WithOnboardingStorage(settingsStorage)
                    .WithNavigationService(navigationService)
                    .WithPermissionsService(permissionsService)
                    .WithAccessRestrictionStorage(settingsStorage)
                    .WithPasswordManagerService<OnePasswordService>()
                    .WithErrorHandlingService(new ErrorHandlingService(navigationService, settingsStorage))
                    .WithFeedbackService(new FeedbackService(userAgent, mailService, dialogService, platformConstants))
                    .Build();

            foundation.RevokeNewUserIfNeeded().Initialize();

            base.InitializeApp(pluginManager, app);
        }
    }
}
