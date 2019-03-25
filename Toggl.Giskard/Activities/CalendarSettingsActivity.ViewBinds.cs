using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Activities
{
    public sealed partial class CalendarSettingsActivity
    {
        private View toggleCalendarsView;
        private Switch toggleCalendarsSwitch;
        private View calendarsContainer;
        private RecyclerView calendarsRecyclerView;
        private Toolbar toolbar;

        protected override void InitializeViews()
        {
            toggleCalendarsView = FindViewById(Resource.Id.ToggleCalendarsView);
            toggleCalendarsSwitch = FindViewById<Switch>(Resource.Id.ToggleCalendarsSwitch);
            calendarsContainer = FindViewById(Resource.Id.CalendarsContainer);
            calendarsRecyclerView = FindViewById<RecyclerView>(Resource.Id.CalendarsRecyclerView);
            toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);
        }
    }
}
