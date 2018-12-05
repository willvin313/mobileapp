using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels.UserAccess
{
    [Preserve(AllMembers = true)]
    public sealed class SignupOrLoginChoiceViewModel : MvxViewModel
    {
        private readonly IMvxNavigationService navigationService;

        public UIAction StartLoginFlowAction { get; }

        public UIAction StartSignUpFlowAction { get; }

        public SignupOrLoginChoiceViewModel(IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            this.navigationService = navigationService;

            StartLoginFlowAction = UIAction.FromAsync(startLoginFlow);
            StartSignUpFlowAction = UIAction.FromAsync(startSignUpFlow);
        }

        private Task startLoginFlow() => navigationService.Navigate<LoginViewModel>();

        private Task startSignUpFlow() => navigationService.Navigate<SignupViewModel>();
    }
}
