using System.Reactive.Concurrency;
using Android.OS;
using Toggl.Multivac;

namespace Toggl.Giskard
{
    public sealed class AndroidSchedulerProvider : ISchedulerProvider
    {
        public IScheduler MainScheduler { get; }
        public IScheduler DefaultScheduler { get; }

        public AndroidSchedulerProvider()
        {
            MainScheduler = new HandlerScheduler(new Handler(Looper.MainLooper), Looper.MainLooper.Thread.Id);
            DefaultScheduler = Scheduler.Default;
        }
    }
}
