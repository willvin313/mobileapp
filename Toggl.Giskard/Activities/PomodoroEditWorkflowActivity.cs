using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Multivac.Extensions;
using Toggl.Foundation.Extensions;
using Android.Text;
using Android.Support.V7.Widget;
using System.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Transformations;
using Toggl.Giskard.ViewHolders;
using TimeEntryExtensions = Toggl.Giskard.Extensions.TimeEntryExtensions;
using TextResources = Toggl.Foundation.Resources;
using TagsAdapter = Toggl.Giskard.Adapters.SimpleAdapter<string>;
using static Toggl.Giskard.Resource.String;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme.BlueStatusBar",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class PomodoroEditWorkflowActivity : ReactiveActivity<PomodoroEditWorkflowViewModel>
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.PomodoroEditWorkflowActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            InitializeViews();

            setupViews();
            setupBindings();
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back)
            {
                ViewModel.Close.Execute();
                return true;
            }

            return base.OnKeyDown(keyCode, e);
        }

        private void setupViews()
        {
        }

        private void setupBindings()
        {
            ViewModel.WorkflowItems
                .Subscribe(workflowView.Update)
                .DisposedBy(DisposeBag);
        }
    }
}
