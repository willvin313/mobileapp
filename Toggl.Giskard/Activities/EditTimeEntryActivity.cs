﻿using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Multivac.Extensions;
using Toggl.Foundation.Extensions;
using Android.Text;
using Android.Support.V7.Widget;
using System.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.UI.Transformations;
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
    public sealed partial class EditTimeEntryActivity : ReactiveActivity<EditTimeEntryViewModel>
    {
        private TagsAdapter tagsAdapter = new TagsAdapter(Resource.Layout.EditTimeEntryTagCell, StringViewHolder.Create);

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.EditTimeEntryActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            InitializeViews();

            setupViews();
            setupBindings();
        }

        protected override void OnResume()
        {
            base.OnResume();
            resetOnboardingOnResume();
        }

        protected override void OnStop()
        {
            base.OnStop();
            clearOnboardingOnStop();
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
            singleTimeEntryModeViews.Visibility = (!ViewModel.IsEditingGroup).ToVisibility();
            timeEntriesGroupModeViews.Visibility = ViewModel.IsEditingGroup.ToVisibility();

            descriptionEditText.Text = ViewModel.Description.Value;

            groupCountTextView.Text = string.Format(
                TextResources.EditingTimeEntryGroup,
                ViewModel.GroupCount);

            var layoutManager = new LinearLayoutManager(this, LinearLayoutManager.Horizontal, false);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 5;
            tagsRecycler.SetLayoutManager(layoutManager);
            tagsRecycler.SetAdapter(tagsAdapter);

            deleteLabel.Text = ViewModel.IsEditingGroup
                ? string.Format(TextResources.DeleteNTimeEntries, ViewModel.GroupCount)
                : TextResources.DeleteThisEntry;
        }

        private void setupBindings()
        {
            closeButton.Rx().Tap()
                .Subscribe(ViewModel.Close.Inputs)
                .DisposedBy(DisposeBag);

            confirmButton.Rx().Tap()
                .Select(_ => descriptionEditText.HasFocus)
                .Subscribe(handleConfirmClick)
                .DisposedBy(DisposeBag);

            descriptionEditText.Rx().FocusChanged()
                .Select(isFocused => isFocused ? Done : Save)
                .Select(Resources.GetString)
                .Subscribe(confirmButton.Rx().TextObserver());

            descriptionEditText.Rx().Text()
                .Subscribe(ViewModel.Description.Accept)
                .DisposedBy(DisposeBag);

            errorContainer.Rx().Tap()
                .Subscribe(ViewModel.DismissSyncErrorMessage.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.SyncErrorMessage
                .Subscribe(errorText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsSyncErrorMessageVisible
                .Subscribe(errorContainer.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.Preferences
                .Select(preferences => preferences.DurationFormat)
                .Select(format => ViewModel.GroupDuration.ToFormattedString(format))
                .Subscribe(groupDurationTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsInaccessible
               .Subscribe(adjustUIForInaccessibleTimeEntry)
               .DisposedBy(DisposeBag);

            ViewModel.ProjectClientTask
                .Select(generateProjectTaskClientFormattedString)
                .Subscribe(projectTaskClientTextView.Rx().TextFormattedObserver())
                .DisposedBy(DisposeBag);

            projectButton.Rx().Tap()
                .Subscribe(ViewModel.SelectProject.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.Tags
                .Select(tags => tags.ToArray())
                .Subscribe(tagsAdapter.Rx().Items())
                .DisposedBy(DisposeBag);

            tagsButton.Rx().Tap()
                .Subscribe(ViewModel.SelectTags.Inputs)
                .DisposedBy(DisposeBag);

            Observable.CombineLatest(
                    tagsAdapter.ItemTapObservable, ViewModel.IsInaccessible,
                    (tap, isInaccessible) => isInaccessible)
                .Where(isInacessible => !isInacessible)
                .SelectUnit()
                .Subscribe(ViewModel.SelectTags.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.IsBillableAvailable
                .Subscribe(billableRelatedViews.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsBillable
                .Subscribe(billableSwitch.Rx().CheckedObserver())
                .DisposedBy(DisposeBag);

            billableButton.Rx().Tap()
                .Subscribe(ViewModel.ToggleBillable.Inputs)
                .DisposedBy(DisposeBag);

            billableSwitch.Rx().Checked()
                .SelectUnit()
                .Subscribe(ViewModel.ToggleBillable.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.StartTime
                .WithLatestFrom(ViewModel.Preferences,
                    (startTime, preferences) => DateTimeToFormattedString.Convert(startTime, preferences.TimeOfDayFormat.Format))
                .Subscribe(startTimeTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.StartTime
                .WithLatestFrom(ViewModel.Preferences,
                    (startTime, preferences) => DateTimeToFormattedString.Convert(startTime, preferences.DateFormat.Short))
                .Subscribe(startDateTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            changeStartTimeButton.Rx().Tap()
               .SelectValue(EditViewTapSource.StartTime)
               .Subscribe(ViewModel.EditTimes.Inputs)
               .DisposedBy(DisposeBag);

            var stopTimeObservable = ViewModel.StopTime
                .Where(stopTime => stopTime.HasValue)
                .Select(stopTime => stopTime.Value);

            stopTimeObservable
                .WithLatestFrom(ViewModel.Preferences,
                    (stopTime, preferences) => DateTimeToFormattedString.Convert(stopTime, preferences.TimeOfDayFormat.Format))
                .Subscribe(stopTimeTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            stopTimeObservable
                .WithLatestFrom(ViewModel.Preferences,
                    (stopTime, preferences) => DateTimeToFormattedString.Convert(stopTime, preferences.DateFormat.Short))
                .Subscribe(stopDateTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            changeStopTimeButton.Rx().Tap()
                .WithLatestFrom(ViewModel.IsTimeEntryRunning, (_, isTimeEntryRunning) => !isTimeEntryRunning)
                .Where(CommonFunctions.Identity)
                .SelectValue(EditViewTapSource.StopTime)
                .Subscribe(ViewModel.EditTimes.Inputs)
                .DisposedBy(DisposeBag);

            changeStopTimeButton.Rx().Tap()
                .WithLatestFrom(ViewModel.IsTimeEntryRunning, (_, isTimeEntryRunning) => isTimeEntryRunning)
                .Where(CommonFunctions.Identity)
                .SelectUnit()
                .Subscribe(ViewModel.StopTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .Subscribe(stopTimeEntryButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .Select(isRunning => !isRunning && !ViewModel.IsEditingGroup)
                .Subscribe(stoppedTimeEntryStopTimeElements.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.Duration
                .WithLatestFrom(ViewModel.Preferences,
                    (duration, preferences) => duration.ToFormattedString(preferences.DurationFormat))
                .Subscribe(durationTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            changeDurationButton.Rx().Tap()
                .SelectValue(EditViewTapSource.Duration)
                .Subscribe(ViewModel.EditTimes.Inputs)
                .DisposedBy(DisposeBag);

            deleteButton.Rx().Tap()
                .Subscribe(ViewModel.Delete.Inputs)
                .DisposedBy(DisposeBag);
        }

        private void adjustUIForInaccessibleTimeEntry(bool isInaccessible)
        {
            descriptionEditText.Enabled = !isInaccessible;

            changeStartTimeButton.Enabled = !isInaccessible;
            changeStopTimeButton.Enabled = !isInaccessible;
            changeDurationButton.Enabled = !isInaccessible;
            stopTimeEntryButton.Enabled = !isInaccessible;

            billableButton.Enabled = !isInaccessible;
            billableSwitch.Enabled = !isInaccessible;

            tagsButton.Enabled = !isInaccessible;
        }

        private void handleConfirmClick(bool hasFocus)
        {
            if (hasFocus)
            {
                descriptionEditText.RemoveFocus();
            }
            else
            {
                ViewModel.Save.Execute();
            }
        }
        
        private ISpannable generateProjectTaskClientFormattedString(EditTimeEntryViewModel.ProjectClientTaskInfo projectClientTask)
            => TimeEntryExtensions.ToProjectTaskClient(
                    projectClientTask.HasProject,
                    projectClientTask.Project,
                    projectClientTask.ProjectColor,
                    projectClientTask.Task,
                    projectClientTask.Client);
    }
}
