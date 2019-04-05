﻿using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Foundation.MvvmCross.ViewModels.Selectable;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class UserCalendarViewHolder : BaseRecyclerViewHolder<SelectableUserCalendarViewModel>
    {
        private CheckBox checkbox;
        private TextView calendarName;

        public static UserCalendarViewHolder Create(View itemView)
            => new UserCalendarViewHolder(itemView);

        public UserCalendarViewHolder(View itemView)
            : base(itemView)
        {
        }

        public UserCalendarViewHolder(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            checkbox = ItemView.FindViewById<CheckBox>(Resource.Id.Checkbox);
            calendarName = ItemView.FindViewById<TextView>(Resource.Id.CalendarName);
        }

        protected override void UpdateView()
        {
            checkbox.Checked = Item.Selected;
            calendarName.Text = Item.Name;
        }

        protected override void OnItemViewClick(object sender, EventArgs args)
        {
            base.OnItemViewClick(sender, args);
            checkbox.Checked = !checkbox.Checked;
        }

        protected override void UpdateTheme(ITheme theme)
        {
            base.UpdateTheme(theme);
            calendarName.SetTextColor(theme.Text.ToNativeColor());
        }
    }
}
