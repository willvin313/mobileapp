using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Android.OS;

namespace Toggl.Giskard
{
    public sealed class HandlerScheduler : IScheduler
    {
        private readonly long looperId;
        private readonly Handler handler;

        public DateTimeOffset Now => DateTimeOffset.Now;

        public HandlerScheduler(Handler handler, long? threadIdAssociatedWithHandler)
        {
            this.handler = handler;
            looperId = threadIdAssociatedWithHandler ?? -1;
        }

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            bool isCancelled = false;
            var innerDisp = new SerialDisposable { Disposable = Disposable.Empty };

            if (looperId > 0 && looperId == Java.Lang.Thread.CurrentThread().Id)
                return action(this, state);

            handler.Post(() => 
            {
                if (isCancelled)
                    return;

                innerDisp.Disposable = action(this, state);
            });

            return new CompositeDisposable(
                Disposable.Create(() => isCancelled = true),
                innerDisp
            );
        }

        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            var isCancelled = false;
            var innerDisp = new SerialDisposable { Disposable = Disposable.Empty };

            handler.PostDelayed(() => 
            {
                if (isCancelled)
                    return;

                innerDisp.Disposable = action(this, state);
            }, dueTime.Ticks / TimeSpan.TicksPerMillisecond);

            return new CompositeDisposable(
                Disposable.Create(() => isCancelled = true),
                innerDisp
            );
        }

        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            if (dueTime <= Now)
                return Schedule(state, action);

            return Schedule(state, dueTime - Now, action);
        }
    }
}
