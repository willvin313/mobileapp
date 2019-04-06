using System;
using System.Linq;
using Toggl.Foundation.Models.Pomodoro;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    public class SelectedSegmentChangeInfo
    {
        public SelectedSegmentChangeInfo(int index, PomodoroWorkflowItem workflowItem)
        {
            Index = index;
            WorkflowItem = workflowItem;
        }

        public int Index { get; }
        public PomodoroWorkflowItem WorkflowItem { get; }
    }
}
