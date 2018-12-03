using System;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross
{
    public abstract class FoundationUiDependencyContainer : FoundationDependencyContainer
    {
        public Lazy<IDialogService> DialogService { get; private set; }
        public Lazy<IBrowserService> BrowserService { get; private set; }
        public Lazy<IFeedbackService> FeedbackService { get; private set; }
        public Lazy<IPermissionsService> PermissionsService { get; private set; }
        public Lazy<IForkingNavigationService> NavigationService { get; private set; }
        public Lazy<IPasswordManagerService> PasswordManagerService { get; private set; }

        protected FoundationUiDependencyContainer(IPlatformInfo platformInfo, ISchedulerProvider schedulerProvider)
            : base(platformInfo, schedulerProvider)
        {
            initializeServices();
        }

        private void initializeServices()
        {
            DialogService = InitializeDialogService();
            BrowserService = InitializeBrowserService();
            FeedbackService = InitializeFeedbackService();
            NavigationService = InitializeNavigationService();
            PermissionsService = InitializePermissionsService();
            PasswordManagerService = InitializePasswordManagerService();
        }

        public abstract Lazy<IDialogService> InitializeDialogService();
        public abstract Lazy<IBrowserService> InitializeBrowserService();
        public abstract Lazy<IPermissionsService> InitializePermissionsService();
        public abstract Lazy<IForkingNavigationService> InitializeNavigationService();

        public override Lazy<IErrorHandlingService> InitializeErrorHandlingService()
            => new Lazy<IErrorHandlingService>(() => new ErrorHandlingService(NavigationService.Value, AccessRestrictionStorage.Value));

        public virtual Lazy<IFeedbackService> InitializeFeedbackService()
            => new Lazy<IFeedbackService>(() => new FeedbackService(MailService.Value, DialogService.Value, PlatformInfo));

        public virtual Lazy<IPasswordManagerService> InitializePasswordManagerService()
            => new Lazy<IPasswordManagerService>(() => new StubPasswordManagerService());
    }
}
