using System;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.Services;
using Toggl.Multivac;

namespace Toggl.Giskard.Services
{
    public sealed class NotificationService : INotificationService
    {
        public IObservable<Unit> Schedule(IImmutableList<Multivac.Notification> notifications)
            => Observable.Return(Unit.Default);

        public IObservable<Unit> UnscheduleAllNotifications()
            => Observable.Return(Unit.Default);
    }
}
