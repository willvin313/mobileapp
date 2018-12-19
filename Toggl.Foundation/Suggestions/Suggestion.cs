using System;
using System.Linq;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.Helper;
using Toggl.Foundation.Models;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Suggestions
{
    [Preserve(AllMembers = true)]
    public sealed class Suggestion : ITimeEntryPrototype, IEquatable<Suggestion>
    {
        public string Description { get; } = "";

        public long? ProjectId { get; } = null;

        public long? TaskId { get; } = null;

        public long? ClientId { get; } = null;

        public string ProjectColor { get; } = Color.NoProject;

        public string ProjectName { get; } = "";

        public string TaskName { get; } = "";

        public string ClientName { get; } = "";

        public bool HasProject { get; } = false;

        public long WorkspaceId { get; }

        public bool IsBillable { get; } = false;

        public long[] TagIds { get; } = Array.Empty<long>();

        public DateTimeOffset StartTime { get; }

        public TimeSpan? Duration { get; } = null;

        public float Certainty { get; }

        public SuggestionProviderType ProviderName { get; }

        internal Suggestion(IDatabaseTimeEntry timeEntry, float certainty, SuggestionProviderType providerName)
        {
            Ensure.Argument.IsInClosedRange(certainty, 0, 1, nameof(certainty));

            Certainty = certainty;
            ProviderName = providerName;

            TaskId = timeEntry.TaskId;
            ProjectId = timeEntry.ProjectId;
            IsBillable = timeEntry.Billable;
            Description = timeEntry.Description;
            WorkspaceId = timeEntry.WorkspaceId;

            if (timeEntry.Project == null) return;

            HasProject = true;
            ProjectName = timeEntry.Project.Name;
            ProjectColor = timeEntry.Project.Color;

            ClientName = timeEntry.Project.Client?.Name ?? "";
            ClientId = timeEntry.Project.ClientId;

            if (timeEntry.Task == null) return;

            TaskName = timeEntry.Task.Name;
        }

        internal Suggestion(CalendarItem calendarItem, long workspaceId, float certainty, SuggestionProviderType providerType)
        {
            Ensure.Argument.IsInClosedRange(certainty, 0, 1, nameof(certainty));
            Ensure.Argument.IsNotNullOrWhiteSpaceString(calendarItem.Description, nameof(calendarItem.Description));

            Certainty = certainty;
            WorkspaceId = workspaceId;
            ProviderName = providerType;
            Description = calendarItem.Description;
        }

        public bool Equals(Suggestion other)
        {
            if (other == null) return false;

            if (Description != other.Description) return false;
            if (ProjectId != other.ProjectId) return false;
            if (TaskId != other.TaskId) return false;
            if (ClientId != other.ClientId) return false;
            if (ProjectColor != other.ProjectColor) return false;
            if (ProjectName != other.ProjectName) return false;
            if (TaskName != other.TaskName) return false;
            if (ClientName != other.ClientName) return false;
            if (WorkspaceId != other.WorkspaceId) return false;
            if (IsBillable != other.IsBillable) return false;
            if (!TagIds.SequenceEqual(other.TagIds)) return false;
            if (StartTime != other.StartTime) return false;
            if (Duration != other.Duration) return false;

            return true;
        }
    }
}
