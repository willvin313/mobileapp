using System;

namespace Toggl.Foundation.MvvmCross.Collections.Diffing
{
    public sealed class DuplicateSectionException<TKey> : Exception
    {
        public TKey DuplicatedIdentity { get; }

        public DuplicateSectionException(TKey identity)
        {
            DuplicatedIdentity = identity;
        }
    }
}
