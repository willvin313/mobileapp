using System;

namespace Toggl.Foundation.MvvmCross.Interfaces
{
    public interface IDiffable<T> : IEquatable<T>
    {
        long Identifier { get; }
    }
}
