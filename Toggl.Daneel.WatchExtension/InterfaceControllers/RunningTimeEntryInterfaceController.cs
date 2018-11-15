using System;

using WatchKit;
using Foundation;
using Toggl.Daneel.WatchExtension.Extensions;
using WatchConnectivity;

namespace Toggl.Daneel.WatchExtension.InterfaceControllers
{
    public partial class RunningTimeEntryInterfaceController : WKInterfaceController
    {
        protected RunningTimeEntryInterfaceController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void Awake(NSObject context)
        {
            base.Awake(context);

            // Configure interface objects here.
            Console.WriteLine("{0} awake with context", this);

            NSNotificationCenter.DefaultCenter.AddObserver(new NSString("DidReceiveApplicationContext"), contextReceived);
        }

        public override void WillActivate()
        {
            // This method is called when the watch view controller is about to be visible to the user.
            Console.WriteLine("{0} will activate", this);
            updateInterface();
        }

        public override void DidDeactivate()
        {
            // This method is called when the watch view controller is no longer visible to the user.
            Console.WriteLine("{0} did deactivate", this);
        }

        partial void OnStopButtonPress(WKInterfaceButton sender)
        {
            var message = new NSDictionary<NSString, NSObject>("action".ToNSString(), "StopRunningTimeEntry".ToNSString());
            WCSession.DefaultSession.SendMessage(message, null, onError);
        }

        private void onError(NSError error)
        {
            Console.WriteLine("Failed to send message: {0)", error);
        }

        private void contextReceived(NSNotification notification)
        {
            updateInterface();
        }

        private void updateInterface()
        {
            var runningTimeEntry = WCSession.DefaultSession.ReceivedApplicationContext["RunningTimeEntry"] as NSDictionary;
            if (runningTimeEntry != null)
            {
                var description = runningTimeEntry["Description"] as NSString;
                var start = runningTimeEntry["Start"] as NSDate;

                DescriptionLabel.SetText(description);
                RunningTimer.SetDate(start);
                RunningTimer.Start();
            }
        }
    }
}
