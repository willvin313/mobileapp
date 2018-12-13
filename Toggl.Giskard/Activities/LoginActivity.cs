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
        }
    }
}
