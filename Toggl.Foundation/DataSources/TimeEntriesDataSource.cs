using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Collections.Generic;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Models;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.ConflictResolution;

namespace Toggl.Foundation.DataSources
{
    internal sealed class TimeEntriesDataSource : ObservableDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry>, ITimeEntriesSource
    {
        private long? currentlyRunningTimeEntryId;
        private readonly ITimeService timeService;
        private readonly IAnalyticsService analyticsService;
        private readonly IRepository<IDatabaseTimeEntry> repository;

        private readonly Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode> alwaysCreate
            = (a, b) => ConflictResolutionMode.Create;

        private readonly Subject<IThreadSafeTimeEntry> timeEntryStartedSubject = new Subject<IThreadSafeTimeEntry>();
        private readonly Subject<IThreadSafeTimeEntry> timeEntryStoppedSubject = new Subject<IThreadSafeTimeEntry>();
        private readonly Subject<IThreadSafeTimeEntry> suggestionStartedSubject = new Subject<IThreadSafeTimeEntry>();
        private readonly Subject<IThreadSafeTimeEntry> timeEntryContinuedSubject = new Subject<IThreadSafeTimeEntry>();

        public IObservable<IThreadSafeTimeEntry> TimeEntryStarted { get; }
        public IObservable<IThreadSafeTimeEntry> TimeEntryStopped { get; }
        public IObservable<IThreadSafeTimeEntry> SuggestionStarted { get; }
        public IObservable<IThreadSafeTimeEntry> TimeEntryContinued { get; }

        public IObservable<bool> IsEmpty { get; }

        public IObservable<IThreadSafeTimeEntry> CurrentlyRunningTimeEntry { get; }

        protected override IRivalsResolver<IDatabaseTimeEntry> RivalsResolver { get; }

        public TimeEntriesDataSource(
            IRepository<IDatabaseTimeEntry> repository,
            ITimeService timeService,
            IAnalyticsService analyticsService)
            : base(repository)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(repository, nameof(repository));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.timeService = timeService;
            this.repository = repository;
            this.analyticsService = analyticsService;

            RivalsResolver = new TimeEntryRivalsResolver(timeService);

            CurrentlyRunningTimeEntry =
                getCurrentlyRunningTimeEntry()
                    .StartWith()
                    .Merge(Created.Where(te => te.IsRunning()))
                    .Merge(Updated.Where(update => update.Id == currentlyRunningTimeEntryId).Select(update => update.Entity))
                    .Merge(Deleted.Where(id => id == currentlyRunningTimeEntryId).Select(_ => null as IThreadSafeTimeEntry))
                    .Select(runningTimeEntry)
                    .ConnectedReplay();

            IsEmpty =
                Observable.Return(default(IThreadSafeTimeEntry))
                    .StartWith()
                    .Merge(Updated.Select(tuple => tuple.Entity))
                    .Merge(Created)
                    .SelectMany(_ => GetAll(te => te.IsDeleted == false))
                    .Select(timeEntries => timeEntries.None());

            RivalsResolver = new TimeEntryRivalsResolver(timeService);

            TimeEntryStarted = timeEntryStartedSubject.AsObservable();
            TimeEntryStopped = timeEntryStoppedSubject.AsObservable();
            SuggestionStarted = suggestionStartedSubject.AsObservable();
            TimeEntryContinued = timeEntryContinuedSubject.AsObservable();
        }

        public override IObservable<IThreadSafeTimeEntry> Create(IThreadSafeTimeEntry entity)
            => repository.UpdateWithConflictResolution(entity.Id, entity, alwaysCreate, RivalsResolver)  
                .ToThreadSafeResult(Convert)
                .SelectMany(CommonFunctions.Identity)
                .Do(HandleConflictResolutionResult)
                .OfType<CreateResult<IThreadSafeTimeEntry>>()
                .FirstAsync()
                .Select(result => result.Entity);

        public void OnTimeEntryStopped(IThreadSafeTimeEntry timeEntry)
        {
            timeEntryStoppedSubject.OnNext(timeEntry);
        }

        public void OnTimeEntrySoftDeleted(IThreadSafeTimeEntry timeEntry)
        {
            DeletedSubject.OnNext(timeEntry.Id);
        }

        public void OnTimeEntryStarted(IThreadSafeTimeEntry timeEntry, TimeEntryStartOrigin origin)
        {
            switch (origin)
            {
                case TimeEntryStartOrigin.ContinueMostRecent:
                    timeEntryContinuedSubject.OnNext(timeEntry);
                    break;

                case TimeEntryStartOrigin.Manual:
                case TimeEntryStartOrigin.Timer:
                    timeEntryStartedSubject.OnNext(timeEntry);
                    break;

                case TimeEntryStartOrigin.Suggestion:
                    suggestionStartedSubject.OnNext(timeEntry);
                    break;
            }
        }

        protected override IThreadSafeTimeEntry Convert(IDatabaseTimeEntry entity)
            => TimeEntry.From(entity);

        protected override ConflictResolutionMode ResolveConflicts(IDatabaseTimeEntry first, IDatabaseTimeEntry second)
            => Resolver.ForTimeEntries.Resolve(first, second);

        private IThreadSafeTimeEntry runningTimeEntry(IThreadSafeTimeEntry timeEntry)
        {
            timeEntry = timeEntry != null && timeEntry.IsRunning() ? timeEntry : null;
            currentlyRunningTimeEntryId = timeEntry?.Id;
            return timeEntry;
        }

        private IObservable<IThreadSafeTimeEntry> getCurrentlyRunningTimeEntry()
            => stopMultipleRunningTimeEntries()
                .SelectMany(_ => getAllRunning())
                .SelectMany(CommonFunctions.Identity)
                .SingleOrDefaultAsync();

        private IObservable<Unit> stopMultipleRunningTimeEntries()
            => getAllRunning()
                .Where(list => list.Count() > 1)
                .SelectMany(BatchUpdate)
                .Track(analyticsService.TwoRunningTimeEntriesInconsistencyFixed)
                .ToList()
                .SelectUnit()
                .DefaultIfEmpty(Unit.Default);

        private IObservable<IEnumerable<IThreadSafeTimeEntry>> getAllRunning()
            => GetAll(te => te.IsDeleted == false && te.Duration == null);
    }
}
