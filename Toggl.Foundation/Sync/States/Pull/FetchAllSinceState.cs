using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Sync.States.Pull
{
    internal sealed class FetchAllSinceState : ISyncState
    {
        private readonly ITogglDatabase database;
        private readonly ITogglApi api;
        private readonly ITimeService timeService;
        private const int sinceDateLimitMonths = 2;
        private const int fetchTimeEntriesForMonths = 2;
        private const int timeEntriesEndDateInclusiveExtraDaysCount = 2;

        public StateResult<IFetchObservables> FetchStarted { get; } = new StateResult<IFetchObservables>();

        public FetchAllSinceState(ITogglDatabase database, ITogglApi api, ITimeService timeService)
        {
            this.database = database;
            this.api = api;
            this.timeService = timeService;
        }

        public IObservable<ITransition> Start() => Observable.Create<ITransition>(observer =>
        {
            var since = database.SinceParameters;
            var observables = new FetchObservables(
                api.Workspaces.GetAll().ConnectedReplay(),
                api.WorkspaceFeatures.GetAll().ConnectedReplay(),
                api.User.Get().ConnectedReplay(),
                getSinceOrAll(since.Get<IDatabaseClient>(), api.Clients.GetAllSince, api.Clients.GetAll).ConnectedReplay(),
                getSinceOrAll(since.Get<IDatabaseProject>(), api.Projects.GetAllSince, api.Projects.GetAll).ConnectedReplay(),
                getSinceOrAll(since.Get<IDatabaseTimeEntry>(), api.TimeEntries.GetAllSince, fetchTwoMonthsOfTimeEntries).ConnectedReplay(),
                getSinceOrAll(since.Get<IDatabaseTag>(), api.Tags.GetAllSince, api.Tags.GetAll).ConnectedReplay(),
                getSinceOrAll(since.Get<IDatabaseTask>(), api.Tasks.GetAllSince, api.Tasks.GetAll).ConnectedReplay(),
                api.Preferences.Get().ConnectedReplay()
            );

            observer.OnNext(FetchStarted.Transition(observables));
            observer.OnCompleted();

            return () => { };
        });

        private IObservable<List<ITimeEntry>> fetchTwoMonthsOfTimeEntries()
            => api.TimeEntries.GetAll(
                start: timeService.CurrentDateTime.AddMonths(-fetchTimeEntriesForMonths),
                end: timeService.CurrentDateTime.AddDays(timeEntriesEndDateInclusiveExtraDaysCount));

        private IObservable<T> getSinceOrAll<T>(DateTimeOffset? threshold,
            Func<DateTimeOffset, IObservable<T>> since, Func<IObservable<T>> all)
            => threshold.HasValue && isWithinLimit(threshold.Value) ? since(threshold.Value) : all();

        private bool isWithinLimit(DateTimeOffset threshold)
            => threshold > timeService.CurrentDateTime.AddMonths(-sinceDateLimitMonths);
    }
}
