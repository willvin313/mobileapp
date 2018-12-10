using System;
using System.Reactive.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
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
              WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class LoginActivity : ReactiveActivity<LoginViewModel>
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.LoginActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            InitializeViews();

            emailEditText.Text = ViewModel.Email.FirstAsync().GetAwaiter().GetResult();
            passwordEditText.Text = ViewModel.Password.FirstAsync().GetAwaiter().GetResult();

            //Text
            ViewModel.ErrorMessage
                .Subscribe(errorTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            emailEditText.Rx().Text()
                .Select(Email.From)
                .Subscribe(ViewModel.SetEmail)
                .DisposedBy(DisposeBag);

            passwordEditText.Rx().Text()
                .Select(Password.From)
                .Subscribe(ViewModel.SetPassword)
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(loginButtonTitle)
                .Subscribe(loginButton.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            //Visibility
            ViewModel.HasError
                .Subscribe(errorTextView.Rx().IsVisible(useGone: false))
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Subscribe(progressBar.Rx().IsVisible(useGone: false))
                .DisposedBy(DisposeBag);

            ViewModel.LoginEnabled
                .Subscribe(loginButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            //Commands
            signupCard.Rx()
                .BindAction(ViewModel.Signup)
                .DisposedBy(DisposeBag);

            loginButton.Rx().Tap()
                .VoidSubscribe(ViewModel.Login)
                .DisposedBy(DisposeBag);

            passwordEditText.Rx().EditorActionSent()
                .VoidSubscribe(ViewModel.Login)
                .DisposedBy(DisposeBag);

            googleLoginButton.Rx().Tap()
                .VoidSubscribe(ViewModel.GoogleLogin)
                .DisposedBy(DisposeBag);

            forgotPasswordView.Rx()
                .BindAction(ViewModel.ForgotPassword)
                .DisposedBy(DisposeBag);

            string loginButtonTitle(bool isLoading)
                => isLoading ? "" : Resources.GetString(Resource.String.Login);

            this.CancelAllNotifications();
        }
    }
}
