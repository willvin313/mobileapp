using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Login;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Services
{
    public sealed class AutomaticSyncingService : IAutomaticSyncingService
    {
        public static TimeSpan MinimumTimeInBackgroundForFullSync { get; } = TimeSpan.FromMinutes(5);

        private readonly IBackgroundService backgroundService;
        private readonly ITimeService timeService;

        private CompositeDisposable syncingDisposeBag;

        public AutomaticSyncingService(
            IBackgroundService backgroundService,
            ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.backgroundService = backgroundService;
            this.timeService = timeService;
        }

        public void Start(ISyncManager syncManager)
        {
            Stop();

            backgroundService.AppResumedFromBackground
                .Where(timeInBackground => timeInBackground >= MinimumTimeInBackgroundForFullSync)
                .SelectMany(_ => syncManager.ForceFullSync())
                .Subscribe()
                .DisposedBy(syncingDisposeBag);

            timeService.MidnightObservable
                .Subscribe(_ => syncManager.CleanUp())
                .DisposedBy(syncingDisposeBag);
        }

        public void Stop()
        {
            syncingDisposeBag?.Dispose();
            syncingDisposeBag = new CompositeDisposable();
        }
    }
}
