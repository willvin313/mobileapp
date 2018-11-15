using System;
using Foundation;
using Toggl.Daneel.WatchExtension.Cells;
using Toggl.Daneel.WatchExtension.Extensions;
using UIKit;
using WatchConnectivity;
using WatchKit;

namespace Toggl.Daneel.WatchExtension.InterfaceControllers
{
    public partial class MainInterfaceController : WKInterfaceController
    {
        protected MainInterfaceController(IntPtr handle) : base(handle)
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
            updateInterface();
        }

        public override void DidDeactivate()
        {
            // This method is called when the watch view controller is no longer visible to the user.
            Console.WriteLine("{0} did deactivate", this);
        }

        partial void OnStartTimeEntrySelected(NSObject sender)
        {
            PresentController("StartTimeEntryInterfaceController", string.Empty);
        }

        private void contextReceived(NSNotification notification)
        {
            updateInterface();
        }

        private void updateInterface()
        {
            var isEmpty = true;

            var suggestionsArray = WCSession.DefaultSession.ReceivedApplicationContext["Suggestions"] as NSArray;
            if (suggestionsArray != null && suggestionsArray.Count > 0)
            {
                SuggestionsTable.SetNumberOfRows((nint)suggestionsArray.Count, "SuggestionRow");
                for (var i = 0; i < SuggestionsTable.NumberOfRows; i++)
                {
                    var suggestion = suggestionsArray.GetItem<NSDictionary>((nuint)i);
                    var description = suggestion["Description"] as NSString;
                    var projectName = suggestion["ProjectName"] as NSString;
                    var colorString = (suggestion["ProjectColor"] as NSString)?.ToString();
                    var color = colorString?.ToUIColor();

                    var row = SuggestionsTable.GetRowController(i) as SuggestionRowController;
                    row.Configure(description, projectName, color);
                }

                SuggestionsLabel.SetHidden(false);
                SuggestionsTable.SetHidden(false);
                isEmpty = false;
            }
            else
            {
                SuggestionsLabel.SetHidden(true);
                SuggestionsTable.SetHidden(true);
            }

            var timeEntriesArray = WCSession.DefaultSession.ReceivedApplicationContext["TodayTimeEntries"] as NSArray;
            if (timeEntriesArray != null && timeEntriesArray.Count > 0)
            {
                TimeEntriesLabel.SetText(new NSString("Today"));
                TimeEntriesTable.SetNumberOfRows((nint)timeEntriesArray.Count, "TimeEntryRow");

                var durationFormatter = new NSDateComponentsFormatter();
                durationFormatter.UnitsStyle = NSDateComponentsFormatterUnitsStyle.Positional;
                durationFormatter.AllowedUnits = NSCalendarUnit.Hour | NSCalendarUnit.Minute | NSCalendarUnit.Second;
                durationFormatter.ZeroFormattingBehavior = NSDateComponentsFormatterZeroFormattingBehavior.Pad;

                for (var i = 0; i < TimeEntriesTable.NumberOfRows; i++)
                {
                    var timeEntry = timeEntriesArray.GetItem<NSDictionary>((nuint)i);
                    var description = timeEntry["Description"] as NSString;
                    NSString projectName = null;
                    NSString duration = null;
                    UIColor color = null;

                    if (timeEntry.ContainsKey("Duration".ToNSString()))
                    {
                        var durationValue = (timeEntry["Duration"] as NSNumber).FloatValue;
                        duration = durationFormatter.StringFromTimeInterval(durationValue).ToNSString();
                    }

                    if (timeEntry.ContainsKey("Project".ToNSString()))
                    {
                        var project = timeEntry["Project"] as NSDictionary;
                        projectName = project["Name"] as NSString;
                        var colorString = (project["Color"] as NSString)?.ToString();
                        color = colorString?.ToUIColor();
                    }

                    var row = TimeEntriesTable.GetRowController(i) as TimeEntryRowController;
                    row.Configure(description, projectName, duration, color);
                }

                TimeEntriesLabel.SetHidden(false);
                TimeEntriesTable.SetHidden(false);
                isEmpty = false;
            }
            else
            {
                TimeEntriesLabel.SetHidden(true);
                TimeEntriesTable.SetHidden(true);
            }

            EmptyStateContainer.SetHidden(!isEmpty);
            EmptyStateLabel.SetHidden(!isEmpty);
        }
    }
}
