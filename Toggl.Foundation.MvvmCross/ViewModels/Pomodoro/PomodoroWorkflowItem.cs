using System;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.Pomodoro
{
    public class PomodoroWorkflowItem : IEquatable<PomodoroWorkflowItem>
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

        public bool Equals(PomodoroWorkflowItem other)
        {
            if (other == null)
                return false;

            return Type == other.Type && Minutes == other.Minutes && WorkflowReference == other.WorkflowReference;
        }

        public override int GetHashCode()
            => HashCode.From(Type, Minutes, WorkflowReference);

        public override bool Equals(object obj)
            => Equals(obj as PomodoroWorkflowItem);
    }
}
