using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.UI.Extensions;
using Toggl.Foundation.UI.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Notification = Toggl.Multivac.Notification;

namespace Toggl.Foundation.Calendar
{
    public abstract class PermissionAwareNotificationService : INotificationService
    {
        private readonly IPermissionsService permissionsService;

        protected PermissionAwareNotificationService(IPermissionsService permissionsService)
        {
            this.permissionsService = permissionsService;
        }

        public IObservable<Unit> Schedule(IImmutableList<Notification> notifications)
            => permissionsService
                .NotificationPermissionGranted
                .DeferAndThrowIfPermissionNotGranted(
                    () => NativeSchedule(notifications)
                );

        public IObservable<Unit> UnscheduleAllNotifications()
            => permissionsService
                .NotificationPermissionGranted
                .DeferAndThrowIfPermissionNotGranted(NativeUnscheduleAllNotifications);

        protected abstract IObservable<Unit> NativeSchedule(IImmutableList<Notification> notifications);

        protected abstract IObservable<Unit> NativeUnscheduleAllNotifications();
    }
}
