using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public sealed class DelayStateTests
    {
        public sealed class TheConstructor
        {
            [Fact]
            public void ThrowsWhenArgumentIsNull()
            {
                Action createDelayState = () => new DelayState(null);

                createDelayState.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheStartMethod
        {
            [Theory]
            [MemberData(nameof(Delays))]
            public void DoesNotContinueBeforeTheDelayIsOver(TimeSpan delay)
            {
                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<ITransition>();
                var state = new DelayState(scheduler);

                state.Start(delay.ToPositive()).Subscribe(observer);
                scheduler.AdvanceBy(delay.ToPositive().Ticks - 10);

                observer.Messages.Should().BeEmpty();
            }

            [Theory]
            [MemberData(nameof(Delays))]
            public void CompletesWhenTheDelayIsOver(TimeSpan delay)
            {
                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<ITransition>();
                var state = new DelayState(scheduler);

                state.Start(delay.ToPositive()).Subscribe(observer);
                scheduler.AdvanceBy(delay.ToPositive().Ticks);

                observer.Messages.Should().HaveCount(1);
            }

            [Theory]
            [MemberData(nameof(Delays))]
            public void ReturnsTheContinueTransition(TimeSpan delay)
            {
                var scheduler = new TestScheduler();
                var observer = scheduler.CreateObserver<ITransition>();
                var state = new DelayState(scheduler);

                state.Start(delay.ToPositive()).Subscribe(observer);
                scheduler.AdvanceBy(delay.ToPositive().Ticks);

                observer.Messages.First().Value.Value.Result.Should().Be(state.Continue);
            }

            public static IEnumerable<object[]> Delays
                => new[]
                {
                    new object[] { TimeSpan.FromMilliseconds(1) },
                    new object[] { TimeSpan.FromSeconds(1) },
                    new object[] { TimeSpan.FromMinutes(1) },
                    new object[] { TimeSpan.FromHours(1) },
                    new object[] { TimeSpan.FromDays(1) }
                };
        }
    }

    internal static class TimeSpanExtensions
    {
        public static TimeSpan ToPositive(this TimeSpan timeSpan)
            => timeSpan >= TimeSpan.Zero ? timeSpan : timeSpan.Negate();
    }
}
