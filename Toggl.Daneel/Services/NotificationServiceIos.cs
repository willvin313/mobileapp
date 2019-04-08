using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Foundation;
using Toggl.Daneel.Extensions;
using Toggl.Foundation;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.UI.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using UserNotifications;
using Notification = Toggl.Multivac.Notification;
using FoundationResources = Toggl.Foundation.Resources;

namespace Toggl.Daneel.Services
{
    public sealed class NotificationServiceIos : PermissionAwareNotificationService
    {
        public const string CalendarEventIdKey = "Id";

        public const string CalendarEventCategory = "CalendarEventCategory";

        public const string OpenAndCreateFromCalendarEvent = "OpenAndStartTimeEntryFromCalendarEvent";
        public const string OpenAndNavigateToCalendar = "OpenAndNavigateToCalendar";
        public const string StartTimeEntryInBackground = "StartTimeEntryInBackground";

        private readonly ITimeService timeService;

        public NotificationServiceIos(IPermissionsService permissionsService, ITimeService timeService)
            : base(permissionsService)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.timeService = timeService;

            var openAndCreateFromCalendarEventAction = UNNotificationAction.FromIdentifier(
                OpenAndCreateFromCalendarEvent,
                FoundationResources.OpenAppAndStartAction,
                UNNotificationActionOptions.AuthenticationRequired | UNNotificationActionOptions.Foreground
            );

            var openAndNavigateToCalendarAction = UNNotificationAction.FromIdentifier(
                OpenAndNavigateToCalendar,
                FoundationResources.OpenAppAction,
                UNNotificationActionOptions.AuthenticationRequired | UNNotificationActionOptions.Foreground
            );

            var startTimeEntryInBackgroundAction = UNNotificationAction.FromIdentifier(
                StartTimeEntryInBackground,
                FoundationResources.StartInBackgroundAction,
                UNNotificationActionOptions.AuthenticationRequired
            );

            var calendarEventCategory = UNNotificationCategory.FromIdentifier(
                CalendarEventCategory,
                new UNNotificationAction[] { openAndCreateFromCalendarEventAction, startTimeEntryInBackgroundAction, openAndNavigateToCalendarAction },
                new string[] { },
                UNNotificationCategoryOptions.None
            );

            UNUserNotificationCenter.Current.SetNotificationCategories(new NSSet<UNNotificationCategory>(calendarEventCategory));
        }

        protected override IObservable<Unit> NativeSchedule(IImmutableList<Notification> notifications)
            => Observable.FromAsync(async () =>
                await notifications
                    .Select(notificationRequest)
                    .Select(UNUserNotificationCenter.Current.AddNotificationRequestAsync)
                    .Apply(Task.WhenAll)
            );

        protected override IObservable<Unit> NativeUnscheduleAllNotifications()
        {
            UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();

            return Observable.Return(Unit.Default);
        }

        private UNNotificationRequest notificationRequest(Notification notification)
        {
            var identifier = notification.GetIdentifier();

            var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(
                notification.GetTriggerTime(timeService.CurrentDateTime),
                repeats: false
            );

            var content = new UNMutableNotificationContent
            {
                Title = notification.Title,
                Body = notification.Description,
                Sound = UNNotificationSound.Default,
                UserInfo = new NSDictionary(CalendarEventIdKey, notification.Id),
                CategoryIdentifier = CalendarEventCategory
            };

            return UNNotificationRequest.FromIdentifier(identifier, content, trigger);
        }
    }
}
