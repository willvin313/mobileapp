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
        public static void RunSequentially(params UIAction[] actions)
        {
            actions
                .Select(RxActionExtensions.DeferredExecute)
                .Aggregate(Observable.Concat)
                .Subscribe();
        }

        public static void RunSequentially<T>(params Func<IObservable<T>>[] actions)
        {
            RunSequentially(actions.Select(Observable.Defer).ToArray());
        }

        public static void RunSequentially<T>(params IObservable<T>[] actions)
        {
            actions
                .Aggregate(Observable.Concat)
                .Subscribe();
        }
    }

    public static class RxActionExtensions
    {
        public static IObservable<Unit> DeferredExecute(this UIAction action)
            => Observable.Defer(() => action.Execute());

        public static IObservable<TElement> DeferredExecute<TInput, TElement>(this RxAction<TInput, TElement> action, TInput input)
            => Observable.Defer(() => action.Execute(input));
    }
}
