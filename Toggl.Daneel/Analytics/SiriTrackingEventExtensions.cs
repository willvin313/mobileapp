using System;
using Foundation;
using Toggl.Daneel.ExtensionKit.Analytics;
using Toggl.Foundation.Analytics;

namespace Toggl.Daneel
{
    public static class SiriTrackingEventExtensions
    {
        public static ITrackableEvent ToTrackableEvent(this SiriTrackingEvent e)
        {
            switch (e.EventType)
            {
                case SiriTrackingEventType.Error:
                    return new SiriIntentErrorTrackingEvent(e.Parameters[SiriErrorEventKeys.Message]);
                case SiriTrackingEventType.StartTimer:
                    return new StartTimeEntryEvent(
                        TimeEntryStartOrigin.Siri,
                        Convert.ToBoolean(e.Parameters[SiriStartTimerEventKeys.HasEmptyDescription]),
                        Convert.ToBoolean(e.Parameters[SiriStartTimerEventKeys.HasProject]),
                        Convert.ToBoolean(e.Parameters[SiriStartTimerEventKeys.HasTask]),
                        Convert.ToInt32(e.Parameters[SiriStartTimerEventKeys.NumberOfTags]),
                        Convert.ToBoolean(e.Parameters[SiriStartTimerEventKeys.IsBillable]),
                        true
                        );
                case SiriTrackingEventType.StopTimer:
                    return new StopTimerEntryEvent(TimeEntryStopOrigin.Siri);
                default:
                    return null;
            }
        }
    }
}
