using System;
using System.Reactive.Disposables;
using Foundation;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Models;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using WatchConnectivity;

namespace Toggl.Daneel.Watch
{
    public sealed class WatchSessionHandler : WCSessionDelegate
    {
        private readonly ITogglDataSource dataSource;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public WatchSessionHandler(ITogglDataSource dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            this.dataSource = dataSource;

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
    }
}
