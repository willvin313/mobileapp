using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Collections.Changes;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Sync;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.BroadcastReceivers;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.Helper;
using Toggl.Giskard.ViewHelpers;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Sync.SyncProgress;
using static Toggl.Giskard.Extensions.CircularRevealAnimation.AnimationType;
using FoundationResources = Toggl.Foundation.Resources;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class MainActivity : MvxAppCompatActivity<MainViewModel>, IReactiveBindingHolder
    {
        private const int snackbarDuration = 5000;
        private NotificationManager notificationManager;
        private MainRecyclerAdapter mainRecyclerAdapter;
        private LinearLayoutManager layoutManager;

        public CompositeDisposable DisposeBag { get; } = new CompositeDisposable();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.MainActivity);
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_fade_out);

            InitializeViews();

            SetSupportActionBar(FindViewById<Toolbar>(Resource.Id.Toolbar));
            SupportActionBar.SetDisplayShowHomeEnabled(false);
            SupportActionBar.SetDisplayShowTitleEnabled(false);

            runningEntryCardFrame.Visibility = ViewStates.Invisible;

            this.Bind(ViewModel.IsTimeEntryRunning, onTimeEntryCardVisibilityChanged);
            this.Bind(ViewModel.SyncProgressState, onSyncChanged);

            mainRecyclerAdapter = new MainRecyclerAdapter(ViewModel.TimeEntries)
            {
                SuggestionsViewModel = ViewModel.SuggestionsViewModel
            };

            this.Bind(mainRecyclerAdapter.TimeEntryTaps, ViewModel.SelectTimeEntry.Inputs);
            this.Bind(mainRecyclerAdapter.ContinueTimeEntrySubject, ViewModel.ContinueTimeEntry.Inputs);
            this.Bind(mainRecyclerAdapter.DeleteTimeEntrySubject, ViewModel.TimeEntriesViewModel.DelayDeleteTimeEntry.Inputs);
            this.Bind(ViewModel.TimeEntriesViewModel.ShouldShowUndo, showUndoDeletion);

            this.Bind(ViewModel.SyncProgressState, updateSyncingIndicator);
            this.Bind(refreshLayout.Rx().Refreshed(), ViewModel.RefreshAction);

            setupLayoutManager(mainRecyclerAdapter);

            var mainLogChange = ViewModel
                .TimeEntries
                .CollectionChange
                .ObserveOn(SynchronizationContext.Current);
            this.Bind(mainLogChange, mainRecyclerAdapter.UpdateChange);

            var isTimeEntryRunning = ViewModel
                .IsTimeEntryRunning
                .ObserveOn(SynchronizationContext.Current);
            this.Bind(isTimeEntryRunning, updateRecyclerViewPadding);

            notificationManager = GetSystemService(NotificationService) as NotificationManager;
            this.BindRunningTimeEntry(notificationManager, ViewModel.CurrentRunningTimeEntry, ViewModel.ShouldShowRunningTimeEntryNotification);
            this.BindIdleTimer(notificationManager, ViewModel.IsTimeEntryRunning, ViewModel.ShouldShowStoppedTimeEntryNotification);
            setupItemTouchHelper(mainRecyclerAdapter);

            this.Bind(ViewModel.TimeEntriesCount, timeEntriesCountSubject);

            ViewModel.ShouldReloadTimeEntryLog
                    .VoidSubscribe(reload)
                    .DisposedBy(DisposeBag);

            setupOnboardingSteps();
        }

        private void reload()
        {
            mainRecyclerAdapter.NotifyDataSetChanged();
        }

        private void setupLayoutManager(MainRecyclerAdapter mainAdapter)
        {
            layoutManager = new LinearLayoutManager(this);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 4;
            mainRecyclerView.SetLayoutManager(layoutManager);
            mainRecyclerView.SetAdapter(mainAdapter);
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
            var newPadding = isRunning ? 104.DpToPixels(this) : 70.DpToPixels(this);
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
                ViewModel.RefreshAction.Execute();
            }
        }

        private async void onTimeEntryCardVisibilityChanged(bool visible)
        {
            if (runningEntryCardFrame == null) return;

            var isCardVisible = runningEntryCardFrame.Visibility == ViewStates.Visible;
            if (isCardVisible == visible) return;

            var fabListener = new FabAsyncHideListener();
            var radialAnimation =
                runningEntryCardFrame
                    .AnimateWithCircularReveal()
                    .SetDuration(TimeSpan.FromSeconds(0.5))
                    .SetBehaviour((x, y, w, h) => (x, y + h, 0, w))
                    .SetType(() => visible ? Appear : Disappear);

            if (visible)
            {
                playButton.Hide(fabListener);
                await fabListener.HideAsync;

                radialAnimation
                    .OnAnimationEnd(_ => stopButton.Show())
                    .Start();
            }
            else
            {
                stopButton.Hide(fabListener);
                await fabListener.HideAsync;

                radialAnimation
                    .OnAnimationEnd(_ => playButton.Show())
                    .Start();
            }
        }

        private void showUndoDeletion(bool show)
        {
            if (!show)
                return;

            Snackbar.Make(coordinatorLayout, FoundationResources.EntryDeleted, snackbarDuration)
                .SetAction(FoundationResources.UndoButtonTitle, view => ViewModel.TimeEntriesViewModel.CancelDeleteTimeEntry.Execute())
                .Show();
        }

        private sealed class FabAsyncHideListener : FloatingActionButton.OnVisibilityChangedListener
        {
            private readonly TaskCompletionSource<object> hideTaskCompletionSource = new TaskCompletionSource<object>();

            public Task HideAsync => hideTaskCompletionSource.Task;

            public override void OnHidden(FloatingActionButton fab)
            {
                base.OnHidden(fab);
                hideTaskCompletionSource.SetResult(null);
            }
        }
    }
}
