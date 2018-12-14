using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels.UserAccess;
using Toggl.Multivac.Extensions;

namespace Toggl.Daneel.ViewControllers.UserAccess
{
    public partial class SignupOrLoginChoiceViewController : ReactiveViewController<SignupOrLoginChoiceViewModel>
    {
        public SignupOrLoginChoiceViewController()
            : base(nameof(SignupOrLoginChoiceViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            SignUpButton.Layer.BorderWidth = 1;
            SignUpButton.Layer.BorderColor = Color.Signup.EnabledButtonColor.ToNativeColor().CGColor;
            SignUpButton.Layer.MasksToBounds = true;

            LoginButton.Rx()
                .BindAction(ViewModel.StartLoginFlowAction)
                .DisposedBy(DisposeBag);

            SignUpButton.Rx()
                .BindAction(ViewModel.StartSignUpFlowAction)
                .DisposedBy(DisposeBag);
        }
    }
}

