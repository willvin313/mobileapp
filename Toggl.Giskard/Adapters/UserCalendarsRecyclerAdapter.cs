using System;
using System.Collections.Generic;
using Android.Views;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.MvvmCross.ViewModels.Selectable;
using Toggl.Giskard.ViewHolders;

namespace Toggl.Giskard.Adapters
{
    public sealed class UserCalendarsRecyclerAdapter
        : BaseSectionedRecyclerAdapter<UserCalendarSourceViewModel, SelectableUserCalendarViewModel>
    {
        protected override BaseRecyclerViewHolder<UserCalendarSourceViewModel> CreateHeaderViewHolder(LayoutInflater inflater, ViewGroup parent, int viewType)
        {
            var view = inflater.Inflate(Resource.Layout.UserCalendarHeader, parent, false);
            return new UserCalendarHeaderViewHolder(view);
        }

        protected override BaseRecyclerViewHolder<SelectableUserCalendarViewModel> CreateItemViewHolder(LayoutInflater inflater, ViewGroup parent, int viewType)
        {
            var view = inflater.Inflate(Resource.Layout.UserCalendarItem, parent, false);
            return new UserCalendarViewHolder(view);
        }
    }
}
