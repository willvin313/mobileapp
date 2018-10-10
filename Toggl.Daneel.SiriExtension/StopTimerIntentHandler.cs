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

namespace SiriExtension
{
    public class StopTimerIntentHandler : StopTimerIntentHandling
    {
        private ITogglApi togglAPI;

        public StopTimerIntentHandler(ITogglApi togglAPI)
        {
            this.togglAPI = togglAPI;
        }

        public override void ConfirmStopTimer(StopTimerIntent intent, Action<StopTimerIntentResponse> completion)
        {
            if (togglAPI == null)
            {
                completion(new StopTimerIntentResponse(StopTimerIntentResponseCode.FailureNoApiToken, null));
                return;
            }

            completion(new StopTimerIntentResponse(StopTimerIntentResponseCode.Ready, null));
        }

        public override void HandleStopTimer(StopTimerIntent intent, Action<StopTimerIntentResponse> completion)
        {
            togglAPI.TimeEntries.GetAll()
                    .Select(getRunningTimeEntry)
                    .SelectMany(stopTimeEntry)
                    .Subscribe(
                    te =>
                    {
                        SharedStorage.instance.SetNeedsSync(true);

                        var timeSpan = TimeSpan.FromSeconds(te.Duration ?? 0);
                        var durationString = $"{timeSpan.Hours} hours, {timeSpan.Minutes} minutes and {timeSpan.Seconds} seconds";
                        
                        var response = StopTimerIntentResponse.SuccessIntentResponseWithEntryDescription(
                            te.Description,
                            durationString
                        );
                        response.EntryStart = te.Start.ToUnixTimeSeconds();
                        response.EntryDuration = te.Duration;
                        
                        // Once Xamarin's bug if fixed we have to use tha above response instead of this one.
                        //var response = new StopTimerIntentResponse(StopTimerIntentResponseCode.Success, null);
                        completion(response);
                    },
                    exception =>
                    {
                        completion(responseFromException(exception));
                    });
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
            if (exception is NoRunningEntryException)
                return new StopTimerIntentResponse(StopTimerIntentResponseCode.SuccessNoRunningEntry, null);

            return new StopTimerIntentResponse(StopTimerIntentResponseCode.Failure, null);
        }
    }
}
