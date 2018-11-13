using System;

using WatchKit;
using Foundation;
using Toggl.Daneel.WatchExtension.Extensions;
using WatchConnectivity;

namespace Toggl.Daneel.WatchExtension.InterfaceControllers
{
    public partial class StartTimeEntryInterfaceController : WKInterfaceController
    {

        private string description = String.Empty;

        protected StartTimeEntryInterfaceController(IntPtr handle) : base(handle)
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
        }

        public override void DidDeactivate()
        {
            // This method is called when the watch view controller is no longer visible to the user.
            Console.WriteLine("{0} did deactivate", this);
        }

        partial void OnEnterDescriptionPress(WKInterfaceButton sender)
        {
            PresentTextInputController(new[] { "Hello, world!", "F**** timesheets!" }, WKTextInputMode.AllowEmoji, (result) =>
            {
                if (result.Count > 0)
                {
                    description = result.GetItem<NSString>(0);
                    EnterDescriptionButton.SetTitle(description);
                }
            });
        }

        partial void OnStartTimeEntryPress(WKInterfaceButton sender)
        {
            var mutableMessage = new NSMutableDictionary<NSString, NSObject>();
            mutableMessage.Add("action".ToNSString(), "StartTimeEntry".ToNSString());
            mutableMessage.Add("Description".ToNSString(), description.ToNSString());

            var message = NSDictionary<NSString, NSObject>.FromObjectsAndKeys(mutableMessage.Values, mutableMessage.Keys);

            WCSession.DefaultSession.SendMessage(message, null, onError);
        }

        private void onError(NSError error)
        {
            Console.WriteLine("Failed to send message: {0)", error);
        }

        private void contextReceived(NSNotification notification)
        {
            var runningTimeEntry = WCSession.DefaultSession.ReceivedApplicationContext["RunningTimeEntry"] as NSDictionary;
            if (runningTimeEntry != null)
            {
                WKExtension.SharedExtension.InvokeOnMainThread(() =>
                {
                    ReloadRootControllers(new[] { "RunningTimeEntryInterfaceController" }, null);
                });
            }
        }
    }
}
