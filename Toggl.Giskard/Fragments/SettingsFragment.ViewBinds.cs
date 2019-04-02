using System;
using System.Linq;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Giskard.Extensions;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class SettingsFragment
    {
        private View aboutView;
        private View manualModeView;
        private View is24hoursModeView;
        private View runningTimerNotificationsView;
        private View stoppedTimerNotificationsView;
        private View avatarContainer;
        private View dateFormatView;
        private View beginningOfWeekView;
        private View durationFormatView;
        private View smartRemindersView;
        private View darkThemeView;
        private View smartRemindersViewSeparator;

        private TextView helpView;
        private TextView aboutLabel;
        private TextView logoutView;
        private TextView feedbackView;
        private TextView nameTextView;
        private TextView emailTextView;
        private TextView darkThemeLabel;
        private TextView manualModeLabel;
        private TextView versionTextView;
        private TextView stoppedTimeLabel;
        private TextView runningTimerLabel;
        private TextView dateFormatTextView;
        private TextView calendarSettingsView;
        private TextView durationFormatTextView;
        private TextView smartRemindersTextView;
        private TextView beginningOfWeekTextView;
        private TextView useTwentyFourHourClockLabel;

        private ImageView avatarView;

        private Switch darkThemeSwitch;
        private Switch manualModeSwitch;
        private Switch is24hoursModeSwitch;
        private Switch runningTimerNotificationsSwitch;
        private Switch stoppedTimerNotificationsSwitch;

        private RecyclerView workspacesRecyclerView;
        private Toolbar toolbar;

        private View[] separators;
        private TextView[] themeableTextViews;

        protected override void InitializeViews(View fragmentView)
        {
            aboutView = fragmentView.FindViewById(Resource.Id.SettingsAboutContainer);
            manualModeView = fragmentView.FindViewById(Resource.Id.SettingsToggleManualModeView);
            is24hoursModeView = fragmentView.FindViewById(Resource.Id.SettingsIs24HourModeView);
            avatarContainer = fragmentView.FindViewById(Resource.Id.SettingsViewAvatarImageContainer);
            dateFormatView = fragmentView.FindViewById(Resource.Id.SettingsDateFormatView);
            beginningOfWeekView = fragmentView.FindViewById(Resource.Id.SettingsSelectBeginningOfWeekView);
            durationFormatView = fragmentView.FindViewById(Resource.Id.SettingsDurationFormatView);
            smartRemindersView = fragmentView.FindViewById(Resource.Id.SmartRemindersView);
            darkThemeView = fragmentView.FindViewById(Resource.Id.DarkThemeView);
            smartRemindersViewSeparator = fragmentView.FindViewById(Resource.Id.SmartReminderSeparator);
            runningTimerNotificationsView = fragmentView.FindViewById(Resource.Id.SettingsRunningTimerNotificationsView);
            stoppedTimerNotificationsView = fragmentView.FindViewById(Resource.Id.SettingsStoppedTimerNotificationsView);

            helpView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsHelpButton);
            logoutView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsLogoutButton);
            nameTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsNameTextView);
            emailTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsEmailTextView);
            calendarSettingsView = fragmentView.FindViewById<TextView>(Resource.Id.CalendarSettingsView);
            feedbackView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsSubmitFeedbackButton);
            versionTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsAppVersionTextView);
            dateFormatTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsDateFormatTextView);
            beginningOfWeekTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsBeginningOfWeekTextView);
            durationFormatTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsDurationFormatTextView);
            smartRemindersTextView = fragmentView.FindViewById<TextView>(Resource.Id.SmartRemindersTextView);

            avatarView = fragmentView.FindViewById<ImageView>(Resource.Id.SettingsViewAvatarImage);

            darkThemeSwitch = fragmentView.FindViewById<Switch>(Resource.Id.DarkThemeSwitch);
            manualModeSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsIsManualModeEnabledSwitch);
            is24hoursModeSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsIs24HourModeSwitch);
            runningTimerNotificationsSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsAreRunningTimerNotificationsEnabledSwitch);
            stoppedTimerNotificationsSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsAreStoppedTimerNotificationsEnabledSwitch);

            workspacesRecyclerView = fragmentView.FindViewById<RecyclerView>(Resource.Id.SettingsWorkspacesRecyclerView);
            toolbar = fragmentView.FindViewById<Toolbar>(Resource.Id.Toolbar);

            aboutLabel = ((ViewGroup)aboutView).GetChildren<TextView>().First();
            darkThemeLabel = ((ViewGroup)darkThemeView).GetChildren<TextView>().First();
            manualModeLabel = ((ViewGroup)manualModeView).GetChildren<TextView>().First();
            useTwentyFourHourClockLabel = ((ViewGroup)is24hoursModeView).GetChildren<TextView>().First();
            stoppedTimeLabel = ((ViewGroup)stoppedTimerNotificationsView).GetChildren<TextView>().First();
            runningTimerLabel = ((ViewGroup)runningTimerNotificationsView).GetChildren<TextView>().First();

            separators = new[]
            {
                Resource.Id.WorkspaceSeparator,
                Resource.Id.DateAndTimeSeparator,
                Resource.Id.DateFormatSeparator,
                Resource.Id.FirstDayOfWeekSeparator,
                Resource.Id.DurationSeparator,
                Resource.Id.Is24HourSeparator,
                Resource.Id.ManualModeSeparator,
                Resource.Id.CalendarSettingsSeparator,
                Resource.Id.SmartReminderSeparator,
                Resource.Id.NotificationsSeparator,
                Resource.Id.RunningTimerSeparator,
                Resource.Id.StoppedTimerSeparator,
                Resource.Id.GeneralSeparator,
                Resource.Id.DarkThemeSeparator,
                Resource.Id.SubmitFeedbackSeparator,
                Resource.Id.HelpSeparator,
                Resource.Id.AboutSeparator,
                Resource.Id.LogoutSeparator
            }.Select(fragmentView.FindViewById).ToArray();

            themeableTextViews = new []
            {
                nameTextView,
                emailTextView,
                versionTextView,
                dateFormatTextView,
                beginningOfWeekTextView,
                durationFormatTextView,
                smartRemindersTextView,
                helpView,
                feedbackView,
                aboutLabel,
                darkThemeLabel,
                manualModeLabel,
                stoppedTimeLabel,
                runningTimerLabel,
                calendarSettingsView,
                useTwentyFourHourClockLabel
            };
        }
    }
}
