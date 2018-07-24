using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Giskard.Views;
using Object = Java.Lang.Object;

namespace Toggl.Giskard.Adapters
{
    public sealed class CalendarPagerAdapter : PagerAdapter
    {
        private readonly Context context;
        private readonly Action<CalendarDayViewModel> calendarDayTapped;
        private readonly IDictionary<int, RecyclerView.Adapter> adapters = new Dictionary<int, RecyclerView.Adapter>();

        private IImmutableList<CalendarPageViewModel> months = ImmutableList<CalendarPageViewModel>.Empty;
        public IImmutableList<CalendarPageViewModel> Months 
        {
            get => months;
            set
            {
                months = value;
                NotifyDataSetChanged();
            }
        }

        public CalendarPagerAdapter(Context context, Action<CalendarDayViewModel> calendarDayTapped)
        {
            this.context = context;
            this.calendarDayTapped = calendarDayTapped;
        }

        public CalendarPagerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override int Count 
            => Months.Count;

        public override Object InstantiateItem(ViewGroup container, int position)
        {
            var inflater = LayoutInflater.FromContext(context);
            var inflatedView = inflater.Inflate(Resource.Layout.ReportsCalendarFragmentPage, container, false);

            var adapter = new CalendarRecyclerAdapter();
            adapter.Items = Months[position].Days;
            adapter.OnItemTapped = calendarDayTapped;
            adapters[position] = adapter;

            var calendarRecyclerView = (CalendarRecyclerView)inflatedView;
            calendarRecyclerView.SetAdapter(adapter);
            container.AddView(inflatedView);
            adapter.NotifyItemRangeChanged(0, Months[position].Days.Count);

            return inflatedView;
        }

        public override void DestroyItem(ViewGroup container, int position, Object @object)
        {
            adapters[position] = null;
            container.RemoveView(@object as View);
        }

        public override bool IsViewFromObject(View view, Object @object)
            => view == @object;

        public override void NotifyDataSetChanged()
        {
            foreach (var adapter in adapters.Values)
            {
                adapter?.NotifyDataSetChanged();
            }
        }
    }
}
