namespace Toggl.Foundation
{
    public static class ApplicationUrls
    {
        public const string Reports = "toggl://reports";

        public const string StartTimeEntry = "toggl://start";

        public static class Main
        {
            public static class Action
            {
                public const string Stop = "stop";
                public const string Continue = "continue";
            }

            public const string Regex = @"toggl://main(\?action=(?<action>.+)|\?description=(?<description>.+))?";

            public static readonly string Open = "toggl://main";

            public static readonly string StopTimeEntry = $"toggl://main?action={Action.Stop}";

            public static readonly string ContinueLastEntry = $"toggl://main?action={Action.Continue}";

            public static string Track(string description) 
                => $"toggl://main?description={description}";
        }

        public static class Calendar
        {
            public const string Regex = @"toggl://calendar(\?eventId=(?<eventId>.+))?";

            public static string Default => ForId(null);

            public static string ForId(string eventId)
                => $"toggl://calendar?eventId={eventId}";
        }
    }
}
