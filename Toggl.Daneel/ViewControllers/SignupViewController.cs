using System;
using System.Reactive.Linq;
using MvvmCross.Binding.BindingContext;
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
    public sealed partial class SignupViewController : KeyboardAwareViewController<SignupViewModel>
    {
        private readonly UIImageView titleImage = new UIImageView(UIImage.FromBundle("togglLogo"));
        private readonly UIBarButtonItem backButton =
            new UIBarButtonItem(UIImage.FromBundle("icBackNoPadding"), UIBarButtonItemStyle.Plain, null);
        private readonly UIImage backIndicatorImage = UIImage.FromBundle("icBackNoPadding");

        private readonly UIImage tosErrorButtonImage = UIImage.FromBundle("icCheckboxSquareErrored");
        private readonly UIImage tosUncheckedButtonImage = UIImage.FromBundle("icCheckboxSquareUnchecked");
        private readonly UIImage tosCheckedButtonImage = UIImage.FromBundle("icCheckboxSquareChecked");

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

            SignUpWithEmailTextField.Rx()
                .Text()
                .Subscribe(ViewModel.EmailRelay.Accept)
                .DisposedBy(DisposeBag);

            SignUpWithEmailTextField.Rx()
                .ShouldReturn()
                .SelectUnit()
                .Subscribe(ViewModel.SignupWithEmail.Inputs)
                .DisposedBy(DisposeBag);

            GoogleSignUpButton.Rx()
                .BindAction(ViewModel.SignupWithGoogle)
                .DisposedBy(DisposeBag);

            SignUpWithEmailButton.Rx()
                .BindAction(ViewModel.SignupWithEmail)
                .DisposedBy(DisposeBag);

            SigningUpWithEmailTextField.Rx()
                .ShouldReturn()
                .Do(_ => setFirstResponder(PasswordTextField))
                .Subscribe()
                .DisposedBy(DisposeBag);

            SigningUpWithEmailTextField.Rx()
                .Text()
                .Subscribe(ViewModel.EmailRelay.Accept)
                .DisposedBy(DisposeBag);

            PasswordMaskingControl.Rx()
                .Tap()
                .Subscribe(ViewModel.TogglePasswordVisibility.Inputs)
                .DisposedBy(DisposeBag);

            PasswordTextField.Rx()
                .Text()
                .Subscribe(ViewModel.PasswordRelay.Accept)
                .DisposedBy(DisposeBag);

            PasswordTextField.Rx()
                .ShouldReturn()
                .SelectUnit()
                .Subscribe(ViewModel.GotoCountrySelection.Inputs)
                .DisposedBy(DisposeBag);

            NextButton.Rx()
                .BindAction(ViewModel.GotoCountrySelection)
                .DisposedBy(DisposeBag);

            SignUpButton.Rx()
                .BindAction(ViewModel.SignUp)
                .DisposedBy(DisposeBag);

            SelectCountryButton.Rx()
                .BindAction(ViewModel.OpenCountryPicker)
                .DisposedBy(DisposeBag);

            TOSButton.Rx()
                .BindAction(ViewModel.ToggleTOSAgreement)
                .DisposedBy(DisposeBag);

            ViewModel.EmailRelay
                .Subscribe(SignUpWithEmailTextField.Rx().TextObserver())
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
                            return PasswordTextField;
                        case SignupViewModel.ShakeTarget.Country:
                            return SelectCountryWrapperView;
                        default:
                            return null;
                    }
                })
                .Do(shake)
                .Do(setFirstResponder)
                .Subscribe()
                .DisposedBy(DisposeBag);

            ViewModel.PasswordRelay
                .Subscribe(PasswordTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.EmailRelay
                .Subscribe(SigningUpWithEmailTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsShowPasswordButtonVisible
                .Subscribe(PasswordMaskingControl.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsPasswordMasked
                .Subscribe(PasswordTextField.Rx().SecureTextEntry())
                .DisposedBy(DisposeBag);

            ViewModel.IsPasswordMasked
                .Subscribe(setMaskingIcon)
                .DisposedBy(DisposeBag);

            ViewModel.ClearPasswordScreenError
                .Select(_ => string.Empty)
                .Subscribe(EmailAndPasswordErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.GotoCountrySelection.Errors
                .Select(e => e.Message)
                .Subscribe(EmailAndPasswordErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.CountryNameLabel
                .Subscribe(SelectCountryTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Subscribe(ActivityIndicator.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Subscribe(UIApplication.SharedApplication.Rx().NetworkActivityIndicatorVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading.Select(signupButtonTitle)
                .Subscribe(SignUpButton.Rx().AnimatedTitle())
                .DisposedBy(DisposeBag);

            ViewModel.CountryErrorLabelVisible
                .Subscribe(CountryErrorLabel.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.TOSErrorLabelVisible
                .DoIf(CommonFunctions.Identity, _ => TOSButton.SetImage(tosErrorButtonImage, UIControlState.Normal))
                .Subscribe(TOSErrorLabel.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.TOSAccepted
                .Subscribe(accepted => TOSButton.SetImage(accepted ? tosCheckedButtonImage : tosUncheckedButtonImage,
                    UIControlState.Normal))
                .DisposedBy(DisposeBag);

            ViewModel.IsEmailScreenVisible
                .DoIf(CommonFunctions.Identity, _ => setFirstResponder(SignUpWithEmailTextField))
                .Subscribe(EmailScreenWrapperView.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsEmailAndPasswordScreenVisible
                .DoIf(CommonFunctions.Identity, _ => setFirstResponder(PasswordTextField))
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
            SigningUpWithEmailTextField.Text = String.Empty;
            SigningUpWithEmailTextField.Enabled = false;
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

        private string signupButtonTitle(bool isLoading) => isLoading ? "" : Resources.SignUpTitle;

        private void shake(UIView view) => view?.Shake();

        private void setFirstResponder(UIView view) => view?.BecomeFirstResponder();

        private void setMaskingIcon(bool masked)
        {
            var imageName = masked ? "icPasswordMasked" : "icPasswordUnmasked";
            PasswordMaskingImageView.Image = UIImage.FromBundle(imageName);
        }
    }
}

