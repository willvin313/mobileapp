using System;

namespace Toggl.Foundation.UI.Interfaces
{
    public interface IDiffableByIdentifier<T> : IEquatable<T>
    {
        long Identifier { get; }
    }
}
