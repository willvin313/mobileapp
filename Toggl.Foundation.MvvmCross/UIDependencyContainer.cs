using System;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;
using MvvmCross.Navigation;

namespace Toggl.Foundation.MvvmCross
{
    public abstract class UIDependencyContainer : DependencyContainer
    {
        private readonly Lazy<IDialogService> dialogService;
        private readonly Lazy<IBrowserService> browserService;
        private readonly Lazy<IPermissionsService> permissionsService;
        private readonly Lazy<IMvxNavigationService> navigationService;
        private readonly Lazy<IPasswordManagerService> passwordManagerService;

        public IDialogService DialogService => dialogService.Value;
        public IBrowserService BrowserService => browserService.Value;
        public IPermissionsService PermissionsService => permissionsService.Value;
        public IMvxNavigationService NavigationService => navigationService.Value;
        public IPasswordManagerService PasswordManagerService => passwordManagerService.Value;

        public static UIDependencyContainer Instance { get; protected set; }

        protected UIDependencyContainer(ApiEnvironment apiEnvironment, UserAgent userAgent)
            : base(apiEnvironment, userAgent)
        {
            dialogService = new Lazy<IDialogService>(CreateDialogService);
            browserService = new Lazy<IBrowserService>(CreateBrowserService);
            permissionsService = new Lazy<IPermissionsService>(CreatePermissionsService);
            navigationService = new Lazy<IMvxNavigationService>(CreateNavigationService);
            passwordManagerService = new Lazy<IPasswordManagerService>(CreatePasswordManagerService);
        }

        protected abstract IDialogService CreateDialogService();
        protected abstract IBrowserService CreateBrowserService();
        protected abstract IPermissionsService CreatePermissionsService();
        protected abstract IMvxNavigationService CreateNavigationService();
        protected abstract IPasswordManagerService CreatePasswordManagerService();

        protected override IErrorHandlingService CreateErrorHandlingService()
            => new ErrorHandlingService(NavigationService, AccessRestrictionStorage);
    }
}
