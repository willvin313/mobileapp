using System;
using System.Reactive.Linq;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    public sealed partial class SignupViewController : KeyboardAwareViewController<SignupViewModel>
    {
        private readonly UIImageView titleImage = new UIImageView(UIImage.FromBundle("togglLogo"));
        private readonly UIBarButtonItem backButton =
            new UIBarButtonItem(UIImage.FromBundle("icBackNoPadding"), UIBarButtonItemStyle.Plain, null);
        private readonly UIImage backIndicatorImage = UIImage.FromBundle("icBackNoPadding");

        public SignupViewController()
            : base(nameof(SignupViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            prepareViews();
            prepareBindings();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            SignUpWithEmailTextField.BecomeFirstResponder();
        }

        private void prepareBindings()
        {
            backButton.Rx()
                .BindAction(ViewModel.Back)
                .DisposedBy(DisposeBag);

            SignUpWithEmailTextField.Rx().Text()
                .Subscribe(ViewModel.EmailRelay.Accept)
                .DisposedBy(DisposeBag);

            SignUpWithEmailTextField.Rx().ShouldReturn()
                .SelectUnit()
                .Subscribe(ViewModel.SignupWithEmail.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.EmailRelay
                .Subscribe(SignUpWithEmailTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            SignUpWithEmailButton.Rx()
                .BindAction(ViewModel.SignupWithEmail)
                .DisposedBy(DisposeBag);

            ViewModel.ClearEmailScreenError
                .Select(_ => string.Empty)
                .Subscribe(EmailScreenErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.SignupWithEmail.Errors
                .Select(e => e.Message)
                .Subscribe(EmailScreenErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.Shake
                .Select(shakeTarget =>
                {
                    switch (shakeTarget)
                    {
                        case SignupViewModel.ShakeTarget.Email:
                            return SignUpWithEmailTextField;
                        case SignupViewModel.ShakeTarget.Password:
                            return new UIView();
                        default:
                            return null;
                    }
                })
                .Do(shake)
                .Do(setFirstResponder)
                .Subscribe()
                .DisposedBy(DisposeBag);

            ViewModel.IsEmailScreenVisible
                .DoIf(CommonFunctions.Identity, _ => setFirstResponder(SignUpWithEmailTextField))
                .Subscribe(EmailScreenWrapperView.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsEmailAndPasswordScreenVisible
                .Subscribe(EmailAndPasswordScreenWrapperView.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsCountrySelectionScreenVisible
                .Subscribe(CountrySelectionScreenWrapperView.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            UIView.Animate(e.AnimationDuration, () =>
            {
                BottomToSafeAreaConstraint.Constant = e.FrameEnd.Height;
                View.LayoutIfNeeded();
            });
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            UIView.Animate(e.AnimationDuration, () =>
            {
                BottomToSafeAreaConstraint.Constant = 0;
                View.LayoutIfNeeded();
            });
        }

        private void prepareViews()
        {
            NavigationItem.TitleView = titleImage;
            NavigationItem.LeftBarButtonItem = backButton;
            NavigationController.NavigationBar.BackIndicatorImage = backIndicatorImage;
            NavigationController.NavigationBar.BackIndicatorTransitionMaskImage = backIndicatorImage;
            EmailScreenErrorLabel.Text = String.Empty;
            setupGoogleButton();
        }

        private void setupGoogleButton()
        {
            var layer = GoogleSignUpButton.Layer;
            layer.MasksToBounds = true;
            layer.CornerRadius = 8;
            layer.BorderColor = Color.Signup.EnabledButtonColor.ToNativeColor().CGColor;
            layer.BorderWidth = 1;
        }

        private void shake(UIView view) => view?.Shake();

        private void setFirstResponder(UIView view) => view?.BecomeFirstResponder();
    }
}

