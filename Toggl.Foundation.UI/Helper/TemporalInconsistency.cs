namespace Toggl.Foundation.UI.Helper
{
    public enum TemporalInconsistency
    {
        StartTimeAfterCurrentTime,
        StartTimeAfterStopTime,
        StopTimeBeforeStartTime,
        DurationTooLong
    }
}
