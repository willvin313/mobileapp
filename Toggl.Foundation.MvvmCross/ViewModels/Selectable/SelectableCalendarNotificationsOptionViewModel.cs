using System;
using Toggl.Foundation.UI.Interfaces;
using Toggl.Multivac;

namespace Toggl.Foundation.UI.ViewModels.Selectable
{
    public sealed class SelectableCalendarNotificationsOptionViewModel : IDiffableByIdentifier<SelectableCalendarNotificationsOptionViewModel>
    {
        public CalendarNotificationsOption Option { get; }

        public bool Selected { get; set; }

        public SelectableCalendarNotificationsOptionViewModel(CalendarNotificationsOption option, bool selected)
        {
            Option = option;
            Selected = selected;
        }

        public long Identifier => Option.GetHashCode();

        public bool Equals(SelectableCalendarNotificationsOptionViewModel other) => Option == other.Option;
    }
}
