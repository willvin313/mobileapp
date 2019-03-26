using System;
using System.Reactive;
using System.Threading.Tasks;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Services
{
    public interface IRxActionFactory
    {
        UIAction FromAction(Action action, IObservable<bool> enabledIf = null);
        UIAction FromAsync(Func<Task> asyncAction, IObservable<bool> enabledIf = null);
        UIAction FromObservable(Func<IObservable<Unit>> workFactory, IObservable<bool> enabledIf = null);

        InputAction<TInput> FromAction<TInput>(Action<TInput> action);
        InputAction<TInput> FromAsync<TInput>(Func<TInput, Task> asyncAction, IObservable<bool> enabledIf = null);
        InputAction<TInput> FromObservable<TInput>(Func<TInput, IObservable<Unit>> workFactory,
            IObservable<bool> enabledIf = null);

        OutputAction<TElement> FromFunction<TElement>(Func<TElement> action);
        OutputAction<TElement> FromAsync<TElement>(Func<Task<TElement>> asyncAction, IObservable<bool> enabledIf = null);
        OutputAction<TElement> FromObservable<TElement>(Func<IObservable<TElement>> workFactory,
            IObservable<bool> enabledIf = null);

        RxAction<TInput, TElement> FromFunction<TInput, TElement>(Func<TInput, TElement> action);
        RxAction<TInput, TElement> FromAsync<TInput, TElement>(Func<TInput, Task<TElement>> asyncAction, IObservable<bool> enabledIf = null);
        RxAction<TInput, TElement> FromObservable<TInput, TElement>(Func<TInput, IObservable<TElement>> workFactory,
            IObservable<bool> enabledIf = null);
    }
}
