using System;
using Toggl.Foundation.MvvmCross.Interfaces;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    public sealed class UserCalendarSourceViewModel : IDiffableByIdentifier<UserCalendarSourceViewModel>
    {
        public string Name { get; }

        public long Identifier => Name.GetHashCode();

        public UserCalendarSourceViewModel(string name)
        {
            Name = name;
        }

        public bool Equals(UserCalendarSourceViewModel other)
            => other != null
                && Name == other.Name;
    }
}
