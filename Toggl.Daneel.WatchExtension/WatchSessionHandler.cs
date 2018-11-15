using System;
using ClockKit;
using Foundation;
using Toggl.Daneel.WatchExtension.Extensions;
using WatchConnectivity;
using WatchKit;

namespace Toggl.Daneel.WatchExtension
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

        [Export("session:didReceiveApplicationContext:")]
        public override void DidReceiveApplicationContext(WCSession session, NSDictionary<NSString, NSObject> applicationContext)
        {
            Console.WriteLine("Did receive application context: {0}", applicationContext);

            if (!WCSession.DefaultSession.ReceivedApplicationContext.ContainsKey("LoggedIn".ToNSString()))
            {
                WKExtension.SharedExtension.InvokeOnMainThread(() =>
                {
                    WKInterfaceController.ReloadRootControllers(new[] { "LoginInterfaceController" }, null);
                });
                return;
            }

            var runningTimeEntry = WCSession.DefaultSession.ReceivedApplicationContext["RunningTimeEntry"] as NSDictionary;
            if (runningTimeEntry != null)
            {
                WKExtension.SharedExtension.InvokeOnMainThread(() =>
                {
                    WKInterfaceController.ReloadRootControllers(new[] { "RunningTimeEntryInterfaceController" }, null);
                });
            }
            else
            {
                WKExtension.SharedExtension.InvokeOnMainThread(() =>
                {
                    WKInterfaceController.ReloadRootControllers(new[] { "MainInterfaceController" }, null);
                });
            }

            var activeComplications = CLKComplicationServer.SharedInstance.ActiveComplications;
            foreach (var complication in activeComplications)
            {
                CLKComplicationServer.SharedInstance.ReloadTimeline(complication);
            }

            NSNotificationCenter.DefaultCenter.PostNotificationName("DidReceiveApplicationContext", null);
        }
    }
}
