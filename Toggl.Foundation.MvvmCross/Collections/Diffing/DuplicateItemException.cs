using System;

namespace Toggl.Foundation.MvvmCross.Collections.Diffing
{
    public sealed class DuplicateItemException<TKey> : Exception
    {
        public TKey DuplicatedIdentity { get; }

        public DuplicateItemException(TKey identity)
        {
            DuplicatedIdentity = identity;
        }
    }
}
