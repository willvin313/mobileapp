using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Sync;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class LeakyBucketTests
    {
        public sealed class TheConstructor
        {
            [Property]
            public void ThrowsWhenHorizonIsLowerOrEqualToZero(NonNegativeInt slotsPerWindow)
            {
                Action creatingClient = () => new LeakyBucket(-slotsPerWindow.Get);

                creatingClient.Should().Throw<ArgumentOutOfRangeException>();
            }
        }

        public sealed class TryClaimFreeSlotMethod
        {
            private readonly DateTimeOffset baseTime
                = new DateTimeOffset(2018, 12, 1, 22, 12, 24, TimeSpan.FromHours(6));

            [Theory]
            [InlineData(2)]
            [InlineData(10)]
            [InlineData(60)]
            public void SendsAllRequestsInAShortPeriodOfTimeUntilReachingTheLimit(int slotsPerWindow)
            {
                var client = new LeakyBucket(slotsPerWindow);

                for (var i = 0; i < slotsPerWindow; i++)
                {
                    var claimed = client.TryClaimFreeSlot(baseTime, out var timeToNextFreeSlot);

                    claimed.Should().BeTrue();
                    timeToNextFreeSlot.Should().Be(TimeSpan.Zero);
                }
            }

            [Theory]
            [InlineData(2)]
            [InlineData(10)]
            [InlineData(60)]
            public void ReturnsNonZeroTimeToNextSlotTooManyRequestsAreSentInAShortPeriodOfTime(int slotsPerWindow)
            {
                var bucket = new LeakyBucket(slotsPerWindow);

                bucket.TryClaimFreeSlots(baseTime, slotsPerWindow, out _);
                var claimed = bucket.TryClaimFreeSlot(baseTime, out var time);

                claimed.Should().BeFalse();
                time.Should().BeGreaterThan(TimeSpan.Zero);
            }

            [Theory]
            [InlineData(2)]
            [InlineData(10)]
            [InlineData(60)]
            public void AllowsSlotsSpreadOutAcrossTheTimeLimitSoThatTheyAreNotSentTooCloseToEachOther(int slotsPerWindow)
            {
                var movingWindowSize = TimeSpan.FromSeconds(10);
                var uniformDelayBetweenRequests = movingWindowSize / slotsPerWindow;
                var times = new Queue<DateTimeOffset>(Enumerable.Range(1, 2 * slotsPerWindow).Select(n => baseTime + (n * uniformDelayBetweenRequests)));
                var bucket = new LeakyBucket(slotsPerWindow, movingWindowSize);

                for (var i = 0; i < times.Count; i++)
                {
                    var now = times.Dequeue();
                    var claimed = bucket.TryClaimFreeSlot(now, out var timeToNextSlot);

                    claimed.Should().BeTrue();
                    timeToNextSlot.Should().Be(TimeSpan.Zero);
                }
            }

            [Fact]
            public void CalculatesTheDelayUntilTheNextFreeSlot()
            {
                var bucket = new LeakyBucket(slotsPerWindow: 2, movingWindowSize: TimeSpan.FromSeconds(10));

                bucket.TryClaimFreeSlot(baseTime, out _);
                bucket.TryClaimFreeSlot(baseTime + TimeSpan.FromSeconds(6), out _);
                var claimed = bucket.TryClaimFreeSlot(baseTime + TimeSpan.FromSeconds(8), out var timeToFreeSlot);

                claimed.Should().BeFalse();
                timeToFreeSlot.Should().Be(TimeSpan.FromSeconds(2));
            }
        }

        public sealed class TheTryClaimFreeSlotsMethod
        {
            private readonly DateTimeOffset baseTime
                = new DateTimeOffset(2018, 12, 1, 22, 12, 24, TimeSpan.FromHours(6));

            [Theory]
            [InlineData(2)]
            [InlineData(10)]
            [InlineData(60)]
            public void AllowsSlotsInAShortPeriodOfTimeUntilReachingTheLimit(int slotsPerWindow)
            {
                var client = new LeakyBucket(slotsPerWindow);

                var claimed = client.TryClaimFreeSlots(baseTime, slotsPerWindow, out var timeToNextFreeSlot);

                claimed.Should().BeTrue();
                timeToNextFreeSlot.Should().Be(TimeSpan.Zero);
            }

            [Theory]
            [InlineData(2)]
            [InlineData(10)]
            [InlineData(60)]
            public void ReturnsNonZeroTimeToNextSlotWhenTooManySlotsAreUsedInAShortPeriodOfTime(int slotsPerWindowLimit)
            {
                var bucket = new LeakyBucket(slotsPerWindowLimit);
                bucket.TryClaimFreeSlot(baseTime, out _);

                var claimed = bucket.TryClaimFreeSlots(baseTime, slotsPerWindowLimit, out var time);

                claimed.Should().BeFalse();
                time.Should().BeGreaterThan(TimeSpan.Zero);
            }

            [Fact]
            public void CalculatesTheDelayUntilNextFreeSlot()
            {
                var bucket = new LeakyBucket(slotsPerWindow: 4, movingWindowSize: TimeSpan.FromSeconds(10));

                bucket.TryClaimFreeSlot(baseTime, out _);
                bucket.TryClaimFreeSlot(baseTime + TimeSpan.FromSeconds(3), out _);
                bucket.TryClaimFreeSlot(baseTime + TimeSpan.FromSeconds(6), out _);
                var claimed = bucket.TryClaimFreeSlots(baseTime + TimeSpan.FromSeconds(8), 3, out var timeToFreeSlot);

                claimed.Should().BeFalse();
                timeToFreeSlot.Should().Be(TimeSpan.FromSeconds(5));
            }

            [Property]
            public void ThrowsWhenTooManySlotsAreRequested(PositiveInt slotsPerWindow)
            {
                if (slotsPerWindow.Get == int.MaxValue) return;

                var bucket = new LeakyBucket(slotsPerWindow.Get);

                Action claimMany = () => bucket.TryClaimFreeSlots(baseTime, slotsPerWindow.Get + 1, out _);

                claimMany.Should().Throw<InvalidOperationException>();
            }
        }
    }
}
