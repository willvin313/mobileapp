using System;

using Foundation;
using ClockKit;
using WatchConnectivity;

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
        }
    }
}
