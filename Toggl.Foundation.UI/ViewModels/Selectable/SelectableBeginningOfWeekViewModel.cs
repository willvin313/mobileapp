using System;
using MvvmCross.ViewModels;
using Toggl.Foundation.UI.Interfaces;
using Toggl.Multivac;

namespace Toggl.Foundation.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectableBeginningOfWeekViewModel : IDiffableByIdentifier<SelectableBeginningOfWeekViewModel>
    {
        public BeginningOfWeek BeginningOfWeek { get; }

        public bool Selected { get; set; }

        public long Identifier => (long)BeginningOfWeek;

        public SelectableBeginningOfWeekViewModel(BeginningOfWeek beginningOfWeek, bool selected)
        {
            BeginningOfWeek = beginningOfWeek;
            Selected = selected;
        }

        public bool Equals(SelectableBeginningOfWeekViewModel other) 
            => BeginningOfWeek == other.BeginningOfWeek && Selected == other.Selected;
    }
}
