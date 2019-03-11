using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public class AnimatableSectionModel<THeader, TItem, TKey> : IAnimatableSectionModel<THeader, TItem, TKey>
        where TKey : IEquatable<TKey>
        where THeader : IDiffable<TKey>
        where TItem : IDiffable<TKey>, IEquatable<TItem>
    {
        public THeader Header { get; private set; }
        public List<TItem> Items { get; private set; }

        public TKey Identity { get; private set; }

        public AnimatableSectionModel()
        {

        }

        public AnimatableSectionModel(THeader header, IEnumerable<TItem> items)
        {
            Header = header;
            Items = items.ToList();
            Identity = Header.Identity;
        }

        public void Initialize(THeader header, IEnumerable<TItem> items)
        {
            Header = header;
            Items = items.ToList();
            Identity = Header.Identity;
        }
    }
}
