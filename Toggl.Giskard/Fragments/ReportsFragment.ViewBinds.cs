using System;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Giskard.Views;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class ReportsFragment
    {
        private FloatingActionButton selectWorkspaceFAB;
        private TextView workspaceName;
        private TextView toolbarCurrentDateRangeText;
        private RecyclerView reportsRecyclerView;
        private ReportsLinearLayout reportsMainContainer;
        private ReportsCalendarView calendarView;
        private Toolbar toolbar;

        protected override void InitializeViews(View fragmentView)
        {
            selectWorkspaceFAB = fragmentView.FindViewById<FloatingActionButton>(Resource.Id.SelectWorkspaceFAB);
            workspaceName = fragmentView.FindViewById<TextView>(Resource.Id.ReportsFragmentWorkspaceName);
            toolbarCurrentDateRangeText = fragmentView.FindViewById<TextView>(Resource.Id.ToolbarCurrentDateRangeText);
            reportsRecyclerView = fragmentView.FindViewById<RecyclerView>(Resource.Id.ReportsFragmentRecyclerView);
            reportsMainContainer = fragmentView.FindViewById<ReportsLinearLayout>(Resource.Id.ReportsFragmentMainContainer);
            calendarView = fragmentView.FindViewById<ReportsCalendarView>(Resource.Id.ReportsFragmentCalendarView);
            reportsMainContainer.CalendarContainer = calendarView;
            toolbar = fragmentView.FindViewById<Toolbar>(Resource.Id.Toolbar);
        }
    }
}
