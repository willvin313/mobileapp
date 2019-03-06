using System;
using System.Reactive.Subjects;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.Calendar;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.ViewHolders
{
    public class CalendarEntryViewHolder : BaseRecyclerViewHolder<CalendarItem>
    {
        private TextView label;

        public ISubject<(CalendarItem, RecyclerView.ViewHolder)> ItemTappedSubject { get; set; }

        public CalendarEntryViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CalendarEntryViewHolder(View itemView) : base(itemView)
        {
        }

        protected override void InitializeViews()
        {
            label = ItemView.FindViewById<TextView>(Resource.Id.EntryLabel);
        }

        protected override void UpdateView()
        {
            ItemView.Background.SetTint(Color.ParseColor(Item.Color));
            label.Text = Item.Description;
        }

        protected override void OnItemViewClick(object sender, EventArgs args)
        {
            ItemTappedSubject?.OnNext((Item, this));
        }
    }
}
