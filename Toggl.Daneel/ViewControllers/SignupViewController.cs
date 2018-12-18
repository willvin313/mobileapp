using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Daneel.ViewControllers
{
    public sealed partial class SignupViewController : ReactiveViewController<SignupViewModel>
    {
        public SignupViewController() : base(nameof(SignupViewController))
        {
        }

        public override void ViewDidLoad()
        {

        }
    }
}

