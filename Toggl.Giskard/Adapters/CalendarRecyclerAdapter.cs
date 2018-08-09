using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross;
using MvvmCross.Platforms.Android;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Giskard.ViewHolders;

namespace Toggl.Giskard.Adapters
{
    public sealed class CalendarRecyclerAdapter : BaseRecyclerAdapter<CalendarDayViewModel>
    {
        private static readonly int itemWidth;

        static CalendarRecyclerAdapter()
        {
            var context = Mvx.Resolve<IMvxAndroidGlobals>().ApplicationContext;
            var service = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            var display = service.DefaultDisplay;
            var size = new Point();
            display.GetSize(size);

            itemWidth = size.X / 7;
        }

        public CalendarRecyclerAdapter()
        {
        }

        public CalendarRecyclerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            base.OnBindViewHolder(holder, position);

            var layoutParams = holder.ItemView.LayoutParameters;
            layoutParams.Width = itemWidth;
            holder.ItemView.LayoutParameters = layoutParams;
        }

        protected override BaseRecyclerViewHolder<CalendarDayViewModel> CreateViewHolder(ViewGroup parent, LayoutInflater inflater)
        {
            var itemView = inflater.Inflate(Resource.Layout.ReportsCalendarFragmentDayCell, parent, false);
            return new CalendarRecylerViewHolder(itemView);
        }
    }
}
