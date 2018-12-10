using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Toggl.Multivac;

namespace Toggl.Foundation.Sync.States
{
    public sealed class DelayState : ISyncState<TimeSpan>
    {
        private readonly IScheduler scheduler;

        public StateResult Continue { get; } = new StateResult();

        public DelayState(IScheduler scheduler)
        {
            Ensure.Argument.IsNotNull(scheduler, nameof(scheduler));

            this.scheduler = scheduler;
        }

        public IObservable<ITransition> Start(TimeSpan delay)
            => Observable.Return(Continue.Transition())
                .Delay(delay, scheduler);
    }
}
