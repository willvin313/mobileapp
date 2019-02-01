using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Tests.TestExtensions
{
    public static class RxActionHelper
    {
        public static IObservable<Unit> ExecuteSequentially(this UIAction action, int numberOfTimes)
        {
            var observable = Enumerable
                .Range(0, numberOfTimes)
                .Select(_ => Observable.Defer(() => action.Execute()))
                .Concat();
            observable.Subscribe();

            return observable;
        }

        public static IObservable<Unit> ExecuteSequentially<TInput>(this InputAction<TInput> action, params TInput[] inputs)
        {
            var observable = inputs
                .Select(input => Observable.Defer(() => action.Execute(input)))
                .Concat();
            observable.Subscribe();

            return observable;
        }

        public static IObservable<Unit> ExecuteSequentially<TInput>(this InputAction<TInput> action, IEnumerable<TInput> inputs)
        {
            var observable = inputs
                .Select(input => Observable.Defer(() => action.Execute(input)))
                .Concat();
            observable.Subscribe();

            return observable;
        }
    }
}
