using System;
using System.Reactive.Linq;
using MvvmCross.Plugin;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross
{
    public sealed class App<TFirstViewModelWhenNotLoggedIn> : MvxApplication
        where TFirstViewModelWhenNotLoggedIn : MvxViewModel
    {
        private readonly UIDependencyContainer dependencyContainer;

        public App()
        {
        }

        public App(UIDependencyContainer dependencyContainer)
        {
            this.dependencyContainer = dependencyContainer;
        }
        
        public override void Initialize()
        {
            var appStart = new AppStart<TFirstViewModelWhenNotLoggedIn>(this, dependencyContainer);
            RegisterAppStart(appStart);
        }

        public override void LoadPlugins(IMvxPluginManager pluginManager)
        {
        }

        protected override IMvxViewModelLocator CreateDefaultViewModelLocator()
            => new TogglViewModelLocator(dependencyContainer);
    }

    [Preserve(AllMembers = true)]
    public sealed class AppStart<TFirstViewModelWhenNotLoggedIn> : MvxAppStart
        where TFirstViewModelWhenNotLoggedIn : MvxViewModel
    {
        private readonly UIDependencyContainer dependencyContainer;

        public AppStart(IMvxApplication app, UIDependencyContainer dependencyContainer)
            : base (app, dependencyContainer.NavigationService)
        {
            this.dependencyContainer = dependencyContainer;
        }

        protected override void Startup(object hint = null)
        {
            revokeNewUserIfNeeded();

            dependencyContainer.BackgroundSyncService.SetupBackgroundSync(dependencyContainer.UserAccessManager);

            base.Startup(hint);
        }
        
        private void revokeNewUserIfNeeded()
        {
            const int newUserThreshold = 60;
            var now = dependencyContainer.TimeService.CurrentDateTime;
            var lastUsed = dependencyContainer.OnboardingStorage.GetLastOpened();
            dependencyContainer.OnboardingStorage.SetLastOpened(now);
            if (!lastUsed.HasValue)
                return;

            var offset = now - lastUsed;
            if (offset < TimeSpan.FromDays(newUserThreshold))
                return;

            dependencyContainer.OnboardingStorage.SetIsNewUser(false);
        }

        protected override async void NavigateToFirstViewModel(object hint = null)
        {
            var timeService = dependencyContainer.TimeService;
            var navigationService = dependencyContainer.NavigationService;
            var onboardingStorage = dependencyContainer.OnboardingStorage;
            var accessRestrictionStorage = dependencyContainer.AccessRestrictionStorage;

            onboardingStorage.SetFirstOpened(timeService.CurrentDateTime);

            if (accessRestrictionStorage.IsApiOutdated() || accessRestrictionStorage.IsClientOutdated())
            {
                await navigationService.Navigate<OutdatedAppViewModel>();
                return;
            }

            if (!dependencyContainer.UserAccessManager.CheckIfLoggedIn())
            {
                await navigationService.Navigate<TFirstViewModelWhenNotLoggedIn>();
                return;
            }
            
            var user = await dependencyContainer.InteractorFactory.GetCurrentUser().Execute();
            if (accessRestrictionStorage.IsUnauthorized(user.ApiToken))
            {
                await navigationService.Navigate<TokenResetViewModel>();
                return;
            }

            dependencyContainer.SyncManager.ForceFullSync().Subscribe();

            await navigationService.Navigate<MainTabBarViewModel>();
        }
    }
}
