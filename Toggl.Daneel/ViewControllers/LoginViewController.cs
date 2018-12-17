using System;
using System.Linq;
using System.Reactive.Linq;
using CoreGraphics;
using Foundation;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [MvxFromStoryboard("Login")]
    public sealed partial class LoginViewController : KeyboardAwareViewController<LoginViewModel>
    {
        private readonly UIImageView titleImage = new UIImageView(UIImage.FromBundle("togglLogo"));
        private readonly UIBarButtonItem backButton =
            new UIBarButtonItem(UIImage.FromBundle("icBackNoPadding"), UIBarButtonItemStyle.Plain, null);
        private readonly UIImage backIndicatorImage = UIImage.FromBundle("icBackNoPadding");

        public LoginViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();

            prepareBindings();
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

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            NavigationItem.TitleView = titleImage;
            LoginWithEmailTextField.BecomeFirstResponder();
            ActivityIndicator.StartSpinning();
        }

        private void prepareBindings()
        {
            backButton.Rx()
                .BindAction(ViewModel.Back)
                .DisposedBy(DisposeBag);

            // First Screen
            LoginWithEmailTextField.Rx().Text()
                .Subscribe(ViewModel.EmailRelay.Accept)
                .DisposedBy(DisposeBag);

            LoginWithEmailButton
                .Rx()
                .BindAction(ViewModel.LoginWithEmail)
                .DisposedBy(DisposeBag);

            GoogleLoginButton.Rx()
                .BindAction(ViewModel.LoginWithGoogle)
                .DisposedBy(DisposeBag);

            PasswordMaskingControl.Rx().Tap()
                .Subscribe(ViewModel.TogglePasswordVisibility.Inputs)
                .DisposedBy(DisposeBag);

            // Second Screen
            ForgotPasswordButton.Rx()
                .BindAction(ViewModel.ForgotPassword)
                .DisposedBy(DisposeBag);

            LoginButton.Rx()
                .BindAction(ViewModel.Login)
                .DisposedBy(DisposeBag);

            SecondScreenEmailTextField.Rx().Text()
                .Subscribe(ViewModel.EmailRelay.Accept)
                .DisposedBy(DisposeBag);

            ViewModel.EmailRelay
                .Subscribe(SecondScreenEmailTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsEmailFieldEdittable
                .Subscribe(SecondScreenEmailTextField.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsPasswordMasked
                .Subscribe(PasswordTextField.Rx().SecureTextEntry())
                .DisposedBy(DisposeBag);

            ViewModel.IsShowPasswordButtonVisible
                .Subscribe(PasswordMaskingControl.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsPasswordMasked
                .Subscribe(setMaskingIcon)
                .DisposedBy(DisposeBag);

            PasswordTextField.Rx().Text()
                .Subscribe(ViewModel.PasswordRelay.Accept)
                .DisposedBy(DisposeBag);

            ViewModel.ClearPasswordScreenError
                .Select(_ => string.Empty)
                .Subscribe(SecondScreenErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.Login.Errors
                .Select(e => e.Message)
                .Subscribe(SecondScreenErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.LoginWithGoogle.Errors
                .Select(e => e.Message)
                .Subscribe(LoginWithEmailErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.ClearEmailScreenError
                .Select(_ => string.Empty)
                .Subscribe(LoginWithEmailErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.EmailRelay
                .Subscribe(LoginWithEmailTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.PasswordRelay
                .Subscribe(PasswordTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.LoginWithEmail.Errors
                .Select(e => e.Message)
                .Subscribe(LoginWithEmailErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoggingIn
                .Subscribe(ActivityIndicator.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoggingIn
                .Subscribe(UIApplication.SharedApplication.Rx().NetworkActivityIndicatorVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoggingIn.Select(loginButtonTitle)
                .Subscribe(LoginButton.Rx().AnimatedTitle())
                .DisposedBy(DisposeBag);

            ViewModel.IsInSecondScreen
                .Invert()
                .Subscribe(FirstScreenWrapperView.Rx().AnimatedIsVisible());

            ViewModel.IsInSecondScreen
                .Subscribe(SecondScreenWrapperView.Rx().AnimatedIsVisible());

            ViewModel.Shake
                .Subscribe(shakeTarget =>
                {
                    switch (shakeTarget)
                    {
                        case LoginViewModel.ShakeTarget.Email:
                            LoginWithEmailTextField.Shake();
                            LoginWithEmailTextField.BecomeFirstResponder();
                            break;
                        case LoginViewModel.ShakeTarget.Password:
                            PasswordTextField.Shake();
                            PasswordTextField.BecomeFirstResponder();
                            break;
                    }
                })
                .DisposedBy(DisposeBag);
        }

        private void prepareViews()
        {
            setupGoogleButton();
            NavigationItem.LeftBarButtonItem = backButton;
            NavigationController.NavigationBar.BackIndicatorImage = backIndicatorImage;
            NavigationController.NavigationBar.BackIndicatorTransitionMaskImage = backIndicatorImage;
            LoginWithEmailErrorLabel.Text = string.Empty;
            SecondScreenErrorLabel.Text = string.Empty;
        }

        private void setupGoogleButton()
        {
            var layer = GoogleLoginButton.Layer;
            layer.MasksToBounds = true;
            layer.CornerRadius = 8;
            layer.BorderColor = Color.Signup.EnabledButtonColor.ToNativeColor().CGColor;
            layer.BorderWidth = 1;
        }

        private string loginButtonTitle(bool isLoading)
            => isLoading ? "" : Resources.LoginTitle;

        private void setMaskingIcon(bool masked)
        {
            var imageName = masked ? "icPasswordMasked" : "icPasswordUnmasked";
            PasswordMaskingImageView.Image = UIImage.FromBundle(imageName);
        }

    }
}

