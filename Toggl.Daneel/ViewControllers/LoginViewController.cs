using System;
using System.Linq;
using System.Reactive.Linq;
using Foundation;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using UIKit;
using static Toggl.Daneel.Extensions.LoginSignupViewExtensions;
using static Toggl.Daneel.Extensions.ViewExtensions;

namespace Toggl.Daneel.ViewControllers
{
    [MvxRootPresentation(WrapInNavigationController = true)]
    [MvxFromStoryboard("Login")]
    public sealed partial class LoginViewController : ReactiveViewController<LoginViewModel>
    {
        private const int iPhoneSeScreenHeight = 568;

        private bool keyboardIsOpen = false;

        private const int topConstraintForBiggerScreens = 72;
        private const int topConstraintForBiggerScreensWithKeyboard = 40;

        private const int emailTopConstraint = 42;
        private const int emailTopConstraintWithKeyboard = 12;

        public LoginViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();

            prepareBindings();

            NavigationController.NavigationBarHidden = true;
            PasswordManagerButton.Hidden = !ViewModel.IsPasswordManagerAvailable;

            UIKeyboard.Notifications.ObserveWillShow(KeyboardWillShow);
            UIKeyboard.Notifications.ObserveWillHide(KeyboardWillHide);

        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            NavigationController?.SetNavigationBarHidden(true, true);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            NavigationController?.SetNavigationBarHidden(false, true);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (View.Frame.Height > iPhoneSeScreenHeight && !keyboardIsOpen)
                TopConstraint.Constant = topConstraintForBiggerScreens;

            SignupCard.SetupBottomCard();
            GoogleLoginButton.SetupGoogleButton();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            ActivityIndicator.Alpha = 0;
            ActivityIndicator.StartSpinning();
            PasswordTextField.ResignFirstResponder();
        }

        private void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            keyboardIsOpen = true;
            if (View.Frame.Height > iPhoneSeScreenHeight)
            {
                TopConstraint.Constant = topConstraintForBiggerScreensWithKeyboard;
            }
            else
            {
                EmailFieldTopConstraint.Constant = emailTopConstraintWithKeyboard;
            }
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            keyboardIsOpen = false;
            if (View.Frame.Height > iPhoneSeScreenHeight)
            {
                TopConstraint.Constant = topConstraintForBiggerScreens;
            }
            else
            {
                EmailFieldTopConstraint.Constant = emailTopConstraint;
            }
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareBindings()
        {
            EmailTextField.Rx().Text()
                .Subscribe(ViewModel.EmailRelay.Accept)
                .DisposedBy(DisposeBag);

            PasswordTextField.Rx().Text()
                .Subscribe(ViewModel.PasswordRelay.Accept)
                .DisposedBy(DisposeBag);

            LoginButton.Rx()
                .BindAction(ViewModel.LoginWithEmail)
                .DisposedBy(DisposeBag);

            GoogleLoginButton.Rx()
                .BindAction(ViewModel.LoginWithGoogle)
                .DisposedBy(DisposeBag);

            ShowPasswordButton.Rx()
                .BindAction(ViewModel.TogglePasswordVisibility)
                .DisposedBy(DisposeBag);

            ForgotPasswordButton.Rx()
                .BindAction(ViewModel.ForgotPassword)
                .DisposedBy(DisposeBag);

            ViewModel.EmailRelay
                .Subscribe(EmailTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoggingIn.Select(loginButtonTitle)
                .Subscribe(LoginButton.Rx().AnimatedTitle())
                .DisposedBy(DisposeBag);

            ViewModel.LoginWithEmail.Errors
                .Select(e => e.Message)
                .Subscribe(ErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.LoginWithEmail.Errors
                .SelectValue(true)
                .Subscribe(ErrorLabel.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoggingIn
                .Subscribe(ActivityIndicator.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            ViewModel.IsPasswordMasked
                .Skip(1)
                .Subscribe(PasswordTextField.Rx().SecureTextEntry())
                .DisposedBy(DisposeBag);

            ViewModel.IsShowPasswordButtonVisible
                .Subscribe(ShowPasswordButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.Shake
                .Subscribe(shakeTargets =>
                {
                    if (shakeTargets.HasFlag(LoginViewModel.ShakeTargets.Email))
                        EmailTextField.Shake();

                    if (shakeTargets.HasFlag(LoginViewModel.ShakeTargets.Password))
                        PasswordTextField.Shake();
                })
                .DisposedBy(DisposeBag);
        }

        private void prepareViews()
        {
            NavigationController.NavigationBarHidden = true;

            LoginButton.SetTitleColor(
                Color.Login.DisabledButtonColor.ToNativeColor(),
                UIControlState.Disabled
            );

            EmailTextField.ShouldReturn += _ =>
            {
                PasswordTextField.BecomeFirstResponder();
                return false;
            };

            PasswordTextField.ShouldReturn += _ =>
            {
                PasswordTextField.ResignFirstResponder();
                // TODO: login here somehow
                return false;
            };

            View.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                EmailTextField.ResignFirstResponder();
                PasswordTextField.ResignFirstResponder();
            }));

            prepareForgotPasswordButton();
            ShowPasswordButton.SetupShowPasswordButton();
        }

        private void prepareForgotPasswordButton()
        {
            var boldFont = UIFont.SystemFontOfSize(12, UIFontWeight.Medium);
            var color = Color.Login.ForgotPassword.ToNativeColor();
            var text = new NSMutableAttributedString(
                Resources.LoginForgotPassword, foregroundColor: color);
            var boldText = new NSAttributedString(
                Resources.LoginGetHelpLoggingIn,
                foregroundColor: color,
                font: boldFont);
            text.Append(boldText);
            ForgotPasswordButton.SetAttributedTitle(text, UIControlState.Normal);
        }

        private string loginButtonTitle(bool isLoading)
            => isLoading ? "" : Resources.LoginTitle;
    }
}

