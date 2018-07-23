using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using static Toggl.Giskard.Resource.Id;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class ReportsCalendarFragment
    {
        private ViewPager pager;

        private LinearLayout header;

        private TextView calendarCurrentMonthTextView;

        private RecyclerView shortcutsRecyclerView;

        protected override void InitializeViews(View view)
        {
            pager = view.FindViewById<ViewPager>(ReportsCalendarFragmentViewPager);

            header = view.FindViewById<LinearLayout>(ReportsCalendarFragmentHeader);

            shortcutsRecyclerView = view.FindViewById<RecyclerView>(ReportsCalendarFragmentShortcuts);

            calendarCurrentMonthTextView = view.FindViewById<TextView>(ReportsCalendarCurrentMonthTitle);

        }
    }
}
