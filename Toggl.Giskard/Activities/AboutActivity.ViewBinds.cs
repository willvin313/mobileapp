using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.Activities
{
    public partial class AboutActivity
    {
        private View licensesSeparator;
        private View privacyPolicySeparator;
        private View termsOfServiceSeparator;

        private TextView licensesButton;
        private TextView privacyPolicyButton;
        private TextView termsOfServiceButton;

        protected override void InitializeViews()
        {
            licensesButton = FindViewById<TextView>(Resource.Id.AboutLicensesButton);
            privacyPolicyButton = FindViewById<TextView>(Resource.Id.AboutPrivacyPolicyButton);
            termsOfServiceButton = FindViewById<TextView>(Resource.Id.AboutTermsOfServiceButton);

            licensesSeparator = FindViewById(Resource.Id.LicensesSeparator);
            privacyPolicySeparator = FindViewById(Resource.Id.PrivacyPolicySeparator);
            termsOfServiceSeparator = FindViewById(Resource.Id.TermsOfServiceSeparator);
        }
    }
}
