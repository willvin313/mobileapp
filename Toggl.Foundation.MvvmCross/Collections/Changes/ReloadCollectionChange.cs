using System;

namespace Toggl.Foundation.MvvmCross.Collections.Changes
{
    [Obsolete("We are moving into using CollectionSection and per platform diffing")]
    public struct ReloadCollectionChange : ICollectionChange
    {
        public override string ToString() => $"Reload";
    }
}
