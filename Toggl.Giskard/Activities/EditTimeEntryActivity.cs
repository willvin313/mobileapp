using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Onboarding.EditView;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.Helper;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.MvvmCross.Parameters.SelectTimeParameters.Origin;
using TimeEntryExtensions = Toggl.Giskard.Extensions.TimeEntryExtensions;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme.BlueStatusBar",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class EditTimeEntryActivity : MvxAppCompatActivity<EditTimeEntryViewModel>
    {
        private PopupWindow projectTooltip;

        public CompositeDisposable DisposeBag { get; private set; } = new CompositeDisposable();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.EditTimeEntryActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            setupBindings();
        }

        protected override void OnResume()
        {
            base.OnResume();

            projectTooltip = projectTooltip
                ?? PopupWindowFactory.PopupWindowWithText(
                    this,
                    Resource.Layout.TooltipWithLeftTopArrow,
                    Resource.Id.TooltipText,
                    Resource.String.CategorizeWithProjects);

            prepareOnboarding();
        }

        protected override void OnStop()
        {
            base.OnStop();
            projectTooltip.Dismiss();
            projectTooltip = null;
        }

        private void prepareOnboarding()
        {
            var storage = ViewModel.OnboardingStorage;

            new CategorizeTimeUsingProjectsOnboardingStep(storage, ViewModel.HasProject)
                .ManageDismissableTooltip(
                    projectTooltip,
                    ProjectContainer,
                    (window, view) => PopupOffsets.FromDp(16, 8, this),
                    storage)
                .DisposedBy(DisposeBag);
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }

        private void setupBindings()
        {
            StartTimeArea.Rx().Tap()
                .Subscribe(_ => ViewModel.SelectTimeCommand.Execute(StartTime))
                .DisposedBy(DisposeBag);

            StopTimeArea.Rx().Tap()
                .Subscribe(_ => ViewModel.StopTimeEntryCommand.Execute(StopTime))
                .DisposedBy(DisposeBag);

            DurationArea.Rx().Tap()
                .Subscribe(_ => ViewModel.SelectTimeCommand.Execute(Duration))
                .DisposedBy(DisposeBag);

            ViewModel.ProjectTaskOrClientChanged
                     .WithLatestFrom(ViewModel.HasProject, (_, hasProject) => hasProject)
                     .Subscribe(onProjectTaskOrClientChanged)
                     .DisposedBy(DisposeBag);
        }

        private void onProjectTaskOrClientChanged(bool hasProject)
        {
            ProjectTaskClientTextView.TextFormatted = 
                TimeEntryExtensions.ToProjectTaskClient(
                    hasProject,
                    ViewModel.Project,
                    ViewModel.ProjectColor,
                    ViewModel.Task,
                    ViewModel.Client);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back)
            {
                ViewModel.CloseCommand.ExecuteAsync();
                return true;
            }

            return base.OnKeyDown(keyCode, e);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (!isDisposing) return;

            DisposeBag?.Dispose();
        }
    }
}
