using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Sync;
using Xunit;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class RateLimiterTests
    {
        public sealed class TheWaitForFreeSlotMethod
        {
            private readonly ILeakyBucket leakyBucket = Substitute.For<ILeakyBucket>();
            private readonly TestScheduler scheduler = new TestScheduler();
            private readonly ITimeService timeService = Substitute.For<ITimeService>();
            private readonly IRateLimiter limiter;

            public TheWaitForFreeSlotMethod()
            {
                limiter = new RateLimiter(leakyBucket, timeService, scheduler);
            }

            [Fact]
            public async Task SendsTheRequestWhenThereIsAFreeSlot()
            {
                leakyBucket.TryClaimFreeSlot(Arg.Any<DateTimeOffset>(), out _).Returns(true);

                await limiter.WaitForFreeSlot();
            }
            [Fact]
            public void DelaysSendingTheRequestByTheGivenDelays()
            {
                var observer = scheduler.CreateObserver<Unit>();
                leakyBucket.TryClaimFreeSlot(Arg.Any<DateTimeOffset>(), out _)
                    .Returns(
                        x =>
                        {
                            x[1] = TimeSpan.FromSeconds(1);
                            return false;
                        },
                        x =>
                        {
                            x[1] = TimeSpan.FromSeconds(2);
                            return false;
                        },
                        x =>
                        {
                            x[1] = TimeSpan.FromSeconds(3);
                            return false;
                        },
                        x => true);

                limiter.WaitForFreeSlot().Subscribe(observer);

                scheduler.AdvanceBy(TimeSpan.FromSeconds(6).Ticks - 1);
                observer.Messages.Should().BeEmpty();
                scheduler.AdvanceBy(1);
                observer.Messages.Should().HaveCount(1);
            }
        }
    }
}
