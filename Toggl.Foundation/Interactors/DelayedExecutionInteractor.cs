using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Toggl.Foundation.Interactors
{
    public sealed class DelayedInteractorExecution<T>
        : IInteractor<IObservable<T>>
    {
        private readonly IInteractor<T> interactor;
        private readonly TimeSpan delay;
        private readonly IScheduler scheduler;

        private IDisposable subscription;

        public DelayedInteractorExecution(
            IInteractor<T> interactor,
            TimeSpan delay,
            IScheduler scheduler)
        {
            this.interactor = interactor;
            this.delay = delay;
            this.scheduler = scheduler;
        }

        public IObservable<T> Execute()
        {
            var result = new Subject<T>();
            if (subscription != null)
            {
                throw new InvalidOperationException("Interactor can't be executed repeatedly.");
            }

            subscription = Observable.Return(Unit.Default)
                .Delay(delay, scheduler)
                .Select(_ => interactor.Execute())
                .Subscribe(result);

            return result.AsObservable();
        }

        public T ExecuteImmediately()
        {
            Cancel();
            return interactor.Execute();
        }

        public void Cancel()
        {
            if (subscription == null)
            {
                throw new InvalidOperationException("Interactor hasn't been executed yet and so it can't be cancelled.");
            }

            subscription.Dispose();
        }
    }
}
