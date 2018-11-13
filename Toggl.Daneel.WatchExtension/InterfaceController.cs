using System;
using Foundation;
using WatchConnectivity;
using WatchKit;

namespace Toggl.Daneel.WatchExtension
{
    public partial class InterfaceController : WKInterfaceController
    {
        protected InterfaceController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void Awake(NSObject context)
        {
            base.Awake(context);
            NSNotificationCenter.DefaultCenter.AddObserver(new NSString("DidReceiveApplicationContext"), contextReceived);

            // Configure interface objects here.
            Console.WriteLine("{0} awake with context", this);
        }

        public override void WillActivate()
        {
            // This method is called when the watch view controller is about to be visible to the user.
            Console.WriteLine("{0} will activate", this);
        }

        public override void DidDeactivate()
        {
            // This method is called when the watch view controller is no longer visible to the user.
            Console.WriteLine("{0} did deactivate", this);
        }

        private void contextReceived(NSNotification notification)
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
