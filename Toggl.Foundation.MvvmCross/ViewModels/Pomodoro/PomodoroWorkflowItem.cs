using System;

namespace Toggl.Foundation.MvvmCross.ViewModels.Pomodoro
{
    public class PomodoroWorkflowItem
    {
        public PomodoroWorkflowItemType Type { get; }
        public TimeSpan Duration { get; }
        public string WorkflowReference { get; }

        public PomodoroWorkflowItem(PomodoroWorkflowItemType type, TimeSpan duration, string workflowReference = null)
        {
            Type = type;
            Duration = duration;
            WorkflowReference = workflowReference ?? string.Empty;
        }

        public PomodoroWorkflowItem(PomodoroWorkflowItemType type, int minutes, string workflowReference = null)
            : this(type, TimeSpan.FromMinutes(minutes), workflowReference) { }
    }
}
