using System;

namespace Toggl.Foundation.Sync
{
    public interface ILeakyBucket
    {
        bool TryClaimFreeSlot(DateTimeOffset now, out TimeSpan timeToFreeSlot);
        bool TryClaimFreeSlots(DateTimeOffset now, int numberOfSlots, out TimeSpan timeToFreeSlot);
    }
}
