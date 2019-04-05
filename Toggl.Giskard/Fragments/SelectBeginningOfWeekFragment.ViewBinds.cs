using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Fragments
{
    public partial class SelectBeginningOfWeekFragment
    {
        private RecyclerView recyclerView;
        private TextView title;
        private TextView subTitle;

        protected override void InitializeViews(View view)
        {
            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.DaysListRecyclerView);
            title = view.FindViewById<TextView>(Resource.Id.Title);
            subTitle = view.FindViewById<TextView>(Resource.Id.SubTitle);
        }
    }
}
