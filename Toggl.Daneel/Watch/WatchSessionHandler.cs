using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Foundation;
using Toggl.Daneel.Extensions.Models;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using WatchConnectivity;
using System.Reactive.Linq;

namespace Toggl.Daneel.Watch
{
    public sealed class WatchSessionHandler : WCSessionDelegate
    {
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public WatchSessionHandler(ITimeService timeService, ITogglDataSource dataSource, IInteractorFactory interactorFactory)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));

            this.timeService = timeService;
            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;

            this.dataSource
                .TimeEntries
                .CurrentlyRunningTimeEntry
                .Subscribe(currentRunningTimeEntryChanged)
                .DisposedBy(disposeBag);
        }

        [Export("session:activationDidCompleteWithState:error:")]
        public override void ActivationDidComplete(WCSession session, WCSessionActivationState activationState, NSError error)
        {
            Console.WriteLine("Session activation state: {0}", activationState);
        }

        [Export("sessionReachabilityDidChange:")]
        public override void SessionReachabilityDidChange(WCSession session)
        {
            Console.WriteLine("Session reachability changed: {0}", session.Reachable);
        }

        [Export("session:didReceiveMessage:")]
        public override void DidReceiveMessage(WCSession session, NSDictionary<NSString, NSObject> message)
        {
            Console.WriteLine("Did receive message: {0}", message);

            var action = (message["action"] as NSString).ToString();

            switch (action)
            {
                case "StopRunningTimeEntry":
                    stopRunningTimeEntry();
                    break;
                default:
                    Console.WriteLine("Unknown action: {0}", action);
                    break;
            }
        }

        [Export("session:didReceiveMessage:replyHandler:")]
        public override void DidReceiveMessage(WCSession session, NSDictionary<NSString, NSObject> message, WCSessionReplyHandler replyHandler)
        {
            Console.WriteLine("Did receive message: {0}", message);

            var response = new NSDictionary<NSString, NSObject>();
            replyHandler(response);
        }

        private void currentRunningTimeEntryChanged(IThreadSafeTimeEntry timeEntry)
        {
            if (WCSession.DefaultSession.ActivationState != WCSessionActivationState.Activated)
                return;

            var timeEntryDict = timeEntry == null ? null : timeEntry.ToNSDictionary();

            var context = WCSession.DefaultSession.ApplicationContext ?? new NSDictionary<NSString, NSObject>();
            var mutableContext = new NSMutableDictionary<NSString, NSObject>(context);
            mutableContext["RunningTimeEntry"] = timeEntryDict;
            var updatedContext = new NSDictionary<NSString, NSObject>(mutableContext.Keys, mutableContext.Values);

            NSError error;
            WCSession.DefaultSession.UpdateApplicationContext(updatedContext, out error);
        }

        private async Task stopRunningTimeEntry()
        {
            await interactorFactory.StopTimeEntry(timeService.CurrentDateTime, TimeEntryStopOrigin.AppleWatch).Execute();
        }
    }
}
