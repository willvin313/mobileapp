﻿using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.UI.ViewModels.Selectable;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class SelectCalendarNotificationsOptionViewHolder : BaseRecyclerViewHolder<SelectableCalendarNotificationsOptionViewModel>
    {
        private RadioButton radioButton;
        private TextView optionName;

        public SelectCalendarNotificationsOptionViewHolder(View itemView) : base(itemView)
        {
        }

        public SelectCalendarNotificationsOptionViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            radioButton = ItemView.FindViewById<RadioButton>(Resource.Id.RadioButton);
            optionName = ItemView.FindViewById<TextView>(Resource.Id.OptionName);
        }

        protected override void UpdateView()
        {
            radioButton.Checked = Item.Selected;
            optionName.Text = Item.Option.Title();
        }
    }
}
