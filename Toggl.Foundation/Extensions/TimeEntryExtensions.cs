using System;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.Models;

namespace Toggl.Foundation.Extensions
{
    public static class TimeEntryExtensions
    {
        public static ITimeEntryPrototype AsTimeEntryPrototype(this string description, DateTimeOffset startTime, long workspaceId)
            => new TimeEntryPrototype(
                workspaceId,
                description: description,
                duration: null,
                startTime: startTime,
                projectId: null,
                taskId: null,
                tagIds: null,
                isBillable: false
            );

        public static ITimeEntryPrototype AsTimeEntryPrototype(this CalendarItem calendarItem, long workspaceId)
            => new TimeEntryPrototype(
                workspaceId,
                description: calendarItem.Description,
                duration: calendarItem.Duration,
                startTime: calendarItem.StartTime,
                projectId: null,
                taskId: null,
                tagIds: null,
                isBillable: false
            );

        public static ITimeEntryPrototype AsTimeEntryPrototype(this TimeSpan timeSpan, DateTimeOffset startTime, long workspaceId)
            => new TimeEntryPrototype(
                workspaceId,
                description: "",
                duration: timeSpan,
                startTime: startTime,
                projectId: null, 
                taskId: null, 
                tagIds: null, 
                isBillable: false
            );

        private sealed class TimeEntryPrototype : ITimeEntryPrototype
        {
            public long WorkspaceId { get; }

            public string Description { get; }

            public TimeSpan? Duration { get; }

            public DateTimeOffset StartTime { get; }

            public long? ProjectId { get; }

            public long? TaskId { get; }

            public long[] TagIds { get; }

            public bool IsBillable { get; }

            public TimeEntryPrototype(
                long workspaceId,
                string description,
                TimeSpan? duration,
                DateTimeOffset startTime,
                long? projectId,
                long? taskId,
                long[] tagIds,
                bool isBillable)
            {
                WorkspaceId = workspaceId;
                Description = description;
                Duration = duration;
                StartTime = startTime;
                ProjectId = projectId;
                TaskId = taskId;
                TagIds = tagIds;
                IsBillable = isBillable;
            }
        }
    }
}
