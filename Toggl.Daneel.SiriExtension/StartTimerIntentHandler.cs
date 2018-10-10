using System;
using System.Linq;
using System.Collections.Generic;
using SiriExtension.Models;
using Toggl.Daneel.ExtensionKit;
using Toggl.Daneel.Intents;
using Toggl.Ultrawave;

namespace SiriExtension
{
    public class StartTimerIntentHandler : StartTimerIntentHandling
    {
        private ITogglApi togglAPI;

        public StartTimerIntentHandler(ITogglApi togglAPI)
        {
            this.togglAPI = togglAPI;
        }

        public override void ConfirmStartTimer(StartTimerIntent intent, Action<StartTimerIntentResponse> completion)
        {
            if (togglAPI == null)
            {
                completion(new StartTimerIntentResponse(StartTimerIntentResponseCode.FailureNoApiToken, null));
                return;
            }

            completion(new StartTimerIntentResponse(StartTimerIntentResponseCode.Ready, null));        }

        public override void HandleStartTimer(StartTimerIntent intent, Action<StartTimerIntentResponse> completion)
        {
            var workspaceId = (long)Convert.ToDouble(intent.Workspace.Identifier);

            var timeEntry = createTimeEntry(workspaceId, intent);
            togglAPI.TimeEntries.Create(timeEntry).Subscribe(te =>
            {
                SharedStorage.instance.SetNeedsSync(true);
                var response = new StartTimerIntentResponse(StartTimerIntentResponseCode.Success, null);
                completion(response);
            }, exception =>
            {
                completion(new StartTimerIntentResponse(StartTimerIntentResponseCode.Failure, null));
            });
        }

        private TimeEntry createTimeEntry(long workspaceId, StartTimerIntent intent)
        {
            if (string.IsNullOrEmpty(intent.EntryDescription))
            {
                return new TimeEntry(workspaceId, null, null, false, DateTimeOffset.Now, null, "", new long[0], (long)SharedStorage.instance.GetUserId(), 0, null, DateTimeOffset.Now);
            }

            return new TimeEntry(
                workspaceId,
                stringToLong(intent.ProjectId.Identifier),
                null,
                intent.Billable == null ? false : intent.Billable.Identifier == "True",
                DateTimeOffset.Now,
                null,
                intent.EntryDescription,
                intent.Tags == null ? new long[0] : stringToLongCollection(intent.Tags.Select(tag => tag.Identifier)),
                (long)SharedStorage.instance.GetUserId(),
                0,
                null,
                DateTimeOffset.Now
            );
        }

        private long? stringToLong(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            return (long)Convert.ToDouble(str);
        }

        private IEnumerable<long> stringToLongCollection(IEnumerable<string> strings)
        {
            if (strings.Count() == 0)
                return new long[0];

            return strings.Select(stringToLong).Cast<long>();
        }
    }
}