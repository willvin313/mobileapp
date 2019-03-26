using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using Intents;
using Toggl.Daneel.Intents;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Multivac.Models;
using Toggl.Foundation.Services;
using UIKit;

namespace Toggl.Daneel.Services
{
    public class IntentDonationServiceIos : IIntentDonationService
    {
        private IAnalyticsService analyticsService;

        public IntentDonationServiceIos(IAnalyticsService analyticsService)
        {
            this.analyticsService = analyticsService;
        }

        public void SetDefaultShortcutSuggestions(IWorkspace workspace)
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                return;
            }

            setupDefaultShortcuts(workspace);
        }

        public void DonateStartTimeEntry(IWorkspace workspace, ITimeEntry timeEntry)
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                return;
            }

            var relevantShortcuts = new List<INRelevantShortcut>();

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
                intent.SuggestedInvocationPhrase = $"Track {timeEntry.Description}";

                // Relevant shortcut for the Siri Watch Face
                var shortcut = createRelevantShortcut(intent);
                relevantShortcuts.Add(shortcut);
            }
            else
            {
                intent.SuggestedInvocationPhrase = Resources.StartTimerInvocationPhrase;
            }

            var interaction = new INInteraction(intent, null);
            interaction.DonateInteraction(trackError);

            // Descriptionless Relevant Shortcut. Always added even if the intent has one
            var descriptionlessIntent = new StartTimerIntent();
            descriptionlessIntent.Workspace = new INObject(workspace.Id.ToString(), workspace.Name);
            var descriptionlessShortcut = createRelevantShortcut(descriptionlessIntent);
            relevantShortcuts.Add(descriptionlessShortcut);

            donateRelevantShortcuts(relevantShortcuts.ToArray());
        }

        public void DonateStopCurrentTimeEntry()
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                return;
            }

            var intent = new StopTimerIntent();
            intent.SuggestedInvocationPhrase = Resources.StopTimerInvocationPhrase;

            var interaction = new INInteraction(intent, null);
            interaction.DonateInteraction(trackError);

            var shortcut = createRelevantShortcut(intent);
            donateRelevantShortcuts(shortcut);
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
                    intent.SuggestedInvocationPhrase = $"Show {Resources.Today.ToLower()}'s time";
                    break;
                case ReportPeriod.Yesterday:
                    intent.Period = ShowReportPeriodReportPeriod.Yesterday;
                    intent.SuggestedInvocationPhrase = $"Show {Resources.Yesterday.ToLower()}'s time";
                    break;
                case ReportPeriod.LastWeek:
                    intent.Period = ShowReportPeriodReportPeriod.LastWeek;
                    intent.SuggestedInvocationPhrase = $"Show {Resources.LastWeek.ToLower()}'s time";
                    break;
                case ReportPeriod.LastMonth:
                    intent.Period = ShowReportPeriodReportPeriod.LastMonth;
                    intent.SuggestedInvocationPhrase = $"Show {Resources.LastMonth.ToLower()}'s time";
                    break;
                case ReportPeriod.ThisMonth:
                    intent.Period = ShowReportPeriodReportPeriod.ThisMonth;
                    intent.SuggestedInvocationPhrase = $"Show {Resources.ThisMonth.ToLower()}'s time";
                    break;
                case ReportPeriod.ThisWeek:
                    intent.Period = ShowReportPeriodReportPeriod.ThisWeek;
                    intent.SuggestedInvocationPhrase = $"Show {Resources.ThisWeek.ToLower()}'s time";
                    break;
                case ReportPeriod.ThisYear:
                    intent.Period = ShowReportPeriodReportPeriod.ThisYear;
                    intent.SuggestedInvocationPhrase = $"Show {Resources.ThisYear.ToLower()}'s time";
                    break;
                case ReportPeriod.Unknown:
                    intent.Period = ShowReportPeriodReportPeriod.Unknown;
                    break;
            }

            var interaction = new INInteraction(intent, null);
            interaction.DonateInteraction(trackError);
        }

        public void DonateShowReport()
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                return;
            }

            var intent = new ShowReportIntent();
            intent.SuggestedInvocationPhrase = Resources.ShowReportsInvocationPhrase;
            var interaction = new INInteraction(intent, null);
            interaction.DonateInteraction(trackError);
        }

        public void ClearAll()
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                return;
            }

            INInteraction.DeleteAllInteractions(_ => { });
            INVoiceShortcutCenter.SharedCenter.SetShortcutSuggestions(new INShortcut[0]);
            INRelevantShortcutStore.DefaultStore.SetRelevantShortcuts(new INRelevantShortcut[0], trackError);
        }

        private void setupDefaultShortcuts(IWorkspace workspace)
        {
            var startTimerIntent = new StartTimerIntent();
            startTimerIntent.Workspace = new INObject(workspace.Id.ToString(), workspace.Name);
            startTimerIntent.SuggestedInvocationPhrase = Resources.StartTimerInvocationPhrase;
            var startShortcut = new INShortcut(startTimerIntent);
            var startRelevantShorcut = new INRelevantShortcut(startShortcut);
            INRelevanceProvider[] startTimerProviders = {
                new INDailyRoutineRelevanceProvider(INDailyRoutineSituation.Work),
                new INDailyRoutineRelevanceProvider(INDailyRoutineSituation.Gym),
                new INDailyRoutineRelevanceProvider(INDailyRoutineSituation.School)
            };
            startRelevantShorcut.RelevanceProviders = startTimerProviders;

            var stopTimerIntent = new StopTimerIntent();
            stopTimerIntent.SuggestedInvocationPhrase = Resources.StopTimerInvocationPhrase;
            var stopShortcut = new INShortcut(stopTimerIntent);
            var stopRelevantShortcut = new INRelevantShortcut(stopShortcut);
            stopRelevantShortcut.RelevanceProviders = new[]
            {
                new INDailyRoutineRelevanceProvider(INDailyRoutineSituation.Home)
            };

            var showReportIntent = new ShowReportIntent();
            showReportIntent.SuggestedInvocationPhrase = Resources.ShowReportsInvocationPhrase;
            var reportShortcut = new INShortcut(showReportIntent);

            var continueTimerIntent = new ContinueTimerIntent
            {
                SuggestedInvocationPhrase = Resources.ContinueTimerInvocationPhrase
            };
            continueTimerIntent.Workspace = new INObject(workspace.Id.ToString(), workspace.Name);
            var continueTimerShortcut = new INShortcut(continueTimerIntent);
            var continueTimerRelevantShortcut = new INRelevantShortcut(continueTimerShortcut)
            {
                RelevanceProviders = startTimerProviders
            };

            var shortcuts = new[] { startShortcut, stopShortcut, reportShortcut, continueTimerShortcut };
            INVoiceShortcutCenter.SharedCenter.SetShortcutSuggestions(shortcuts);

            var relevantShortcuts = new[] { startRelevantShorcut, stopRelevantShortcut, continueTimerRelevantShortcut };
            INRelevantShortcutStore.DefaultStore.SetRelevantShortcuts(relevantShortcuts, trackError);
        }

        private INRelevantShortcut createRelevantShortcut(INIntent intent)
        {
            var shortcut = new INShortcut(intent);
            var relevantShortcut = new INRelevantShortcut(shortcut);
            relevantShortcut.RelevanceProviders = new List<INRelevanceProvider>().ToArray();
            return relevantShortcut;
        }

        private void donateRelevantShortcuts(params INRelevantShortcut[] relevantShortcuts)
        {
            INRelevantShortcutStore.DefaultStore.SetRelevantShortcuts(relevantShortcuts, trackError);
        }

        private void trackError(NSError error)
        {
            if (error == null)
                return;

            analyticsService.TrackAnonymized(new Exception(error.LocalizedDescription));
        }
    }
}
