using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Calendar
{
    public abstract class PermissionAwareCalendarService : ICalendarService
    {
        private readonly IPermissionsService permissionsService;

        protected PermissionAwareCalendarService(IPermissionsService permissionsService)
        {
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));

            this.permissionsService = permissionsService;
        }

        public IObservable<IEnumerable<CalendarItem>> GetEventsForDate(DateTime date)
        {
            var startOfDay = new DateTimeOffset(date.Date);
            var endOfDay = startOfDay.AddDays(1);

            return GetEventsInRange(startOfDay, endOfDay);
        }

        public IObservable<CalendarItem> GetEventWithId(string id)
            => permissionsService
                .CalendarPermissionGranted
                .DeferAndThrowIfPermissionNotGranted(
                    () => Observable.Return(NativeGetCalendarItemWithId(id))
                );

        public IObservable<IEnumerable<CalendarItem>> GetEventsInRange(DateTimeOffset start, DateTimeOffset end)
            => permissionsService
                .CalendarPermissionGranted
                .DeferAndThrowIfPermissionNotGranted(
                    () => Observable.Return(NativeGetEventsInRange(start, end))
                );

        public IObservable<IEnumerable<UserCalendar>> GetUserCalendars()
            => permissionsService
                .CalendarPermissionGranted
                .DeferAndThrowIfPermissionNotGranted(
                    () => Observable.Return(NativeGetUserCalendars())
                );

        protected abstract CalendarItem NativeGetCalendarItemWithId(string id);

        protected abstract IEnumerable<UserCalendar> NativeGetUserCalendars();

        protected abstract IEnumerable<CalendarItem> NativeGetEventsInRange(DateTimeOffset start, DateTimeOffset end);
    }
}
