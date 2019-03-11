using System;
using System.Collections.Generic;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public interface IDiffable<TKey>
        where TKey : IEquatable<TKey>
    {
        TKey Identity { get; }
    }

    public interface ISectionModel<THeader, TItem>
    {
        THeader Header { get; }
        List<TItem> Items { get; }
        void Initialize(THeader header, IEnumerable<TItem> items);
    }

    public interface IAnimatableSectionModel<TSection, TItem, TKey> : ISectionModel<TSection, TItem>, IDiffable<TKey>
        where TKey : IEquatable<TKey>
        where TItem : IDiffable<TKey>, IEquatable<TItem>
    { }
}
