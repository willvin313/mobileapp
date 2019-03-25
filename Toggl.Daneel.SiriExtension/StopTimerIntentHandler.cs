using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;
using Toggl.Daneel.Intents;
using Foundation;
using Toggl.Multivac.Models;
using SiriExtension.Models;
using SiriExtension.Exceptions;
using Toggl.Daneel.ExtensionKit;
using Toggl.Daneel.ExtensionKit.Analytics;
using Toggl.Daneel.ExtensionKit.Extensions;

namespace SiriExtension
{
    public class StopTimerIntentHandler : StopTimerIntentHandling
    {
        private ITogglApi togglAPI;
        private static ITimeEntry runningEntry;
        private const string stopTimerActivityType = "StopTimer";

        public StopTimerIntentHandler(ITogglApi togglAPI)
        {
            this.togglAPI = togglAPI;
        }

        public override void ConfirmStopTimer(StopTimerIntent intent, Action<StopTimerIntentResponse> completion)
        {
            if (togglAPI == null)
            {
                var userActivity = new NSUserActivity(stopTimerActivityType);
                userActivity.SetResponseText("Log in to use this shortcut.");
                completion(new StopTimerIntentResponse(StopTimerIntentResponseCode.FailureNoApiToken, userActivity));
                return;
            }

            var lastUpdated = SharedStorage.instance.GetLastUpdateDate();
            togglAPI.TimeEntries.GetAll()
                .Select(checkSyncConflicts(lastUpdated))
                .Select(getRunningTimeEntry)
                .Subscribe(
                runningTE =>
                {
                    runningEntry = runningTE;
                    var userActivity = new NSUserActivity(stopTimerActivityType);
                    userActivity.SetEntryDescription(runningTE.Description);
                    completion(new StopTimerIntentResponse(StopTimerIntentResponseCode.Ready, userActivity));
                },
                exception =>
                {
                    SharedStorage.instance.AddSiriTrackingEvent(SiriTrackingEvent.Error(exception.Message));
                    completion(responseFromException(exception));
                });
        }

        public override void HandleStopTimer(StopTimerIntent intent, Action<StopTimerIntentResponse> completion)
        {
            SharedStorage.instance.SetNeedsSync(true);

            stopTimeEntry(runningEntry)
                .Subscribe(
                    stoppedTimeEntry =>
                    {
                        var timeSpan = TimeSpan.FromSeconds(stoppedTimeEntry.Duration ?? 0);

                        var response = string.IsNullOrEmpty(stoppedTimeEntry.Description)
                            ? StopTimerIntentResponse.SuccessWithEmptyDescriptionIntentResponseWithEntryDurationString(
                                durationStringForTimeSpan(timeSpan))
                            : StopTimerIntentResponse.SuccessIntentResponseWithEntryDescription(
                                stoppedTimeEntry.Description, durationStringForTimeSpan(timeSpan)
                            );
                        response.EntryStart = stoppedTimeEntry.Start.ToUnixTimeSeconds();
                        response.EntryDuration = stoppedTimeEntry.Duration;

                        SharedStorage.instance.AddSiriTrackingEvent(SiriTrackingEvent.StopTimer());

                        completion(response);
                    },
                    exception =>
                    {
                        SharedStorage.instance.AddSiriTrackingEvent(SiriTrackingEvent.Error(exception.Message));
                        completion(responseFromException(exception));
                    }
                );
        }

        private Func<List<ITimeEntry>, List<ITimeEntry>> checkSyncConflicts(DateTimeOffset lastUpdated)
        {
            return tes =>
            {
                // If there are no changes since last sync, or there are changes in the server but not in the app, we are ok
                if (tes.Count == 0 || tes.OrderBy(te => te.At).Last().At >= lastUpdated)
                {
                    return tes;
                }

                throw new AppOutdatedException();
            };
        }

        private string durationStringForTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.Hours == 0 && timeSpan.Minutes == 0)
            {
                return $"{timeSpan.Seconds} seconds";
            }

            if (timeSpan.Hours == 0)
            {
                return $"{timeSpan.Minutes} minutes and {timeSpan.Seconds} seconds";
            }

            if (timeSpan.Minutes == 0)
            {
                return $"{timeSpan.Hours} hours and {timeSpan.Seconds} seconds";
            }

            return $"{timeSpan.Hours} hours, {timeSpan.Minutes} minutes and {timeSpan.Seconds} seconds";
        }

        private ITimeEntry getRunningTimeEntry(IList<ITimeEntry> timeEntries)
        {
            try
            {
                var runningTE = timeEntries.Where(te => te.Duration == null).First();
                return runningTE;
            } catch {
                throw new NoRunningEntryException();
            }
        }

        private IObservable<ITimeEntry> stopTimeEntry(ITimeEntry timeEntry)
        {
            var duration = (long)(DateTime.Now - timeEntry.Start).TotalSeconds;
            return togglAPI.TimeEntries.Update(
                TimeEntry.from(timeEntry).with(duration)
            );
        }

        private StopTimerIntentResponse responseFromException(Exception exception)
        {
            var userActivity = new NSUserActivity(stopTimerActivityType);
            if (exception is NoRunningEntryException)
            {
                userActivity.SetResponseText("There's no entry currently running.");
                return new StopTimerIntentResponse(StopTimerIntentResponseCode.FailureNoTimerRunning, userActivity);
            }

            if (exception is AppOutdatedException) {
                userActivity.SetResponseText("Open the app to sync your data, then try again.");
                return new StopTimerIntentResponse(StopTimerIntentResponseCode.FailureSyncConflict, userActivity);
            }

            userActivity.SetResponseText("Something went wrong, please try again.");
            return new StopTimerIntentResponse(StopTimerIntentResponseCode.Failure, userActivity);
        }
    }
}
