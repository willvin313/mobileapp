using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.Activities
{
    public partial class SettingsActivity
    {
        private View helpView;
        private View aboutView;
        private View logoutView;
        private View feedbackView;
        private View manualModeView;
        private View groupTimeEntriesView;
        private View is24hoursModeView;
        private View runningTimerNotificationsView;
        private View stoppedTimerNotificationsView;
        private View avatarContainer;
        private View dateFormatView;
        private View beginningOfWeekView;
        private View durationFormatView;

        private TextView nameTextView;
        private TextView emailTextView;
        private TextView versionTextView;
        private TextView dateFormatTextView;
        private TextView beginningOfWeekTextView;
        private TextView durationFormatTextView;

        private ImageView avatarView;

        private Switch groupTimeEntriesSwitch;
        private Switch is24hoursModeSwitch;
        private Switch manualModeSwitch;
        private Switch runningTimerNotificationsSwitch;
        private Switch stoppedTimerNotificationsSwitch;

        private RecyclerView workspacesRecyclerView;

        protected override void InitializeViews()
        {
            helpView = FindViewById(Resource.Id.SettingsHelpButton);
            aboutView = FindViewById(Resource.Id.SettingsAboutContainer);
            logoutView = FindViewById(Resource.Id.SettingsLogoutButton);
            feedbackView = FindViewById(Resource.Id.SettingsSubmitFeedbackButton);
            manualModeView = FindViewById(Resource.Id.SettingsToggleManualModeView);
            is24hoursModeView = FindViewById(Resource.Id.SettingsIs24HourModeView);
            groupTimeEntriesView = FindViewById(Resource.Id.GroupTimeEntriesView);
            avatarContainer = FindViewById(Resource.Id.SettingsViewAvatarImageContainer);
            dateFormatView = FindViewById(Resource.Id.SettingsDateFormatView);
            beginningOfWeekView = FindViewById(Resource.Id.SettingsSelectBeginningOfWeekView);
            durationFormatView = FindViewById(Resource.Id.SettingsDurationFormatView);
            runningTimerNotificationsView = FindViewById(Resource.Id.SettingsRunningTimerNotificationsView);
            stoppedTimerNotificationsView = FindViewById(Resource.Id.SettingsStoppedTimerNotificationsView);

            nameTextView = FindViewById<TextView>(Resource.Id.SettingsNameTextView);
            emailTextView = FindViewById<TextView>(Resource.Id.SettingsEmailTextView);
            versionTextView = FindViewById<TextView>(Resource.Id.SettingsAppVersionTextView);
            dateFormatTextView = FindViewById<TextView>(Resource.Id.SettingsDateFormatTextView);
            beginningOfWeekTextView = FindViewById<TextView>(Resource.Id.SettingsBeginningOfWeekTextView);
            durationFormatTextView = FindViewById<TextView>(Resource.Id.SettingsDurationFormatTextView);

            avatarView = FindViewById<ImageView>(Resource.Id.SettingsViewAvatarImage);
            manualModeSwitch = FindViewById<Switch>(Resource.Id.SettingsIsManualModeEnabledSwitch);
            is24hoursModeSwitch = FindViewById<Switch>(Resource.Id.SettingsIs24HourModeSwitch);
            runningTimerNotificationsSwitch = FindViewById<Switch>(Resource.Id.SettingsAreRunningTimerNotificationsEnabledSwitch);
            stoppedTimerNotificationsSwitch = FindViewById<Switch>(Resource.Id.SettingsAreStoppedTimerNotificationsEnabledSwitch);
            groupTimeEntriesSwitch = FindViewById<Switch>(Resource.Id.GroupTimeEntriesSwitch);

            workspacesRecyclerView = FindViewById<RecyclerView>(Resource.Id.SettingsWorkspacesRecyclerView);
        }
    }
}
