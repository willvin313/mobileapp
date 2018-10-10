using System;
using System.Linq;
using Foundation;
using Intents;
using Toggl.Daneel.Intents;
using Toggl.Foundation;
using Toggl.Multivac.Models;
using Toggl.Foundation.Services;
using Toggl.Multivac.Models;
using UIKit;

namespace Toggl.Daneel.Services
{
    public class IntentDonationService : IIntentDonationService
    {
        public void DonateStartTimeEntry(IWorkspace workspace, ITimeEntry timeEntry)
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                return;
            }

            var intent = new StartTimerIntent();
            intent.Workspace = new INObject(workspace.Id.ToString(), workspace.Name);
            if (!string.IsNullOrEmpty(timeEntry.Description))
            {
                // If any of the tags or the project id were just created and haven't sync we ignore this action until the user repeats it
                if (timeEntry.ProjectId < 0 || timeEntry.TagIds.Any(tagId => tagId < 0)) 
                {
                    return;
                }

                intent.EntryDescription = timeEntry.Description;
                intent.ProjectId = new INObject(timeEntry.ProjectId.ToString(), timeEntry.ProjectId.ToString());
                intent.Tags = timeEntry.TagIds.Select(tag => new INObject(tag.ToString(), tag.ToString())).ToArray();
                intent.Billable = new INObject(timeEntry.Billable.ToString(), timeEntry.Billable.ToString());
                intent.SuggestedInvocationPhrase = timeEntry.Description;
            }
            else
            {
                intent.SuggestedInvocationPhrase = "Start timer";
            }

            var interaction = new INInteraction(intent, null);
            interaction.DonateInteraction(onCompletion);
        }

        public void DonateStopCurrentTimeEntry()
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                return;
            }

            var intent = new StopTimerIntent();
            intent.SuggestedInvocationPhrase = "Stop timer";

            var interaction = new INInteraction(intent, null);
            interaction.DonateInteraction(onCompletion);
        }

        public void DonateShowReport(ReportPeriod period)
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                return;
            }

            var intent = new ShowReportPeriodIntent();
            switch (period)
            {
                case ReportPeriod.Today:
                    intent.Period = ShowReportPeriodReportPeriod.Today;
                    break;
                case ReportPeriod.Yesterday:
                    intent.Period = ShowReportPeriodReportPeriod.Yesterday;
                    break;
                case ReportPeriod.LastWeek:
                    intent.Period = ShowReportPeriodReportPeriod.LastWeek;
                    break;
                case ReportPeriod.LastMonth:
                    intent.Period = ShowReportPeriodReportPeriod.LastMonth;
                    break;
                case ReportPeriod.ThisMonth:
                    intent.Period = ShowReportPeriodReportPeriod.ThisMonth;
                    break;
                case ReportPeriod.ThisWeek:
                    intent.Period = ShowReportPeriodReportPeriod.ThisWeek;
                    break;
                case ReportPeriod.ThisYear:
                    intent.Period = ShowReportPeriodReportPeriod.ThisYear;
                    break;
                case ReportPeriod.Unknown:
                    intent.Period = ShowReportPeriodReportPeriod.Unknown;
                    break;
            }

            var interaction = new INInteraction(intent, null);
            interaction.DonateInteraction(onCompletion);
        }

        public void DonateShowReport()
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                return;
            }

            var intent = new ShowReportIntent();
            var interaction = new INInteraction(intent, null);
            interaction.DonateInteraction(onCompletion);
        }

        public void ClearAll()
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                return;
            }

            INInteraction.DeleteAllInteractions(_ => { });
        }

        private Action<NSError> onCompletion
        {
            get
            {
                return error =>
                {
                    if (!(error is null))
                    {
                        Console.WriteLine($"Interaction donation failed: {error}");
                    }
                    else
                    {
                        Console.WriteLine("Successfully donated interaction.");
                    }
                };
            }
        }
    }
}