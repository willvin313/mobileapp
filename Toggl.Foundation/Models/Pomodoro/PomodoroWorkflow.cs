using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Models.Pomodoro
{
    public class PomodoroWorkflow : IEquatable<PomodoroWorkflow>
    {
        private static ItemsComparer itemsComparer = new ItemsComparer();

        public string Id { get; }
        public PomodoroWorkflowType Type { get; }
        public string Name { get; }
        public IReadOnlyList<PomodoroWorkflowItem> Items { get; }

        public PomodoroWorkflow(string id, PomodoroWorkflowType type, string name, IEnumerable<PomodoroWorkflowItem> items)
        {
            Id = id;
            Type = type;
            Name = name;
            Items = items.ToList();
        }

        public bool Equals(PomodoroWorkflow other)
        {
            if (other == null)
                return false;

            return Id == other.Id
                && Type == other.Type
                && Name == other.Name
                && Items.SequenceEqual(other.Items, itemsComparer);
        }

        public PomodoroWorkflow Clone()
            => new PomodoroWorkflow(Id, Type, Name, Items);

        public PomodoroWorkflow CloneWithUpdatedItems(IEnumerable<PomodoroWorkflowItem> newItems)
            => new PomodoroWorkflow(Id, Type, Name, newItems);

        private class ItemsComparer : IEqualityComparer<PomodoroWorkflowItem>
        {
            public bool Equals(PomodoroWorkflowItem itemA, PomodoroWorkflowItem itemB)
                => itemA.Equals(itemB);

            public int GetHashCode(PomodoroWorkflowItem item)
                => item.GetHashCode();
        }
    }

}
