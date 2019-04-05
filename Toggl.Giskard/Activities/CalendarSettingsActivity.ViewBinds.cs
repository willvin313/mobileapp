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
        private TextView selectCalendarsTextView;
        private TextView selectCalendarsSubTextView;
        private View separator;
        private View activityTopSeparator;
        private View activityBottomSeparator;
        private TextView linkCalendarsText;
        private TextView linkCalendarsSubText;

        protected override void InitializeViews()
        {
            toggleCalendarsView = FindViewById(Resource.Id.ToggleCalendarsView);
            toggleCalendarsSwitch = FindViewById<Switch>(Resource.Id.ToggleCalendarsSwitch);
            calendarsContainer = FindViewById(Resource.Id.CalendarsContainer);
            calendarsRecyclerView = FindViewById<RecyclerView>(Resource.Id.CalendarsRecyclerView);
            toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);
            selectCalendarsTextView = FindViewById<TextView>(Resource.Id.SelectCalendarsTextView);
            selectCalendarsSubTextView = FindViewById<TextView>(Resource.Id.SelectCalendarsSubTextView);
            separator = FindViewById(Resource.Id.Separator);
            activityTopSeparator = FindViewById(Resource.Id.CalendarActivityTopSeparator);
            activityBottomSeparator = FindViewById(Resource.Id.CalendarActivityBottomSeparator);
            linkCalendarsText = FindViewById<TextView>(Resource.Id.LinkCalendarsText);
            linkCalendarsSubText = FindViewById<TextView>(Resource.Id.LinkCalendarsSubText);
        }
    }
}
