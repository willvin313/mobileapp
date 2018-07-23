using System;
using System.Collections.Immutable;
using Toggl.Giskard.Adapters;

namespace Toggl.Giskard.Extensions
{
    public static partial class ViewExtensions
    {
        public static Action<IImmutableList<T>> BindItems<T>(this BaseRecyclerAdapter<T> adapter)
            => collection => adapter.Items = collection;
    }
}
