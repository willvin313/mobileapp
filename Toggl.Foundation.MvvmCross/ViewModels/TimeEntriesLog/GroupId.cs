using System;
using System.Linq;
using System.Text.RegularExpressions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog
{
    public sealed class GroupId : IEquatable<GroupId>
    {
        private readonly DateTime date;
        private readonly string description;
        private readonly long workspaceId;
        private readonly long? projectId;
        private readonly long? taskId;
        private readonly bool isBillable;
        private readonly long[] tagIds;

        public GroupId(IThreadSafeTimeEntry sample)
        {
            date = sample.Start.LocalDateTime.Date;
            description = sample.Description;
            workspaceId = sample.WorkspaceId;
            projectId = sample.Project?.Id;
            taskId = sample.Task?.Id;
            isBillable = sample.Billable;
            tagIds = sample.TagIds.OrderBy(tag => tag).ToArray();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is GroupId other && Equals(other);
        }

        public bool Equals(GroupId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return date.Equals(other.date)
                   && string.Equals(description, other.description)
                   && workspaceId == other.workspaceId
                   && projectId == other.projectId
                   && taskId == other.taskId
                   && isBillable == other.isBillable
                   && Equals(tagIds, other.tagIds);
        }

        public override int GetHashCode()
        {
            var hashCode = HashCode.From(date, description, workspaceId, projectId, taskId, isBillable);
            return tagIds.Aggregate(hashCode, HashCode.From);
        }
    }
}
