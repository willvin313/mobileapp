using System;

using Foundation;
using ClockKit;
using WatchConnectivity;
using UIKit;

namespace Toggl.Daneel.WatchExtension
{
    [Register("ComplicationController")]
    public class ComplicationController : CLKComplicationDataSource
    {
        protected ComplicationController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic
        }

        public override void GetSupportedTimeTravelDirections(CLKComplication complication, Action<CLKComplicationTimeTravelDirections> handler)
        {
            handler(CLKComplicationTimeTravelDirections.None);
        }

        public override void GetCurrentTimelineEntry(CLKComplication complication, Action<CLKComplicationTimelineEntry> handler)
        {
            if (complication.Family == CLKComplicationFamily.UtilitarianLarge)
            {
                var template = new CLKComplicationTemplateUtilitarianLargeFlat();

                var runningTimeEntry = WCSession.DefaultSession.ReceivedApplicationContext["RunningTimeEntry"] as NSDictionary;
                if (runningTimeEntry != null)
                {
                    var description = (runningTimeEntry["Description"] as NSString).ToString();
                    description = description.Length > 0 ? description : "No description";

                    var textProvider = CLKSimpleTextProvider.FromText(description, "Stop");
                    template.TextProvider = textProvider;
                    var entry = CLKComplicationTimelineEntry.Create(NSDate.Now, template);
                    handler(entry);
                }
                else
                {
                    var textProvider = CLKSimpleTextProvider.FromText("Start time entry", "Start");
                    template.TextProvider = textProvider;
                    var entry = CLKComplicationTimelineEntry.Create(NSDate.Now, template);
                    handler(entry);
                }
            }
            if (complication.Family == CLKComplicationFamily.ModularLarge)
            {
                var runningTimeEntry = WCSession.DefaultSession.ReceivedApplicationContext["RunningTimeEntry"] as NSDictionary;
                if (runningTimeEntry != null)
                {
                    var description = (runningTimeEntry["Description"] as NSString).ToString();
                    description = description.Length > 0 ? description : "No description";

                    var startTime = runningTimeEntry["Start"] as NSDate;

                    var template = new CLKComplicationTemplateModularLargeStandardBody();
                    var descriptionTextProvider = CLKSimpleTextProvider.FromText(description, "Toggl");
                    descriptionTextProvider.TintColor = UIColor.Red;
                    var projectTextProvider = CLKSimpleTextProvider.FromText("No project", "Toggl");
                    projectTextProvider.TintColor = UIColor.LightGray;
                    var runningTimeTextProvider = CLKRelativeDateTextProvider.FromDate(startTime, CLKRelativeDateStyle.Timer, NSCalendarUnit.Hour | NSCalendarUnit.Minute | NSCalendarUnit.Second);
                    runningTimeTextProvider.TintColor = UIColor.White;
                    template.HeaderTextProvider = descriptionTextProvider;
                    template.Body1TextProvider = projectTextProvider;
                    template.Body2TextProvider = runningTimeTextProvider;

                    var entry = CLKComplicationTimelineEntry.Create(NSDate.Now, template);
                    handler(entry);
                }
                else
                {
                    var template = new CLKComplicationTemplateModularLargeStandardBody();
                    var headerTextProvider = CLKSimpleTextProvider.FromText("Toggl", "Toggl");
                    headerTextProvider.TintColor = UIColor.Red;
                    var subheaderTextProvider = CLKSimpleTextProvider.FromText("Start time entry", "Toggl");
                    subheaderTextProvider.TintColor = UIColor.White;
                    template.HeaderTextProvider = headerTextProvider;
                    template.Body1TextProvider = subheaderTextProvider;

                    var entry = CLKComplicationTimelineEntry.Create(NSDate.Now, template);
                    handler(entry);
                }
            }
        }

        public override void GetLocalizableSampleTemplate(CLKComplication complication, Action<CLKComplicationTemplate> handler)
        {
            // This method will be called once per supported complication, and the results will be cached
            if (complication.Family == CLKComplicationFamily.UtilitarianLarge)
            {
                var template = new CLKComplicationTemplateUtilitarianLargeFlat();
                var textProvider = CLKSimpleTextProvider.FromText("Hello, World!", "Toggl");
                template.TextProvider = textProvider;

                handler(template);
            }
            if (complication.Family == CLKComplicationFamily.ModularLarge)
            {
                var template = new CLKComplicationTemplateModularLargeStandardBody();
                var descriptionTextProvider = CLKSimpleTextProvider.FromText("Hello, World!", "Toggl");
                var projectTextProvider = CLKSimpleTextProvider.FromText("No project", "Toggl");
                var runningTimeTextProvider = CLKTimeTextProvider.FromDate(NSDate.Now);
                template.HeaderTextProvider = descriptionTextProvider;
                template.Body1TextProvider = projectTextProvider;
                template.Body2TextProvider = runningTimeTextProvider;

                handler(template);
            }
        }
    }
}
