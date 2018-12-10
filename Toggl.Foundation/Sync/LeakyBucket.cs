using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Toggl.Multivac;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.Sync
{
    internal sealed class LeakyBucket : ILeakyBucket
    {
        private const int standardSlotsLimit = 60;
        private static readonly TimeSpan standardMovingWindowWidth = TimeSpan.FromSeconds(60);

        private readonly int slotsPerWindow;
        private readonly TimeSpan movingWindowSize;

        private readonly Queue<DateTimeOffset> historyWindow = new Queue<DateTimeOffset>();

        public LeakyBucket(
            int slotsPerWindow = standardSlotsLimit,
            TimeSpan? movingWindowSize = null)
        {
            if (slotsPerWindow <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    $"The value of {nameof(slotsPerWindow)} must be greater than 0, the constructor was given {slotsPerWindow}.");
            }

            this.slotsPerWindow = slotsPerWindow;
            this.movingWindowSize = movingWindowSize ?? standardMovingWindowWidth;
        }

        public bool TryClaimFreeSlot(DateTimeOffset now, out TimeSpan timeToFreeSlot)
            => TryClaimFreeSlots(now, numberOfSlots: 1, timeToFreeSlot: out timeToFreeSlot);

        public bool TryClaimFreeSlots(DateTimeOffset now, int numberOfSlots, out TimeSpan timeToFreeSlot)
        {
            lock (historyWindow)
            {
                if (numberOfSlots > slotsPerWindow)
                {
                    throw new InvalidOperationException(
                        $"It is not possible to allocate {numberOfSlots} slots because the maximum size of a window is {slotsPerWindow}.");
                }

                timeToFreeSlot = timeToNextFreeSlots(now, numberOfSlots);
                var claimSlots = timeToFreeSlot == TimeSpan.Zero;

                if (claimSlots)
                {
                    for (var i = 0; i < numberOfSlots; i++)
                        useSlot(now, historyWindow);
                }

                return claimSlots;
            }
        }

        private TimeSpan timeToNextFreeSlots(
            DateTimeOffset now,
            int numberOfParallelRequests)
        {
            var window = new Queue<DateTimeOffset>(historyWindow);
            var totalDelay = TimeSpan.Zero;

            for (var i = 0; i < numberOfParallelRequests; i++)
            {
                var delayTime = timeToNextFreeSlot(now + totalDelay, window);
                totalDelay += delayTime;
                useSlot(now + totalDelay, window);
            }

            return totalDelay;
        }

        private TimeSpan timeToNextFreeSlot(DateTimeOffset now, Queue<DateTimeOffset> window)
            => window.Count == slotsPerWindow
                ? calculateTimeUntilNextFreeSlot(
                    oldestRequest: window.Peek(),
                    now: now)
                : TimeSpan.Zero;

        private TimeSpan calculateTimeUntilNextFreeSlot(DateTimeOffset oldestRequest, DateTimeOffset now)
        {
            var elapsedTimeBetweenRequests = now - oldestRequest;
            return elapsedTimeBetweenRequests < movingWindowSize
                    ? movingWindowSize - elapsedTimeBetweenRequests
                    : TimeSpan.Zero;
        }

        private void useSlot(DateTimeOffset time, Queue<DateTimeOffset> window)
        {
            if (window.Count == slotsPerWindow)
            {
                window.Dequeue();
            }

            window.Enqueue(time);
        }
    }
}
