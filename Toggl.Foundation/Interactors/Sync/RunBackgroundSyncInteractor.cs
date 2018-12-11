using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Models;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Notification = Toggl.Multivac.Notification;

namespace Toggl.Foundation.Interactors
{
    public class RunBackgroundSyncInteractor : IInteractor<IObservable<SyncOutcome>>
    {
        private readonly ISyncManager syncManager;
        private readonly IAnalyticsService analyticsService;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly INotificationService notificationService;
        private readonly ITimeService timeService;

        public RunBackgroundSyncInteractor(
            ISyncManager syncManager,
            IAnalyticsService analyticsService,
            IStopwatchProvider stopwatchProvider,
            INotificationService notificationService,
            ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(notificationService, nameof(notificationService));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.syncManager = syncManager;
            this.analyticsService = analyticsService;
            this.stopwatchProvider = stopwatchProvider;
            this.notificationService = notificationService;
            this.timeService = timeService;
        }

        public IObservable<SyncOutcome> Execute()
        {
            var syncTimeStopwatch = stopwatchProvider.Create(MeasuredOperation.BackgroundSync);
            var systemStopwatch = new Stopwatch();
            systemStopwatch.Start();
            analyticsService.BackgroundSyncStarted.Track();
            sendNotification("SyncStart", "Sync Started");
            return syncManager.ForceFullSync()
                              .LastAsync()
                              .Select(_ => SyncOutcome.NewData)
                              .Catch((Exception error) => syncFailed(error))
                              .Do(_ => systemStopwatch.Stop())
                              .Do(_ => syncTimeStopwatch.Stop())
                              .Do(_ => sendNotification("SyncStop", $"Sync Finished in {systemStopwatch.Elapsed:hh\\:mm\\:ss}"))
                              .Do(outcome => analyticsService.BackgroundSyncFinished.Track(outcome.ToString()));
        }

        private IObservable<SyncOutcome> syncFailed(Exception error)
        {
            analyticsService.BackgroundSyncFailed
                .Track(error.GetType().FullName, error.Message, error.StackTrace);
            return Observable.Return(SyncOutcome.Failed);
        }

        private void sendNotification(string id, string message)
        {
            var notification = new Notification(id, "Toggl", message, timeService.CurrentDateTime);
            var list = ImmutableList.Create<Notification>(notification);
            notificationService.Schedule(list);
        }
    }
}
