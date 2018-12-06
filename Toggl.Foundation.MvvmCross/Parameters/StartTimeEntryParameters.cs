using System;
using System.Collections.Generic;
using static Toggl.Foundation.Helper.Constants;

namespace Toggl.Foundation.MvvmCross.Parameters
{
    public sealed class StartTimeEntryParameters
    {
        private static readonly TimeSpan defaultManualModeDuration = TimeSpan.FromMinutes(DefaultTimeEntryDurationForManualModeInMinutes);

        public DateTimeOffset StartTime { get; }

        public string PlaceholderText { get; }

        public string EntryDescription { get; }

        public TimeSpan? Duration { get; }

        public long? WorkspaceId { get; }

        public long? ProjectId { get; }

        public long? TaskId { get; }

        public IEnumerable<long> TagIds { get; }

        public StartTimeEntryParameters(
            DateTimeOffset startTime,
            string placeholderText,
            TimeSpan? duration,
            long? workspaceId,
            string entryDescription = "",
            long? projectId = null,
            long? taskId = null,
            IEnumerable<long> tagIds = null)
        {
            StartTime = startTime;
            PlaceholderText = placeholderText;        
            Duration = duration;
            WorkspaceId = workspaceId;
            EntryDescription = entryDescription;
            ProjectId = projectId;
            TaskId = taskId;
            TagIds = tagIds;
        }

        public static StartTimeEntryParameters ForManualMode(DateTimeOffset now)
            => new StartTimeEntryParameters(
                now.Subtract(defaultManualModeDuration),
                Resources.ManualTimeEntryPlaceholder,
                defaultManualModeDuration,
                null);
        
        public static StartTimeEntryParameters ForTimerMode(DateTimeOffset now)
            => new StartTimeEntryParameters(now, Resources.StartTimeEntryPlaceholder, null, null);
    }
}
