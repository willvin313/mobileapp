using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Text;
using Android.Views;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;
using Toggl.Foundation.Sync;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.Helper;
using Toggl.Giskard.Services;
using Toggl.Giskard.ViewHelpers;
using Toggl.Multivac.Extensions;
using static Android.Content.Context;
using static Toggl.Foundation.Sync.SyncProgress;
using static Toggl.Giskard.Extensions.CircularRevealAnimation.AnimationType;
using static Toggl.Giskard.Extensions.FloatingActionButtonExtensions;
using FoundationResources = Toggl.Foundation.Resources;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class MainFragment : ReactiveFragment<MainViewModel>
    {
        private const int snackbarDuration = 5000;
        private NotificationManager notificationManager;
        private MainRecyclerAdapter mainRecyclerAdapter;
        private LinearLayoutManager layoutManager;
        private FirebaseStopwatchProviderAndroid localStopwatchProvider = new FirebaseStopwatchProviderAndroid();
        private CancellationTokenSource cardAnimationCancellation;
        private bool shouldShowRatingViewOnResume;
        private ISubject<bool> visibilityChangedSubject = new BehaviorSubject<bool>(false);
        private IObservable<bool> visibilityChanged => visibilityChangedSubject.AsObservable();

        private Drawable addDrawable;
        private Drawable playDrawable;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var onCreateStopwatch = localStopwatchProvider.Create(MeasuredOperation.MainActivityOnCreate);
            onCreateStopwatch.Start();

            var view = inflater.Inflate(Resource.Layout.MainFragment, container, false);

            InitializeViews(view);
            setupToolbar();

            runningEntryCardFrame.Visibility = ViewStates.Invisible;

            stopButton.Rx().BindAction(ViewModel.StopTimeEntry, _ => TimeEntryStopOrigin.Manual).DisposedBy(DisposeBag);

            playButton.Rx().BindAction(ViewModel.StartTimeEntry, _ => true).DisposedBy(DisposeBag);
            playButton.Rx().BindAction(ViewModel.StartTimeEntry, _ => false, ButtonEventType.LongPress).DisposedBy(DisposeBag);

            timeEntryCard.Rx().Tap()
                .WithLatestFrom(ViewModel.CurrentRunningTimeEntry,
                    (_, te) => (new[] { te.Id }, EditTimeEntryOrigin.RunningTimeEntryCard))
                .Subscribe(ViewModel.SelectTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.ElapsedTime
                .Subscribe(timeEntryCardTimerLabel.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentRunningTimeEntry
                .Select(te => te?.Description ?? "")
                .Subscribe(timeEntryCardDescriptionLabel.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentRunningTimeEntry
                .Select(te => string.IsNullOrWhiteSpace(te?.Description))
                .Subscribe(timeEntryCardAddDescriptionLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentRunningTimeEntry
                .Select(te => string.IsNullOrWhiteSpace(te?.Description))
                .Invert()
                .Subscribe(timeEntryCardDescriptionLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentRunningTimeEntry
                .Select(CreateProjectClientTaskLabel)
                .Subscribe(timeEntryCardProjectClientTaskLabel.Rx().TextFormattedObserver())
                .DisposedBy(DisposeBag);

            var projectVisibilityObservable = ViewModel.CurrentRunningTimeEntry
                .Select(te => te?.Project != null);

            projectVisibilityObservable
                .Subscribe(timeEntryCardProjectClientTaskLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            projectVisibilityObservable
                .Subscribe(timeEntryCardDotContainer.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            var projectColorObservable = ViewModel.CurrentRunningTimeEntry
                .Select(te => te?.Project?.Color ?? "#000000")
                .Select(Color.ParseColor);

            projectColorObservable
                .Subscribe(timeEntryCardDotView.Rx().DrawableColor())
                .DisposedBy(DisposeBag);

            addDrawable = ContextCompat.GetDrawable(Context, Resource.Drawable.add_white);
            playDrawable = ContextCompat.GetDrawable(Context, Resource.Drawable.play_white);

            ViewModel.IsInManualMode
                .Select(isManualMode => isManualMode ? addDrawable : playDrawable)
                .Subscribe(playButton.SetDrawableImageSafe)
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .Subscribe(onTimeEntryCardVisibilityChanged)
                .DisposedBy(DisposeBag);

            ViewModel.SyncProgressState
                .Subscribe(onSyncChanged)
                .DisposedBy(DisposeBag);

            mainRecyclerAdapter = new MainRecyclerAdapter(ViewModel.TimeService)
            {
                SuggestionsViewModel = ViewModel.SuggestionsViewModel,
                RatingViewModel = ViewModel.RatingViewModel,
                StopwatchProvider = localStopwatchProvider
            };
            mainRecyclerAdapter.SetupRatingViewVisibility(shouldShowRatingViewOnResume);

            setupRecycler();

            mainRecyclerAdapter.ToggleGroupExpansion
                .Subscribe(ViewModel.TimeEntriesViewModel.ToggleGroupExpansion.Inputs)
                .DisposedBy(DisposeBag);

            mainRecyclerAdapter.TimeEntryTaps
                .Select(editEventInfo)
                .Subscribe(ViewModel.SelectTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            mainRecyclerAdapter.ContinueTimeEntry
                .Select(vm => (vm.LogItem.RepresentedTimeEntriesIds.First(), vm.ContinueMode))
                .Subscribe(ViewModel.ContinueTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            mainRecyclerAdapter.DeleteTimeEntrySubject
                .Select(vm => vm.RepresentedTimeEntriesIds)
                .Subscribe(ViewModel.TimeEntriesViewModel.DelayDeleteTimeEntries.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.TimeEntriesViewModel.TimeEntriesPendingDeletion
                .Subscribe(showUndoDeletion)
                .DisposedBy(DisposeBag);

            ViewModel.SyncProgressState
                .Subscribe(updateSyncingIndicator)
                .DisposedBy(DisposeBag);

            refreshLayout.Rx().Refreshed()
                 .Subscribe(ViewModel.Refresh.Inputs)
                 .DisposedBy(DisposeBag);

            ViewModel.TimeEntries
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(mainRecyclerAdapter.UpdateCollection)
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(updateRecyclerViewPadding)
                .DisposedBy(DisposeBag);

            notificationManager = Activity.GetSystemService(NotificationService) as NotificationManager;
            Activity.BindRunningTimeEntry(notificationManager, ViewModel.CurrentRunningTimeEntry, ViewModel.ShouldShowRunningTimeEntryNotification)
                .DisposedBy(DisposeBag);
            Activity.BindIdleTimer(notificationManager, ViewModel.IsTimeEntryRunning, ViewModel.ShouldShowStoppedTimeEntryNotification)
                .DisposedBy(DisposeBag);
            setupItemTouchHelper(mainRecyclerAdapter);

            ViewModel.TimeEntriesCount
                .Subscribe(timeEntriesCountSubject)
                .DisposedBy(DisposeBag);

            ViewModel.ShouldReloadTimeEntryLog
                .Subscribe(reload)
                .DisposedBy(DisposeBag);

            ViewModel.ShouldShowWelcomeBack
                .Subscribe(onWelcomeBackViewVisibilityChanged)
                .DisposedBy(DisposeBag);

            ViewModel.ShouldShowEmptyState
                .Subscribe(onEmptyStateVisibilityChanged)
                .DisposedBy(DisposeBag);

            ViewModel.ShouldShowRatingView
                .Subscribe(setupRatingViewVisibility)
                .DisposedBy(DisposeBag);

            setupOnboardingSteps();
            onCreateStopwatch.Stop();

            return view;
        }

        public ISpannable CreateProjectClientTaskLabel(IThreadSafeTimeEntry te)
        {
            if (te == null)
                return new SpannableString(string.Empty);

            var hasProject = te.ProjectId != null;
            return Extensions.TimeEntryExtensions.ToProjectTaskClient(hasProject, te.Project?.Name, te.Project?.Color, te.Task?.Name, te.Project?.Client?.Name);
        }

        private void setupRatingViewVisibility(bool isVisible)
        {
            mainRecyclerAdapter.SetupRatingViewVisibility(isVisible);
            shouldShowRatingViewOnResume = isVisible;
        }

        public void SetFragmentIsVisible(bool isVisible)
        {
            visibilityChangedSubject.OnNext(isVisible);
        }

        private void reload()
        {
            mainRecyclerAdapter.NotifyDataSetChanged();
        }

        private void setupRecycler()
        {
            layoutManager = new LinearLayoutManager(Context);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 4;
            mainRecyclerView.SetLayoutManager(layoutManager);
            mainRecyclerView.SetAdapter(mainRecyclerAdapter);
        }

        private void setupItemTouchHelper(MainRecyclerAdapter mainAdapter)
        {
            var callback = new MainRecyclerViewTouchCallback(mainAdapter);
            var itemTouchHelper = new ItemTouchHelper(callback);
            itemTouchHelper.AttachToRecyclerView(mainRecyclerView);
        }

        private void updateSyncingIndicator(SyncProgress state)
        {
            refreshLayout.Refreshing = state == Syncing;
        }

        private void updateRecyclerViewPadding(bool isRunning)
        {
            var newPadding = isRunning ? 104.DpToPixels(Context) : 70.DpToPixels(Context);
            mainRecyclerView.SetPadding(0, 0, 0, newPadding);
        }

        private void onSyncChanged(SyncProgress syncProgress)
        {
            switch (syncProgress)
            {
                case Failed:
                case Unknown:
                case OfflineModeDetected:

                    var errorMessage = syncProgress == OfflineModeDetected
                                     ? FoundationResources.Offline
                                     : FoundationResources.SyncFailed;

                    var snackbar = Snackbar.Make(coordinatorLayout, errorMessage, Snackbar.LengthLong)
                        .SetAction(FoundationResources.TapToRetry, onRetryTapped);
                    snackbar.SetDuration(snackbarDuration);
                    snackbar.Show();
                    break;
            }

            void onRetryTapped(View view)
            {
                ViewModel.Refresh.Execute();
            }
        }

        private (long[], EditTimeEntryOrigin) editEventInfo(LogItemViewModel item)
        {
            var origin = item.IsTimeEntryGroupHeader
                ? EditTimeEntryOrigin.GroupHeader
                : item.BelongsToGroup
                    ? EditTimeEntryOrigin.GroupTimeEntry
                    : EditTimeEntryOrigin.SingleTimeEntry;

            return (item.RepresentedTimeEntriesIds, origin);
        }

        private void onTimeEntryCardVisibilityChanged(bool visible)
        {
            cardAnimationCancellation?.Cancel();
            if (runningEntryCardFrame == null) return;

            var isCardVisible = runningEntryCardFrame.Visibility == ViewStates.Visible;
            if (isCardVisible == visible) return;

            cardAnimationCancellation = new CancellationTokenSource();

            var buttonToHide = visible ? playButton : stopButton;
            var buttonToShow = visible ? stopButton : playButton;

            var radialAnimation =
                runningEntryCardFrame
                    .AnimateWithCircularReveal()
                    .WithCancellationToken(cardAnimationCancellation.Token)
                    .SetDuration(TimeSpan.FromSeconds(0.5))
                    .SetBehaviour((x, y, w, h) => (x, y + h, 0, w))
                    .SetType(() => visible ? Appear : Disappear);

            buttonToHide.Hide(((Action)onFabHidden).ToFabVisibilityListener());

            void onFabHidden()
            {
                radialAnimation
                    .OnAnimationEnd(_ => buttonToShow.Show())
                    .OnAnimationCancel(buttonToHide.Show)
                    .Start();
            }
        }

        private void onEmptyStateVisibilityChanged(bool shouldShowEmptyState)
        {
            if (shouldShowEmptyState)
            {
                if (emptyStateView == null)
                {
                    emptyStateView = emptyStateViewStub.Inflate();
                }

                emptyStateView.Visibility = ViewStates.Visible;
            }
            else if (emptyStateView != null)
            {
                emptyStateView.Visibility = ViewStates.Gone;
            }
        }

        private void showUndoDeletion(int? numberOfTimeEntriesPendingDeletion)
        {
            if (!numberOfTimeEntriesPendingDeletion.HasValue)
                return;

            var undoText = numberOfTimeEntriesPendingDeletion > 1
                ? String.Format(FoundationResources.MultipleEntriesDeleted, numberOfTimeEntriesPendingDeletion)
                : FoundationResources.EntryDeleted;

            Snackbar.Make(coordinatorLayout, undoText, snackbarDuration)
                .SetAction(FoundationResources.UndoButtonTitle, view => ViewModel.TimeEntriesViewModel.CancelDeleteTimeEntry.Execute())
                .Show();
        }

        private void onWelcomeBackViewVisibilityChanged(bool shouldShowWelcomeBackView)
        {
            if (shouldShowWelcomeBackView)
            {
                if (welcomeBackView == null)
                {
                    welcomeBackView = welcomeBackStub.Inflate();
                }

                welcomeBackView.Visibility = ViewStates.Visible;
            }
            else if (welcomeBackView != null)
            {
                welcomeBackView.Visibility = ViewStates.Gone;
            }
        }

        private void setupToolbar()
        {
            var activity = Activity as AppCompatActivity;
            toolbar.Title = "";
            activity.SetSupportActionBar(toolbar);
        }
    }
}
