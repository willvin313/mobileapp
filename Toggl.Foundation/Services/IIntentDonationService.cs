using Toggl.Multivac.Models;

namespace Toggl.Foundation.Services
{
    public enum ReportPeriod
    {
        Unknown,
        LastMonth,
        LastWeek,
        Yesterday,
        Today,
        ThisWeek,
        ThisMonth,
        ThisYear
    }

    public interface IIntentDonationService
    {
        void DonateStartTimeEntry(IWorkspace workspace, ITimeEntry timeEntry);
        void DonateStopCurrentTimeEntry();
        void DonateShowReport(ReportPeriod period);
        void DonateShowReport();
        void ClearAll();
    }
}