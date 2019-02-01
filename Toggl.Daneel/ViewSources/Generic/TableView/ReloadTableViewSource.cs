using System.Collections.Immutable;

namespace Toggl.Daneel.ViewSources
{
    public class ReloadTableViewSource<TModel> : BaseTableViewSource<TModel>
    {
        public ReloadTableViewSource(CellConfiguration configureCell, IImmutableList<IImmutableList<TModel>> sections)
            : base(configureCell, sections)
        {
        }

        public ReloadTableViewSource(CellConfiguration configureCell, IImmutableList<TModel> items = null)
            : base(configureCell, items == null ? ImmutableList<IImmutableList<TModel>>.Empty : ImmutableList.Create(items))
        {
        }

        public void SetItems(IImmutableList<IImmutableList<TModel>> sections)
        {
            this.Sections = sections;
        }

        public void SetItems(IImmutableList<TModel> items)
        {
            Sections = ImmutableList.Create(items);
        }
    }
}
