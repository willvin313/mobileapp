using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Giskard.Adapters;

namespace Toggl.Giskard.Fragments
{
    public partial class SelectDurationFormatFragment
    {
        private RecyclerView recyclerView;
        private TextView title;
        private SelectDurationFormatRecyclerAdapter selectDurationRecyclerAdapter;
        protected override void InitializeViews(View view)
        {
            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.SelectDurationFormatRecyclerView);
            title = view.FindViewById<TextView>(Resource.Id.Title);
        }
    }
}
