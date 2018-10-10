using System;
using System.Collections.Generic;
using System.Linq;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.MvvmCross.Transformations;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.ViewHolders
{
    public class MainLogSectionViewHolder : BaseRecyclerViewHolder<IReadOnlyList<TimeEntryViewModel>>
    {
        private TextView mainLogHeaderTitle;
        private TextView mainLogHeaderDuration;

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
            if (Item.Count == 0)
                return;

            var firstItem = Item.First();
            mainLogHeaderTitle.Text = DateToTitleString.Convert(firstItem.StartTime.Date);

            var totalDuration = Item.Sum(vm => vm.Duration);
            mainLogHeaderDuration.Text = totalDuration.ToFormattedString(firstItem.DurationFormat);
        }
    }
}
