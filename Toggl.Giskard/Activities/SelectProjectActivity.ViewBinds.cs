using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using static Toggl.Giskard.Resource.Id;

namespace Toggl.Giskard.Activities
{
    public sealed partial class SelectProjectActivity
    {
        private View closeButton;
        private TextView searchField;
        private RecyclerView recyclerView;

        protected override void InitializeViews()
        {
            closeButton = FindViewById(CloseButton);
            searchField = FindViewById<TextView>(SearchField);
            recyclerView = FindViewById<RecyclerView>(Resource.Id.RecyclerView);
        }
    }
}
