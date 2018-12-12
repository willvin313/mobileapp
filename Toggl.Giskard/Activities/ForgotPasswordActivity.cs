using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme.WhiteStatusBar",
        ScreenOrientation = ScreenOrientation.Portrait,
        WindowSoftInputMode = SoftInput.StateVisible,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class ForgotPasswordActivity : ReactiveActivity<ForgotPasswordViewModel>
    {
        private Subject<Unit> closeSubject = new Subject<Unit>();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.ForgotPasswordActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_right, Resource.Animation.abc_fade_out);

            setupToolbar();
            InitializeViews();
            setupInputField();

            ViewModel.ErrorMessage
                .Subscribe(errorMessage =>
                {
                    loginEmail.Error = errorMessage;
                })
                .DisposedBy(DisposeBag);

            loginEmailEditText.Rx().Text()
                .Select(Email.From)
                .Subscribe(ViewModel.Email.OnNext)
                .DisposedBy(DisposeBag);

            ViewModel.Reset.Executing
                .Subscribe(loadingProgressBar.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.PasswordResetSuccessful
                .Where(success => success)
                .Subscribe(_ => showResetPasswordSuccessToast())
                .DisposedBy(DisposeBag);

            ViewModel.PasswordResetSuccessful
                .Invert()
                .Subscribe(resetPasswordButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            resetPasswordButton.Rx()
                .BindAction(ViewModel.Reset)
                .DisposedBy(DisposeBag);

            closeSubject
                .Subscribe(ViewModel.Close.Inputs)
                .DisposedBy(DisposeBag);
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_right);
        }

        private void setupInputField()
        {
            loginEmailEditText.SetFocus();
            loginEmailEditText.SetSelection(loginEmailEditText.Text?.Length ?? 0);
        }

        private void showResetPasswordSuccessToast()
        {
            loginEmailEditText.RemoveFocus();
            Toast.MakeText(this, Resource.String.ResetPasswordEmailSentMessage, ToastLength.Long).Show();
        }

        private void setupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.ForgotPasswordToolbar);

            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.Title = GetString(Resource.String.ForgotPasswordTitle);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                closeSubject.OnNext(Unit.Default);
                return true;
            }
            return base.OnOptionsItemSelected(item);
        }
    }
}
