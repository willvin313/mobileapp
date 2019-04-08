using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;
using MvvmCross.ViewModels;
using NSubstitute;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Login;
using Toggl.Foundation.UI.Services;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Sync;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public abstract class BaseViewModelTests<TViewModel> : BaseMvvmCrossTests
        where TViewModel : MvxViewModel
    {
        protected ITogglApi Api { get; } = Substitute.For<ITogglApi>();
        protected IApiFactory ApiFactory { get; } = Substitute.For<IApiFactory>();
        protected ITogglDatabase Database { get; } = Substitute.For<ITogglDatabase>();
        protected ISyncManager SyncManager { get; } = Substitute.For<ISyncManager>();
        protected IUserAccessManager UserAccessManager { get; } = Substitute.For<IUserAccessManager>();
        protected IRatingService RatingService { get; } = Substitute.For<IRatingService>();
        protected IDialogService DialogService { get; } = Substitute.For<IDialogService>();
        protected IBrowserService BrowserService { get; } = Substitute.For<IBrowserService>();
        protected ILicenseProvider LicenseProvider { get; } = Substitute.For<ILicenseProvider>();
        protected IBackgroundService BackgroundService { get; } = Substitute.For<IBackgroundService>();
        protected IPlatformInfo PlatformInfo { get; } = Substitute.For<IPlatformInfo>();
        protected IOnboardingStorage OnboardingStorage { get; } = Substitute.For<IOnboardingStorage>();
        protected IRemoteConfigService RemoteConfigService { get; } = Substitute.For<IRemoteConfigService>();
        protected IPasswordManagerService PasswordManagerService { get; } = Substitute.For<IPasswordManagerService>();
        protected IErrorHandlingService ErrorHandlingService { get; } = Substitute.For<IErrorHandlingService>();

        protected ISuggestionProviderContainer SuggestionProviderContainer { get; } =
            Substitute.For<ISuggestionProviderContainer>();
        protected IAccessRestrictionStorage AccessRestrictionStorage { get; } = Substitute.For<IAccessRestrictionStorage>();
        protected IStopwatchProvider StopwatchProvider { get; } = Substitute.For<IStopwatchProvider>();

        protected TestScheduler TestScheduler { get; }
        protected IRxActionFactory RxActionFactory { get; }

        protected TViewModel ViewModel { get; private set; }

        protected BaseViewModelTests()
        {
            TestScheduler = SchedulerProvider.TestScheduler;
            RxActionFactory = new RxActionFactory(SchedulerProvider);

            Setup();
        }

        protected abstract TViewModel CreateViewModel();

        private void Setup()
        {
            AdditionalSetup();

            ViewModel = CreateViewModel();

            AdditionalViewModelSetup();
        }

        protected virtual void AdditionalSetup()
        {
        }

        protected virtual void AdditionalViewModelSetup()
        {
        }
    }
}
