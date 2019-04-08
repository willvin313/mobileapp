using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Widget;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Giskard.Extensions;
using static Toggl.Foundation.Resources;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Activities
{
    public sealed partial class TokenResetActivity
    {
        private Toolbar toolbar;

        private EditText passwordEditText;
        private ProgressBar progressBar;
        private TextView emailLabel;
        private TextView signoutLabel;
        private FloatingActionButton doneButton;

        protected override void InitializeViews()
        {
            toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);

            passwordEditText = FindViewById<EditText>(Resource.Id.TokenResetPassword);
            progressBar = FindViewById<ProgressBar>(Resource.Id.TokenResetProgressBar);
            emailLabel = FindViewById<TextView>(Resource.Id.TokenResetEmailLabel);
            signoutLabel = FindViewById<TextView>(Resource.Id.TokenResetSignOutLabel);
            doneButton = FindViewById<FloatingActionButton>(Resource.Id.TokenResetDoneButton);
        }
    }
}
