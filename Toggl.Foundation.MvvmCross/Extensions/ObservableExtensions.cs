using System;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.Exceptions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.Extensions
{
    public static class ObservableExtensions
    {
        public static IObservable<T> AsDriver<T>(this IObservable<T> observable, ISchedulerProvider schedulerProvider)
            => observable.AsDriver(default(T), schedulerProvider);

        public static IDisposable Subscribe(this IObservable<Unit> observable, Action onNext)
            => observable.Subscribe((Unit _) => onNext());

        public static IObservable<T> DeferAndThrowIfPermissionNotGranted<T>(this IObservable<bool> permissionGrantedObservable, Func<IObservable<T>> implementation)
            => Observable.DeferAsync(async cancellationToken =>
            {
                var isAuthorized = await permissionGrantedObservable;
                if (!isAuthorized)
                {
                    return Observable.Throw<T>(
                        new NotAuthorizedException("You don't have permission to schedule notifications")
                    );
                }

                return implementation();
            });
    }
}
