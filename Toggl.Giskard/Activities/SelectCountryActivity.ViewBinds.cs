using Android.Support.V7.Widget;
using Android.Widget;

namespace Toggl.Giskard.Activities
{
    public partial class SelectCountryActivity
    {
        private ImageView backImageView;
        private EditText filterEditText;
        private RecyclerView recyclerView;

        protected override void InitializeViews()
        {
            backImageView = FindViewById<ImageView>(Resource.Id.BackImageView);
            filterEditText = FindViewById<EditText>(Resource.Id.FilterEditText);
            recyclerView = FindViewById<RecyclerView>(Resource.Id.RecyclerView);
        }
    }
}