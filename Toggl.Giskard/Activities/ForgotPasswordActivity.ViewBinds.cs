using Android.Support.Constraints;
using Android.Support.Design.Widget;
using Android.Widget;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Activities
{
    public partial class ForgotPasswordActivity
    {
        private ImageView backButton;
        private TextInputLayoutWithHelperText emailTextLayout;
        private TextInputEditText emailTextField;
        private TextView needHelpContactUsButton;
        private Button resetPasswordButton;
        private ProgressBar activityIndicator;
        private ConstraintLayout rootLayout;

        protected override void InitializeViews()
        {
            backButton = FindViewById<ImageView>(Resource.Id.BackButton);
            emailTextLayout = FindViewById<TextInputLayoutWithHelperText>(Resource.Id.EmailTextLayout);
            emailTextField = FindViewById<TextInputEditText>(Resource.Id.EmailTextField);
            needHelpContactUsButton = FindViewById<TextView>(Resource.Id.NeedHelpContactUsButton);
            resetPasswordButton = FindViewById<Button>(Resource.Id.ResetPasswordButton);
            activityIndicator = FindViewById<ProgressBar>(Resource.Id.ActivityIndicator);
            rootLayout = FindViewById<ConstraintLayout>(Resource.Id.RootLayout);
        }
    }
}
