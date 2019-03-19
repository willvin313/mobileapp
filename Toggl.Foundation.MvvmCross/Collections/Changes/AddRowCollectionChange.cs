using System;

namespace Toggl.Foundation.MvvmCross.Collections.Changes
{
    [Obsolete("We are moving into using CollectionSection and per platform diffing")]
    public struct AddRowCollectionChange<T> : ICollectionChange
    {
        public SectionedIndex Index { get; }

        public T Item { get; }

        public AddRowCollectionChange(SectionedIndex index, T item)
        {
            Index = index;
            Item = item;
        }

        public override string ToString() => $"Add row: {Index} ({Item})";
    }
}
