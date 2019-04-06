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
using Toggl.Foundation.Models.Pomodoro;
using Android.Widget;
using Android.Graphics;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme.BlueStatusBar",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class PomodoroEditWorkflowActivity : ReactiveActivity<PomodoroEditWorkflowViewModel>
    {
        private readonly int inactiveResourceBackground = Resource.Drawable.GrayBorderRoundedRectangleWithWhiteBackground;
        private readonly int activeResourceBackground = Resource.Drawable.BlueButton;
        private readonly Color inactiveButtonTextColor = new Color(128, 128, 128);
        private readonly Color activeButtonTextColor = new Color(255, 255, 255);

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
            nameEditText.Text = ViewModel.Workflow.Name;

            updateUI(ViewModel.Workflow.Items.First());
        }

        private void setupBindings()
        {
            workflowView.Rx().SelectedIndex()
                .Subscribe(ViewModel.SelectItemByIndex.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.WorkflowItems
                .Subscribe(workflowView.Update)
                .DisposedBy(DisposeBag);

            ViewModel.SelectedWorkflowItemIndex
               .Subscribe(index => workflowView.SelectedWorkflowItemIndex = index)
               .DisposedBy(DisposeBag);

            ViewModel.SelectedWorkflowItem
                .WhereNotNull()
                .Subscribe(updateUI)
                .DisposedBy(DisposeBag);

            closeButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            durationSeekBar.Rx().Duration()
                .Subscribe(ViewModel.UpdateDuration.Inputs)
                .DisposedBy(DisposeBag);

            durationSeekBar.Rx().Duration()
                .Select(duration => $"{duration} min")
                .Subscribe(segmentDurationTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            addSegmentButton.Rx()
                .BindAction(ViewModel.AddWorkflowItem)
                .DisposedBy(DisposeBag);

            deleteSegmentButton.Rx()
                .BindAction(ViewModel.DeleteWorkflowItem)
                .DisposedBy(DisposeBag);

            workTypeButton.Rx().Tap()
                .SelectValue(PomodoroWorkflowItemType.Work)
                .Subscribe(ViewModel.UpdateCurrentWorkflowItemType.Inputs)
                .DisposedBy(DisposeBag);

            restTypeButton.Rx().Tap()
                .SelectValue(PomodoroWorkflowItemType.Rest)
                .Subscribe(ViewModel.UpdateCurrentWorkflowItemType.Inputs)
                .DisposedBy(DisposeBag);
        }

        private void updateUI(PomodoroWorkflowItem item)
        {
            durationSeekBar.Duration = item.Minutes;

            if (item.Type == PomodoroWorkflowItemType.Work)
            {
                workTypeButton.SetBackgroundResource(activeResourceBackground);
                workTypeButton.SetTextColor(activeButtonTextColor);
                restTypeButton.SetBackgroundResource(inactiveResourceBackground);
                restTypeButton.SetBackgroundColor(inactiveButtonTextColor);
            }
            else if (item.Type == PomodoroWorkflowItemType.Rest)
            {
                restTypeButton.SetBackgroundResource(activeResourceBackground);
                restTypeButton.SetTextColor(activeButtonTextColor);
                workTypeButton.SetBackgroundResource(inactiveResourceBackground);
                workTypeButton.SetBackgroundColor(inactiveButtonTextColor);
            }
        }
    }
}
