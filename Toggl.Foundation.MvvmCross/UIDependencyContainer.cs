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
        public Lazy<IDialogService> DialogService { get; }
        public Lazy<IBrowserService> BrowserService { get; }
        public Lazy<IKeyValueStorage> KeyValueStorage { get; }
        public Lazy<IOnboardingStorage> OnboardingStorage { get; }
        public Lazy<IPermissionsService> PermissionsService { get; }
        public Lazy<IMvxNavigationService> NavigationService { get; }
        public Lazy<IPasswordManagerService> PasswordManagerService { get; }
        public Lazy<IAccessRestrictionStorage> AccessRestrictionStorage { get; }

        protected UiDependencyContainer(ApiEnvironment apiEnvironment, UserAgent userAgent)
            : base(apiEnvironment, userAgent)
        {
            DialogService = new Lazy<IDialogService>(CreateDialogService);
            BrowserService = new Lazy<IBrowserService>(CreateBrowserService);
            KeyValueStorage = new Lazy<IKeyValueStorage>(CreateKeyValueStorage);
            OnboardingStorage = new Lazy<IOnboardingStorage>(CreateOnboardingStorage);
            PermissionsService = new Lazy<IPermissionsService>(CreatePermissionsService);
            NavigationService = new Lazy<IMvxNavigationService>(CreateNavigationService);
            PasswordManagerService = new Lazy<IPasswordManagerService>(CreatePasswordManagerService);
            AccessRestrictionStorage = new Lazy<IAccessRestrictionStorage>(CreateAccessRestrictionStorage);
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
            => new ErrorHandlingService(NavigationService.Value, AccessRestrictionStorage.Value);
    }
}
