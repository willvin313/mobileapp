using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.Fragments
{
	public sealed partial class SelectDateFormatFragment
	{
        private RecyclerView recyclerView;
        private TextView title;

        protected override void InitializeViews(View rootView)
        {
            recyclerView = rootView.FindViewById<RecyclerView>(Resource.Id.SelectDateFormatRecyclerView);
            title = rootView.FindViewById<TextView>(Resource.Id.Title);
        }
    }
}
