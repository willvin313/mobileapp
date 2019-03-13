using System;
using Toggl.PrimeRadiant.Settings;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Ultrawave;

namespace Toggl.Foundation.MvvmCross
{
    public abstract class UiDependencyContainer : DependencyContainer
    {
        public Lazy<IDialogService> DialogService { get; }
        public Lazy<IBrowserService> BrowserService { get; }
        public Lazy<IKeyValueStorage> KeyValueStorage { get; }
        public Lazy<IOnboardingStorage> OnboardingStorage { get; }
        public Lazy<IPermissionsService> PermissionsService { get; }
        public Lazy<IForkingNavigationService> NavigationService { get; }
        public Lazy<IPasswordManagerService> PasswordManagerService { get; }
        public Lazy<IAccessRestrictionStorage> AccessRestrictionStorage { get; }

        protected UiDependencyContainer(ApiEnvironment apiEnvironment)
            : base(apiEnvironment)
        {
            DialogService = new Lazy<IDialogService>(CreateDialogService);
            BrowserService = new Lazy<IBrowserService>(CreateBrowserService);
            KeyValueStorage = new Lazy<IKeyValueStorage>(CreateKeyValueStorage);
            OnboardingStorage = new Lazy<IOnboardingStorage>(CreateOnboardingStorage);
            PermissionsService = new Lazy<IPermissionsService>(CreatePermissionsService);
            NavigationService = new Lazy<IForkingNavigationService>(CreateNavigationService);
            PasswordManagerService = new Lazy<IPasswordManagerService>(CreatePasswordManagerService);
            AccessRestrictionStorage = new Lazy<IAccessRestrictionStorage>(CreateAccessRestrictionStorage);
        }

        protected abstract IDialogService CreateDialogService();
        protected abstract IBrowserService CreateBrowserService();
        protected abstract IKeyValueStorage CreateKeyValueStorage();
        protected abstract IOnboardingStorage CreateOnboardingStorage();
        protected abstract IPermissionsService CreatePermissionsService();
        protected abstract IForkingNavigationService CreateNavigationService();
        protected abstract IPasswordManagerService CreatePasswordManagerService();
        protected abstract IAccessRestrictionStorage CreateAccessRestrictionStorage();

        protected override IErrorHandlingService CreateErrorHandlingService()
            => new ErrorHandlingService(NavigationService.Value, AccessRestrictionStorage.Value);
    }
}
