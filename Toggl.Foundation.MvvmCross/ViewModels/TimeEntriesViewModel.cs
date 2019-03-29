using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Helper;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Transformations;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog.Identity;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{

    [Preserve(AllMembers = true)]
    public sealed class TimeEntriesViewModel
    {
        private readonly ISyncManager syncManager;
        private readonly IInteractorFactory interactorFactory;
        private readonly IAnalyticsService analyticsService;
        private readonly ISchedulerProvider schedulerProvider;

        private readonly TimeEntriesGroupsFlattening groupsFlatteningStrategy;

        private readonly HashSet<long> hiddenTimeEntries = new HashSet<long>();
        private Subject<int?> timeEntriesPendingDeletionSubject = new Subject<int?>();
        private IDisposable delayedDeletionDisposable;
        private long[] timeEntriesToDelete;

        public IObservable<IEnumerable<AnimatableSectionModel<DaySummaryViewModel, LogItemViewModel, IMainLogKey>>> TimeEntries { get; }
        public IObservable<bool> Empty { get; }
        public IObservable<int> Count { get; }
        public IObservable<int?> TimeEntriesPendingDeletion { get; }

        public InputAction<long[]> DelayDeleteTimeEntries { get; }
        public InputAction<GroupId> ToggleGroupExpansion { get; }
        public UIAction CancelDeleteTimeEntry { get; }

        public TimeEntriesViewModel(
            ITogglDataSource dataSource,
            ISyncManager syncManager,
            IInteractorFactory interactorFactory,
            IAnalyticsService analyticsService,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory,
            ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.syncManager = syncManager;
            this.interactorFactory = interactorFactory;
            this.analyticsService = analyticsService;
            this.schedulerProvider = schedulerProvider;

            DelayDeleteTimeEntries = rxActionFactory.FromAction<long[]>(delayDeleteTimeEntries);
            ToggleGroupExpansion = rxActionFactory.FromAction<GroupId>(toggleGroupExpansion);
            CancelDeleteTimeEntry = rxActionFactory.FromAction(cancelDeleteTimeEntry);

            groupsFlatteningStrategy = new TimeEntriesGroupsFlattening(timeService);

            var deletingOrPressingUndo = timeEntriesPendingDeletionSubject.SelectUnit();
            var collapsingOrExpanding = ToggleGroupExpansion.Elements;

            var visibleTimeEntries = interactorFactory.ObserveAllTimeEntriesVisibleToTheUser().Execute()
                .Select(timeEntries => timeEntries.Where(isNotRunning))
                .ReemitWhen(deletingOrPressingUndo)
                .Select(timeEntries => timeEntries.Where(isNotDeleted))
                .Select(group)
                .ReemitWhen(collapsingOrExpanding);

             TimeEntries = Observable.CombineLatest(visibleTimeEntries, dataSource.Preferences.Current, groupsFlatteningStrategy.Flatten)
                    .AsDriver(schedulerProvider);

            Empty = TimeEntries
                .Select(groups => groups.None())
                .AsDriver(schedulerProvider);

            Count = TimeEntries
                .Select(log => log.Sum(day => day.Items.Count))
                .AsDriver(schedulerProvider);

            TimeEntriesPendingDeletion = timeEntriesPendingDeletionSubject.AsObservable().AsDriver(schedulerProvider);
        }

        public async Task FinalizeDelayDeleteTimeEntryIfNeeded()
        {
            if (timeEntriesToDelete == null)
            {
                return;
            }

            delayedDeletionDisposable.Dispose();
            await deleteTimeEntries(timeEntriesToDelete);
            timeEntriesToDelete = null;
            timeEntriesPendingDeletionSubject.OnNext(null);
        }

        private IEnumerable<IGrouping<DateTime, IThreadSafeTimeEntry>> group(
            IEnumerable<IThreadSafeTimeEntry> timeEntries)
            => timeEntries
                .OrderByDescending(te => te.Start)
                .GroupBy(te => te.Start.LocalDateTime.Date);

        private void toggleGroupExpansion(GroupId groupId)
        {
            groupsFlatteningStrategy.ToggleGroupExpansion(groupId);
        }

        private void delayDeleteTimeEntries(long[] timeEntries)
        {
            timeEntriesToDelete = timeEntries;
            hiddenTimeEntries.AddRange(timeEntries);

            timeEntriesPendingDeletionSubject.OnNext(timeEntries.Length);

            delayedDeletionDisposable = Observable.Merge( // If 5 seconds pass or we try to delete another TE
                    Observable.Return(timeEntries).Delay(Constants.UndoTime, schedulerProvider.DefaultScheduler),
                    timeEntriesPendingDeletionSubject
                        .Where(numberOfDeletedTimeEntries => numberOfDeletedTimeEntries != null)
                        .SelectValue(timeEntries)
                )
                .Take(1)
                .SelectMany(deleteTimeEntries)
                .Do(deletedTimeEntries =>
                {
                    // Hide bar if there isn't other TE trying to be deleted
                    if (deletedTimeEntries == timeEntriesToDelete)
                    {
                        hiddenTimeEntries.Clear();
                        timeEntriesPendingDeletionSubject.OnNext(null);
                    }
                })
                .Subscribe();
        }

        private void cancelDeleteTimeEntry()
        {
            timeEntriesToDelete = null;
            hiddenTimeEntries.Clear();
            delayedDeletionDisposable.Dispose();
            timeEntriesPendingDeletionSubject.OnNext(null);
        }

        private IObservable<long[]> deleteTimeEntries(long[] timeEntries)
        {
            var observables =
                interactorFactory.SoftDeleteMultipleTimeEntries(timeEntries)
                    .Execute()
                    .Track(analyticsService.DeleteTimeEntry);

            return observables.SelectValue(timeEntries);
        }

        private bool isNotRunning(IThreadSafeTimeEntry timeEntry) => !timeEntry.IsRunning();

        private bool isNotDeleted(IThreadSafeTimeEntry timeEntry)
            => !hiddenTimeEntries.Contains(timeEntry.Id);
    }
}
