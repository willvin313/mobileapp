using Toggl.Daneel.Intents;
using Toggl.Foundation.Services;

namespace Toggl.Daneel.Extensions
{
    public static class IntentPeriodExtension
    {
        public static ReportPeriod ToReportPeriod(this ShowReportPeriodReportPeriod intentPeriod)
        {
            switch (intentPeriod)
            {
                case ShowReportPeriodReportPeriod.Today:
                    return ReportPeriod.Today;
                case ShowReportPeriodReportPeriod.Yesterday:
                    return ReportPeriod.Yesterday;
                case ShowReportPeriodReportPeriod.LastMonth:
                    return ReportPeriod.LastMonth;
                case ShowReportPeriodReportPeriod.ThisMonth:
                    return ReportPeriod.ThisMonth;
                case ShowReportPeriodReportPeriod.LastWeek:
                    return ReportPeriod.LastWeek;
                case ShowReportPeriodReportPeriod.ThisWeek:
                    return ReportPeriod.ThisWeek;
                case ShowReportPeriodReportPeriod.ThisYear:
                    return ReportPeriod.ThisYear;
                default:
                    return ReportPeriod.Unknown;
            }
        }
    }
}