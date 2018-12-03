using System;
using System.Reactive.Concurrency;
using Android.Content;
using MvvmCross;
using MvvmCross.Binding;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Android;
using MvvmCross.Platforms.Android.Presenters;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Plugin;
using MvvmCross.ViewModels;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.Giskard.BroadcastReceivers;
using Toggl.Giskard.Presenters;
using Toggl.Giskard.Services;
using Toggl.PrimeRadiant.Realm;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;
using ColorPlugin = MvvmCross.Plugin.Color.Platforms.Android.Plugin;
using VisibilityPlugin = MvvmCross.Plugin.Visibility.Platforms.Android.Plugin;

namespace Toggl.Giskard
{
    public sealed partial class Setup : MvxAppCompatSetup<App<LoginViewModel>>
    {
        private const int maxNumberOfSuggestions = 5;

        private IAnalyticsService analyticsService;
        private IForkingNavigationService navigationService;

#if USE_PRODUCTION_API
        private const ApiEnvironment environment = ApiEnvironment.Production;
#else
        private const ApiEnvironment environment = ApiEnvironment.Staging;
#endif

        protected override MvxBindingBuilder CreateBindingBuilder() => new TogglBindingBuilder();

        protected override IMvxNavigationService InitializeNavigationService(IMvxViewModelLocatorCollection collection)
        {
            analyticsService = new AnalyticsServiceAndroid();

            var loader = CreateViewModelLoader(collection);
            Mvx.RegisterSingleton<IMvxViewModelLoader>(loader);

            navigationService = new NavigationService(null, loader, analyticsService, Platform.Giskard);

            Mvx.RegisterSingleton<IForkingNavigationService>(navigationService);
            Mvx.RegisterSingleton<IMvxNavigationService>(navigationService);
            return navigationService;
        }

        protected override IMvxAndroidViewPresenter CreateViewPresenter()
            => new TogglPresenter(AndroidViewAssemblies);

        protected override void InitializeApp(IMvxPluginManager pluginManager, IMvxApplication app)
        {

            var schedulerProvider = new AndroidSchedulerProvider();
            var platformInfo = new AndroidPlatformInfo(environment, ApplicationContext);

            var dependencyContainer = new AndroidDependencyContainer(navigationService, platformInfo, schedulerProvider);

            foundation.RevokeNewUserIfNeeded().Initialize();

            ensureDataSourceInitializationIfLoggedIn();


            ApplicationContext.RegisterReceiver(new TimezoneChangedBroadcastReceiver(dependencyContainer.TimeService.Value),
                new IntentFilter(Intent.ActionTimezoneChanged));

            base.InitializeApp(pluginManager, app);
        }

        protected override IMvxAndroidCurrentTopActivity CreateAndroidCurrentTopActivity()
        {
            var mvxApplication = MvxAndroidApplication.Instance;
            var activityLifecycleCallbacksManager = new QueryableMvxLifecycleMonitorCurrentTopActivity();
            mvxApplication.RegisterActivityLifecycleCallbacks(activityLifecycleCallbacksManager);
            return activityLifecycleCallbacksManager;
        }

        // Skip the sluggish and reflection-based manager and load our plugins by hand
        protected override IMvxPluginManager InitializePluginFramework()
        {
            LoadPlugins(null);
            return null;
        }

        public override void LoadPlugins(IMvxPluginManager pluginManager)
        {
            new ColorPlugin().Load();
            new VisibilityPlugin().Load();
        }

        protected override void PerformBootstrapActions()
        {
            // This method uses reflection to find classes that inherit from
            // IMvxBootstrapAction, creates instances of these classes and then
            // calls their Run method. We can skip it since we don't have such classes.
        }

        void ensureDataSourceInitializationIfLoggedIn()
        {
            /* Why? The ITogglDataSource is lazily initialized by the login manager
             * during some of it's methods calls.
             * The App.cs code that makes those calls don't have time to
             * do so during rehydration and on starup on some phones.
             * This call makes sure the ITogglDataSource singleton is registered
             * and ready to be injected during those times.
             */
            var loginManager = Mvx.Resolve<ILoginManager>();
            var dataSource = loginManager.GetDataSourceIfLoggedIn();
        }
    }
}
