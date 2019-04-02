using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHelpers;

namespace Toggl.Giskard.ViewHolders
{
    public class MainLogSectionViewHolder : BaseRecyclerViewHolder<TimeEntryCollectionViewModel>
    {
        private TextView mainLogHeaderTitle;
        private TextView mainLogHeaderDuration;

        public DateTimeOffset Now { private get; set; }

        public MainLogSectionViewHolder(View itemView) : base(itemView)
        {
        }

        public MainLogSectionViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            mainLogHeaderTitle = ItemView.FindViewById<TextView>(Resource.Id.MainLogHeaderTitle);
            mainLogHeaderDuration = ItemView.FindViewById<TextView>(Resource.Id.MainLogHeaderDuration);
        }

        protected override void UpdateView()
        {
            mainLogHeaderTitle.Text = Item.DurationText;
            mainLogHeaderDuration.Text = Item.HeaderDate(Now);
        }

        protected override void UpdateTheme(ITheme theme)
        {
            mainLogHeaderTitle.SetTextColor(theme.Text.ToNativeColor());
            mainLogHeaderDuration.SetTextColor(theme.Text.ToNativeColor());
        }
    }
}
