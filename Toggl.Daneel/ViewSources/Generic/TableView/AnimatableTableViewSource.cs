using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using Foundation;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Collections.Diffing;
using UIKit;

namespace Toggl.Daneel.ViewSources.Generic.TableView
{
    internal abstract class AnimatableTableViewSource<TSection, THeader, TModel, TKey>
        : BaseTableViewSource<TSection, THeader, TModel>
        where TKey : IEquatable<TKey>
        where TSection : IAnimatableSectionModel<THeader, TModel, TKey>, new()
        where TModel : IDiffable<TKey>, IEquatable<TModel>
        where THeader : IDiffable<TKey>
    {
        public IObserver<IEnumerable<TSection>> AnimateChanges(UITableView tableView)
        {
            return Observer.Create<IEnumerable<TSection>>(finalSections =>
            {
                var initialSections = Sections;
                if (initialSections == null || initialSections.Count == 0)
                {
                    SetSections(finalSections);
                    tableView.ReloadData();
                    return;
                }

                // if view is not in view hierarchy, performing batch updates will crash the app
                if (tableView.Window == null)
                {
                    SetSections(finalSections);
                    tableView.ReloadData();
                    return;
                }

                var diff = new Diffing<TSection, THeader, TModel, TKey>(initialSections, finalSections);
                var changeset = diff.ComputeDifferences();

                // The changesets have to be applied one after another. Not in one transaction.
                // iOS is picky about the changes which can happen in a single transaction.
                // Don't put BeginUpdates() ... EndUpdates() around the foreach, it has to stay this way,
                // otherwise the app might crash from time to time.

                foreach (var difference in changeset)
                {
                    tableView.BeginUpdates();
                    SetSections(difference.FinalSections);
                    performChangesetUpdates(tableView, difference);
                    tableView.EndUpdates();
                }
            });
        }

        private void performChangesetUpdates(UITableView tableView, Diffing<TSection, THeader, TModel, TKey>.Changeset changes)
        {
            NSIndexSet newIndexSet(List<int> indexes)
            {
                var indexSet = new NSMutableIndexSet();
                foreach (var i in indexes)
                {
                    indexSet.Add((nuint) i);
                }

                return indexSet;
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
