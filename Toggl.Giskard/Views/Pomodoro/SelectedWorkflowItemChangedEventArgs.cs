using System;
using System.Linq;
using Toggl.Foundation.Models.Pomodoro;

namespace Toggl.Giskard.Views.Pomodoro
{
    public class SelectedWorkflowItemChangedEventArgs : EventArgs
    {
        public PomodoroWorkflowItem WorkflowItem { get; private set; }
        public int Index { get; private set; }

        public SelectedWorkflowItemChangedEventArgs(PomodoroWorkflowItem workflowItem, int index)
        {
            WorkflowItem = workflowItem;
            Index = index;
        }
    }
}
