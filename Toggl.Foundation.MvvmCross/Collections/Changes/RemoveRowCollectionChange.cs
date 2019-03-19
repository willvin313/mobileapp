using System;

namespace Toggl.Foundation.MvvmCross.Collections.Changes
{
    [Obsolete("We are moving into using CollectionSection and per platform diffing")]
    public struct RemoveRowCollectionChange : ICollectionChange
    {
        public SectionedIndex Index { get; }

        public RemoveRowCollectionChange(SectionedIndex index)
        {
            Index = index;
        }

        public override string ToString() => $"Remove row: {Index}";
    }
}
