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
        public LoginViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();

            prepareBindings();
        }

        private void prepareBindings()
        {
            EmailTextField.Rx().Text()
                .Subscribe(ViewModel.EmailRelay.Accept)
                .DisposedBy(DisposeBag);

            LoginButton.Rx()
                .BindAction(ViewModel.LoginWithEmail)
                .DisposedBy(DisposeBag);

            GoogleLoginButton.Rx()
                .BindAction(ViewModel.LoginWithGoogle)
                .DisposedBy(DisposeBag);

            ViewModel.LoginWithGoogle.Errors.Debug("ERROR").Subscribe()
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

            ViewModel.Shake
                .Subscribe(shakeTargets =>
                {
                    if (shakeTargets.HasFlag(LoginViewModel.ShakeTargets.Email))
                        EmailTextField.Shake();
                })
                .DisposedBy(DisposeBag);
        }

        private void prepareViews()
        {
            setupGoogleButton();
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
    }
}

