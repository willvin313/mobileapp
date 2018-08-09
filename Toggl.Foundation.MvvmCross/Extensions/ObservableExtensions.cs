using System;
using System.Reactive;
using System.Threading.Tasks;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.Extensions
{
    public static class ObservableExtensions
    {
        public static IObservable<T> AsDriver<T>(this IObservable<T> observable, ISchedulerProvider schedulerProvider)
            => observable.AsDriver(default(T), schedulerProvider);

        public static IDisposable VoidSubscribe<T>(this IObservable<T> observable, Action onNext)
            => observable.Subscribe((T _) => onNext());

        public static IDisposable Subscribe(this IObservable<Unit> observable, Func<Task> onNext)
            => observable.Subscribe(async (Unit _) => await onNext());
    }
}
