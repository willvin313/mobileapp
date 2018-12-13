using System;
using System.Reactive.Linq;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [MvxChildPresentation]
    public sealed partial class ForgotPasswordViewController
        : KeyboardAwareViewController<ForgotPasswordViewModel>
    {
        private const int distanceFromTop = 136;
        private const int backButtonFontSize = 14;
        private const int iPhoneSeScreenHeight = 568;
        private const int resetButtonBottomSpacing = 32;

        private bool viewInitialized;

        public ForgotPasswordViewController() : base(nameof(ForgotPasswordViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = Resources.LoginForgotPassword;

            prepareViews();

            prepareBindings();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (viewInitialized) return;

            viewInitialized = true;

            if (View.Frame.Height > iPhoneSeScreenHeight)
                TopConstraint.Constant = distanceFromTop;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            EmailTextField.BecomeFirstResponder();
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            ResetPasswordButtonBottomConstraint.Constant = e.FrameEnd.Height + resetButtonBottomSpacing;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            ResetPasswordButtonBottomConstraint.Constant = resetButtonBottomSpacing;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareBindings()
        {
            EmailTextField.Rx().ShouldReturn()
                .Subscribe(ViewModel.Reset.Inputs)
                .DisposedBy(DisposeBag);

            EmailTextField.Rx().Text()
                .Select(Email.From)
                .Subscribe(ViewModel.Email.OnNext)
                .DisposedBy(DisposeBag);

            ResetPasswordButton.Rx()
                .BindAction(ViewModel.Reset)
                .DisposedBy(DisposeBag);

            ViewModel.Reset.Errors
                .Subscribe(updateErrorMessage)
                .DisposedBy(DisposeBag);

            ViewModel.Reset.Executing
                .Subscribe(loading =>
                {
                    if (loading)
                    {
                        updateErrorMessage(null);
                    }
                    updateResetPasswordButton(loading);
                })
                .DisposedBy(DisposeBag);

            ViewModel.Reset.Executing
                .Subscribe(ActivityIndicator.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            ViewModel.Reset.Elements
                .Subscribe(_ => {
                    DoneCard.Rx().IsVisibleWithFade()(true);
                    ResetPasswordButton.Rx().IsVisibleWithFade()(false);
                })
                .DisposedBy(DisposeBag);
        }

        private void updateResetPasswordButton(bool loading)
        {
            UIView.Transition(
                ResetPasswordButton,
                Animation.Timings.EnterTiming,
                UIViewAnimationOptions.TransitionCrossDissolve,
                () => ResetPasswordButton.SetTitle(loading ? "" : Resources.GetPasswordResetLink, UIControlState.Normal),
                null
            );
        }

        private void updateErrorMessage(Exception exception)
        {
            ErrorLabel.Text = exception != null
                ? exception.Message
                : string.Empty;

            ErrorLabel.Hidden = exception == null;

            if(exception != null)
                EmailTextField.BecomeFirstResponder();
        }

        private void prepareViews()
        {
            NavigationController.NavigationBarHidden = false;

            ResetPasswordButton.SetTitleColor(
                Color.Login.DisabledButtonColor.ToNativeColor(),
                UIControlState.Disabled
            );

            ActivityIndicator.StartSpinning();

            ErrorLabel.Hidden = true;

            prepareBackbutton();
        }

        private void prepareBackbutton()
        {
            var image = UIImage
                .FromBundle("icBackNoPadding")
                .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            var color = Color.NavigationBar.BackButton.ToNativeColor();
            var backButton = new UIButton();
            backButton.TintColor = color;
            backButton.SetImage(image, UIControlState.Normal);
            backButton.SetTitleColor(color, UIControlState.Normal);
            backButton.SetTitle(Resources.Back, UIControlState.Normal);
            backButton.TitleLabel.Font = UIFont.SystemFontOfSize(backButtonFontSize, UIFontWeight.Medium);

            backButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            //Spacing between button image and title
            var spacing = 6;
            backButton.ImageEdgeInsets = new UIEdgeInsets(0, 0, 0, spacing);
            backButton.TitleEdgeInsets = new UIEdgeInsets(0, spacing, 0, 0);

            NavigationItem.HidesBackButton = true;
            NavigationItem.LeftItemsSupplementBackButton = false;
            NavigationItem.SetLeftBarButtonItem(new UIBarButtonItem(backButton), true);

            //Otherwise title gets clipped
            var frame = backButton.Frame;
            frame.Width = 90;
            backButton.Frame = frame;
            backButton.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;
        }
    }
}
