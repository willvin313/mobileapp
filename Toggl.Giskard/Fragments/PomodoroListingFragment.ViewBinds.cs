using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class PomodoroListingFragment
    {
        private RecyclerView workflowsListView;

        protected override void InitializeViews(View fragmentView)
        {
            workflowsListView = fragmentView.FindViewById<RecyclerView>(Resource.Id.WorkflowListView);
        }
    }
}
