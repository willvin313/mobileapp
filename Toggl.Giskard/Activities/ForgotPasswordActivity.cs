using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme.WhiteStatusBar",
        ScreenOrientation = ScreenOrientation.Portrait,
        WindowSoftInputMode = SoftInput.AdjustResize | SoftInput.StateVisible,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class ForgotPasswordActivity : ReactiveActivity<ForgotPasswordViewModel>
    {
        private const int snackBarDuration = 5000;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.ForgotPasswordActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_right, Resource.Animation.abc_fade_out);

            InitializeViews();
            initializeEmailHelperText();

            setupBindings();

            emailTextField.SetFocus();
        }

        private void setupBindings()
        {
            backButton.Rx().BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            emailTextField.Rx().EditorActionSent()
                .Subscribe(ViewModel.ResetPassword.Inputs)
                .DisposedBy(DisposeBag);

            emailTextField.Rx().Text()
                .Select(Email.From)
                .Subscribe(ViewModel.Email.OnNext)
                .DisposedBy(DisposeBag);

            ViewModel.Email
                .SelectToString()
                .Subscribe(emailTextField.Rx().TextObserver(ignoreUnchanged: true))
                .DisposedBy(DisposeBag);

            ViewModel.ResetPassword.Errors
                .Select(e => e.Message)
                .Do(_ => emailTextLayout.ErrorEnabled = true)
                .Subscribe(emailTextLayout.Rx().ErrorText())
                .DisposedBy(DisposeBag);

            ViewModel.SuggestContactSupport
                .Subscribe(needHelpContactUsButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            needHelpContactUsButton.Rx().Tap()
                .Subscribe(ViewModel.ContactUs.Inputs)
                .DisposedBy(DisposeBag);

            resetPasswordButton.Rx()
                .BindAction(ViewModel.ResetPassword)
                .DisposedBy(DisposeBag);

            ViewModel.ResetPassword.Executing
                .Subscribe(onPasswordResettingStateChange)
                .DisposedBy(DisposeBag);

            ViewModel.ResetPassword.Elements
                .Do(_ => resetPasswordButton.Visibility = ViewStates.Gone)
                .Subscribe(_ => showResetPasswordSuccessSnackbar())
                .DisposedBy(DisposeBag);

            ViewModel.ClearErrors
                .Subscribe(_ => emailTextLayout.ErrorEnabled = false)
                .DisposedBy(DisposeBag);
        }

        private void onPasswordResettingStateChange(bool isLoading)
        {
            if (isLoading)
            {
                emailTextLayout.HelperTextEnabled = false;
                emailTextLayout.ErrorEnabled = false;
            }

            activityIndicator.Visibility = isLoading.ToVisibility();

            resetPasswordButton.Text = isLoading
                ? string.Empty
                : GetString(Resource.String.GetPasswordResetLink);
        }

        private void initializeEmailHelperText()
        {
            emailTextLayout.HelperTextEnabled = true;
            emailTextLayout.HelperText = GetString(Resource.String.ForgotPasswordEmailExplanation).AsJavaString();
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_right);
        }

        private void showResetPasswordSuccessSnackbar()
        {
            var snackbar = Snackbar.Make(rootLayout, Resource.String.ResetPasswordEmailSentMessage, Snackbar.LengthLong);
            snackbar.SetDuration(snackBarDuration);
            snackbar.Show();
        }
    }
}
