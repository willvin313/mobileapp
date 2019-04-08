﻿using System;
using System.Collections.Generic;
using System.Reactive;

namespace Toggl.Foundation.UI.Services
{
    public interface IDialogService
    {
        IObservable<bool> Confirm(
            string title,
            string message,
            string confirmButtonText,
            string dismissButtonText);

        IObservable<Unit> Alert(string title, string message, string buttonTitle);

        IObservable<bool> ConfirmDestructiveAction(ActionType type, params object[] formatArguments);

        IObservable<T> Select<T>(string title, IEnumerable<(string ItemName, T Item)> options, int initialSelectionIndex);
    }

    public enum ActionType
    {
        DiscardNewTimeEntry,
        DiscardEditingChanges,
        DeleteExistingTimeEntry,
        DeleteMultipleExistingTimeEntries,
        DiscardFeedback
    }
}
