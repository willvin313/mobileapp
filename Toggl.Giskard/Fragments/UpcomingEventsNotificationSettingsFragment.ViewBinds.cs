using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class UpcomingEventsNotificationSettingsFragment
    {
        private RecyclerView recyclerView;
        private TextView title;
        private TextView subTitle;

        protected override void InitializeViews(View view)
        {
            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.CalendarsRecyclerView);
            title = view.FindViewById<TextView>(Resource.Id.Title);
            subTitle = view.FindViewById<TextView>(Resource.Id.SubTitle);
        }
    }
}
