using System;
using Toggl.Foundation.MvvmCross.Collections;

namespace Toggl.Daneel.ViewSources.Generic.TableView
{
    internal abstract class AnimatableTableViewSource<TSection, THeader, TModel, TKey>
        : BaseTableViewSource<TSection, THeader, TModel>
        where TKey : IEquatable<TKey>
        where TSection : IAnimatableSectionModel<THeader, TModel, TKey>, new()
        where TModel : IDiffable<TKey>, IEquatable<TModel>
        where THeader : IDiffable<TKey>
    {
    }
}
