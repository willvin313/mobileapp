using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Foundation;
using MvvmCross;
using Toggl.Daneel.Cells;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Collections.Diffing;
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

        public static IObserver<IEnumerable<TSection>> AnimateSections<TSection, THeader, TModel, TKey>(
            this IReactive<UITableView> reactive,
            BaseTableViewSource<TSection, THeader, TModel> dataSource)
            where TKey : IEquatable<TKey>
            where TSection : IAnimatableSectionModel<THeader, TModel, TKey>, new()
            where TModel : IDiffable<TKey>, IEquatable<TModel>
            where THeader : IDiffable<TKey>
        {
            return Observer.Create<IEnumerable<TSection>>(finalSections =>
            {
                var initialSections = dataSource.Sections;
                if (initialSections == null || initialSections.Count == 0)
                {
                    dataSource.SetSections(finalSections);
                    reactive.Base.ReloadData();
                    return;
                }

                // if view is not in view hierarchy, performing batch updates will crash the app
                if (reactive.Base.Window == null)
                {
                    dataSource.SetSections(finalSections);
                    reactive.Base.ReloadData();
                    return;
                }

                var stopwatchProvider = IosDependencyContainer.Instance.StopwatchProvider;
                var stopwatch = stopwatchProvider.Create(MeasuredOperation.Diffing);
                stopwatch.Start();

                var diff = new Diffing<TSection, THeader, TModel, TKey>(initialSections, finalSections);
                var changeset = diff.ComputeDifferences();

                stopwatch.Stop();

                // The changesets have to be applied one after another. Not in one transaction.
                // iOS is picky about the changes which can happen in a single transaction.
                // Don't put BeginUpdates() ... EndUpdates() around the foreach, it has to stay this way,
                // otherwise the app might crash from time to time.

                foreach (var difference in changeset)
                {
                    reactive.Base.BeginUpdates();
                    dataSource.SetSections(difference.FinalSections);
                    reactive.Base.performChangesetUpdates(difference);
                    reactive.Base.EndUpdates();

                    foreach (var section in difference.UpdatedSections)
                    {
                        if (reactive.Base.GetHeaderView(section) is BaseTableHeaderFooterView<THeader> headerView)
                        {
                            headerView.Item = difference.FinalSections[section].Header;
                        }
                    }
                }
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

        private static void performChangesetUpdates<TSection, THeader, TModel, TKey>(
            this UITableView tableView,
            Diffing<TSection, THeader, TModel, TKey>.Changeset changes)
            where TKey : IEquatable<TKey>
            where TSection : IAnimatableSectionModel<THeader, TModel, TKey>, new()
            where TModel : IDiffable<TKey>, IEquatable<TModel>
            where THeader : IDiffable<TKey>

        {
            NSIndexSet newIndexSet(List<int> indexes)
            {
                var indexSet = new NSMutableIndexSet();
                foreach (var i in indexes)
                {
                    indexSet.Add((nuint) i);
                }

                return indexSet as NSIndexSet;
            }

            tableView.DeleteSections(newIndexSet(changes.DeletedSections), UITableViewRowAnimation.Fade);
            // Updated sections doesn't mean reload entire section, somebody needs to update the section view manually
            // otherwise all cells will be reloaded for nothing.
            tableView.InsertSections(newIndexSet(changes.InsertedSections), UITableViewRowAnimation.Fade);

            foreach (var (from, to) in changes.MovedSections)
            {
                tableView.MoveSection(from, to);
            }
            tableView.DeleteRows(
                changes.DeletedItems.Select(item => NSIndexPath.FromRowSection(item.itemIndex, item.sectionIndex)).ToArray(),
                UITableViewRowAnimation.Top
            );

            tableView.InsertRows(
                changes.InsertedItems.Select(item =>
                    NSIndexPath.FromItemSection(item.itemIndex, item.sectionIndex)).ToArray(),
                UITableViewRowAnimation.Automatic
            );
            tableView.ReloadRows(
                changes.UpdatedItems.Select(item => NSIndexPath.FromRowSection(item.itemIndex, item.sectionIndex))
                    .ToArray(),
                // No animation so it doesn't fade showing the cells behind it
                UITableViewRowAnimation.None
            );

            foreach (var (from, to) in changes.MovedItems)
            {
                tableView.MoveRow(
                    NSIndexPath.FromRowSection(from.itemIndex, from.sectionIndex),
                    NSIndexPath.FromRowSection(to.itemIndex, to.sectionIndex)
                );
            }
        }
    }
}
