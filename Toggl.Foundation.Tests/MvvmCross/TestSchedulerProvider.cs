using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using Toggl.Multivac;

namespace Toggl.Foundation.Tests.MvvmCross
{
    public sealed class TestSchedulerProvider : ISchedulerProvider
    {
        public readonly TestScheduler TestScheduler;

        public TestSchedulerProvider()
        {
            TestScheduler = new TestScheduler();
        }

        public IScheduler MainScheduler => TestScheduler;
        public IScheduler DefaultScheduler => TestScheduler;
    }
}
