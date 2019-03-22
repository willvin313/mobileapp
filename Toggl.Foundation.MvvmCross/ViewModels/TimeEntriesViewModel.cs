using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class TimeEntriesViewModel
    {
        private readonly IInteractorFactory interactorFactory;
        private readonly IAnalyticsService analyticsService;
        private readonly ISchedulerProvider schedulerProvider;

        private readonly TimeEntriesGroupsFlattening groupsFlatteningStrategy;

        private readonly Subject<int?> timeEntriesPendingDeletionSubject = new Subject<int?>();
        private readonly HashSet<long> timeEntriesToDelete = new HashSet<long>();

        private DelayedInteractorExecution<IObservable<Unit>> delayedDeletion;

        public IObservable<IEnumerable<AnimatableSectionModel<DaySummaryViewModel, LogItemViewModel, IMainLogKey>>> TimeEntries { get; }
        public IObservable<bool> Empty { get; }
        public IObservable<int> Count { get; }
        public IObservable<int?> TimeEntriesPendingDeletion { get; }

        public InputAction<long[]> DelayDeleteTimeEntries { get; }
        public InputAction<GroupId> ToggleGroupExpansion { get; }
        public UIAction CancelDeleteTimeEntry { get; }

        public TimeEntriesViewModel(
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            IAnalyticsService analyticsService,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory,
            ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

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
            if (delayedDeletion != null)
            {
                await delayedDeletion?.ExecuteImmediately();
                delayedDeletion = null;

                timeEntriesPendingDeletionSubject.OnNext(null);
            }
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
            if (delayedDeletion != null)
            {
                delayedDeletion.ExecuteImmediately().Subscribe();
                analyticsService.DeleteTimeEntry.Track();
            }

            timeEntriesPendingDeletionSubject.OnNext(timeEntries.Length);
            timeEntriesToDelete.AddRange(timeEntries);

            delayedDeletion = new DelayedInteractorExecution<IObservable<Unit>>(
                interactorFactory.SoftDeleteMultipleTimeEntries(timeEntries),
                Constants.UndoTime,
                schedulerProvider.BackgroundScheduler);

            delayedDeletion.Execute()
                .Track(analyticsService.DeleteTimeEntry)
                .Do(dismissUndo)
                .Do(observable => observable.Subscribe())
                .Subscribe();
        }

        private void cancelDeleteTimeEntry()
        {
            delayedDeletion?.Cancel();
            delayedDeletion = null;

            dismissUndo();
        }

        private void dismissUndo()
        {
            timeEntriesToDelete.Clear();
            timeEntriesPendingDeletionSubject.OnNext(null);
        }

        private bool isNotRunning(IThreadSafeTimeEntry timeEntry) => !timeEntry.IsRunning();
        private bool isNotDeleted(IThreadSafeTimeEntry timeEntry) => !timeEntriesToDelete.Contains(timeEntry.Id);
    }
}
