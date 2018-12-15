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
    [MvxFromStoryboard("Login")]
    public sealed partial class LoginViewController : ReactiveViewController<LoginViewModel>
    {
        private readonly UIImageView titleImage = new UIImageView(UIImage.FromBundle("togglLogo"));
        private readonly UIBarButtonItem backButton = new UIBarButtonItem("CUSTOM BCK", UIBarButtonItemStyle.Plain, null);

        public LoginViewController(IntPtr handle) : base(handle)
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
            NavigationItem.TitleView = titleImage;
            LoginWithEmailTextField.BecomeFirstResponder();
        }

        private void prepareBindings()
        {
            LoginWithEmailTextField.Rx().Text()
                .Subscribe(ViewModel.EmailRelay.Accept)
                .DisposedBy(DisposeBag);

            LoginWithEmailButton
                .Rx()
                .BindAction(ViewModel.ContinueToPaswordScreen)
                .DisposedBy(DisposeBag);

            GoogleLoginButton.Rx()
                .BindAction(ViewModel.LoginWithGoogle)
                .DisposedBy(DisposeBag);

            backButton.Rx()
                .BindAction(ViewModel.Back)
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

            ViewModel.ContinueToPaswordScreen.Errors
                .Select(e => e.Message)
                .Subscribe(LoginWithEmailErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoggingIn
                .Subscribe(ActivityIndicator.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoggingIn.Select(loginButtonTitle)
                .Subscribe(LoginWithEmailButton.Rx().AnimatedTitle())
                .DisposedBy(DisposeBag);

            ViewModel.IsInSecondScreen
                .Invert()
                .Subscribe(FirstScreenWrapperView.Rx().AnimatedIsVisible());

            ViewModel.IsInSecondScreen
                .Subscribe(SecondScreenWrapperView.Rx().AnimatedIsVisible());

            ViewModel.Shake
                .Subscribe(shakeTargets =>
                {
                    if (shakeTargets.HasFlag(LoginViewModel.ShakeTargets.Email))
                        LoginWithEmailTextField.Shake();
                })
                .DisposedBy(DisposeBag);
        }

        private void prepareViews()
        {
            setupGoogleButton();
            NavigationItem.LeftBarButtonItem = backButton;
            LoginWithEmailErrorLabel.Text = string.Empty;
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
            => isLoading ? "" : Resources.LoginWithEmail;
    }
}

