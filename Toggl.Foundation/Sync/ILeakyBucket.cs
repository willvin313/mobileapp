using System;

namespace Toggl.Foundation.Sync
{
    internal interface ILeakyBucket
    {
        void SlotWasUsed(DateTimeOffset time);
        void SlotsWereUsed(DateTimeOffset time, int numberOfSlots);
        bool HasFreeSlot(DateTimeOffset now, out TimeSpan timeToFreeSlot);
        bool HasFreeSlots(DateTimeOffset now, int numberOfSlots, out TimeSpan timeToFreeSlot);
    }
}
