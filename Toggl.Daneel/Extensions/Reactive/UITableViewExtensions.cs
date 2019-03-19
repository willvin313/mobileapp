using System;
using System.Collections.Generic;
using System.Reactive;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Reactive;
using UIKit;

namespace Toggl.Daneel.Extensions.Reactive
{
    public static class UITableViewExtensions
    {
        public static IObserver<IEnumerable<TSection>> ReloadSections<TSection, THeader, TModel>(
            this IReactive<UITableView> reactive, BaseTableViewSource<TSection, THeader, TModel> dataSource)
        where TSection : ISectionModel<THeader, TModel>, new()
        {
            return Observer.Create<IEnumerable<TSection>>(list =>
            {
                dataSource.SetSections(list);
                reactive.Base.ReloadData();
            });
        }

        public static IObserver<IEnumerable<TModel>> ReloadItems<TSection, THeader, TModel>(
            this IReactive<UITableView> reactive, BaseTableViewSource<TSection, THeader, TModel> dataSource)
        where TSection : SectionModel<THeader, TModel>, new()
        {
            return Observer.Create<IEnumerable<TModel>>(list =>
            {
                dataSource.SetItems(list);
                reactive.Base.ReloadData();
            });
        }
    }
}
