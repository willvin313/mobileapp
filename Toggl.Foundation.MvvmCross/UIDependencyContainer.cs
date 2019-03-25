using System;
using Toggl.PrimeRadiant.Settings;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;
using MvvmCross.Navigation;

namespace Toggl.Foundation.MvvmCross
{
    public abstract class UiDependencyContainer : DependencyContainer
    {
        private readonly Lazy<IDialogService> dialogService;
        private readonly Lazy<IBrowserService> browserService;
        private readonly Lazy<IKeyValueStorage> keyValueStorage;
        private readonly Lazy<IOnboardingStorage> onboardingStorage;
        private readonly Lazy<IPermissionsService> permissionsService;
        private readonly Lazy<IMvxNavigationService> navigationService;
        private readonly Lazy<IPasswordManagerService> passwordManagerService;
        private readonly Lazy<IAccessRestrictionStorage> accessRestrictionStorage;

        public IDialogService DialogService => dialogService.Value;
        public IBrowserService BrowserService => browserService.Value;
        public IKeyValueStorage KeyValueStorage => keyValueStorage.Value;
        public IOnboardingStorage OnboardingStorage => onboardingStorage.Value;
        public IPermissionsService PermissionsService => permissionsService.Value;
        public IMvxNavigationService NavigationService => navigationService.Value;
        public IPasswordManagerService PasswordManagerService => passwordManagerService.Value;
        public IAccessRestrictionStorage AccessRestrictionStorage => accessRestrictionStorage.Value;

        protected UiDependencyContainer(ApiEnvironment apiEnvironment, UserAgent userAgent)
            : base(apiEnvironment, userAgent)
        {
            dialogService = new Lazy<IDialogService>(CreateDialogService);
            browserService = new Lazy<IBrowserService>(CreateBrowserService);
            keyValueStorage = new Lazy<IKeyValueStorage>(CreateKeyValueStorage);
            onboardingStorage = new Lazy<IOnboardingStorage>(CreateOnboardingStorage);
            permissionsService = new Lazy<IPermissionsService>(CreatePermissionsService);
            navigationService = new Lazy<IMvxNavigationService>(CreateNavigationService);
            passwordManagerService = new Lazy<IPasswordManagerService>(CreatePasswordManagerService);
            accessRestrictionStorage = new Lazy<IAccessRestrictionStorage>(CreateAccessRestrictionStorage);
        }

        protected abstract IDialogService CreateDialogService();
        protected abstract IBrowserService CreateBrowserService();
        protected abstract IKeyValueStorage CreateKeyValueStorage();
        protected abstract IOnboardingStorage CreateOnboardingStorage();
        protected abstract IPermissionsService CreatePermissionsService();
        protected abstract IMvxNavigationService CreateNavigationService();
        protected abstract IPasswordManagerService CreatePasswordManagerService();
        protected abstract IAccessRestrictionStorage CreateAccessRestrictionStorage();

        protected override IErrorHandlingService CreateErrorHandlingService()
            => new ErrorHandlingService(NavigationService, AccessRestrictionStorage);
    }
}
