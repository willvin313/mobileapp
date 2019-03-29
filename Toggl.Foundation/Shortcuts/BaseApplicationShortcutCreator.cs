using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Shortcuts
{
    public abstract class BaseApplicationShortcutCreator : IApplicationShortcutCreator
    {
        private bool noTimeEntries;

        private readonly ApplicationShortcut reportsShortcut = new ApplicationShortcut(
            ApplicationUrls.Reports,
            Resources.Reports,
            ShortcutType.Reports
        );

        private readonly ApplicationShortcut startTimeEntryShortcut = new ApplicationShortcut(
            ApplicationUrls.StartTimeEntry,
            Resources.StartTimeEntry,
            ShortcutType.StartTimeEntry
        );

        private readonly ApplicationShortcut continueLastEntryShortcut = new ApplicationShortcut(
            ApplicationUrls.Main.ContinueLastEntry,
            Resources.ContinueLastEntry,
            ShortcutType.ContinueLastTimeEntry
        );

        private readonly ApplicationShortcut showCalendarShortcut = new ApplicationShortcut(
            ApplicationUrls.Calendar.Default,
            Resources.Calendar,
            ShortcutType.Calendar
        );

        private readonly HashSet<ShortcutType> supportedShortcutTypes;

        protected BaseApplicationShortcutCreator(HashSet<ShortcutType> supportedShortcutTypes)
        {
            this.supportedShortcutTypes = supportedShortcutTypes;
        }

        public void OnLogin(IInteractorFactory interactorFactory)
        {
            if (interactorFactory == null) return;

            useShortcutsWhichAreSupported(new[] { reportsShortcut, startTimeEntryShortcut, showCalendarShortcut });

            var visibleTimeEntries = interactorFactory.ObserveAllTimeEntriesVisibleToTheUser().Execute();

            visibleTimeEntries
                .Subscribe(timeEntries => noTimeEntries = timeEntries.None());

            visibleTimeEntries
                .Select(timeEntries => timeEntries.SingleOrDefault(timeEntry => timeEntry.IsRunning()))
                .DistinctUntilChanged()
                .Subscribe(onCurrentTimeEntryChanged);
        }

        public void OnLogout()
            => ClearAllShortCuts();

        private void onCurrentTimeEntryChanged(IDatabaseTimeEntry timeEntry)
        {
            var shortcuts = new List<ApplicationShortcut> { startTimeEntryShortcut };

            if (timeEntry == null && !noTimeEntries)
                shortcuts.Add(continueLastEntryShortcut);

            shortcuts.Add(reportsShortcut);
            shortcuts.Add(showCalendarShortcut);

            useShortcutsWhichAreSupported(shortcuts);
        }

        private void useShortcutsWhichAreSupported(IEnumerable<ApplicationShortcut> shortcuts)
        {
            SetShortcuts(shortcuts.Where(isShortcutSupported));
        }

        protected abstract void ClearAllShortCuts();

        protected abstract void SetShortcuts(IEnumerable<ApplicationShortcut> shortcuts);

        private bool isShortcutSupported(ApplicationShortcut shortcut)
            => supportedShortcutTypes?.Contains(shortcut.Type) ?? false;
    }
}
