using System;
using MvvmCross.ViewModels;

namespace Toggl.Foundation.UI.ViewModels.Hints
{
    public sealed class ToggleReportsCalendarVisibilityHint : MvxPresentationHint
    {
        public bool ForceHide { get; }

        public ToggleReportsCalendarVisibilityHint(bool forceHide = false)
        {
            ForceHide = forceHide;
        }
    }
}
