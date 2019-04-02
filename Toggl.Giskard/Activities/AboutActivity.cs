using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class AboutActivity : ReactiveActivity<AboutViewModel>
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.AboutActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_right, Resource.Animation.abc_fade_out);

            InitializeViews();

            licensesButton.Rx()
                .BindAction(ViewModel.OpenLicensesView)
                .DisposedBy(DisposeBag);

            privacyPolicyButton.Rx()
                .BindAction(ViewModel.OpenPrivacyPolicyView)
                .DisposedBy(DisposeBag);

            termsOfServiceButton.Rx()
                .BindAction(ViewModel.OpenTermsOfServiceView)
                .DisposedBy(DisposeBag);

            setupToolbar();
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_right);
        }

        protected override void OnThemeChanged(ITheme theme)
        {
            var separatorColor = theme.Separator.ToNativeColor();
            licensesSeparator.SetBackgroundColor(separatorColor);
            privacyPolicySeparator.SetBackgroundColor(separatorColor);
            termsOfServiceSeparator.SetBackgroundColor(separatorColor);

            var textColor = theme.Text.ToNativeColor();
            licensesButton.SetTextColor(textColor);
            privacyPolicyButton.SetTextColor(textColor);
            termsOfServiceButton.SetTextColor(textColor);
        }

        private void setupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);

            toolbar.Title = GetString(Resource.String.About);

            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            toolbar.NavigationClick += onNavigateBack;
        }

        private void onNavigateBack(object sender, Toolbar.NavigationClickEventArgs e)
        {
            Finish();
        }
    }
}
