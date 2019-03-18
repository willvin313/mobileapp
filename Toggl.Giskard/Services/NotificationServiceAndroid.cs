using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Toggl.Foundation.Services;
using Toggl.Giskard.BroadcastReceivers;
using Toggl.Giskard.Extensions;
using Toggl.Multivac.Extensions;
using Notification = Android.App.Notification;
using Uri = Android.Net.Uri;

namespace Toggl.Giskard.Services
{
    public sealed class NotificationServiceAndroid : INotificationService
    {
        private const string scheduledNotificationsSharedPreferencesName = "TogglNotifications";
        private const string scheduledNotificationsStorageKey = "TogglNotificationIds";
        private readonly ISharedPreferences sharedPreferences;

        public NotificationServiceAndroid()
        {
            sharedPreferences = Application.Context.GetSharedPreferences(scheduledNotificationsSharedPreferencesName, FileCreationMode.Private);
        }

        public IObservable<Unit> Schedule(IImmutableList<Multivac.Notification> notifications)
            => Observable.Start(() =>
            {
                var context = Application.Context;
                var notificationIntent = context.PackageManager.GetLaunchIntentForPackage(context.PackageName);
                notificationIntent.SetPackage(null);
                var pendingIntent = PendingIntent.GetActivity(context, 0, notificationIntent, 0);
                var notificationManager = NotificationManager.FromContext(context);
                var notificationsBuilder = context.CreateNotificationBuilderWithDefaultChannel(notificationManager);
                var notificationIdsToBeSaved = new List<string>(notifications.Count);

                foreach (var notification in notifications)
                {
                    var notificationId = notification.Id.GetHashCode(); // this is a temp measure, might be better to change this in the future
                    var androidNotification = notificationsBuilder.SetAutoCancel(true)
                        .SetContentIntent(pendingIntent)
                        .SetContentTitle(notification.Title)
                        .SetSmallIcon(Resource.Drawable.ic_icon_running)
                        .SetContentText(notification.Description)
                        .Build();
                    var scheduleAt = notification.ScheduledTime;

                    scheduleNotification(notificationId, androidNotification, scheduleAt);
                    notificationIdsToBeSaved.Add(notification.Id);
                }

                saveScheduledNotificationIds(notificationIdsToBeSaved);
            });

        public IObservable<Unit> UnscheduleAllNotifications()
            => Observable.Start(() =>
            {
                var context = Application.Context;
                var notificationManager = NotificationManagerCompat.From(context);
                notificationManager.CancelAll();

                var notificationIds = getSavedNotificationIds();
                var alarmManager = (AlarmManager) Application.Context.GetSystemService(Context.AlarmService);
                notificationIds.ForEach(notificationId => unScheduleNotification(notificationId, alarmManager));
                clearSavedNotificationIds();
            });

        private static void scheduleNotification(int notificationId, Notification notification, DateTimeOffset scheduleAt)
        {
            var alarmManager = (AlarmManager) Application.Context.GetSystemService(Context.AlarmService);
            var scheduledNotificationIntent = new Intent(Application.Context, typeof(SmartAlertCalendarEventBroadcastReceiver));
            scheduledNotificationIntent.SetData(getSmartAlertIdentifierUri(notificationId));
            scheduledNotificationIntent.PutExtra(SmartAlertCalendarEventBroadcastReceiver.NotificationId, notificationId);
            scheduledNotificationIntent.PutExtra(SmartAlertCalendarEventBroadcastReceiver.Notification, notification);

            cancelExistingPendingIntentIfNecessary(notificationId, scheduledNotificationIntent, alarmManager);

            var pendingIntent = PendingIntent.GetBroadcast(Application.Context, notificationId, scheduledNotificationIntent, PendingIntentFlags.CancelCurrent);
            var futureInMillis = (long) (scheduleAt - DateTimeOffset.Now).TotalMilliseconds;

            alarmManager.SetExact(AlarmType.ElapsedRealtimeWakeup, SystemClock.ElapsedRealtime() + futureInMillis, pendingIntent);
        }

        private void unScheduleNotification(string notificationId, AlarmManager alarmManager)
        {
            var scheduledNotificationIntent = new Intent(Application.Context, typeof(SmartAlertCalendarEventBroadcastReceiver));
            scheduledNotificationIntent.SetData(getSmartAlertIdentifierUri(notificationId.GetHashCode()));
            cancelExistingPendingIntentIfNecessary(notificationId.GetHashCode(), scheduledNotificationIntent, alarmManager);
        }

        private static void cancelExistingPendingIntentIfNecessary(int notificationId, Intent scheduledNotificationIntent, AlarmManager alarmManager)
        {
            var oldIntent = PendingIntent.GetBroadcast(Application.Context, notificationId, scheduledNotificationIntent, PendingIntentFlags.NoCreate);

            if (oldIntent == null) return;

            alarmManager.Cancel(oldIntent);
        }

        private static Uri getSmartAlertIdentifierUri(int notificationId)
            => Uri.Parse($"toggl-notifications://smartAlerts/{notificationId}");

        private IEnumerable<string> getSavedNotificationIds()
            => sharedPreferences.GetStringSet(scheduledNotificationsStorageKey, new List<string>())
                .ToImmutableList();

        private void clearSavedNotificationIds()
        {
            sharedPreferences.Edit()
                .Remove(scheduledNotificationsStorageKey)
                .Commit();
        }

        private void saveScheduledNotificationIds(ICollection<string> notificationIds)
        {
            sharedPreferences.Edit()
                .PutStringSet(scheduledNotificationsStorageKey, notificationIds)
                .Commit();
        }
    }
}
