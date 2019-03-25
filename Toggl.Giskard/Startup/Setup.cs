using Android.App;
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
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Giskard.BroadcastReceivers;
using Toggl.Giskard.Presenters;
using Toggl.Giskard.Startup;
using ColorPlugin = MvvmCross.Plugin.Color.Platforms.Android.Plugin;
using VisibilityPlugin = MvvmCross.Plugin.Visibility.Platforms.Android.Plugin;

namespace Toggl.Giskard
{
    public sealed partial class Setup : MvxAppCompatSetup<App<LoginViewModel>>
    {
        public Setup()
        {
            #if !USE_PRODUCTION_API
            System.Net.ServicePointManager.ServerCertificateValidationCallback
                  += (sender, certificate, chain, sslPolicyErrors) => true;
            #endif

            var applicationContext = Application.Context;
            var packageInfo = applicationContext.PackageManager.GetPackageInfo(applicationContext.PackageName, 0);

            AndroidDependencyContainer.Instance = new AndroidDependencyContainer(packageInfo.VersionName);
        }

        protected override MvxBindingBuilder CreateBindingBuilder() => new TogglBindingBuilder();

        protected override IMvxNavigationService InitializeNavigationService(IMvxViewModelLocatorCollection collection)
        {
            var loader = CreateViewModelLoader(collection);
            Mvx.RegisterSingleton(loader);

            var container = AndroidDependencyContainer.Instance;
            container.ForkingNavigationService =
                new NavigationService(null, loader, container.AnalyticsService, Platform.Giskard);

            Mvx.RegisterSingleton<IMvxNavigationService>(container.ForkingNavigationService);
            return container.ForkingNavigationService;
        }

        protected override IMvxApplication CreateApp()
            => new App<LoginViewModel>(AndroidDependencyContainer.Instance);

        protected override IMvxAndroidViewPresenter CreateViewPresenter()
            => new TogglPresenter(AndroidViewAssemblies);

        protected override void InitializeApp(IMvxPluginManager pluginManager, IMvxApplication app)
        {
            var dependencyContainer = AndroidDependencyContainer.Instance;
            ApplicationContext.RegisterReceiver(new TimezoneChangedBroadcastReceiver(dependencyContainer.TimeService),
                new IntentFilter(Intent.ActionTimezoneChanged));

            //TODO: Move this elsewhere
            //foundation.RevokeNewUserIfNeeded().Initialize();

            //ensureDataSourceInitializationIfLoggedIn();
            createApplicationLifecycleObserver(dependencyContainer.BackgroundService);

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

        //TODO: Verify this works
        //void ensureDataSourceInitializationIfLoggedIn()
        //{
        //    /* Why? The ITogglDataSource is lazily initialized by the login manager
        //     * during some of it's methods calls.
        //     * The App.cs code that makes those calls don't have time to
        //     * do so during rehydration and on starup on some phones.
        //     * This call makes sure the ITogglDataSource singleton is registered
        //     * and ready to be injected during those times.
        //     */
        //    var userAccessManager = Mvx.Resolve<IUserAccessManager>();
        //    userAccessManager.TryInitializingAccessToUserData(out _, out _);
        //}

        private void createApplicationLifecycleObserver(IBackgroundService backgroundService)
        {
            var mvxApplication = MvxAndroidApplication.Instance;
            var appLifecycleObserver = new ApplicationLifecycleObserver(backgroundService);
            mvxApplication.RegisterActivityLifecycleCallbacks(appLifecycleObserver);
            mvxApplication.RegisterComponentCallbacks(appLifecycleObserver);
        }
    }
}
