using System;
namespace Toggl.Foundation.UI.Parameters
{
    public sealed class SelectProjectParameter
    {
        public long? ProjectId { get; set; }

        public long? TaskId { get; set; }

        public long WorkspaceId { get; set; }

        public static SelectProjectParameter WithIds(long? projectId, long? taskId, long workspaceId)
            => new SelectProjectParameter { ProjectId = projectId, TaskId = taskId, WorkspaceId = workspaceId };
    }
}
