using System;
using System.Linq;
using System.Reactive.Linq;
using Foundation;
using SiriExtension.Models;
using Toggl.Daneel.ExtensionKit;
using Toggl.Daneel.ExtensionKit.Analytics;
using Toggl.Daneel.ExtensionKit.Extensions;
using Toggl.Daneel.Intents;
using Toggl.Multivac.Models;
using Toggl.Ultrawave;

namespace SiriExtension
{
    public class ContinueTimerIntentHandler: ContinueTimerIntentHandling
    {
        private ITogglApi togglAPI;
        private static ITimeEntry lastEntry;
        private const string continueTimerActivityType = "ContinueTimer";

        public ContinueTimerIntentHandler(ITogglApi togglApi)
        {
            togglAPI = togglApi;
        }

        public override void ConfirmContinueTimer(ContinueTimerIntent intent, Action<ContinueTimerIntentResponse> completion)
        {
            if (togglAPI == null)
            {
                completion(new ContinueTimerIntentResponse(ContinueTimerIntentResponseCode.FailureNoApiToken, null));
                return;
            }

            togglAPI.TimeEntries.GetAll()
                .FirstAsync()
                .Select(timeEntries => timeEntries.First())
                .Subscribe(
                    timeEntry =>
                    {
                        lastEntry = timeEntry;
                        var userActivity = new NSUserActivity(continueTimerActivityType);
                        userActivity.SetEntryDescription(timeEntry.Description);
                        completion(new ContinueTimerIntentResponse(ContinueTimerIntentResponseCode.Ready, userActivity));
                    },
                    exception =>
                    {
                        completion(new ContinueTimerIntentResponse(ContinueTimerIntentResponseCode.Failure, null));
                    });
        }

        public override void HandleContinueTimer(ContinueTimerIntent intent, Action<ContinueTimerIntentResponse> completion)
        {

            togglAPI.TimeEntries.Create(continueTimeEntry(lastEntry))
                .Subscribe(
                    te =>
                    {
                        SharedStorage.instance.SetNeedsSync(true);
                        var response = string.IsNullOrEmpty(te.Description)
                            ? new ContinueTimerIntentResponse(ContinueTimerIntentResponseCode.Success, null)
                            : ContinueTimerIntentResponse.SuccessWithEntryDescriptionIntentResponseWithEntryDescription(
                                te.Description
                            );

                        SharedStorage.instance.AddSiriTrackingEvent(SiriTrackingEvent.StartTimer(te));

                        completion(response);
                    },
                    exception =>
                    {

                        SharedStorage.instance.AddSiriTrackingEvent(SiriTrackingEvent.Error(exception.Message));
                        completion(new ContinueTimerIntentResponse(ContinueTimerIntentResponseCode.Failure, null));
                    });
        }

        private ITimeEntry continueTimeEntry(ITimeEntry originalTE) =>
            new TimeEntry(
                originalTE.WorkspaceId,
                originalTE.ProjectId,
                originalTE.TaskId,
                originalTE.Billable,
                DateTimeOffset.Now,
                null,
                originalTE.Description,
                originalTE.TagIds ?? new long[0],
                originalTE.UserId,
                0,
                null,
                DateTimeOffset.Now
            );
    }
}
