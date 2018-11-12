using System;
using Foundation;
using WatchConnectivity;

namespace Toggl.Daneel.Watch
{
    public sealed class WatchSessionHandler : WCSessionDelegate
    {
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
    }
}
