using Android.Views;
using Toggl.Giskard.Views;
using Android.Widget;

namespace Toggl.Giskard.Fragments
{
    public partial class CalendarFragment
    {
        private View headerCalendarEventsView;
        private View headerTimeEntriesView;
        private TextView headerDayTextView;
        private TextView headerWeekdayTextView;
        private TextView headerCalendarEventsTextView;
        private TextView headerTimeEntriesTextView;
        private Button headerLinkCalendarsButton;
        private CalendarRecyclerView calendarRecyclerView;
        private ViewStub onboardingViewStub;
        private View onboardingView;
        private Button getStartedButton;
        private TextView skipButton;

        protected override void InitializeViews(View view)
        {
            headerCalendarEventsView = view.FindViewById(Resource.Id.HeaderCalendarEventsView);
            headerTimeEntriesView = view.FindViewById(Resource.Id.HeaderTimeEntriesView);
            headerDayTextView = view.FindViewById<TextView>(Resource.Id.Day);
            headerWeekdayTextView = view.FindViewById<TextView>(Resource.Id.Weekday);
            headerCalendarEventsTextView = view.FindViewById<TextView>(Resource.Id.HeaderCalendarEventsTextView);
            headerTimeEntriesTextView = view.FindViewById<TextView>(Resource.Id.HeaderTimeEntriesTextView);
            headerLinkCalendarsButton = view.FindViewById<Button>(Resource.Id.HeaderLinkCalendarsButton);
            calendarRecyclerView = view.FindViewById<CalendarRecyclerView>(Resource.Id.calendarRecyclerView);
            onboardingViewStub = view.FindViewById<ViewStub>(Resource.Id.OnboardingViewStub);
        }
    }
}
