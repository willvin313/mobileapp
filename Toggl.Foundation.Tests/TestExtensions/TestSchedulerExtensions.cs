using System;
using System.Reactive;
using System.Reactive.Disposables;
using Microsoft.Reactive.Testing;

namespace Toggl.Foundation.Tests.TestExtensions
{
    public static class TestSchedulerExtensions
    {
        public static ITestableObserver<T> CreateObserverFor<T>(this TestScheduler scheduler, IObservable<T> observable)
        {
            var observer = scheduler.CreateObserver<T>();
            observable.Subscribe(observer);
            return observer;
        }
    }
}
