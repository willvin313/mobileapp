using System;

namespace Toggl.Foundation.MvvmCross.ViewModels.Pomodoro
{
    public class PomodoroWorkflowItem
    {
        public PomodoroWorkflowItemType Type { get; }
        public int Minutes { get; }
        public string WorkflowReference { get; }

        public TimeSpan Duration => TimeSpan.FromMinutes(Minutes);

        public PomodoroWorkflowItem(PomodoroWorkflowItemType type, int minutes, string workflowReference = null)
        {
            Type = type;
            Minutes = minutes;
            WorkflowReference = workflowReference ?? string.Empty;
        }
    }
}
