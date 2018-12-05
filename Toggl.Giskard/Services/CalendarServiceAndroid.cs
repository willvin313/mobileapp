using System;
using System.Collections.Generic;
using Android.Database;
using MvvmCross;
using MvvmCross.Platforms.Android;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using static Android.Provider.CalendarContract;

namespace Toggl.Giskard.Services
{
    public sealed class CalendarServiceAndroid : PermissionAwareCalendarService
    {
        private static readonly string[] calendarProjection =
        {
            Calendars.InterfaceConsts.Id,
            Calendars.InterfaceConsts.CalendarDisplayName,
            Calendars.InterfaceConsts.AccountName,
        };

        private static readonly string[] calendarIdProjection =
        {
            Calendars.InterfaceConsts.Id
        };

        private static readonly string[] eventsProjection =
        {
            Events.InterfaceConsts.Id,
            Events.InterfaceConsts.Dtstart,
            Events.InterfaceConsts.Dtend,
            Events.InterfaceConsts.Title,
            Events.InterfaceConsts.EventColor,
            Events.InterfaceConsts.CalendarId,
            Events.InterfaceConsts.AllDay
        };

        private const int calendarIdIndex = 0;
        private const int calendarDisplayNameIndex = 1;
        private const int calendarAccountNameIndex = 2;

        private const int eventIdIndex = 0;
        private const int eventStartDateIndex = 1;
        private const int eventEndDateIndex = 2;
        private const int eventDescriptionIndex = 3;
        private const int eventColorIndex = 4;
        private const int eventCalendarIdIndex = 5;
        private const int eventIsAllDayIndex = 6;

        public CalendarServiceAndroid(IPermissionsService permissionsService)
            : base(permissionsService)
        {
        }

        protected override IEnumerable<UserCalendar> NativeGetUserCalendars()
        {
            var appContext = Mvx.Resolve<IMvxAndroidGlobals>().ApplicationContext;

            var cursor = appContext.ContentResolver.Query(Calendars.ContentUri, calendarProjection, null, null, null);
            if (cursor.Count <= 0)
                yield break;

            while (cursor.MoveToNext())
            {
                var id = cursor.GetString(calendarIdIndex);
                var displayName = cursor.GetString(calendarDisplayNameIndex);
                var accountName = cursor.GetString(calendarAccountNameIndex);

                yield return new UserCalendar(id, displayName, accountName);
            }
        }

        protected override IEnumerable<CalendarItem> NativeGetEventsInRange(DateTimeOffset start, DateTimeOffset end)
        {
            var appContext = Mvx.Resolve<IMvxAndroidGlobals>().ApplicationContext;

            var selection =  getEventSelectionByDate(start, end);
            var cursor = appContext.ContentResolver.Query(Events.ContentUri, eventsProjection, selection, null, null);
            if (cursor.Count <= 0)
                yield break;

            while (cursor.MoveToNext())
            {
                var isAllDay = cursor.GetInt(eventIsAllDayIndex) == 1;
                if (isAllDay)
                    continue;

                yield return calendarItemFromCursor(cursor);
            }
        }

        protected override CalendarItem NativeGetCalendarItemWithId(string id)
        {
            var appContext = Mvx.Resolve<IMvxAndroidGlobals>().ApplicationContext;

            var cursor = appContext.ContentResolver.Query(Events.ContentUri, eventsProjection, $"({Events.InterfaceConsts.Id} = ?)", new [] { id }, null);
            if (cursor.Count <= 0)
                throw new InvalidOperationException("An invalid calendar Id was provided");

            cursor.MoveToNext();
            return calendarItemFromCursor(cursor);
        }

        private static CalendarItem calendarItemFromCursor(ICursor cursor)
        {
            var id = cursor.GetString(eventIdIndex);
            var startDateUnixTime = cursor.GetInt(eventStartDateIndex);
            var endDateUnixTime = cursor.GetInt(eventEndDateIndex);
            var description = cursor.GetString(eventDescriptionIndex);
            var color = cursor.GetString(eventColorIndex);
            var calendarId = cursor.GetString(eventCalendarIdIndex);

            var startDate = DateTimeOffset.FromUnixTimeMilliseconds(startDateUnixTime);

            return new CalendarItem(
                id: id,
                source: CalendarItemSource.Calendar,
                startTime: startDate,
                duration: DateTimeOffset.FromUnixTimeMilliseconds(endDateUnixTime) - startDate,
                description: description,
                iconKind: CalendarIconKind.Event,
                color: color,
                calendarId: calendarId
            );
        }

        private static string getEventSelectionByDate(DateTimeOffset startDate, DateTimeOffset endDate)
            => $"(({Events.InterfaceConsts.Dtstart} >= {startDate.ToUnixTimeMilliseconds()}) AND ({Events.InterfaceConsts.Dtend} <= {endDate.ToUnixTimeMilliseconds()}))";
    }
}
