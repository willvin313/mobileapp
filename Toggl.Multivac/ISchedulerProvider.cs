using System.Reactive.Concurrency;

namespace Toggl.Multivac
{
    public interface ISchedulerProvider
    {
        IScheduler MainScheduler { get; }
        IScheduler DefaultScheduler { get; }
    }
}
