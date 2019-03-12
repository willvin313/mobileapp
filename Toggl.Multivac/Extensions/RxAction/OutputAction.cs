using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Toggl.Multivac.Extensions
{
    public sealed class OutputAction<TOutput> : RxAction<Unit, TOutput>
    {
        private OutputAction(Func<Unit, IObservable<TOutput>> workFactory, IScheduler mainScheduler, IObservable<bool> enabledIf = null)
            : base(workFactory, mainScheduler, enabledIf)
        {
        }

        public void Execute()
        {
            Inputs.OnNext(Unit.Default);
        }

        public IObservable<TOutput> ExecuteWithCompletion()
            => ExecuteWithCompletion(Unit.Default);

        public static OutputAction<TOutput> FromAsync(Func<Task<TOutput>> asyncAction, IScheduler mainScheduler, IObservable<bool> enabledIf = null)
        {
            IObservable<TOutput> workFactory(Unit _)
                => asyncAction().ToObservable();

            return new OutputAction<TOutput>(workFactory, mainScheduler, enabledIf);
        }
    }
}
