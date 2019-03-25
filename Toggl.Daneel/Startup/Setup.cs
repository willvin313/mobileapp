using Foundation;
using MvvmCross;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Ios.Core;
using MvvmCross.Platforms.Ios.Presenters;
using MvvmCross.Plugin;
using MvvmCross.ViewModels;
using Toggl.Daneel.Presentation;
using Toggl.Daneel.Services;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.ViewModels;
using ColorPlugin = MvvmCross.Plugin.Color.Platforms.Ios.Plugin;
using VisibilityPlugin = MvvmCross.Plugin.Visibility.Platforms.Ios.Plugin;

namespace Toggl.Daneel
{
    public partial class Setup : MvxIosSetup<App<OnboardingViewModel>>
    {
        public Setup()
        {
            #if !USE_PRODUCTION_API
            System.Net.ServicePointManager.ServerCertificateValidationCallback
                  += (sender, certificate, chain, sslPolicyErrors) => true;
            #endif
            var version = NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();
            IosDependencyContainer.Instance = new IosDependencyContainer(Presenter as TogglPresenter, version);
        }

        protected override IMvxApplication CreateApp()
        {
            var app = new App<OnboardingViewModel>(IosDependencyContainer.Instance);
            return app;
        }

        protected override IMvxIosViewPresenter CreateViewPresenter()
            => new TogglPresenter(ApplicationDelegate, Window);

        protected override IMvxNavigationService InitializeNavigationService(IMvxViewModelLocatorCollection collection)
        {
            var loader = CreateViewModelLoader(collection);
            Mvx.RegisterSingleton(loader);

            var container = IosDependencyContainer.Instance;
            container.ForkingNavigationService =
                new NavigationService(null, loader, container.AnalyticsService, Platform.Daneel);

            Mvx.RegisterSingleton<IMvxNavigationService>(container.ForkingNavigationService);
            return container.ForkingNavigationService;
        }

        // TODO: incorporate these calls somewher (container .ctor maybe?)
        //protected override void InitializeApp(IMvxPluginManager pluginManager, IMvxApplication app)
        //{
            
        //    foundation.RevokeNewUserIfNeeded().Initialize();

        //    base.InitializeApp(pluginManager, app);
        //}

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
    }
}
